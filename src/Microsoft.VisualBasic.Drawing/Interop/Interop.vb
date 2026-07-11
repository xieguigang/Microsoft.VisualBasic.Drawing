Imports System.Drawing
Imports System.IO
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Imaging
Imports SkiaSharp
Imports Bitmap = Microsoft.VisualBasic.Imaging.Bitmap
Imports Font = Microsoft.VisualBasic.Imaging.Font
Imports FontStyle = Microsoft.VisualBasic.Imaging.FontStyle
Imports Image = Microsoft.VisualBasic.Imaging.Image
Imports Pen = Microsoft.VisualBasic.Imaging.Pen

''' <summary>
''' Helper for interop with gdi+ in .net-windows
''' </summary>
Public Module SkiaInterop

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

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="bitmap"></param>
    ''' <returns></returns>
    ''' <remarks>
    ''' the transparent will be lost when cast memory bitmap to skia bitmap object
    ''' </remarks>
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
                Throw New NotImplementedException("ImageFormats: " & format.ToString & " convert to skia encoder format.")
        End Select
    End Function

    ''' <summary>
    ''' Create the stroke pen paint style
    ''' </summary>
    ''' <param name="pen"></param>
    ''' <returns></returns>
    <Extension>
    Public Function CreatePaint(pen As Pen) As SKPaint
        Return New SKPaint With {
            .Color = pen.Color.AsSKColor,
            .IsAntialias = True,
            .Style = SKPaintStyle.Stroke,
            .StrokeWidth = pen.Width
        }
    End Function

    ''' <summary>
    ''' convert a brush object (from Microsoft.VisualBasic.Imaging) into a skia paint
    ''' for fill operations. supports solid / linear gradient / texture brush in a
    ''' best-effort manner and degrades gracefully (no exception) for unknown types.
    ''' </summary>
    <Extension>
    Public Function CreatePaint(brush As Brush) As SKPaint
        Dim paint As New SKPaint With {
            .Style = SKPaintStyle.Fill,
            .IsAntialias = True
        }

        If TypeOf brush Is SolidBrush Then
            paint.Color = DirectCast(brush, SolidBrush).Color.AsSKColor
            Return paint
        End If

        Dim btype As Type = brush.GetType()
        Dim tname As String = btype.Name

        If tname.Contains("Gradient") Then
            Dim shader As SKShader = TryBuildGradientShader(brush, btype)

            If shader IsNot Nothing Then
                paint.Shader = shader
                Return paint
            End If
        ElseIf tname.Contains("Texture") Then
            Dim shader As SKShader = TryBuildTextureShader(brush, btype)

            If shader IsNot Nothing Then
                paint.Shader = shader
                Return paint
            End If
        End If

        Call $"The brush type '{tname}' is not supported by the skia backend, using transparent black as fallback.".warning
        paint.Color = SKColors.Transparent
        Return paint
    End Function

    Private Function TryBuildGradientShader(brush As Brush, btype As Type) As SKShader
        Dim rectProp As PropertyInfo = btype.GetProperty("Rectangle")
        Dim rect As RectangleF = RectangleF.Empty

        If rectProp IsNot Nothing Then
            Dim rv As Object = rectProp.GetValue(brush)

            If rv IsNot Nothing Then
                Dim gr As Rectangle = DirectCast(rv, Rectangle)
                rect = New RectangleF(gr.X, gr.Y, gr.Width, gr.Height)
            End If
        End If

        Dim colsProp As PropertyInfo = btype.GetProperty("LinearColors")
        Dim cols As Color() = Nothing

        If colsProp IsNot Nothing Then
            cols = TryCast(colsProp.GetValue(brush), Color())
        End If

        If cols Is Nothing OrElse cols.Length < 2 Then
            Dim c1 As PropertyInfo = btype.GetProperty("Color1")
            Dim c2 As PropertyInfo = btype.GetProperty("Color2")

            If c1 IsNot Nothing AndAlso c2 IsNot Nothing Then
                Dim v1 As Object = c1.GetValue(brush)
                Dim v2 As Object = c2.GetValue(brush)

                If v1 IsNot Nothing AndAlso v2 IsNot Nothing Then
                    cols = {DirectCast(v1, Color), DirectCast(v2, Color)}
                End If
            End If
        End If

        If cols Is Nothing OrElse cols.Length < 2 Then
            Return Nothing
        End If

        Dim startP As New SKPoint(rect.Left, rect.Top)
        Dim endP As New SKPoint(rect.Right, rect.Bottom)

        If rect.IsEmpty Then
            startP = New SKPoint(0, 0)
            endP = New SKPoint(100, 100)
        End If

        Dim skColors = cols.Select(Function(c) c.AsSKColor).ToArray()

        Return SKShader.CreateLinearGradient(startP, endP, skColors, SKShaderTileMode.Clamp)
    End Function

    Private Function TryBuildTextureShader(brush As Brush, btype As Type) As SKShader
        Dim imgProp As PropertyInfo = btype.GetProperty("Image")

        If imgProp Is Nothing Then
            Return Nothing
        End If

        Dim img As Object = imgProp.GetValue(brush)

        If img Is Nothing Then
            Return Nothing
        End If

        Dim skImg As SKImage = AsSKImage(DirectCast(img, Image))

        If skImg Is Nothing Then
            Return Nothing
        End If

        Return SKShader.CreateImage(skImg, SKShaderTileMode.Repeat)
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
