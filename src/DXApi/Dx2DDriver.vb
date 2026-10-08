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
        ' the text size measurement of the cross platform ``Font`` type is
        ' delegated to an external driver, the directwrite text layout of the
        ' directx canvas is registered here, otherwise the font metrics of the
        ' css layout engine will throw an ``InvalidProgramException``.
        Call DriverLoad.Register(Function(text As String, font As Microsoft.VisualBasic.Imaging.Font) As SizeF
                                     Return DxTextMeasurer.Measure(text, font)
                                 End Function)
    End Sub

    ''' <summary>
    ''' A tiny off screen directx canvas that is only used for measuring the
    ''' size of a text run through the directwrite api.
    ''' </summary>
    Private NotInheritable Class DxTextMeasurer

        ''' <summary>
        ''' the shared canvas is created lazily because the gpu device of the
        ''' process may be unavailable during the program startup.
        ''' </summary>
        Private Shared canvas As DxGraphics
        Private Shared ReadOnly syncRoot As New Object

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Measure the layout size of the given text run.
        ''' </summary>
        ''' <param name="text"></param>
        ''' <param name="font"></param>
        ''' <returns>
        ''' the pixel size of the text bounding box, an empty size is returned
        ''' when the directx device is not available on this machine.
        ''' </returns>
        Public Shared Function Measure(text As String, font As Microsoft.VisualBasic.Imaging.Font) As SizeF
            SyncLock syncRoot
                If canvas Is Nothing Then
                    Try
                        canvas = New DxGraphics(8, 8, Color.Transparent)
                    Catch ex As Exception
                        Return SizeF.Empty
                    End Try
                End If

                Try
                    Return canvas.MeasureString(text, font)
                Catch ex As Exception
                    Return SizeF.Empty
                End Try
            End SyncLock
        End Function
    End Class

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
