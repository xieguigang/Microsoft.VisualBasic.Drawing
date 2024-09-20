Imports Microsoft.VisualBasic.Drawing
Imports Microsoft.VisualBasic.Imaging
Imports Font = System.Drawing.Font

Module Program
    Sub Main(args As String())
        Dim canvas As New Graphics(1000, 1000, "#ffffff")

        canvas.DrawString("123456", New Font(FontFace.SegoeUI, 20), "#ff000f".TranslateColor, 30, 600)
        canvas.Save("./test.png")
    End Sub
End Module
