Imports System.Drawing
Imports Microsoft.VisualBasic.Drawing
Imports Microsoft.VisualBasic.Imaging

Module test23

    Sub RunGradient()
        Call Rendering(New Graphics(512, 365, "#ffffff")).Save("./RedBlueArgb32GradientWithAlpha.bmp", ImageFormats.Bmp)
        Call Rendering(New Graphics(512, 365, "#ffffff")).Save("./RedBlueArgb32GradientWithAlpha.png")
        Call Rendering(New SvgGraphics(512, 365)).Save("./RedBlueArgb32GradientWithAlpha.svg")
        Call Rendering(New PdfGraphics(512, 365)).Save("./RedBlueArgb32GradientWithAlpha.pdf")

        Call Rendering2(New Graphics(256, 256, "#ffffff")).Save("./RedGreen24BitGradient.bmp", ImageFormats.Bmp)
        Call Rendering2(New Graphics(256, 256, "#ffffff")).Save("./RedGreen24BitGradient.png")
        Call Rendering2(New SvgGraphics(256, 256)).Save("./RedGreen24BitGradient.svg")
        Call Rendering2(New PdfGraphics(256, 256)).Save("./RedGreen24BitGradient.pdf")

        Pause()
    End Sub

    Private Function Rendering2(Of T As SkiaGraphics)(g As T) As T
        For row As Integer = 0 To g.Height - 1
            For column As Integer = 0 To g.Width - 1
                Call g.FillRectangle(New SolidBrush(Color.FromArgb(255, row, column, 0)), New Rectangle(row, column, 1, 1))
            Next
        Next

        Return g
    End Function

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
End Module
