Imports System.ComponentModel
Imports System.Drawing
Imports System.Windows.Forms
Imports Microsoft.VisualBasic.Drawing.DirectX.Scene3D
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Drawing3D
Imports Bitmap = Microsoft.VisualBasic.Imaging.Bitmap
Imports Brush = Microsoft.VisualBasic.Imaging.Brush
Imports Font = Microsoft.VisualBasic.Imaging.Font
Imports ImageFormats = Microsoft.VisualBasic.Imaging.ImageFormats
Imports SolidBrush = Microsoft.VisualBasic.Imaging.SolidBrush
Imports std = System.Math

''' <summary>
''' A winforms canvas that renders an interactive 3d scene through the gpu
''' accelerated directx pipeline.
''' </summary>
''' <remarks>
''' This control is the winforms adapter of the reusable 3d scene pipeline of
''' the <c>Microsoft.VisualBasic.Drawing.DirectX.Scene3D</c> namespace:
'''
''' * the scene geometry is owned by <see cref="Scene3D.Scene"/>
''' * the view interaction is owned by <see cref="OrbitCameraController"/>
''' * the lighting is owned by <see cref="SceneLighting"/>
''' * the rendering is done by an <see cref="ISceneRenderBackend"/>, the default
'''   back end renders through the direct2d gpu canvas of the base class
'''
''' The control only translates the winforms mouse and keyboard events into the
''' neutral input model of the controller and requests a repaint afterwards, so
''' the whole pipeline stays reusable outside of winforms.
'''
''' Input mapping: the left button orbits the camera, the right button
''' translates the view, the mouse wheel changes the view distance.
''' </remarks>
Public Class DxScene3DCanvas : Inherits DxCanvas

    Private ReadOnly m_scene As New Scene()
    Private ReadOnly m_options As New SceneRenderOptions()
    Private ReadOnly m_controller As New OrbitCameraController()
    Private ReadOnly m_lighting As New SceneLighting()

    Private m_renderer As ISceneRenderBackend = Direct2DSceneRenderer.Default
    ''' <summary>the font of the debug overlay, it is created once and reused</summary>
    Private ReadOnly m_debugFont As New Font("Consolas", 9)
    Private m_showDebugOverlay As Boolean = False
    Private m_extraDebugText As String = Nothing
    Private m_enableKeyboardShortcuts As Boolean = True
    Private m_lastSceneError As String = Nothing

    Public Sub New()
        ' the scene back end clears the canvas itself, so that the background
        ' color and the lighting can be changed at runtime
        Me.AutoClear = False

        AddHandler m_controller.ViewChanged, AddressOf OnControllerViewChanged
        AddHandler Me.Render, AddressOf OnSceneRender

        Call m_lighting.ApplyTo(m_controller.Camera)
    End Sub

    ' /********************************************************************************/
    '  the scene
    ' /********************************************************************************/

    ''' <summary>
    ''' the geometry of the scene, this object is never nothing
    ''' </summary>
    <Browsable(False)>
    Public ReadOnly Property Scene As Scene
        Get
            Return m_scene
        End Get
    End Property

    ''' <summary>
    ''' does the scene hold any geometry?
    ''' </summary>
    <Browsable(False)>
    Public ReadOnly Property HasScene As Boolean
        Get
            Return m_scene.HasData
        End Get
    End Property

    ''' <summary>
    ''' the number of the faces in the scene
    ''' </summary>
    <Browsable(False)>
    Public ReadOnly Property SurfaceCount As Integer
        Get
            Return m_scene.SurfaceCount
        End Get
    End Property

    ''' <summary>
    ''' the number of the points in the scene
    ''' </summary>
    <Browsable(False)>
    Public ReadOnly Property PointCount As Integer
        Get
            Return m_scene.PointCount
        End Get
    End Property

    ''' <summary>
    ''' replace the geometry of the scene with the given faces and fit the view
    ''' onto the new model
    ''' </summary>
    Public Sub LoadSurfaces(faces As IEnumerable(Of Surface))
        Call m_scene.LoadSurfaces(faces)
        Call ApplyNewScene()
    End Sub

    ''' <summary>
    ''' replace the geometry of the scene with the given point cloud and fit the
    ''' view onto the new model
    ''' </summary>
    Public Sub LoadPointCloud(points As IEnumerable(Of PointCloudPoint))
        Call m_scene.LoadPointCloud(points)
        Call ApplyNewScene()
    End Sub

    ''' <summary>
    ''' drop all of the geometry of the scene
    ''' </summary>
    Public Sub ClearScene()
        Call m_scene.Clear()
        Call Invalidate()
        RaiseEvent SceneChanged(Me, EventArgs.Empty)
    End Sub

    ' /********************************************************************************/
    '  the render pipeline
    ' /********************************************************************************/

    ''' <summary>
    ''' the rendering back end of the scene, assign another implementation to
    ''' render the same scene through a different pipeline
    ''' </summary>
    <Browsable(False)>
    <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property Renderer As ISceneRenderBackend
        Get
            Return m_renderer
        End Get
        Set(value As ISceneRenderBackend)
            m_renderer = If(value, Direct2DSceneRenderer.Default)
            Call Invalidate()
        End Set
    End Property

    ''' <summary>
    ''' a readable description of the rendering back end that is in use
    ''' </summary>
    <Browsable(False)>
    Public ReadOnly Property RendererDescription As String
        Get
            Return m_renderer.Description
        End Get
    End Property

    ''' <summary>
    ''' the camera interaction controller of the scene
    ''' </summary>
    <Browsable(False)>
    Public ReadOnly Property Controller As OrbitCameraController
        Get
            Return m_controller
        End Get
    End Property

    ''' <summary>
    ''' the lighting parameters of the scene
    ''' </summary>
    <Browsable(False)>
    Public ReadOnly Property Lighting As SceneLighting
        Get
            Return m_lighting
        End Get
    End Property

    ''' <summary>
    ''' the presentation options of the scene
    ''' </summary>
    <Browsable(False)>
    Public ReadOnly Property Options As SceneRenderOptions
        Get
            Return m_options
        End Get
    End Property

    ''' <summary>
    ''' the message of the last rendering failure, nothing means that the last
    ''' frame was rendered without an error
    ''' </summary>
    <Browsable(False)>
    Public ReadOnly Property LastSceneError As String
        Get
            Return m_lastSceneError
        End Get
    End Property

    ' /********************************************************************************/
    '  the presentation options
    ' /********************************************************************************/

    ''' <summary>
    ''' the presentation mode of the scene
    ''' </summary>
    <Category("DirectX")>
    <DefaultValue(SceneRenderMode.Surface)>
    <Description("the presentation mode of the scene")>
    Public Property RenderMode As SceneRenderMode
        Get
            Return m_options.Mode
        End Get
        Set(value As SceneRenderMode)
            If m_options.Mode = value Then
                Return
            End If

            m_options.Mode = value
            Call Invalidate()
            RaiseEvent RenderModeChanged(Me, EventArgs.Empty)
        End Set
    End Property

    ''' <summary>
    ''' the name of the heat map color scheme, for example ``viridis``
    ''' </summary>
    <Category("DirectX")>
    <DefaultValue("viridis")>
    <Description("the heat map color scheme of the point presentation modes")>
    Public Property ColorScheme As String
        Get
            Return m_options.ColorScheme
        End Get
        Set(value As String)
            Dim scheme As String = If(String.IsNullOrEmpty(value), "viridis", value)

            If m_options.ColorScheme = scheme Then
                Return
            End If

            m_options.ColorScheme = scheme
            Call Invalidate()
        End Set
    End Property

    ''' <summary>
    ''' the edge length in pixels of a point cloud point
    ''' </summary>
    <Category("DirectX")>
    <DefaultValue(2)>
    <Description("the edge length in pixels of a point cloud point")>
    Public Property PointSize As Integer
        Get
            Return m_options.PointSize
        End Get
        Set(value As Integer)
            Dim size As Integer = If(value < 1, 1, value)

            If m_options.PointSize = size Then
                Return
            End If

            m_options.PointSize = size
            Call Invalidate()
        End Set
    End Property

    ''' <summary>
    ''' the alpha channel of the heat map colors of a point cloud
    ''' </summary>
    <Category("DirectX")>
    <DefaultValue(255)>
    <Description("the alpha channel of the heat map colors of a point cloud")>
    Public Property PointAlpha As Integer
        Get
            Return m_options.PointAlpha
        End Get
        Set(value As Integer)
            Dim alpha As Integer = std.Max(0, std.Min(255, value))

            If m_options.PointAlpha = alpha Then
                Return
            End If

            m_options.PointAlpha = alpha
            Call Invalidate()
        End Set
    End Property

    ''' <summary>
    ''' use the per point color of the source file instead of the heat map
    ''' coloring when the file provides one
    ''' </summary>
    <Category("DirectX")>
    <DefaultValue(False)>
    <Description("use the per point color of the source file when it provides one")>
    Public Property UseEmbeddedColor As Boolean
        Get
            Return m_options.UseEmbeddedColor
        End Get
        Set(value As Boolean)
            If m_options.UseEmbeddedColor = value Then
                Return
            End If

            m_options.UseEmbeddedColor = value
            Call Invalidate()
        End Set
    End Property

    ''' <summary>
    ''' draw the ground grid below the model
    ''' </summary>
    <Category("DirectX")>
    <DefaultValue(True)>
    <Description("draw the ground grid below the model")>
    Public Property ShowGround As Boolean
        Get
            Return m_options.ShowGround
        End Get
        Set(value As Boolean)
            If m_options.ShowGround = value Then
                Return
            End If

            m_options.ShowGround = value
            Call Invalidate()
        End Set
    End Property

    ''' <summary>
    ''' the color of the ground grid lines
    ''' </summary>
    <Category("DirectX")>
    <DefaultValue(GetType(Color), "Gray")>
    <Description("the color of the ground grid lines")>
    Public Property GroundColor As Color
        Get
            Return m_options.GroundColor
        End Get
        Set(value As Color)
            m_options.GroundColor = value
            Call Invalidate()
        End Set
    End Property

    ' /********************************************************************************/
    '  the debug overlay
    ' /********************************************************************************/

    ''' <summary>
    ''' draw the camera parameters and the extra debug text onto the canvas
    ''' </summary>
    <Category("DirectX")>
    <DefaultValue(False)>
    <Description("draw the camera parameters of the scene onto the canvas")>
    Public Property ShowDebugOverlay As Boolean
        Get
            Return m_showDebugOverlay
        End Get
        Set(value As Boolean)
            If m_showDebugOverlay = value Then
                Return
            End If

            m_showDebugOverlay = value
            Call Invalidate()
        End Set
    End Property

    ''' <summary>
    ''' additional lines that the debug overlay displays below the camera
    ''' parameters, the host may fill this with its frame rate for example
    ''' </summary>
    <Browsable(False)>
    <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property ExtraDebugText As String
        Get
            Return m_extraDebugText
        End Get
        Set(value As String)
            m_extraDebugText = value
        End Set
    End Property

    ''' <summary>
    ''' the text that the debug overlay displays
    ''' </summary>
    <Browsable(False)>
    Public ReadOnly Property DebugText As String
        Get
            Dim camera As String = m_controller.Camera.ToString()

            If String.IsNullOrEmpty(m_extraDebugText) Then
                Return camera
            End If

            Return camera & Environment.NewLine & m_extraDebugText
        End Get
    End Property

    ' /********************************************************************************/
    '  the commands
    ' /********************************************************************************/

    ''' <summary>
    ''' handle the keyboard shortcuts of the canvas
    ''' </summary>
    <Category("DirectX")>
    <DefaultValue(True)>
    <Description("handle the keyboard shortcuts of the canvas")>
    Public Property EnableKeyboardShortcuts As Boolean
        Get
            Return m_enableKeyboardShortcuts
        End Get
        Set(value As Boolean)
            m_enableKeyboardShortcuts = value
        End Set
    End Property

    ''' <summary>
    ''' compute the view distance that makes the whole model visible
    ''' </summary>
    Public Sub FitView()
        Call m_scene.FitView(m_controller.Camera, Me.ClientSize)
        Call Invalidate()
    End Sub

    ''' <summary>
    ''' restore the default rotation angles and the offset, and fit the view
    ''' onto the model again
    ''' </summary>
    ''' <remarks>
    ''' the lighting parameters are not touched here, the host resets them
    ''' through <see cref="SceneLighting.Reset"/> when it needs to
    ''' </remarks>
    Public Sub ResetView()
        Call m_controller.Reset()
        Call FitView()
    End Sub

    ''' <summary>
    ''' synchronize the viewport of the camera with the client area of this
    ''' control
    ''' </summary>
    Public Sub UpdateViewport()
        m_controller.Camera.Screen = Me.ClientSize
    End Sub

    ''' <summary>
    ''' move the camera towards the model
    ''' </summary>
    Public Sub ZoomIn()
        Call m_controller.ZoomBy(m_controller.ZoomInFactor)
    End Sub

    ''' <summary>
    ''' move the camera away from the model
    ''' </summary>
    Public Sub ZoomOut()
        Call m_controller.ZoomBy(m_controller.ZoomOutFactor)
    End Sub

    ''' <summary>
    ''' switch to the next presentation mode
    ''' </summary>
    Public Sub CycleRenderMode()
        Select Case Me.RenderMode
            Case SceneRenderMode.Surface
                Me.RenderMode = SceneRenderMode.Mesh
            Case SceneRenderMode.Mesh
                Me.RenderMode = SceneRenderMode.PointCloud
            Case Else
                Me.RenderMode = SceneRenderMode.Surface
        End Select
    End Sub

    ''' <summary>
    ''' export the frame that is currently displayed as a raster image
    ''' </summary>
    ''' <remarks>
    ''' this forces a synchronous repaint of the control, so it must not be
    ''' called from inside a render handler
    ''' </remarks>
    Public Function Snapshot() As Bitmap
        Return MyBase.CaptureFrame()
    End Function

    ''' <summary>
    ''' export the frame that is currently displayed into an image file
    ''' </summary>
    ''' <remarks>
    ''' this forces a synchronous repaint of the control, so it must not be
    ''' called from inside a render handler
    ''' </remarks>
    Public Function SaveSnapshot(file As String, Optional format As ImageFormats = ImageFormats.Png) As Boolean
        Return MyBase.SaveImage(file, format)
    End Function

    ''' <summary>
    ''' request a repaint of the canvas
    ''' </summary>
    Public Sub RequestRender()
        Call Invalidate()
    End Sub

    ' /********************************************************************************/
    '  the events
    ' /********************************************************************************/

    ''' <summary>
    ''' raised after the geometry of the scene has been replaced
    ''' </summary>
    Public Event SceneChanged As EventHandler

    ''' <summary>
    ''' raised after the view of the scene has been changed by the interaction
    ''' </summary>
    Public Event ViewChanged As EventHandler

    ''' <summary>
    ''' raised after the presentation mode of the scene has been changed
    ''' </summary>
    Public Event RenderModeChanged As EventHandler

    ''' <summary>
    ''' raised when the user requests a snapshot with the keyboard, the host
    ''' should ask for a file name and call <see cref="SaveSnapshot"/>
    ''' </summary>
    Public Event SnapshotRequested As EventHandler

    ' /********************************************************************************/
    '  the winforms input adapter
    ' /********************************************************************************/

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        Call MyBase.OnMouseDown(e)

        ' the wheel and the keyboard are delivered to the focused control only
        Call Focus()

        Select Case e.Button
            Case MouseButtons.Left
                Call m_controller.MouseDown(SceneMouseButton.Left, e.X, e.Y)
            Case MouseButtons.Right
                Call m_controller.MouseDown(SceneMouseButton.Right, e.X, e.Y)
            Case MouseButtons.Middle
                Call m_controller.MouseDown(SceneMouseButton.Middle, e.X, e.Y)
        End Select
    End Sub

    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
        Call MyBase.OnMouseMove(e)
        Call m_controller.MouseMove(e.X, e.Y)
    End Sub

    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
        Call MyBase.OnMouseUp(e)

        Select Case e.Button
            Case MouseButtons.Left
                Call m_controller.MouseUp(SceneMouseButton.Left)
            Case MouseButtons.Right
                Call m_controller.MouseUp(SceneMouseButton.Right)
            Case MouseButtons.Middle
                Call m_controller.MouseUp(SceneMouseButton.Middle)
        End Select
    End Sub

    Protected Overrides Sub OnMouseWheel(e As MouseEventArgs)
        Call MyBase.OnMouseWheel(e)
        Call m_controller.MouseWheel(e.Delta)
    End Sub

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        Call MyBase.OnKeyDown(e)

        If Not m_enableKeyboardShortcuts Then
            Return
        End If

        Select Case e.KeyCode
            Case Keys.R
                Call ResetView()
            Case Keys.F
                Call FitView()
            Case Keys.M
                Call CycleRenderMode()
            Case Keys.G
                Me.ShowGround = Not Me.ShowGround
            Case Keys.D
                Me.ShowDebugOverlay = Not Me.ShowDebugOverlay
            Case Keys.S
                RaiseEvent SnapshotRequested(Me, EventArgs.Empty)
            Case Keys.Add, Keys.Oemplus, Keys.Up
                Call ZoomIn()
            Case Keys.Subtract, Keys.OemMinus, Keys.Down
                Call ZoomOut()
            Case Else
                Return
        End Select

        e.Handled = True
        e.SuppressKeyPress = True
    End Sub

    ''' <summary>
    ''' the navigation keys and the shortcut keys are input keys of this canvas
    ''' </summary>
    Protected Overrides Function IsInputKey(keyData As Keys) As Boolean
        Select Case keyData And Keys.KeyCode
            Case Keys.R, Keys.F, Keys.M, Keys.G, Keys.D, Keys.S
                Return True
            Case Keys.Add, Keys.Subtract, Keys.Oemplus, Keys.OemMinus
                Return True
            Case Keys.Up, Keys.Down
                Return True
            Case Else
                Return MyBase.IsInputKey(keyData)
        End Select
    End Function

    ' /********************************************************************************/
    '  private helpers
    ' /********************************************************************************/

    ''' <summary>
    ''' render the scene of the current frame and the debug overlay
    ''' </summary>
    Private Sub OnSceneRender(sender As Object, e As DxRenderEventArgs)
        Dim canvas As IGraphics = e.Graphics

        If canvas Is Nothing Then
            Return
        End If

        ' the background color of the base control is the single source of truth
        m_options.BackgroundColor = Me.BackgroundColor

        Try
            Call m_lighting.ApplyTo(m_controller.Camera)

            ' the background is cleared first, so a failed scene render still
            ' shows the background color instead of undefined back buffer content
            Call canvas.Clear(Me.BackgroundColor)

            If m_scene.HasData Then
                Call m_renderer.Render(canvas, m_scene, m_controller.Camera, m_options)
            End If

            m_lastSceneError = Nothing
        Catch ex As Exception
            ' a broken frame must not take the host application down, the
            ' message is exposed through LastSceneError instead
            m_lastSceneError = ex.Message
        End Try

        If Not m_showDebugOverlay Then
            Return
        End If

        Try
            Call DrawDebugOverlay(canvas)
        Catch ex As Exception
            ' a failure of the overlay must not abort the whole frame, so the
            ' overlay is switched off and the reason is reported instead
            m_showDebugOverlay = False
            m_lastSceneError = "debug overlay disabled: " & ex.Message
        End Try
    End Sub

    ''' <summary>
    ''' draw the camera parameters onto the canvas
    ''' </summary>
    Private Sub DrawDebugOverlay(canvas As IGraphics)
        If Not m_showDebugOverlay Then
            Return
        End If

        Dim text As String = Me.DebugText

        If String.IsNullOrEmpty(text) Then
            Return
        End If

        Dim lines As String() = text.Split(New String() {vbCrLf, vbLf}, StringSplitOptions.None)
        Dim padding As Single = 6.0F
        Dim origin As New PointF(8.0F, 8.0F)

        Dim font As Font = m_debugFont
        Dim lineHeight As Single = font.Size * 1.35F
        Dim width As Single = 0

        For Each line As String In lines
            Dim size As SizeF = canvas.MeasureString(line, font)

            If size.Width > width Then
                width = size.Width
            End If
        Next

        Dim height As Single = lineHeight * lines.Length

        Using background As Brush = New SolidBrush(Color.FromArgb(160, 0, 0, 0))
            Call canvas.FillRectangle(background,
                                      origin.X,
                                      origin.Y,
                                      width + padding * 2.0F,
                                      height + padding * 1.5F)
        End Using

        Using textBrush As Brush = New SolidBrush(Color.FromArgb(230, 90, 160, 255))
            For i As Integer = 0 To lines.Length - 1
                Call canvas.DrawString(lines(i), font, textBrush,
                                       origin.X + padding,
                                       origin.Y + padding * 0.75F + i * lineHeight)
            Next
        End Using
    End Sub

    ''' <summary>
    ''' fit the view onto a newly loaded model and reset the view state
    ''' </summary>
    Private Sub ApplyNewScene()
        Call m_controller.Reset()
        Call FitView()
        Call Invalidate()
        RaiseEvent SceneChanged(Me, EventArgs.Empty)
    End Sub

    Private Sub OnControllerViewChanged(sender As Object, e As EventArgs)
        RaiseEvent ViewChanged(Me, EventArgs.Empty)
        Call Invalidate()
    End Sub
End Class
