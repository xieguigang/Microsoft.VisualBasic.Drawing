Imports System.Drawing
Imports Microsoft.VisualBasic.Imaging.Driver

''' <summary>
''' The device interop driver of the directx 2d graphics engine.
''' </summary>
Public Module Dx2DDriver

    ''' <summary>
    ''' Replace the gdi raster image drawing driver with the directx api based
    ''' gpu accelerated canvas.
    ''' </summary>
    Public Sub RegisterDx2D()
        Call DriverLoad.Register(New DxDriver, Drivers.GDI)
    End Sub

    Private Class DxDriver : Inherits DeviceInterop

        Public Overrides Function CreateGraphic(size As Size, fill As Color, dpi As Integer) As Microsoft.VisualBasic.Imaging.IGraphics
            Return New DxGraphics(size.Width, size.Height, fill, dpi)
        End Function

        Public Overrides Function CreateCanvas2D(background As Microsoft.VisualBasic.Imaging.Bitmap, direct_access As Boolean) As Microsoft.VisualBasic.Imaging.IGraphics
            Dim canvas As New DxGraphics(background.Width, background.Height, Color.Transparent)

            Call canvas.DrawImage(background, New Point)

            Return canvas
        End Function

        Public Overrides Function CreateCanvas2D(background As Microsoft.VisualBasic.Imaging.Image, direct_access As Boolean) As Microsoft.VisualBasic.Imaging.IGraphics
            Dim canvas As New DxGraphics(background.Width, background.Height, Color.Transparent)

            Call canvas.DrawImage(background, New Point)

            Return canvas
        End Function

        ''' <summary>
        ''' extract the raster image data of the directx canvas
        ''' </summary>
        ''' <remarks>
        ''' the gdi image data model (<c>ImageData</c>) is defined in the
        ''' imaging driver assembly which is not a dependency of this project,
        ''' the raster image of the canvas can be read through
        ''' <see cref="DxGraphics.GetRasterImage"/> instead.
        ''' </remarks>
        Public Overrides Function GetData(g As Microsoft.VisualBasic.Imaging.IGraphics, padding() As Integer) As IGraphicsData
            Throw New NotSupportedException(
                "the directx canvas does not provide the gdi image data model, " &
                "use DxGraphics.GetRasterImage instead."
            )
        End Function
    End Class

End Module
