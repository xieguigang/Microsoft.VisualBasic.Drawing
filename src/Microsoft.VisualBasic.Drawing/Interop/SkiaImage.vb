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
        Dim data = Image.Encode(SKEncodedImageFormat.Png, 100)

        Try
            Call data.SaveTo(s)
            Call s.Flush()
        Catch ex As Exception
            Call App.LogException(ex)
        End Try
    End Sub
End Class
