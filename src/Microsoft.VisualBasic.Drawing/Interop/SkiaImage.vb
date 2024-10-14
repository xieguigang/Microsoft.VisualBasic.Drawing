Imports System.Drawing
Imports System.IO
Imports Microsoft.VisualBasic.Imaging
Imports SkiaSharp

Public Class SkiaImage : Inherits Image

    Public Overrides ReadOnly Property Size As Size
    Public ReadOnly Property Image As SKImage

    Sub New(canvas As Graphics)
        Size = canvas.Size
        Image = canvas.m_surface.Snapshot
    End Sub

    Sub New(size As Size, image As SKImage)
        Me.Size = size
        Me.Image = image
    End Sub

    Public Overrides Sub Save(s As Stream, format As ImageFormats)
        Dim data = Image.Encode(format.GetSkiaEncodeFormat, 100)

        Try
            Call data.SaveTo(s)
            Call s.Flush()
        Catch ex As Exception
            Call App.LogException(ex)
        End Try
    End Sub

    Protected Overrides Function ConvertToBitmapStream() As MemoryStream
        Dim s As New MemoryStream
        Call Save(s, ImageFormats.Bmp)
        Call s.Seek(Scan0, SeekOrigin.Begin)
        Return s
    End Function

    Public Shared Narrowing Operator CType(img As SkiaImage) As Bitmap
        Return FromStream(img.ConvertToBitmapStream)
    End Operator
End Class
