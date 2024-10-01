Imports System.Drawing
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Driver

Public Module SkiaDriver

    Public Sub Register()
        DriverLoad.libgdiplus_raster = AddressOf CreateGdiPlusCanvas
        DriverLoad.svg = AddressOf CreateSvgCanvas
        DriverLoad.pdf = AddressOf CreatePdfCanvas
    End Sub

    Private Function CreateGdiPlusCanvas(size As Size, fill As Color, dpi As Integer) As IGraphics
        Return New Graphics(size.Width, size.Height, fill, dpi)
    End Function

    Private Function CreateSvgCanvas(size As Size, fill As Color, dpi As Integer) As IGraphics
        Dim svg As New SvgGraphics(size.Width, size.Height, dpi)
        svg.Clear(fill)
        Return svg
    End Function

    Private Function CreatePdfCanvas(size As Size, fill As Color, dpi As Integer) As IGraphics
        Dim pdf As New PdfGraphics(size.Width, size.Height, dpi)
        pdf.Clear(fill)
        Return pdf
    End Function
End Module
