Imports System.Drawing
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Driver

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
            Throw New NotImplementedException()
        End Function

        Public Overrides Function GetData(g As IGraphics) As IGraphicsData
            Throw New NotImplementedException()
        End Function
    End Class

    Private Class SvgInterop : Inherits DeviceInterop

        Public Overrides Function CreateGraphic(size As Size, fill As Color, dpi As Integer) As IGraphics
            Dim svg As New SvgGraphics(size.Width, size.Height, dpi)
            svg.Clear(fill)
            Return svg
        End Function

        Public Overrides Function CreateCanvas2D(background As Bitmap, direct_access As Boolean) As IGraphics
            Throw New NotImplementedException()
        End Function

        Public Overrides Function GetData(g As IGraphics) As IGraphicsData
            Throw New NotImplementedException()
        End Function
    End Class

    Private Class PdfInterop : Inherits DeviceInterop

        Public Overrides Function CreateGraphic(size As Size, fill As Color, dpi As Integer) As IGraphics
            Dim pdf As New PdfGraphics(size.Width, size.Height, dpi)
            pdf.Clear(fill)
            Return pdf
        End Function

        Public Overrides Function CreateCanvas2D(background As Bitmap, direct_access As Boolean) As IGraphics
            Throw New NotImplementedException()
        End Function

        Public Overrides Function GetData(g As IGraphics) As IGraphicsData
            Throw New NotImplementedException()
        End Function
    End Class
End Module