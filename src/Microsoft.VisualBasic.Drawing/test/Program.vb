Imports System.Drawing
Imports Microsoft.VisualBasic.Drawing
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Math2D
Imports SkiaSharp

Module Program
    Sub Main(args As String())
        Call Main22222({"G:\mzkit\Rscript\Library\mzkit_app\test\msn_peaks\umap.png"})
        Call Pause()

        Call resizetest()

        ' Call simpleNativeDrawTest()
        Call RunGradient()
        Call testDrawing()
    End Sub

    Sub resizetest()
        Dim bmp As New Graphics(1000, 1000, "#ffffff")
        Call draw(bmp)
        Dim img As SkiaImage = bmp.ImageResource
        Dim large = img.Resize(3500, 2000)

        Using s = "./raw.png".Open(IO.FileMode.OpenOrCreate)
            Call img.Save(s, ImageFormats.Png)
        End Using
        Using s = "./raw_scaled.png".Open(IO.FileMode.OpenOrCreate)
            Call large.Save(s, ImageFormats.Png)
        End Using

        Dim large_canvas As New Graphics(3500, 2000, "#ffffff")
        Call large_canvas.DrawImage(img, 0, 0, large_canvas.Width, large_canvas.Height)

        Using s = "./raw_skia.png".Open(IO.FileMode.OpenOrCreate)
            Call large_canvas.ImageResource.Save(s, ImageFormats.Png)
        End Using


        large_canvas = New Graphics(2500, 5000, "#ffffff", dpi:=300)
        img = SkiaImage.FromFile("E:\Microsoft.VisualBasic.Drawing\demo\debug.png")

        Call large_canvas.DrawImage(img, 0, 0, large_canvas.Width, large_canvas.Height)

        Using s = "./raw_skia2.png".Open(IO.FileMode.OpenOrCreate)
            Call large_canvas.ImageResource.Save(s, ImageFormats.Png)
        End Using

        Pause()
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
            .Color = SKColors.Black, ' �ı���ɫ
            .TextSize = 48,          ' �ı���С
            .IsAntialias = True ' ���ÿ����
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

    Private Function draw(g As SkiaGraphics) As SkiaGraphics
        g.Clear(Color.White)
        g.DrawString("123456", FontFace.SegoeUI, 20, "#ff000f".TranslateColor(), 300, 900)
        g.DrawPath(New Polygon2D({30, 69, 888, 777}, {691, 258, 666, 1}), Color.Green, 5, Color.Blue, dash:={5, 10})
        g.DrawLine(50, 50, 300, 600, Color.Red, 5)

        Return g
    End Function

    Private Sub testDrawing()
        Dim bmp As New Graphics(1000, 1000, "#ffffff")

        DirectCast(draw(bmp), Graphics).Save("./test.bmp", ImageFormats.Bmp)

        draw(New Graphics(1000, 1000, "#ffffff")).Save("./test.png")
        draw(New SvgGraphics(1000, 1000)).Save("./test.svg")
        draw(New PdfGraphics(1000, 1000)).Save("./test.pdf")
    End Sub
End Module
