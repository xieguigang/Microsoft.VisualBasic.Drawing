Imports System.Drawing
Imports Microsoft.VisualBasic.Drawing
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Math2D
Imports SkiaSharp

Module Program
    Sub Main(args As String())
        ' Call simpleNativeDrawTest()
        Call testDrawing()
    End Sub

    Sub simpleNativeDrawTest()
        Dim width = 500
        Dim height = 100
        Dim bitmap As New SKBitmap(width, height)
        Dim text = "Hello World"

        Using canvas As New SKCanvas(bitmap)
            canvas.Clear(SKColors.White)

            Dim paint As New SKPaint With
        {
            .Color = SKColors.Black, ' 文本颜色
            .TextSize = 48,          ' 文本大小
            .IsAntialias = True ' 启用抗锯齿
        }
            Dim textBounds = New SKRect()
            paint.MeasureText(text, textBounds)

            Dim x = (width - textBounds.Width) / 2
            Dim y = (height - textBounds.Height) / 2 + textBounds.Height

            canvas.DrawText(text, x, y, paint)
        End Using

        Using encoded = bitmap.Encode(SKEncodedImageFormat.Bmp, 100)
            Using stream = System.IO.File.OpenWrite("hello_world.png")
                encoded.SaveTo(stream)
            End Using
        End Using
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

        Dim bmp As New Graphics(10, 10, "#ffffff")

        bmp.Clear(Color.Red)
        bmp.Save("./test.bmp", ImageFormats.Bmp)

        draw(New Graphics(1000, 1000, "#ffffff")).Save("./test.png")
        draw(New SvgGraphics(1000, 1000)).Save("./test.svg")
        draw(New PdfGraphics(1000, 1000)).Save("./test.pdf")
    End Sub
End Module
