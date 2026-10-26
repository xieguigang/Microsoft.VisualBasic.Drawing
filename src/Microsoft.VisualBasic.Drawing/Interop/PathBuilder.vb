Imports System.Drawing
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.GraphicsPath
Imports Microsoft.VisualBasic.Linq
Imports SkiaSharp

Module PathBuilder

    <Extension>
    Public Function CreatePath(path As GraphicsPath) As SKPath
        Dim skia As New SKPath

        ' 同步填充规则：GDI+ 默认 Alternate（奇偶），SKPath 默认 Winding（非零）
        skia.FillType = If(path.FillMode = Imaging.FillMode.Alternate, SKPathFillType.EvenOdd, SKPathFillType.Winding)

        For Each op As GraphicsPath.op In path.AsEnumerable
            Select Case op.GetType
                Case GetType(GraphicsPath.op_AddArc) : Call skia.AddArc(DirectCast(op, op_AddArc))
                Case GetType(GraphicsPath.op_AddBezier) : Call skia.AddBezier(DirectCast(op, op_AddBezier))
                Case GetType(GraphicsPath.op_AddCurve) : Call skia.AddCurve(DirectCast(op, op_AddCurve))
                Case GetType(GraphicsPath.op_AddEllipse) : Call skia.AddEllipse(DirectCast(op, op_AddEllipse))
                Case GetType(GraphicsPath.op_AddLine) : Call skia.AddLine(DirectCast(op, op_AddLine))
                Case GetType(GraphicsPath.op_AddLines) : Call skia.AddLines(DirectCast(op, op_AddLines))
                Case GetType(GraphicsPath.op_AddPolygon) : Call skia.AddPolygon(DirectCast(op, op_AddPolygon))
                Case GetType(GraphicsPath.op_AddRectangle) : Call skia.AddRectangle(DirectCast(op, op_AddRectangle))

                Case GetType(GraphicsPath.op_Reset) : Call skia.Reset()
                Case GetType(GraphicsPath.op_CloseFigure) : Call skia.Close()
                Case GetType(GraphicsPath.op_CloseAllFigures) : Call skia.Close()

                Case Else
                    Throw New NotImplementedException(op.GetType.FullName)
            End Select
        Next

        Return skia
    End Function

    <Extension>
    Private Sub AddRectangle(skia As SKPath, rect As GraphicsPath.op_AddRectangle)
        Call skia.AddRect(rect.rect.AsRectangle)
    End Sub

    <Extension>
    Private Sub AddPolygon(skia As SKPath, polygon As GraphicsPath.op_AddPolygon)
        Call skia.AddPoly(polygon.points.Select(Function(p) p.AsSKPoint).ToArray, close:=True)
    End Sub

    <Extension>
    Private Sub AddLines(skia As SKPath, line As GraphicsPath.op_AddLines)
        Call skia.MoveTo(line.points(0).AsSKPoint)

        For i As Integer = 1 To line.points.Length - 1
            Call skia.LineTo(line.points(i).AsSKPoint)
        Next
    End Sub

    <Extension>
    Private Sub AddLine(skia As SKPath, line As GraphicsPath.op_AddLine)
        Call skia.MoveTo(line.a.AsSKPoint)
        Call skia.LineTo(line.b.AsSKPoint)
    End Sub

    <Extension>
    Private Sub AddEllipse(skia As SKPath, ellipse As GraphicsPath.op_AddEllipse)
        ' op_AddEllipse 里的 r1/r2 是"半径"，GDI+ 侧写的是 AddEllipse(x, y, r1 * 2, r2 * 2)；
        ' 这里之前漏掉了乘以 2，导致 Skia 画出来的椭圆只有 1/4 面积
        Call skia.AddOval(New SKRect(ellipse.x, ellipse.y, ellipse.x + ellipse.r1 * 2, ellipse.y + ellipse.r2 * 2))
    End Sub

    <Extension>
    Private Sub AddCurve(skia As SKPath, curve As GraphicsPath.op_AddCurve)
        Dim points As PointF() = curve.points

        If points.Length < 2 Then
            Return
        End If
        If points.Length = 2 Then
            Call skia.MoveTo(points(0).AsSKPoint)
            Call skia.LineTo(points(1).AsSKPoint)

            Return
        End If

        ' GDI+ 的 AddCurve 是"穿过所有点的基数样条"（tension 默认 0.5）；
        ' 用逐段折线近似会明显失真，这里换算成 Catmull-Rom 的三次贝塞尔控制点
        Call skia.MoveTo(points(0).AsSKPoint)

        For i As Integer = 0 To points.Length - 2
            Dim p1 As PointF = points(i)
            Dim p2 As PointF = points(i + 1)
            Dim p0 As PointF = If(i = 0, p1, points(i - 1))
            Dim p3 As PointF = If(i + 2 < points.Length, points(i + 2), p2)
            Dim c1 As New PointF(p1.X + (p2.X - p0.X) / 6, p1.Y + (p2.Y - p0.Y) / 6)
            Dim c2 As New PointF(p2.X - (p3.X - p1.X) / 6, p2.Y - (p3.Y - p1.Y) / 6)

            Call skia.CubicTo(c1.AsSKPoint, c2.AsSKPoint, p2.AsSKPoint)
        Next
    End Sub

    <Extension>
    Private Sub AddBezier(skia As SKPath, bezier As GraphicsPath.op_AddBezier)
        Call skia.MoveTo(bezier.pt1.AsSKPoint)
        Call skia.CubicTo(bezier.pt2.AsSKPoint, bezier.pt3.AsSKPoint, bezier.pt4.AsSKPoint)
    End Sub

    <Extension>
    Private Sub AddArc(skia As SKPath, arc As GraphicsPath.op_AddArc)
        ' GDI+ 的 AddArc 以"度"为单位，SKPath.ArcTo 也是"度"。
        ' 这里之前多转了一次弧度，导致圆角矩形/扇形/弧线的角度被缩小 57 倍而完全变形
        Call skia.ArcTo(
            New SKRect(arc.rect.Left, arc.rect.Top, arc.rect.Right, arc.rect.Bottom),
            CSng(arc.startAngle),
            CSng(arc.sweepAngle),
            False
        )
    End Sub
End Module
