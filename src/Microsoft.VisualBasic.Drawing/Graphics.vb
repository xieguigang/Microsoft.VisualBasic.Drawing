Imports System.Drawing
Imports System.IO
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Driver
Imports SkiaSharp

''' <summary>
''' A wrapper of the skia sharp canvas object for raster image drawing
''' </summary>
Public Class Graphics : Inherits SkiaGraphics
    Implements GdiRasterGraphics

    ''' <summary>
    ''' image save must dispose the canvas at first
    ''' </summary>
    Friend ReadOnly m_surface As SKBitmap

    Public Overrides ReadOnly Property Driver As Drivers
        Get
            Return Drivers.GDI
        End Get
    End Property

    Public ReadOnly Property ImageResource As Image Implements GdiRasterGraphics.ImageResource
        Get
            Return New SkiaImage(Me)
        End Get
    End Property

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Sub New(width As Integer, height As Integer,
            Optional fill As String = "#ffffff",
            Optional dpi As Integer = 100)

        Call Me.New(width, height, TranslateColor(fill), dpi)
    End Sub

    Sub New(width As Integer, height As Integer, fill As Color, Optional dpi As Integer = 100)
        Call MyBase.New(width, height, dpi)

        ' 20241014 the bitmap pixel should be in 32 bit ARGB format
        ' so the bitmap construct must be configed as 
        ' SKColorType.Bgra8888,
        ' SKAlphaType.Premul
        m_surface = New SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul)
        m_canvas = New SKCanvas(m_surface)

        If fill.IsEmpty Then
            Call Clear(Color.Transparent)
        Else
            Call Clear(fill)
        End If
    End Sub

    Sub New(bitmap As SKBitmap, Optional dpi As Integer = 100)
        Call MyBase.New(bitmap.Width, bitmap.Height, dpi)

        m_surface = bitmap
        m_canvas = New SKCanvas(m_surface)
    End Sub

    Public Function GetPixel(x As Integer, y As Integer) As Color
        Call Flush()

        Dim skcolor As SKColor = m_surface.GetPixel(x, y)
        Dim gdicolor As Color = Color.FromArgb(skcolor.Alpha, skcolor.Red, skcolor.Green, skcolor.Blue)

        Return gdicolor
    End Function

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Sub SetPixel(x As Integer, y As Integer, pixel As Color)
        m_surface.SetPixel(x, y, pixel.AsSKColor)
    End Sub

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Overrides Function ToString() As String
        Return "raster_buffer: " & StringFormats.Lanudry(Width * Height * 4&)
    End Function

    Public Overloads Function Save(file As String, Optional format As ImageFormats = ImageFormats.Png) As Boolean
        Using s As Stream = file.Open(FileMode.OpenOrCreate, doClear:=True,)
            Return Save(s, format)
        End Using
    End Function

    Public Overrides Function Save(file As Stream, format As ImageFormats) As Boolean
        Call Dispose()
        Return SkiaImage.SaveRasterImage(m_surface, file, format)
    End Function

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Overrides Sub Save(file As Stream)
        Call Save(file, format:=ImageFormats.Png)
    End Sub

    Public Shared Function FromImage(bitmap As Bitmap) As Graphics
        If bitmap Is Nothing Then
            Throw New ArgumentNullException(NameOf(bitmap), "The bitmap cannot be null.")
        End If
        Dim skBitmap As New SKBitmap(bitmap.Width, bitmap.Height)
        Dim canvas As New SKCanvas(skBitmap)
        ' Draw the GDI+ Bitmap onto the SkiaSharp canvas
        canvas.DrawImage(bitmap.AsSKImage, 0, 0)
        Return New Graphics(skBitmap)
    End Function

    Protected Overrides Sub ReleaseHandle()
        If Not m_isDisposed Then
            Call m_canvas.Flush()
            Call m_canvas.Dispose()
        End If

        m_isDisposed = True
    End Sub
End Class
