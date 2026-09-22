Imports System.Runtime.InteropServices

Imports Native

''' <summary>
''' An off screen gpu canvas: a d3d11 texture that is shared with direct2d
''' through a dxgi surface.
''' </summary>
''' <remarks>
''' The drawing pipeline of this class is:
'''
''' 1. create a d3d11 texture2d with the RENDER_TARGET bind flag
''' 2. QueryInterface the IDXGISurface of the texture
''' 3. ID2D1Factory::CreateDxgiSurfaceRenderTarget on the dxgi surface
''' 4. draw the 2d primitives through the direct2d render target
''' 5. copy the texture into a staging texture and map it back to the
'''    managed memory for generating the raster image output
''' </remarks>
Friend Class DxRenderTarget : Implements IDisposable

    Friend ReadOnly Property Width As Integer
    Friend ReadOnly Property Height As Integer
    Friend ReadOnly Property Device As DxDevice
    Friend ReadOnly Property Target As ID2D1RenderTarget

    ''' <summary>
    ''' the gpu side render target texture
    ''' </summary>
    Private texture As ID3D11Texture2D
    ''' <summary>
    ''' the cpu readable mirror of <see cref="texture"/>, used for the pixel read back
    ''' </summary>
    Private staging As ID3D11Texture2D
    ''' <summary>
    ''' the raw IDXGISurface pointer of <see cref="texture"/>
    ''' </summary>
    Private surface As IntPtr

    Private drawing As Boolean = False
    Private m_disposed As Boolean = False

    Friend Sub New(device As DxDevice, width As Integer, height As Integer, Optional dpi As Single = 96.0F)
        If width <= 0 OrElse height <= 0 Then
            Throw New ArgumentException($"invalid canvas size: [{width}, {height}]")
        End If

        _Device = device
        _Width = width
        _Height = height

        Dim desc As New D3D11_TEXTURE2D_DESC With {
            .Width = CUInt(width),
            .Height = CUInt(height),
            .MipLevels = 1,
            .ArraySize = 1,
            .Format = DXGI_FORMAT.B8G8R8A8_UNORM,
            .SampleDesc = New DXGI_SAMPLE_DESC With {.Count = 1, .Quality = 0},
            .Usage = D3D11_USAGE.DEFAULT,
            .BindFlags = CUInt(D3D11_BIND_FLAG.RENDER_TARGET),
            .CPUAccessFlags = 0,
            .MiscFlags = 0
        }
        Dim tex As ID3D11Texture2D = Nothing

        Call ThrowIfFailed(
            device.Device.CreateTexture2D(desc, IntPtr.Zero, tex),
            "ID3D11Device::CreateTexture2D"
        )

        texture = tex

        ' the staging texture for the pixel read back
        desc.Usage = D3D11_USAGE.STAGING
        desc.BindFlags = 0
        desc.CPUAccessFlags = CUInt(D3D11_CPU_ACCESS_FLAG.READ)

        Dim stage As ID3D11Texture2D = Nothing

        Call ThrowIfFailed(
            device.Device.CreateTexture2D(desc, IntPtr.Zero, stage),
            "ID3D11Device::CreateTexture2D(staging)"
        )

        staging = stage

        ' share the texture with direct2d
        surface = ComQuery(texture, DxConstants.IID_IDXGISurface, "IDXGISurface")

        Dim props As New D2D1_RENDER_TARGET_PROPERTIES With {
            .type = D2D1_RENDER_TARGET_TYPE.DEFAULT,
            .pixelFormat = New D2D1_PIXEL_FORMAT With {
                .format = DXGI_FORMAT.B8G8R8A8_UNORM,
                .alphaMode = D2D1_ALPHA_MODE.PREMULTIPLIED
            },
            .dpiX = If(dpi <= 0, 96.0F, dpi),
            .dpiY = If(dpi <= 0, 96.0F, dpi),
            .usage = D2D1_RENDER_TARGET_USAGE.NONE,
            .minLevel = D2D1_FEATURE_LEVEL.DEFAULT
        }
        Dim target As ID2D1RenderTarget = Nothing

        Call ThrowIfFailed(
            device.Factory2D.CreateDxgiSurfaceRenderTarget(surface, props, target),
            "ID2D1Factory::CreateDxgiSurfaceRenderTarget"
        )

        _Target = target

        ' the dxgi surface render target requires an explicit begin/end draw pair
        Call target.BeginDraw()

        drawing = True
    End Sub

    ''' <summary>
    ''' submit all of the pending draw commands to the gpu device
    ''' </summary>
    Friend Sub Flush()
        If m_disposed Then
            Return
        End If

        Call _Target.Flush(IntPtr.Zero, IntPtr.Zero)
    End Sub

    ''' <summary>
    ''' finish the current drawing batch, this is required before the pixel
    ''' read back operation.
    ''' </summary>
    Private Sub EndDraw()
        If drawing Then
            Call ThrowIfFailed(_Target.EndDraw(IntPtr.Zero, IntPtr.Zero), "ID2D1RenderTarget::EndDraw")
            drawing = False
        End If
    End Sub

    Private Sub BeginDraw()
        If Not drawing Then
            Call _Target.BeginDraw()
            drawing = True
        End If
    End Sub

    ''' <summary>
    ''' read back the rendered pixels from the gpu texture
    ''' </summary>
    ''' <returns>
    ''' a BGRA (blue, green, red, alpha) ordered pixel buffer with the
    ''' premultiplied alpha value
    ''' </returns>
    Friend Function ReadPixels() As Byte()
        If m_disposed Then
            Throw New ObjectDisposedException(NameOf(DxRenderTarget))
        End If

        Call EndDraw()

        Dim context As ID3D11DeviceContext = _Device.Context

        Call context.CopyResource(staging, texture)

        Dim mapped As D3D11_MAPPED_SUBRESOURCE

        Call ThrowIfFailed(
            context.Map(staging, 0, D3D11_MAP.READ, 0, mapped),
            "ID3D11DeviceContext::Map"
        )

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

            Return buffer
        Finally
            Call context.Unmap(staging, 0)
            Call BeginDraw()
        End Try
    End Function

    Private Sub Dispose(disposing As Boolean)
        If m_disposed Then
            Return
        End If

        m_disposed = True

        If drawing Then
            Try
                Call _Target.EndDraw(IntPtr.Zero, IntPtr.Zero)
            Catch
            End Try

            drawing = False
        End If

        SafeRelease(_Target)
        SafeRelease(staging)
        SafeRelease(texture)

        If surface <> IntPtr.Zero Then
            Call Marshal.Release(surface)
            surface = IntPtr.Zero
        End If
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
