Imports System.IO
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Imaging
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

    Public Iterator Function GetGifFrames(file As Stream) As IEnumerable(Of Bitmap)
        Dim filedata As SKCodec = SKCodec.Create(file)

        If Not filedata Is Nothing Then
            Dim info = filedata.Info

            For Each frame As SKCodecFrameInfo In filedata.FrameInfo
                Using bitmap As SKBitmap = New SKBitmap(Info.Width, Info.Height, Info.ColorType, Info.AlphaType)
                    Call filedata.GetPixels(frame, bitmap)
                End Using
            Next
        End If
    End Function
End Module
