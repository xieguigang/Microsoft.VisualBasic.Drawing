Imports System.Diagnostics
Imports System.Drawing
Imports System.Windows.Forms
Imports Microsoft.VisualBasic.Drawing.DirectX
Imports Microsoft.VisualBasic.Imaging
Imports ImagingBitmap = Microsoft.VisualBasic.Imaging.Bitmap

''' <summary>
''' The winforms demo of the <see cref="DxCanvas"/> control: the window hosts
''' the gpu accelerated canvas and draws a scene of tens of thousands of random
''' polygons through the <c>Render</c> event.
''' </summary>
''' <remarks>
''' The polygons are generated once per canvas size and then replayed by every
''' frame, so the frame time that is shown in the status bar is the pure
''' drawing cost of the directx gpu pipeline.
''' </remarks>
Module DxCanvasDemo

    ''' <summary>
    ''' the amount of the polygon primitives of the demo scene
    ''' </summary>
    Friend Const DEFAULT_POLYGON_COUNT As Integer = 30000
    ''' <summary>the radius range of the small polygon scene</summary>
    Friend Const MIN_RADIUS As Integer = 3
    Friend Const MAX_RADIUS As Integer = 17

    ''' <summary>
    ''' open the interactive demo window of the <see cref="DxCanvas"/> control
    ''' </summary>
    ''' <param name="polygonCount">
    ''' the amount of the polygon primitives of the demo scene
    ''' </param>
    Sub Run(Optional polygonCount As Integer = 0)
        If polygonCount <= 0 Then
            polygonCount = DEFAULT_POLYGON_COUNT
        End If

        Call Application.EnableVisualStyles()
        Call printHeader(polygonCount)

        Call Application.Run(New DxCanvasDemoForm(polygonCount))
    End Sub

    ''' <summary>
    ''' open the demo window, repaint it on a fixed interval, close it after the
    ''' given amount of seconds and then print the frame statistics of that run.
    ''' </summary>
    ''' <remarks>
    ''' This is the headless friendly mode that is used to verify the control
    ''' without any user interaction: the first frame is reported separately
    ''' because it pays for the gpu warm up and the scene generation.
    ''' </remarks>
    Sub RunSmoke(Optional polygonCount As Integer = 0, Optional seconds As Integer = 3)
        If polygonCount <= 0 Then
            polygonCount = DEFAULT_POLYGON_COUNT
        End If

        Call Application.EnableVisualStyles()
        Call printHeader(polygonCount)

        Dim form As New DxCanvasDemoForm(polygonCount)
        Dim repaint As New Timer With {.Interval = 100}
        Dim autoClose As New Timer With {.Interval = seconds * 1000}
        Dim file As String = "./dxcanvas_smoke.png"
        Dim captured As String = "(the frame capture failed)"

        AddHandler repaint.Tick,
            Sub(sender As Object, e As EventArgs)
                ' invalidating the form would not repaint the canvas: the parent
                ' clips the child window out of its own update region
                Call form.InvalidateCanvas()
            End Sub
        AddHandler autoClose.Tick,
            Sub(sender As Object, e As EventArgs)
                Call autoClose.Stop()
                Call repaint.Stop()

                ' export the last frame, this also proves that the pixel read
                ' back of the swap chain back buffer works
                Dim frame As ImagingBitmap = form.CaptureFrame()

                If frame IsNot Nothing Then
                    Call frame.Save(file, ImageFormats.Png)

                    captured = describeFrame(frame, form.BackgroundColor)
                End If

                Call form.Close()
            End Sub

        Call repaint.Start()
        Call autoClose.Start()

        Try
            Call Application.Run(form)
        Finally
            Call repaint.Dispose()
            Call autoClose.Dispose()
        End Try

        Console.WriteLine("-----------------------------------------------------------------")
        Console.WriteLine($" device             : {form.DeviceDescription}")
        Console.WriteLine($" polygons per frame : {form.PolygonCount.ToString("N0")}")
        Console.WriteLine($" frames presented   : {form.FrameCount}")
        Console.WriteLine($" first frame        : {form.FirstFrameMs} ms  (gpu warm up + scene setup)")
        Console.WriteLine($" average frame      : {form.AverageFrameMs.ToString("F2")} ms  (steady state)")
        Console.WriteLine($" last frame         : {form.LastFrameMs} ms")
        Console.WriteLine($" rendering error    : {If(form.LastError, "(none)")}")
        Console.WriteLine($" captured frame     : {captured}")
        Console.WriteLine($" exported image     : {IO.Path.GetFullPath(file)}")
        Console.WriteLine("-----------------------------------------------------------------")
    End Sub

    ''' <summary>
    ''' build a short textual summary of a captured frame, this proves that the
    ''' polygons are really rasterized by the gpu pipeline
    ''' </summary>
    Private Function describeFrame(image As ImagingBitmap, background As Color) As String
        Dim backgroundArgb As Integer = background.ToArgb()
        Dim samples As Integer = 0
        Dim painted As Integer = 0
        Dim red As Long = 0
        Dim green As Long = 0
        Dim blue As Long = 0

        For y As Integer = 0 To image.Height - 1 Step 5
            For x As Integer = 0 To image.Width - 1 Step 5
                Dim c As Color = image.GetPixel(x, y)

                samples += 1
                red += c.R
                green += c.G
                blue += c.B

                If c.ToArgb() <> backgroundArgb Then
                    painted += 1
                End If
            Next
        Next

        Return String.Format(
            "{0}x{1} px, {2} samples, {3} painted ({4:P1}), mean rgb = ({5:F0}, {6:F0}, {7:F0})",
            image.Width, image.Height, samples, painted,
            If(samples > 0, painted / CDbl(samples), 0),
            If(samples > 0, red / CDbl(samples), 0),
            If(samples > 0, green / CDbl(samples), 0),
            If(samples > 0, blue / CDbl(samples), 0)
        )
    End Function

    Private Sub printHeader(polygonCount As Integer)
        Console.WriteLine("=================================================================")
        Console.WriteLine(" DxCanvas winform control demo - directx gpu accelerated 2d canvas")
        Console.WriteLine($" polygons per frame : {polygonCount.ToString("N0")}")
        Console.WriteLine("=================================================================")
    End Sub

End Module

''' <summary>
''' The demo window of the <see cref="DxCanvas"/> control.
''' </summary>
''' <remarks>
''' The layout is: a status bar on the top, a button bar on the bottom and the
''' directx canvas that takes the rest of the window. The status bar shows the
''' gpu device, the polygon count, the last frame time and the average frame
''' time, which makes the performance of a large polygon scene directly visible
''' when the window is resized.
''' </remarks>
Friend Class DxCanvasDemoForm : Inherits Form

    Private Const STATUS_HEIGHT As Integer = 26
    Private Const BUTTON_HEIGHT As Integer = 42

    Private ReadOnly m_polygonCount As Integer
    Private status As Label
    Private canvas As DxCanvas
    Private redrawButton As Button
    Private saveButton As Button

    ''' <summary>
    ''' the polygon scene, it is rebuilt when the canvas size is changed
    ''' </summary>
    Private scene As DxBenchmark.PolygonDraw() = Nothing
    Private sceneSize As Size = Size.Empty

    Private frames As Integer = 0
    Private totalMs As Long = 0
    ''' <summary>the sum of every frame except the first one</summary>
    Private steadyMs As Long = 0
    Private firstMs As Long = 0
    Private lastMs As Long = 0
    ''' <summary>
    ''' the device description is cached here because the gpu canvas is already
    ''' released when the window is closed
    ''' </summary>
    Private m_deviceDescription As String = Nothing

    Friend Sub New(polygonCount As Integer)
        m_polygonCount = polygonCount

        Call setupWindow()
        Call setupControls()

        AddHandler canvas.Render, AddressOf canvas_Render
    End Sub

    Friend ReadOnly Property PolygonCount As Integer
        Get
            Return m_polygonCount
        End Get
    End Property

    Friend ReadOnly Property FrameCount As Integer
        Get
            Return frames
        End Get
    End Property

    ''' <summary>
    ''' the time of the very first frame, it includes the gpu warm up and the
    ''' scene generation
    ''' </summary>
    Friend ReadOnly Property FirstFrameMs As Long
        Get
            Return firstMs
        End Get
    End Property

    Friend ReadOnly Property LastFrameMs As Long
        Get
            Return lastMs
        End Get
    End Property

    ''' <summary>
    ''' the average time of the frames behind the first one
    ''' </summary>
    Friend ReadOnly Property AverageFrameMs As Double
        Get
            If frames <= 1 Then
                Return 0
            End If

            Return steadyMs / CDbl(frames - 1)
        End Get
    End Property

    Friend ReadOnly Property DeviceDescription As String
        Get
            If m_deviceDescription IsNot Nothing Then
                Return m_deviceDescription
            End If

            Return canvas.DeviceDescription
        End Get
    End Property

    Friend ReadOnly Property LastError As String
        Get
            Return canvas.LastError
        End Get
    End Property

    ''' <summary>
    ''' request the next frame of the directx canvas
    ''' </summary>
    Friend Sub InvalidateCanvas()
        Call canvas.Invalidate()
    End Sub

    ''' <summary>
    ''' export the current canvas content into an image file
    ''' </summary>
    Friend Function SaveCanvas(file As String) As Boolean
        Return canvas.SaveImage(file)
    End Function

    ''' <summary>
    ''' export the current canvas content as a raster image
    ''' </summary>
    Friend Function CaptureFrame() As ImagingBitmap
        Return canvas.CaptureFrame()
    End Function

    ''' <summary>
    ''' the background color of the directx canvas
    ''' </summary>
    Friend ReadOnly Property BackgroundColor As Color
        Get
            Return canvas.BackgroundColor
        End Get
    End Property

    ' /********************************************************************************/
    '  the render handler: the demo scene
    ' /********************************************************************************/

    ''' <summary>
    ''' the scene is generated once per canvas size and then replayed by every
    ''' frame, so that the frame time is the pure drawing cost
    ''' </summary>
    Private Sub canvas_Render(sender As Object, e As DxRenderEventArgs)
        If scene Is Nothing OrElse Not sceneSize.Equals(e.Size) Then
            scene = DxBenchmark.GenerateScene(m_polygonCount, DxCanvasDemo.MIN_RADIUS, DxCanvasDemo.MAX_RADIUS, e.Width, e.Height)
            sceneSize = e.Size
        End If

        Dim watch As Stopwatch = Stopwatch.StartNew()

        Call DxBenchmark.DrawScene(e.Graphics, scene)

        watch.Stop()

        lastMs = watch.ElapsedMilliseconds
        totalMs += lastMs

        If frames = 0 Then
            firstMs = lastMs
        Else
            steadyMs += lastMs
        End If

        frames += 1

        If m_deviceDescription Is Nothing Then
            m_deviceDescription = canvas.DeviceDescription
        End If

        Call updateStatus()
    End Sub

    Private Sub updateStatus()
        status.Text = String.Format(
            "device: {0}   |   polygons: {1}   |   frame: {2} ms   |   average: {3:F2} ms   |   frames: {4}",
            DeviceDescription,
            m_polygonCount.ToString("N0"),
            lastMs,
            AverageFrameMs,
            frames
        )
    End Sub

    ' /********************************************************************************/
    '  the window layout
    ' /********************************************************************************/

    Private Sub setupWindow()
        Dim title As String = String.Format(
            "DxCanvas - directx gpu 2d canvas - {0} polygons",
            m_polygonCount.ToString("N0")
        )

        Me.Text = title
        Me.ClientSize = New Size(1280, 800)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.MinimumSize = New Size(480, 320)
        Me.BackColor = Color.FromArgb(45, 45, 48)
    End Sub

    Private Sub setupControls()
        status = New Label With {
            .Dock = DockStyle.Top,
            .Height = STATUS_HEIGHT,
            .AutoSize = False,
            .TextAlign = ContentAlignment.MiddleLeft,
            .ForeColor = Color.Gainsboro,
            .BackColor = Color.FromArgb(37, 37, 38),
            .Padding = New Padding(8, 0, 0, 0),
            .Text = "initializing the directx canvas ..."
        }

        redrawButton = New Button With {
            .Text = "重绘",
            .Width = 110,
            .Height = 28,
            .FlatStyle = FlatStyle.Flat
        }

        saveButton = New Button With {
            .Text = "保存图片",
            .Width = 110,
            .Height = 28,
            .FlatStyle = FlatStyle.Flat
        }

        Dim buttonBar As New FlowLayoutPanel With {
            .Dock = DockStyle.Bottom,
            .Height = BUTTON_HEIGHT,
            .Padding = New Padding(8, 6, 8, 0),
            .BackColor = Color.FromArgb(37, 37, 38)
        }

        Call buttonBar.Controls.Add(redrawButton)
        Call buttonBar.Controls.Add(saveButton)

        canvas = New DxCanvas With {
            .Dock = DockStyle.Fill,
            .AutoClear = True,
            .BackgroundColor = Color.FromArgb(22, 22, 26),
            .VSync = True
        }

        ' the fill canvas is added first so that it is docked last and takes
        ' the area that is left by the two bars
        Call Me.Controls.Add(canvas)
        Call Me.Controls.Add(status)
        Call Me.Controls.Add(buttonBar)

        AddHandler redrawButton.Click,
            Sub(sender As Object, e As EventArgs)
                Call canvas.Invalidate()
            End Sub
        AddHandler saveButton.Click, AddressOf saveClick
    End Sub

    Private Sub saveClick(sender As Object, e As EventArgs)
        Dim file As String = $"./dxcanvas_{DateTime.Now:yyyyMMdd_HHmmss}.png"

        Try
            If canvas.SaveImage(file) Then
                Call MessageBox.Show(
                    Me,
                    "the canvas is saved to:" & vbCrLf & IO.Path.GetFullPath(file),
                    "DxCanvas",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                )
            Else
                Call MessageBox.Show(Me, canvas.LastError, "DxCanvas", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        Catch ex As Exception
            Call MessageBox.Show(Me, ex.Message, "DxCanvas", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

End Class
