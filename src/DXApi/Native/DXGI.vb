Imports System.Runtime.InteropServices

''' <summary>
''' The DXGI (dxgi.dll) interop declarations.
''' </summary>
''' <remarks>
''' This project uses DXGI for two tasks:
'''
''' 1. <see cref="IDXGISurface"/> - sharing a d3d11 texture with direct2d
''' 2. the swap chain (<see cref="IDXGIFactory2"/> + <see cref="IDXGISwapChain"/>)
'''    - presenting the gpu canvas onto a winforms control window
'''
''' Every com interface in this project is declared as a <b>flat vtable</b>: the
''' methods of the base interfaces are written into the derived interface on
''' their exact slot position, an interface inheritance (<c>Inherits</c>) is
''' never used here because it shifts the vtable layout of the derived
''' interface. Only the slots that are actually called are kept, the trailing
''' slots can be omitted safely.
''' </remarks>
Friend Module DXGI

    ''' <summary>
    ''' create a dxgi factory, the directx 11.1 (or above) factory is requested
    ''' through <see cref="DxConstants.IID_IDXGIFactory2"/>.
    ''' </summary>
    <DllImport("dxgi.dll", EntryPoint:="CreateDXGIFactory1", PreserveSig:=True)>
    Friend Function CreateDXGIFactory1(ByRef riid As Guid, <Out> ByRef factory As IntPtr) As Integer
    End Function

    ''' <summary>
    ''' DXGI_FORMAT enumeration (subset)
    ''' </summary>
    Friend Enum DXGI_FORMAT As Integer
        UNKNOWN = 0
        R8G8B8A8_UNORM = 28
        B8G8R8A8_UNORM = 87
        B8G8R8X8_UNORM = 88
    End Enum

    ''' <summary>
    ''' how the swap chain presents the back buffer
    ''' </summary>
    Friend Enum DXGI_SWAP_EFFECT As Integer
        DISCARD = 0
        SEQUENTIAL = 1
        FLIP_SEQUENTIAL = 3
        FLIP_DISCARD = 4
    End Enum

    Friend Enum DXGI_SCALING As Integer
        STRETCH = 0
        NONE = 1
        ASPECT_RATIO_STRETCH = 2
    End Enum

    Friend Enum DXGI_USAGE As UInteger
        SHADER_INPUT = &H10UI
        RENDER_TARGET_OUTPUT = &H20UI
        BACK_BUFFER = &H40UI
        [SHARED] = &H80UI
    End Enum

    Friend Enum DXGI_ALPHA_MODE As Integer
        UNSPECIFIED = 0
        PREMULTIPLIED = 1
        STRAIGHT = 2
    End Enum

    Friend Enum DXGI_PRESENT As UInteger
        TEST = 1UI
        DO_NOT_SEQUENCE = 2UI
        RESTART = 4UI
    End Enum

    Friend Enum DXGI_MODE_SCALING As Integer
        UNSPECIFIED = 0
        CENTERED = 1
        STRETCHED = 2
    End Enum

    Friend Enum DXGI_MODE_SCANLINE_ORDER As Integer
        UNSPECIFIED = 0
        PROGRESSIVE = 1
        INTERLACED = 2
        INTERLACED_UPPER_FIELD_FIRST = 3
        INTERLACED_LOWER_FIELD_FIRST = 4
    End Enum

    Friend Enum DXGI_SWAP_CHAIN_FLAG As UInteger
        NONPREROTATED = 0UI
        ALLOW_MODE_SWITCH = 1UI
        GDI_COMPATIBLE = 2UI
    End Enum

End Module

<StructLayout(LayoutKind.Sequential)>
Friend Structure DXGI_SAMPLE_DESC
    Public Count As UInteger
    Public Quality As UInteger
End Structure

<StructLayout(LayoutKind.Sequential)>
Friend Structure DXGI_SURFACE_DESC
    Public Width As UInteger
    Public Height As UInteger
    Public Format As Integer
    Public SampleDesc As DXGI_SAMPLE_DESC
End Structure

<StructLayout(LayoutKind.Sequential)>
Friend Structure DXGI_RATIONAL
    Public Numerator As UInteger
    Public Denominator As UInteger
End Structure

''' <summary>
''' DXGI_SWAP_CHAIN_DESC1, the 48 bytes sequential layout matches dxgi1_2.h
''' </summary>
''' <remarks>
''' the field order and the field size here must never be changed, a wrong
''' layout is silently accepted by the clr and only blows up inside the
''' native call.
''' </remarks>
<StructLayout(LayoutKind.Sequential)>
Friend Structure DXGI_SWAP_CHAIN_DESC1
    Public Width As UInteger
    Public Height As UInteger
    Public Format As Integer
    ''' <summary>BOOL, stereo rendering is not supported here and is always zero</summary>
    Public Stereo As Integer
    Public SampleDesc As DXGI_SAMPLE_DESC
    Public BufferUsage As UInteger
    Public BufferCount As UInteger
    Public Scaling As Integer
    Public SwapEffect As Integer
    Public AlphaMode As Integer
    Public Flags As UInteger
End Structure

<StructLayout(LayoutKind.Sequential)>
Friend Structure DXGI_SWAP_CHAIN_FULLSCREEN_DESC
    Public RefreshRate As DXGI_RATIONAL
    Public ScanlineOrdering As Integer
    Public Scaling As Integer
    Public Windowed As Integer
End Structure

''' <summary>
''' IDXGISurface: the dxgi surface object that is queried from the
''' d3d11 texture2d resource, Direct2D renders on top of this surface.
''' </summary>
''' <remarks>
''' this project never calls any method of this interface, it only
''' needs the interface pointer for <see cref="D2D1.CreateDxgiSurfaceRenderTarget"/>
''' so the vtable declaration is kept empty on purpose.
''' </remarks>
<ComImport>
<Guid("cafcb56c-6ac3-4889-bf47-9e23bbd260ec")>
<InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
Friend Interface IDXGISurface
End Interface

''' <summary>
''' IDXGIFactory2: used for creating a flip model swap chain on a window.
''' </summary>
''' <remarks>
''' the slots 3 ~ 14 are the methods of IDXGIObject, IDXGIFactory and
''' IDXGIFactory1, they are declared here on purpose so that the slot of
''' <see cref="CreateSwapChainForHwnd"/> lands on 15, but none of them is
''' ever called.
''' </remarks>
<ComImport>
<Guid("50c83a1c-e072-4c48-87b0-3630fa36a6d0")>
<InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
Friend Interface IDXGIFactory2

    ' /******************************************************************************/
    '  slot 3 ~ 6, IDXGIObject
    ' /******************************************************************************/

    <PreserveSig>
    Function SetPrivateData(name As Guid, dataSize As UInteger, data As IntPtr) As Integer
    <PreserveSig>
    Function SetPrivateDataInterface(name As Guid, data As IntPtr) As Integer
    <PreserveSig>
    Function GetPrivateData(name As Guid, ByRef dataSize As UInteger, data As IntPtr) As Integer
    <PreserveSig>
    Function GetParent(ByRef riid As Guid, <Out> ByRef parent As IntPtr) As Integer

    ' /******************************************************************************/
    '  slot 7 ~ 11, IDXGIFactory
    ' /******************************************************************************/

    <PreserveSig>
    Function EnumAdapters(adapter As UInteger, <Out> ByRef pAdapter As IntPtr) As Integer
    <PreserveSig>
    Function MakeWindowAssociation(windowHandle As IntPtr, flags As UInteger) As Integer
    <PreserveSig>
    Function GetWindowAssociation(<Out> ByRef windowHandle As IntPtr) As Integer
    <PreserveSig>
    Function CreateSwapChain(pDevice As IntPtr, desc As IntPtr, <Out> ByRef swapChain As IntPtr) As Integer
    <PreserveSig>
    Function CreateSoftwareAdapter([module] As IntPtr, <Out> ByRef adapter As IntPtr) As Integer

    ' /******************************************************************************/
    '  slot 12 ~ 13, IDXGIFactory1
    ' /******************************************************************************/

    <PreserveSig>
    Function EnumAdapters1(adapter As UInteger, <Out> ByRef pAdapter As IntPtr) As Integer
    <PreserveSig>
    Function IsCurrent() As Integer

    ' /******************************************************************************/
    '  slot 14 ~ 15, IDXGIFactory2
    ' /******************************************************************************/

    <PreserveSig>
    Function IsWindowedStereoEnabled() As Integer

    ''' <summary>
    ''' create a swap chain that is bound to a window handle
    ''' </summary>
    ''' <param name="pDevice">
    ''' the d3d11 device that renders the swap chain back buffer
    ''' </param>
    ''' <param name="hwnd">the target window handle</param>
    ''' <param name="desc">the swap chain description</param>
    ''' <param name="fullscreenDesc">
    ''' the fullscreen description, a null pointer is passed here because the
    ''' canvas is always windowed
    ''' </param>
    ''' <param name="restrictToOutput">a null pointer, no output restriction</param>
    <PreserveSig>
    Function CreateSwapChainForHwnd(
            pDevice As IntPtr,
            hwnd As IntPtr,
            ByRef desc As DXGI_SWAP_CHAIN_DESC1,
            fullscreenDesc As IntPtr,
            restrictToOutput As IntPtr,
            <Out> ByRef swapChain As IntPtr) As Integer

End Interface

''' <summary>
''' IDXGISwapChain: the flip model presentation queue.
''' </summary>
''' <remarks>
''' the slots 3 ~ 7 are the methods of IDXGIObject and IDXGIDeviceSubObject,
''' they are declared here on purpose so that the slots of
''' <see cref="Present"/>, <see cref="GetBuffer"/> and
''' <see cref="ResizeBuffers"/> land on 8, 9 and 13.
''' </remarks>
<ComImport>
<Guid("310d36a0-d02c-4a0a-aa04-6a9d23b8886a")>
<InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
Friend Interface IDXGISwapChain

    ' /******************************************************************************/
    '  slot 3 ~ 6, IDXGIObject
    ' /******************************************************************************/

    <PreserveSig>
    Function SetPrivateData(name As Guid, dataSize As UInteger, data As IntPtr) As Integer
    <PreserveSig>
    Function SetPrivateDataInterface(name As Guid, data As IntPtr) As Integer
    <PreserveSig>
    Function GetPrivateData(name As Guid, ByRef dataSize As UInteger, data As IntPtr) As Integer
    <PreserveSig>
    Function GetParent(ByRef riid As Guid, <Out> ByRef parent As IntPtr) As Integer

    ' /******************************************************************************/
    '  slot 7, IDXGIDeviceSubObject
    ' /******************************************************************************/

    <PreserveSig>
    Function GetDevice(ByRef riid As Guid, <Out> ByRef device As IntPtr) As Integer

    ' /******************************************************************************/
    '  slot 8 ~ 13, IDXGISwapChain
    ' /******************************************************************************/

    ''' <summary>
    ''' present the current back buffer onto the window
    ''' </summary>
    ''' <param name="syncInterval">
    ''' the number of the vertical sync intervals to wait, 0 presents
    ''' immediately and 1 waits for the next vertical blank
    ''' </param>
    <PreserveSig>
    Function Present(syncInterval As UInteger, flags As UInteger) As Integer

    ''' <summary>
    ''' get one of the swap chain back buffers
    ''' </summary>
    ''' <param name="buffer">the zero based back buffer index</param>
    ''' <param name="riid">
    ''' the interface id of the requested surface, for example
    ''' <see cref="DxConstants.IID_IDXGISurface"/>
    ''' </param>
    <PreserveSig>
    Function GetBuffer(buffer As UInteger, ByRef riid As Guid, <Out> ByRef surface As IntPtr) As Integer

    <PreserveSig>
    Function SetFullscreenState(fullscreen As Integer, target As IntPtr) As Integer
    <PreserveSig>
    Function GetFullscreenState(<Out> ByRef fullscreen As Integer, <Out> ByRef target As IntPtr) As Integer
    <PreserveSig>
    Function GetDesc(desc As IntPtr) As Integer

    ''' <summary>
    ''' resize the back buffers, all of the back buffer references must be
    ''' released before this call and must be re-acquired afterwards.
    ''' </summary>
    ''' <param name="bufferCount">0 keeps the current back buffer count</param>
    ''' <param name="newFormat">
    ''' <see cref="DXGI_FORMAT.UNKNOWN"/> keeps the current pixel format
    ''' </param>
    <PreserveSig>
    Function ResizeBuffers(
            bufferCount As UInteger,
            width As UInteger,
            height As UInteger,
            newFormat As Integer,
            flags As UInteger) As Integer

End Interface
