Imports System.Drawing
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
''' The vertex buffer of the figure is written into a pinned scratch buffer
''' that is owned by this submitter, so that no managed array has to be
''' allocated and marshalled for each polygon primitive.
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
    Private Const MAX_FIGURES As Integer = 512

    ''' <summary>
    ''' the vertex capacity of the pinned scratch buffer
    ''' </summary>
    Private Const MAX_SCRATCH_POINTS As Integer = 512

    Private ReadOnly factory As ID2D1Factory
    Private ReadOnly target As ID2D1RenderTarget

    ''' <summary>
    ''' a pinned managed vertex buffer, it is reused by every polygon so that
    ''' no per primitive allocation is required
    ''' </summary>
    Private ReadOnly scratch As Single()
    Private scratchHandle As GCHandle
    Private scratchPtr As IntPtr

    Private geometry As ID2D1PathGeometry = Nothing
    Private sink As ID2D1GeometrySink = Nothing
    Private writer As DxSinkWriter = Nothing
    Private brush As ID2D1Brush = Nothing
    Private strokeStyle As ID2D1StrokeStyle = Nothing
    Private brushKey As Integer = 0
    Private isStroke As Boolean = False
    Private strokeWidth As Single = 1.0F
    Private figures As Integer = 0
    Private m_disposed As Boolean = False

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
        scratch = New Single(MAX_SCRATCH_POINTS * 2 - 1) {}
        scratchHandle = GCHandle.Alloc(scratch, GCHandleType.Pinned)
        scratchPtr = scratchHandle.AddrOfPinnedObject()
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
    Friend Sub AddPolygon(points As PointF(), stroke As Boolean, key As Integer,
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
            Dim pathPtr As IntPtr = IntPtr.Zero

            Call ThrowIfFailed(factory.CreatePathGeometry(pathPtr), "ID2D1Factory::CreatePathGeometry")

            Dim path As ID2D1PathGeometry = ComObject(Of ID2D1PathGeometry)(pathPtr)
            Dim sinkPtr As IntPtr = IntPtr.Zero

            Call ThrowIfFailed(path.Open(sinkPtr), "ID2D1PathGeometry::Open")

            sink = ComObject(Of ID2D1GeometrySink)(sinkPtr)
            writer = New DxSinkWriter(sink)
            writer.SetFillMode(D2D1_FILL_MODE.WINDING)

            geometry = path
            brushKey = key
            isStroke = stroke
            brush = fillBrush
            strokeWidth = width
            strokeStyle = style
            figures = 0
        End If

        Call writer.BeginFigure(New D2D1_POINT_2F With {.x = points(0).X, .y = points(0).Y})

        If points.Length > 1 Then
            If points.Length <= MAX_SCRATCH_POINTS Then
                For i As Integer = 0 To points.Length - 1
                    scratch(i * 2) = points(i).X
                    scratch(i * 2 + 1) = points(i).Y
                Next

                Call writer.AddLines(New IntPtr(scratchPtr.ToInt64() + 8L), CUInt(points.Length - 1))
            Else
                ' a polygon that is larger than the scratch buffer
                Call DxPathBuilder.AddLines(sink, DxPathBuilder.ToPoints(points), 1)
            End If
        End If

        writer.EndFigureClosed()

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
        Dim writerRef As DxSinkWriter = writer

        geometry = Nothing
        sink = Nothing
        writer = Nothing
        figures = 0

        Try
            writerRef.Close()

            If isStroke Then
                Call target.DrawGeometry(path, brush, strokeWidth, strokeStyle)
            Else
                Call target.FillGeometry(path, brush, Nothing)
            End If
        Finally
            writerRef.Release()
            Call SafeRelease(sinkRef)
            Call SafeRelease(path)
        End Try

        brush = Nothing
        strokeStyle = Nothing
        brushKey = 0
    End Sub

    ''' <summary>
    ''' drop the accumulated polygon primitives without submitting them
    ''' </summary>
    ''' <remarks>
    ''' This is required when the render target that owns the pending geometry
    ''' is not usable any more (a removed gpu device for example): the pending
    ''' figures can not be rasterized any more, so they are released directly
    ''' instead of being drawn.
    ''' </remarks>
    Friend Sub Discard()
        If geometry Is Nothing Then
            Return
        End If

        Dim path As ID2D1PathGeometry = geometry
        Dim sinkRef As ID2D1GeometrySink = sink
        Dim writerRef As DxSinkWriter = writer

        geometry = Nothing
        sink = Nothing
        writer = Nothing
        figures = 0
        brush = Nothing
        strokeStyle = Nothing
        brushKey = 0

        Try
            writerRef.Release()
        Catch
        End Try

        Call SafeRelease(sinkRef)
        Call SafeRelease(path)
    End Sub

    ''' <summary>
    ''' the brush key of the current pending batch, -1 means nothing pending
    ''' </summary>
    Friend ReadOnly Property CurrentKey As Integer
        Get
            If geometry Is Nothing Then
                Return -1
            End If

            Return brushKey
        End Get
    End Property

    ''' <summary>
    ''' is the pending batch a stroke batch?
    ''' </summary>
    Friend ReadOnly Property CurrentIsStroke As Boolean
        Get
            Return geometry IsNot Nothing AndAlso isStroke
        End Get
    End Property

    Private Sub Dispose(disposing As Boolean)
        If m_disposed Then
            Return
        End If

        m_disposed = True

        Call Flush()

        If scratchHandle.IsAllocated Then
            Call scratchHandle.Free()
        End If

        scratchPtr = IntPtr.Zero
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        Call Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub
End Class
