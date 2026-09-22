Imports System.Runtime.InteropServices

''' <summary>
''' The polygon batch submitter.
''' </summary>
''' <remarks>
''' Submitting one draw call per polygon primitive is the main bottleneck of
''' a large polygon scene: every FillGeometry / DrawGeometry call goes through
''' a com interop boundary and forces direct2d to build a new geometry object.
'''
''' This batch submitter appends every polygon of the same brush as an
''' independent figure of one single ID2D1PathGeometry object, and then all
''' of the accumulated polygons are rasterized by the gpu device through just
''' one draw call, so the per primitive overhead is reduced from O(n) to
''' O(n / batch_size).
'''
''' The fill mode of the batch geometry is D2D1_FILL_MODE_WINDING, so that
''' the overlapped area of two polygons is still filled, which keeps the same
''' visual result as drawing the polygons one by one.
''' </remarks>
Friend Class DxPolygonBatch : Implements IDisposable

    ''' <summary>
    ''' flush the geometry after this amount of figures so that the memory
    ''' cost of a single batch is bounded.
    ''' </summary>
    Private Const MAX_FIGURES As Integer = 4096

    Private ReadOnly factory As ID2D1Factory
    Private ReadOnly target As ID2D1RenderTarget

    Private geometry As ID2D1PathGeometry = Nothing
    Private sink As ID2D1GeometrySink = Nothing
    Private brush As ID2D1Brush = Nothing
    Private strokeStyle As ID2D1StrokeStyle = Nothing
    Private brushKey As String = Nothing
    Private isStroke As Boolean = False
    Private strokeWidth As Single = 1.0F
    Private figures As Integer = 0

    ''' <summary>
    ''' the amount of the polygon primitives that are accumulated in the
    ''' current batch
    ''' </summary>
    Friend ReadOnly Property PendingFigures As Integer
        Get
            Return figures
        End Get
    End Property

    Friend Sub New(factory2d As ID2D1Factory, renderTarget As ID2D1RenderTarget)
        factory = factory2d
        target = renderTarget
    End Sub

    ''' <summary>
    ''' append a closed polygon primitive into the current batch
    ''' </summary>
    ''' <param name="points">the vertex buffer of the polygon</param>
    ''' <param name="stroke">
    ''' true for a stroke (outline) drawing and false for a fill drawing
    ''' </param>
    ''' <param name="key">
    ''' the brush cache key, a new batch is required when the key is changed
    ''' </param>
    Friend Sub AddPolygon(points As D2D1_POINT_2F(), stroke As Boolean, key As String,
                          fillBrush As ID2D1Brush,
                          Optional width As Single = 1.0F,
                          Optional style As ID2D1StrokeStyle = Nothing)

        If points Is Nothing OrElse points.Length < 2 Then
            Return
        End If

        If geometry IsNot Nothing Then
            If stroke <> isStroke OrElse key <> brushKey OrElse (stroke AndAlso width <> strokeWidth) Then
                ' the brush is changed, submit the current batch at first
                Call Flush()
            End If
        End If

        If geometry Is Nothing Then
            Dim path As ID2D1PathGeometry = Nothing

            Call ThrowIfFailed(factory.CreatePathGeometry(path), "ID2D1Factory::CreatePathGeometry")
            Call ThrowIfFailed(path.Open(sink), "ID2D1PathGeometry::Open")
            Call sink.SetFillMode(D2D1_FILL_MODE.WINDING)

            geometry = path
            brushKey = key
            isStroke = stroke
            brush = fillBrush
            strokeWidth = width
            strokeStyle = style
            figures = 0
        End If

        Call sink.BeginFigure(points(0), D2D1_FIGURE_BEGIN.FILLED)

        If points.Length > 1 Then
            Dim tail As D2D1_POINT_2F() = New D2D1_POINT_2F(points.Length - 2) {}

            Call Array.Copy(points, 1, tail, 0, tail.Length)
            Call sink.AddLines(tail, CUInt(tail.Length))
        End If

        Call sink.EndFigure(D2D1_FIGURE_END.CLOSED)

        figures += 1

        If figures >= MAX_FIGURES Then
            Call Flush()
        End If
    End Sub

    ''' <summary>
    ''' submit all of the accumulated polygon primitives to the gpu device
    ''' </summary>
    Friend Sub Flush()
        If geometry Is Nothing Then
            Return
        End If

        Dim path As ID2D1PathGeometry = geometry
        Dim sinkRef As ID2D1GeometrySink = sink

        geometry = Nothing
        sink = Nothing
        figures = 0

        Try
            Call ThrowIfFailed(sinkRef.Close(), "ID2D1GeometrySink::Close")

            If isStroke Then
                Call target.DrawGeometry(path, brush, strokeWidth, strokeStyle)
            Else
                Call target.FillGeometry(path, brush, Nothing)
            End If
        Finally
            Call SafeRelease(sinkRef)
            Call SafeRelease(path)
        End Try

        brush = Nothing
        strokeStyle = Nothing
        brushKey = Nothing
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        Call Flush()
        GC.SuppressFinalize(Me)
    End Sub
End Class
