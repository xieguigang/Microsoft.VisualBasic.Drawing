Imports System.Drawing
Imports Microsoft.VisualBasic.Drawing
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Math2D

Module Program
    Sub Main(args As String())
        Call testDrawing()
    End Sub

    Private Sub testDriver()
        SkiaDriver.Register()
    End Sub

    Private Sub testDrawing()
        Dim draw = Function(g As SkiaGraphics) As SkiaGraphics
                       g.Clear(Color.White)
                       g.DrawString("123456", FontFace.SegoeUI, 20, "#ff000f".TranslateColor(), 300, 900)
                       g.DrawPath(New Polygon2D({30, 69, 888, 777}, {691, 258, 666, 1}), Color.Green, 5, Color.Blue, dash:={5, 10})
                       g.DrawLine(50, 50, 300, 600, Color.Red, 5)

                       Return g
                   End Function

        draw(New Graphics(1000, 1000, "#ffffff")).Save("./test.png")
        draw(New SvgGraphics(1000, 1000)).Save("./test.svg")
        draw(New PdfGraphics(1000, 1000)).Save("./test.pdf")
    End Sub
End Module
