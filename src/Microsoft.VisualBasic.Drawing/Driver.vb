Imports System.Drawing
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Driver
Imports Microsoft.VisualBasic.Imaging.SVG
Imports Microsoft.VisualBasic.Imaging.SVG.XML
Imports Microsoft.VisualBasic.MIME.Html.CSS

Public Module SkiaDriver

    Public Sub Register()
        DriverLoad.libgdiplus_raster = New RasterInterop
        DriverLoad.svg = New SvgInterop
        DriverLoad.pdf = New PdfInterop
    End Sub

    Private Class RasterInterop : Inherits DeviceInterop

        Public Overrides Function CreateGraphic(size As Size, fill As Color, dpi As Integer) As IGraphics
            Return New Graphics(size.Width, size.Height, fill, dpi)
        End Function

        Public Overrides Function CreateCanvas2D(background As Bitmap, direct_access As Boolean) As IGraphics
            Return New Graphics(background.CastSkiaBitmap)
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

        Public Overrides Function GetData(g As IGraphics, padding() As Integer) As IGraphicsData
            Return New PdfImage(g, g.Size, New Padding(padding))
        End Function
    End Class
End Module