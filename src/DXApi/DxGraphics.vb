Imports System.Drawing
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Driver

Public Class DxGraphics : Inherits IGraphics

    Public Overrides ReadOnly Property Size As Size

    ''' <summary>
    ''' DirectX graphics engine always generates the raster image output
    ''' </summary>
    ''' <returns></returns>
    Public Overrides ReadOnly Property Driver As Drivers
        Get
            Return Drivers.GDI
        End Get
    End Property

    Public Overrides Property RenderingOrigin As Point

    Public Overrides Property TextContrast As Integer

    Public Sub New(dpi As Integer)
        MyBase.New(dpi)
    End Sub

    ''' <summary>
    ''' create canvas graphics with raaster image as drawing background
    ''' </summary>
    ''' <param name="background"></param>
    Sub New(background As Bitmap)

    End Sub

    Public Overrides Sub AddMetafileComment(data() As Byte)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawArc(pen As Pen, rect As System.Drawing.RectangleF, startAngle As Single, sweepAngle As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawArc(pen As Pen, rect As System.Drawing.Rectangle, startAngle As Single, sweepAngle As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawArc(pen As Pen, x As Integer, y As Integer, width As Integer, height As Integer, startAngle As Integer, sweepAngle As Integer)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawArc(pen As Pen, x As Single, y As Single, width As Single, height As Single, startAngle As Single, sweepAngle As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawBezier(pen As Pen, pt1 As System.Drawing.Point, pt2 As System.Drawing.Point, pt3 As System.Drawing.Point, pt4 As System.Drawing.Point)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawBezier(pen As Pen, pt1 As System.Drawing.PointF, pt2 As System.Drawing.PointF, pt3 As System.Drawing.PointF, pt4 As System.Drawing.PointF)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawBezier(pen As Pen, x1 As Single, y1 As Single, x2 As Single, y2 As Single, x3 As Single, y3 As Single, x4 As Single, y4 As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawBeziers(pen As Pen, points() As System.Drawing.PointF)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawBeziers(pen As Pen, points() As System.Drawing.Point)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawClosedCurve(pen As Pen, points() As System.Drawing.Point)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawClosedCurve(pen As Pen, points() As System.Drawing.PointF)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawCurve(pen As Pen, points() As System.Drawing.Point)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawCurve(pen As Pen, points() As System.Drawing.PointF)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawCurve(pen As Pen, points() As System.Drawing.PointF, tension As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawCurve(pen As Pen, points() As System.Drawing.Point, tension As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawCurve(pen As Pen, points() As System.Drawing.PointF, offset As Integer, numberOfSegments As Integer)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawCurve(pen As Pen, points() As System.Drawing.PointF, offset As Integer, numberOfSegments As Integer, tension As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawCurve(pen As Pen, points() As System.Drawing.Point, offset As Integer, numberOfSegments As Integer, tension As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawEllipse(pen As Pen, rect As System.Drawing.Rectangle)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawEllipse(pen As Pen, rect As System.Drawing.RectangleF)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawEllipse(pen As Pen, x As Single, y As Single, width As Single, height As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawEllipse(pen As Pen, x As Integer, y As Integer, width As Integer, height As Integer)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawImage(image As Image, point As System.Drawing.Point)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawImage(image As Image, destPoints() As System.Drawing.Point)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawImage(image As Image, destPoints() As System.Drawing.PointF)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawImage(image As Image, rect As System.Drawing.Rectangle)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawImage(image As Image, point As System.Drawing.PointF)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawImage(image As Image, rect As System.Drawing.RectangleF)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawImage(image As Image, x As Integer, y As Integer)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawImage(image As Image, x As Single, y As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawImage(image As Image, x As Single, y As Single, width As Single, height As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawImage(image As Image, x As Integer, y As Integer, width As Integer, height As Integer)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawImageUnscaled(image As Image, rect As System.Drawing.Rectangle)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawImageUnscaled(image As Image, point As System.Drawing.Point)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawImageUnscaled(image As Image, x As Integer, y As Integer)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawImageUnscaled(image As Image, x As Integer, y As Integer, width As Integer, height As Integer)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawImageUnscaledAndClipped(image As Image, rect As System.Drawing.Rectangle)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawLine(pen As Pen, pt1 As System.Drawing.PointF, pt2 As System.Drawing.PointF)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawLine(pen As Pen, pt1 As System.Drawing.Point, pt2 As System.Drawing.Point)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawLine(pen As Pen, x1 As Integer, y1 As Integer, x2 As Integer, y2 As Integer)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawLine(pen As Pen, x1 As Single, y1 As Single, x2 As Single, y2 As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawLines(pen As Pen, points() As System.Drawing.PointF)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawLines(pen As Pen, points() As System.Drawing.Point)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawPath(pen As Pen, path As GraphicsPath)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawPie(pen As Pen, rect As System.Drawing.Rectangle, startAngle As Single, sweepAngle As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawPie(pen As Pen, rect As System.Drawing.RectangleF, startAngle As Single, sweepAngle As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawPie(pen As Pen, x As Integer, y As Integer, width As Integer, height As Integer, startAngle As Integer, sweepAngle As Integer)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawPie(pen As Pen, x As Single, y As Single, width As Single, height As Single, startAngle As Single, sweepAngle As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawCircle(center As System.Drawing.PointF, fill As System.Drawing.Color, stroke As Pen, radius As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawPolygon(pen As Pen, points() As System.Drawing.PointF)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawPolygon(pen As Pen, points() As System.Drawing.Point)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawRectangle(pen As Pen, rect As System.Drawing.Rectangle)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawRectangle(pen As Pen, rect As System.Drawing.RectangleF)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawRectangle(pen As Pen, x As Single, y As Single, width As Single, height As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawRectangle(pen As Pen, x As Integer, y As Integer, width As Integer, height As Integer)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawRectangles(pen As Pen, rects() As System.Drawing.RectangleF)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawRectangles(pen As Pen, rects() As System.Drawing.Rectangle)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawString(s As String, font As Font, brush As Brush, ByRef point As System.Drawing.PointF)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawString(s As String, font As Font, brush As Brush, layoutRectangle As System.Drawing.RectangleF)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawString(s As String, font As Font, brush As Brush, ByRef x As Single, ByRef y As Single, angle As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub DrawString(s As String, font As Font, brush As Brush, x As Single, y As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub ExcludeClip(rect As System.Drawing.Rectangle)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub FillClosedCurve(brush As Brush, points() As System.Drawing.PointF)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub FillClosedCurve(brush As Brush, points() As System.Drawing.Point)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub FillEllipse(brush As Brush, rect As System.Drawing.Rectangle)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub FillEllipse(brush As Brush, rect As System.Drawing.RectangleF)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub FillEllipse(brush As Brush, x As Single, y As Single, width As Single, height As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub FillEllipse(brush As Brush, x As Integer, y As Integer, width As Integer, height As Integer)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub FillPath(brush As Brush, path As GraphicsPath)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub FillPie(brush As Brush, rect As System.Drawing.Rectangle, startAngle As Single, sweepAngle As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub FillPie(brush As Brush, x As Integer, y As Integer, width As Integer, height As Integer, startAngle As Integer, sweepAngle As Integer)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub FillPie(brush As Brush, x As Single, y As Single, width As Single, height As Single, startAngle As Single, sweepAngle As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub FillPolygon(brush As Brush, points() As System.Drawing.Point)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub FillPolygon(brush As Brush, points() As System.Drawing.PointF)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub FillRectangle(brush As Brush, rect As System.Drawing.Rectangle)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub FillRectangle(brush As Brush, rect As System.Drawing.RectangleF)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub FillRectangle(brush As Brush, x As Integer, y As Integer, width As Integer, height As Integer)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub FillRectangle(brush As Brush, x As Single, y As Single, width As Single, height As Single)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub Flush()
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub IntersectClip(rect As System.Drawing.RectangleF)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub IntersectClip(rect As System.Drawing.Rectangle)
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

    Public Overrides Sub SetClip(rect As System.Drawing.RectangleF)
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub SetClip(rect As System.Drawing.Rectangle)
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

    Protected Overrides Sub ClearCanvas(color As System.Drawing.Color)
        Throw New NotImplementedException()
    End Sub

    Protected Overrides Sub ReleaseHandle()
        Throw New NotImplementedException()
    End Sub

    Public Overrides Function GetContextInfo() As GraphicsContextInfo
        Throw New NotImplementedException()
    End Function

    Public Overrides Function IsVisible(rect As System.Drawing.Rectangle) As Boolean
        Throw New NotImplementedException()
    End Function

    Public Overrides Function IsVisible(rect As System.Drawing.RectangleF) As Boolean
        Throw New NotImplementedException()
    End Function

    Public Overrides Function IsVisible(x As Integer, y As Integer, width As Integer, height As Integer) As Boolean
        Throw New NotImplementedException()
    End Function

    Public Overrides Function IsVisible(x As Single, y As Single, width As Single, height As Single) As Boolean
        Throw New NotImplementedException()
    End Function

    Public Overrides Function MeasureString(text As String, font As Font) As System.Drawing.SizeF
        Throw New NotImplementedException()
    End Function

    Public Overrides Function MeasureString(text As String, font As Font, width As Integer) As System.Drawing.SizeF
        Throw New NotImplementedException()
    End Function

    Public Overrides Function MeasureString(text As String, font As Font, layoutArea As System.Drawing.SizeF) As System.Drawing.SizeF
        Throw New NotImplementedException()
    End Function

    Public Overrides Function GetStringPath(s As String, rect As System.Drawing.RectangleF, font As Font) As GraphicsPath
        Throw New NotImplementedException()
    End Function
End Class
