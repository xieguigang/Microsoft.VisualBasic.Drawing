Imports System.Diagnostics
Imports System.Drawing
Imports Microsoft.VisualBasic.Drawing.DirectX
Imports Microsoft.VisualBasic.Imaging
Imports std = System.Math

Imports Pen = Microsoft.VisualBasic.Imaging.Pen
Imports SkiaCanvas = Microsoft.VisualBasic.Drawing.Graphics
Imports SolidBrush = Microsoft.VisualBasic.Imaging.SolidBrush

''' <summary>
''' The demo test of the <see cref="DxGraphics"/> object: draw a scene that
''' contains a large amount of polygon primitives on both the skia raster
''' canvas and the directx gpu canvas, then compare the time cost of the
''' two backends.
''' </summary>
''' <remarks>
''' The polygons of the scene are generated series by series, which is the
''' same drawing order as a typical chart renderer that draws one data
''' series after another. This drawing order lets the directx backend merge
''' the polygons of the same brush into one single gpu draw call.
''' </remarks>
Module DxBenchmark

    Const CANVAS_WIDTH As Integer = 1920
    Const CANVAS_HEIGHT As Integer = 1080
    Const DEFAULT_POLYGON_COUNT As Integer = 30000
    Const VERTEX_COUNT As Integer = 6

    ''' <summary>
    ''' one polygon primitive of the benchmark scene
    ''' </summary>
    Private Class PolygonDraw
        Public Property Points As PointF()
        Public Property Brush As SolidBrush
        Public Property Pen As Pen
    End Class

    ''' <summary>
    ''' run the polygon drawing benchmark
    ''' </summary>
    ''' <param name="polygonCount">
    ''' the amount of the polygon primitives of the test scene
    ''' </param>
    Sub Run(Optional polygonCount As Integer = 0)
        If polygonCount <= 0 Then
            polygonCount = DEFAULT_POLYGON_COUNT
        End If

        Console.WriteLine("=================================================================")
        Console.WriteLine($" polygon drawing benchmark: {polygonCount.ToString("N0")} polygons")
        Console.WriteLine($" canvas size              : [{CANVAS_WIDTH}, {CANVAS_HEIGHT}], {VERTEX_COUNT} vertices per polygon")
        Console.WriteLine("=================================================================")

        ' a scene full of small markers: the cpu side geometry setup is the
        ' dominant cost of this case
        Call BenchScene("small polygons", polygonCount, 3, 17, "small")

        ' a scene full of bigger polygons: the raster fill rate becomes the
        ' dominant cost of this case
        Call BenchScene("large polygons", polygonCount, 10, 40, "large")

        Console.WriteLine("=================================================================")
    End Sub

    Private Sub BenchScene(title As String, count As Integer, minRadius As Integer, maxRadius As Integer, tag As String)
        Dim scene As PolygonDraw() = GenerateScene(count, minRadius, maxRadius)

        Console.WriteLine($"--- {title} (radius {minRadius} ~ {maxRadius}px) ---")

        Dim fill As Long = 0
        Dim stroke As Long = 0
        Dim flush As Long = 0

        ' ---------------------------------------------------------------
        '  the baseline: the skia sharp raster canvas
        ' ---------------------------------------------------------------
        Dim skia As SkiaCanvas = New SkiaCanvas(CANVAS_WIDTH, CANVAS_HEIGHT, "#ffffff")
        Dim skiaMs As Long = DrawScene(skia, scene, fill, stroke, flush)

        Console.WriteLine($" [skia    ] {skiaMs.ToString("N0")} ms  (fill = {fill}, stroke = {stroke})")

        Call skia.Save($"./polygons_skia_{tag}.png")
        Call skia.Dispose()

        ' ---------------------------------------------------------------
        '  the directx gpu canvas, one draw call per polygon
        ' ---------------------------------------------------------------
        Dim dxNoBatch As DxGraphics = New DxGraphics(CANVAS_WIDTH, CANVAS_HEIGHT, "#ffffff")
        Dim dxNoBatchMs As Long

        dxNoBatch.BatchFill = False
        dxNoBatchMs = DrawScene(dxNoBatch, scene, fill, stroke, flush)

        Console.WriteLine($" [directx ] {dxNoBatchMs.ToString("N0")} ms  (batch = off)")

        Call dxNoBatch.Dispose()

        ' ---------------------------------------------------------------
        '  the directx gpu canvas, the polygons of the same brush are
        '  merged into one single geometry object
        ' ---------------------------------------------------------------
        Dim dx As DxGraphics = New DxGraphics(CANVAS_WIDTH, CANVAS_HEIGHT, "#ffffff")
        Dim dxMs As Long

        dx.BatchFill = True
        dxMs = DrawScene(dx, scene, fill, stroke, flush)

        Console.WriteLine($" [directx ] {dxMs.ToString("N0")} ms  (batch = on, submit = {fill + stroke}, raster = {flush})")
        Console.WriteLine($" [device  ] {dx.DeviceDescription}")

        Call dx.Save($"./polygons_directx_{tag}.png")
        Call dx.Dispose()

        Console.WriteLine($" speed up vs skia          : {SpeedUp(skiaMs, dxMs)}")
        Console.WriteLine($" speed up by batch merging : {SpeedUp(dxNoBatchMs, dxMs)}")
    End Sub

    Private Function SpeedUp(baselineMs As Long, testMs As Long) As String
        If testMs <= 0 Then
            Return "n/a"
        End If

        Return $"{(baselineMs / testMs).ToString("F2")}x"
    End Function

    ''' <summary>
    ''' draw the whole scene on the given canvas
    ''' </summary>
    ''' <param name="fillMs">
    ''' the time that is spent on submitting the polygon fill commands
    ''' </param>
    ''' <param name="strokeMs">
    ''' the time that is spent on submitting the polygon outline commands
    ''' </param>
    ''' <param name="flushMs">
    ''' the time that is spent on waiting the backend to finish the raster work
    ''' </param>
    Private Function DrawScene(g As IGraphics, scene As PolygonDraw(),
                               ByRef fillMs As Long, ByRef strokeMs As Long, ByRef flushMs As Long) As Long

        Dim watch As Stopwatch = Stopwatch.StartNew()

        ' fill the polygons at first and then stroke the outlines of them,
        ' both of the two backends run exactly the same drawing sequence
        For Each polygon As PolygonDraw In scene
            Call g.FillPolygon(polygon.Brush, polygon.Points)
        Next

        fillMs = watch.ElapsedMilliseconds

        For Each polygon As PolygonDraw In scene
            Call g.DrawPolygon(polygon.Pen, polygon.Points)
        Next

        strokeMs = watch.ElapsedMilliseconds - fillMs

        Call g.Flush()

        watch.Stop()

        flushMs = watch.ElapsedMilliseconds - fillMs - strokeMs

        Return watch.ElapsedMilliseconds
    End Function

    ''' <summary>
    ''' generate a random polygon scene, the polygons are generated series by
    ''' series so that the drawing order is grouped by the color series.
    ''' </summary>
    Private Function GenerateScene(count As Integer, minRadius As Integer, maxRadius As Integer) As PolygonDraw()
        Dim rnd As New Random(20240923)
        Dim palette As String() = {
            "#e6194b", "#3cb44b", "#ffe119", "#4363d8", "#f58231",
            "#911eb4", "#46f0f0", "#f032e6", "#bcf60c", "#fabebe"
        }
        Dim brushes As SolidBrush() = palette _
            .Select(Function(c) New SolidBrush(c.TranslateColor)) _
            .ToArray()
        Dim scene As PolygonDraw() = New PolygonDraw(count - 1) {}
        Dim series As Integer = brushes.Length
        Dim perSeries As Integer = count \ series
        Dim index As Integer = 0

        For s As Integer = 0 To series - 1
            Dim size As Integer = If(s = series - 1, count - index, perSeries)

            For k As Integer = 1 To size
                If index >= count Then
                    Exit For
                End If

                scene(index) = CreatePolygon(rnd, brushes(s), minRadius, maxRadius)
                index += 1
            Next
        Next

        Return scene
    End Function

    Private Function CreatePolygon(rnd As Random, brush As SolidBrush, minRadius As Integer, maxRadius As Integer) As PolygonDraw
        Dim cx As Double = rnd.NextDouble() * CANVAS_WIDTH
        Dim cy As Double = rnd.NextDouble() * CANVAS_HEIGHT
        Dim radius As Double = minRadius + rnd.NextDouble() * (maxRadius - minRadius)
        Dim points As PointF() = New PointF(VERTEX_COUNT - 1) {}

        For k As Integer = 0 To VERTEX_COUNT - 1
            Dim angle As Double = 2 * std.PI * k / VERTEX_COUNT
            Dim r As Double = radius * (0.55 + rnd.NextDouble() * 0.45)

            points(k) = New PointF(CSng(cx + r * std.Cos(angle)), CSng(cy + r * std.Sin(angle)))
        Next

        Return New PolygonDraw With {
            .Points = points,
            .Brush = brush,
            .Pen = New Pen(brush.Color, 1.0F)
        }
    End Function
End Module
