Imports System.Drawing
Imports System.Numerics
Imports System.Runtime.InteropServices
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Drawing3D
Imports Brush = Microsoft.VisualBasic.Imaging.Brush
Imports SolidBrush = Microsoft.VisualBasic.Imaging.SolidBrush
Imports std = System.Math
Imports Tpl = System.Threading.Tasks.Parallel

Namespace Scene3D

    ''' <summary>
    ''' one corner of a face on the gpu: the position, the normal of the face and
    ''' the color of the face.
    ''' </summary>
    ''' <remarks>
    ''' The three corners of a triangle repeat the very same normal and color
    ''' value, so the flat shading of the cpu painter is reproduced by the
    ''' interpolator of the gpu for free. The size of this structure is the
    ''' stride of <see cref="Scene3DInputLayout.SurfaceStride"/>.
    ''' </remarks>
    <StructLayout(LayoutKind.Sequential)>
    Friend Structure SurfaceVertex
        Public X As Single
        Public Y As Single
        Public Z As Single
        Public NX As Single
        Public NY As Single
        Public NZ As Single
        Public R As Byte
        Public G As Byte
        Public B As Byte
        Public A As Byte
    End Structure

    ''' <summary>
    ''' one point of a point cloud on the gpu.
    ''' </summary>
    ''' <remarks>
    ''' The structure is shared by both point modes: the heat map mode fills the
    ''' heat value and the optional embedded color, the lambert mode fills the
    ''' normal of the face that the point belongs to and leaves the color empty
    ''' (an alpha of zero means that the point has no color of its own).
    ''' The size of this structure is the stride of
    ''' <see cref="Scene3DInputLayout.PointInstanceStride"/>.
    ''' </remarks>
    <StructLayout(LayoutKind.Sequential)>
    Friend Structure PointInstance
        Public X As Single
        Public Y As Single
        Public Z As Single
        Public NX As Single
        Public NY As Single
        Public NZ As Single
        Public Heat As Single
        Public R As Byte
        Public G As Byte
        Public B As Byte
        Public A As Byte
    End Structure

    ''' <summary>
    ''' the raw buffer and the texture handles of a blittable structure array
    ''' </summary>
    <StructLayout(LayoutKind.Sequential)>
    Friend Structure QuadCorner
        Public X As Single
        Public Y As Single
    End Structure

    ''' <summary>
    ''' one end point of a connection line on the gpu: the position plus the color
    ''' of the line itself.
    ''' </summary>
    ''' <remarks>
    ''' Both end points of one line carry the same color, so a line list of these
    ''' vertices draws a solid colored line. The size of this structure is the
    ''' stride of <see cref="Scene3DInputLayout.LineStride"/>.
    ''' </remarks>
    <StructLayout(LayoutKind.Sequential)>
    Friend Structure LineVertex
        Public X As Single
        Public Y As Single
        Public Z As Single
        Public R As Byte
        Public G As Byte
        Public B As Byte
        Public A As Byte
    End Structure

    ''' <summary>
    ''' The gpu mirror of the geometry of one <see cref="Scene"/>.
    ''' </summary>
    ''' <remarks>
    ''' The geometry is uploaded once and is reused by every frame afterwards:
    ''' the per frame work of the gpu back end is limited to the constant buffer
    ''' and to a handful of draw calls, which is the whole point of the 3d
    ''' pipeline. The object is rebuilt whenever the scene revision, the heat map
    ''' scheme or the point cloud coloring changes (see the signature that the
    ''' owner computes through
    ''' <see cref="SceneRenderOptions.GeometrySignature"/>).
    ''' </remarks>
    Friend NotInheritable Class GpuSceneGeometry : Implements IDisposable

        Private ReadOnly m_device As DxDevice
        Private ReadOnly m_palette As New SceneColorPalette()
        Private ReadOnly m_signature As String
        Private ReadOnly m_pointMode As Single

        Private m_surfaceBuffer As IntPtr = IntPtr.Zero
        Private m_surfaceVertexCount As Integer = 0
        Private m_groundBuffer As IntPtr = IntPtr.Zero
        Private m_groundVertexCount As Integer = 0
        Private m_quadBuffer As IntPtr = IntPtr.Zero
        Private m_lineBuffer As IntPtr = IntPtr.Zero
        Private m_lineVertexCount As Integer = 0
        Private m_instanceBuffer As IntPtr = IntPtr.Zero
        Private m_instanceCount As Integer = 0
        Private m_paletteTexture As IntPtr = IntPtr.Zero
        Private m_paletteView As IntPtr = IntPtr.Zero
        Private m_paletteLevels As Integer = 0

        ''' <summary>
        ''' the unit normals of the faces, they are kept in the managed memory
        ''' because the lambert point mode evaluates them again for every frame
        ''' </summary>
        Private m_faceNormals As Vector3() = Nothing

        Private m_disposed As Boolean = False

        ''' <summary>
        ''' the identity of the scene data that this geometry was built from
        ''' </summary>
        Friend ReadOnly Property Signature As String
            Get
                Return m_signature
            End Get
        End Property

        ''' <summary>
        ''' how many points are drawn by this geometry
        ''' </summary>
        Friend ReadOnly Property InstanceCount As Integer
            Get
                Return m_instanceCount
            End Get
        End Property

        ''' <summary>
        ''' the point mode that belongs to the content of the scene: a model is
        ''' drawn as a lit point cloud, a point cloud keeps its own heat values
        ''' </summary>
        Friend ReadOnly Property PointMode As Single
            Get
                Return m_pointMode
            End Get
        End Property

        ''' <summary>
        ''' the number of the sampled colors of the palette texture
        ''' </summary>
        Friend ReadOnly Property PaletteLevels As Integer
            Get
                Return m_paletteLevels
            End Get
        End Property

        Friend Sub New(device As DxDevice, scene As Scene, options As SceneRenderOptions, signature As String)
            m_device = device
            m_signature = signature
            m_pointMode = If(scene.SurfaceCount > 0, Scene3DShaders.PointModeLit, Scene3DShaders.PointModeHeat)

            Call BuildPalette(options)
            Call BuildSurfaces(scene)
            Call BuildGround(scene)
            Call BuildLines(scene)
            Call BuildQuad()

            ' the point instances are only needed by the point cloud modes, so
            ' they are built when they are requested the first time
            If scene.SurfaceCount = 0 AndAlso scene.PointCount > 0 Then
                Call BuildCloudInstances(scene, options)
            End If
        End Sub

        ''' <summary>
        ''' the vertex buffer of the faces of the model, three corners per
        ''' triangle
        ''' </summary>
        Friend ReadOnly Property SurfaceBuffer As IntPtr
            Get
                Return m_surfaceBuffer
            End Get
        End Property

        ''' <summary>
        ''' the number of the vertices in <see cref="SurfaceBuffer"/>
        ''' </summary>
        Friend ReadOnly Property SurfaceVertexCount As Integer
            Get
                Return m_surfaceVertexCount
            End Get
        End Property

        ''' <summary>
        ''' the line list of the ground grid
        ''' </summary>
        Friend ReadOnly Property GroundBuffer As IntPtr
            Get
                Return m_groundBuffer
            End Get
        End Property

        ''' <summary>
        ''' the number of the vertices in <see cref="GroundBuffer"/>
        ''' </summary>
        Friend ReadOnly Property GroundVertexCount As Integer
            Get
                Return m_groundVertexCount
            End Get
        End Property

        ''' <summary>
        ''' the line list of the connection lines of the scene, two vertices per
        ''' line
        ''' </summary>
        Friend ReadOnly Property LineBuffer As IntPtr
            Get
                Return m_lineBuffer
            End Get
        End Property

        ''' <summary>
        ''' the number of the vertices in <see cref="LineBuffer"/>
        ''' </summary>
        Friend ReadOnly Property LineVertexCount As Integer
            Get
                Return m_lineVertexCount
            End Get
        End Property

        ''' <summary>
        ''' the six corners of the unit quad of one point
        ''' </summary>
        Friend ReadOnly Property QuadBuffer As IntPtr
            Get
                Return m_quadBuffer
            End Get
        End Property

        ''' <summary>
        ''' the shader resource view of the palette texture
        ''' </summary>
        Friend ReadOnly Property PaletteView As IntPtr
            Get
                Return m_paletteView
            End Get
        End Property

        ''' <summary>
        ''' the instance buffer of the points, it is created on demand
        ''' </summary>
        ''' <remarks>
        ''' a model is drawn as a point cloud by instancing its own vertices, so
        ''' this buffer duplicates the vertex buffer of the model: building it
        ''' lazily keeps the memory of the common surface mode small.
        ''' </remarks>
        Friend Function EnsureInstances(scene As Scene, options As SceneRenderOptions) As IntPtr
            If m_instanceBuffer = IntPtr.Zero Then
                If scene.SurfaceCount > 0 Then
                    Call BuildModelInstances(scene)
                ElseIf scene.PointCount > 0 Then
                    Call BuildCloudInstances(scene, options)
                End If
            End If

            Return m_instanceBuffer
        End Function

        ''' <summary>
        ''' the lowest and the highest lambert factor of the faces of the model
        ''' for the current camera.
        ''' </summary>
        ''' <remarks>
        ''' The cpu painter normalizes the heat values of the lit point mode over
        ''' the actual factor range of the faces, and that range moves with the
        ''' camera, so it has to be evaluated again for every frame of that mode.
        ''' The pass only walks over the small normal array of the faces, it does
        ''' not build any geometry.
        ''' </remarks>
        Friend Function ComputeLitHeatRange(camera As Camera) As Vector2
            Dim normals As Vector3() = m_faceNormals

            If normals Is Nothing OrElse normals.Length = 0 Then
                Return New Vector2(0, 1)
            End If

            Dim rotation As Matrix4x4 = SceneTransform.RotationMatrix(camera)
            Dim light As Point3D = camera.LightDirection
            Dim ambient As Double = camera.AmbientStrength
            Dim n As Integer = normals.Length
            Dim parts As Integer = std.Max(1, std.Min(Environment.ProcessorCount, n))
            Dim perPart As Integer = (n + parts - 1) \ parts
            Dim lo(parts - 1) As Double
            Dim hi(parts - 1) As Double

            Tpl.For(0, parts,
                Sub(p As Integer)
                    Dim start As Integer = p * perPart
                    Dim [end] As Integer = std.Min(n, start + perPart) - 1
                    Dim low As Double = Double.MaxValue
                    Dim high As Double = Double.MinValue

                    For i As Integer = start To [end]
                        Dim nr As Vector3 = Vector3.Transform(normals(i), rotation)

                        If nr.Z < 0 Then
                            nr = Vector3.Negate(nr)
                        End If

                        Dim diffuse As Double = std.Max(0, nr.X * light.X + nr.Y * light.Y + nr.Z * light.Z)
                        Dim factor As Double = ambient + (1 - ambient) * diffuse

                        If factor < low Then low = factor
                        If factor > high Then high = factor
                    Next

                    lo(p) = low
                    hi(p) = high
                End Sub)

            Dim minFactor As Double = Double.MaxValue
            Dim maxFactor As Double = Double.MinValue

            For p As Integer = 0 To parts - 1
                If lo(p) < minFactor Then minFactor = lo(p)
                If hi(p) > maxFactor Then maxFactor = hi(p)
            Next

            If minFactor = Double.MaxValue OrElse maxFactor = Double.MinValue Then
                Return New Vector2(0, 1)
            End If

            Dim range As Double = maxFactor - minFactor

            If range < 1.0E-09 Then
                range = 1
            End If

            Return New Vector2(CSng(minFactor), CSng(1.0 / range))
        End Function

        ''' <summary>
        ''' the base color of a face, this is the very same rule that the cpu
        ''' painter applies: only a solid brush has a color, everything else is
        ''' black
        ''' </summary>
        Friend Shared Function FaceColor(face As Surface) As Color
            If TypeOf face.brush Is SolidBrush Then
                Return DirectCast(face.brush, SolidBrush).Color
            End If

            Return Color.Black
        End Function

        ''' <summary>
        ''' the unit normal of a face, it is computed from the first three
        ''' vertices exactly as the lambert shading of the cpu pipeline does.
        ''' A degenerate face keeps a zero normal, the shader then keeps the base
        ''' color of the face.
        ''' </summary>
        Friend Shared Function FaceNormal(vertices As Point3D()) As Vector3
            If vertices Is Nothing OrElse vertices.Length < 3 Then
                Return New Vector3(0, 0, 0)
            End If

            Dim a As Point3D = vertices(0)
            Dim b As Point3D = vertices(1)
            Dim c As Point3D = vertices(2)
            Dim nx As Double = (b.Y - a.Y) * (c.Z - a.Z) - (b.Z - a.Z) * (c.Y - a.Y)
            Dim ny As Double = (b.Z - a.Z) * (c.X - a.X) - (b.X - a.X) * (c.Z - a.Z)
            Dim nz As Double = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X)
            Dim mag As Double = std.Sqrt(nx * nx + ny * ny + nz * nz)

            If mag = 0 Then
                Return New Vector3(0, 0, 0)
            End If

            Return New Vector3(CSng(nx / mag), CSng(ny / mag), CSng(nz / mag))
        End Function

        ''' <summary>
        ''' build the triangle list of the faces of the model, every polygon is
        ''' fanned out into triangles
        ''' </summary>
        Private Sub BuildSurfaces(scene As Scene)
            Dim faces As Surface() = scene.Surfaces
            Dim n As Integer = If(faces Is Nothing, 0, faces.Length)

            If n = 0 Then
                m_faceNormals = New Vector3() {}
                Return
            End If

            Dim normals(n - 1) As Vector3
            Dim vertices As New List(Of SurfaceVertex)(n * 3)

            For i As Integer = 0 To n - 1
                Dim face As Surface = faces(i)
                Dim points As Point3D() = face.vertices
                Dim normal As Vector3 = FaceNormal(points)
                Dim color As Color = FaceColor(face)

                normals(i) = normal

                ' a polygon with more than three corners is fanned out into
                ' triangles that all share the first corner
                For k As Integer = 1 To points.Length - 2
                    Call AddVertex(vertices, points(0), normal, color)
                    Call AddVertex(vertices, points(k), normal, color)
                    Call AddVertex(vertices, points(k + 1), normal, color)
                Next
            Next

            m_faceNormals = normals
            m_surfaceVertexCount = vertices.Count

            If vertices.Count = 0 Then
                Return
            End If

            Dim data As SurfaceVertex() = vertices.ToArray()

            m_surfaceBuffer = CreateImmutableBuffer(data, D3D11_BIND_FLAG.VERTEX_BUFFER)
        End Sub

        Private Shared Sub AddVertex(buffer As List(Of SurfaceVertex), p As Point3D, normal As Vector3, color As Color)
            Call buffer.Add(New SurfaceVertex With {
                .X = CSng(p.X),
                .Y = CSng(p.Y),
                .Z = CSng(p.Z),
                .NX = normal.X,
                .NY = normal.Y,
                .NZ = normal.Z,
                .R = color.R,
                .G = color.G,
                .B = color.B,
                .A = color.A
            })
        End Sub

        ''' <summary>
        ''' build the square ground grid below the model, it is the very same
        ''' grid that the cpu painter draws
        ''' </summary>
        Private Sub BuildGround(scene As Scene)
            Dim half As Double = scene.Radius * 2.0
            Const divisions As Integer = 20
            Dim stepv As Double = (2.0 * half) / divisions
            Dim z As Double = scene.GroundZ
            Dim lines As New List(Of Vector3)((divisions + 1) * 4)

            For i As Integer = 0 To divisions
                Dim t As Double = -half + i * stepv

                Call lines.Add(New Vector3(CSng(-half), CSng(t), CSng(z)))
                Call lines.Add(New Vector3(CSng(half), CSng(t), CSng(z)))
                Call lines.Add(New Vector3(CSng(t), CSng(-half), CSng(z)))
                Call lines.Add(New Vector3(CSng(t), CSng(half), CSng(z)))
            Next

            m_groundVertexCount = lines.Count
            m_groundBuffer = CreateImmutableBuffer(lines.ToArray(), D3D11_BIND_FLAG.VERTEX_BUFFER)
        End Sub

        ''' <summary>
        ''' build the vertex buffer of the connection lines of the scene
        ''' </summary>
        ''' <remarks>
        ''' One line becomes two vertices of a line list and both vertices repeat
        ''' the color of the line (see <see cref="LineSegment"/>). The array is
        ''' allocated with its final size instead of being collected into a list
        ''' first: a connectome of a whole brain provides millions of lines, so the
        ''' doubling of the peak memory of a list plus a ToArray copy is worth
        ''' avoiding here.
        ''' The buffer is immutable like every other buffer of this class: the scene
        ''' revision changes whenever the caller loads another set of lines, which
        ''' rebuilds the whole geometry (see D3D11ScenePipeline.GeometryOf).
        ''' </remarks>
        Private Sub BuildLines(scene As Scene)
            Dim lines As LineSegment() = scene.Lines
            Dim n As Integer = If(lines Is Nothing, 0, lines.Length)

            If n = 0 Then
                Return
            End If

            Dim vertices(n * 2 - 1) As LineVertex

            For i As Integer = 0 To n - 1
                Dim line As LineSegment = lines(i)
                Dim color As Color = If(line.Color.A = 0, LineSegment.DefaultColor, line.Color)
                Dim head As Integer = i * 2

                vertices(head) = New LineVertex With {
                    .X = CSng(line.A.X),
                    .Y = CSng(line.A.Y),
                    .Z = CSng(line.A.Z),
                    .R = color.R,
                    .G = color.G,
                    .B = color.B,
                    .A = color.A
                }
                vertices(head + 1) = New LineVertex With {
                    .X = CSng(line.B.X),
                    .Y = CSng(line.B.Y),
                    .Z = CSng(line.B.Z),
                    .R = color.R,
                    .G = color.G,
                    .B = color.B,
                    .A = color.A
                }
            Next

            m_lineVertexCount = vertices.Length
            m_lineBuffer = CreateImmutableBuffer(vertices, D3D11_BIND_FLAG.VERTEX_BUFFER)
        End Sub

        ''' <summary>
        ''' build the unit quad that every point is expanded into
        ''' </summary>
        Private Sub BuildQuad()
            Dim corners As QuadCorner() = {
                New QuadCorner With {.X = -0.5F, .Y = -0.5F},
                New QuadCorner With {.X = 0.5F, .Y = -0.5F},
                New QuadCorner With {.X = 0.5F, .Y = 0.5F},
                New QuadCorner With {.X = -0.5F, .Y = -0.5F},
                New QuadCorner With {.X = 0.5F, .Y = 0.5F},
                New QuadCorner With {.X = -0.5F, .Y = 0.5F}
            }

            m_quadBuffer = CreateImmutableBuffer(corners, D3D11_BIND_FLAG.VERTEX_BUFFER)
        End Sub

        ''' <summary>
        ''' build the point instances of a point cloud, the heat value is the
        ''' normalized intensity exactly as the cpu painter computes it
        ''' </summary>
        Private Sub BuildCloudInstances(scene As Scene, options As SceneRenderOptions)
            Dim cloud As PointCloudPoint() = scene.Points
            Dim count As Integer = If(cloud Is Nothing, 0, cloud.Length)

            If count = 0 Then
                Return
            End If

            Dim minIntensity As Double = scene.IntensityMin
            Dim maxIntensity As Double = scene.IntensityMax
            Dim range As Double = maxIntensity - minIntensity

            If range = 0 Then
                range = 1
            End If

            Dim instances(count - 1) As PointInstance
            Dim inspectEmbedded As Boolean = options.UseEmbeddedColor

            Tpl.For(0, count,
                Sub(i As Integer)
                    Dim p As PointCloudPoint = cloud(i)
                    Dim instance As New PointInstance With {
                        .X = CSng(p.X),
                        .Y = CSng(p.Y),
                        .Z = CSng(p.Z),
                        .NX = 0,
                        .NY = 0,
                        .NZ = 0,
                        .Heat = 0,
                        .R = 0,
                        .G = 0,
                        .B = 0,
                        .A = 0
                    }

                    If inspectEmbedded AndAlso Not String.IsNullOrEmpty(p.Color) Then
                        ' the very same cached brush that the direct2d back end
                        ' uses, so the two back ends cannot drift apart
                        Dim brush As Brush = m_palette.GetEmbeddedBrush(p.Color)
                        Dim color As Color = If(brush Is Nothing, Color.Black, DirectCast(brush, SolidBrush).Color)
                        Dim alpha As Integer = color.A

                        ' an alpha of zero marks a point without an embedded
                        ' color, a pure black point keeps a minimal alpha so
                        ' that the shader can still tell the two apart
                        If alpha = 0 Then
                            alpha = 1
                        End If

                        instance.R = color.R
                        instance.G = color.G
                        instance.B = color.B
                        instance.A = CByte(alpha)

                        ' the hand drawn point sizes are carried in the scalar slot,
                        ' which the embedded color mode leaves unused; a factor of
                        ' one keeps every existing caller at the global point size
                        instance.Heat = CSng(If(p.SizeScale > 0, p.SizeScale, 1))
                    Else
                        ' a zero intensity falls back to the Z coordinate
                        Dim value As Double = If(p.Intensity <> 0, p.Intensity, p.Z)
                        Dim t As Double = (value - minIntensity) / range

                        If t < 0 Then
                            t = 0
                        ElseIf t > 1 Then
                            t = 1
                        End If

                        instance.Heat = CSng(t)

                        ' the heat slot is taken by the palette lookup here, so the
                        ' per point size travels in the normal slot instead
                        ' (the palette mode never reads it)
                        instance.NX = CSng(If(p.SizeScale > 0, p.SizeScale, 1))
                    End If

                    instances(i) = instance
                End Sub)

            m_instanceCount = count
            m_instanceBuffer = CreateImmutableBuffer(instances, D3D11_BIND_FLAG.VERTEX_BUFFER)
        End Sub

        ''' <summary>
        ''' build the point instances of a model: every corner of every face
        ''' becomes one point, the lambert factor of its face is the heat value
        ''' </summary>
        Private Sub BuildModelInstances(scene As Scene)
            Dim faces As Surface() = scene.Surfaces
            Dim n As Integer = If(faces Is Nothing, 0, faces.Length)

            If n = 0 Then
                Return
            End If

            Dim instances As New List(Of PointInstance)(n * 3)

            For i As Integer = 0 To n - 1
                Dim points As Point3D() = faces(i).vertices
                Dim normal As Vector3 = If(m_faceNormals IsNot Nothing AndAlso m_faceNormals.Length > i,
                                          m_faceNormals(i),
                                          FaceNormal(points))

                For k As Integer = 0 To points.Length - 1
                    Call instances.Add(New PointInstance With {
                        .X = CSng(points(k).X),
                        .Y = CSng(points(k).Y),
                        .Z = CSng(points(k).Z),
                        .NX = normal.X,
                        .NY = normal.Y,
                        .NZ = normal.Z,
                        .Heat = 0,
                        .R = 0,
                        .G = 0,
                        .B = 0,
                        .A = 0
                    })
                Next
            Next

            m_instanceCount = instances.Count
            m_instanceBuffer = CreateImmutableBuffer(instances.ToArray(), D3D11_BIND_FLAG.VERTEX_BUFFER)
        End Sub

        ''' <summary>
        ''' build the one row palette texture of the heat map scheme
        ''' </summary>
        Private Sub BuildPalette(options As SceneRenderOptions)
            Dim table As Color() = m_palette.GetTable(options.ColorScheme, options.PointAlpha)

            If table Is Nothing OrElse table.Length = 0 Then
                Return
            End If

            Dim levels As Integer = table.Length
            Dim pixels As Byte() = New Byte(levels * 4 - 1) {}

            For i As Integer = 0 To levels - 1
                Dim c As Color = table(i)

                pixels(i * 4 + 0) = c.R
                pixels(i * 4 + 1) = c.G
                pixels(i * 4 + 2) = c.B
                pixels(i * 4 + 3) = c.A
            Next

            Dim desc As New D3D11_TEXTURE2D_DESC With {
                .Width = CUInt(levels),
                .Height = 1,
                .MipLevels = 1,
                .ArraySize = 1,
                .Format = CInt(DXGI_FORMAT.R8G8B8A8_UNORM),
                .SampleDesc = New DXGI_SAMPLE_DESC With {.Count = 1, .Quality = 0},
                .Usage = CInt(D3D11_USAGE.IMMUTABLE),
                .BindFlags = CUInt(D3D11_BIND_FLAG.SHADER_RESOURCE),
                .CPUAccessFlags = 0,
                .MiscFlags = 0
            }
            Dim pinned As GCHandle = GCHandle.Alloc(pixels, GCHandleType.Pinned)
            Dim initial As New D3D11_SUBRESOURCE_DATA With {
                .pSysMem = pinned.AddrOfPinnedObject(),
                .SysMemPitch = CUInt(levels * 4),
                .SysMemSlicePitch = CUInt(levels * 4)
            }
            Dim pinnedInitial As GCHandle = GCHandle.Alloc(initial, GCHandleType.Pinned)
            Dim texture As IntPtr = IntPtr.Zero

            Try
                Call ThrowIfFailed(
                    m_device.Device.CreateTexture2D(desc, pinnedInitial.AddrOfPinnedObject(), texture),
                    "ID3D11Device::CreateTexture2D(palette)")

                Dim view As IntPtr = IntPtr.Zero

                Call ThrowIfFailed(
                    m_device.Device.CreateShaderResourceView(texture, IntPtr.Zero, view),
                    "ID3D11Device::CreateShaderResourceView")

                m_paletteTexture = texture
                m_paletteView = view
                m_paletteLevels = levels
            Finally
                pinned.Free()
                pinnedInitial.Free()
            End Try
        End Sub

        ''' <summary>
        ''' create an immutable gpu buffer that is initialized with the given
        ''' blittable array
        ''' </summary>
        Private Function CreateImmutableBuffer(Of T As Structure)(items As T(), bind As D3D11_BIND_FLAG) As IntPtr
            If items Is Nothing OrElse items.Length = 0 Then
                Return IntPtr.Zero
            End If

            Dim pinned As GCHandle = GCHandle.Alloc(items, GCHandleType.Pinned)

            Try
                Dim desc As New D3D11_BUFFER_DESC With {
                    .ByteWidth = CUInt(items.Length * Marshal.SizeOf(GetType(T))),
                    .Usage = CInt(D3D11_USAGE.IMMUTABLE),
                    .BindFlags = CUInt(bind),
                    .CPUAccessFlags = 0,
                    .MiscFlags = 0,
                    .StructureByteStride = 0
                }
                Dim pinnedDesc As GCHandle = GCHandle.Alloc(desc, GCHandleType.Pinned)
                Dim initial As New D3D11_SUBRESOURCE_DATA With {
                    .pSysMem = pinned.AddrOfPinnedObject(),
                    .SysMemPitch = 0,
                    .SysMemSlicePitch = 0
                }
                Dim pinnedInitial As GCHandle = GCHandle.Alloc(initial, GCHandleType.Pinned)
                Dim buffer As IntPtr = IntPtr.Zero

                Try
                    Call ThrowIfFailed(
                        m_device.Device.CreateBuffer(pinnedDesc.AddrOfPinnedObject(), pinnedInitial.AddrOfPinnedObject(), buffer),
                        "ID3D11Device::CreateBuffer")
                Finally
                    pinnedDesc.Free()
                    pinnedInitial.Free()
                End Try

                Return buffer
            Finally
                pinned.Free()
            End Try
        End Function

        Private Sub Dispose(disposing As Boolean)
            If m_disposed Then
                Return
            End If

            m_disposed = True

            Call ReleaseHandle(m_paletteView)
            Call ReleaseHandle(m_paletteTexture)
            Call ReleaseHandle(m_instanceBuffer)
            Call ReleaseHandle(m_quadBuffer)
            Call ReleaseHandle(m_lineBuffer)
            Call ReleaseHandle(m_groundBuffer)
            Call ReleaseHandle(m_surfaceBuffer)

            m_paletteView = IntPtr.Zero
            m_paletteTexture = IntPtr.Zero
            m_instanceBuffer = IntPtr.Zero
            m_quadBuffer = IntPtr.Zero
            m_lineBuffer = IntPtr.Zero
            m_groundBuffer = IntPtr.Zero
            m_surfaceBuffer = IntPtr.Zero
            m_instanceCount = 0
            m_surfaceVertexCount = 0
            m_groundVertexCount = 0
            m_lineVertexCount = 0
            m_faceNormals = Nothing
        End Sub

        Private Shared Sub ReleaseHandle(ByRef handle As IntPtr)
            If handle = IntPtr.Zero Then
                Return
            End If

            Call Marshal.Release(handle)
            handle = IntPtr.Zero
        End Sub

        Protected Overrides Sub Finalize()
            Call Dispose(False)
            MyBase.Finalize()
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Call Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub
    End Class
End Namespace
