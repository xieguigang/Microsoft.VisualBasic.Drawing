Imports System.IO
Imports System.Numerics
Imports System.Runtime.InteropServices
Imports System.Text

Namespace Scene3D

    ''' <summary>
    ''' The per frame constants of the 3d pipeline, the layout of this structure
    ''' is the layout of the ``SceneConstants`` constant buffer of the hlsl code.
    ''' </summary>
    ''' <remarks>
    ''' The two matrices are uploaded exactly as the managed ``System.Numerics``
    ''' row vector matrices are stored (the translation of a matrix lives in its
    ''' fourth row, like ``Vector4.Transform`` expects it). A constant buffer of
    ''' a ``float4x4`` is packed column major by default, which reads such a row
    ''' vector matrix correctly through the ``mul(matrix, vector)`` form of the
    ''' shader: the two conventions cancel out. Do not transpose the matrices on
    ''' the way in, that would only swap the w row with the depth row.
    ''' The size of this structure is 224 bytes, a multiple of the 16 byte
    ''' alignment that a constant buffer requires.
    ''' </remarks>
    <StructLayout(LayoutKind.Sequential)>
    Friend Structure SceneConstants
        ''' <summary>``rotation * projection``</summary>
        Public WorldViewProjection As Matrix4x4
        ''' <summary>the rotation of the camera</summary>
        Public WorldRotation As Matrix4x4
        ''' <summary>xyz = the unit vector that points towards the light source</summary>
        Public LightDirection As Vector4
        ''' <summary>rgb = the light color, a = the ambient strength</summary>
        Public LightColor As Vector4
        ''' <summary>x = the point size in pixels, y = the point mode, z = the palette levels, w = use the embedded point colors</summary>
        Public ShadingParams As Vector4
        ''' <summary>x = 2 / width, y = -2 / height (the screen y axis points downwards)</summary>
        Public ViewportScale As Vector4
        ''' <summary>the color of the wire frame lines and of the ground grid</summary>
        Public UnlitColor As Vector4
        ''' <summary>x = the lowest heat value, y = 1 / (the highest heat value - the lowest one)</summary>
        Public HeatParams As Vector4
    End Structure

    ''' <summary>
    ''' The hlsl source of the 3d pipeline and its run time compiler.
    ''' </summary>
    ''' <remarks>
    ''' The hlsl source itself is the ``Scene3DShaders.hlsl`` embedded resource of
    ''' this assembly, it is read back through <see cref="Source"/>.
    ''' The shaders are compiled on the fly by ``d3dcompiler_47.dll``, so this
    ''' library has no build time dependency on the windows sdk and a missing
    ''' compiler only makes the gpu back end unavailable: the caller catches the
    ''' exception of <see cref="Compile"/> and falls back to the polygon painter
    ''' of the direct2d back end.
    ''' </remarks>
    Friend Module Scene3DShaders

        ''' <summary>
        ''' the point mode of <c>ShadingParams.y</c>: the heat value comes from the instance data
        ''' </summary>
        Friend Const PointModeHeat As Single = 0
        ''' <summary>
        ''' the point mode of <c>ShadingParams.y</c>: the heat value is the lambert factor of the face normal
        ''' </summary>
        Friend Const PointModeLit As Single = 1

        ''' <summary>
        ''' the shader profile of the vertex shaders
        ''' </summary>
        Friend Const VertexProfile As String = "vs_4_0"
        ''' <summary>
        ''' the shader profile of the pixel shaders
        ''' </summary>
        Friend Const PixelProfile As String = "ps_4_0"

        Friend Const EntrySurfaceVertex As String = "VS_Surface"
        Friend Const EntrySurfacePixel As String = "PS_Surface"
        Friend Const EntryPositionVertex As String = "VS_Position"
        Friend Const EntryUnlitPixel As String = "PS_Unlit"
        Friend Const EntryPointVertex As String = "VS_Point"
        Friend Const EntryPointPixel As String = "PS_Point"
        ''' <summary>the vertex shader of the connection lines (position + per vertex color)</summary>
        Friend Const EntryLineVertex As String = "VS_Line"
        ''' <summary>the pixel shader of the connection lines (returns the interpolated vertex color)</summary>
        Friend Const EntryLinePixel As String = "PS_LineColor"
        Friend Const EntryBlitVertex As String = "VS_Blit"
        Friend Const EntryBlitPixel As String = "PS_Blit"

        ''' <summary>
        ''' the file name of the hlsl source that is embedded into this assembly
        ''' </summary>
        Friend Const SourceFile As String = "Scene3DShaders.hlsl"

        ''' <summary>
        ''' the shader source of the whole 3d pipeline
        ''' </summary>
        Friend ReadOnly Source As String = BuildSource()

        ''' <summary>
        ''' read the hlsl source of the 3d pipeline out of the resources of this
        ''' assembly
        ''' </summary>
        ''' <remarks>
        ''' The source lives in ``Scene3DShaders.hlsl`` and is packed as an
        ''' embedded resource by the project file, so that a hlsl editor can be
        ''' used while writing it and no build time tool chain is involved.
        ''' </remarks>
        ''' <exception cref="InvalidOperationException">
        ''' the ``Scene3DShaders.hlsl`` resource is missing, which means that the
        ''' build did not pack <see cref="SourceFile"/>
        ''' </exception>
        Private Function BuildSource() As String
            Dim assembly As Reflection.Assembly = GetType(Scene3DShaders).Assembly
            Dim resource As String = Nothing

            ' the logical name of an embedded resource is derived from the root
            ' namespace and the folder of the file, so it is looked up by its
            ' suffix instead of hard coding that name here
            For Each name As String In assembly.GetManifestResourceNames()
                If name.EndsWith(SourceFile, StringComparison.OrdinalIgnoreCase) Then
                    resource = name
                    Exit For
                End If
            Next

            If resource Is Nothing Then
                Throw New InvalidOperationException(
                    $"the embedded hlsl source '{SourceFile}' is missing from the assembly '{assembly.GetName().Name}'")
            End If

            Using stream As Stream = assembly.GetManifestResourceStream(resource)
                Using reader As New StreamReader(stream, Encoding.UTF8, True)
                    ' a byte order mark would reach the compiler as a stray token,
                    ' so a mark that survives the stream reader is dropped here
                    Return reader.ReadToEnd().TrimStart(ChrW(&HFEFF))
                End Using
            End Using
        End Function

        ''' <summary>
        ''' compile one shader entry point into byte code
        ''' </summary>
        ''' <param name="entryPoint">the name of the entry point function</param>
        ''' <param name="profile">the shader profile, <see cref="VertexProfile"/> or <see cref="PixelProfile"/></param>
        ''' <exception cref="InvalidOperationException">
        ''' the hlsl compiler is not available or the shader does not compile,
        ''' the message carries the compiler output
        ''' </exception>
        Friend Function Compile(entryPoint As String, profile As String) As Byte()
            Dim bytes As Byte() = Encoding.ASCII.GetBytes(Source)
            Dim pinned As GCHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned)
            Dim code As IntPtr = IntPtr.Zero
            Dim errors As IntPtr = IntPtr.Zero

            Try
                Dim flags As UInteger = CUInt(D3DCompiler.D3DCOMPILE_FLAG.ENABLE_STRICTNESS) Or
                                        CUInt(D3DCompiler.D3DCOMPILE_FLAG.OPTIMIZATION_LEVEL3)
                Dim hr As Integer

                Try
                    hr = D3DCompiler.D3DCompile(
                        pinned.AddrOfPinnedObject(),
                        New IntPtr(bytes.Length),
                        "Scene3D",
                        IntPtr.Zero,
                        IntPtr.Zero,
                        entryPoint,
                        profile,
                        flags,
                        0UI,
                        code,
                        errors
                    )
                Catch ex As DllNotFoundException
                    Throw New InvalidOperationException(
                        $"the hlsl compiler d3dcompiler_47.dll is not available on this system: {ex.Message}", ex)
                Catch ex As EntryPointNotFoundException
                    Throw New InvalidOperationException(
                        $"the hlsl compiler d3dcompiler_47.dll does not export D3DCompile: {ex.Message}", ex)
                End Try

                If hr < 0 Then
                    Throw New InvalidOperationException(
                        $"the shader {entryPoint} ({profile}) does not compile: {ReadBlob(errors)}")
                End If

                Return ReadBytes(code)
            Finally
                pinned.Free()

                ' both blobs own one reference that was handed out by the
                ' compiler, the blob is only read through its vtable here so
                ' that this simple one to one ownership stays true
                If errors <> IntPtr.Zero Then
                    Call Marshal.Release(errors)
                End If

                If code <> IntPtr.Zero Then
                    Call Marshal.Release(code)
                End If
            End Try
        End Function

        ''' <summary>
        ''' the slot of <c>ID3DBlob::GetBufferPointer</c>
        ''' </summary>
        Private Const BlobGetBufferPointer As Integer = 3
        ''' <summary>
        ''' the slot of <c>ID3DBlob::GetBufferSize</c>
        ''' </summary>
        Private Const BlobGetBufferSize As Integer = 4

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function GetBufferPointerFn(instance As IntPtr) As IntPtr

        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Private Delegate Function GetBufferSizeFn(instance As IntPtr) As UInteger

        ''' <summary>
        ''' copy the data block of a blob into the managed memory
        ''' </summary>
        Private Function ReadBytes(blob As IntPtr) As Byte()
            If blob = IntPtr.Zero Then
                Return New Byte() {}
            End If

            Dim sizeApi As GetBufferSizeFn = ResolveVtable(Of GetBufferSizeFn)(blob, BlobGetBufferSize)
            Dim bufferApi As GetBufferPointerFn = ResolveVtable(Of GetBufferPointerFn)(blob, BlobGetBufferPointer)
            Dim size As Integer = CInt(sizeApi(blob))

            If size <= 0 Then
                Return New Byte() {}
            End If

            Dim buffer As Byte() = New Byte(size - 1) {}

            Call Marshal.Copy(bufferApi(blob), buffer, 0, size)

            Return buffer
        End Function

        ''' <summary>
        ''' read the text of a compiler message blob
        ''' </summary>
        Private Function ReadBlob(blob As IntPtr) As String
            Try
                Dim message As Byte() = ReadBytes(blob)

                If message.Length = 0 Then
                    Return "(no compiler output)"
                End If

                Return Encoding.ASCII.GetString(message).Trim()
            Catch ex As Exception
                Return $"(unreadable compiler output: {ex.Message})"
            End Try
        End Function
    End Module

    ''' <summary>
    ''' The vertex layouts of the 3d pipeline and the strides that belong to
    ''' them.
    ''' </summary>
    ''' <remarks>
    ''' One face is stored as three corners that repeat the face normal and the
    ''' face color, so the flat shading of the cpu painter is reproduced by the
    ''' interpolator of the gpu without any additional work.
    ''' </remarks>
    Friend Module Scene3DInputLayout

        ''' <summary>the stride of one surface (and wire frame) vertex</summary>
        Friend Const SurfaceStride As UInteger = 28
        ''' <summary>the stride of one ground grid vertex</summary>
        Friend Const PositionStride As UInteger = 12
        ''' <summary>the stride of one point cloud instance</summary>
        Friend Const PointInstanceStride As UInteger = 32
        ''' <summary>the stride of one corner of the unit quad of a point</summary>
        Friend Const PointQuadStride As UInteger = 8
        ''' <summary>the stride of one end point of a connection line</summary>
        Friend Const LineStride As UInteger = 16

        ''' <summary>
        ''' one face corner: position, face normal and face color
        ''' </summary>
        Friend ReadOnly SurfaceElements As D3D11_INPUT_ELEMENT_DESC() = New D3D11_INPUT_ELEMENT_DESC() {
            Element("POSITION", 0, DXGI_FORMAT.R32G32B32_FLOAT, 0, 0, D3D11_INPUT_CLASSIFICATION.PER_VERTEX_DATA, 0),
            Element("NORMAL", 0, DXGI_FORMAT.R32G32B32_FLOAT, 0, 12, D3D11_INPUT_CLASSIFICATION.PER_VERTEX_DATA, 0),
            Element("COLOR", 0, DXGI_FORMAT.R8G8B8A8_UNORM, 0, 24, D3D11_INPUT_CLASSIFICATION.PER_VERTEX_DATA, 0)
        }

        ''' <summary>
        ''' the position only layout, it is shared by the wire frame pass and by
        ''' the ground grid
        ''' </summary>
        Friend ReadOnly PositionElements As D3D11_INPUT_ELEMENT_DESC() = New D3D11_INPUT_ELEMENT_DESC() {
            Element("POSITION", 0, DXGI_FORMAT.R32G32B32_FLOAT, 0, 0, D3D11_INPUT_CLASSIFICATION.PER_VERTEX_DATA, 0)
        }

        ''' <summary>
        ''' one point: the per instance centre and heat value of slot one, plus
        ''' the per vertex corner of the unit quad of slot zero
        ''' </summary>
        Friend ReadOnly PointElements As D3D11_INPUT_ELEMENT_DESC() = New D3D11_INPUT_ELEMENT_DESC() {
            Element("POSITION", 0, DXGI_FORMAT.R32G32B32_FLOAT, 1, 0, D3D11_INPUT_CLASSIFICATION.PER_INSTANCE_DATA, 1),
            Element("TEXCOORD", 0, DXGI_FORMAT.R32G32B32_FLOAT, 1, 12, D3D11_INPUT_CLASSIFICATION.PER_INSTANCE_DATA, 1),
            Element("TEXCOORD", 1, DXGI_FORMAT.R32_FLOAT, 1, 24, D3D11_INPUT_CLASSIFICATION.PER_INSTANCE_DATA, 1),
            Element("COLOR", 0, DXGI_FORMAT.R8G8B8A8_UNORM, 1, 28, D3D11_INPUT_CLASSIFICATION.PER_INSTANCE_DATA, 1),
            Element("TEXCOORD", 2, DXGI_FORMAT.R32G32_FLOAT, 0, 0, D3D11_INPUT_CLASSIFICATION.PER_VERTEX_DATA, 0)
        }

        ''' <summary>
        ''' one end point of a connection line: the position plus the color of the
        ''' line itself
        ''' </summary>
        ''' <remarks>
        ''' Both end points of one line repeat the same color, so the interpolator
        ''' of the gpu cannot introduce a color gradient along the line. The layout
        ''' is a line list (two vertices per line), the gpu draws one pixel wide
        ''' lines exactly like the direct2d back end does.
        ''' </remarks>
        Friend ReadOnly LineElements As D3D11_INPUT_ELEMENT_DESC() = New D3D11_INPUT_ELEMENT_DESC() {
            Element("POSITION", 0, DXGI_FORMAT.R32G32B32_FLOAT, 0, 0, D3D11_INPUT_CLASSIFICATION.PER_VERTEX_DATA, 0),
            Element("COLOR", 0, DXGI_FORMAT.R8G8B8A8_UNORM, 0, 12, D3D11_INPUT_CLASSIFICATION.PER_VERTEX_DATA, 0)
        }

        ''' <summary>
        ''' build one element of an input layout
        ''' </summary>
        ''' <remarks>
        ''' the semantic name is copied into an unmanaged ansi block because the
        ''' element structure stays blittable (see
        ''' <see cref="D3D11_INPUT_ELEMENT_DESC"/>). The names of the layout
        ''' tables above are allocated once for the whole process, which is the
        ''' very purpose of a static input layout table.
        ''' </remarks>
        Private Function Element(semantic As String, index As UInteger, format As DXGI_FORMAT, slot As UInteger,
                                 offset As UInteger, classification As D3D11_INPUT_CLASSIFICATION,
                                 stepRate As UInteger) As D3D11_INPUT_ELEMENT_DESC
            Return New D3D11_INPUT_ELEMENT_DESC With {
                .SemanticName = Marshal.StringToHGlobalAnsi(semantic),
                .SemanticIndex = index,
                .Format = CInt(format),
                .InputSlot = slot,
                .AlignedByteOffset = offset,
                .InputSlotClass = CInt(classification),
                .InstanceDataStepRate = stepRate
            }
        End Function
    End Module
End Namespace
