Imports System.Diagnostics
Imports System.IO
Imports Microsoft.VisualBasic.Drawing.DirectX
Imports Microsoft.VisualBasic.Drawing.DirectX.Scene3D
Imports Microsoft.VisualBasic.Imaging.Driver
Imports ImageFormats = Microsoft.VisualBasic.Imaging.ImageFormats
Imports std = System.Math

''' <summary>
''' The main window of the directx accelerated 3d model viewer.
''' </summary>
''' <remarks>
''' The viewer is only a thin host of the reusable scene pipeline: the model
''' files are loaded by <see cref="ModelSceneLoader"/> into the scene of the
''' <see cref="DxScene3DCanvas"/> control, and the window only synchronizes its
''' user interface with the state of that control.
''' </remarks>
Public Class FormMain

    ''' <summary>the heat map color schemes of the point presentation modes</summary>
    Private ReadOnly schemes As String() = {
        "viridis", "magma", "inferno", "plasma", "turbo", "jet",
        "rainbow", "cividis", "mako", "rocket", "viridis:rocket"
    }

    ''' <summary>the selectable edge length of a point cloud point</summary>
    Private ReadOnly pointSizes As String() = {"1", "2", "3", "4", "5", "6", "8", "10", "12"}

    Private currentFile As String = Nothing
    Private showDebug As Boolean = False

    ''' <summary>the off screen multi sampled direct3d 11 back end</summary>
    Private ReadOnly gpuRenderer As New Direct3D11SceneRenderer()
    ''' <summary>the direct back end that renders into the canvas back buffer</summary>
    Private ReadOnly gpuDirectRenderer As New Direct3D11DirectSceneRenderer()

    Private ReadOnly fpsWatch As New Stopwatch()
    Private lastFrameMs As Single = 0
    Private fps As Single = 0

    ' /********************************************************************************/
    '  startup
    ' /********************************************************************************/

    Private Sub FormMain_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' the landscape material readers rely on the registered image drivers
        Call ImageDriver.Register()

        Call SetupToolbar()
        Call SetupCanvas()
        Call ResetLighting()
        Call UpdateStatus()

        Me.fpsWatch.Start()

        ' a model that is given on the command line is opened right away, so the
        ' viewer can be used as a file association handler
        Call OpenCommandLineModel()
    End Sub

    ''' <summary>
    ''' open the model file that was passed on the command line
    ''' </summary>
    ''' <remarks>
    ''' the first argument that exists on disk is used: the other arguments of a
    ''' winforms host process are runtime switches, not model files
    ''' </remarks>
    Private Sub OpenCommandLineModel()
        Dim arguments As String() = Environment.GetCommandLineArgs()

        For i As Integer = 1 To arguments.Length - 1
            Dim candidate As String = arguments(i)

            If File.Exists(candidate) Then
                ' a model that can not be read is reported in the status bar
                ' instead of a modal dialog: a dialog would block the startup of
                ' the viewer before its window is even visible
                Dim errorText As String = LoadFile(candidate)

                If Not String.IsNullOrEmpty(errorText) Then
                    Me.lblDevice.Text = $"加载失败: {errorText}"
                    Me.lblDevice.ForeColor = Color.Firebrick
                End If

                Return
            End If
        Next
    End Sub

    Private Sub SetupToolbar()
        Me.cboMode.Items.AddRange(New Object() {"表面渲染", "三角形网格", "点云 (PLY)"})
        Me.cboScheme.Items.AddRange(schemes)
        Me.numPointSize.Items.AddRange(pointSizes)
        Me.cboPipeline.Items.AddRange(New Object() {
            "D3D11 抗锯齿",
            "D3D11 直连",
            "Direct2D 逐面"
        })

        Me.cboMode.SelectedIndex = 0
        Me.cboScheme.SelectedIndex = 0
        Me.numPointSize.SelectedIndex = 1
        Me.cboPipeline.SelectedIndex = 0
        Me.chkAntiAlias.Checked = False
        Me.chkCull.Checked = False
    End Sub

    Private Sub SetupCanvas()
        With Me.canvas
            .AutoClear = False
            .BackgroundColor = Color.White
            .RenderMode = SceneRenderMode.Surface
            .ColorScheme = "viridis"
            .PointSize = 2
            .PointAlpha = 255
            .UseEmbeddedColor = False
            .ShowGround = True
            .ShowDebugOverlay = False
            .EnableKeyboardShortcuts = True

            ' the true 3d pipeline is the default back end, the direct2d
            ' polygon painter stays available for the comparison
            .Renderer = gpuRenderer
            .MultisampleCount = 1
            .CullBackFaces = False
        End With

        Me.btnBgColor.BackColor = Me.canvas.BackgroundColor
        Me.chkShowGround.Checked = Me.canvas.ShowGround
        Me.chkShowDebug.Checked = Me.canvas.ShowDebugOverlay
    End Sub

    ' /********************************************************************************/
    '  the rendering pipeline of the canvas
    ' /********************************************************************************/

    ''' <summary>
    ''' switch between the direct3d 11 pipeline and the direct2d polygon painter
    ''' </summary>
    Private Sub PipelineChanged(sender As Object, e As EventArgs) Handles cboPipeline.SelectedIndexChanged
        Select Case Me.cboPipeline.SelectedIndex
            Case 1
                Me.canvas.Renderer = gpuDirectRenderer
            Case 2
                Me.canvas.Renderer = Direct2DSceneRenderer.Default
            Case Else
                Me.canvas.Renderer = gpuRenderer
        End Select

        ' the anti aliasing and the back face culling are features of the
        ' direct3d 11 pipeline only, they are meaningless for the painter
        Dim gpu As Boolean = Me.cboPipeline.SelectedIndex <> 2

        Me.chkAntiAlias.Enabled = gpu AndAlso Me.cboPipeline.SelectedIndex = 0
        Me.chkCull.Enabled = gpu

        Call UpdateStatus()
    End Sub

    ''' <summary>
    ''' the number of the samples of the multi sample anti aliasing
    ''' </summary>
    Private Sub AntiAliasChanged(sender As Object, e As EventArgs) Handles chkAntiAlias.CheckedChanged
        Me.canvas.MultisampleCount = If(Me.chkAntiAlias.Checked, 4, 1)
        Call UpdateStatus()
    End Sub

    ''' <summary>
    ''' discard the faces that point away from the viewer
    ''' </summary>
    Private Sub CullChanged(sender As Object, e As EventArgs) Handles chkCull.CheckedChanged
        Me.canvas.CullBackFaces = Me.chkCull.Checked
        Call UpdateStatus()
    End Sub

    ' /********************************************************************************/
    '  open a model or a point cloud
    ' /********************************************************************************/

    Private Sub OpenClick(sender As Object, e As EventArgs) Handles openItem.Click
        Using dialog As New OpenFileDialog()
            dialog.Title = "打开三维模型或 PLY 点云"
            dialog.Filter = ModelSceneLoader.FileDialogFilter

            If dialog.ShowDialog(Me) <> DialogResult.OK Then
                Return
            End If

            Call OpenFile(dialog.FileName)
        End Using
    End Sub

    ''' <summary>
    ''' load the given file into the scene, a failure is reported to the user
    ''' instead of breaking the viewer
    ''' </summary>
    Private Sub OpenFile(filePath As String)
        Dim errorText As String = LoadFile(filePath)

        If Not String.IsNullOrEmpty(errorText) Then
            MessageBox.Show(Me,
                            "加载失败: " & errorText,
                            "错误",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error)
        End If
    End Sub

    ''' <summary>
    ''' load the given file into the scene
    ''' </summary>
    ''' <returns>
    ''' the message of the failure, or nothing when the file was loaded
    ''' </returns>
    Private Function LoadFile(filePath As String) As String
        Try
            If ModelSceneLoader.IsPointCloudFile(filePath) Then
                Dim points As PointCloudPoint() = ModelSceneLoader.LoadPointCloud(filePath)

                If points.Length = 0 Then
                    Throw New InvalidDataException("点云文件之中没有任何点。")
                End If

                Me.canvas.LoadPointCloud(points)
                Me.cboMode.SelectedIndex = 2
                Me.numPointSize.Enabled = True
                Me.chkEmbedded.Enabled = True
            Else
                Dim faces = ModelSceneLoader.LoadSurfaces(filePath)

                If faces.Length = 0 Then
                    Throw New InvalidDataException("模型文件之中没有任何有效的三角面。")
                End If

                Me.canvas.LoadSurfaces(faces)
                Me.cboMode.SelectedIndex = 0
                Me.numPointSize.Enabled = False
                Me.chkEmbedded.Enabled = False
            End If

            currentFile = filePath
            Call ResetLighting()
            Call UpdateStatus()

            Return Nothing
        Catch ex As Exception
            Return ex.Message
        End Try
    End Function

    ' /********************************************************************************/
    '  the render modes
    ' /********************************************************************************/

    Private Sub ModeChanged(sender As Object, e As EventArgs) Handles cboMode.SelectedIndexChanged
        Select Case Me.cboMode.SelectedIndex
            Case 1
                Me.canvas.RenderMode = SceneRenderMode.Mesh
            Case 2
                Me.canvas.RenderMode = SceneRenderMode.PointCloud
            Case Else
                Me.canvas.RenderMode = SceneRenderMode.Surface
        End Select

        Call UpdateStatus()
    End Sub

    Private Sub Canvas_RenderModeChanged(sender As Object, e As EventArgs) Handles canvas.RenderModeChanged
        Dim index As Integer = 0

        Select Case Me.canvas.RenderMode
            Case SceneRenderMode.Mesh
                index = 1
            Case SceneRenderMode.PointCloud
                index = 2
        End Select

        If Me.cboMode.SelectedIndex <> index Then
            Me.cboMode.SelectedIndex = index
        End If

        Call UpdateStatus()
    End Sub

    Private Sub SchemeChanged(sender As Object, e As EventArgs) Handles cboScheme.SelectedIndexChanged
        If Me.cboScheme.SelectedIndex < 0 Then
            Return
        End If

        Me.canvas.ColorScheme = Me.cboScheme.Text
    End Sub

    Private Sub PointSizeChanged(sender As Object, e As EventArgs) Handles numPointSize.SelectedIndexChanged
        Dim size As Integer = 0

        If Me.numPointSize.SelectedIndex >= 0 AndAlso Integer.TryParse(Me.numPointSize.Text, size) Then
            Me.canvas.PointSize = size
        End If
    End Sub

    Private Sub EmbeddedChanged(sender As Object, e As EventArgs) Handles chkEmbedded.CheckedChanged
        Me.canvas.UseEmbeddedColor = Me.chkEmbedded.Checked
    End Sub

    ' /********************************************************************************/
    '  the ground and the background
    ' /********************************************************************************/

    Private Sub ShowGroundChanged(sender As Object, e As EventArgs) Handles chkShowGround.CheckedChanged, groundItem.CheckedChanged
        Dim show As Boolean = IsCheckedItem(sender)

        If Me.chkShowGround.Checked <> show Then
            Me.chkShowGround.Checked = show
        End If

        If Me.groundItem.Checked <> show Then
            Me.groundItem.Checked = show
        End If

        Me.canvas.ShowGround = show
    End Sub

    ''' <summary>
    ''' read the checked state of a checkable tool strip item
    ''' </summary>
    Private Shared Function IsCheckedItem(item As Object) As Boolean
        If TypeOf item Is ToolStripButton Then
            Return DirectCast(item, ToolStripButton).Checked
        End If

        If TypeOf item Is ToolStripMenuItem Then
            Return DirectCast(item, ToolStripMenuItem).Checked
        End If

        Return False
    End Function

    Private Sub BgColorClick(sender As Object, e As EventArgs) Handles btnBgColor.Click, bgColorItem.Click
        Using dialog As New ColorDialog()
            dialog.Color = Me.canvas.BackgroundColor
            dialog.FullOpen = True

            If dialog.ShowDialog(Me) <> DialogResult.OK Then
                Return
            End If

            Me.canvas.BackgroundColor = dialog.Color
            Me.btnBgColor.BackColor = dialog.Color
            Call Me.canvas.RequestRender()
        End Using
    End Sub

    ' /********************************************************************************/
    '  the view
    ' /********************************************************************************/

    Private Sub ResetViewClick(sender As Object, e As EventArgs) Handles btnReset.Click, resetViewItem.Click
        Call Me.canvas.ResetView()
        Call ResetLighting()
        Call UpdateStatus()
    End Sub

    Private Sub FitViewClick(sender As Object, e As EventArgs) Handles fitViewItem.Click
        Call Me.canvas.FitView()
        Call UpdateStatus()
    End Sub

    Private Sub Canvas_ViewChanged(sender As Object, e As EventArgs) Handles canvas.ViewChanged
        Call UpdateStatus()
    End Sub

    Private Sub Canvas_SceneChanged(sender As Object, e As EventArgs) Handles canvas.SceneChanged
        Call UpdateStatus()
    End Sub

    ' /********************************************************************************/
    '  the lighting
    ' /********************************************************************************/

    Private Sub LightingScroll(sender As Object, e As EventArgs) Handles trkAzimuth.Scroll,
                                                                            trkElevation.Scroll,
                                                                            trkAmbient.Scroll,
                                                                            trkIntensity.Scroll
        Call ApplyLighting()
    End Sub

    ''' <summary>
    ''' push the slider values into the lighting of the scene and refresh the
    ''' value labels
    ''' </summary>
    Private Sub ApplyLighting()
        With Me.canvas.Lighting
            .Azimuth = Me.trkAzimuth.Value
            .Elevation = Me.trkElevation.Value
            .Ambient = Me.trkAmbient.Value
            .Intensity = Me.trkIntensity.Value
            .ApplyTo(Me.canvas.Controller.Camera)

            Me.lblAzimuthValue.Text = .Azimuth.ToString()
            Me.lblElevationValue.Text = .Elevation.ToString()
            Me.lblAmbientValue.Text = .Ambient.ToString()
            Me.lblIntensityValue.Text = .Intensity.ToString()
        End With

        Call Me.canvas.RequestRender()
        Call UpdateStatus()
    End Sub

    ''' <summary>
    ''' restore the default lighting parameters and the default light color
    ''' </summary>
    Private Sub ResetLighting()
        Me.canvas.Lighting.Reset()

        Me.trkAzimuth.Value = SceneLighting.DefaultAzimuth
        Me.trkElevation.Value = SceneLighting.DefaultElevation
        Me.trkAmbient.Value = SceneLighting.DefaultAmbient
        Me.trkIntensity.Value = SceneLighting.DefaultIntensity
        Me.lblLightColor.BackColor = Me.canvas.Lighting.LightColor

        Call ApplyLighting()
    End Sub

    Private Sub LightColorClick(sender As Object, e As EventArgs) Handles btnLightColor.Click
        Using dialog As New ColorDialog()
            dialog.Color = Me.canvas.Lighting.LightColor
            dialog.FullOpen = True

            If dialog.ShowDialog(Me) <> DialogResult.OK Then
                Return
            End If

            Me.canvas.Lighting.LightColor = dialog.Color
            Me.lblLightColor.BackColor = dialog.Color

            Call ApplyLighting()
        End Using
    End Sub

    Private Sub ResetLightClick(sender As Object, e As EventArgs) Handles btnResetLight.Click
        Call ResetLighting()
    End Sub

    ' /********************************************************************************/
    '  the debug overlay
    ' /********************************************************************************/

    Private Sub ShowDebugChanged(sender As Object, e As EventArgs) Handles chkShowDebug.CheckedChanged, debugItem.CheckedChanged
        Dim show As Boolean = IsCheckedItem(sender)

        If Me.chkShowDebug.Checked <> show Then
            Me.chkShowDebug.Checked = show
        End If

        If Me.debugItem.Checked <> show Then
            Me.debugItem.Checked = show
        End If

        showDebug = show
        Me.canvas.ShowDebugOverlay = show
        Me.lastFrameMs = 0
        Me.fps = 0

        Call Me.canvas.RequestRender()
    End Sub

    ''' <summary>
    ''' the render handler of the canvas, it maintains the frame rate that the
    ''' debug overlay and the status bar display
    ''' </summary>
    Private Sub Canvas_Render(sender As Object, e As DxRenderEventArgs) Handles canvas.Render
        If Not showDebug Then
            Return
        End If

        Dim elapsed As Single = CSng(Me.fpsWatch.Elapsed.TotalMilliseconds)
        Dim delta As Single = elapsed - Me.lastFrameMs

        Me.lastFrameMs = elapsed

        If delta > 0 Then
            Dim instant As Single = 1000.0F / delta
            Me.fps = If(Me.fps <= 0, instant, Me.fps * 0.9F + instant * 0.1F)
        End If

        Me.canvas.ExtraDebugText = $"FPS: {Me.fps:F1}"
    End Sub

    ' /********************************************************************************/
    '  the snapshot
    ' /********************************************************************************/

    Private Sub SnapshotClick(sender As Object, e As EventArgs) Handles btnSnapshot.Click, snapshotItem.Click
        Call SaveSnapshot()
    End Sub

    Private Sub Canvas_SnapshotRequested(sender As Object, e As EventArgs) Handles canvas.SnapshotRequested
        Call SaveSnapshot()
    End Sub

    ''' <summary>
    ''' export the frame that is currently displayed into an image file
    ''' </summary>
    Private Sub SaveSnapshot()
        If Not Me.canvas.HasScene Then
            MessageBox.Show(Me, "当前没有可以导出的内容，请先打开一个模型或点云。",
                            "截图", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Using dialog As New SaveFileDialog()
            dialog.Title = "保存当前画面"
            dialog.Filter = "PNG 图片 (*.png)|*.png|BMP 图片 (*.bmp)|*.bmp|WebP 图片 (*.webp)|*.webp|JPEG 图片 (*.jpg)|*.jpg"
            dialog.FileName = "model-viewer.png"

            If dialog.ShowDialog(Me) <> DialogResult.OK Then
                Return
            End If

            Dim format As ImageFormats = ImageFormats.Png

            Select Case Path.GetExtension(dialog.FileName).ToLowerInvariant()
                Case ".bmp"
                    format = ImageFormats.Bmp
                Case ".webp"
                    format = ImageFormats.Webp
                Case ".jpg", ".jpeg"
                    format = ImageFormats.Jpeg
            End Select

            Try
                ' the export forces a synchronous repaint of the canvas, so it
                ' runs outside of the render event
                If Me.canvas.SaveSnapshot(dialog.FileName, format) Then
                    MessageBox.Show(Me, "画面已保存: " & dialog.FileName,
                                    "截图", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Else
                    MessageBox.Show(Me, "画面导出失败。",
                                    "截图", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                End If
            Catch ex As Exception
                MessageBox.Show(Me, "截图失败: " & ex.Message,
                                "错误", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Using
    End Sub

    ' /********************************************************************************/
    '  the about dialog and the exit
    ' /********************************************************************************/

    Private Sub AboutClick(sender As Object, e As EventArgs) Handles aboutItem.Click
        Using dialog As New FormAbout()
            dialog.ShowDialog(Me)
        End Using
    End Sub

    Private Sub ExitClick(sender As Object, e As EventArgs) Handles exitItem.Click
        Me.Close()
    End Sub

    ' /********************************************************************************/
    '  the status bar
    ' /********************************************************************************/

    ''' <summary>
    ''' synchronize the status bar with the current state of the canvas
    ''' </summary>
    Private Sub UpdateStatus()
        Dim mode As String = Nothing

        Select Case Me.canvas.RenderMode
            Case SceneRenderMode.Mesh
                mode = "三角形网格"
            Case SceneRenderMode.PointCloud
                mode = "点云"
            Case Else
                mode = "表面渲染"
        End Select

        Dim camera = Me.canvas.Controller.Camera
        Dim file As String = If(String.IsNullOrEmpty(currentFile), "(未加载)", Path.GetFileName(currentFile))
        Dim counts As String = If(Me.canvas.PointCount > 0,
                                  $"点数: {Me.canvas.PointCount}",
                                  $"面数: {Me.canvas.SurfaceCount}")

        Dim pipeline As String = If(Me.canvas.IsGpuPipelineActive,
                                    $"Direct3D 11 {Me.canvas.ActiveMultisampleCount}x",
                                    "Direct2D painter")

        Me.lblStatus.Text =
            $"文件: {file}  |  模式: {mode}  |  {counts}  |  角度 X={camera.AngleX:F1}° Y={camera.AngleY:F1}°  |  视距: {camera.ViewDistance:F1}  |  环境光: {Me.canvas.Lighting.Ambient}%  亮度: {Me.canvas.Lighting.Intensity}%"

        ' a device that is still being created and a frame that failed both have
        ' to be visible here, otherwise a missing canvas looks like a silent
        ' "nothing is drawn" problem
        Dim errorText As String = Me.canvas.LastSceneError

        If String.IsNullOrEmpty(errorText) Then
            errorText = Me.canvas.LastError
        End If

        Dim fallback As String = Me.canvas.RendererFallbackReason

        If Not String.IsNullOrEmpty(errorText) Then
            Me.lblDevice.Text = $"渲染错误: {errorText}"
            Me.lblDevice.ForeColor = Color.Firebrick
        ElseIf Not String.IsNullOrEmpty(fallback) Then
            ' the gpu pipeline handed the frame over to the polygon painter, the
            ' reason must be visible or the missing performance looks random
            Me.lblDevice.Text = $"已回退到逐面绘制: {fallback}"
            Me.lblDevice.ForeColor = Color.DarkOrange
        Else
            Me.lblDevice.Text = $"管线: {pipeline}  |  设备: {Me.canvas.DeviceDescription}"
            Me.lblDevice.ForeColor = SystemColors.ControlText
        End If
    End Sub

    ''' <summary>
    ''' the directx canvas has been created, so the device information of the
    ''' status bar is refreshed and the scene is drawn onto the new canvas
    ''' </summary>
    Private Sub Canvas_DeviceCreated(sender As Object, e As EventArgs) Handles canvas.DeviceCreated
        Call UpdateStatus()
        Call Me.canvas.RequestRender()
    End Sub

    Private Sub 复制设备错误信息ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 复制设备错误信息ToolStripMenuItem.Click
        Call Clipboard.SetText(lblDevice.Text)
    End Sub
End Class
