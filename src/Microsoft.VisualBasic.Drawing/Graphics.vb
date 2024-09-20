Imports System.IO
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Imaging
Imports SkiaSharp

''' <summary>
''' A wrapper of the skia sharp canvas object
''' </summary>
Public Class Graphics : Inherits SkiaGraphics

    ReadOnly m_info As SKImageInfo
    ReadOnly m_surface As SKSurface
    ReadOnly m_canvas As SKCanvas

    Public ReadOnly Property Width As Integer
        Get
            Return w
        End Get
    End Property

    Public ReadOnly Property Height As Integer
        Get
            Return h
        End Get
    End Property

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Sub New(width As Integer, height As Integer, Optional fill As String = "#ffffff")
        Call Me.New(width, height, fill.TranslateColor)
    End Sub

    Sub New(width As Integer, height As Integer, Optional fill As ArgbColor? = Nothing)
        Call MyBase.New(width, height)

        m_info = New SKImageInfo(width, height)
        m_surface = SKSurface.Create(m_info)
        m_canvas = m_surface.Canvas

        If fill Is Nothing Then
            Call Clear(Color.Transparent)
        Else
            Call Clear(Color.White)
        End If
    End Sub

    Sub New(size As Size, Optional fill As ArgbColor? = Nothing)
        Call Me.New(size.Width, size.Height, fill)
    End Sub

    Public Function GetPixel(x As Integer, y As Integer) As Color
        Throw New NotImplementedException
    End Function

    Public Sub SetPixel(x As Integer, y As Integer, pixel As Color)
        Throw New NotImplementedException
    End Sub

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Overloads Sub Clear(fill As Color)
        Call m_canvas.Clear(fill.AsSKColor)
    End Sub

    Public Overrides Sub Clear(color As String)
        Call m_canvas.Clear(color.TranslateSKColor)
    End Sub

    Public Sub DrawString(s As String, font As Font, color As Color, x As Single, y As Single)
        Dim textPain As New SKPaint With {
           .IsAntialias = True,
           .Style = SKPaintStyle.Fill,
           .Color = color.AsSKColor,
           .TextSize = font.Size
        }

        m_canvas.DrawText(s, x, y, textPain)
    End Sub

    Public Overrides Function ToString() As String
        Return "raster_buffer: " & StringFormats.Lanudry(Width * Height * 4&)
    End Function

    Public Function Save(file As String, Optional format As ImageFormats = ImageFormats.Png) As Boolean
        Using s As Stream = file.Open(FileMode.OpenOrCreate, doClear:=True,)
            Return Save(s, format)
        End Using
    End Function

    Public Function Save(file As Stream, format As ImageFormats) As Boolean
        Dim image = m_surface.Snapshot
        Dim data = image.Encode(SKEncodedImageFormat.Png, 100)

        Try
            Call data.SaveTo(file)
            Call file.Flush()
        Catch ex As Exception
            Call App.LogException(ex)
            Return False
        End Try

        Return True
    End Function

End Class
