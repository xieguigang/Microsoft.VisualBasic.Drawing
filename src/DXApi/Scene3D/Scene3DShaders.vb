Imports System.Numerics
Imports System.Runtime.InteropServices
Imports System.Text

Namespace Scene3D

    ''' <summary>
    ''' The per frame constants of the 3d pipeline, the layout of this structure
    ''' is the layout of the ``SceneConstants`` constant buffer of the hlsl code.
    ''' </summary>
    ''' <remarks>
    ''' The two matrices are stored already transposed (see
    ''' <see cref="SceneTransform"/>), so the shader can use the classic
    ''' ``mul(matrix, vector)`` form with the default column major packing.
    ''' The size of this structure is 208 bytes, a multiple of the 16 byte
    ''' alignment that a constant buffer requires.
    ''' </remarks>
    <StructLayout(LayoutKind.Sequential)>
    Friend Structure SceneConstants
        ''' <summary>``rotation * projection``, transposed</summary>
        Public WorldViewProjection As Matrix4x4
        ''' <summary>the rotation of the camera, transposed</summary>
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
        Friend Const EntryBlitVertex As String = "VS_Blit"
        Friend Const EntryBlitPixel As String = "PS_Blit"

        ''' <summary>
        ''' the shader source of the whole 3d pipeline
        ''' </summary>
        Friend ReadOnly Source As String = BuildSource()

        Private Function BuildSource() As String
            Dim hlsl As String =
                "cbuffer SceneConstants : register(b0)" & vbCrLf &
                "{" & vbCrLf &
                "    float4x4 worldViewProj;" & vbCrLf &
                "    float4x4 worldRotation;" & vbCrLf &
                "    float4   lightDirection;" & vbCrLf &
                "    float4   lightColor;" & vbCrLf &
                "    float4   shadingParams;" & vbCrLf &
                "    float4   viewportScale;" & vbCrLf &
                "    float4   unlitColor;" & vbCrLf &
                "    float4   heatParams;" & vbCrLf &
                "};" & vbCrLf &
                "" & vbCrLf &
                "Texture2D    sceneTexture : register(t0);" & vbCrLf &
                "SamplerState sceneSampler : register(s0);" & vbCrLf &
                "" & vbCrLf &
                "struct SurfaceInput" & vbCrLf &
                "{" & vbCrLf &
                "    float3 position : POSITION;" & vbCrLf &
                "    float3 normal   : NORMAL;" & vbCrLf &
                "    float4 color    : COLOR;" & vbCrLf &
                "};" & vbCrLf &
                "" & vbCrLf &
                "struct SurfaceOutput" & vbCrLf &
                "{" & vbCrLf &
                "    float4 position : SV_POSITION;" & vbCrLf &
                "    float3 normal   : TEXCOORD0;" & vbCrLf &
                "    float4 color    : COLOR;" & vbCrLf &
                "};" & vbCrLf &
                "" & vbCrLf &
                "struct PositionInput" & vbCrLf &
                "{" & vbCrLf &
                "    float3 position : POSITION;" & vbCrLf &
                "};" & vbCrLf &
                "" & vbCrLf &
                "struct PointInstanceInput" & vbCrLf &
                "{" & vbCrLf &
                "    float3 position : POSITION;" & vbCrLf &
                "    float3 normal   : TEXCOORD0;" & vbCrLf &
                "    float  heat     : TEXCOORD1;" & vbCrLf &
                "    float4 color    : COLOR;" & vbCrLf &
                "};" & vbCrLf &
                "" & vbCrLf &
                "struct PointQuadInput" & vbCrLf &
                "{" & vbCrLf &
                "    float2 corner : TEXCOORD2;" & vbCrLf &
                "};" & vbCrLf &
                "" & vbCrLf &
                "struct PointOutput" & vbCrLf &
                "{" & vbCrLf &
                "    float4 position : SV_POSITION;" & vbCrLf &
                "    float  heat     : TEXCOORD0;" & vbCrLf &
                "    float4 color    : COLOR;" & vbCrLf &
                "};" & vbCrLf &
                "" & vbCrLf &
                "SurfaceOutput VS_Surface(SurfaceInput input)" & vbCrLf &
                "{" & vbCrLf &
                "    SurfaceOutput output;" & vbCrLf &
                "    output.position = mul(worldViewProj, float4(input.position, 1));" & vbCrLf &
                "    output.normal = input.normal;" & vbCrLf &
                "    output.color = input.color;" & vbCrLf &
                "    return output;" & vbCrLf &
                "}" & vbCrLf &
                "" & vbCrLf &
                "float4 PS_Surface(SurfaceOutput input) : SV_TARGET" & vbCrLf &
                "{" & vbCrLf &
                "    float3 n = input.normal;" & vbCrLf &
                "" & vbCrLf &
                "    if (dot(n, n) < 1e-8f)" & vbCrLf &
                "    {" & vbCrLf &
                "        return input.color;" & vbCrLf &
                "    }" & vbCrLf &
                "" & vbCrLf &
                "    float3 lit = mul(worldRotation, float4(n, 0)).xyz;" & vbCrLf &
                "" & vbCrLf &
                "    if (lit.z < 0)" & vbCrLf &
                "    {" & vbCrLf &
                "        lit = -lit;" & vbCrLf &
                "    }" & vbCrLf &
                "" & vbCrLf &
                "    float diffuse = max(0, dot(normalize(lit), normalize(lightDirection.xyz)));" & vbCrLf &
                "    float ambient = lightColor.a;" & vbCrLf &
                "    float factor = ambient + (1 - ambient) * diffuse;" & vbCrLf &
                "" & vbCrLf &
                "    return float4(lerp(input.color.rgb, lightColor.rgb, factor), input.color.a);" & vbCrLf &
                "}" & vbCrLf &
                "" & vbCrLf &
                "float4 VS_Position(PositionInput input) : SV_POSITION" & vbCrLf &
                "{" & vbCrLf &
                "    return mul(worldViewProj, float4(input.position, 1));" & vbCrLf &
                "}" & vbCrLf &
                "" & vbCrLf &
                "float4 PS_Unlit(float4 position : SV_POSITION) : SV_TARGET" & vbCrLf &
                "{" & vbCrLf &
                "    return unlitColor;" & vbCrLf &
                "}" & vbCrLf &
                "" & vbCrLf &
                "PointOutput VS_Point(PointInstanceInput instance, PointQuadInput quad)" & vbCrLf &
                "{" & vbCrLf &
                "    PointOutput output;" & vbCrLf &
                "    float4 clip = mul(worldViewProj, float4(instance.position, 1));" & vbCrLf &
                "" & vbCrLf &
                "    if (clip.w <= 0.0001f)" & vbCrLf &
                "    {" & vbCrLf &
                "        output.position = float4(0, 0, -1, 1);" & vbCrLf &
                "        output.heat = 0;" & vbCrLf &
                "        output.color = instance.color;" & vbCrLf &
                "        return output;" & vbCrLf &
                "    }" & vbCrLf &
                "" & vbCrLf &
                "    float heat = instance.heat;" & vbCrLf &
                "" & vbCrLf &
                "    if (shadingParams.y > 0.5f)" & vbCrLf &
                "    {" & vbCrLf &
                "        float3 n = mul(worldRotation, float4(instance.normal, 0)).xyz;" & vbCrLf &
                "" & vbCrLf &
                "        if (n.z < 0)" & vbCrLf &
                "        {" & vbCrLf &
                "            n = -n;" & vbCrLf &
                "        }" & vbCrLf &
                "" & vbCrLf &
                "        heat = lightColor.a + (1 - lightColor.a) * max(0, dot(normalize(n), normalize(lightDirection.xyz)));" & vbCrLf &
                "    }" & vbCrLf &
                "" & vbCrLf &
                "    clip.xy += quad.corner * shadingParams.x * viewportScale.xy * clip.w;" & vbCrLf &
                "    output.position = clip;" & vbCrLf &
                "    output.heat = heat;" & vbCrLf &
                "    output.color = instance.color;" & vbCrLf &
                "    return output;" & vbCrLf &
                "}" & vbCrLf &
                "" & vbCrLf &
                "float4 PS_Point(PointOutput input) : SV_TARGET" & vbCrLf &
                "{" & vbCrLf &
                "    if (shadingParams.y <= 0.5f && shadingParams.w > 0.5f && input.color.a > 0.0f)" & vbCrLf &
                "    {" & vbCrLf &
                "        return input.color;" & vbCrLf &
                "    }" & vbCrLf &
                "" & vbCrLf &
                "    float heat = (input.heat - heatParams.x) * heatParams.y;" & vbCrLf &
                "    float levels = shadingParams.z;" & vbCrLf &
                "    float index = floor(saturate(heat) * (levels - 1) + 0.5f);" & vbCrLf &
                "    float u = (index + 0.5f) / levels;" & vbCrLf &
                "" & vbCrLf &
                "    return sceneTexture.Sample(sceneSampler, float2(u, 0.5f));" & vbCrLf &
                "}" & vbCrLf &
                "" & vbCrLf &
                "struct BlitOutput" & vbCrLf &
                "{" & vbCrLf &
                "    float4 position : SV_POSITION;" & vbCrLf &
                "    float2 uv       : TEXCOORD0;" & vbCrLf &
                "};" & vbCrLf &
                "" & vbCrLf &
                "BlitOutput VS_Blit(uint vertexId : SV_VertexID)" & vbCrLf &
                "{" & vbCrLf &
                "    BlitOutput output;" & vbCrLf &
                "    float2 corner = float2((vertexId << 1) & 2, vertexId & 2);" & vbCrLf &
                "" & vbCrLf &
                "    output.uv = corner;" & vbCrLf &
                "    output.position = float4(corner * float2(2, -2) + float2(-1, 1), 0, 1);" & vbCrLf &
                "    return output;" & vbCrLf &
                "}" & vbCrLf &
                "" & vbCrLf &
                "float4 PS_Blit(BlitOutput input) : SV_TARGET" & vbCrLf &
                "{" & vbCrLf &
                "    float4 color = sceneTexture.Sample(sceneSampler, input.uv);" & vbCrLf &
                "" & vbCrLf &
                "    return float4(color.rgb, 1.0f);" & vbCrLf &
                "}" & vbCrLf

            Return hlsl
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
