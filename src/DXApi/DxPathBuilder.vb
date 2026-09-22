Imports System.Drawing
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Linq
Imports std = System.Math

''' <summary>
''' The direct2d geometry constructor: converts the gdi+ style drawing
''' primitive into a ID2D1Geometry object so that it can be rasterized
''' by the gpu device.
''' </summary>
Friend Module DxPathBuilder

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Friend Function ToPoints(points As PointF()) As D2D1_POINT_2F()
        Dim buffer As D2D1_POINT_2F() = New D2D1_POINT_2F(points.Length - 1) {}

        For i As Integer = 0 To points.Length - 1
            buffer(i) = New D2D1_POINT_2F With {.x = points(i).X, .y = points(i).Y}
        Next

        Return buffer
    End Function

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Friend Function ToPoints(points As Point()) As D2D1_POINT_2F()
        Dim buffer As D2D1_POINT_2F() = New D2D1_POINT_2F(points.Length - 1) {}

        For i As Integer = 0 To points.Length - 1
            buffer(i) = New D2D1_POINT_2F With {.x = points(i).X, .y = points(i).Y}
        Next

        Return buffer
    End Function

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Friend Function Ellipse(rect As RectangleF) As D2D1_ELLIPSE
        Return New D2D1_ELLIPSE With {
            .point = New D2D1_POINT_2F With {
                .x = rect.Left + rect.Width / 2.0F,
                .y = rect.Top + rect.Height / 2.0F
            },
            .radiusX = rect.Width / 2.0F,
            .radiusY = rect.Height / 2.0F
        }
    End Function

    ' /********************************************************************************/
    '  geometry sink helpers
    ' /********************************************************************************/

    Private Function NewGeometry(factory As ID2D1Factory, ByRef sink As ID2D1GeometrySink) As ID2D1PathGeometry
        Dim geometry As ID2D1PathGeometry = Nothing

        Call ThrowIfFailed(factory.CreatePathGeometry(geometry), "ID2D1Factory::CreatePathGeometry")
        Call ThrowIfFailed(geometry.Open(sink), "ID2D1PathGeometry::Open")
        Call sink.SetFillMode(D2D1_FILL_MODE.WINDING)

        Return geometry
    End Function

    Private Sub CloseGeometry(sink As ID2D1GeometrySink)
        If sink IsNot Nothing Then
            Call ThrowIfFailed(sink.Close(), "ID2D1GeometrySink::Close")
            Call SafeRelease(sink)
        End If
    End Sub

    ''' <summary>
    ''' AddLines of the polygon, the first point is consumed by the BeginFigure
    ''' call so that only the remaining points are appended here.
    ''' </summary>
    Private Sub AddTail(sink As ID2D1GeometrySink, points As D2D1_POINT_2F())
        If points.Length <= 1 Then
            Return
        End If

        Dim tail As D2D1_POINT_2F() = New D2D1_POINT_2F(points.Length - 2) {}

        Call Array.Copy(points, 1, tail, 0, tail.Length)
        Call sink.AddLines(tail, CUInt(tail.Length))
    End Sub

    ' /********************************************************************************/
    '  the primitive geometry builders
    ' /********************************************************************************/

    ''' <summary>
    ''' build a polygon (or a polyline when <paramref name="closed"/> is false)
    ''' </summary>
    Friend Function Polygon(factory As ID2D1Factory, points As D2D1_POINT_2F(), closed As Boolean) As ID2D1PathGeometry
        Dim sink As ID2D1GeometrySink = Nothing
        Dim geometry As ID2D1PathGeometry = NewGeometry(factory, sink)

        Try
            Call sink.BeginFigure(points(0), D2D1_FIGURE_BEGIN.FILLED)
            Call AddTail(sink, points)
            Call sink.EndFigure(If(closed, D2D1_FIGURE_END.CLOSED, D2D1_FIGURE_END.OPEN))
        Finally
            Call CloseGeometry(sink)
        End Try

        Return geometry
    End Function

    ''' <summary>
    ''' build a cubic bezier spline
    ''' </summary>
    Friend Function Bezier(factory As ID2D1Factory, pt1 As D2D1_POINT_2F, pt2 As D2D1_POINT_2F,
                           pt3 As D2D1_POINT_2F, pt4 As D2D1_POINT_2F) As ID2D1PathGeometry

        Dim sink As ID2D1GeometrySink = Nothing
        Dim geometry As ID2D1PathGeometry = NewGeometry(factory, sink)
        Dim segment As New D2D1_BEZIER_SEGMENT With {.point1 = pt2, .point2 = pt3, .point3 = pt4}

        Try
            Call sink.BeginFigure(pt1, D2D1_FIGURE_BEGIN.FILLED)
            Call sink.AddBezier(segment)
            Call sink.EndFigure(D2D1_FIGURE_END.OPEN)
        Finally
            Call CloseGeometry(sink)
        End Try

        Return geometry
    End Function

    ''' <summary>
    ''' build a bezier spline sequence from a point array of 3n+1 points
    ''' </summary>
    Friend Function Beziers(factory As ID2D1Factory, points As D2D1_POINT_2F()) As ID2D1PathGeometry
        Dim sink As ID2D1GeometrySink = Nothing
        Dim geometry As ID2D1PathGeometry = NewGeometry(factory, sink)
        Dim count As Integer = (points.Length - 1) \ 3

        Try
            Call sink.BeginFigure(points(0), D2D1_FIGURE_BEGIN.FILLED)

            If count > 0 Then
                Dim segments As D2D1_BEZIER_SEGMENT() = New D2D1_BEZIER_SEGMENT(count - 1) {}

                For i As Integer = 0 To count - 1
                    segments(i) = New D2D1_BEZIER_SEGMENT With {
                        .point1 = points(i * 3 + 1),
                        .point2 = points(i * 3 + 2),
                        .point3 = points(i * 3 + 3)
                    }
                Next

                Call sink.AddBeziers(segments, CUInt(segments.Length))
            End If

            Call sink.EndFigure(D2D1_FIGURE_END.OPEN)
        Finally
            Call CloseGeometry(sink)
        End Try

        Return geometry
    End Function

    ''' <summary>
    ''' compute the arc segment of a gdi+ style arc: the angle unit is degree
    ''' and the positive sweep angle goes clockwise on the screen.
    ''' </summary>
    Private Sub GetArc(rect As RectangleF, startAngle As Single, sweepAngle As Single,
                       ByRef startPoint As D2D1_POINT_2F, ByRef arc As D2D1_ARC_SEGMENT)

        Dim cx As Single = rect.Left + rect.Width / 2.0F
        Dim cy As Single = rect.Top + rect.Height / 2.0F
        Dim rx As Single = rect.Width / 2.0F
        Dim ry As Single = rect.Height / 2.0F
        Dim a0 As Double = startAngle * std.PI / 180.0
        Dim a1 As Double = (startAngle + sweepAngle) * std.PI / 180.0

        startPoint = New D2D1_POINT_2F With {
            .x = CSng(cx + rx * std.Cos(a0)),
            .y = CSng(cy + ry * std.Sin(a0))
        }
        arc = New D2D1_ARC_SEGMENT With {
            .point = New D2D1_POINT_2F With {
                .x = CSng(cx + rx * std.Cos(a1)),
                .y = CSng(cy + ry * std.Sin(a1))
            },
            .size = New D2D1_SIZE_F With {.width = rx, .height = ry},
            .rotationAngle = 0.0F,
            .sweepDirection = If(sweepAngle >= 0, D2D1_SWEEP_DIRECTION.CLOCKWISE, D2D1_SWEEP_DIRECTION.COUNTER_CLOCKWISE),
            .arcSize = If(std.Abs(sweepAngle) > 180, D2D1_ARC_SIZE.LARGE, D2D1_ARC_SIZE.SMALL)
        }
    End Sub

    ''' <summary>
    ''' build an open arc
    ''' </summary>
    Friend Function Arc(factory As ID2D1Factory, rect As RectangleF,
                        startAngle As Single, sweepAngle As Single) As ID2D1PathGeometry

        Dim sink As ID2D1GeometrySink = Nothing
        Dim geometry As ID2D1PathGeometry = NewGeometry(factory, sink)
        Dim startPoint As D2D1_POINT_2F
        Dim segment As D2D1_ARC_SEGMENT

        Call GetArc(rect, startAngle, sweepAngle, startPoint, segment)

        Try
            Call sink.BeginFigure(startPoint, D2D1_FIGURE_BEGIN.FILLED)
            Call sink.AddArc(segment)
            Call sink.EndFigure(D2D1_FIGURE_END.OPEN)
        Finally
            Call CloseGeometry(sink)
        End Try

        Return geometry
    End Function

    ''' <summary>
    ''' build a pie shape: the center point, the arc and the closed outline
    ''' </summary>
    Friend Function Pie(factory As ID2D1Factory, rect As RectangleF,
                        startAngle As Single, sweepAngle As Single) As ID2D1PathGeometry

        Dim sink As ID2D1GeometrySink = Nothing
        Dim geometry As ID2D1PathGeometry = NewGeometry(factory, sink)
        Dim startPoint As D2D1_POINT_2F
        Dim segment As D2D1_ARC_SEGMENT
        Dim center As New D2D1_POINT_2F With {
            .x = rect.Left + rect.Width / 2.0F,
            .y = rect.Top + rect.Height / 2.0F
        }

        Call GetArc(rect, startAngle, sweepAngle, startPoint, segment)

        Try
            Call sink.BeginFigure(center, D2D1_FIGURE_BEGIN.FILLED)
            Call sink.AddLine(startPoint)
            Call sink.AddArc(segment)
            Call sink.EndFigure(D2D1_FIGURE_END.CLOSED)
        Finally
            Call CloseGeometry(sink)
        End Try

        Return geometry
    End Function

    ''' <summary>
    ''' build an ellipse figure through two half arc segments
    ''' </summary>
    Private Sub AddEllipseFigure(sink As ID2D1GeometrySink, rect As RectangleF)
        Dim cx As Single = rect.Left + rect.Width / 2.0F
        Dim cy As Single = rect.Top + rect.Height / 2.0F
        Dim rx As Single = rect.Width / 2.0F
        Dim ry As Single = rect.Height / 2.0F
        Dim right As New D2D1_POINT_2F With {.x = cx + rx, .y = cy}
        Dim left As New D2D1_POINT_2F With {.x = cx - rx, .y = cy}
        Dim size As New D2D1_SIZE_F With {.width = rx, .height = ry}
        Dim half As New D2D1_ARC_SEGMENT With {
            .point = left,
            .size = size,
            .rotationAngle = 0.0F,
            .sweepDirection = D2D1_SWEEP_DIRECTION.CLOCKWISE,
            .arcSize = D2D1_ARC_SIZE.SMALL
        }
        Dim half2 As New D2D1_ARC_SEGMENT With {
            .point = right,
            .size = size,
            .rotationAngle = 0.0F,
            .sweepDirection = D2D1_SWEEP_DIRECTION.CLOCKWISE,
            .arcSize = D2D1_ARC_SIZE.SMALL
        }

        Call sink.BeginFigure(right, D2D1_FIGURE_BEGIN.FILLED)
        Call sink.AddArc(half)
        Call sink.AddArc(half2)
        Call sink.EndFigure(D2D1_FIGURE_END.CLOSED)
    End Sub

    ''' <summary>
    ''' build a rectangle figure
    ''' </summary>
    Private Sub AddRectangleFigure(sink As ID2D1GeometrySink, rect As RectangleF)
        Call sink.BeginFigure(New D2D1_POINT_2F With {.x = rect.Left, .y = rect.Top}, D2D1_FIGURE_BEGIN.FILLED)
        Call sink.AddLine(New D2D1_POINT_2F With {.x = rect.Right, .y = rect.Top})
        Call sink.AddLine(New D2D1_POINT_2F With {.x = rect.Right, .y = rect.Bottom})
        Call sink.AddLine(New D2D1_POINT_2F With {.x = rect.Left, .y = rect.Bottom})
        Call sink.EndFigure(D2D1_FIGURE_END.CLOSED)
    End Sub

    ' /********************************************************************************/
    '  the cardinal spline (gdi+ DrawCurve) conversion
    ' /********************************************************************************/

    Private Function At(points As D2D1_POINT_2F(), i As Integer, closed As Boolean) As D2D1_POINT_2F
        If closed Then
            Dim n As Integer = points.Length
            Dim m As Integer = ((i Mod n) + n) Mod n

            Return points(m)
        Else
            If i < 0 Then
                Return points(0)
            ElseIf i >= points.Length Then
                Return points(points.Length - 1)
            Else
                Return points(i)
            End If
        End If
    End Function

    ''' <summary>
    ''' convert the cardinal spline control points into a cubic bezier
    ''' segment sequence
    ''' </summary>
    Private Function CardinalSegments(points As D2D1_POINT_2F(), tension As Single, closed As Boolean) As D2D1_BEZIER_SEGMENT()
        Dim n As Integer = points.Length

        If n < 2 Then
            Return New D2D1_BEZIER_SEGMENT() {}
        End If

        Dim count As Integer = If(closed, n, n - 1)
        Dim segments As D2D1_BEZIER_SEGMENT() = New D2D1_BEZIER_SEGMENT(count - 1) {}
        Dim k As Single = If(tension <= 0, 0.5F, tension) / 3.0F

        For i As Integer = 0 To count - 1
            Dim p0 As D2D1_POINT_2F = At(points, i - 1, closed)
            Dim p1 As D2D1_POINT_2F = At(points, i, closed)
            Dim p2 As D2D1_POINT_2F = At(points, i + 1, closed)
            Dim p3 As D2D1_POINT_2F = At(points, i + 2, closed)

            segments(i) = New D2D1_BEZIER_SEGMENT With {
                .point1 = New D2D1_POINT_2F With {.x = p1.x + (p2.x - p0.x) * k, .y = p1.y + (p2.y - p0.y) * k},
                .point2 = New D2D1_POINT_2F With {.x = p2.x - (p3.x - p1.x) * k, .y = p2.y - (p3.y - p1.y) * k},
                .point3 = p2
            }
        Next

        Return segments
    End Function

    ''' <summary>
    ''' build a cardinal spline curve
    ''' </summary>
    Friend Function Curve(factory As ID2D1Factory, points As D2D1_POINT_2F(),
                          tension As Single, closed As Boolean) As ID2D1PathGeometry

        Dim sink As ID2D1GeometrySink = Nothing
        Dim geometry As ID2D1PathGeometry = NewGeometry(factory, sink)

        Dim segments As D2D1_BEZIER_SEGMENT() = CardinalSegments(points, tension, closed)

        Try
            Call sink.BeginFigure(points(0), D2D1_FIGURE_BEGIN.FILLED)

            If segments.Length > 0 Then
                Call sink.AddBeziers(segments, CUInt(segments.Length))
            End If

            Call sink.EndFigure(If(closed, D2D1_FIGURE_END.CLOSED, D2D1_FIGURE_END.OPEN))
        Finally
            Call CloseGeometry(sink)
        End Try

        Return geometry
    End Function

    ' /********************************************************************************/
    '  replay a gdi+ GraphicsPath object into a direct2d path geometry
    ' /********************************************************************************/

    ''' <summary>
    ''' replay the drawing operation sequence of a <see cref="GraphicsPath"/>
    ''' object into a direct2d path geometry.
    ''' </summary>
    Friend Function FromGraphicsPath(factory As ID2D1Factory, path As GraphicsPath) As ID2D1PathGeometry
        Dim sink As ID2D1GeometrySink = Nothing
        Dim geometry As ID2D1PathGeometry = NewGeometry(factory, sink)

        If path.FillMode = FillMode.Alternate Then
            Call sink.SetFillMode(D2D1_FILL_MODE.ALTERNATE)
        End If

        Try
            For Each op As GraphicsPath.op In path.AsEnumerable()
                Call Replay(sink, op)
            Next
        Finally
            Call CloseGeometry(sink)
        End Try

        Return geometry
    End Function

    Private Sub Replay(sink As ID2D1GeometrySink, op As GraphicsPath.op)
        Select Case op.GetType.Name
            Case NameOf(GraphicsPath.op_AddLine)
                Dim line = DirectCast(op, GraphicsPath.op_AddLine)

                Call sink.BeginFigure(ToPoint2F(line.a), D2D1_FIGURE_BEGIN.FILLED)
                Call sink.AddLine(ToPoint2F(line.b))
                Call sink.EndFigure(D2D1_FIGURE_END.OPEN)

            Case NameOf(GraphicsPath.op_AddLines)
                Dim pts As D2D1_POINT_2F() = ToPoints(DirectCast(op, GraphicsPath.op_AddLines).points)

                Call sink.BeginFigure(pts(0), D2D1_FIGURE_BEGIN.FILLED)
                Call AddTail(sink, pts)
                Call sink.EndFigure(D2D1_FIGURE_END.OPEN)

            Case NameOf(GraphicsPath.op_AddPolygon)
                Dim pts As D2D1_POINT_2F() = ToPoints(DirectCast(op, GraphicsPath.op_AddPolygon).points)

                Call sink.BeginFigure(pts(0), D2D1_FIGURE_BEGIN.FILLED)
                Call AddTail(sink, pts)
                Call sink.EndFigure(D2D1_FIGURE_END.CLOSED)

            Case NameOf(GraphicsPath.op_AddRectangle)
                Call AddRectangleFigure(sink, DirectCast(op, GraphicsPath.op_AddRectangle).rect)

            Case NameOf(GraphicsPath.op_AddEllipse)
                Dim e = DirectCast(op, GraphicsPath.op_AddEllipse)

                Call AddEllipseFigure(sink, New RectangleF(e.x, e.y, e.r1 * 2.0F, e.r2 * 2.0F))

            Case NameOf(GraphicsPath.op_AddEllipseRect)
                Call AddEllipseFigure(sink, DirectCast(op, GraphicsPath.op_AddEllipseRect).rect)

            Case NameOf(GraphicsPath.op_AddArc)
                Dim arc = DirectCast(op, GraphicsPath.op_AddArc)
                Dim startPoint As D2D1_POINT_2F
                Dim segment As D2D1_ARC_SEGMENT

                Call GetArc(arc.rect, arc.startAngle, arc.sweepAngle, startPoint, segment)
                Call sink.BeginFigure(startPoint, D2D1_FIGURE_BEGIN.FILLED)
                Call sink.AddArc(segment)
                Call sink.EndFigure(D2D1_FIGURE_END.OPEN)

            Case NameOf(GraphicsPath.op_AddPie)
                Dim pie = DirectCast(op, GraphicsPath.op_AddPie)
                Dim startPoint As D2D1_POINT_2F
                Dim segment As D2D1_ARC_SEGMENT
                Dim center As New D2D1_POINT_2F With {
                    .x = pie.rect.Left + pie.rect.Width / 2.0F,
                    .y = pie.rect.Top + pie.rect.Height / 2.0F
                }

                Call GetArc(pie.rect, pie.startAngle, pie.sweepAngle, startPoint, segment)
                Call sink.BeginFigure(center, D2D1_FIGURE_BEGIN.FILLED)
                Call sink.AddLine(startPoint)
                Call sink.AddArc(segment)
                Call sink.EndFigure(D2D1_FIGURE_END.CLOSED)

            Case NameOf(GraphicsPath.op_AddBezier)
                Dim bz = DirectCast(op, GraphicsPath.op_AddBezier)
                Dim segment As New D2D1_BEZIER_SEGMENT With {
                    .point1 = ToPoint2F(bz.pt2),
                    .point2 = ToPoint2F(bz.pt3),
                    .point3 = ToPoint2F(bz.pt4)
                }

                Call sink.BeginFigure(ToPoint2F(bz.pt1), D2D1_FIGURE_BEGIN.FILLED)
                Call sink.AddBezier(segment)
                Call sink.EndFigure(D2D1_FIGURE_END.OPEN)

            Case NameOf(GraphicsPath.op_AddBeziers)
                Call BeziersInto(sink, ToPoints(DirectCast(op, GraphicsPath.op_AddBeziers).points))

            Case NameOf(GraphicsPath.op_AddCurve)
                Call CurveInto(sink, ToPoints(DirectCast(op, GraphicsPath.op_AddCurve).points), 0.5F, closed:=False)

            Case NameOf(GraphicsPath.op_AddClosedCurve)
                Dim cc = DirectCast(op, GraphicsPath.op_AddClosedCurve)

                Call CurveInto(sink, ToPoints(cc.points), cc.tension, closed:=True)

            Case NameOf(GraphicsPath.op_AddPath)
                Dim subpath = DirectCast(op, GraphicsPath.op_AddPath).path

                If subpath IsNot Nothing Then
                    For Each child As GraphicsPath.op In subpath.AsEnumerable()
                        Call Replay(sink, child)
                    Next
                End If

            Case Else
                ' StartFigure / CloseFigure / Reset / Transform / Warp / Widen /
                ' Flatten / Reverse / GetBounds / AddString are ignored: the
                ' figure state is managed by this geometry builder itself.
                Exit Sub
        End Select
    End Sub

    Private Sub BeziersInto(sink As ID2D1GeometrySink, points As D2D1_POINT_2F())
        Dim count As Integer = (points.Length - 1) \ 3

        Call sink.BeginFigure(points(0), D2D1_FIGURE_BEGIN.FILLED)

        If count > 0 Then
            Dim segments As D2D1_BEZIER_SEGMENT() = New D2D1_BEZIER_SEGMENT(count - 1) {}

            For i As Integer = 0 To count - 1
                segments(i) = New D2D1_BEZIER_SEGMENT With {
                    .point1 = points(i * 3 + 1),
                    .point2 = points(i * 3 + 2),
                    .point3 = points(i * 3 + 3)
                }
            Next

            Call sink.AddBeziers(segments, CUInt(segments.Length))
        End If

        Call sink.EndFigure(D2D1_FIGURE_END.OPEN)
    End Sub

    Private Sub CurveInto(sink As ID2D1GeometrySink, points As D2D1_POINT_2F(), tension As Single, closed As Boolean)
        Dim segments As D2D1_BEZIER_SEGMENT() = CardinalSegments(points, tension, closed)

        Call sink.BeginFigure(points(0), D2D1_FIGURE_BEGIN.FILLED)

        If segments.Length > 0 Then
            Call sink.AddBeziers(segments, CUInt(segments.Length))
        End If

        Call sink.EndFigure(If(closed, D2D1_FIGURE_END.CLOSED, D2D1_FIGURE_END.OPEN))
    End Sub
End Module
