''' <summary>
''' The abstract gpu render surface that the <see cref="DxGraphics"/> canvas
''' draws on.
''' </summary>
''' <remarks>
''' Two render surfaces are implemented in this project:
'''
''' 1. <see cref="DxRenderTarget"/> - an off screen d3d11 texture, the rendered
'''    pixels can be read back into the managed memory for generating the
'''    raster image output
''' 2. <see cref="DxSwapChainTarget"/> - a dxgi flip model swap chain that is
'''    bound to a window handle, every finished frame is presented onto the
'''    window
'''
''' The whole drawing implementation of <see cref="DxGraphics"/> only depends
''' on this contract, so that exactly the same drawing code runs on both of
''' the surfaces.
''' </remarks>
Friend MustInherit Class DxRenderSurface : Implements IDisposable

    Private _width As Integer
    Private _height As Integer
    Private m_disposed As Boolean = False

    ''' <summary>
    ''' the size of this surface in pixels
    ''' </summary>
    Friend ReadOnly Property Width As Integer
        Get
            Return _width
        End Get
    End Property

    Friend ReadOnly Property Height As Integer
        Get
            Return _height
        End Get
    End Property

    ''' <summary>
    ''' the direct2d / d3d11 device that owns this surface
    ''' </summary>
    Friend ReadOnly Property Device As DxDevice

    ''' <summary>
    ''' the direct2d render target which receives the drawing commands
    ''' </summary>
    Friend MustOverride ReadOnly Property Target As ID2D1RenderTarget

    Friend ReadOnly Property IsDisposed As Boolean
        Get
            Return m_disposed
        End Get
    End Property

    ''' <summary>
    ''' does this surface have to be rebuilt before the next frame?
    ''' </summary>
    ''' <remarks>
    ''' a window swap chain sets this flag when the gpu device was removed or
    ''' reset during the previous frame submission. the drawing canvas checks
    ''' this flag at the beginning of a frame and calls <see cref="Recreate"/>
    ''' when it is set.
    ''' </remarks>
    Friend Overridable ReadOnly Property NeedsRecreate As Boolean
        Get
            Return False
        End Get
    End Property

    ''' <summary>
    ''' the native d3d11 texture that is behind the direct2d render target of
    ''' this surface, or zero when the surface can not be used as a direct3d
    ''' output.
    ''' </summary>
    ''' <remarks>
    ''' The 3d pipeline of the scene3d namespace renders into the very same
    ''' texture that direct2d draws on, so the gpu accelerated 3d scene and the
    ''' 2d overlay of the canvas end up on one surface without a copy in
    ''' between.
    '''
    ''' The returned pointer is owned by this surface: it must not be released
    ''' by the caller, and it is only valid until the surface is rebuilt.
    ''' </remarks>
    Friend Overridable ReadOnly Property RenderTargetTexture As IntPtr
        Get
            Return IntPtr.Zero
        End Get
    End Property

    ''' <summary>
    ''' can the rendered pixels be read back into the managed memory?
    ''' </summary>
    ''' <remarks>
    ''' the back buffer of a flip model swap chain can not be mapped directly,
    ''' so such a surface reports false here and its pixels can only be
    ''' captured through a staging texture copy.
    ''' </remarks>
    Friend Overridable ReadOnly Property SupportsReadback As Boolean
        Get
            Return False
        End Get
    End Property

    ''' <summary>
    ''' should the frame submission wait for the vertical blank?
    ''' </summary>
    Friend Property VSync As Boolean = True

    Protected Sub New(device As DxDevice, width As Integer, height As Integer)
        If width <= 0 OrElse height <= 0 Then
            Throw New ArgumentException($"invalid canvas size: [{width}, {height}]")
        End If

        _Device = device
        _width = width
        _height = height
    End Sub

    ''' <summary>
    ''' update the size of this surface, the native resources of the new size
    ''' must be recreated by the implementation.
    ''' </summary>
    Friend MustOverride Sub Resize(newWidth As Integer, newHeight As Integer)

    ''' <summary>
    ''' open a new drawing batch on the render target
    ''' </summary>
    Friend MustOverride Sub BeginDraw()

    ''' <summary>
    ''' finish the current drawing batch, for a window surface the frame is
    ''' presented onto the screen here.
    ''' </summary>
    Friend MustOverride Sub EndDraw()

    ''' <summary>
    ''' rebuild the native resources after <see cref="NeedsRecreate"/> is set
    ''' </summary>
    Friend Overridable Sub Recreate()
        ' this surface never reports NeedsRecreate
    End Sub

    ''' <summary>
    ''' submit all of the pending drawing commands to the gpu device
    ''' </summary>
    Friend Overridable Sub Flush()
        ' this surface has nothing additional to flush
    End Sub

    ''' <summary>
    ''' read back the rendered pixels from the gpu device
    ''' </summary>
    ''' <param name="deferred">
    ''' The back buffer of a flip model swap chain only holds valid pixels
    ''' while the frame is submitted: right after the presentation its content
    ''' is undefined again. Such a surface returns this flag as True and no
    ''' pixels at all, the caller has to finish the current frame through
    ''' <see cref="EndDraw"/> instead and then read the pixels from
    ''' <see cref="CapturedPixels"/>.
    ''' </param>
    ''' <returns>
    ''' a BGRA (blue, green, red, alpha) ordered pixel buffer with the
    ''' premultiplied alpha value
    ''' </returns>
    Friend Overridable Function ReadPixels(ByRef deferred As Boolean) As Byte()
        deferred = False

        Throw New NotSupportedException(
            "the render surface " & Me.GetType().Name & " does not support the pixel read back"
        )
    End Function

    ''' <summary>
    ''' the pixels that were captured while the last frame was submitted
    ''' </summary>
    ''' <remarks>see the <c>deferred</c> parameter of <see cref="ReadPixels"/></remarks>
    Friend Overridable ReadOnly Property CapturedPixels As Byte()
        Get
            Return Nothing
        End Get
    End Property

    ''' <summary>
    ''' update the canvas size, this is only called by the implementation of
    ''' <see cref="Resize(Integer, Integer)"/>
    ''' </summary>
    Protected Sub SetSize(newWidth As Integer, newHeight As Integer)
        _width = newWidth
        _height = newHeight
    End Sub

    ''' <summary>
    ''' release every native resource that is owned by this surface
    ''' </summary>
    Protected MustOverride Sub ReleaseHandle()

    Private Sub Dispose(disposing As Boolean)
        If m_disposed Then
            Return
        End If

        m_disposed = True

        Try
            Call ReleaseHandle()
        Catch
            ' the gpu resource may already been released by the finalizer thread
        End Try
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
