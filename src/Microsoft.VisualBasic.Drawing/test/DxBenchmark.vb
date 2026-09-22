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

        Dim scene As PolygonDraw() = GenerateScene(polygonCount)

        Console.WriteLine("=============================================================")
        Console.WriteLine($" polygon drawing benchmark: {polygonCount.ToString("N0")} polygons")
        Console.WriteLine($" canvas size              : [{CANVAS_WIDTH}, {CANVAS_HEIGHT}], {VERTEX_COUNT} vertices per polygon")
        Console.WriteLine("=============================================================")

        ' ---------------------------------------------------------------
        '  the baseline: the skia sharp raster canvas
        ' ---------------------------------------------------------------
        Dim skia As SkiaCanvas = New SkiaCanvas(CANVAS_WIDTH, CANVAS_HEIGHT, "#ffffff")
        Dim skiaMs As Long = DrawScene(skia, scene)

        Console.WriteLine($" [skia    ] {skiaMs.ToString("N0")} ms")

        Call skia.Save("./polygons_skia.png")
        Call skia.Dispose()

        ' ---------------------------------------------------------------
        '  the directx gpu canvas, one draw call per polygon
        ' ---------------------------------------------------------------
        Dim dxNoBatch As DxGraphics = New DxGraphics(CANVAS_WIDTH, CANVAS_HEIGHT, "#ffffff")
        Dim dxNoBatchMs As Long

        dxNoBatch.BatchFill = False
        dxNoBatchMs = DrawScene(dxNoBatch, scene)

        Console.WriteLine($" [directx ] {dxNoBatchMs.ToString("N0")} ms  (batch = off)")
        Console.WriteLine($" [device  ] {dxNoBatch.DeviceDescription}")

        Call dxNoBatch.Save("./polygons_directx.png")
        Call dxNoBatch.Dispose()

        ' ---------------------------------------------------------------
        '  the directx gpu canvas, the polygons of the same brush are
        '  merged into one single geometry object
        ' ---------------------------------------------------------------
        Dim dx As DxGraphics = New DxGraphics(CANVAS_WIDTH, CANVAS_HEIGHT, "#ffffff")
        Dim dxMs As Long

        dx.BatchFill = True
        dxMs = DrawScene(dx, scene)

        Console.WriteLine($" [directx ] {dxMs.ToString("N0")} ms  (batch = on)")

        Call dx.Save("./polygons_directx_batch.png")
        Call dx.Dispose()

        ' ---------------------------------------------------------------
        '  the summary
        ' ---------------------------------------------------------------
        Console.WriteLine("-------------------------------------------------------------")
        Console.WriteLine($" speed up (batch off) : {SpeedUp(skiaMs, dxNoBatchMs)}")
        Console.WriteLine($" speed up (batch on ) : {SpeedUp(skiaMs, dxMs)}")
        Console.WriteLine($" speed up (internal ) : {SpeedUp(dxNoBatchMs, dxMs)}")
        Console.WriteLine("=============================================================")
    End Sub

    Private Function SpeedUp(baselineMs As Long, testMs As Long) As String
        If testMs <= 0 Then
            Return "n/a"
        End If

        Return $"{(baselineMs / testMs).ToString("F2")}x"
    End Function

    ''' <summary>
    ''' draw the whole scene on the given canvas and returns the time cost
    ''' in milliseconds
    ''' </summary>
    Private Function DrawScene(g As IGraphics, scene As PolygonDraw()) As Long
        Dim watch As Stopwatch = Stopwatch.StartNew()

        ' fill the polygons at first and then stroke the outlines of them,
        ' both of the two backends run exactly the same drawing sequence
        For Each polygon As PolygonDraw In scene
            Call g.FillPolygon(polygon.Brush, polygon.Points)
        Next
        For Each polygon As PolygonDraw In scene
            Call g.DrawPolygon(polygon.Pen, polygon.Points)
        Next

        Call g.Flush()

        watch.Stop()

        Return watch.ElapsedMilliseconds
    End Function

    ''' <summary>
    ''' generate a random polygon scene, a fixed random seed is used here so
    ''' that the benchmark result is reproducible.
    ''' </summary>
    Private Function GenerateScene(count As Integer) As PolygonDraw()
        Dim rnd As New Random(20240923)
        Dim palette As String() = {
            "#e6194b", "#3cb44b", "#ffe119", "#4363d8", "#f58231",
            "#911eb4", "#46f0f0", "#f032e6", "#bcf60c", "#fabebe"
        }
        Dim brushes As SolidBrush() = palette _
            .Select(Function(c) New SolidBrush(c.TranslateColor)) _
            .ToArray()
        Dim scene As PolygonDraw() = New PolygonDraw(count - 1) {}

        For i As Integer = 0 To count - 1
            Dim cx As Double = rnd.NextDouble() * CANVAS_WIDTH
            Dim cy As Double = rnd.NextDouble() * CANVAS_HEIGHT
            Dim radius As Double = 3 + rnd.NextDouble() * 14
            Dim brush As SolidBrush = brushes(rnd.Next(brushes.Length))
            Dim points As PointF() = New PointF(VERTEX_COUNT - 1) {}

            For k As Integer = 0 To VERTEX_COUNT - 1
                Dim angle As Double = 2 * std.PI * k / VERTEX_COUNT
                Dim r As Double = radius * (0.55 + rnd.NextDouble() * 0.45)

                points(k) = New PointF(CSng(cx + r * std.Cos(angle)), CSng(cy + r * std.Sin(angle)))
            Next

            scene(i) = New PolygonDraw With {
                .Points = points,
                .Brush = brush,
                .Pen = New Pen(brush.Color, 1.0F)
            }
        Next

        Return scene
    End Function
End Module
