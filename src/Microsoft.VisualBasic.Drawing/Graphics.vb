Imports System.Drawing
Imports Microsoft.VisualBasic.ComponentModel.Collection

Public Class Graphics

    ReadOnly m_pixels As Color()()
    ReadOnly m_size As Size

    Public ReadOnly Property Width As Integer
        Get
            Return m_size.Width
        End Get
    End Property

    Public ReadOnly Property Height As Integer
        Get
            Return m_size.Height
        End Get
    End Property

    Sub New(size As Size, Optional fill As Color? = Nothing)
        m_size = size
        m_pixels = RectangularArray.Matrix(Of Color)(size.Width, size.Height)

        If fill Is Nothing Then
            Call Clear(Color.Transparent)
        Else
            Call Clear(Color.White)
        End If
    End Sub

    Public Function GetPixel(x As Integer, y As Integer) As Color
        Return m_pixels(y)(x)
    End Function

    Public Sub SetPixel(x As Integer, y As Integer, pixel As Color)
        m_pixels(y)(x) = pixel
    End Sub

    Public Sub Clear(fill As Color)
        For Each row As Color() In m_pixels
            For i As Integer = 0 To row.Length - 1
                row(i) = fill
            Next
        Next
    End Sub

    Public Sub DrawString(s As String, font As Font, color As Color, x As Single, y As Single)

    End Sub

    Public Overrides Function ToString() As String
        Return "raster_buffer: " & StringFormats.Lanudry(Width * Height * 4&)
    End Function

End Class
