Imports System.Windows.Forms

''' <summary>
''' The entry point of the boids demo.
''' </summary>
''' <remarks>
''' The original demo was a two dimensional gdi+ rendered flock started through
''' the winforms application framework (<c>My.MyApplication</c>). The executable
''' now has to choose between two windows, so it owns its <see cref="Main"/>
''' itself and the application framework entry point is switched off
''' (<c>MySubMain=false</c> in <c>Application.myapp</c>).
''' </remarks>
Public Module Program

    ''' <summary>
    ''' start the demo window that the arguments request
    ''' </summary>
    ''' <param name="args">the command line arguments, see <see cref="BoidsOptions"/></param>
    Public Sub Main(args As String())
        Dim options As BoidsOptions = BoidsOptions.Parse(args)

        If options.ShowHelp Then
            Call Console.WriteLine(BoidsOptions.HelpText())
            Return
        End If

        If options.SelfTest Then
            Environment.ExitCode = SelfTest3D.Run()
            Return
        End If

        ' the two dimensional window has always been started dpi unaware and has
        ' to keep its pixel exact appearance, while the directx canvas is only
        ' crisp when the system scales the window per monitor
        If options.Mode = BoidsMode.TwoDimensional Then
            Call Application.SetHighDpiMode(HighDpiMode.DpiUnaware)
        Else
            Call Application.SetHighDpiMode(HighDpiMode.PerMonitorV2)
        End If

        Call Application.EnableVisualStyles()
        Call Application.SetCompatibleTextRenderingDefault(False)

        If options.Mode = BoidsMode.TwoDimensional Then
            Call Application.Run(New FormRender())
        Else
            Call Application.Run(New FormRender3D With {.Settings = options})
        End If
    End Sub

End Module
