Imports System.Drawing
Imports Microsoft.VisualBasic.Drawing
Imports Microsoft.VisualBasic.Imaging

Module test23

    Sub RunGradient()
        Call Rendering(New Graphics(256, 256, "#ffffff")).Save("./RedBlueArgb32GradientWithAlpha.bmp", ImageFormats.Bmp)
        Call Rendering(New Graphics(256, 256, "#ffffff")).Save("./RedBlueArgb32GradientWithAlpha.png")
        Call Rendering(New SvgGraphics(256, 256)).Save("./RedBlueArgb32GradientWithAlpha.svg")
        Call Rendering(New PdfGraphics(256, 256)).Save("./RedBlueArgb32GradientWithAlpha.pdf")
    End Sub

    Private Function Rendering(Of T As SkiaGraphics)(g As T) As T
        For row As Integer = 0 To g.Height - 1
            For column As Integer = 0 To g.Width - 1
                Call g.FillRectangle(New SolidBrush(Color.FromArgb((row + column) / 2, row, 0, column)), New Rectangle(row, column, 1, 1))
            Next
        Next

        Return g
    End Function
End Module
