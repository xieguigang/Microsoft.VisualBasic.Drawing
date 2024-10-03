Imports System.Drawing
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

End Class
