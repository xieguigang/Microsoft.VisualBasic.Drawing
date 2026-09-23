Imports System.Drawing
Imports System.Windows.Forms
Imports Microsoft.VisualBasic.Drawing.DirectX
Imports Microsoft.VisualBasic.Imaging.Drawing3D.Models

''' <summary>
''' Reproduces the reported failure: the canvas is created and renders, then the
''' control has to build a new swap chain for the same window (after its window
''' handle was recreated or after the canvas was released), and
''' IDXGIFactory2::CreateSwapChainForHwnd fails with E_ACCESSDENIED.
''' </summary>
Module Program

    Private ReadOnly watch As New System.Diagnostics.Stopwatch()
    Private creations As Integer = 0
    Private frames As Integer = 0
    Private lastError As String = Nothing
    Private deviceLost As Boolean = False
    Private wasCreated As Boolean = False

    <STAThread>
    Sub Main()
        Call Application.EnableVisualStyles()
        Call Application.SetCompatibleTextRenderingDefault(False)
        Call Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException)
        AddHandler Application.ThreadException, Sub(s As Object, e As Threading.ThreadExceptionEventArgs)
                                                    Console.WriteLine("!! thread exception: " & e.Exception.Message)
                                                End Sub

        Dim form As New Form() With {.Text = "probe2", .StartPosition = FormStartPosition.CenterScreen}
        Dim canvas As New DxScene3DCanvas() With {.Dock = DockStyle.Fill}

        form.ClientSize = New Size(640, 480)
        form.Controls.Add(canvas)

        AddHandler canvas.Render, Sub(s, e) frames += 1
        AddHandler canvas.DeviceCreated, Sub(s, e)
                                             creations += 1
                                             Console.WriteLine($"   [{watch.ElapsedMilliseconds} ms] canvas created #{creations}: {canvas.DeviceDescription}")
                                         End Sub

        Dim cube As New Cube(1)
        AddHandler form.Load, Sub(s, e) Call canvas.LoadSurfaces(cube.faces)

        Dim steps As New Queue(Of Action)()
        Dim pump As New Timer() With {.Interval = 700}

        ' every error transition of the canvas is logged, the intermediate
        ' failures are overwritten by the retry otherwise
        Dim monitor As New Timer() With {.Interval = 20}

        AddHandler monitor.Tick, Sub(s, e)
                                     If canvas.LastError IsNot Nothing AndAlso canvas.LastError <> lastError Then
                                         lastError = canvas.LastError
                                         Console.WriteLine($"   [{watch.ElapsedMilliseconds} ms] >> error: {lastError}")
                                     End If

                                     If canvas.IsDeviceLost <> deviceLost Then
                                         deviceLost = canvas.IsDeviceLost
                                         Console.WriteLine($"   [{watch.ElapsedMilliseconds} ms] >> device lost = {deviceLost}  frames={frames}")
                                     End If

                                     If canvas.IsCanvasCreated <> wasCreated Then
                                         wasCreated = canvas.IsCanvasCreated
                                         Console.WriteLine($"   [{watch.ElapsedMilliseconds} ms] >> canvas created = {wasCreated}  frames={frames}")
                                     End If
                                 End Sub

        AddHandler pump.Tick, Sub(s, e)
                                  Dim action As Action = Nothing

                                  If steps.Count = 0 Then
                                      pump.Stop()
                                      Console.WriteLine()
                                      Console.WriteLine("result:")
                                      Console.WriteLine("   canvas created : " & canvas.IsCanvasCreated)
                                      Console.WriteLine("   last error     : " & If(canvas.LastError, "(none)"))
                                      form.Close()
                                      Return
                                  End If

                                  action = steps.Dequeue()
                                  Console.WriteLine()
                                  Call action()
                                  Call Application.DoEvents()

                                  If canvas.LastError IsNot Nothing AndAlso canvas.LastError <> lastError Then
                                      lastError = canvas.LastError
                                      Console.WriteLine("   >> canvas error: " & lastError)
                                  End If
                              End Sub

        ' step 1: the canvas has to be created and render while the window is visible
        steps.Enqueue(Sub()
                          Console.WriteLine("step 1: the canvas renders on the visible window")
                          Call canvas.RequestRender()
                          Call Application.DoEvents()
                          Console.WriteLine("   created=" & canvas.IsCanvasCreated & "  frames=" & frames)
                      End Sub)

        ' step 2: resize the window, this goes through ResizeBuffers
        steps.Enqueue(Sub()
                          Console.WriteLine("step 2: the window is resized (ResizeBuffers)")
                          form.ClientSize = New Size(800, 600)
                          Call Application.DoEvents()
                          Call canvas.RequestRender()
                          Call Application.DoEvents()
                          Console.WriteLine("   created=" & canvas.IsCanvasCreated & "  frames=" & frames)
                      End Sub)

        ' step 3: force the control to recreate its window handle, the swap chain
        ' is released here and a new one has to be created for the new handle
        steps.Enqueue(Sub()
                          Console.WriteLine("step 3: the window handle of the control is recreated")
                          Dim before As IntPtr = canvas.Handle

                          canvas.RightToLeft = RightToLeft.Yes
                          canvas.RightToLeft = RightToLeft.No
                          Call Application.DoEvents()

                          Console.WriteLine("   handle changed = " & (before <> canvas.Handle))
                          Call canvas.RequestRender()
                          Call Application.DoEvents()
                          Console.WriteLine("   created=" & canvas.IsCanvasCreated & "  frames=" & frames)
                      End Sub)

        ' step 4: a larger model is loaded, the way the viewer does it
        steps.Enqueue(Sub()
                          Console.WriteLine("step 4: a dense model is loaded")

                          Dim faces As New List(Of Microsoft.VisualBasic.Imaging.Drawing3D.Surface)()
                          For i As Integer = 0 To 63
                              For j As Integer = 0 To 63
                                  Dim a As Single = i * 0.1F
                                  Dim b As Single = j * 0.1F

                                  faces.Add(New Microsoft.VisualBasic.Imaging.Drawing3D.Surface With {
                                      .vertices = {
                                          New Microsoft.VisualBasic.Imaging.Drawing3D.Point3D(a, b, 0),
                                          New Microsoft.VisualBasic.Imaging.Drawing3D.Point3D(a + 0.1F, b, 0),
                                          New Microsoft.VisualBasic.Imaging.Drawing3D.Point3D(a, b + 0.1F, 0.2F)
                                      },
                                      .brush = New Microsoft.VisualBasic.Imaging.SolidBrush(Color.SteelBlue)
                                  })
                              Next
                          Next

                          Call canvas.LoadSurfaces(faces)
                          Call Application.DoEvents()
                          Call canvas.RequestRender()
                          Call Application.DoEvents()
                          Console.WriteLine("   created=" & canvas.IsCanvasCreated & "  frames=" & frames & "  faces=" & canvas.SurfaceCount)
                      End Sub)

        ' step 5: the canvas has to be alive at the end, a few seconds are waited
        ' here to see whether the rebuild eventually recovers
        steps.Enqueue(Sub()
                          Console.WriteLine("step 5: waiting for the retry to recover")
                          Call canvas.RequestRender()
                          Call Application.DoEvents()
                          Console.WriteLine("   created=" & canvas.IsCanvasCreated & "  frames=" & frames)
                      End Sub)

        steps.Enqueue(Sub()
                          Dim until As Long = watch.ElapsedMilliseconds + 3000

                          Do While watch.ElapsedMilliseconds < until
                              Call Application.DoEvents()
                              Threading.Thread.Sleep(10)
                          Loop

                          Console.WriteLine("step 6: after a three second wait")
                          Call canvas.RequestRender()
                          Call Application.DoEvents()
                          Console.WriteLine("   created=" & canvas.IsCanvasCreated & "  frames=" & frames)
                      End Sub)

        AddHandler form.Shown, Sub(s, e)
                                   watch.Start()
                                   monitor.Start()
                                   pump.Start()
                               End Sub

        Call Application.Run(form)

        If canvas.IsCanvasCreated AndAlso canvas.LastError Is Nothing Then
            Console.WriteLine()
            Console.WriteLine("PASS: the swap chain survived the rebuilds")
        Else
            Console.WriteLine()
            Console.WriteLine("FAIL: the swap chain could not be rebuilt")
            Environment.ExitCode = 1
        End If
    End Sub
End Module
