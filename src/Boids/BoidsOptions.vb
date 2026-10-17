Imports std = System.Math

''' <summary>
''' The presentation mode of the boids demo.
''' </summary>
Public Enum BoidsMode

    ''' <summary>
    ''' the original gdi+ rendered two dimensional flock
    ''' </summary>
    TwoDimensional

    ''' <summary>
    ''' the directx rendered three dimensional flock (the default)
    ''' </summary>
    ThreeDimensional
End Enum

''' <summary>
''' The command line options of the boids demo.
''' </summary>
''' <remarks>
''' The executable starts the three dimensional directx window by default and
''' falls back to the two dimensional gdi+ window with <c>--2d</c>, so the
''' original demo stays available without a second build.
''' </remarks>
Public Class BoidsOptions

    ''' <summary>
    ''' The boids per cubic simulation unit that the three dimensional field is
    ''' sized for.
    ''' </summary>
    ''' <remarks>
    ''' The neighborhood of a boid is one cell of the spatial grid plus its
    ''' surrounding shell, so the number of the candidate neighbors is the
    ''' density times the volume of that shell. The two dimensional demo runs at
    ''' roughly 0.05 boids per square unit and sees about a hundred candidates per
    ''' boid; at 1.1E-3 boids per cubic unit the three dimensional grid (which
    ''' probes twenty seven cells instead of nine) sees the very same number, so
    ''' the per boid cost of the physics stays comparable.
    ''' </remarks>
    Public Const TargetDensity As Double = 0.0011

    ''' <summary>
    ''' the size of the simulated cube when the caller does not provide one
    ''' </summary>
    Public Const MinimumDomainSize As Double = 100

    ''' <summary>
    ''' which demo window is started
    ''' </summary>
    Public Property Mode As BoidsMode = BoidsMode.ThreeDimensional

    ''' <summary>
    ''' the number of the boids of the flock
    ''' </summary>
    Public Property BoidCount As Integer = 120000

    ''' <summary>
    ''' the edge length of the cubic simulation field, zero asks for the size
    ''' that <see cref="ResolveDomainSize"/> derives from the boid count
    ''' </summary>
    Public Property DomainSize As Double = 0

    ''' <summary>
    ''' the edge length in pixels of one boid
    ''' </summary>
    Public Property PointSize As Integer = 2

    ''' <summary>
    ''' the sample count of the multi sample anti aliasing of the direct3d back
    ''' end, one disables it
    ''' </summary>
    Public Property MultisampleCount As Integer = 1

    ''' <summary>
    ''' run the numeric self test instead of opening a window
    ''' </summary>
    Public Property SelfTest As Boolean = False

    ''' <summary>
    ''' the caller asked for the usage text
    ''' </summary>
    Public Property ShowHelp As Boolean = False

    ''' <summary>
    ''' the edge length of the simulated cube: the configured one, or the size
    ''' that keeps the neighbor count of a boid at the level of the two
    ''' dimensional demo
    ''' </summary>
    Public Function ResolveDomainSize() As Double
        If DomainSize > 0 Then
            Return DomainSize
        End If

        Dim count As Integer = std.Max(1, Me.BoidCount)
        Dim edge As Double = std.Pow(count / TargetDensity, 1.0 / 3.0)

        Return std.Max(MinimumDomainSize, std.Ceiling(edge))
    End Function

    ''' <summary>
    ''' parse the command line arguments of the executable
    ''' </summary>
    ''' <param name="args">the raw arguments of the entry point</param>
    ''' <returns>
    ''' the parsed options, an unknown argument is reported on the standard error
    ''' but does not abort the start
    ''' </returns>
    Public Shared Function Parse(args As String()) As BoidsOptions
        Dim options As New BoidsOptions()

        If args Is Nothing Then
            Return options
        End If

        Dim i As Integer = 0

        While i < args.Length
            Dim arg As String = args(i)
            Dim name As String = If(arg, "").ToLowerInvariant()

            Select Case name
                Case "--2d", "-2d", "--2dim"
                    options.Mode = BoidsMode.TwoDimensional
                Case "--3d", "-3d", "--3dim"
                    options.Mode = BoidsMode.ThreeDimensional
                Case "--selftest"
                    options.SelfTest = True
                Case "--help", "-h", "-?", "/?"
                    options.ShowHelp = True
                Case "--boids", "--count"
                    options.BoidCount = TakeInt(args, i, options.BoidCount, 1, 5000000)
                Case "--domain"
                    options.DomainSize = TakeDouble(args, i, options.DomainSize, MinimumDomainSize)
                Case "--point", "--pointsize"
                    options.PointSize = TakeInt(args, i, options.PointSize, 1, 16)
                Case "--msaa"
                    options.MultisampleCount = TakeInt(args, i, options.MultisampleCount, 1, 8)
                Case Else
                    Call Console.Error.WriteLine($"boids: unknown argument '{arg}', it is ignored.")
                    Call Console.Error.WriteLine("       run with --help for the list of the supported arguments.")
            End Select

            i += 1
        End While

        Return options
    End Function

    ''' <summary>
    ''' the usage text of the executable
    ''' </summary>
    Public Shared Function HelpText() As String
        Dim text As New Text.StringBuilder()

        Call text.AppendLine("Boids Algorithm - flocking simulation")
        Call text.AppendLine()
        Call text.AppendLine("usage: Boids [--2d | --3d] [options]")
        Call text.AppendLine()
        Call text.AppendLine("  --2d              start the original two dimensional gdi+ window")
        Call text.AppendLine("  --3d              start the three dimensional directx window (default)")
        Call text.AppendLine("  --boids <n>       number of the simulated boids (default 120000)")
        Call text.AppendLine("  --domain <size>   edge length of the cubic field (default: derived from the count)")
        Call text.AppendLine("  --point <n>       edge length in pixels of one boid (default 2)")
        Call text.AppendLine("  --msaa <n>        multi sample anti aliasing of the direct3d pipeline (default 1)")
        Call text.AppendLine("  --selftest        run the numeric self test and exit")
        Call text.AppendLine("  --help            show this text")
        Call text.AppendLine()
        Call text.AppendLine("keys of the three dimensional window:")
        Call text.AppendLine("  left drag orbit, right drag pan, wheel zoom")
        Call text.AppendLine("  R reset view, F fit view, G ground grid, D debug text, S snapshot")
        Call text.AppendLine("  F5 recreate the flock")

        Return text.ToString()
    End Function

    ''' <summary>
    ''' read the integer value that follows the argument at <paramref name="i"/>
    ''' and advance the cursor past it
    ''' </summary>
    Private Shared Function TakeInt(args As String(), ByRef i As Integer, fallback As Integer,
                                    minValue As Integer, maxValue As Integer) As Integer
        Dim value As Integer

        If i + 1 < args.Length AndAlso Integer.TryParse(args(i + 1), value) Then
            i += 1
            Return std.Max(minValue, std.Min(maxValue, value))
        End If

        Call Console.Error.WriteLine($"boids: '{args(i)}' needs an integer value, {fallback} is kept.")

        Return fallback
    End Function

    ''' <summary>
    ''' read the floating point value that follows the argument at
    ''' <paramref name="i"/> and advance the cursor past it
    ''' </summary>
    Private Shared Function TakeDouble(args As String(), ByRef i As Integer, fallback As Double,
                                       minValue As Double) As Double
        Dim value As Double

        If i + 1 < args.Length AndAlso Double.TryParse(args(i + 1), value) Then
            i += 1
            Return std.Max(minValue, value)
        End If

        Call Console.Error.WriteLine($"boids: '{args(i)}' needs a numeric value, {fallback} is kept.")

        Return fallback
    End Function
End Class
