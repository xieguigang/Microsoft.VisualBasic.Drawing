Imports System.Drawing
Imports System.IO
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Math2D
Imports SkiaSharp
Imports std = System.Math

Public MustInherit Class SkiaGraphics : Inherits IGraphics

    Protected ReadOnly canvasRect As SKRect
    Protected m_canvas As SKCanvas

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Sub New(width As Integer, height As Integer, dpi As Integer)
        Call MyBase.New(dpi)

        Size = New Size(width, height)
        canvasRect = SKRect.Create(width, height)
    End Sub

    Public Overrides ReadOnly Property Size As Size
    Public Overrides Property PageScale As Single
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
        Dim textPain As New SKPaint With {
           .IsAntialias = True,
           .Style = SKPaintStyle.Fill,
           .Color = color.AsSKColor,
           .TextSize = fontSize,
           .Typeface = SKTypeface.FromFamilyName(fontName)
        }

        m_canvas.DrawText(s, x, y, textPain)
    End Sub

    Public Overrides Sub DrawString(s As String, font As Font, brush As Brush, ByRef point As PointF)
        Call DrawString(s, font.Name, font.Size, DirectCast(brush, SolidBrush).Color, point.X, point.Y)
    End Sub

    Public Overrides Sub DrawString(s As String, font As Font, brush As Brush, layoutRectangle As RectangleF)
        Call DrawString(s, font.Name, font.Size, DirectCast(brush, SolidBrush).Color, layoutRectangle.Left, layoutRectangle.Top)
    End Sub

    Public Overrides Sub DrawString(s As String, font As Font, brush As Brush, ByRef x As Single, ByRef y As Single, angle As Single)
        Using paint As New SKPaint With {
                .TextSize = font.Size,
                .Color = DirectCast(brush, SolidBrush).Color.AsSKColor,
                .IsAntialias = True
            }

            Dim textBounds As New SKRect
            paint.MeasureText(s, textBounds)

            m_canvas.Translate(x, y)
            m_canvas.RotateDegrees(angle)
            m_canvas.DrawText(s, -textBounds.MidX, -textBounds.MidY, paint)
            m_canvas.RotateDegrees(-angle)
            m_canvas.Translate(-x, -y)
        End Using
    End Sub

    Public Overrides Sub DrawString(s As String, font As Font, brush As Brush, x As Single, y As Single)
        Call DrawString(s, font.Name, font.Size, DirectCast(brush, SolidBrush).Color, x, y)
    End Sub

    Public Overloads Sub DrawLine(x1 As Single, y1 As Single, x2 As Single, y2 As Single, color As Color, width As Single,
                                  Optional dash As Single() = Nothing)

        Using paint As New SKPaint With {
            .Color = color.AsSKColor,
            .StrokeWidth = width
        }
            If Not dash.IsNullOrEmpty Then
                paint.PathEffect = SKPathEffect.CreateDash(dash, 0)
            End If

            Call m_canvas.DrawLine(x1, y1, x2, y2, paint)
        End Using
    End Sub

    Public Overrides Sub DrawLine(pen As Pen, pt1 As PointF, pt2 As PointF)
        Call DrawLine(pt1.X, pt1.Y, pt2.X, pt2.Y, pen.Color, pen.Width)
    End Sub

    Public Overrides Sub DrawLine(pen As Pen, pt1 As Point, pt2 As Point)
        Call DrawLine(pt1.X, pt1.Y, pt2.X, pt2.Y, pen.Color, pen.Width)
    End Sub

    Public Overrides Sub DrawLine(pen As Pen, x1 As Integer, y1 As Integer, x2 As Integer, y2 As Integer)
        Call DrawLine(x1, y1, x2, y2, pen.Color, pen.Width)
    End Sub

    Public Overrides Sub DrawLine(pen As Pen, x1 As Single, y1 As Single, x2 As Single, y2 As Single)
        Call DrawLine(x1, y1, x2, y2, pen.Color, pen.Width)
    End Sub

    Public Overloads Sub DrawPath(path As Polygon2D, color As Color, width As Single,
                                  Optional fill As Color? = Nothing,
                                  Optional dash As Single() = Nothing)

        Using skpath As New SKPath
            Dim x = path.xpoints
            Dim y = path.ypoints

            Call skpath.MoveTo(x(0), y(0))

            For i As Integer = 1 To x.Length - 1
                Call skpath.LineTo(x(i), y(i))
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
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawArc(pen As Pen, rect As RectangleF, startAngle As Single, sweepAngle As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawArc(pen As Pen, rect As Rectangle, startAngle As Single, sweepAngle As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawArc(pen As Pen, x As Integer, y As Integer, width As Integer, height As Integer, startAngle As Integer, sweepAngle As Integer)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawArc(pen As Pen, x As Single, y As Single, width As Single, height As Single, startAngle As Single, sweepAngle As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawBezier(pen As Pen, pt1 As Point, pt2 As Point, pt3 As Point, pt4 As Point)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawBezier(pen As Pen, pt1 As PointF, pt2 As PointF, pt3 As PointF, pt4 As PointF)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawBezier(pen As Pen, x1 As Single, y1 As Single, x2 As Single, y2 As Single, x3 As Single, y3 As Single, x4 As Single, y4 As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawBeziers(pen As Pen, points() As PointF)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawBeziers(pen As Pen, points() As Point)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawClosedCurve(pen As Pen, points() As Point)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawClosedCurve(pen As Pen, points() As PointF)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawCurve(pen As Pen, points() As Point)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawCurve(pen As Pen, points() As PointF)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawCurve(pen As Pen, points() As PointF, tension As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawCurve(pen As Pen, points() As Point, tension As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawCurve(pen As Pen, points() As PointF, offset As Integer, numberOfSegments As Integer)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawCurve(pen As Pen, points() As PointF, offset As Integer, numberOfSegments As Integer, tension As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawCurve(pen As Pen, points() As Point, offset As Integer, numberOfSegments As Integer, tension As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawEllipse(pen As Pen, rect As Rectangle)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawEllipse(pen As Pen, rect As RectangleF)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawEllipse(pen As Pen, x As Single, y As Single, width As Single, height As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawEllipse(pen As Pen, x As Integer, y As Integer, width As Integer, height As Integer)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawImage(image As Imaging.Image, point As Point)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawImage(image As Imaging.Image, destPoints() As Point)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawImage(image As Imaging.Image, destPoints() As PointF)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawImage(image As Imaging.Image, rect As Rectangle)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawImage(image As Imaging.Image, point As PointF)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawImage(image As Imaging.Image, rect As RectangleF)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawImage(image As Imaging.Image, x As Integer, y As Integer)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawImage(image As Imaging.Image, x As Single, y As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawImage(image As Imaging.Image, x As Single, y As Single, width As Single, height As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawImage(image As Imaging.Image, x As Integer, y As Integer, width As Integer, height As Integer)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawImageUnscaled(image As Imaging.Image, rect As Rectangle)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawImageUnscaled(image As Imaging.Image, point As Point)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawImageUnscaled(image As Imaging.Image, x As Integer, y As Integer)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawImageUnscaled(image As Imaging.Image, x As Integer, y As Integer, width As Integer, height As Integer)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawImageUnscaledAndClipped(image As Imaging.Image, rect As Rectangle)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawLines(pen As Pen, points() As PointF)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawLines(pen As Pen, points() As Point)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawPie(pen As Pen, rect As Rectangle, startAngle As Single, sweepAngle As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawPie(pen As Pen, rect As RectangleF, startAngle As Single, sweepAngle As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawPie(pen As Pen, x As Integer, y As Integer, width As Integer, height As Integer, startAngle As Integer, sweepAngle As Integer)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawPie(pen As Pen, x As Single, y As Single, width As Single, height As Single, startAngle As Single, sweepAngle As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawCircle(center As PointF, fill As Color, stroke As Pen, radius As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawPolygon(pen As Pen, points() As PointF)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawPolygon(pen As Pen, points() As Point)
        Throw New NotImplementedException()
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
            .StrokeWidth = pen.Width
        }
            Call m_canvas.DrawRect(x, y, width, height, paint)
        End Using
    End Sub

    Public Overrides Sub DrawRectangle(pen As Pen, x As Integer, y As Integer, width As Integer, height As Integer)
        Call DrawRectangle(pen, CSng(x), CSng(y), CSng(width), CSng(height))
    End Sub

    Public Overrides Sub DrawRectangles(pen As Pen, rects() As RectangleF)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawRectangles(pen As Pen, rects() As Rectangle)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub ExcludeClip(rect As Rectangle)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub FillClosedCurve(brush As Brush, points() As PointF)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub FillClosedCurve(brush As Brush, points() As Point)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub FillEllipse(brush As Brush, rect As Rectangle)
        With rect.Centre
            Call FillEllipse(brush, .X, .Y, rect.Width, rect.Height)
        End With
    End Sub

    Public Overrides Sub FillEllipse(brush As Brush, rect As RectangleF)
        With rect.Centre
            Call FillEllipse(brush, .X, .Y, rect.Width, rect.Height)
        End With
    End Sub

    Public Overrides Sub FillEllipse(brush As Brush, x As Single, y As Single, width As Single, height As Single)
        Using paint As New SKPaint With {
            .Style = SKPaintStyle.Fill,
            .Color = DirectCast(brush, SolidBrush).Color.AsSKColor
        }
            Call m_canvas.DrawOval(x, y, width, height, paint)
        End Using
    End Sub

    Public Overrides Sub FillEllipse(brush As Brush, x As Integer, y As Integer, width As Integer, height As Integer)
        Call FillEllipse(brush, CSng(x), CSng(y), CSng(width), CSng(height))
    End Sub

    Public Overrides Sub FillPath(brush As Brush, path As GraphicsPath)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub FillPie(brush As Brush, rect As Rectangle, startAngle As Single, sweepAngle As Single)
        FillPie(brush, rect.Left, rect.Top, rect.Width, rect.Height, startAngle, sweepAngle)
    End Sub

    Public Overrides Sub FillPie(brush As Brush, x As Integer, y As Integer, width As Integer, height As Integer, startAngle As Integer, sweepAngle As Integer)
        FillPie(brush, CSng(x), CSng(y), CSng(width), CSng(height), CSng(startAngle), CSng(sweepAngle))
    End Sub

    Public Overrides Sub FillPie(brush As Brush, x As Single, y As Single, width As Single, height As Single, startAngle As Single, sweepAngle As Single)
        Using path As New SKPath
            Dim startRadians As Single = startAngle * (std.PI / 180)
            Dim sweepRadians As Single = sweepAngle * (std.PI / 180)
            Dim radiusX As Single = width / 2
            Dim radiusY As Single = height / 2

            Call path.MoveTo(New SKPoint(x, y))
            Call path.ArcTo(New SKRect(x - radiusX, y - radiusY, x + radiusX, y + radiusY),
                   startRadians, sweepRadians, False)
            Call path.LineTo(New SKPoint(x, y))

            Using paint As New SKPaint With {
                .Style = SKPaintStyle.Fill,
                .Color = DirectCast(brush, SolidBrush).Color.AsSKColor
            }
                Call m_canvas.DrawPath(path, paint)
            End Using
        End Using
    End Sub

    Public Overrides Sub FillPolygon(brush As Brush, points() As Point)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub FillPolygon(brush As Brush, points() As PointF)
        Throw New NotImplementedException()
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
        Using paint As New SKPaint With {
            .Style = SKPaintStyle.Fill,
            .Color = DirectCast(brush, SolidBrush).Color.AsSKColor
        }

            Call m_canvas.DrawRect(New SKRect(x, y, x + width, y + height), paint)
        End Using
    End Sub

    Public Overrides Sub Flush()
        m_canvas.Flush()
    End Sub

    Public Overrides Sub IntersectClip(rect As RectangleF)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub IntersectClip(rect As Rectangle)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub ResetClip()
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub ResetTransform()
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub RotateTransform(angle As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub ScaleTransform(sx As Single, sy As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub SetClip(rect As RectangleF)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub SetClip(rect As Rectangle)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub TranslateClip(dx As Single, dy As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub TranslateClip(dx As Integer, dy As Integer)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub TranslateTransform(dx As Single, dy As Single)
        Throw New NotImplementedException()
    End Sub

    Protected Overrides Sub ClearCanvas(color As Color)
        m_canvas.Clear(color.AsSKColor)
    End Sub

    Public Overloads Function MeasureString(text As String, fontName As String, fontSize As Single) As (Width As Single, Height As Single)
        Using paint As New SKPaint With {
            .TextSize = fontSize,
            .IsAntialias = True,
            .Typeface = SKTypeface.FromFamilyName(fontName)
        }

            Dim textBounds As New SKRect
            Call paint.MeasureText(text, textBounds)

            Return (textBounds.Width, textBounds.Height)
        End Using
    End Function

    Public Overrides Function MeasureString(text As String, font As Font) As SizeF
        Using paint As New SKPaint With {
            .TextSize = font.Size,
            .IsAntialias = True,
            .Typeface = SKTypeface.FromFamilyName(font.Name)
        }

            Dim textBounds As New SKRect
            Call paint.MeasureText(text, textBounds)

            Return New SizeF(textBounds.Width, textBounds.Height)
        End Using
    End Function

    Public Overrides Function MeasureString(text As String, font As Font, width As Integer) As SizeF
        Return MeasureString(text, font)
    End Function

    Public Overrides Function MeasureString(text As String, font As Font, layoutArea As SizeF) As SizeF
        Return MeasureString(text, font)
    End Function

    Public Overrides Function GetContextInfo() As Object
        Throw New NotImplementedException()
    End Function

    Public Overrides Function GetNearestColor(color As Color) As Color
        Throw New NotImplementedException()
    End Function

    Public Overrides Function IsVisible(rect As Rectangle) As Boolean
        Throw New NotImplementedException()
    End Function

    Public Overrides Function IsVisible(rect As RectangleF) As Boolean
        Throw New NotImplementedException()
    End Function

    Public Overrides Function IsVisible(x As Integer, y As Integer, width As Integer, height As Integer) As Boolean
        Throw New NotImplementedException()
    End Function

    Public Overrides Function IsVisible(x As Single, y As Single, width As Single, height As Single) As Boolean
        Throw New NotImplementedException()
    End Function

    Public MustOverride Sub Save(file As Stream)

    Public Function Save(filepath As String) As Boolean
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

End Class
