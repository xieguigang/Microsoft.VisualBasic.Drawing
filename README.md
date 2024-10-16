# Microsoft.VisualBasic.Drawing

A replacement of the ``System.Drawing`` on linux environment for help migrant sciBASIC.NET based application from .net 6 to .net 8

this project provides the driver code for access the graphics drawing on different os based on the skia sharp project
from the .net 8.0, the gdi+ and pdf graphics drawing code was removed from scibasic.net framework, only works for the graphics math algorithm.

#### Note about skiasharp save bitmap

Currently, the bitmap file is not supported in skiasharp, the ``SKData`` is always nothing of you try to get bitmap encode data, example as:

```vbnet
' encoded is null always
' if we try to encode skbitmap as bitmap file
Using encoded = SKBitmap.Encode(SKEncodedImageFormat.Bmp, 100)
    Using stream = System.IO.File.OpenWrite("hello_world.png")
        encoded.SaveTo(stream)
    End Using
End Using
```

The [BMP#](https://github.com/dsoronda/bmp-sharp) project was applied to resolve this problem.

### Graphics Demo

```vbnet
Private Function Rendering(Of T As SkiaGraphics)(g As T) As T
    Dim offset As Integer = 20

    For row As Integer = offset To offset + 255
        For column As Integer = offset To offset + 255
            Call g.FillRectangle(
                New SolidBrush(Color.FromArgb(255, row - offset, 0, column - offset)),
                New Rectangle(row, column, 1, 1)
            )
        Next
    Next

    Dim font As New Font(FontFace.Consolas, 14)
    Dim h = g.MeasureString("A", font)
    Dim y = 300

    Call g.DrawString("Call g.FillRectangle(", font, Brushes.Green, New PointF(15, y))
    Call g.DrawString("    New SolidBrush(Color.FromArgb(255, row, 0, column)),", font, Brushes.Green, New PointF(15, y + h.Height * 1.75))
    Call g.DrawString("    New Rectangle(row, column, 1, 1)", font, Brushes.Green, New PointF(15, y + h.Height * 3.5))
    Call g.DrawString(")", font, Brushes.Green, New PointF(15, y + h.Height * 5.25))

    Return g
End Function
```

```vbnet
Call Rendering(New Graphics(512, 365, "#ffffff")).Save("./RedBlueArgb32GradientWithAlpha.bmp", ImageFormats.Bmp)
Call Rendering(New Graphics(512, 365, "#ffffff")).Save("./RedBlueArgb32GradientWithAlpha.webp", ImageFormats.Webp)
Call Rendering(New Graphics(512, 365, "#ffffff")).Save("./RedBlueArgb32GradientWithAlpha.png")
Call Rendering(New SvgGraphics(512, 365)).Save("./RedBlueArgb32GradientWithAlpha.svg")
Call Rendering(New PdfGraphics(512, 365)).Save("./RedBlueArgb32GradientWithAlpha.pdf")
```

![](./demo/RedBlueArgb32GradientWithAlpha.png)
![](./demo/RedGreen24BitGradient.svg)
