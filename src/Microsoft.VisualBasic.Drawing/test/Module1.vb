Imports Microsoft.VisualBasic.Drawing

Module ImagePreview

    Sub Main22222(args As String())
        If args.Length = 0 Then
            Console.WriteLine("Usage: ImagePreview <image_path> [width]")
            Return
        End If

        Dim imagePath As String = args(0)
        Dim terminalWidth As Integer = If(args.Length > 1, Integer.Parse(args(1)), Console.WindowWidth - 1)
        Dim preview As String = GenerateImagePreview(imagePath, terminalWidth)
        Console.Write(preview)
    End Sub

End Module