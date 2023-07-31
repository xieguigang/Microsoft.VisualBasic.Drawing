Imports System.IO
Imports Microsoft.VisualBasic.Drawing.ImageFormat
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.BitmapImage

Public Class Image

    ReadOnly core As BitmapBuffer

    Sub New(file As FileEncoder)
        core = file.LoadBuffer
    End Sub

    Public Function Save(file As Stream, Optional format As ImageFormats = ImageFormats.Png) As Boolean
        Select Case format
            Case ImageFormats.Bmp : Return New Bitmap(file).SaveBuffer(core)
            Case ImageFormats.Gif : Return New Gif(file).SaveBuffer(core)
            Case ImageFormats.Jpeg : Return New Jpeg(file).SaveBuffer(core)
            Case ImageFormats.Png : Return New Png(file).SaveBuffer(core)
            Case ImageFormats.Tiff : Return New TIFF(file).SaveBuffer(core)
            Case Else
                Throw New NotImplementedException
        End Select
    End Function

    Public Shared Function FromFile(file As Stream) As Image
        Dim format As Type = FileEncoder.GetEncoder(file)
        Dim img As FileEncoder = Activator.CreateInstance(format, New Object() {file})

        Return New Image(file:=img)
    End Function
End Class
