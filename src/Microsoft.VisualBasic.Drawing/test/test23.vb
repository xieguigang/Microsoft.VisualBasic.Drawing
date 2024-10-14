Imports System.Drawing
Imports Microsoft.VisualBasic.Drawing
Imports Microsoft.VisualBasic.Imaging

Module test23

    Sub RunGradient()
        Call Rendering(New Graphics(256, 256, "#ffffff")).Save("./RedBlueArgb32GradientWithAlpha.bmp", ImageFormats.Bmp)
        Call Rendering(New Graphics(256, 256, "#ffffff")).Save("./RedBlueArgb32GradientWithAlpha.png")
        Call Rendering(New SvgGraphics(256, 256)).Save("./RedBlueArgb32GradientWithAlpha.svg")
        Call Rendering(New PdfGraphics(256, 256)).Save("./RedBlueArgb32GradientWithAlpha.pdf")

        Call Rendering2(New Graphics(256, 256, "#ffffff")).Save("./RedGreen24BitGradient.bmp", ImageFormats.Bmp)
        Call Rendering2(New Graphics(256, 256, "#ffffff")).Save("./RedGreen24BitGradient.png")
        Call Rendering2(New SvgGraphics(256, 256)).Save("./RedGreen24BitGradient.svg")
        Call Rendering2(New PdfGraphics(256, 256)).Save("./RedGreen24BitGradient.pdf")
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
        For row As Integer = 0 To g.Height - 1
            For column As Integer = 0 To g.Width - 1
                Call g.FillRectangle(New SolidBrush(Color.FromArgb(255, row, 0, column)), New Rectangle(row, column, 1, 1))
            Next
        Next

        Return g
    End Function
End Module
