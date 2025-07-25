Imports Microsoft.VisualBasic.CommandLine
Imports Microsoft.VisualBasic.Drawing

Module Program

    Public Function Main() As Integer
        Return GetType(Program).RunCLI(App.CommandLine, executeFile:=AddressOf PreviewFile, executeEmpty:=AddressOf Help)
    End Function

    Private Function Help() As Integer
        Call Console.WriteLine("Usage: preview <image-file> [--width <display_width, should less than the console width>] [--true-color|--256-color]")
        Return 0
    End Function

    Private Function PreviewFile(file As String, args As CommandLine) As Integer
        Dim terminalWidth As Integer = args("--width") Or (Console.WindowWidth - 1)
        Dim trueColor As Boolean = args("--true-color")
        Dim c256Color As Boolean = args("--256-color")

        If trueColor Then
            ANSI.useTrueColor = True
        End If
        If c256Color Then
            ANSI.useTrueColor = False
        End If

        Dim preview As String = ANSI.GenerateImagePreview(file, terminalWidth)
        Call Console.Write(preview)
        Return 0
    End Function
End Module
