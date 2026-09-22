Imports System.Diagnostics
Imports System.Drawing
Imports System.Windows.Forms
Imports Microsoft.VisualBasic.Drawing.DirectX
Imports Microsoft.VisualBasic.Imaging

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
    ''' open the demo window and close it automatically after the given amount of
    ''' seconds, then print the frame statistics of that run.
    ''' </summary>
    ''' <remarks>
    ''' this is the headless friendly mode that is used to verify the control
    ''' without a user interaction.
    ''' </remarks>
    Sub RunSmoke(Optional polygonCount As Integer = 0, Optional seconds As Integer = 3)
        If polygonCount <= 0 Then
            polygonCount = DEFAULT_POLYGON_COUNT
        End If

        Call Application.EnableVisualStyles()
        Call printHeader(polygonCount)

        Dim form As New DxCanvasDemoForm(polygonCount)
        Dim autoClose As New Timer With {.Interval = seconds * 1000}

        AddHandler autoClose.Tick,
            Sub(sender As Object, e As EventArgs)
                Call autoClose.Stop()
                Call form.Close()
            End Sub

        Call autoClose.Start()

        Try
            Call Application.Run(form)
        Finally
            Call autoClose.Dispose()
        End Try

        Console.WriteLine("-----------------------------------------------------------------")
        Console.WriteLine($" device             : {form.DeviceDescription}")
        Console.WriteLine($" polygons per frame : {form.PolygonCount.ToString("N0")}")
        Console.WriteLine($" frames presented   : {form.FrameCount}")
        Console.WriteLine($" last frame         : {form.LastFrameMs} ms")
        Console.WriteLine($" average frame      : {form.AverageFrameMs.ToString("F2")} ms")
        Console.WriteLine($" rendering error    : {If(form.LastError, "(none)")}")
        Console.WriteLine("-----------------------------------------------------------------")
    End Sub

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
''' time, which makes the performance difference of a large polygon scene
''' directly visible when the window is resized.
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
    Private lastMs As Long = 0

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

    Friend ReadOnly Property LastFrameMs As Long
        Get
            Return lastMs
        End Get
    End Property

    Friend ReadOnly Property AverageFrameMs As Double
        Get
            If frames <= 0 Then
                Return 0
            End If

            Return totalMs / CDbl(frames)
        End Get
    End Property

    Friend ReadOnly Property DeviceDescription As String
        Get
            Return canvas.DeviceDescription
        End Get
    End Property

    Friend ReadOnly Property LastError As String
        Get
            Return canvas.LastError
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
        frames += 1
        totalMs += lastMs

        Call updateStatus()
    End Sub

    Private Sub updateStatus()
        status.Text = String.Format(
            "device: {0}   |   polygons: {1}   |   frame: {2} ms   |   average: {3:F2} ms   |   frames: {4}",
            canvas.DeviceDescription,
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
