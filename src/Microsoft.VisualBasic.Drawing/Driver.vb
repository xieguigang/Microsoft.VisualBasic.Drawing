Imports System.Drawing
Imports System.IO
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Driver
Imports Microsoft.VisualBasic.Imaging.SVG
Imports Microsoft.VisualBasic.Imaging.SVG.XML
Imports Microsoft.VisualBasic.MIME.Html.CSS
Imports SkiaSharp

Public Module SkiaDriver

    Public Sub Register()
        Call DriverLoad.Register(New RasterInterop, Drivers.GDI)
        Call DriverLoad.Register(New SvgInterop, Drivers.SVG)
        Call DriverLoad.Register(New PdfInterop, Drivers.PDF)
        Call DriverLoad.Register(AddressOf SkiaImage.FromFile)
        Call DriverLoad.Register(AddressOf SkiaDriver.MeasureString)
    End Sub

    Public Function MeasureString(text As String, font As Font) As SizeF
        Dim textBounds As New SKRect

        Using paint As New SKPaint With {
            .TextSize = font.Size,
            .IsAntialias = True,
            .Typeface = SKTypeface.FromFamilyName(font.Name)
        }
            Call paint.MeasureText(text, textBounds)

            Return New SizeF(textBounds.Width + 1, textBounds.Height + 1)
        End Using
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