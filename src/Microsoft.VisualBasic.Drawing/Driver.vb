Imports System.Drawing
Imports System.IO
Imports System.Text
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Driver
Imports Microsoft.VisualBasic.Imaging.SVG
Imports Microsoft.VisualBasic.Imaging.SVG.XML
Imports Microsoft.VisualBasic.MIME.Html.CSS
Imports SkiaSharp
Imports Bitmap = Microsoft.VisualBasic.Imaging.Bitmap
Imports Font = Microsoft.VisualBasic.Imaging.Font
Imports Image = Microsoft.VisualBasic.Imaging.Image

Public Module SkiaDriver

    Public Sub Register()
        Call DriverLoad.Register(New RasterInterop, Drivers.GDI)
        Call DriverLoad.Register(New SvgInterop, Drivers.SVG)
        Call DriverLoad.Register(New PdfInterop, Drivers.PDF)
        Call DriverLoad.Register(AddressOf SkiaImage.FromFile)
        Call DriverLoad.Register(AddressOf SkiaDriver.MeasureString)
    End Sub

    ''' <summary>
    ''' 与 GDI+ <c>Graphics.MeasureString</c> 语义对齐的文本测量。
    '''
    ''' <para>
    ''' 旧实现返回的是"紧致墨迹框 + 1px"：高度明显小于 GDI+ 的行框高度（ascent + descent + leading），
    ''' 于是同一个图例 / 标题在 Skia 下总是比 GDI+ 矮一截、行距更紧。
    ''' 这里改为按字体度量返回行框尺寸，并补上 GDI+ 那约 1/6 em 的内边距。
    ''' </para>
    ''' </summary>
    ''' <param name="text">文本</param>
    ''' <param name="font">字体</param>
    Public Function MeasureString(text As String, font As Font) As SizeF
        Return MeasureString(text, font, 0)
    End Function

    ''' <summary>
    ''' 与 GDI+ <c>Graphics.MeasureString(text, font, width)</c> 语义对齐的文本测量：
    ''' 当 <paramref name="width"/> 大于 0 时按宽度折行后再累加行高。
    ''' </summary>
    ''' <param name="text">文本</param>
    ''' <param name="font">字体</param>
    ''' <param name="width">折行宽度（像素），非正数表示不折行</param>
    Public Function MeasureString(text As String, font As Font, width As Single) As SizeF
        If text.StringEmpty Then
            Return New SizeF(0, 0)
        End If

        Using paint As New SKPaint With {.IsAntialias = True},
            skfont As SKFont = font.CreateSkiaFont

            Dim metrics As SKFontMetrics = skfont.Metrics
            Dim lineHeight! = -metrics.Ascent + metrics.Descent + metrics.Leading
            Dim pad! = font.SkiaFontPixels / 6.0F
            Dim lines As List(Of String) = TextLines(text, skfont, paint, width)
            Dim maxWidth As Single = 0

            For Each line As String In lines
                Dim bounds As New SKRect

                Call skfont.MeasureText(line, bounds, paint)

                If bounds.Width > maxWidth Then
                    maxWidth = bounds.Width
                End If
            Next

            Return New SizeF(maxWidth + pad, lineHeight * lines.Count + pad)
        End Using
    End Function

    ''' <summary>
    ''' 按宽度做贪心折行（宽度非正数时只按换行符切分）
    ''' </summary>
    Private Function TextLines(text As String, skfont As SKFont, paint As SKPaint, width As Single) As List(Of String)
        Dim out As New List(Of String)

        For Each raw As String In text.Replace(vbCrLf, vbLf).Split(ControlChars.Lf)
            If width <= 0 OrElse raw.StringEmpty Then
                out.Add(raw)
                Continue For
            End If

            Dim line As New StringBuilder
            Dim bounds As New SKRect

            For Each word As String In raw.Split(" "c)
                Dim candidate As String = If(line.Length = 0, word, line.ToString & " " & word)

                Call skfont.MeasureText(candidate, bounds, paint)

                If bounds.Width > width AndAlso line.Length > 0 Then
                    out.Add(line.ToString())
                    line.Clear()
                    Call line.Append(word)
                Else
                    If line.Length > 0 Then
                        Call line.Append(" "c)
                    End If
                    Call line.Append(word)
                End If
            Next

            out.Add(line.ToString())
        Next

        If out.Count = 0 Then
            out.Add("")
        End If

        Return out
    End Function

    Private Class RasterInterop : Inherits DeviceInterop

        Public Overrides Function CreateGraphic(size As Size, fill As Color, dpi As Integer) As IGraphics
            Return New Graphics(size.Width, size.Height, fill, dpi)
        End Function

        Public Overrides Function CreateCanvas2D(background As Bitmap, direct_access As Boolean) As IGraphics
            Return New Graphics(background.CastSkiaBitmap)
        End Function

        Public Overrides Function CreateCanvas2D(background As Image, direct_access As Boolean) As IGraphics
            Dim bitmap As SKBitmap

            If TypeOf background Is SkiaImage Then
                bitmap = DirectCast(background, SkiaImage).Image
            Else
                Using s As Stream = New MemoryStream
                    Call background.Save(s, ImageFormats.Png)
                    Call s.Seek(0, SeekOrigin.Begin)
                    Call s.Flush()

                    bitmap = SKBitmap.Decode(s)
                End Using
            End If

            Return New Graphics(bitmap)
        End Function

        Public Overrides Function GetData(g As IGraphics, padding() As Integer) As IGraphicsData
            Dim img As Image = DirectCast(g, GdiRasterGraphics).ImageResource
            Dim data As New ImageData(img, g.Size, New Padding(padding))
            Return data
        End Function
    End Class

    Private Class SvgInterop : Inherits DeviceInterop

        Public Overrides Function CreateGraphic(size As Size, fill As Color, dpi As Integer) As IGraphics
            Dim svg As New SvgGraphics(size.Width, size.Height, dpi)
            svg.Clear(fill)
            Return svg
        End Function

        Public Overrides Function CreateCanvas2D(background As Bitmap, direct_access As Boolean) As IGraphics
            Dim svg As New SvgGraphics(background.Width, background.Height, 100)
            svg.DrawImage(background, New Point)
            Return svg
        End Function

        Public Overrides Function CreateCanvas2D(background As Image, direct_access As Boolean) As IGraphics
            Dim svg As New SvgGraphics(background.Width, background.Height, 100)
            svg.DrawImage(background, New Point)
            Return svg
        End Function

        Public Overrides Function GetData(g As IGraphics, padding As Integer()) As IGraphicsData
            Dim svg As SvgGraphics = DirectCast(g, SvgGraphics)
            Dim doc As SvgDocument = SvgDocument.Parse(xml:=svg.GetSvgText)
            Dim model As New SVGDataLayers(doc)

            Return New SVGData(model, svg.Size, New Padding(padding))
        End Function
    End Class

    Private Class PdfInterop : Inherits DeviceInterop

        Public Overrides Function CreateGraphic(size As Size, fill As Color, dpi As Integer) As IGraphics
            Dim pdf As New PdfGraphics(size.Width, size.Height, dpi)
            pdf.Clear(fill)
            Return pdf
        End Function

        Public Overrides Function CreateCanvas2D(background As Bitmap, direct_access As Boolean) As IGraphics
            Dim pdf As New PdfGraphics(background.Width, background.Height)
            pdf.DrawImage(background, New Point)
            Return pdf
        End Function

        Public Overrides Function CreateCanvas2D(background As Image, direct_access As Boolean) As IGraphics
            Dim pdf As New PdfGraphics(background.Width, background.Height)
            pdf.DrawImage(background, New Point)
            Return pdf
        End Function

        Public Overrides Function GetData(g As IGraphics, padding() As Integer) As IGraphicsData
            Return New PdfImage(g, g.Size, New Padding(padding))
        End Function
    End Class
End Module