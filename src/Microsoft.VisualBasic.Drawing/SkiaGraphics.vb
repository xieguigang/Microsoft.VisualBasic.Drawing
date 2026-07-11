Imports System.Drawing
Imports System.IO
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.ComponentModel.Algorithm.base
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.BitmapImage
Imports Microsoft.VisualBasic.Imaging.Math2D
Imports SkiaSharp
Imports Brush = Microsoft.VisualBasic.Imaging.Brush
Imports Font = Microsoft.VisualBasic.Imaging.Font
Imports Image = Microsoft.VisualBasic.Imaging.Image
Imports Pen = Microsoft.VisualBasic.Imaging.Pen
Imports SolidBrush = Microsoft.VisualBasic.Imaging.SolidBrush
Imports std = System.Math

''' <summary>
''' the abstract wrapper for the skiasharp library
''' </summary>
Public MustInherit Class SkiaGraphics : Inherits IGraphics
    Implements SaveGdiBitmap

    Protected ReadOnly canvasRect As SKRect
    Protected m_canvas As SKCanvas
    Protected m_clipSaveCount As Integer = -1
    Protected m_lastClipRect As RectangleF?

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Sub New(width As Integer, height As Integer, dpi As Integer)
        Call MyBase.New(dpi)

        If width < 0 OrElse height < 0 Then
            Throw New InvalidDataException($"negative canvas size is not valid! (width:{width}, height:{height})")
        End If

        Size = New Size(width, height)
        canvasRect = SKRect.Create(width, height)
    End Sub

    ''' <summary>
    ''' the graphics size of current skia canvas
    ''' </summary>
    ''' <returns></returns>
    Public Overrides ReadOnly Property Size As Size
    Public Overrides Property RenderingOrigin As Point
    Public Overrides Property TextContrast As Integer

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Overrides Sub Clear(fill As Color)
        Call m_canvas.Clear(fill.AsSKColor)
    End Sub

    Public Overloads Sub Clear(color As String)
        Call m_canvas.Clear(TranslateColor(color).AsSKColor)
    End Sub

    Public Overloads Sub DrawString(s As String, fontName As String, fontSize As Single, color As Color, x As Single, y As Single)
        Using textPain As New SKPaint With {
           .IsAntialias = True,
           .Style = SKPaintStyle.Fill,
           .Color = color.AsSKColor
        }
            Dim textBounds As New SKRect
            Dim skfont As New SKFont(SKTypeface.FromFamilyName(fontName), fontSize)
            Dim text As SKTextBlob = SKTextBlob.Create(s, skfont)

            If s Is Nothing Then
                s = ""
                Call $"the given string for drawing is nothing at stack trace: {vbCrLf}{Environment.StackTrace}".warning
            End If

            Call skfont.MeasureText(s, textBounds, textPain)
            Call m_canvas.DrawText(text, x, y + textBounds.Height, textPain)
        End Using
    End Sub

    Public Overrides Sub DrawString(s As String, font As Font, brush As Brush, ByRef point As PointF)
        Call DrawString(s, font.Name, font.Size, DirectCast(brush, SolidBrush).Color, point.X, point.Y)
    End Sub

    Public Overrides Sub DrawString(s As String, font As Font, brush As Brush, layoutRectangle As RectangleF)
        Call DrawString(s, font.Name, font.Size, DirectCast(brush, SolidBrush).Color, layoutRectangle.Left, layoutRectangle.Top)
    End Sub

    Public Overrides Sub DrawString(s As String, font As Font, brush As Brush,
                                    ByRef x As Single,
                                    ByRef y As Single, angle As Single)

        Using paint As New SKPaint With {
                .Color = DirectCast(brush, SolidBrush).Color.AsSKColor,
                .IsAntialias = True,
                .Style = SKPaintStyle.Fill
            }, skfont As New SKFont(SKTypeface.FromFamilyName(font.Name), font.Size)

            Dim textBounds As New SKRect
            Dim txt = SKTextBlob.Create(s, skfont)

            ' get text bounds size
            Call skfont.MeasureText(s, textBounds, paint)

            m_canvas.Translate(x, y)
            m_canvas.RotateDegrees(angle)
            m_canvas.DrawText(txt, -textBounds.MidX, -textBounds.MidY, paint)
            m_canvas.RotateDegrees(-angle)
            m_canvas.Translate(-x, -y)
        End Using
    End Sub

    Public Overrides Sub DrawString(s As String, font As Font, brush As Brush, x As Single, y As Single)
        Call DrawString(s, font.Name, font.Size, DirectCast(brush, SolidBrush).Color, x, y)
    End Sub

    Public Overloads Sub DrawLine(x1 As Single, y1 As Single, x2 As Single, y2 As Single,
                                  color As Color,
                                  width As Single,
                                  Optional dash As SKPathEffect = Nothing)

        Using paint As New SKPaint With {
            .Color = color.AsSKColor,
            .StrokeWidth = width,
            .Style = SKPaintStyle.Stroke,
            .PathEffect = dash
        }
            Call m_canvas.DrawLine(x1, y1, x2, y2, paint)
        End Using
    End Sub

    Public Overrides Sub DrawLine(pen As Pen, pt1 As PointF, pt2 As PointF)
        Call DrawLine(pt1.X, pt1.Y, pt2.X, pt2.Y, pen.Color, pen.Width, pen.LineDashStyle)
    End Sub

    Public Overrides Sub DrawLine(pen As Pen, pt1 As Point, pt2 As Point)
        Call DrawLine(pt1.X, pt1.Y, pt2.X, pt2.Y, pen.Color, pen.Width, pen.LineDashStyle)
    End Sub

    Public Overrides Sub DrawLine(pen As Pen, x1 As Integer, y1 As Integer, x2 As Integer, y2 As Integer)
        Call DrawLine(x1, y1, x2, y2, pen.Color, pen.Width, pen.LineDashStyle)
    End Sub

    Public Overrides Sub DrawLine(pen As Pen, x1 As Single, y1 As Single, x2 As Single, y2 As Single)
        Call DrawLine(x1, y1, x2, y2, pen.Color, pen.Width, pen.LineDashStyle)
    End Sub

    Public Overloads Sub DrawPath(path As Polygon2D, color As Color, width As Single,
                                  Optional fill As Color? = Nothing,
                                  Optional dash As Single() = Nothing)

        Using skpath As New SKPath
            Dim x = path.xpoints
            Dim y = path.ypoints

            Call skpath.MoveTo(CSng(x(0)), CSng(y(0)))

            For i As Integer = 1 To x.Length - 1
                Call skpath.LineTo(CSng(x(i)), CSng(y(i)))
            Next

            Call skpath.Close()

            If Not fill Is Nothing Then
                Using paint As New SKPaint With {
                    .Style = SKPaintStyle.Fill,
                    .Color = CType(fill, Color).AsSKColor
                }
                    Call m_canvas.DrawPath(skpath, paint)
                End Using
            End If

            Using paint As New SKPaint With {
                .Color = color.AsSKColor,
                .StrokeWidth = width,
                .Style = SKPaintStyle.Stroke
            }
                If Not dash.IsNullOrEmpty Then
                    paint.PathEffect = SKPathEffect.CreateDash(dash, 0)
                End If

                Call m_canvas.DrawPath(skpath, paint)
            End Using
        End Using
    End Sub

    Public Overrides Sub DrawPath(pen As Pen, path As GraphicsPath)
        Using skpath As SKPath = path.CreatePath
            Using stroke As SKPaint = pen.CreatePaint
                Call m_canvas.DrawPath(skpath, stroke)
            End Using
        End Using
    End Sub

    Public Overrides Sub AddMetafileComment(data() As Byte)
        Call "AddMetafileComment is not supported by the skia backend and will be ignored.".warning
    End Sub

    Public Overrides Sub DrawArc(pen As Pen, rect As RectangleF, startAngle As Single, sweepAngle As Single)
        Call DrawArc(pen, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle)
    End Sub

    Public Overrides Sub DrawArc(pen As Pen, rect As Rectangle, startAngle As Single, sweepAngle As Single)
        Call DrawArc(pen, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle)
    End Sub

    Public Overrides Sub DrawArc(pen As Pen, x As Integer, y As Integer, width As Integer, height As Integer, startAngle As Integer, sweepAngle As Integer)
        Call DrawArc(pen, CSng(x), CSng(y), CSng(width), CSng(height), CSng(startAngle), CSng(sweepAngle))
    End Sub

    Public Overrides Sub DrawArc(pen As Pen, x As Single, y As Single, width As Single, height As Single, startAngle As Single, sweepAngle As Single)
        Using stroke As SKPaint = pen.CreatePaint, path As New SKPath
            path.AddArc(New SKRect(x, y, x + width, y + height), startAngle, sweepAngle)
            m_canvas.DrawPath(path, stroke)
        End Using
    End Sub

    Public Overrides Sub DrawBezier(pen As Pen, pt1 As Point, pt2 As Point, pt3 As Point, pt4 As Point)
        Call DrawBezier(pen, CSng(pt1.X), CSng(pt1.Y), CSng(pt2.X), CSng(pt2.Y), CSng(pt3.X), CSng(pt3.Y), CSng(pt4.X), CSng(pt4.Y))
    End Sub

    Public Overrides Sub DrawBezier(pen As Pen, pt1 As PointF, pt2 As PointF, pt3 As PointF, pt4 As PointF)
        Call DrawBezier(pen, pt1.X, pt1.Y, pt2.X, pt2.Y, pt3.X, pt3.Y, pt4.X, pt4.Y)
    End Sub

    Public Overrides Sub DrawBezier(pen As Pen, x1 As Single, y1 As Single, x2 As Single, y2 As Single, x3 As Single, y3 As Single, x4 As Single, y4 As Single)
        Using path As New SKPath, paint As SKPaint = pen.CreatePaint
            Call path.MoveTo(x1, y1)
            Call path.CubicTo(x2, y2, x3, y3, x4, y4)
            Call m_canvas.DrawPath(path, paint)
        End Using
    End Sub

    Public Overrides Sub DrawBeziers(pen As Pen, points() As PointF)
        If points Is Nothing OrElse points.Length < 4 Then
            Return
        End If

        Using path As New SKPath, paint As SKPaint = pen.CreatePaint
            Call path.MoveTo(points(0).X, points(0).Y)

            Dim i As Integer = 1

            Do While i + 2 <= points.Length - 1
                Call path.CubicTo(points(i).X, points(i).Y, points(i + 1).X, points(i + 1).Y, points(i + 2).X, points(i + 2).Y)
                i += 3
            Loop

            Call m_canvas.DrawPath(path, paint)
        End Using
    End Sub

    Public Overrides Sub DrawBeziers(pen As Pen, points() As Point)
        If points Is Nothing OrElse points.Length < 4 Then
            Return
        End If

        Call DrawBeziers(pen, points.Select(Function(p) New PointF(p.X, p.Y)).ToArray)
    End Sub

    Public Overrides Sub DrawClosedCurve(pen As Pen, points() As Point)
        If points Is Nothing Then
            Return
        End If

        Call DrawClosedCurve(pen, points.Select(Function(p) New PointF(p.X, p.Y)).ToArray)
    End Sub

    Public Overrides Sub DrawClosedCurve(pen As Pen, points() As PointF)
        Call DrawClosedCurveCore(pen, points, 0.5F)
    End Sub

    Public Overrides Sub DrawCurve(pen As Pen, points() As Point)
        If points Is Nothing Then
            Return
        End If

        Call DrawCurve(pen, points.Select(Function(p) New PointF(p.X, p.Y)).ToArray)
    End Sub

    Public Overrides Sub DrawCurve(pen As Pen, points() As PointF)
        Call DrawCurveCore(pen, points, 0, points.Length - 1, 0.5F)
    End Sub

    Public Overrides Sub DrawCurve(pen As Pen, points() As PointF, tension As Single)
        Call DrawCurveCore(pen, points, 0, points.Length - 1, tension)
    End Sub

    Public Overrides Sub DrawCurve(pen As Pen, points() As Point, tension As Single)
        If points Is Nothing Then
            Return
        End If

        Call DrawCurve(pen, points.Select(Function(p) New PointF(p.X, p.Y)).ToArray, tension)
    End Sub

    Public Overrides Sub DrawCurve(pen As Pen, points() As PointF, offset As Integer, numberOfSegments As Integer)
        Call DrawCurveCore(pen, points, offset, numberOfSegments, 0.5F)
    End Sub

    Public Overrides Sub DrawCurve(pen As Pen, points() As PointF, offset As Integer, numberOfSegments As Integer, tension As Single)
        Call DrawCurveCore(pen, points, offset, numberOfSegments, tension)
    End Sub

    Public Overrides Sub DrawCurve(pen As Pen, points() As Point, offset As Integer, numberOfSegments As Integer, tension As Single)
        If points Is Nothing Then
            Return
        End If

        Call DrawCurve(pen, points.Select(Function(p) New PointF(p.X, p.Y)).ToArray, offset, numberOfSegments, tension)
    End Sub

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Overrides Sub DrawEllipse(pen As Pen, rect As Rectangle)
        Call DrawEllipse(pen, rect.X, rect.Y, rect.Width, rect.Height)
    End Sub

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Overrides Sub DrawEllipse(pen As Pen, rect As RectangleF)
        Call DrawEllipse(pen, rect.X, rect.Y, rect.Width, rect.Height)
    End Sub

    Public Overrides Sub DrawEllipse(pen As Pen, x As Single, y As Single, width As Single, height As Single)
        Using stroke As SKPaint = pen.CreatePaint
            m_canvas.DrawOval(New SKRect(x, y, x + width, y + height), stroke)
        End Using
    End Sub

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Overrides Sub DrawEllipse(pen As Pen, x As Integer, y As Integer, width As Integer, height As Integer)
        Call DrawEllipse(pen, CSng(x), CSng(y), CSng(width), CSng(height))
    End Sub

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Overrides Sub DrawImage(image As Image, point As Point)
        Call DrawImage(image, CSng(point.X), CSng(point.Y), CSng(image.Width), CSng(image.Height))
    End Sub

    Public Overrides Sub DrawImage(image As Image, destPoints() As Point)
        If destPoints Is Nothing OrElse destPoints.Length < 3 Then
            If destPoints IsNot Nothing AndAlso destPoints.Length >= 1 Then
                Call DrawImage(image, CSng(destPoints(0).X), CSng(destPoints(0).Y))
            End If

            Return
        End If

        Call DrawImage(image, destPoints.Select(Function(p) New PointF(p.X, p.Y)).ToArray)
    End Sub

    Public Overrides Sub DrawImage(image As Image, destPoints() As PointF)
        If image Is Nothing OrElse destPoints Is Nothing OrElse destPoints.Length < 3 Then
            If image IsNot Nothing AndAlso destPoints IsNot Nothing AndAlso destPoints.Length >= 1 Then
                Call DrawImage(image, destPoints(0).X, destPoints(0).Y)
            End If

            Return
        End If

        Dim w As Single = CSng(image.Width)
        Dim h As Single = CSng(image.Height)
        Dim map As SKMatrix

        If destPoints.Length = 3 Then
            Dim p0 = destPoints(0), p1 = destPoints(1), p2 = destPoints(2)
            Dim invW = 1.0F / w
            Dim invH = 1.0F / h

            map = New SKMatrix With {
                .ScaleX = (p1.X - p0.X) * invW,
                .SkewX = (p2.X - p0.X) * invH,
                .TransX = p0.X,
                .SkewY = (p1.Y - p0.Y) * invW,
                .ScaleY = (p2.Y - p0.Y) * invH,
                .TransY = p0.Y,
                .Persp0 = 0,
                .Persp1 = 0,
                .Persp2 = 1
            }
        Else
            Dim src = {New PointF(0, 0), New PointF(w, 0), New PointF(0, h), New PointF(w, h)}
            map = ComputeHomography(src, {destPoints(0), destPoints(1), destPoints(2), destPoints(3)})
        End If

        Dim current As SKMatrix = m_canvas.TotalMatrix

        Call m_canvas.Save()
        Call m_canvas.SetMatrix(SKMatrix.Concat(current, map))

        Using paint As New SKPaint With {.IsAntialias = True}
            Call m_canvas.DrawImage(image.AsSKImage, New SKRect(0, 0, w, h), SKSamplingOptions.Default, paint)
        End Using

        Call m_canvas.Restore()
    End Sub

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Overrides Sub DrawImage(image As Image, rect As Rectangle)
        Call DrawImage(image, CSng(rect.Left), CSng(rect.Top), CSng(rect.Width), CSng(rect.Height))
    End Sub

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Overrides Sub DrawImage(image As Image, point As PointF)
        Call DrawImage(image, CSng(point.X), CSng(point.Y), CSng(image.Width), CSng(image.Height))
    End Sub

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Overrides Sub DrawImage(image As Image, rect As RectangleF)
        Call DrawImage(image, rect.Left, rect.Top, rect.Width, rect.Height)
    End Sub

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Overrides Sub DrawImage(image As Image, x As Integer, y As Integer)
        Call DrawImage(image, x, y, image.Width, image.Height)
    End Sub

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Overrides Sub DrawImage(image As Image, x As Single, y As Single)
        Call DrawImage(image, x, y, image.Width, image.Height)
    End Sub

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Overrides Sub DrawImage(image As Image, x As Single, y As Single, width As Single, height As Single)
        Using blender As New SKPaint With {
            .IsAntialias = True
        }
            Dim rect As New SKRect(x, y, x + width, y + height)
            Dim opt = SKSamplingOptions.Default

            Call m_canvas.DrawImage(image.AsSKImage, rect, opt, blender)
        End Using
    End Sub

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Overrides Sub DrawImage(image As Image, x As Integer, y As Integer, width As Integer, height As Integer)
        Call DrawImage(image, CSng(x), CSng(y), CSng(width), CSng(height))
    End Sub

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Overrides Sub DrawImageUnscaled(image As Image, rect As Rectangle)
        Call DrawImage(image, CSng(rect.Left), CSng(rect.Top), CSng(image.Width), CSng(image.Height))
    End Sub

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Overrides Sub DrawImageUnscaled(image As Image, point As Point)
        Call DrawImage(image, CSng(point.X), CSng(point.Y), CSng(image.Width), CSng(image.Height))
    End Sub

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Overrides Sub DrawImageUnscaled(image As Image, x As Integer, y As Integer)
        Call DrawImage(image, CSng(x), CSng(y), CSng(image.Width), CSng(image.Height))
    End Sub

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Overrides Sub DrawImageUnscaled(image As Image, x As Integer, y As Integer, width As Integer, height As Integer)
        Call DrawImage(image, CSng(x), CSng(y), CSng(width), CSng(height))
    End Sub

    Public Overrides Sub DrawImageUnscaledAndClipped(image As Image, rect As Rectangle)
        If image Is Nothing Then
            Return
        End If

        Dim w As Single = CSng(image.Width)
        Dim h As Single = CSng(image.Height)

        Call m_canvas.Save()
        Call m_canvas.ClipRect(New SKRect(rect.X, rect.Y, rect.X + rect.Width, rect.Y + rect.Height), SKClipOperation.Intersect, True)

        Using paint As New SKPaint With {.IsAntialias = True}
            Call m_canvas.DrawImage(image.AsSKImage, New SKRect(0, 0, w, h), SKSamplingOptions.Default, paint)
        End Using

        Call m_canvas.Restore()
    End Sub

    Public Overrides Sub DrawLines(pen As Pen, points() As PointF)
        Dim pt1 As PointF
        Dim pt2 As PointF
        Dim stroke As SKPathEffect = pen.LineDashStyle

        For Each line As SlideWindow(Of PointF) In points.SlideWindows(2)
            pt1 = line(0)
            pt2 = line(1)

            Call DrawLine(pt1.X, pt1.Y, pt2.X, pt2.Y, pen.Color, pen.Width, stroke)
        Next
    End Sub

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Overrides Sub DrawLines(pen As Pen, points() As Point)
        Call DrawLines(pen, points.Select(Function(p) New PointF(p.X, p.Y)).ToArray)
    End Sub

    Public Overrides Sub DrawPie(pen As Pen, rect As Rectangle, startAngle As Single, sweepAngle As Single)
        Call DrawPie(pen, CSng(rect.X), CSng(rect.Y), CSng(rect.Width), CSng(rect.Height), startAngle, sweepAngle)
    End Sub

    Public Overrides Sub DrawPie(pen As Pen, rect As RectangleF, startAngle As Single, sweepAngle As Single)
        Call DrawPie(pen, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle)
    End Sub

    Public Overrides Sub DrawPie(pen As Pen, x As Integer, y As Integer, width As Integer, height As Integer, startAngle As Integer, sweepAngle As Integer)
        Call DrawPie(pen, CSng(x), CSng(y), CSng(width), CSng(height), CSng(startAngle), CSng(sweepAngle))
    End Sub

    Public Overrides Sub DrawPie(pen As Pen, x As Single, y As Single, width As Single, height As Single, startAngle As Single, sweepAngle As Single)
        Using path As SKPath = BuildPiePath(x, y, width, height, startAngle, sweepAngle),
              paint As SKPaint = pen.CreatePaint
            Call m_canvas.DrawPath(path, paint)
        End Using
    End Sub

    Public Overrides Sub DrawCircle(center As PointF, fill As Color, stroke As Pen, radius As Single)
        Call FillEllipse(New SolidBrush(fill), New RectangleF(center.X - radius, center.Y - radius, radius, radius))

        If stroke IsNot Nothing Then
            Call DrawEllipse(stroke, New RectangleF(center.X - radius, center.Y - radius, radius, radius))
        End If
    End Sub

    Public Overrides Sub DrawPolygon(pen As Pen, points() As PointF)
        If points.TryCount <= 2 Then
            Return
        End If

        Using path As New SKPath
            Call path.MoveTo(points(0).X, points(0).Y)

            For i As Integer = 1 To points.Length - 1
                Call path.LineTo(points(i).X, points(i).Y)
            Next

            Call path.Close()

            Using paint As New SKPaint With {
                .Color = pen.Color.AsSKColor,
                .Style = SKPaintStyle.Stroke,
                .StrokeWidth = pen.Width
            }
                Call m_canvas.DrawPath(path, paint)
            End Using
        End Using
    End Sub

    Public Overrides Sub DrawPolygon(pen As Pen, points() As Point)
        If points.TryCount > 2 Then
            Call DrawPolygon(pen, points.Select(Function(p) New PointF(p.X, p.Y)).ToArray)
        End If
    End Sub

    Public Overrides Sub DrawRectangle(pen As Pen, rect As Rectangle)
        Call DrawRectangle(pen, rect.Left, rect.Top, rect.Width, rect.Height)
    End Sub

    Public Overrides Sub DrawRectangle(pen As Pen, rect As RectangleF)
        Call DrawRectangle(pen, rect.Left, rect.Top, rect.Width, rect.Height)
    End Sub

    Public Overrides Sub DrawRectangle(pen As Pen, x As Single, y As Single, width As Single, height As Single)
        Using paint As New SKPaint With {
            .Color = pen.Color.AsSKColor,
            .StrokeWidth = pen.Width,
            .Style = SKPaintStyle.Stroke
        }
            Call m_canvas.DrawRect(x, y, width, height, paint)
        End Using
    End Sub

    Public Overrides Sub DrawRectangle(pen As Pen, x As Integer, y As Integer, width As Integer, height As Integer)
        Call DrawRectangle(pen, CSng(x), CSng(y), CSng(width), CSng(height))
    End Sub

    Public Overrides Sub DrawRectangles(pen As Pen, rects() As RectangleF)
        If rects Is Nothing Then
            Return
        End If

        For Each rect As RectangleF In rects
            Call DrawRectangle(pen, rect)
        Next
    End Sub

    Public Overrides Sub DrawRectangles(pen As Pen, rects() As Rectangle)
        If rects Is Nothing Then
            Return
        End If

        For Each rect As Rectangle In rects
            Call DrawRectangle(pen, rect)
        Next
    End Sub

    Public Overrides Sub ExcludeClip(rect As Rectangle)
        If m_clipSaveCount < 0 Then
            m_clipSaveCount = m_canvas.Save()
        End If

        Call m_canvas.ClipRect(New SKRect(rect.X, rect.Y, rect.X + rect.Width, rect.Y + rect.Height), SKClipOperation.Difference, True)
    End Sub

    Public Overrides Sub FillClosedCurve(brush As Brush, points() As PointF)
        Call FillClosedCurveCore(brush, points, 0.5F)
    End Sub

    Public Overrides Sub FillClosedCurve(brush As Brush, points() As Point)
        If points Is Nothing Then
            Return
        End If

        Call FillClosedCurve(brush, points.Select(Function(p) New PointF(p.X, p.Y)).ToArray)
    End Sub

    Public Overrides Sub FillEllipse(brush As Brush, rect As Rectangle)
        Call FillEllipse(brush, CSng(rect.Left), CSng(rect.Top), CSng(rect.Width), CSng(rect.Height))
    End Sub

    Public Overrides Sub FillEllipse(brush As Brush, rect As RectangleF)
        Call FillEllipse(brush, rect.Left, rect.Top, rect.Width, rect.Height)
    End Sub

    Public Overrides Sub FillEllipse(brush As Brush, x As Single, y As Single, width As Single, height As Single)
        Using paint As SKPaint = brush.CreatePaint()
            Call m_canvas.DrawOval(x, y, width, height, paint)
        End Using
    End Sub

    Public Overrides Sub FillEllipse(brush As Brush, x As Integer, y As Integer, width As Integer, height As Integer)
        Call FillEllipse(brush, CSng(x), CSng(y), CSng(width), CSng(height))
    End Sub

    Public Overrides Sub FillPath(brush As Brush, path As GraphicsPath)
        Using skia As SKPath = PathBuilder.CreatePath(path),
            paint As SKPaint = brush.CreatePaint()
            Call m_canvas.DrawPath(skia, paint)
        End Using
    End Sub

    Public Overrides Sub FillPie(brush As Brush, rect As Rectangle, startAngle As Single, sweepAngle As Single)
        FillPie(brush, rect.Left, rect.Top, rect.Width, rect.Height, startAngle, sweepAngle)
    End Sub

    Public Overrides Sub FillPie(brush As Brush, x As Integer, y As Integer, width As Integer, height As Integer, startAngle As Integer, sweepAngle As Integer)
        FillPie(brush, CSng(x), CSng(y), CSng(width), CSng(height), CSng(startAngle), CSng(sweepAngle))
    End Sub

    Public Overrides Sub FillPie(brush As Brush, x As Single, y As Single, width As Single, height As Single, startAngle As Single, sweepAngle As Single)
        Using path As New SKPath
            Using paint As New SKPaint With {
                .Style = SKPaintStyle.Fill,
                .Color = DirectCast(brush, SolidBrush).Color.AsSKColor
            }

                Call m_canvas.DrawArc(New SKRect(x, y, x + width, y + height), startAngle, sweepAngle, True, paint)
            End Using
        End Using
    End Sub

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Overrides Sub FillPolygon(brush As Brush, points() As Point)
        If points.TryCount > 2 Then
            Call FillPolygon(brush, points.Select(Function(p) New PointF(p.X, p.Y)).ToArray)
        End If
    End Sub

    Public Overrides Sub FillPolygon(brush As Brush, points() As PointF)
        If points.TryCount <= 2 Then
            Return
        End If

        Using path As New SKPath
            Call path.MoveTo(points(0).X, points(0).Y)

            For i As Integer = 1 To points.Length - 1
                Call path.LineTo(points(i).X, points(i).Y)
            Next

            Call path.Close()

            Using paint As SKPaint = brush.CreatePaint()
                Call m_canvas.DrawPath(path, paint)
            End Using
        End Using
    End Sub

    Public Overrides Sub FillRectangle(brush As Brush, rect As Rectangle)
        Call FillRectangle(brush, rect.Left, rect.Top, rect.Width, rect.Height)
    End Sub

    Public Overrides Sub FillRectangle(brush As Brush, rect As RectangleF)
        Call FillRectangle(brush, rect.Left, rect.Top, rect.Width, rect.Height)
    End Sub

    Public Overrides Sub FillRectangle(brush As Brush, x As Integer, y As Integer, width As Integer, height As Integer)
        Call FillRectangle(brush, CSng(x), CSng(y), CSng(width), CSng(height))
    End Sub

    Public Overrides Sub FillRectangle(brush As Brush, x As Single, y As Single, width As Single, height As Single)
        Using paint As SKPaint = brush.CreatePaint()
            Call m_canvas.DrawRect(New SKRect(x, y, x + width, y + height), paint)
        End Using
    End Sub

    Public Overrides Sub Flush()
        If Not m_isDisposed Then
            Call m_canvas.Flush()
        End If
    End Sub

    Public Overrides Sub IntersectClip(rect As RectangleF)
        If m_clipSaveCount < 0 Then
            m_clipSaveCount = m_canvas.Save()
        End If

        Call m_canvas.ClipRect(rect.AsRectangle, SKClipOperation.Intersect, True)

        If m_lastClipRect.HasValue Then
            m_lastClipRect = RectangleF.Intersect(m_lastClipRect.Value, rect)
        Else
            m_lastClipRect = rect
        End If
    End Sub

    Public Overrides Sub IntersectClip(rect As Rectangle)
        Call IntersectClip(New RectangleF(rect.X, rect.Y, rect.Width, rect.Height))
    End Sub

    Public Overrides Sub ResetClip()
        If m_clipSaveCount >= 0 Then
            Call m_canvas.RestoreToCount(m_clipSaveCount)
            m_clipSaveCount = -1
        End If

        m_lastClipRect = Nothing
    End Sub

    Public Overrides Sub ResetTransform()
        Call m_canvas.ResetMatrix()
    End Sub

    Public Overrides Sub RotateTransform(angle As Single)
        Call m_canvas.RotateDegrees(angle)
    End Sub

    Public Overrides Sub ScaleTransform(sx As Single, sy As Single)
        Call m_canvas.Scale(sx, sy)
    End Sub

    Public Overrides Sub SetClip(rect As RectangleF)
        If m_clipSaveCount >= 0 Then
            Call m_canvas.RestoreToCount(m_clipSaveCount)
        End If

        m_clipSaveCount = m_canvas.Save()
        Call m_canvas.ClipRect(rect.AsRectangle, SKClipOperation.Difference, True)
        m_lastClipRect = rect
    End Sub

    Public Overrides Sub SetClip(rect As Rectangle)
        Call SetClip(New RectangleF(rect.X, rect.Y, rect.Width, rect.Height))
    End Sub

    Public Overrides Sub TranslateClip(dx As Single, dy As Single)
        If m_lastClipRect.HasValue Then
            Dim r As RectangleF = m_lastClipRect.Value
            r.Offset(dx, dy)
            Call SetClip(r)
        End If
    End Sub

    Public Overrides Sub TranslateClip(dx As Integer, dy As Integer)
        Call TranslateClip(CSng(dx), CSng(dy))
    End Sub

    Public Overrides Sub TranslateTransform(dx As Single, dy As Single)
        Call m_canvas.Translate(dx, dy)
    End Sub

    Protected Overrides Sub ClearCanvas(color As Color)
        m_canvas.Clear(color.AsSKColor)
    End Sub

    Public Overloads Function MeasureString(text As String, fontName As String, fontSize As Single) As (Width As Single, Height As Single)
        Using paint As New SKPaint With {.IsAntialias = True},
            font As New SKFont(typeface:=SKTypeface.FromFamilyName(fontName), size:=fontSize)

            Dim textBounds As New SKRect
            Call font.MeasureText(text, textBounds, paint)

            Return (textBounds.Width, textBounds.Height)
        End Using
    End Function

    Public Overrides Function MeasureString(text As String, font As Font) As SizeF
        Return SkiaDriver.MeasureString(text, font)
    End Function

    Public Overrides Function MeasureString(text As String, font As Font, width As Integer) As SizeF
        Return SkiaDriver.MeasureString(text, font)
    End Function

    Public Overrides Function MeasureString(text As String, font As Font, layoutArea As SizeF) As SizeF
        Return SkiaDriver.MeasureString(text, font)
    End Function

    Public Overrides Function GetContextInfo() As Object
        Return m_canvas
    End Function

    Public Overrides Function IsVisible(rect As Rectangle) As Boolean
        Return IsVisible(New RectangleF(rect.X, rect.Y, rect.Width, rect.Height))
    End Function

    Public Overrides Function IsVisible(rect As RectangleF) As Boolean
        Dim canvasR As New RectangleF(canvasRect.Left, canvasRect.Top, canvasRect.Width, canvasRect.Height)

        If Not rect.IntersectsWith(canvasR) Then
            Return False
        End If

        If m_lastClipRect.HasValue Then
            Return rect.IntersectsWith(m_lastClipRect.Value)
        End If

        Return True
    End Function

    Public Overrides Function IsVisible(x As Integer, y As Integer, width As Integer, height As Integer) As Boolean
        Return IsVisible(New RectangleF(CSng(x), CSng(y), CSng(width), CSng(height)))
    End Function

    Public Overrides Function IsVisible(x As Single, y As Single, width As Single, height As Single) As Boolean
        Return IsVisible(New RectangleF(x, y, width, height))
    End Function

    Public Overrides Function GetStringPath(s As String, rect As RectangleF, font As Font) As GraphicsPath
        Dim path As New SKPath
        Dim x As Single = rect.X
        Dim y As Single = rect.Y

        Using style As New SKFont() With {.Size = font.Size, .Typeface = font.CreateSkiaTypeface}
            path = style.GetTextPath(s, New SKPoint(x, y))
        End Using

        Dim glyphs = path.GetPoints(path.PointCount)
        Dim points = glyphs.Select(Function(p) New PointF(p.X, p.Y))

        Return New GraphicsPath(points)
    End Function

    ''' <summary>
    ''' build a catmull-rom spline path from the given points. the tension default
    ''' in gdi+ is 0.5, and the bezier control points are derived as
    ''' cp1 = p1 + (p2 - p0) * tension / 3, cp2 = p2 - (p3 - p1) * tension / 3.
    ''' </summary>
    Private Function BuildSplinePath(points() As PointF, tension As Single, closed As Boolean) As SKPath
        Dim path As New SKPath
        Dim n As Integer = points.Length

        If n < 2 Then
            Return path
        End If

        Call path.MoveTo(points(0).X, points(0).Y)

        Dim tf As Single = tension / 3.0F

        If closed Then
            For i As Integer = 0 To n - 1
                Dim p0 As PointF = points((i - 1 + n) Mod n)
                Dim p1 As PointF = points(i)
                Dim p2 As PointF = points((i + 1) Mod n)
                Dim p3 As PointF = points((i + 2) Mod n)

                Call path.CubicTo(p1.X + (p2.X - p0.X) * tf, p1.Y + (p2.Y - p0.Y) * tf,
                                  p2.X - (p3.X - p1.X) * tf, p2.Y - (p3.Y - p1.Y) * tf,
                                  p2.X, p2.Y)
            Next

            Call path.Close()
        Else
            For i As Integer = 0 To n - 2
                Dim p0 As PointF = points(If(i - 1 < 0, 0, i - 1))
                Dim p1 As PointF = points(i)
                Dim p2 As PointF = points(i + 1)
                Dim p3 As PointF = points(If(i + 2 > n - 1, n - 1, i + 2))

                Call path.CubicTo(p1.X + (p2.X - p0.X) * tf, p1.Y + (p2.Y - p0.Y) * tf,
                                  p2.X - (p3.X - p1.X) * tf, p2.Y - (p3.Y - p1.Y) * tf,
                                  p2.X, p2.Y)
            Next
        End If

        Return path
    End Function

    Private Sub DrawCurveCore(pen As Pen, points() As PointF, offset As Integer, numberOfSegments As Integer, tension As Single)
        If points Is Nothing Then
            Return
        End If

        Dim n As Integer = points.Length

        If n < 2 Then
            Return
        End If

        Dim segs As Integer = If(numberOfSegments <= 0, n - 1, numberOfSegments)

        If offset < 0 Then
            offset = 0
        End If

        If offset + segs > n - 1 Then
            segs = n - 1 - offset
        End If

        If segs < 1 Then
            Return
        End If

        Dim pts(segs) As PointF

        For i As Integer = 0 To segs
            pts(i) = points(offset + i)
        Next

        Using path As SKPath = BuildSplinePath(pts, tension, closed:=False),
              paint As SKPaint = pen.CreatePaint
            Call m_canvas.DrawPath(path, paint)
        End Using
    End Sub

    Private Sub DrawClosedCurveCore(pen As Pen, points() As PointF, tension As Single)
        If points Is Nothing OrElse points.Length < 3 Then
            Return
        End If

        Using path As SKPath = BuildSplinePath(points, tension, closed:=True),
              paint As SKPaint = pen.CreatePaint
            Call m_canvas.DrawPath(path, paint)
        End Using
    End Sub

    Private Sub FillClosedCurveCore(brush As Brush, points() As PointF, tension As Single)
        If points Is Nothing OrElse points.Length < 3 Then
            Return
        End If

        Using path As SKPath = BuildSplinePath(points, tension, closed:=True),
              paint As SKPaint = brush.CreatePaint()
            Call m_canvas.DrawPath(path, paint)
        End Using
    End Sub

    Private Function BuildPiePath(x As Single, y As Single, width As Single, height As Single, startAngle As Single, sweepAngle As Single) As SKPath
        Dim path As New SKPath
        Dim cx As Single = x + width / 2.0F
        Dim cy As Single = y + height / 2.0F
        Dim oval As New SKRect(x, y, x + width, y + height)

        Call path.ArcTo(oval, startAngle, sweepAngle, True)
        Call path.LineTo(cx, cy)
        Call path.Close()

        Return path
    End Function

    Private Function ComputeHomography(src() As PointF, dst() As PointF) As SKMatrix
        Dim A(7)() As Double
        Dim b(7) As Double

        For i As Integer = 0 To 3
            Dim x As Double = src(i).X
            Dim y As Double = src(i).Y
            Dim X2 As Double = dst(i).X
            Dim Y2 As Double = dst(i).Y

            A(2 * i) = {x, y, 1, 0, 0, 0, -x * X2, -y * X2}
            b(2 * i) = X2
            A(2 * i + 1) = {0, 0, 0, x, y, 1, -x * Y2, -y * Y2}
            b(2 * i + 1) = Y2
        Next

        Dim h As Double() = SolveLinear(A, b)

        If h Is Nothing Then
            Return SKMatrix.Identity
        End If

        Return New SKMatrix With {
            .ScaleX = CSng(h(0)),
            .SkewX = CSng(h(1)),
            .TransX = CSng(h(2)),
            .SkewY = CSng(h(3)),
            .ScaleY = CSng(h(4)),
            .TransY = CSng(h(5)),
            .Persp0 = CSng(h(6)),
            .Persp1 = CSng(h(7)),
            .Persp2 = 1
        }
    End Function

    Private Function SolveLinear(A()() As Double, b() As Double) As Double()
        Dim n As Integer = b.Length

        For i As Integer = 0 To n - 1
            Dim piv As Integer = i

            For k As Integer = i + 1 To n - 1
                If std.Abs(A(k)(i)) > std.Abs(A(piv)(i)) Then
                    piv = k
                End If
            Next

            If piv <> i Then
                Dim tmp() As Double = A(i)
                A(i) = A(piv)
                A(piv) = tmp

                Dim tb As Double = b(i)
                b(i) = b(piv)
                b(piv) = tb
            End If

            Dim div As Double = A(i)(i)

            If div = 0 Then
                Return Nothing
            End If

            For j As Integer = i To n - 1
                A(i)(j) /= div
            Next

            b(i) /= div

            For k As Integer = 0 To n - 1
                If k <> i Then
                    Dim f As Double = A(k)(i)

                    If f <> 0 Then
                        For j As Integer = i To n - 1
                            A(k)(j) -= f * A(i)(j)
                        Next

                        b(k) -= f * b(i)
                    End If
                End If
            Next
        Next

        Return b
    End Function

    Public MustOverride Sub Save(file As Stream)

    Public Function Save(filepath As String) As Boolean
        'Call m_canvas.Flush()
        'Call m_canvas.Dispose()

        Try
            Using s As Stream = filepath.Open(FileMode.OpenOrCreate, doClear:=True)
                Call Save(s)
            End Using
        Catch ex As Exception
            Call App.LogException(New Exception(filepath, ex))
            Return False
        End Try

        Return True
    End Function

    Public MustOverride Function Save(Stream As Stream, format As ImageFormats) As Boolean Implements SaveGdiBitmap.Save

End Class
