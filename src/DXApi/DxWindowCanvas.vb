Imports System.Drawing
Imports System.IO
Imports Microsoft.VisualBasic.Imaging
Imports Bitmap = Microsoft.VisualBasic.Imaging.Bitmap

''' <summary>
''' A gpu accelerated 2d drawing canvas that presents its frames onto a window.
''' </summary>
''' <remarks>
''' This class is the hosting object of the winform <c>DxCanvas</c> control, and
''' it is the public entry point of the directx drawing backend for every window
''' based host:
'''
''' 1. a dxgi flip model swap chain is created on the given window handle
''' 2. the back buffer of the swap chain is used as the direct2d render target
''' 3. the drawing goes through exactly the same <see cref="DxGraphics"/> object
'''    that the off screen canvas uses
''' 4. every finished frame is presented onto the window
'''
''' The frame boundary is explicit here: <see cref="BeginDraw"/> opens a frame,
''' the caller draws through <see cref="Graphics"/>, and <see cref="EndDraw"/>
''' submits the frame and presents it.
'''
''' A removed gpu device is handled transparently: the swap chain and the
''' device dependent drawing resources are rebuilt at the beginning of the next
''' frame, so the caller does not have to deal with the device loss at all.
''' </remarks>
Public Class DxWindowCanvas : Implements IDisposable

    Private ReadOnly surface As DxSwapChainTarget
    Private ReadOnly m_graphics As DxGraphics
    Private m_disposed As Boolean = False

    ''' <summary>
    ''' create a gpu accelerated canvas on top of the given window handle
    ''' </summary>
    ''' <param name="hwnd">
    ''' the window handle that receives the presented frames
    ''' </param>
    ''' <param name="width">the canvas size in pixels</param>
    ''' <param name="height">the canvas size in pixels</param>
    ''' <param name="dpi">the device dpi of the hosting window</param>
    ''' <param name="vsync">
    ''' true to wait for the vertical blank on every frame submission
    ''' </param>
    Public Sub New(hwnd As IntPtr,
                   width As Integer,
                   height As Integer,
                   Optional dpi As Single = 96.0F,
                   Optional vsync As Boolean = True)

        surface = New DxSwapChainTarget(DxDevice.Default, hwnd, width, height, dpi, vsync)
        m_graphics = New DxGraphics(surface, Color.Transparent, If(dpi <= 0, 96, CInt(dpi)))
    End Sub

    ''' <summary>
    ''' the drawing canvas of this window, every drawing command goes here
    ''' </summary>
    Public ReadOnly Property Graphics As DxGraphics
        Get
            Return m_graphics
        End Get
    End Property

    ''' <summary>
    ''' a short description of the gpu device that is in use, for example as
    ''' "HARDWARE(feature_level=0xB100)" or "WARP(feature_level=0xB100)"
    ''' </summary>
    Public ReadOnly Property DeviceDescription As String
        Get
            Return m_graphics.DeviceDescription
        End Get
    End Property

    Public ReadOnly Property Width As Integer
        Get
            Return surface.Width
        End Get
    End Property

    Public ReadOnly Property Height As Integer
        Get
            Return surface.Height
        End Get
    End Property

    ''' <summary>
    ''' should every frame submission wait for the vertical blank?
    ''' </summary>
    ''' <remarks>
    ''' waiting for the vertical blank keeps the presentation smooth, but it
    ''' blocks the calling thread for up to one screen refresh interval.
    ''' </remarks>
    Public Property VSync As Boolean
        Get
            Return surface.VSync
        End Get
        Set(value As Boolean)
            surface.VSync = value
        End Set
    End Property

    ''' <summary>
    ''' is the gpu device lost? the swap chain is rebuilt at the next frame.
    ''' </summary>
    Public ReadOnly Property IsDeviceLost As Boolean
        Get
            Return surface.NeedsRecreate
        End Get
    End Property

    ''' <summary>
    ''' open a new drawing frame
    ''' </summary>
    Public Sub BeginDraw()
        If m_disposed Then
            Return
        End If

        Call m_graphics.BeginFrame()
    End Sub

    ''' <summary>
    ''' finish the current drawing frame and present it onto the window
    ''' </summary>
    Public Sub EndDraw()
        If m_disposed Then
            Return
        End If

        Call m_graphics.EndFrame()
    End Sub

    ''' <summary>
    ''' resize the swap chain canvas
    ''' </summary>
    ''' <remarks>
    ''' every device dependent drawing resource is rebuilt on the new size, so
    ''' this must not be called while a frame is opened.
    ''' </remarks>
    Public Sub Resize(width As Integer, height As Integer)
        If m_disposed Then
            Return
        End If

        Call m_graphics.ResizeCanvas(width, height)
    End Sub

    ''' <summary>
    ''' export the current canvas content as a raster image
    ''' </summary>
    ''' <remarks>
    ''' the back buffer of a window canvas is only readable while the frame is
    ''' being submitted, so the current frame is finished (and presented) here
    ''' when it is still open.
    ''' </remarks>
    Public Function GetRasterImage() As Bitmap
        If m_disposed Then
            Throw New ObjectDisposedException(NameOf(DxWindowCanvas))
        End If

        Return m_graphics.GetRasterImage()
    End Function

    ''' <summary>
    ''' save the current canvas content into a image file
    ''' </summary>
    Public Function SaveImage(file As String, Optional format As ImageFormats = ImageFormats.Png) As Boolean
        If m_disposed Then
            Throw New ObjectDisposedException(NameOf(DxWindowCanvas))
        End If

        Return m_graphics.Save(file, format)
    End Function

    ''' <summary>
    ''' save the current canvas content into a stream
    ''' </summary>
    Public Function SaveImage(stream As Stream, format As ImageFormats) As Boolean
        If m_disposed Then
            Throw New ObjectDisposedException(NameOf(DxWindowCanvas))
        End If

        Return m_graphics.Save(stream, format)
    End Function

    Private Sub Dispose(disposing As Boolean)
        If m_disposed Then
            Return
        End If

        m_disposed = True

        ' the surface is owned by this canvas and the graphics object only
        ' borrows it, so both of them are released here
        Call m_graphics.Dispose()
        Call surface.Dispose()
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        Call Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub
End Class
