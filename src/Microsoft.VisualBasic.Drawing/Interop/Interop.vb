Imports System.Drawing
Imports System.IO
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Imaging
Imports SkiaSharp

''' <summary>
''' Helper for interop with gdi+ in .net-windows
''' </summary>
Public Module Interop

    <Extension>
    Public Function AsRectangle(rect As RectangleF) As SKRect
        Return New SKRect(rect.Left, rect.Top, rect.Left + rect.Width, rect.Top + rect.Height)
    End Function

    <Extension>
    Public Function AsRectangle(rect As Rectangle) As SKRect
        Return New SKRect(rect.Left, rect.Top, rect.Left + rect.Width, rect.Top + rect.Height)
    End Function

    <Extension>
    Public Function AsSKPoint(point As PointF) As SKPoint
        Return New SKPoint(point.X, point.Y)
    End Function

    <Extension>
    Public Function AsSKPoint(point As Point) As SKPoint
        Return New SKPoint(point.X, point.Y)
    End Function

    <Extension>
    Public Function AsSKImage(image As Image) As SKImage
        If TypeOf image Is SkiaImage Then
            Return SKImage.FromBitmap(DirectCast(image, SkiaImage).Image)
        ElseIf TypeOf image Is Bitmap Then
            Return SKImage.FromBitmap(DirectCast(image, Bitmap).CastSkiaBitmap)
        Else
            Throw New NotImplementedException(image.GetType.FullName)
        End If
    End Function

    <Extension>
    Public Function CastSkiaBitmap(bitmap As Bitmap) As SKBitmap
        Using ms As New MemoryStream
            Call bitmap.Save(ms, ImageFormats.Bmp)
            Call ms.Flush()
            Call ms.Seek(Scan0, SeekOrigin.Begin)

            Return SKBitmap.Decode(ms)
        End Using
    End Function

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    <Extension>
    Public Function AsSKColor(color As Color) As SKColor
        Return New SKColor(color.R, color.G, color.B, color.A)
    End Function

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    <Extension>
    Public Function TranslateSKColor(color As String) As SKColor
        Return color.TranslateColor.AsSKColor
    End Function

    ''' <summary>
    ''' Convert the skia image to gdi+ image
    ''' </summary>
    ''' <param name="skImage"></param>
    ''' <returns></returns>
    <Extension>
    Public Function SkiaToGdiPlusImage(skImage As SKImage) As Image
        Using data As SKData = skImage.Encode(SKEncodedImageFormat.Png, 100)
            Dim bytes As Byte() = data.ToArray

            Using ms As New MemoryStream(bytes)
                Return Image.FromStream(ms)
            End Using
        End Using
    End Function

    <Extension>
    Public Function GetSkiaEncodeFormat(format As ImageFormats) As SKEncodedImageFormat
        Select Case format
            Case ImageFormats.Bmp : Return SKEncodedImageFormat.Bmp
            Case ImageFormats.Gif : Return SKEncodedImageFormat.Gif
            Case ImageFormats.Icon : Return SKEncodedImageFormat.Ico
            Case ImageFormats.Jpeg : Return SKEncodedImageFormat.Jpeg
            Case ImageFormats.Png : Return SKEncodedImageFormat.Png
            Case ImageFormats.Webp : Return SKEncodedImageFormat.Webp
            Case Else
                Throw New NotImplementedException(format.ToString)
        End Select
    End Function

    <Extension>
    Public Function CreatePaint(pen As Pen) As SKPaint
        Return New SKPaint With {
            .Color = pen.Color.AsSKColor,
            .IsAntialias = True,
            .Style = SKPaintStyle.Stroke,
            .StrokeWidth = pen.Width
        }
    End Function

    <Extension>
    Public Function CreateSkiaFont(font As Font) As SKFont
        Dim typeface = font.CreateSkiaTypeface
        Dim skfont As New SKFont(typeface, font.Size)
        Return skfont
    End Function

    <Extension>
    Public Function CreateSkiaTypeface(font As Font) As SKTypeface
        Dim style As SKFontStyleWeight = SKFontStyleWeight.Normal
        Dim slant As SKFontStyleSlant = SKFontStyleSlant.Upright

        Select Case font.Style
            Case FontStyle.Bold : style = SKFontStyleWeight.Bold
            Case FontStyle.Italic : slant = SKFontStyleSlant.Italic
        End Select

        Return SKTypeface.FromFamilyName(font.Name, style, SKFontStyleWidth.Normal, slant)
    End Function
End Module
