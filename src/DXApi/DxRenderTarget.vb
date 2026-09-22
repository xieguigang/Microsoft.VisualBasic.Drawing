Imports System.Runtime.InteropServices

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
Friend Class DxRenderTarget : Inherits DxRenderSurface

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

    Private m_target As ID2D1RenderTarget
    Private surfaceDpi As Single
    Private drawing As Boolean = False

    Friend Overrides ReadOnly Property Target As ID2D1RenderTarget
        Get
            Return m_target
        End Get
    End Property

    Friend Overrides ReadOnly Property SupportsReadback As Boolean
        Get
            Return True
        End Get
    End Property

    Friend Sub New(device As DxDevice, width As Integer, height As Integer, Optional dpi As Single = 96.0F)
        MyBase.New(device, width, height)

        surfaceDpi = If(dpi <= 0, 96.0F, dpi)

        Call CreateResources()
        Call BeginDraw()
    End Sub

    ''' <summary>
    ''' create the d3d11 textures and the direct2d render target of the
    ''' current canvas size
    ''' </summary>
    Private Sub CreateResources()
        Dim desc As New D3D11_TEXTURE2D_DESC With {
            .Width = CUInt(Width),
            .Height = CUInt(Height),
            .MipLevels = 1,
            .ArraySize = 1,
            .Format = DXGI_FORMAT.B8G8R8A8_UNORM,
            .SampleDesc = New DXGI_SAMPLE_DESC With {.Count = 1, .Quality = 0},
            .Usage = D3D11_USAGE.DEFAULT,
            .BindFlags = CUInt(D3D11_BIND_FLAG.RENDER_TARGET),
            .CPUAccessFlags = 0,
            .MiscFlags = 0
        }
        Dim texPtr As IntPtr = IntPtr.Zero

        Call ThrowIfFailed(
            Device.Device.CreateTexture2D(desc, IntPtr.Zero, texPtr),
            "ID3D11Device::CreateTexture2D"
        )

        texture = ComObject(Of ID3D11Texture2D)(texPtr)

        ' the staging texture for the pixel read back
        desc.Usage = D3D11_USAGE.STAGING
        desc.BindFlags = 0
        desc.CPUAccessFlags = CUInt(D3D11_CPU_ACCESS_FLAG.READ)

        Dim stagePtr As IntPtr = IntPtr.Zero

        Call ThrowIfFailed(
            Device.Device.CreateTexture2D(desc, IntPtr.Zero, stagePtr),
            "ID3D11Device::CreateTexture2D(staging)"
        )

        staging = ComObject(Of ID3D11Texture2D)(stagePtr)

        ' share the texture with direct2d
        surface = ComQuery(texture, DxConstants.IID_IDXGISurface, "IDXGISurface")

        Dim props As New D2D1_RENDER_TARGET_PROPERTIES With {
            .type = D2D1_RENDER_TARGET_TYPE.DEFAULT,
            .pixelFormat = New D2D1_PIXEL_FORMAT With {
                .format = DXGI_FORMAT.B8G8R8A8_UNORM,
                .alphaMode = D2D1_ALPHA_MODE.PREMULTIPLIED
            },
            .dpiX = surfaceDpi,
            .dpiY = surfaceDpi,
            .usage = D2D1_RENDER_TARGET_USAGE.NONE,
            .minLevel = D2D1_FEATURE_LEVEL.DEFAULT
        }
        Dim rawTarget As IntPtr = IntPtr.Zero

        Call ThrowIfFailed(
            Device.Factory2D.CreateDxgiSurfaceRenderTarget(surface, props, rawTarget),
            "ID2D1Factory::CreateDxgiSurfaceRenderTarget"
        )

        m_target = ComObject(Of ID2D1RenderTarget)(rawTarget)
    End Sub

    ''' <summary>
    ''' release the native resources of the current canvas size
    ''' </summary>
    Private Sub ReleaseResources()
        If drawing Then
            Dim tag1 As ULong = 0
            Dim tag2 As ULong = 0

            Try
                Call m_target.EndDraw(tag1, tag2)
            Catch
            End Try

            drawing = False
        End If

        Call SafeRelease(m_target)
        Call SafeRelease(staging)
        Call SafeRelease(texture)

        If surface <> IntPtr.Zero Then
            Call Marshal.Release(surface)
            surface = IntPtr.Zero
        End If
    End Sub

    ''' <summary>
    ''' submit all of the pending draw commands to the gpu device
    ''' </summary>
    Friend Overrides Sub Flush()
        If IsDisposed Then
            Return
        End If

        Dim tag1 As ULong
        Dim tag2 As ULong

        Call m_target.Flush(tag1, tag2)
    End Sub

    ''' <summary>
    ''' finish the current drawing batch, this is required before the pixel
    ''' read back operation.
    ''' </summary>
    Friend Overrides Sub EndDraw()
        If drawing Then
            Dim tag1 As ULong
            Dim tag2 As ULong

            Call ThrowIfFailed(m_target.EndDraw(tag1, tag2), "ID2D1RenderTarget::EndDraw")
            drawing = False
        End If
    End Sub

    Friend Overrides Sub BeginDraw()
        If Not drawing Then
            Call m_target.BeginDraw()
            drawing = True
        End If
    End Sub

    ''' <summary>
    ''' resize the off screen canvas, the d3d11 textures and the direct2d
    ''' render target are recreated on the new size.
    ''' </summary>
    Friend Overrides Sub Resize(newWidth As Integer, newHeight As Integer)
        If IsDisposed OrElse newWidth <= 0 OrElse newHeight <= 0 Then
            Return
        End If

        If newWidth = Width AndAlso newHeight = Height Then
            Return
        End If

        Call ReleaseResources()
        Call SetSize(newWidth, newHeight)
        Call CreateResources()
        Call BeginDraw()
    End Sub

    ''' <summary>
    ''' read back the rendered pixels from the gpu texture
    ''' </summary>
    ''' <returns>
    ''' a BGRA (blue, green, red, alpha) ordered pixel buffer with the
    ''' premultiplied alpha value
    ''' </returns>
    Friend Overrides Function ReadPixels() As Byte()
        If IsDisposed Then
            Throw New ObjectDisposedException(NameOf(DxRenderTarget))
        End If

        Call EndDraw()

        Dim context As ID3D11DeviceContext = Device.Context

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

    Protected Overrides Sub ReleaseHandle()
        Call ReleaseResources()
    End Sub
End Class
