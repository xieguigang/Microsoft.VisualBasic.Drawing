Imports System.Drawing
Imports Microsoft.VisualBasic.Imaging.Driver

Public Module Dx2DDriver

    ''' <summary>
    ''' Replace the gdi raster image drawing driver to directx api
    ''' </summary>
    Public Sub RegisterDx2D()
        Call DriverLoad.Register(New DxDriver, Drivers.GDI)
    End Sub

    Private Class DxDriver : Inherits DeviceInterop

        Public Overrides Function CreateGraphic(size As Size, fill As Color, dpi As Integer) As Imaging.IGraphics
            Throw New NotImplementedException()
        End Function

        Public Overrides Function CreateCanvas2D(background As Imaging.Bitmap, direct_access As Boolean) As Imaging.IGraphics
            Throw New NotImplementedException()
        End Function

        Public Overrides Function CreateCanvas2D(background As Imaging.Image, direct_access As Boolean) As Imaging.IGraphics
            Throw New NotImplementedException()
        End Function

        Public Overrides Function GetData(g As Imaging.IGraphics, padding() As Integer) As IGraphicsData
            Throw New NotImplementedException()
        End Function
    End Class

End Module
