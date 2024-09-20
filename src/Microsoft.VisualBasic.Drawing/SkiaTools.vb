Imports System.Runtime.CompilerServices
Imports SkiaSharp

Public Module SkiaTools

    <Extension>
    Public Function ConvertToPngFile(svg As SkiaSharp.Extended.Svg.SKSvg) As SKImage
        Using bitmap As New SKBitmap(svg.Picture.CullRect.Width, svg.Picture.CullRect.Height)
            Using canvas As New SKCanvas(bitmap)
                Call canvas.DrawPicture(svg.Picture)
                Return SKImage.FromBitmap(bitmap)
            End Using
        End Using
    End Function

    <Extension>
    Public Sub ConvertToPdfFile(svg As SkiaSharp.Extended.Svg.SKSvg, pdffile As String)
        Using pdfDocument = SKDocument.CreatePdf(pdffile)
            Dim canvas = pdfDocument.BeginPage(svg.Picture.CullRect.Width, svg.Picture.CullRect.Height)

            Call canvas.DrawPicture(svg.Picture)
            Call pdfDocument.EndPage()
            Call pdfDocument.Close()
        End Using
    End Sub
End Module
