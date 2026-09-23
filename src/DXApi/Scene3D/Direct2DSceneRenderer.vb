Imports System.Drawing
Imports System.Linq
Imports System.Threading.Tasks
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Drawing3D
Imports Microsoft.VisualBasic.Imaging.Drawing3D.Math3D
Imports Brush = Microsoft.VisualBasic.Imaging.Brush
Imports Pen = Microsoft.VisualBasic.Imaging.Pen
Imports SolidBrush = Microsoft.VisualBasic.Imaging.SolidBrush
Imports std = System.Math
Imports Tpl = System.Threading.Tasks.Parallel

Namespace Scene3D

    ''' <summary>
    ''' The default scene render back end: it renders the 3d scene through the
    ''' shared ``IGraphics`` api, so it runs on the gpu accelerated direct2d
    ''' canvas of this library.
    ''' </summary>
    ''' <remarks>
    ''' The imaging of this back end is exactly the imaging of the well known
    ''' painter's algorithm renderer of the imaging framework:
    '''
    ''' 1. the faces are rotated by the camera
    ''' 2. the rotated faces are projected and shaded (lambert diffuse + ambient)
    ''' 3. the faces are sorted by their average depth, the farthest face first
    ''' 4. the sorted faces are filled in that order, so near faces overpaint the
    '''    far ones
    '''
    ''' The default back end therefore produces the same picture as the cpu based
    ''' viewer that this pipeline replaces, while the polygon submission itself
    ''' is executed by the gpu device.
    '''
    ''' Two differences to the original helper of the imaging framework are made
    ''' on purpose: the shading brushes are cached per color instead of allocating
    ''' a brush for every face of every frame, and the ground grid and the frame
    ''' are drawn through the same canvas object.
    ''' </remarks>
    Public Class Direct2DSceneRenderer
        Implements ISceneRenderBackend

        ''' <summary>
        ''' a shared default instance of this back end
        ''' </summary>
        Public Shared ReadOnly [Default] As New Direct2DSceneRenderer()

        ''' <summary>
        ''' the color of the wire frame edges of <see cref="SceneRenderMode.Mesh"/>
        ''' </summary>
        Public Shared ReadOnly MeshEdgeColor As Color = Color.FromArgb(200, 30, 30, 30)

        ''' <summary>
        ''' the number of the cached shading brushes
        ''' </summary>
        Public Const BrushCacheLimit As Integer = 8192

        ''' <summary>
        ''' the highest number of connection lines that this cpu based back end
        ''' projects per frame, a denser graph is subsampled
        ''' </summary>
        ''' <remarks>
        ''' every line of this back end costs one <c>DrawLine</c> call plus two point
        ''' projections, a connectome of a whole brain provides millions of them.
        ''' </remarks>
        Public Const MaxProjectedLines As Integer = 50000

        Private ReadOnly m_brushCache As New Dictionary(Of Color, Brush)()

        ''' <summary>
        ''' the heat map color palette cache of this back end
        ''' </summary>
        Public ReadOnly Property Palette As SceneColorPalette = New SceneColorPalette()

        Public ReadOnly Property Name As String Implements ISceneRenderBackend.Name
            Get
                Return "Direct2D"
            End Get
        End Property

        Public ReadOnly Property Description As String Implements ISceneRenderBackend.Description
            Get
                Return "Gpu accelerated painter's algorithm renderer on the shared IGraphics canvas (Direct2D)"
            End Get
        End Property

        Public Sub Render(canvas As IGraphics,
                          scene As Scene,
                          camera As Camera,
                          options As SceneRenderOptions) Implements ISceneRenderBackend.Render

            If canvas Is Nothing OrElse scene Is Nothing OrElse camera Is Nothing OrElse options Is Nothing Then
                Return
            End If

            camera.Screen = canvas.Size
            canvas.Clear(options.BackgroundColor)

            If Not scene.HasData Then
                Return
            End If

            If scene.SurfaceCount > 0 Then
                Call DrawGround(canvas, scene, camera, options)

                Select Case options.Mode
                    Case SceneRenderMode.Mesh
                        Call DrawMesh(canvas, scene, camera)
                    Case SceneRenderMode.PointCloud
                        ' even a solid model can be presented as a point cloud:
                        ' every vertex becomes a heat map colored point and the
                        ' color is driven by the light intensity of its face
                        Call DrawModelAsPointCloud(canvas, scene, camera, options)
                    Case Else
                        Call DrawSurfaces(canvas, scene, camera)
                End Select
            ElseIf scene.PointCount > 0 Then
                Call DrawGround(canvas, scene, camera, options)
                Call DrawPointCloud(canvas, scene, camera, options)
            End If

            ' the connection lines are an overlay of the scene, they are drawn
            ' after the primary geometry just like the gpu back end draws them
            If scene.LineCount > 0 AndAlso options.ShowConnections Then
                Call DrawConnections(canvas, scene, camera)
            End If
        End Sub

        ''' <summary>
        ''' drop all of the cached brushes and color tables, call this when the
        ''' scene has been replaced completely
        ''' </summary>
        Public Sub Clear()
            m_brushCache.Clear()
            Palette.Clear()
        End Sub

        ''' <summary>
        ''' the number of the cached shading brushes
        ''' </summary>
        Public ReadOnly Property BrushCacheCount As Integer
            Get
                Return m_brushCache.Count
            End Get
        End Property

        ''' <summary>
        ''' draw the square ground grid below the model, it is drawn before the
        ''' model so that the opaque faces occlude the grid lines behind them
        ''' </summary>
        Private Sub DrawGround(canvas As IGraphics,
                               scene As Scene,
                               camera As Camera,
                               options As SceneRenderOptions)

            If Not options.ShowGround Then
                Return
            End If

            Dim half As Double = scene.Radius * 2.0
            Const divisions As Integer = 20
            Dim stepv As Double = (2.0 * half) / divisions
            Dim z As Double = scene.GroundZ

            Using pen As New Pen(options.GroundColor, 1)
                For i As Integer = 0 To divisions
                    Dim t As Double = -half + i * stepv

                    Dim a As PointF = ToScreen(camera, New Point3D(-half, t, z))
                    Dim b As PointF = ToScreen(camera, New Point3D(half, t, z))
                    Call canvas.DrawLine(pen, a.X, a.Y, b.X, b.Y)

                    Dim c As PointF = ToScreen(camera, New Point3D(t, -half, z))
                    Dim d As PointF = ToScreen(camera, New Point3D(t, half, z))
                    Call canvas.DrawLine(pen, c.X, c.Y, d.X, d.Y)
                Next
            End Using
        End Sub

        ''' <summary>
        ''' filled faces with the painter's algorithm and the per face lambert
        ''' shading
        ''' </summary>
        Private Sub DrawSurfaces(canvas As IGraphics, scene As Scene, camera As Camera)
            Dim faces As Surface() = camera.Rotate(scene.Surfaces).ToArray()
            Dim n As Integer = faces.Length

            If n = 0 Then
                Return
            End If

            Dim screen As Size = camera.Screen
            Dim points(n - 1)() As PointF
            Dim colors(n - 1) As Color

            ' the faces are independent of each other, so the projection and the
            ' shading can run in parallel while the results are written back by
            ' index
            Tpl.For(0, n, Sub(i As Integer)
                              Dim face As Surface = faces(i)

                              points(i) = face.vertices _
                                  .Select(Function(p As Point3D) camera.Project(p).ToPointF(screen)) _
                                  .ToArray()

                              colors(i) = camera.Lighting(face)
                          End Sub)

            ' the painter's algorithm: the farthest face is drawn first
            Dim order As List(Of Integer) = PainterAlgorithm.OrderProvider(faces, AddressOf AverageZ)

            For Each index As Integer In order
                Call canvas.FillPolygon(GetShadingBrush(colors(index)), points(index))
            Next
        End Sub

        ''' <summary>
        ''' the triangle wire frame of the model, every face is only outlined
        ''' </summary>
        Private Sub DrawMesh(canvas As IGraphics, scene As Scene, camera As Camera)
            Dim faces As Surface() = camera.Rotate(scene.Surfaces).ToArray()
            Dim screen As Size = camera.Screen

            Using pen As New Pen(MeshEdgeColor, 1)
                For Each face As Surface In faces
                    Dim points As PointF() = face.vertices _
                        .Select(Function(p As Point3D) camera.Project(p).ToPointF(screen)) _
                        .ToArray()

                    If points.Length >= 2 Then
                        Call canvas.DrawPolygon(pen, points)
                    End If
                Next
            End Using
        End Sub

        ''' <summary>
        ''' present a solid model as a point cloud: every vertex is drawn as a
        ''' point and the heat map color is driven by the light intensity of the
        ''' face that the vertex belongs to
        ''' </summary>
        Private Sub DrawModelAsPointCloud(canvas As IGraphics,
                                          scene As Scene,
                                          camera As Camera,
                                          options As SceneRenderOptions)

            Dim faces As Surface() = scene.Surfaces
            Dim n As Integer = faces.Length

            If n = 0 Then
                Return
            End If

            Dim facePoints(n - 1)() As PointF
            Dim faceIntensity(n - 1) As Double

            ' the faces are independent of each other, so the projection and the
            ' intensity can run in parallel
            Tpl.For(0, n, Sub(i As Integer)
                              Dim face As Surface = faces(i)
                              Dim projected As Point3D() = camera.Project(camera.Rotate(face.vertices)).ToArray()
                              Dim buffer(projected.Length - 1) As PointF

                              For k As Integer = 0 To projected.Length - 1
                                  buffer(k) = New PointF(CSng(projected(k).X), CSng(projected(k).Y))
                              Next

                              facePoints(i) = buffer
                              faceIntensity(i) = FaceLightFactor(camera, face)
                          End Sub)

            Dim total As Integer = 0
            For i As Integer = 0 To n - 1
                total += facePoints(i).Length
            Next

            If total = 0 Then
                Return
            End If

            Dim points(total - 1) As PointF
            Dim intensities(total - 1) As Double
            Dim index As Integer = 0

            For i As Integer = 0 To n - 1
                For k As Integer = 0 To facePoints(i).Length - 1
                    points(index) = facePoints(i)(k)
                    intensities(index) = faceIntensity(i)
                    index += 1
                Next
            Next

            Dim minIntensity As Double = Double.MaxValue
            Dim maxIntensity As Double = Double.MinValue

            For i As Integer = 0 To total - 1
                If intensities(i) < minIntensity Then
                    minIntensity = intensities(i)
                End If
                If intensities(i) > maxIntensity Then
                    maxIntensity = intensities(i)
                End If
            Next

            Dim range As Double = maxIntensity - minIntensity

            If range < 1.0E-09 Then
                range = 1
            End If

            Dim brushes As Brush() = Palette.GetBrushes(options.ColorScheme, options.PointAlpha)
            Dim sz As Integer = std.Max(1, options.PointSize)
            Dim half As Single = sz / 2.0F

            For i As Integer = 0 To total - 1
                Dim t As Double = (intensities(i) - minIntensity) / range
                Dim brush As Brush = Palette.GetHeatBrush(t, options.ColorScheme, options.PointAlpha)

                Call canvas.FillRectangle(brush, points(i).X - half, points(i).Y - half, CSng(sz), CSng(sz))
            Next
        End Sub

        ''' <summary>
        ''' the point cloud of the scene, every point is drawn as a heat map
        ''' colored square
        ''' </summary>
        Private Sub DrawPointCloud(canvas As IGraphics,
                                   scene As Scene,
                                   camera As Camera,
                                   options As SceneRenderOptions)

            Dim cloud As PointCloudPoint() = scene.Points
            Dim count As Integer = cloud.Length

            If count = 0 Then
                Return
            End If

            Dim brushes As Brush() = Palette.GetBrushes(options.ColorScheme, options.PointAlpha)
            Dim colorCount As Integer = brushes.Length
            Dim sz As Integer = std.Max(1, options.PointSize)
            Dim half As Single = sz / 2.0F

            Dim xy(count - 1) As PointF
            Dim colorIndex(count - 1) As Integer

            Dim points(count - 1) As Point3D
            For i As Integer = 0 To count - 1
                points(i) = New Point3D(cloud(i).X, cloud(i).Y, cloud(i).Z)
            Next

            ' the rotation is done by the simd batch rotation of the camera
            Dim rotated As Point3D() = camera.Rotate(points)

            Dim viewDistance As Single = camera.ViewDistance
            Dim fov As Single = camera.FieldOfView
            Dim w2 As Single = CSng(camera.Screen.Width) / 2.0F
            Dim h2 As Single = CSng(camera.Screen.Height) / 2.0F
            Dim ox As Single = camera.Offset.X
            Dim oy As Single = camera.Offset.Y

            Dim minIntensity As Double = scene.IntensityMin
            Dim maxIntensity As Double = scene.IntensityMax
            Dim range As Double = maxIntensity - minIntensity

            If range = 0 Then
                range = 1
            End If

            ' the projection of the point cloud is a scalar loop that matches the
            ' formula of the camera, so it can run in parallel with the color
            ' lookup while the results are written back by index
            Tpl.For(0, count, Sub(i As Integer)
                                        Dim p As Point3D = rotated(i)
                                        Dim depth As Single = viewDistance + CSng(p.Z)
                                        Dim factor As Single = If(depth <= 0, 0, fov / depth)

                                        xy(i) = New PointF(
                                            CSng(p.X) * factor + w2 + ox,
                                            CSng(p.Y) * factor + h2 + oy)

                                        If options.UseEmbeddedColor AndAlso Not String.IsNullOrEmpty(cloud(i).Color) Then
                                            colorIndex(i) = -1
                                        Else
                                            ' a zero intensity falls back to the Z coordinate
                                            Dim value As Double = If(cloud(i).Intensity <> 0, cloud(i).Intensity, cloud(i).Z)
                                            Dim t As Double = (value - minIntensity) / range

                                            If t < 0 Then
                                                t = 0
                                            ElseIf t > 1 Then
                                                t = 1
                                            End If

                                            colorIndex(i) = CInt(t * (colorCount - 1))
                                        End If
                                    End Sub)

            For i As Integer = 0 To count - 1
                Dim brush As Brush

                If colorIndex(i) < 0 Then
                    brush = Palette.GetEmbeddedBrush(cloud(i).Color)

                    If brush Is Nothing Then
                        brush = brushes(0)
                    End If
                Else
                    brush = brushes(std.Max(0, std.Min(colorCount - 1, colorIndex(i))))
                End If

                Call canvas.FillRectangle(brush, xy(i).X - half, xy(i).Y - half, CSng(sz), CSng(sz))
            Next
        End Sub

        ''' <summary>
        ''' the average Z coordinate of the vertices of a face, this is the depth
        ''' key of the painter's algorithm
        ''' </summary>
        Private Shared Function AverageZ(face As Surface) As Double
            Dim vertices As Point3D() = face.vertices

            If vertices Is Nothing OrElse vertices.Length = 0 Then
                Return 0
            End If

            Dim sum As Double = 0

            For i As Integer = 0 To vertices.Length - 1
                sum += vertices(i).Z
            Next

            Return sum / vertices.Length
        End Function

        ''' <summary>
        ''' the light intensity factor of a face, this is the very same formula
        ''' that the shading of the faces uses: the face normal is oriented
        ''' towards the viewer and the diffuse term is the dot product with the
        ''' light direction
        ''' </summary>
        Private Shared Function FaceLightFactor(camera As Camera, face As Surface) As Double
            Dim vertices As Point3D() = face.vertices

            If vertices Is Nothing OrElse vertices.Length < 3 Then
                Return camera.AmbientStrength
            End If

            Dim a As Point3D = vertices(0)
            Dim b As Point3D = vertices(1)
            Dim c As Point3D = vertices(2)
            Dim normal As Point3D = (b - a).CrossProduct(c - a)
            Dim magnitude As Double = normal.Length()

            If magnitude = 0 Then
                Return camera.AmbientStrength
            End If

            normal = normal.Multiply(1 / magnitude)

            If normal.Z < 0 Then
                normal = normal.Multiply(-1)
            End If

            Dim diffuse As Double = std.Max(0, normal.DotProduct(camera.LightDirection))

            Return camera.AmbientStrength + (1 - camera.AmbientStrength) * diffuse
        End Function

        ''' <summary>
        ''' draw the connection lines of the scene
        ''' </summary>
        ''' <remarks>
        ''' This is the slow path by nature: one ``DrawLine`` call and two point
        ''' projections per connection, while the gpu back end draws the whole graph
        ''' with a single line list draw call. The lines are therefore subsampled
        ''' when there are more than <see cref="MaxProjectedLines"/> of them, so a
        ''' connectome of millions of connections cannot freeze the user interface
        ''' while the fallback is active (the shape of the graph stays readable, the
        ''' density is only reduced). The pens are cached per color: a graph colors
        ''' its connections by kind, so the number of distinct colors stays small.
        ''' </remarks>
        Private Sub DrawConnections(canvas As IGraphics, scene As Scene, camera As Camera)
            Dim lines As LineSegment() = scene.Lines
            Dim n As Integer = If(lines Is Nothing, 0, lines.Length)

            If n = 0 Then
                Return
            End If

            Dim stride As Integer = 1

            If n > MaxProjectedLines Then
                stride = CInt(std.Ceiling(n / CDbl(MaxProjectedLines)))
            End If

            Dim pens As New Dictionary(Of Color, Pen)()

            Try
                For i As Integer = 0 To n - 1 Step stride
                    Dim line As LineSegment = lines(i)
                    Dim color As Color = If(line.Color.A = 0, LineSegment.DefaultColor, line.Color)
                    Dim pen As Pen = Nothing

                    If Not pens.TryGetValue(color, pen) Then
                        pen = New Pen(color, 1)
                        pens(color) = pen
                    End If

                    Dim a As PointF = ToScreen(camera, line.A)
                    Dim b As PointF = ToScreen(camera, line.B)

                    Call canvas.DrawLine(pen, a.X, a.Y, b.X, b.Y)
                Next
            Finally
                For Each pen As Pen In pens.Values
                    pen.Dispose()
                Next
            End Try
        End Sub

        ''' <summary>
        ''' rotate and project a single point onto the screen coordinates
        ''' </summary>
        Private Shared Function ToScreen(camera As Camera, p As Point3D) As PointF
            Dim rotated As Point3D = camera.Rotate(p)
            Dim projected As Point3D = camera.Project(rotated)

            Return New PointF(CSng(projected.X), CSng(projected.Y))
        End Function

        ''' <summary>
        ''' get the cached shading brush of the given color
        ''' </summary>
        Private Function GetShadingBrush(color As Color) As Brush
            Dim brush As Brush = Nothing

            If m_brushCache.TryGetValue(color, brush) Then
                Return brush
            End If

            If m_brushCache.Count >= BrushCacheLimit Then
                m_brushCache.Clear()
            End If

            brush = New SolidBrush(color)
            m_brushCache(color) = brush

            Return brush
        End Function
    End Class
End Namespace
