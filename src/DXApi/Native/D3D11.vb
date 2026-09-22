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
    ''' ID3D11Device, only <see cref="CreateTexture2D"/> (vtable slot 5) is used,
    ''' the immediate device context is returned from <see cref="D3D11.D3D11CreateDevice"/>
    ''' </summary>
    <ComImport>
    <Guid("db6f6ddb-ac77-4e88-8253-819df9bbf140")>
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Friend Interface ID3D11Device
        <PreserveSig> Function CreateBuffer(desc As IntPtr, initialData As IntPtr, <Out> ByRef buffer As IntPtr) As Integer
        <PreserveSig> Function CreateTexture1D(desc As IntPtr, initialData As IntPtr, <Out> ByRef texture As IntPtr) As Integer
        <PreserveSig> Function CreateTexture2D(ByRef desc As D3D11_TEXTURE2D_DESC, initialData As IntPtr, <Out> ByRef texture As ID3D11Texture2D) As Integer
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
    End Interface
