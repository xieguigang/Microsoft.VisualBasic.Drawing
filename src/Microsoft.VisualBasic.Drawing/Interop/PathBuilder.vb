Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Math
Imports SkiaSharp

Module PathBuilder

    <Extension>
    Public Function CreatePath(path As GraphicsPath) As SKPath
        Dim skia As New SKPath

        For Each op As GraphicsPath.op In path.AsEnumerable
            Select Case op.GetType
                Case GetType(GraphicsPath.op_AddArc) : Call skia.AddArc(op)
                Case GetType(GraphicsPath.op_AddBezier) : Call skia.AddBezier(op)
                Case GetType(GraphicsPath.op_AddCurve) : Call skia.AddCurve(op)
                Case GetType(GraphicsPath.op_AddEllipse) : Call skia.AddEllipse(op)
                Case GetType(GraphicsPath.op_AddLine) : Call skia.AddLine(op)
                Case GetType(GraphicsPath.op_AddLines) : Call skia.AddLines(op)
                Case GetType(GraphicsPath.op_AddPolygon) : Call skia.AddPolygon(op)
                Case GetType(GraphicsPath.op_AddRectangle) : Call skia.AddRectangle(op)

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
        Call skia.AddOval(New SKRect(ellipse.x, ellipse.y, ellipse.r1 + ellipse.x, ellipse.r2 + ellipse.y))
    End Sub

    <Extension>
    Private Sub AddCurve(skia As SKPath, curve As GraphicsPath.op_AddCurve)
        Call skia.AddPoly(curve.points.Select(Function(p) p.AsSKPoint).ToArray, close:=False)
    End Sub

    <Extension>
    Private Sub AddBezier(skia As SKPath, bezier As GraphicsPath.op_AddBezier)
        Call skia.MoveTo(bezier.pt1.AsSKPoint)
        Call skia.CubicTo(bezier.pt2.AsSKPoint, bezier.pt3.AsSKPoint, bezier.pt4.AsSKPoint)
    End Sub

    <Extension>
    Private Sub AddArc(skia As SKPath, arc As GraphicsPath.op_AddArc)
        Dim startAngleRad = Trigonometric.ToRadians(arc.startAngle)
        Dim sweepAngleRad = Trigonometric.ToRadians(arc.sweepAngle)
        Dim rx = arc.rect.Width / 2.0F
        Dim ry = arc.rect.Height / 2.0F
        Dim cx = arc.rect.Left + rx
        Dim cy = arc.rect.Top + ry

        Call skia.ArcTo(
            New SKRect(arc.rect.Left, arc.rect.Top, arc.rect.Right, arc.rect.Bottom),
            startAngleRad,
            sweepAngleRad,
            False
        )
    End Sub
End Module
