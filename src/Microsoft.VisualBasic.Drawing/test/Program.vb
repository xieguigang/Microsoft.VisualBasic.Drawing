Imports Microsoft.VisualBasic.Drawing
Imports Microsoft.VisualBasic.Imaging

Module Program
    Sub Main(args As String())
        Dim canvas As SkiaGraphics = New Graphics(1000, 1000, "#ffffff")

        canvas.DrawString("123456", FontFace.SegoeUI, 20, ArgbColor.TranslateColor("#ff000f"), 30, 600)
        canvas.Save("./test.png")

        canvas = New SvgGraphics(1000, 1000)
        canvas.Clear("#ffffff")

        canvas.DrawString("123456", FontFace.SegoeUI, 20, ArgbColor.TranslateColor("#ff000f"), 30, 600)
        canvas.Save("./test.svg")

        canvas = New PdfGraphics(1000, 1000)
        canvas.Clear("#ffffff")

        canvas.DrawString("123456", FontFace.SegoeUI, 20, ArgbColor.TranslateColor("#ff000f"), 30, 600)
        canvas.Save("./test.pdf")
    End Sub
End Module
