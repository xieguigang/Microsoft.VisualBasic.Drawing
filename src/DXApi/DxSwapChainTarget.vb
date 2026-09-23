Imports System.Runtime.InteropServices

''' <summary>
''' A gpu render surface that presents its frames onto a window: a dxgi flip
''' model swap chain whose back buffer is used as the direct2d render target.
''' </summary>
''' <remarks>
''' The frame pipeline of this class is:
'''
''' 1. create a dxgi factory through <c>CreateDXGIFactory1</c>
''' 2. create a flip model swap chain on the given window handle
''' 3. QueryInterface the <c>IDXGISurface</c> of the back buffer
''' 4. <c>ID2D1Factory::CreateDxgiSurfaceRenderTarget</c> on that surface
''' 5. draw the frame through the direct2d render target
''' 6. <c>ID2D1RenderTarget::EndDraw</c>, then <c>IDXGISwapChain::Present</c>
'''
''' The swap chain is dispatched <b>by raw vtable slot</b> instead of a typed
''' com interface: the factory hands out an <c>IDXGISwapChain1</c> (or newer)
''' object, and resolving it through <see cref="Marshal.GetTypedObjectForIUnknown(IntPtr, Type)"/>
''' would tie this code to one exact swap chain interface id. Reading the three
''' needed slots directly - the same technique that the geometry sink writer
''' of this project uses - keeps this class independent of the swap chain
''' version.
'''
''' A flip model back buffer only holds valid pixels while the frame is being
''' submitted, so the optional frame capture is taken between the end of the
''' direct2d frame and the presentation.
''' </remarks>
Friend Class DxSwapChainTarget : Inherits DxRenderSurface

    ''' <summary>
    ''' IDXGIObject(3) + IDXGIDeviceSubObject(4) + IDXGISwapChain slot position
    ''' of <c>Present</c>
    ''' </summary>
    Private Const SLOT_PRESENT As Integer = 8
    ''' <summary>the slot of <c>IDXGISwapChain::GetBuffer</c></summary>
    Private Const SLOT_GET_BUFFER As Integer = 9
    ''' <summary>the slot of <c>IDXGISwapChain::ResizeBuffers</c></summary>
    Private Const SLOT_RESIZE_BUFFERS As Integer = 13

    '
    ' the swap chain of a flip model canvas is rebuilt when the back buffers can
    ' not be resized in place
    '

    ''' <summary>
    ''' the amount of the back buffers of the swap chain, a flip model swap
    ''' chain requires at least two of them.
    ''' </summary>
    Private Const BACK_BUFFER_COUNT As UInteger = 2UI

    <UnmanagedFunctionPointer(CallingConvention.StdCall)>
    Private Delegate Function PresentFn(instance As IntPtr, syncInterval As UInteger, flags As UInteger) As Integer

    <UnmanagedFunctionPointer(CallingConvention.StdCall)>
    Private Delegate Function GetBufferFn(instance As IntPtr, buffer As UInteger,
                                           ByRef riid As Guid, ByRef surface As IntPtr) As Integer

    <UnmanagedFunctionPointer(CallingConvention.StdCall)>
    Private Delegate Function ResizeBuffersFn(instance As IntPtr, bufferCount As UInteger,
                                              width As UInteger, height As UInteger,
                                              newFormat As Integer, flags As UInteger) As Integer

    Private factory As IDXGIFactory2

    ''' <summary>
    ''' the raw <c>IDXGISwapChain</c> pointer, one interface reference is owned
    ''' by this class
    ''' </summary>
    Private swapChain As IntPtr

    ''' <summary>
    ''' the vtable entries of <see cref="swapChain"/>
    ''' </summary>
    Private presentApi As PresentFn
    Private getBufferApi As GetBufferFn
    Private resizeBuffersApi As ResizeBuffersFn

    ''' <summary>
    ''' the raw IDXGISurface pointer of the current back buffer
    ''' </summary>
    Private surface As IntPtr
    Private m_target As ID2D1RenderTarget

    ''' <summary>
    ''' the cpu readable mirror of the back buffer, it is only created when a
    ''' frame capture is requested
    ''' </summary>
    Private staging As ID3D11Texture2D

    Private windowHandle As IntPtr
    Private surfaceDpi As Single
    Private drawing As Boolean = False
    Private m_needsRecreate As Boolean = False
    Private captureRequested As Boolean = False
    Private captured As Byte() = Nothing

    Friend Overrides ReadOnly Property Target As ID2D1RenderTarget
        Get
            Return m_target
        End Get
    End Property

    Friend Overrides ReadOnly Property NeedsRecreate As Boolean
        Get
            Return m_needsRecreate
        End Get
    End Property

    ''' <summary>
    ''' the pixels that were captured while the last frame was submitted
    ''' </summary>
    Friend Overrides ReadOnly Property CapturedPixels As Byte()
        Get
            Return captured
        End Get
    End Property

    ''' <summary>
    ''' the window handle that receives the presented frames
    ''' </summary>
    Friend ReadOnly Property Handle As IntPtr
        Get
            Return windowHandle
        End Get
    End Property

    Friend Sub New(device As DxDevice,
                   hwnd As IntPtr,
                   width As Integer,
                   height As Integer,
                   Optional dpi As Single = 96.0F,
                   Optional vsync As Boolean = True)

        MyBase.New(device, width, height)

        If hwnd = IntPtr.Zero Then
            Throw New ArgumentException("a window handle is required for the swap chain canvas")
        End If

        windowHandle = hwnd
        surfaceDpi = If(dpi <= 0, 96.0F, dpi)
        VSync = vsync

        Call CreateSwapChain()
        Call CreateTarget()
        Call BeginDraw()
    End Sub

    ''' <summary>
    ''' resolve one vtable slot of a raw com interface pointer as a delegate
    ''' </summary>
    ''' <remarks>the delegate must be kept alive as long as it is used</remarks>
    Private Shared Function ResolveVtable(Of TDelegate As Class)(instance As IntPtr, slot As Integer) As TDelegate
        Dim vtable As IntPtr = Marshal.ReadIntPtr(instance)
        Dim entry As IntPtr = Marshal.ReadIntPtr(vtable, slot * IntPtr.Size)

        Return Marshal.GetDelegateForFunctionPointer(Of TDelegate)(entry)
    End Function

    ''' <summary>
    ''' create the flip model swap chain on the target window
    ''' </summary>
    ''' <remarks>
    ''' the window must already be visible here: dxgi rejects the creation on a
    ''' window that is still being created, so the hosting control creates the
    ''' canvas on its first paint request instead of inside its handle creation.
    ''' </remarks>
    Private Sub CreateSwapChain()
        Dim rawFactory As IntPtr = IntPtr.Zero
        Dim iid As Guid = DxConstants.IID_IDXGIFactory2

        Call ThrowIfFailed(
            DXGI.CreateDXGIFactory1(iid, rawFactory),
            "CreateDXGIFactory1"
        )

        factory = ComObject(Of IDXGIFactory2)(rawFactory)

        Dim desc As New DXGI_SWAP_CHAIN_DESC1 With {
            .Width = CUInt(Width),
            .Height = CUInt(Height),
            .Format = DXGI_FORMAT.B8G8R8A8_UNORM,
            .Stereo = 0,
            .SampleDesc = New DXGI_SAMPLE_DESC With {.Count = 1, .Quality = 0},
            .BufferUsage = CUInt(DXGI_USAGE.RENDER_TARGET_OUTPUT),
            .BufferCount = BACK_BUFFER_COUNT,
            .Scaling = DXGI_SCALING.STRETCH,
            .SwapEffect = DXGI_SWAP_EFFECT.FLIP_DISCARD,
            .AlphaMode = DXGI_ALPHA_MODE.UNSPECIFIED,
            .Flags = 0
        }

        ' the com method takes the device as a raw pointer, so the reference
        ' that is returned here is released again after the call
        Dim rawDevice As IntPtr = Marshal.GetIUnknownForObject(Device.Device)

        Try
            Dim rawSwapChain As IntPtr = IntPtr.Zero

            Call ThrowIfFailed(
                factory.CreateSwapChainForHwnd(rawDevice, windowHandle, desc, IntPtr.Zero, IntPtr.Zero, rawSwapChain),
                "IDXGIFactory2::CreateSwapChainForHwnd"
            )

            swapChain = rawSwapChain
            presentApi = ResolveVtable(Of PresentFn)(swapChain, SLOT_PRESENT)
            getBufferApi = ResolveVtable(Of GetBufferFn)(swapChain, SLOT_GET_BUFFER)
            resizeBuffersApi = ResolveVtable(Of ResizeBuffersFn)(swapChain, SLOT_RESIZE_BUFFERS)
        Finally
            Call Marshal.Release(rawDevice)
        End Try
    End Sub

    ''' <summary>
    ''' create the direct2d render target on the current back buffer
    ''' </summary>
    Private Sub CreateTarget()
        Dim iid As Guid = DxConstants.IID_IDXGISurface
        Dim rawSurface As IntPtr = IntPtr.Zero

        Call ThrowIfFailed(
            getBufferApi(swapChain, 0UI, iid, rawSurface),
            "IDXGISwapChain::GetBuffer"
        )

        surface = rawSurface

        Dim target As ID2D1RenderTarget = Nothing

        ' the window back buffer has no meaningful alpha channel, but the
        ' accepted alpha mode of a dxgi surface render target depends on the
        ' driver, so both of the possible modes are tried here
        If Not TryCreateDxgiTarget(D2D1_ALPHA_MODE.IGNORE, target) Then
            Call TryCreateDxgiTarget(D2D1_ALPHA_MODE.PREMULTIPLIED, target)
        End If

        If target Is Nothing Then
            Throw New ExternalException("unable to create a direct2d render target on the swap chain back buffer")
        End If

        m_target = target
    End Sub

    Private Function TryCreateDxgiTarget(alphaMode As Integer, ByRef target As ID2D1RenderTarget) As Boolean
        Dim props As New D2D1_RENDER_TARGET_PROPERTIES With {
            .type = D2D1_RENDER_TARGET_TYPE.DEFAULT,
            .pixelFormat = New D2D1_PIXEL_FORMAT With {
                .format = DXGI_FORMAT.B8G8R8A8_UNORM,
                .alphaMode = alphaMode
            },
            .dpiX = surfaceDpi,
            .dpiY = surfaceDpi,
            .usage = D2D1_RENDER_TARGET_USAGE.NONE,
            .minLevel = D2D1_FEATURE_LEVEL.DEFAULT
        }
        Dim rawTarget As IntPtr = IntPtr.Zero
        Dim hr As Integer = Device.Factory2D.CreateDxgiSurfaceRenderTarget(surface, props, rawTarget)

        If hr < 0 Then
            Return False
        End If

        target = ComObject(Of ID2D1RenderTarget)(rawTarget)

        Return True
    End Function

    Friend Overrides Sub BeginDraw()
        If IsDisposed OrElse drawing Then
            Return
        End If

        Call m_target.BeginDraw()
        drawing = True
    End Sub

    ''' <summary>
    ''' finish the direct2d frame, capture the back buffer when it was
    ''' requested and present the frame onto the window
    ''' </summary>
    Friend Overrides Sub EndDraw()
        If Not drawing Then
            Return
        End If

        drawing = False

        Dim tag1 As ULong
        Dim tag2 As ULong
        Dim hr As Integer = m_target.EndDraw(tag1, tag2)

        If hr < 0 Then
            Call HandleFailure(hr, "ID2D1RenderTarget::EndDraw")

            Return
        End If

        If captureRequested Then
            captureRequested = False
            captured = Nothing

            Call CaptureBackBuffer()
        End If

        hr = presentApi(swapChain, If(VSync, 1UI, 0UI), 0UI)

        If hr < 0 Then
            Call HandleFailure(hr, "IDXGISwapChain::Present")
        End If
    End Sub

    ''' <summary>
    ''' the pixel read back of a window canvas can only be taken while the
    ''' frame is submitted, so it is deferred to the end of the current frame
    ''' </summary>
    Friend Overrides Function ReadPixels(ByRef deferred As Boolean) As Byte()
        If IsDisposed Then
            Throw New ObjectDisposedException(NameOf(DxSwapChainTarget))
        End If

        deferred = True
        captured = Nothing
        captureRequested = True

        Return Nothing
    End Function

    Friend Overrides Sub Flush()
        If IsDisposed OrElse m_target Is Nothing Then
            Return
        End If

        Dim tag1 As ULong
        Dim tag2 As ULong

        Call m_target.Flush(tag1, tag2)
    End Sub

    ''' <summary>
    ''' resize the swap chain back buffers
    ''' </summary>
    Friend Overrides Sub Resize(newWidth As Integer, newHeight As Integer)
        If IsDisposed OrElse newWidth <= 0 OrElse newHeight <= 0 Then
            Return
        End If

        If newWidth = Width AndAlso newHeight = Height Then
            Return
        End If

        Call EndDraw()
        Call ReleaseTargetResources()

        Call SetSize(newWidth, newHeight)

        If m_needsRecreate Then
            ' the swap chain itself is not usable any more, it is rebuilt on
            ' the new size instead of being resized
            Call ReleaseSwapChain()
            Call CreateSwapChain()

            m_needsRecreate = False
        ElseIf Not TryResizeBuffers(newWidth, newHeight) Then
            ' a flip model swap chain rejects ResizeBuffers while any direct or
            ' indirect reference to a back buffer is still alive, so the whole
            ' chain is rebuilt as the fallback of the failed resize
            Call ReleaseSwapChain()
            Call CreateSwapChain()
        End If

        Call CreateTarget()
        Call BeginDraw()
    End Sub

    ''' <summary>
    ''' resize the back buffers of the swap chain, a failure is reported instead
    ''' of being thrown so that the caller can rebuild the whole chain
    ''' </summary>
    ''' <remarks>
    ''' A flip model swap chain rejects <c>IDXGISwapChain::ResizeBuffers</c> with
    ''' ``DXGI_ERROR_INVALID_CALL`` while any direct or indirect reference to one
    ''' of its back buffers is still alive. The references that the direct2d and
    ''' d3d11 device context keep are not fully under the control of this class,
    ''' so the resize is only attempted here and the caller rebuilds the whole
    ''' swap chain when it does not succeed.
    ''' </remarks>
    Private Function TryResizeBuffers(width As Integer, height As Integer) As Boolean
        If resizeBuffersApi Is Nothing Then
            Return False
        End If

        Dim hr As Integer = resizeBuffersApi(
            swapChain,
            BACK_BUFFER_COUNT,
            CUInt(width),
            CUInt(height),
            DXGI_FORMAT.UNKNOWN,
            0UI)

        If hr >= 0 Then
            Return True
        End If

        If IsDeviceLost(hr) Then
            m_needsRecreate = True
        End If

        Return False
    End Function

    ''' <summary>
    ''' rebuild the whole swap chain after the gpu device was removed or reset
    ''' </summary>
    Friend Overrides Sub Recreate()
        If IsDisposed OrElse Not m_needsRecreate Then
            Return
        End If

        Call ReleaseTargetResources()
        Call ReleaseSwapChain()

        Call CreateSwapChain()
        Call CreateTarget()

        m_needsRecreate = False
        drawing = False
    End Sub

    ''' <summary>
    ''' translate a failed frame submission: a removed device only marks this
    ''' surface for the rebuild that happens at the beginning of the next
    ''' frame, every other failure is reported to the caller.
    ''' </summary>
    Private Sub HandleFailure(hr As Integer, api As String)
        If IsDeviceLost(hr) Then
            m_needsRecreate = True

            Return
        End If

        If hr = DXGI_ERROR_WAS_STILL_DRAWING Then
            ' the gpu is still busy with the previous frame, this frame is
            ' simply dropped and the next one is drawn on the same swap chain
            Return
        End If

        Throw New ExternalException($"{api} call failure, HRESULT: 0x{hr:X8}", hr)
    End Sub

    ''' <summary>
    ''' copy the current back buffer into a staging texture and map it back
    ''' into the managed memory
    ''' </summary>
    Private Sub CaptureBackBuffer()
        Dim iid As Guid = GetType(ID3D11Texture2D).GUID
        Dim rawTexture As IntPtr = IntPtr.Zero

        If getBufferApi(swapChain, 0UI, iid, rawTexture) < 0 Then
            Return
        End If

        Dim backBuffer As ID3D11Texture2D = Nothing

        Try
            backBuffer = ComObject(Of ID3D11Texture2D)(rawTexture)

            Dim context As ID3D11DeviceContext = Device.Context

            Call CreateStaging()

            Call context.CopyResource(staging, backBuffer)

            Dim mapped As D3D11_MAPPED_SUBRESOURCE

            If context.Map(staging, 0, D3D11_MAP.READ, 0, mapped) < 0 Then
                Return
            End If

            Try
                Dim rowBytes As Integer = Width * 4
                Dim buffer As Byte() = New Byte(rowBytes * Height - 1) {}

                If mapped.RowPitch = rowBytes Then
                    Call Marshal.Copy(mapped.pData, buffer, 0, buffer.Length)
                Else
                    For y As Integer = 0 To Height - 1
                        Call Marshal.Copy(New IntPtr(mapped.pData.ToInt64() + CLng(y) * mapped.RowPitch), buffer, y * rowBytes, rowBytes)
                    Next
                End If

                captured = buffer
            Finally
                Call context.Unmap(staging, 0)
            End Try
        Finally
            Call SafeRelease(backBuffer)
        End Try
    End Sub

    ''' <summary>
    ''' create the staging texture that receives the captured frame
    ''' </summary>
    Private Sub CreateStaging()
        If staging IsNot Nothing Then
            Return
        End If

        Dim desc As New D3D11_TEXTURE2D_DESC With {
            .Width = CUInt(Width),
            .Height = CUInt(Height),
            .MipLevels = 1,
            .ArraySize = 1,
            .Format = DXGI_FORMAT.B8G8R8A8_UNORM,
            .SampleDesc = New DXGI_SAMPLE_DESC With {.Count = 1, .Quality = 0},
            .Usage = D3D11_USAGE.STAGING,
            .BindFlags = 0,
            .CPUAccessFlags = CUInt(D3D11_CPU_ACCESS_FLAG.READ),
            .MiscFlags = 0
        }
        Dim raw As IntPtr = IntPtr.Zero

        Call ThrowIfFailed(
            Device.Device.CreateTexture2D(desc, IntPtr.Zero, raw),
            "ID3D11Device::CreateTexture2D(staging)"
        )

        staging = ComObject(Of ID3D11Texture2D)(raw)
    End Sub

    ''' <summary>
    ''' release the direct2d render target and the back buffer of the current
    ''' size, the swap chain itself stays alive
    ''' </summary>
    Private Sub ReleaseTargetResources()
        Call SafeRelease(m_target)

        If surface <> IntPtr.Zero Then
            Call Marshal.Release(surface)
            surface = IntPtr.Zero
        End If

        Call SafeRelease(staging)

        captured = Nothing
        captureRequested = False
    End Sub

    Private Sub ReleaseSwapChain()
        ' the delegates must not be used after the interface is released
        presentApi = Nothing
        getBufferApi = Nothing
        resizeBuffersApi = Nothing

        If swapChain <> IntPtr.Zero Then
            Call Marshal.Release(swapChain)
            swapChain = IntPtr.Zero
        End If

        Call SafeRelease(factory)
    End Sub

    Protected Overrides Sub ReleaseHandle()
        Call ReleaseTargetResources()
        Call ReleaseSwapChain()
    End Sub
End Class
