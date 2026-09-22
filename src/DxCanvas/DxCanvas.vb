Imports System.ComponentModel
Imports System.Drawing
Imports ImageFormats = Microsoft.VisualBasic.Imaging.ImageFormats

''' <summary>
''' A winforms control that draws its whole content through the gpu accelerated
''' directx 2d pipeline.
''' </summary>
''' <remarks>
''' The control owns a dxgi flip model swap chain that is bound to its own
''' window handle, so the winform paint cycle drives the gpu presentation
''' directly:
'''
''' 1. <see cref="OnPaint"/> opens a drawing frame on the swap chain
''' 2. the <see cref="Render"/> event is raised, the handler draws through the
'''    <c>IGraphics</c> of the event data
''' 3. <see cref="OnPaint"/> finishes the frame and presents it onto the window
'''
''' The control never paints through gdi+ - the gpu canvas owns every pixel of
''' the client area - so the background painting is suppressed and the opaque
''' style is set, which removes the flickering frame of a gdi+ filled control.
'''
''' The canvas is recreated together with the window handle, so docking,
''' re-parenting and a dpi change are handled transparently. A removed gpu
''' device is handled by the backend itself: the swap chain is rebuilt at the
''' beginning of the next frame.
''' </remarks>
Partial Public Class DxCanvas

    Private m_canvas As DxWindowCanvas = Nothing
    Private m_graphics As DxGraphics = Nothing
    ''' <summary>guard against the reentrant paint request of a nested message loop</summary>
    Private m_rendering As Boolean = False
    Private m_autoClear As Boolean = True
    Private m_backgroundColor As Color = Color.White
    Private m_vsync As Boolean = True
    Private m_lastError As String = Nothing
    ''' <summary>the pending export request of <see cref="SaveImage"/></summary>
    Private m_capturePending As Boolean = False
    Private m_captureResult As Boolean = False
    Private m_captureFile As String = Nothing
    Private m_captureFormat As ImageFormats = ImageFormats.Png

    Public Sub New()
        Call InitializeComponent()

        ' the directx canvas paints every pixel of the control itself, so the
        ' gdi+ background painting is suppressed here to avoid a flickering
        ' frame, and the double buffering of winforms is not required at all
        ' because the swap chain already presents an off screen buffer
        Call SetStyle(ControlStyles.UserPaint Or
                      ControlStyles.AllPaintingInWmPaint Or
                      ControlStyles.Opaque, True)

        Call SetStyle(ControlStyles.OptimizedDoubleBuffer, False)
        Call SetStyle(ControlStyles.ResizeRedraw, True)
    End Sub

    ''' <summary>
    ''' raised when the control needs to redraw its content
    ''' </summary>
    ''' <remarks>
    ''' the frame is already opened when the event is raised: everything that is
    ''' drawn in the handler becomes visible when the frame is presented right
    ''' after the event returns.
    ''' </remarks>
    Public Event Render(sender As Object, e As DxRenderEventArgs)

    ' /********************************************************************************/
    '  the public api of the control
    ' /********************************************************************************/

    ''' <summary>
    ''' the gpu accelerated drawing canvas of this control
    ''' </summary>
    ''' <remarks>
    ''' the canvas is only available after the window handle of the control has
    ''' been created. The content that is drawn outside of the
    ''' <see cref="Render"/> event is presented by the next paint request.
    ''' </remarks>
    <Browsable(False)>
    Public ReadOnly Property Graphics As DxGraphics
        Get
            Return m_graphics
        End Get
    End Property

    ''' <summary>
    ''' a short description of the gpu device that is in use
    ''' </summary>
    <Browsable(False)>
    Public ReadOnly Property DeviceDescription As String
        Get
            If m_canvas Is Nothing Then
                Return "the directx canvas is not created yet"
            End If

            Return m_canvas.DeviceDescription
        End Get
    End Property

    ''' <summary>
    ''' was the gpu device removed? the swap chain is rebuilt on the next frame.
    ''' </summary>
    <Browsable(False)>
    Public ReadOnly Property IsDeviceLost As Boolean
        Get
            If m_canvas Is Nothing Then
                Return False
            End If

            Return m_canvas.IsDeviceLost
        End Get
    End Property

    ''' <summary>
    ''' the message of the last rendering failure, Nothing means that every
    ''' frame was drawn without an error
    ''' </summary>
    <Browsable(False)>
    Public ReadOnly Property LastError As String
        Get
            Return m_lastError
        End Get
    End Property

    ''' <summary>
    ''' clear the canvas with the <see cref="BackgroundColor"/> at the beginning
    ''' of every frame
    ''' </summary>
    ''' <remarks>
    ''' the back buffer of a flip model swap chain holds undefined pixels after
    ''' the frame has been presented, so this should only be turned off when the
    ''' render handler covers the whole canvas itself.
    ''' </remarks>
    <Category("DirectX")>
    <DefaultValue(True)>
    <Description("clear the canvas with the background color at the beginning of every frame")>
    Public Property AutoClear As Boolean
        Get
            Return m_autoClear
        End Get
        Set(value As Boolean)
            m_autoClear = value
        End Set
    End Property

    ''' <summary>
    ''' the color of the canvas background
    ''' </summary>
    <Category("DirectX")>
    <DefaultValue(GetType(Color), "White")>
    <Description("the background color of the canvas")>
    Public Property BackgroundColor As Color
        Get
            Return m_backgroundColor
        End Get
        Set(value As Color)
            m_backgroundColor = value
        End Set
    End Property

    ''' <summary>
    ''' should the frame submission wait for the vertical blank?
    ''' </summary>
    ''' <remarks>
    ''' waiting for the vertical blank keeps the presentation smooth, but it
    ''' blocks the ui thread for up to one screen refresh interval per frame.
    ''' </remarks>
    <Category("DirectX")>
    <DefaultValue(True)>
    <Description("wait for the vertical blank on every frame submission")>
    Public Property VSync As Boolean
        Get
            Return m_vsync
        End Get
        Set(value As Boolean)
            m_vsync = value

            If m_canvas IsNot Nothing Then
                m_canvas.VSync = value
            End If
        End Set
    End Property

    ''' <summary>
    ''' save the current canvas content into an image file
    ''' </summary>
    ''' <remarks>
    ''' The back buffer of the window swap chain only holds valid pixels while
    ''' the frame is being submitted, so this forces a synchronous repaint of
    ''' the control and captures the pixels of that frame: the
    ''' <see cref="Render"/> event is raised one more time to do so.
    ''' </remarks>
    Public Function SaveImage(file As String, Optional format As ImageFormats = ImageFormats.Png) As Boolean
        If m_rendering Then
            Throw New InvalidOperationException(
                "the canvas can not be exported from inside the Render event"
            )
        End If

        If m_canvas Is Nothing Then
            Return False
        End If

        m_captureFile = file
        m_captureFormat = format
        m_capturePending = True
        m_captureResult = False

        Try
            Call Invalidate()
            Call Update()
        Finally
            m_capturePending = False
        End Try

        Return m_captureResult
    End Function

    ' /********************************************************************************/
    '  the winforms lifecycle
    ' /********************************************************************************/

    ''' <summary>
    ''' the canvas is not created here: dxgi rejects a swap chain that is
    ''' created on a window which is still being created, so the canvas is
    ''' created lazily by the first paint request instead.
    ''' </summary>
    Protected Overrides Sub OnHandleCreated(e As EventArgs)
        Call MyBase.OnHandleCreated(e)
    End Sub

    ''' <summary>
    ''' the window handle is destroyed, so the swap chain that is bound to it has
    ''' to be released here, this also covers the disposal of the control.
    ''' </summary>
    Protected Overrides Sub OnHandleDestroyed(e As EventArgs)
        Call ReleaseCanvas()
        Call MyBase.OnHandleDestroyed(e)
    End Sub

    Protected Overrides Sub OnSizeChanged(e As EventArgs)
        Call MyBase.OnSizeChanged(e)

        If m_canvas IsNot Nothing Then
            Try
                Call m_canvas.Resize(ClientSize.Width, ClientSize.Height)
            Catch ex As Exception
                m_lastError = ex.Message
                Call ReleaseCanvas()
            End Try
        End If

        Call Invalidate()
    End Sub

    ''' <summary>
    ''' the directx canvas owns the whole client area, so the gdi+ background
    ''' painting is suppressed here
    ''' </summary>
    Protected Overrides Sub OnPaintBackground(pevent As PaintEventArgs)
        ' the gpu canvas paints every pixel of the client area
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Call MyBase.OnPaint(e)

        If m_rendering Then
            Return
        End If

        ' the handle may have been recreated (a docked panel, a dpi change or a
        ' previous rendering failure)
        If m_canvas Is Nothing Then
            Call CreateCanvas()

            If m_canvas Is Nothing Then
                Return
            End If
        End If

        m_rendering = True

        Try
            Try
                Call m_canvas.BeginDraw()

                If m_autoClear Then
                    Call m_graphics.Clear(m_backgroundColor)
                End If
            Catch ex As Exception
                ' the gpu canvas is not usable, it is rebuilt by the next paint
                m_lastError = ex.Message
                Call ReleaseCanvas()

                Return
            End Try

            ' an exception of the render handler is a caller bug and is not
            ' swallowed here, the frame is only finished when it returns
            RaiseEvent Render(Me, New DxRenderEventArgs(m_graphics, m_graphics.Size))

            If m_capturePending Then
                m_capturePending = False

                Try
                    ' the export reads the back buffer between the end of the
                    ' direct2d frame and the presentation, and it submits (and
                    ' presents) the frame by itself
                    m_captureResult = m_graphics.Save(m_captureFile, m_captureFormat)
                    m_lastError = Nothing
                Catch ex As Exception
                    m_lastError = ex.Message
                    m_captureResult = False
                End Try
            Else
                Try
                    Call m_canvas.EndDraw()

                    m_lastError = Nothing
                Catch ex As Exception
                    m_lastError = ex.Message
                    Call ReleaseCanvas()
                End Try
            End If
        Finally
            m_rendering = False
        End Try
    End Sub

    ' /********************************************************************************/
    '  private helpers
    ' /********************************************************************************/

    ''' <summary>
    ''' create the directx canvas on the window of this control
    ''' </summary>
    Private Sub CreateCanvas()
        If m_canvas IsNot Nothing Then
            Return
        End If

        Dim size As Size = ClientSize

        If size.Width <= 0 OrElse size.Height <= 0 Then
            Return
        End If

        Try
            ' the canvas works in the pixel coordinate system of the window, so
            ' one drawing unit is exactly one pixel of the client area
            m_canvas = New DxWindowCanvas(Handle, size.Width, size.Height, 96.0F, m_vsync)
            m_graphics = m_canvas.Graphics
            m_lastError = Nothing
        Catch ex As Exception
            m_lastError = ex.Message
            Call ReleaseCanvas()
        End Try
    End Sub

    Private Sub ReleaseCanvas()
        m_graphics = Nothing

        If m_canvas IsNot Nothing Then
            Try
                Call m_canvas.Dispose()
            Catch ex As Exception
                m_lastError = ex.Message
            End Try

            m_canvas = Nothing
        End If
    End Sub
End Class
