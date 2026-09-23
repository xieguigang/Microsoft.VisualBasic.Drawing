Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms
Imports Microsoft.VisualBasic.Drawing.DirectX
Imports Microsoft.VisualBasic.Drawing.DirectX.Scene3D
Imports Microsoft.VisualBasic.Imaging.Drawing3D.Models
Imports ImageFormats = Microsoft.VisualBasic.Imaging.ImageFormats

''' <summary>
''' A throw away window level probe of the DxScene3DCanvas control: it hosts the
''' control in a real winforms window the same way the model viewer does, forces
''' a paint while the window is still hidden (the situation that used to break
''' the canvas creation permanently) and then verifies that the canvas is
''' created, that the scene renders and that a frame can be exported.
''' </summary>
Module Program

    Private frames As Integer = 0
    Private deviceCreatedMs As Long = -1
    Private failures As Integer = 0
    Private ReadOnly watch As New System.Diagnostics.Stopwatch()

    <STAThread>
    Sub Main()
        Call Application.EnableVisualStyles()
        Call Application.SetCompatibleTextRenderingDefault(False)
        Call Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException)

        ' an exception that is thrown from inside a paint handler is swallowed
        ' by winforms unless it is logged here
        AddHandler Application.ThreadException, Sub(s As Object, e As Threading.ThreadExceptionEventArgs)
                                                    Console.WriteLine("!! thread exception: " & e.Exception.ToString())
                                                End Sub

        Dim form As New Form() With {
            .Text = "DxCanvas probe",
            .StartPosition = FormStartPosition.CenterScreen
        }
        Dim canvas As New DxScene3DCanvas() With {
            .Dock = DockStyle.Fill,
            .ShowDebugOverlay = True
        }

        form.ClientSize = New Size(640, 480)
        form.Controls.Add(canvas)

        AddHandler canvas.Render, Sub(s, e) frames += 1
        AddHandler canvas.DeviceCreated, Sub(s, e)
                                             deviceCreatedMs = watch.ElapsedMilliseconds
                                             Console.WriteLine($"   [event] DeviceCreated after {deviceCreatedMs} ms : {canvas.DeviceDescription}")
                                         End Sub

        watch.Start()

        ' ------------------------------------------------------------------
        ' reproduce the hostile first paint: a paint request while the hosting
        ' window is still hidden. dxgi rejects a flip model swap chain on a
        ' window that is not visible yet.
        ' ------------------------------------------------------------------
        form.CreateControl()

        Try
            Using snapshot As New Bitmap(form.ClientSize.Width, form.ClientSize.Height)
                Call form.DrawToBitmap(snapshot, New Rectangle(New Point, form.ClientSize))
            End Using

            Console.WriteLine("hidden first paint ->")
        Catch ex As Exception
            Console.WriteLine("hidden first paint could not be forced: " & ex.Message)
        End Try

        Console.WriteLine("   canvas created   : " & canvas.IsCanvasCreated)
        Console.WriteLine("   last error       : " & If(canvas.LastError, "(none)"))

        ' ------------------------------------------------------------------
        ' the model viewer flow: load the geometry and request a paint from
        ' inside the Load handler, that is before the window becomes visible
        ' ------------------------------------------------------------------
        Dim cube As New Cube(1)

        AddHandler form.Load, Sub(s, e)
                                  Call canvas.LoadSurfaces(cube.faces)
                                  Call canvas.RequestRender()
                              End Sub

        ' ------------------------------------------------------------------
        ' verify after the window is shown, the retry has to recover the canvas
        ' ------------------------------------------------------------------
        Dim check As New Timer() With {.Interval = 1500}

        AddHandler check.Tick, Sub(s, e)
                                   check.Stop()
                                   Call Verify(canvas)
                                   form.Close()
                               End Sub

        Dim trace As New Timer() With {.Interval = 400}
        AddHandler trace.Tick, Sub(s, e)
                                   Console.WriteLine($"   [trace] {watch.ElapsedMilliseconds} ms  frames={frames}  canvas={canvas.IsCanvasCreated}  err={If(canvas.LastError, "-")}")
                               End Sub

        AddHandler form.Shown, Sub(s, e)
                                   check.Start()
                                   trace.Start()
                               End Sub

        Call Application.Run(form)

        Console.WriteLine()

        If failures = 0 Then
            Console.WriteLine("ALL CHECKS PASSED")
        Else
            Console.WriteLine($"{failures} CHECK(S) FAILED")
            Environment.ExitCode = 1
        End If
    End Sub

    Private Sub Check(condition As Boolean, name As String)
        If condition Then
            Console.WriteLine("  ok   " & name)
        Else
            failures += 1
            Console.WriteLine("  FAIL " & name)
        End If
    End Sub

    Private Sub Verify(canvas As DxScene3DCanvas)
        Console.WriteLine()
        Console.WriteLine("after the window was shown ->")

        Call Check(canvas.IsCanvasCreated, "the directx canvas has been created")
        Call Check(canvas.DeviceDescription <> "the directx canvas is not created yet",
                   $"the device description is available ({canvas.DeviceDescription})")
        Call Check(String.IsNullOrEmpty(canvas.LastError), "no canvas error: " & If(canvas.LastError, "(none)"))
        Call Check(String.IsNullOrEmpty(canvas.LastSceneError), "no scene error: " & If(canvas.LastSceneError, "(none)"))
        Call Check(canvas.HasScene, "the scene holds the loaded geometry")
        Call Check(canvas.SurfaceCount = 6, $"the loaded cube contributes 6 faces ({canvas.SurfaceCount})")
        Call Check(frames > 0, $"the scene has been rendered ({frames} frames)")
        Call Check(deviceCreatedMs >= 0, $"the device became available after {deviceCreatedMs} ms")

        ' every presentation mode must draw a frame without an error
        For Each mode As SceneRenderMode In New SceneRenderMode() {
                SceneRenderMode.Surface, SceneRenderMode.Mesh, SceneRenderMode.PointCloud}

            Dim before As Integer = frames

            canvas.RenderMode = mode
            Call canvas.RequestRender()
            Call Application.DoEvents()

            Call Check(frames > before, $"the {mode} mode rendered a frame ({frames})")
            Call Check(String.IsNullOrEmpty(canvas.LastSceneError), $"the {mode} mode has no scene error: " & If(canvas.LastSceneError, "(none)"))
        Next

        Call Check(canvas.LastError Is Nothing, "no canvas error after the mode changes")

        ' the frame export has to travel through the flip model back buffer
        Dim exportPath As String = Path.Combine(Path.GetTempPath(), "dxcanvas_probe.png")

        If File.Exists(exportPath) Then
            Call File.Delete(exportPath)
        End If

        canvas.RenderMode = SceneRenderMode.Surface
        Dim saved As Boolean = canvas.SaveSnapshot(exportPath, ImageFormats.Png)

        Call Check(saved, "the frame export reported success")
        Call Check(File.Exists(exportPath), "the frame export wrote a file")
        Call Check(File.Exists(exportPath) AndAlso New FileInfo(exportPath).Length > 1024, "the exported frame is not empty")

        If File.Exists(exportPath) Then
            Console.WriteLine($"   exported frame: {exportPath} ({New FileInfo(exportPath).Length} bytes)")
        End If

        ' the captured frame has to contain the model itself, not only the
        ' background color of the canvas
        Dim frame = canvas.Snapshot()

        Call Check(frame IsNot Nothing, "the frame capture returned an image")

        If frame IsNot Nothing Then
            Dim drawn As Integer = 0

            For y As Integer = 0 To frame.Height - 1
                For x As Integer = 0 To frame.Width - 1
                    Dim pixel As Color = frame.GetPixel(x, y)

                    If pixel.R < 250 OrElse pixel.G < 250 OrElse pixel.B < 250 Then
                        drawn += 1
                    End If
                Next
            Next

            Call Check(drawn > 500, $"the captured frame contains the drawn model ({drawn} pixels of {frame.Width * frame.Height})")
        End If

        ' the zoom interaction must work on the live canvas
        Dim beforeZoom As Single = canvas.Controller.Camera.ViewDistance
        Call canvas.ZoomIn()
        Call Check(canvas.Controller.Camera.ViewDistance < beforeZoom, "zooming works on the live canvas")
    End Sub
End Module
