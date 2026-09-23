Imports System.Runtime.InteropServices

''' <summary>
''' The D3D11 (d3d11.dll) interop declarations.
''' </summary>
''' <remarks>
''' D3D11 is used here as the gpu device host: it creates the off-screen
''' texture which is shared with Direct2D through a dxgi surface, and it
''' also performs the pixel read back through a staging texture.
''' </remarks>
Friend Module D3D11

        Friend Const D3D11_SDK_VERSION As UInteger = 7

        Friend Enum D3D_DRIVER_TYPE As Integer
            UNKNOWN = 0
            HARDWARE = 1
            REFERENCE = 2
            NULL = 3
            SOFTWARE = 4
            WARP = 5
        End Enum

        <Flags>
        Friend Enum D3D11_CREATE_DEVICE_FLAG As UInteger
            NONE = 0
            SINGLETHREADED = &H1
            DEBUG = &H2
            SWITCH_TO_REF = &H4
            PREVENT_INTERNAL_THREADING_OPTIMIZATIONS = &H8
            ''' <summary>
            ''' required for the interop between d3d11 and direct2d
            ''' </summary>
            BGRA_SUPPORT = &H20
            DEBUGGABLE = &H40
        End Enum

        Friend Enum D3D_FEATURE_LEVEL As UInteger
            LEVEL_9_1 = &H9100
            LEVEL_9_2 = &H9200
            LEVEL_9_3 = &H9300
            LEVEL_10_0 = &HA000
            LEVEL_10_1 = &HA100
            LEVEL_11_0 = &HB000
            LEVEL_11_1 = &HB100
            LEVEL_12_0 = &HC000
            LEVEL_12_1 = &HC100
        End Enum

        Friend Enum D3D11_USAGE As Integer
            [DEFAULT] = 0
            IMMUTABLE = 1
            DYNAMIC = 2
            STAGING = 3
        End Enum

        <Flags>
        Friend Enum D3D11_BIND_FLAG As UInteger
            VERTEX_BUFFER = &H1
            INDEX_BUFFER = &H2
            CONSTANT_BUFFER = &H4
            SHADER_RESOURCE = &H8
            STREAM_OUTPUT = &H10
            RENDER_TARGET = &H20
            DEPTH_STENCIL = &H40
            UNORDERED_ACCESS = &H80
        End Enum

        <Flags>
        Friend Enum D3D11_CPU_ACCESS_FLAG As UInteger
            NONE = 0
            WRITE = &H10000
            READ = &H20000
        End Enum

        Friend Enum D3D11_MAP As Integer
            READ = 1
            WRITE = 2
            READ_WRITE = 3
            WRITE_DISCARD = 4
            WRITE_NO_OVERWRITE = 5
        End Enum

        Friend Enum D3D11_PRIMITIVE_TOPOLOGY As Integer
            UNDEFINED = 0
            POINTLIST = 1
            LINELIST = 2
            LINESTRIP = 3
            TRIANGLELIST = 4
            TRIANGLESTRIP = 5
        End Enum

        Friend Enum D3D11_FILL_MODE As Integer
            WIREFRAME = 2
            SOLID = 3
        End Enum

        Friend Enum D3D11_CULL_MODE As Integer
            NONE = 1
            [FRONT] = 2
            BACK = 3
        End Enum

        Friend Enum D3D11_COMPARISON_FUNC As Integer
            NEVER = 1
            LESS = 2
            EQUAL = 3
            LESS_EQUAL = 4
            GREATER = 5
            NOT_EQUAL = 6
            GREATER_EQUAL = 7
            ALWAYS = 8
        End Enum

        Friend Enum D3D11_DEPTH_WRITE_MASK As Integer
            ZERO = 0
            ALL = 1
        End Enum

        Friend Enum D3D11_STENCIL_OP As Integer
            KEEP = 1
            ZERO = 2
            REPLACE = 3
            INCR_SAT = 4
            DECR_SAT = 5
            INVERT = 6
            INCR = 7
            DECR = 8
        End Enum

        Friend Enum D3D11_BLEND As Integer
            ZERO = 1
            ONE = 2
            SRC_COLOR = 3
            INV_SRC_COLOR = 4
            SRC_ALPHA = 5
            INV_SRC_ALPHA = 6
            DEST_ALPHA = 7
            INV_DEST_ALPHA = 8
            DEST_COLOR = 9
            INV_DEST_COLOR = 10
            SRC_ALPHA_SAT = 11
            BLEND_FACTOR = 14
            INV_BLEND_FACTOR = 15
        End Enum

        Friend Enum D3D11_BLEND_OP As Integer
            ADD = 1
            SUBTRACT = 2
            REV_SUBTRACT = 3
            MIN = 4
            MAX = 5
        End Enum

        <Flags>
        Friend Enum D3D11_COLOR_WRITE_ENABLE As Integer
            RED = &H1
            GREEN = &H2
            BLUE = &H4
            ALPHA = &H8
            ALL = &HF
        End Enum

        <Flags>
        Friend Enum D3D11_RESOURCE_MISC_FLAG As UInteger
            GENERATE_MIPS = &H1
            ''' <summary>the visual basic ``Shared`` keyword forces the brackets</summary>
            [SHARED] = &H2
            TEXTURECUBE = &H4
            DRAWINDIRECT_ARGS = &H10
            BUFFER_ALLOW_RAW_VIEWS = &H20
            BUFFER_STRUCTURED = &H40
            RESOURCE_CLAMP = &H80
        End Enum

        <Flags>
        Friend Enum D3D11_CLEAR_FLAG As UInteger
            DEPTH = &H1
            STENCIL = &H2
        End Enum

        Friend Enum D3D11_INPUT_CLASSIFICATION As Integer
            PER_VERTEX_DATA = 0
            PER_INSTANCE_DATA = 1
        End Enum

        Friend Enum D3D11_FILTER As Integer
            MIN_MAG_MIP_POINT = &H0
            MIN_MAG_POINT_MIP_LINEAR = &H1
            MIN_POINT_MAG_LINEAR_MIP_POINT = &H4
            MIN_POINT_MAG_MIP_LINEAR = &H5
            MIN_LINEAR_MAG_MIP_POINT = &H10
            MIN_LINEAR_MAG_POINT_MIP_LINEAR = &H11
            MIN_MAG_LINEAR_MIP_POINT = &H14
            MIN_MAG_MIP_LINEAR = &H15
            ANISOTROPIC = &H55
        End Enum

        Friend Enum D3D11_TEXTURE_ADDRESS_MODE As Integer
            WRAP = 1
            MIRROR = 2
            CLAMP = 3
            BORDER = 4
            MIRROR_ONCE = 5
        End Enum

        ''' <summary>
        ''' the quality level that asks the driver to use the standard multi
        ''' sample pattern, accepted by the ``Quality`` field of
        ''' <c>CreateTexture2D</c> when the sample count is greater than one.
        ''' </summary>
        Friend Const D3D11_STANDARD_MULTISAMPLE_PATTERN As UInteger = &HFFFFFFFFUI

        ''' <summary>
        ''' create the d3d11 device and its immediate device context
        ''' </summary>
        <DllImport("d3d11.dll", EntryPoint:="D3D11CreateDevice", PreserveSig:=True)>
        Friend Function D3D11CreateDevice(
            adapter As IntPtr,
            driverType As D3D_DRIVER_TYPE,
            software As IntPtr,
            flags As UInteger,
            <[In]> featureLevels As UInteger(),
            featureLevelsCount As UInteger,
            sdkVersion As UInteger,
            <Out> ByRef device As ID3D11Device,
            <Out> ByRef featureLevel As UInteger,
            <Out> ByRef immediateContext As ID3D11DeviceContext
        ) As Integer
        End Function

    End Module

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure D3D11_TEXTURE2D_DESC
        Public Width As UInteger
        Public Height As UInteger
        Public MipLevels As UInteger
        Public ArraySize As UInteger
        Public Format As Integer
        Public SampleDesc As DXGI_SAMPLE_DESC
        Public Usage As Integer
        Public BindFlags As UInteger
        Public CPUAccessFlags As UInteger
        Public MiscFlags As UInteger
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure D3D11_MAPPED_SUBRESOURCE
        Public pData As IntPtr
        Public RowPitch As UInteger
        Public DepthPitch As UInteger
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure D3D11_BOX
        Public left As UInteger
        Public top As UInteger
        Public front As UInteger
        Public right As UInteger
        Public bottom As UInteger
        Public back As UInteger
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure D3D11_BUFFER_DESC
        Public ByteWidth As UInteger
        Public Usage As Integer
        Public BindFlags As UInteger
        Public CPUAccessFlags As UInteger
        Public MiscFlags As UInteger
        Public StructureByteStride As UInteger
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure D3D11_SUBRESOURCE_DATA
        Public pSysMem As IntPtr
        Public SysMemPitch As UInteger
        Public SysMemSlicePitch As UInteger
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure D3D11_VIEWPORT
        Public TopLeftX As Single
        Public TopLeftY As Single
        Public Width As Single
        Public Height As Single
        Public MinDepth As Single
        Public MaxDepth As Single
    End Structure

    ''' <summary>
    ''' one element of the input layout of a vertex shader
    ''' </summary>
    <StructLayout(LayoutKind.Sequential)>
    Friend Structure D3D11_INPUT_ELEMENT_DESC
        <MarshalAs(UnmanagedType.LPStr)> Public SemanticName As String
        Public SemanticIndex As UInteger
        ''' <summary>a DXGI_FORMAT value</summary>
        Public Format As Integer
        Public InputSlot As UInteger
        Public AlignedByteOffset As UInteger
        ''' <summary>a D3D11_INPUT_CLASSIFICATION value</summary>
        Public InputSlotClass As Integer
        Public InstanceDataStepRate As UInteger
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure D3D11_DEPTH_STENCILOP_DESC
        Public StencilFailOp As Integer
        Public StencilDepthFailOp As Integer
        Public StencilPassOp As Integer
        Public StencilFunc As Integer
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure D3D11_DEPTH_STENCIL_DESC
        Public DepthEnable As Integer
        Public DepthWriteMask As Integer
        Public DepthFunc As Integer
        Public StencilEnable As Integer
        Public StencilReadMask As Byte
        Public StencilWriteMask As Byte
        Public FrontFace As D3D11_DEPTH_STENCILOP_DESC
        Public BackFace As D3D11_DEPTH_STENCILOP_DESC
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure D3D11_RASTERIZER_DESC
        Public FillMode As Integer
        Public CullMode As Integer
        Public FrontCounterClockwise As Integer
        Public DepthBias As Integer
        Public DepthBiasClamp As Single
        Public SlopeScaledDepthBias As Single
        Public DepthClipEnable As Integer
        Public ScissorEnable As Integer
        Public MultisampleEnable As Integer
        Public AntialiasedLineEnable As Integer
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure D3D11_RENDER_TARGET_BLEND_DESC
        Public BlendEnable As Integer
        Public SrcBlend As Integer
        Public DestBlend As Integer
        Public BlendOp As Integer
        Public SrcBlendAlpha As Integer
        Public DestBlendAlpha As Integer
        Public BlendOpAlpha As Integer
        Public RenderTargetWriteMask As Byte
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure D3D11_BLEND_DESC
        Public AlphaToCoverageEnable As Integer
        Public IndependentBlendEnable As Integer
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=8)>
        Public RenderTarget As D3D11_RENDER_TARGET_BLEND_DESC()
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure D3D11_SAMPLER_DESC
        Public Filter As Integer
        Public AddressU As Integer
        Public AddressV As Integer
        Public AddressW As Integer
        Public MipLODBias As Single
        Public MaxAnisotropy As UInteger
        Public ComparisonFunc As Integer
        Public BorderColor0 As Single
        Public BorderColor1 As Single
        Public BorderColor2 As Single
        Public BorderColor3 As Single
        Public MinLOD As Single
        Public MaxLOD As Single
    End Structure

    ' /********************************************************************************/
    '  The following vtable slots are declared in the exact order of the native
    '  d3d11.h header file. Only the methods that are actually used by this project
    '  carry a real signature, the remaining slots are declared with a compact
    '  signature just for keeping the vtable layout aligned.
    ' /********************************************************************************/

    <ComImport>
    <Guid("1841e5c8-16b0-489b-bcc8-44cfb0d5deae")>
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Friend Interface ID3D11DeviceChild
        <PreserveSig> Function GetDevice(<Out> ByRef device As IntPtr) As Integer
        <PreserveSig> Function GetPrivateData(ByRef guid As Guid, ByRef size As UInteger, data As IntPtr) As Integer
        <PreserveSig> Function SetPrivateData(ByRef guid As Guid, size As UInteger, data As IntPtr) As Integer
        <PreserveSig> Function SetPrivateDataInterface(ByRef guid As Guid, data As IntPtr) As Integer
    End Interface

    <ComImport>
    <Guid("dc8e63f3-d12b-4952-b47b-5e45026a862d")>
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Friend Interface ID3D11Resource

        ' slot 3 .. slot 6, ID3D11DeviceChild
        <PreserveSig> Function GetDevice(<Out> ByRef device As IntPtr) As Integer
        <PreserveSig> Function GetPrivateData(ByRef guid As Guid, ByRef size As UInteger, data As IntPtr) As Integer
        <PreserveSig> Function SetPrivateData(ByRef guid As Guid, size As UInteger, data As IntPtr) As Integer
        <PreserveSig> Function SetPrivateDataInterface(ByRef guid As Guid, data As IntPtr) As Integer
        ' slot 7
        <PreserveSig> Sub GetResourceDimension(dimension As IntPtr)
        ' slot 8
        <PreserveSig> Sub SetEvictionPriority(priority As UInteger)
        ' slot 9
        <PreserveSig> Function GetEvictionPriority() As UInteger
    End Interface

    ''' <summary>
    ''' ID3D11Texture2D, no method is required by this project
    ''' </summary>
    <ComImport>
    <Guid("6f15aaf2-d208-4e89-9ab4-489535d34f9c")>
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Friend Interface ID3D11Texture2D

        ' slot 3 .. slot 6, ID3D11DeviceChild
        <PreserveSig> Function GetDevice(<Out> ByRef device As IntPtr) As Integer
        <PreserveSig> Function GetPrivateData(ByRef guid As Guid, ByRef size As UInteger, data As IntPtr) As Integer
        <PreserveSig> Function SetPrivateData(ByRef guid As Guid, size As UInteger, data As IntPtr) As Integer
        <PreserveSig> Function SetPrivateDataInterface(ByRef guid As Guid, data As IntPtr) As Integer
        ' slot 7 .. slot 9, ID3D11Resource
        <PreserveSig> Sub GetResourceDimension(dimension As IntPtr)
        <PreserveSig> Sub SetEvictionPriority(priority As UInteger)
        <PreserveSig> Function GetEvictionPriority() As UInteger
        ' slot 10
        <PreserveSig> Sub GetDesc(<Out> ByRef desc As D3D11_TEXTURE2D_DESC)
    End Interface

    ''' <summary>
    ''' ID3D11Device, the resource factory of the gpu device.
    ''' </summary>
    ''' <remarks>
    ''' The slots are declared in the exact order of the native d3d11.h header
    ''' file: the methods that are not used by this project keep a compact
    ''' signature, but no slot may be skipped or the whole vtable of this
    ''' interface silently shifts (vb.net com interfaces are dispatched by the
    ''' declaration order, not by a name).
    '''
    ''' The native resources that are created by this factory are kept as raw
    ''' interface pointers: such a pointer is only handed back to the device
    ''' context or to another factory call, so the raw form avoids both the
    ''' extra query interface of the com marshaler and a wrong interface cast
    ''' at the call site.
    ''' </remarks>
    <ComImport>
    <Guid("db6f6ddb-ac77-4e88-8253-819df9bbf140")>
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Friend Interface ID3D11Device
        ' slot 3
        <PreserveSig> Function CreateBuffer(desc As IntPtr, initialData As IntPtr, <Out> ByRef buffer As IntPtr) As Integer
        ' slot 4
        <PreserveSig> Function CreateTexture1D(desc As IntPtr, initialData As IntPtr, <Out> ByRef texture As IntPtr) As Integer
        ' slot 5
        <PreserveSig> Function CreateTexture2D(ByRef desc As D3D11_TEXTURE2D_DESC, initialData As IntPtr, <Out> ByRef texture As IntPtr) As Integer
        ' slot 6
        <PreserveSig> Function CreateTexture3D(desc As IntPtr, initialData As IntPtr, <Out> ByRef texture As IntPtr) As Integer
        ' slot 7
        <PreserveSig> Function CreateShaderResourceView(resource As IntPtr, desc As IntPtr, <Out> ByRef view As IntPtr) As Integer
        ' slot 8
        <PreserveSig> Function CreateUnorderedAccessView(resource As IntPtr, desc As IntPtr, <Out> ByRef view As IntPtr) As Integer
        ' slot 9
        <PreserveSig> Function CreateRenderTargetView(resource As IntPtr, desc As IntPtr, <Out> ByRef view As IntPtr) As Integer
        ' slot 10
        <PreserveSig> Function CreateDepthStencilView(resource As IntPtr, desc As IntPtr, <Out> ByRef view As IntPtr) As Integer
        ' slot 11
        <PreserveSig> Function CreateInputLayout(<[In]> elements As D3D11_INPUT_ELEMENT_DESC(), numElements As UInteger,
                                                shaderBytecode As IntPtr, bytecodeLength As UInteger,
                                                <Out> ByRef layout As IntPtr) As Integer
        ' slot 12
        <PreserveSig> Function CreateVertexShader(shaderBytecode As IntPtr, bytecodeLength As UInteger,
                                                  classLinkage As IntPtr, <Out> ByRef shader As IntPtr) As Integer
        ' slot 13
        <PreserveSig> Function CreateGeometryShader(shaderBytecode As IntPtr, bytecodeLength As UInteger,
                                                    classLinkage As IntPtr, <Out> ByRef shader As IntPtr) As Integer
        ' slot 14
        <PreserveSig> Function CreateGeometryShaderWithStreamOutput(shaderBytecode As IntPtr, bytecodeLength As UInteger,
                                                                    outputSignature As IntPtr, numEntries As UInteger,
                                                                    bufferStrides As IntPtr, numStrides As UInteger,
                                                                    rasterizedStream As UInteger, classLinkage As IntPtr,
                                                                    <Out> ByRef shader As IntPtr) As Integer
        ' slot 15
        <PreserveSig> Function CreatePixelShader(shaderBytecode As IntPtr, bytecodeLength As UInteger,
                                                 classLinkage As IntPtr, <Out> ByRef shader As IntPtr) As Integer
        ' slot 16
        <PreserveSig> Function CreateHullShader(shaderBytecode As IntPtr, bytecodeLength As UInteger,
                                                classLinkage As IntPtr, <Out> ByRef shader As IntPtr) As Integer
        ' slot 17
        <PreserveSig> Function CreateDomainShader(shaderBytecode As IntPtr, bytecodeLength As UInteger,
                                                  classLinkage As IntPtr, <Out> ByRef shader As IntPtr) As Integer
        ' slot 18
        <PreserveSig> Function CreateComputeShader(shaderBytecode As IntPtr, bytecodeLength As UInteger,
                                                   classLinkage As IntPtr, <Out> ByRef shader As IntPtr) As Integer
        ' slot 19
        <PreserveSig> Function CreateClassLinkage(<Out> ByRef linkage As IntPtr) As Integer
        ' slot 20
        <PreserveSig> Function CreateBlendState(ByRef desc As D3D11_BLEND_DESC, <Out> ByRef state As IntPtr) As Integer
        ' slot 21
        <PreserveSig> Function CreateDepthStencilState(ByRef desc As D3D11_DEPTH_STENCIL_DESC, <Out> ByRef state As IntPtr) As Integer
        ' slot 22
        <PreserveSig> Function CreateRasterizerState(ByRef desc As D3D11_RASTERIZER_DESC, <Out> ByRef state As IntPtr) As Integer
        ' slot 23
        <PreserveSig> Function CreateSamplerState(ByRef desc As D3D11_SAMPLER_DESC, <Out> ByRef state As IntPtr) As Integer
        ' slot 24
        <PreserveSig> Function CreateQuery(desc As IntPtr, <Out> ByRef query As IntPtr) As Integer
        ' slot 25
        <PreserveSig> Function CreatePredicate(desc As IntPtr, <Out> ByRef predicate As IntPtr) As Integer
        ' slot 26
        <PreserveSig> Function CreateCounter(desc As IntPtr, <Out> ByRef counter As IntPtr) As Integer
        ' slot 27
        <PreserveSig> Function CreateDeferredContext(flags As UInteger, <Out> ByRef context As IntPtr) As Integer
        ' slot 28
        <PreserveSig> Function OpenSharedResource(hResource As IntPtr, ByRef riid As Guid, <Out> ByRef resource As IntPtr) As Integer
        ' slot 29
        <PreserveSig> Function CheckFormatSupport(format As Integer, <Out> ByRef support As UInteger) As Integer
        ' slot 30
        <PreserveSig> Function CheckMultisampleQualityLevels(format As Integer, sampleCount As UInteger,
                                                             <Out> ByRef qualityLevels As UInteger) As Integer
    End Interface

    ''' <summary>
    ''' ID3D11DeviceContext, the pixel read back is implemented via
    ''' CopyResource (vtable slot 47) + Map/Unmap (vtable slot 14/15)
    ''' </summary>
    <ComImport>
    <Guid("c0bfa96c-e089-44fb-8eaf-26f8796190da")>
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Friend Interface ID3D11DeviceContext

        ' slot 3 .. slot 6, ID3D11DeviceChild
        <PreserveSig> Function GetDevice(<Out> ByRef device As IntPtr) As Integer
        <PreserveSig> Function GetPrivateData(ByRef guid As Guid, ByRef size As UInteger, data As IntPtr) As Integer
        <PreserveSig> Function SetPrivateData(ByRef guid As Guid, size As UInteger, data As IntPtr) As Integer
        <PreserveSig> Function SetPrivateDataInterface(ByRef guid As Guid, data As IntPtr) As Integer
        ' slot 7
        <PreserveSig> Sub VSSetConstantBuffers(startSlot As UInteger, numBuffers As UInteger, buffers As IntPtr)
        ' slot 8
        <PreserveSig> Sub PSSetShaderResources(startSlot As UInteger, numViews As UInteger, views As IntPtr)
        ' slot 9
        <PreserveSig> Sub PSSetShader(shader As IntPtr, classInstances As IntPtr, numClassInstances As UInteger)
        ' slot 10
        <PreserveSig> Sub PSSetSamplers(startSlot As UInteger, numSamplers As UInteger, samplers As IntPtr)
        ' slot 11
        <PreserveSig> Sub VSSetShader(shader As IntPtr, classInstances As IntPtr, numClassInstances As UInteger)
        ' slot 12
        <PreserveSig> Sub DrawIndexed(indexCount As UInteger, startIndexLocation As UInteger, baseVertexLocation As Integer)
        ' slot 13
        <PreserveSig> Sub Draw(vertexCount As UInteger, startVertexLocation As UInteger)

        ' slot 14
        <PreserveSig> Function Map(resource As ID3D11Texture2D, subresource As UInteger, mapType As UInteger, mapFlags As UInteger, ByRef mapped As D3D11_MAPPED_SUBRESOURCE) As Integer
        ' slot 15
        <PreserveSig> Sub Unmap(resource As ID3D11Texture2D, subresource As UInteger)

        ' slot 16
        <PreserveSig> Sub PSSetConstantBuffers(startSlot As UInteger, numBuffers As UInteger, buffers As IntPtr)
        ' slot 17
        <PreserveSig> Sub IASetInputLayout(layout As IntPtr)
        ' slot 18
        <PreserveSig> Sub IASetVertexBuffers(startSlot As UInteger, numBuffers As UInteger, buffers As IntPtr, strides As IntPtr, offsets As IntPtr)
        ' slot 19
        <PreserveSig> Sub IASetIndexBuffer(buffer As IntPtr, format As Integer, offset As UInteger)
        ' slot 20
        <PreserveSig> Sub DrawIndexedInstanced(indexCountPerInstance As UInteger, instanceCount As UInteger, startIndexLocation As UInteger, baseVertexLocation As Integer, startInstanceLocation As UInteger)
        ' slot 21
        <PreserveSig> Sub DrawInstanced(vertexCountPerInstance As UInteger, instanceCount As UInteger, startVertexLocation As UInteger, startInstanceLocation As UInteger)
        ' slot 22
        <PreserveSig> Sub GSSetConstantBuffers(startSlot As UInteger, numBuffers As UInteger, buffers As IntPtr)
        ' slot 23
        <PreserveSig> Sub GSSetShader(shader As IntPtr, classInstances As IntPtr, numClassInstances As UInteger)
        ' slot 24
        <PreserveSig> Sub IASetPrimitiveTopology(topology As Integer)
        ' slot 25
        <PreserveSig> Sub VSSetShaderResources(startSlot As UInteger, numViews As UInteger, views As IntPtr)
        ' slot 26
        <PreserveSig> Sub VSSetSamplers(startSlot As UInteger, numSamplers As UInteger, samplers As IntPtr)
        ' slot 27
        <PreserveSig> Sub Begin(async As IntPtr)
        ' slot 28
        <PreserveSig> Sub [End](async As IntPtr)
        ' slot 29
        <PreserveSig> Function GetData(async As IntPtr, data As IntPtr, dataSize As UInteger, flags As UInteger) As Integer
        ' slot 30
        <PreserveSig> Sub SetPredication(predicate As IntPtr, predicateValue As Integer)
        ' slot 31
        <PreserveSig> Sub GSSetShaderResources(startSlot As UInteger, numViews As UInteger, views As IntPtr)
        ' slot 32
        <PreserveSig> Sub GSSetSamplers(startSlot As UInteger, numSamplers As UInteger, samplers As IntPtr)
        ' slot 33
        <PreserveSig> Sub OMSetRenderTargets(numViews As UInteger, renderTargetViews As IntPtr, depthStencilView As IntPtr)
        ' slot 34
        <PreserveSig> Sub OMSetRenderTargetsAndUnorderedAccessViews(numRTV As UInteger, rtv As IntPtr, dsv As IntPtr, uavStartSlot As UInteger, numUAVs As UInteger, uav As IntPtr, uavInitialCounts As IntPtr)
        ' slot 35
        <PreserveSig> Sub OMSetBlendState(state As IntPtr, blendFactor As IntPtr, sampleMask As UInteger)
        ' slot 36
        <PreserveSig> Sub OMSetDepthStencilState(state As IntPtr, stencilRef As UInteger)
        ' slot 37
        <PreserveSig> Sub SOSetTargets(numBuffers As UInteger, buffers As IntPtr, offsets As IntPtr)
        ' slot 38
        <PreserveSig> Sub DrawAuto()
        ' slot 39
        <PreserveSig> Sub DrawIndexedInstancedIndirect(buffer As IntPtr, alignedByteOffsetForArgs As UInteger)
        ' slot 40
        <PreserveSig> Sub DrawInstancedIndirect(buffer As IntPtr, alignedByteOffsetForArgs As UInteger)
        ' slot 41
        <PreserveSig> Sub Dispatch(x As UInteger, y As UInteger, z As UInteger)
        ' slot 42
        <PreserveSig> Sub DispatchIndirect(buffer As IntPtr, alignedByteOffsetForArgs As UInteger)
        ' slot 43
        <PreserveSig> Sub RSSetState(state As IntPtr)
        ' slot 44
        <PreserveSig> Sub RSSetViewports(numViewports As UInteger, viewports As IntPtr)
        ' slot 45
        <PreserveSig> Sub RSSetScissorRects(numRects As UInteger, rects As IntPtr)

        ' slot 46
        <PreserveSig> Sub CopySubresourceRegion(dst As ID3D11Texture2D, dstSubresource As UInteger, dstX As UInteger, dstY As UInteger, dstZ As UInteger, src As ID3D11Texture2D, srcSubresource As UInteger, ByRef box As D3D11_BOX)
        ' slot 47
        <PreserveSig> Sub CopyResource(dst As ID3D11Texture2D, src As ID3D11Texture2D)
        ' slot 48
        ''' <remarks>
        ''' the destination resource is a raw pointer so that this method also
        ''' accepts a buffer (for example the per frame constant buffer), not
        ''' only a texture2d.
        ''' </remarks>
        <PreserveSig> Sub UpdateSubresource(dst As IntPtr, dstSubresource As UInteger, dstBox As IntPtr, srcData As IntPtr, srcRowPitch As UInteger, srcDepthPitch As UInteger)
        ' slot 49
        <PreserveSig> Sub CopyStructureCount(dstBuffer As IntPtr, dstAlignedByteOffset As UInteger, srcView As IntPtr)
        ' slot 50
        <PreserveSig> Sub ClearRenderTargetView(renderTargetView As IntPtr, colorRGBA As IntPtr)
        ' slot 51
        <PreserveSig> Sub ClearUnorderedAccessViewUint(unorderedAccessView As IntPtr, values As IntPtr)
        ' slot 52
        <PreserveSig> Sub ClearUnorderedAccessViewFloat(unorderedAccessView As IntPtr, values As IntPtr)
        ' slot 53
        <PreserveSig> Sub ClearDepthStencilView(depthStencilView As IntPtr, clearFlags As UInteger, depth As Single, stencil As Byte)
        ' slot 54
        <PreserveSig> Sub GenerateMips(shaderResourceView As IntPtr)
        ' slot 55
        <PreserveSig> Sub SetResourceMinLOD(resource As IntPtr, minLOD As Single)
        ' slot 56
        <PreserveSig> Function GetResourceMinLOD(resource As IntPtr) As Single
        ' slot 57
        <PreserveSig> Sub ResolveSubresource(dstResource As IntPtr, dstSubresource As UInteger, srcResource As IntPtr, srcSubresource As UInteger, format As Integer)
    End Interface
