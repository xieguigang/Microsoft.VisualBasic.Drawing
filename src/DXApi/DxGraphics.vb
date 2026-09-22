Imports System.Drawing
Imports System.IO
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.BitmapImage
Imports Microsoft.VisualBasic.Imaging.Driver
Imports std = System.Math

Imports Brush = Microsoft.VisualBasic.Imaging.Brush
Imports Font = Microsoft.VisualBasic.Imaging.Font
Imports GraphicsPath = Microsoft.VisualBasic.Imaging.GraphicsPath
Imports Image = Microsoft.VisualBasic.Imaging.Image
Imports Pen = Microsoft.VisualBasic.Imaging.Pen

''' <summary>
''' A gpu accelerated 2d drawing canvas that is implemented on top of the
''' Direct2D / D3D11 / DXGI / DirectWrite api.
''' </summary>
''' <remarks>
''' This graphics object talks to the directx api completely through
''' p/invoke: there is no unmanaged helper library, no c++/cli bridge and
''' no unsafe code block in this project, the whole interop layer is pure
''' managed vb.net code.
'''
''' The drawing commands are submitted to the gpu device through a direct2d
''' render target which is created on top of a d3d11 texture, and the final
''' raster image is read back from the gpu texture when the image output is
''' required.
''' </remarks>
Public Class DxGraphics : Inherits IGraphics
    Implements GdiRasterGraphics
    Implements SaveGdiBitmap

    ''' <summary>
    ''' the off screen gpu canvas
    ''' </summary>
    Friend ReadOnly renderTarget As DxRenderTarget
    Friend ReadOnly brushes As DxBrushCache
    Friend ReadOnly batch As DxPolygonBatch

    ''' <summary>
    ''' the axis aligned clip rectangles that are pushed on the render target
    ''' </summary>
    Private ReadOnly clips As New List(Of RectangleF)()

    ''' <summary>
    ''' the current world transform of the render target
    ''' </summary>
    Private transform As D2D1_MATRIX_3X2_F

    Private m_batchFill As Boolean = True
    Private m_released As Boolean = False

    ''' <summary>
    ''' Merge the consecutive polygon primitives that share the same brush
    ''' into one single geometry object and rasterize them through one draw
    ''' call. This switch is the key of the performance improvement on a
    ''' scene that contains a large amount of polygons.
    ''' </summary>
    ''' <returns></returns>
    Public Property BatchFill As Boolean
        Get
            Return m_batchFill
        End Get
        Set(value As Boolean)
            If m_batchFill AndAlso Not value Then
                Call batch.Flush()
            End If

            m_batchFill = value
        End Set
    End Property

    ''' <summary>
    ''' the canvas size in pixels
    ''' </summary>
    ''' <returns></returns>
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


    ''' <summary>
    ''' a short description of the underlying gpu device
    ''' </summary>
    Public ReadOnly Property DeviceDescription As String
        Get
            Return renderTarget.Device.Description
        End Get
    End Property

    Public Overrides Property RenderingOrigin As Point
    Public Overrides Property TextContrast As Integer

    ' /********************************************************************************/
    '  constructor
    ' /********************************************************************************/

    Sub New(width As Integer, height As Integer, Optional dpi As Integer = 96)
        Call Me.New(width, height, Color.Transparent, dpi)
    End Sub

    ''' <summary>
    ''' create a gpu accelerated canvas of the given size
    ''' </summary>
    ''' <param name="fill">
    ''' the background color of the canvas, use <see cref="Color.Transparent"/>
    ''' for a transparent canvas.
    ''' </param>
    Sub New(width As Integer, height As Integer, fill As Color, Optional dpi As Integer = 96)
        Call MyBase.New(dpi)

        _Size = New Size(width, height)
        transform = IdentityMatrix()

        renderTarget = New DxRenderTarget(DxDevice.Default, width, height, If(dpi <= 0, 96.0F, CSng(dpi)))
        brushes = New DxBrushCache(renderTarget.Target, renderTarget.Device.Factory2D, renderTarget.Device.FactoryWrite)
        batch = New DxPolygonBatch(renderTarget.Device.Factory2D, renderTarget.Target)

        If Not fill.IsEmpty Then
            Call ClearCanvas(fill)
        End If
    End Sub

    ''' <summary>
    ''' create a gpu accelerated canvas of the given size, the background
    ''' color is given as a html color string, example as "#ffffff"
    ''' </summary>
    Sub New(width As Integer, height As Integer, fill As String, Optional dpi As Integer = 96)
        Call Me.New(width, height, TranslateColor(fill), dpi)
    End Sub

    ' /********************************************************************************/
    '  private helpers
    ' /********************************************************************************/

    ''' <summary>
    ''' submit the pending polygon batch, this is required before any other
    ''' drawing operation so that the drawing order is preserved.
    ''' </summary>
    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Private Sub EndBatch()
        Call batch.Flush()
    End Sub

    Private Sub FillGeometry(geometry As ID2D1PathGeometry, brush As Brush)
        Try
            Call renderTarget.Target.FillGeometry(geometry, brushes.GetBrush(brush), Nothing)
        Finally
            Call SafeRelease(geometry)
        End Try
    End Sub

    Private Sub DrawGeometry(geometry As ID2D1PathGeometry, pen As Pen)
        Try
            Call renderTarget.Target.DrawGeometry(geometry, brushes.GetBrush(pen.Color), pen.Width, brushes.GetStrokeStyle(pen))
        Finally
            Call SafeRelease(geometry)
        End Try
    End Sub

    ''' <summary>
    ''' append a polygon primitive into the batch submitter
    ''' </summary>
    Private Sub PolygonPrimitive(points As D2D1_POINT_2F(), stroke As Boolean,
                                 brush As ID2D1Brush,
                                 key As String,
                                 Optional width As Single = 1.0F,
                                 Optional style As ID2D1StrokeStyle = Nothing)

        Call batch.AddPolygon(points, stroke, key, brush, width, style)

        If Not m_batchFill Then
            Call batch.Flush()
        End If
    End Sub

    Private Function PenKey(pen As Pen) As String
        Dim dash As Single() = If(pen.DashPattern, New Single() {})

        Return $"{pen.Color.ToArgb()}|{pen.Width}|{CInt(pen.DashStyle)}|{CInt(pen.StartCap)}|{CInt(pen.EndCap)}|{CInt(pen.LineJoin)}|{String.Join(",", dash)}"
    End Function

    Private Sub PushClip(rect As RectangleF)
        Dim r As D2D1_RECT_F = ToRectF(rect)

        Call renderTarget.Target.PushAxisAlignedClip(r, D2D1_ANTIALIAS_MODE.PER_PRIMITIVE)
        Call clips.Add(rect)
    End Sub

    Private Sub PopAllClips()
        While clips.Count > 0
            Call renderTarget.Target.PopAxisAlignedClip()
            Call clips.RemoveAt(clips.Count - 1)
        End While
    End Sub

    Private Sub PushAllClips()
        For Each rect As RectangleF In clips
            Call renderTarget.Target.PushAxisAlignedClip(ToRectF(rect), D2D1_ANTIALIAS_MODE.PER_PRIMITIVE)
        Next
    End Sub

    Private Sub DrawTextCore(s As String, font As Font, brush As Brush, rect As D2D1_RECT_F, angle As Single, originX As Single, originY As Single)
        Call EndBatch()

        If String.IsNullOrEmpty(s) Then
            Return
        End If

        Dim format As IDWriteTextFormat = brushes.GetTextFormat(font)
        Dim fill As ID2D1Brush = brushes.GetBrush(brush)
        Dim textPtr As IntPtr = Marshal.StringToCoTaskMemUni(s)

        Try
            If angle = 0.0F Then
                Call renderTarget.Target.DrawText(textPtr, CUInt(s.Length), format, rect, fill, D2D1_DRAW_TEXT_OPTIONS.NONE, DWRITE_MEASURING_MODE.NATURAL)
            Else
                Dim current As D2D1_MATRIX_3X2_F = transform
                Dim rotated As D2D1_MATRIX_3X2_F = Multiply(current, RotationMatrix(angle, originX, originY))

                Call renderTarget.Target.SetTransform(rotated)
                Call renderTarget.Target.DrawText(textPtr, CUInt(s.Length), format, rect, fill, D2D1_DRAW_TEXT_OPTIONS.NONE, DWRITE_MEASURING_MODE.NATURAL)
                Call renderTarget.Target.SetTransform(current)
            End If
        Finally
            Call Marshal.ZeroFreeCoTaskMemUnicode(textPtr)
        End Try
    End Sub

    Private Sub DrawImageCore(image As Image, dest As RectangleF)
        Call EndBatch()

        If image Is Nothing Then
            Return
        End If

        Dim buffer As Byte() = GetPixelBuffer(image)

        If buffer Is Nothing Then
            Throw New NotSupportedException($"the image model '{image.GetType.Name}' is not a raster buffer, it can not be drawn by the directx canvas.")
        End If

        Dim stride As Integer = image.Width * 4

        If buffer.Length <> stride * image.Height Then
            Throw New NotSupportedException("only the 32bpp BGRA raster image is supported by the directx canvas.")
        End If

        Dim pixels As Byte() = Premultiply(CType(buffer.Clone(), Byte()))
        Dim handle As GCHandle = GCHandle.Alloc(pixels, GCHandleType.Pinned)

        Try
            Dim props As New D2D1_BITMAP_PROPERTIES With {
                .pixelFormat = New D2D1_PIXEL_FORMAT With {
                    .format = DXGI_FORMAT.B8G8R8A8_UNORM,
                    .alphaMode = D2D1_ALPHA_MODE.PREMULTIPLIED
                },
                .dpiX = 96.0F,
                .dpiY = 96.0F
            }
            Dim bitmapPtr As IntPtr = IntPtr.Zero

            Call ThrowIfFailed(
                renderTarget.Target.CreateBitmap(
                    Pack64(image.Width, image.Height),
                    handle.AddrOfPinnedObject(),
                    CUInt(stride), props, bitmapPtr),
                "ID2D1RenderTarget::CreateBitmap"
            )

            Dim bitmap As ID2D1Bitmap = ComObject(Of ID2D1Bitmap)(bitmapPtr)

            Try
                Call renderTarget.Target.DrawBitmap(bitmap, ToRectF(dest), 1.0F, D2D1_BITMAP_INTERPOLATION_MODE.LINEAR, IntPtr.Zero)
            Finally
                Call SafeRelease(bitmap)
            End Try
        Finally
            Call handle.Free()
        End Try
    End Sub

    Private Shared Function Slice(points As PointF(), offset As Integer, numberOfSegments As Integer) As PointF()
        If offset < 0 Then
            offset = 0
        End If

        If numberOfSegments <= 0 Then
            numberOfSegments = points.Length - offset - 1
        End If

        Dim n As Integer = std.Min(numberOfSegments + 1, points.Length - offset)

        If n <= 0 Then
            Return New PointF() {}
        End If

        Dim buffer As PointF() = New PointF(n - 1) {}

        Call Array.Copy(points, offset, buffer, 0, n)

        Return buffer
    End Function

    Private Shared Function Slice(points As Point(), offset As Integer, numberOfSegments As Integer) As Point()
        Dim buffer As PointF() = Slice(points.Select(Function(p) New PointF(p.X, p.Y)).ToArray(), offset, numberOfSegments)
        Dim result As Point() = New Point(buffer.Length - 1) {}

        For i As Integer = 0 To buffer.Length - 1
            result(i) = New Point(CInt(buffer(i).X), CInt(buffer(i).Y))
        Next

        Return result
    End Function

    ' /********************************************************************************/
    '  the image output
    ' /********************************************************************************/

    ''' <summary>
    ''' read back the gpu texture and generate the raster image output
    ''' </summary>
    Public ReadOnly Property ImageResource As Image Implements GdiRasterGraphics.ImageResource
        Get
            Return GetRasterImage()
        End Get
    End Property

    ''' <summary>
    ''' read back the rendered pixels from the gpu device to the managed memory
    ''' </summary>
    ''' <returns>
    ''' a raster image object of the current canvas content
    ''' </returns>
    Public Function GetRasterImage() As Bitmap
        If m_released Then
            Throw New ObjectDisposedException(NameOf(DxGraphics))
        End If

        Call EndBatch()
        Call PopAllClips()

        Dim pixels As Byte() = renderTarget.ReadPixels()

        Call PushAllClips()

        Return CreateBitmap(Unpremultiply(pixels), Width, Height)
    End Function

    ''' <summary>
    ''' submit all of the pending polygon primitives of the batch submitter
    ''' </summary>
    Public Sub FlushBatch()
        Call batch.Flush()
    End Sub

    Public Function Save(file As String, Optional format As ImageFormats = ImageFormats.Png) As Boolean
        Call GetRasterImage().Save(file, format)

        Return True
    End Function

    Public Function Save(stream As Stream, format As ImageFormats) As Boolean Implements SaveGdiBitmap.Save
        Call GetRasterImage().Save(stream, format)

        Return True
    End Function

    ' /********************************************************************************/
    '  IGraphics implementation
    ' /********************************************************************************/

    Public Overrides Sub AddMetafileComment(data() As Byte)
        ' the direct2d backend does not produce any metafile output
    End Sub

    Protected Overrides Sub ClearCanvas(color As System.Drawing.Color)
        Call EndBatch()

        Dim fill As D2D1_COLOR_F = ToColorF(color)

        Call renderTarget.Target.Clear(fill)
    End Sub

    Public Overrides Sub Flush()
        Call EndBatch()
        Call renderTarget.Flush()
    End Sub

    Protected Overrides Sub ReleaseHandle()
        If m_released Then
            Return
        End If

        m_released = True

        Try
            Call batch.Flush()
            Call PopAllClips()
        Catch
        End Try

        Call SafeRelease(batch)
        Call SafeRelease(brushes)
        Call SafeRelease(renderTarget)
    End Sub

    ' /********************************************************************************/
    '  arc, bezier and curve
    ' /********************************************************************************/

    Public Overrides Sub DrawArc(pen As Pen, rect As System.Drawing.RectangleF, startAngle As Single, sweepAngle As Single)
        Call EndBatch()
        Call DrawGeometry(DxPathBuilder.Arc(Factory, rect, startAngle, sweepAngle), pen)
    End Sub

    Public Overrides Sub DrawArc(pen As Pen, rect As System.Drawing.Rectangle, startAngle As Single, sweepAngle As Single)
        Call DrawArc(pen, New RectangleF(rect.Left, rect.Top, rect.Width, rect.Height), startAngle, sweepAngle)
    End Sub

    Public Overrides Sub DrawArc(pen As Pen, x As Integer, y As Integer, width As Integer, height As Integer, startAngle As Integer, sweepAngle As Integer)
        Call DrawArc(pen, New RectangleF(x, y, width, height), startAngle, sweepAngle)
    End Sub

    Public Overrides Sub DrawArc(pen As Pen, x As Single, y As Single, width As Single, height As Single, startAngle As Single, sweepAngle As Single)
        Call DrawArc(pen, New RectangleF(x, y, width, height), startAngle, sweepAngle)
    End Sub

    Public Overrides Sub DrawBezier(pen As Pen, pt1 As System.Drawing.Point, pt2 As System.Drawing.Point, pt3 As System.Drawing.Point, pt4 As System.Drawing.Point)
        Call DrawBezier(pen,
            New PointF(pt1.X, pt1.Y), New PointF(pt2.X, pt2.Y),
            New PointF(pt3.X, pt3.Y), New PointF(pt4.X, pt4.Y))
    End Sub

    Public Overrides Sub DrawBezier(pen As Pen, pt1 As System.Drawing.PointF, pt2 As System.Drawing.PointF, pt3 As System.Drawing.PointF, pt4 As System.Drawing.PointF)
        Call EndBatch()
        Call DrawGeometry(DxPathBuilder.Bezier(Factory, ToPoint2F(pt1), ToPoint2F(pt2), ToPoint2F(pt3), ToPoint2F(pt4)), pen)
    End Sub

    Public Overrides Sub DrawBezier(pen As Pen, x1 As Single, y1 As Single, x2 As Single, y2 As Single, x3 As Single, y3 As Single, x4 As Single, y4 As Single)
        Call DrawBezier(pen, New PointF(x1, y1), New PointF(x2, y2), New PointF(x3, y3), New PointF(x4, y4))
    End Sub

    Public Overrides Sub DrawBeziers(pen As Pen, points() As System.Drawing.PointF)
        Call EndBatch()
        Call DrawGeometry(DxPathBuilder.Beziers(Factory, ToPoints(points)), pen)
    End Sub

    Public Overrides Sub DrawBeziers(pen As Pen, points() As System.Drawing.Point)
        Call DrawBeziers(pen, points.Select(Function(p) New PointF(p.X, p.Y)).ToArray())
    End Sub

    Public Overrides Sub DrawClosedCurve(pen As Pen, points() As System.Drawing.Point)
        Call DrawClosedCurve(pen, points.Select(Function(p) New PointF(p.X, p.Y)).ToArray())
    End Sub

    Public Overrides Sub DrawClosedCurve(pen As Pen, points() As System.Drawing.PointF)
        Call EndBatch()
        Call DrawGeometry(DxPathBuilder.Curve(Factory, ToPoints(points), 0.5F, closed:=True), pen)
    End Sub

    Public Overrides Sub DrawCurve(pen As Pen, points() As System.Drawing.Point)
        Call DrawCurve(pen, points.Select(Function(p) New PointF(p.X, p.Y)).ToArray())
    End Sub

    Public Overrides Sub DrawCurve(pen As Pen, points() As System.Drawing.PointF)
        Call EndBatch()
        Call DrawGeometry(DxPathBuilder.Curve(Factory, ToPoints(points), 0.5F, closed:=False), pen)
    End Sub

    Public Overrides Sub DrawCurve(pen As Pen, points() As System.Drawing.PointF, tension As Single)
        Call EndBatch()
        Call DrawGeometry(DxPathBuilder.Curve(Factory, ToPoints(points), tension, closed:=False), pen)
    End Sub

    Public Overrides Sub DrawCurve(pen As Pen, points() As System.Drawing.Point, tension As Single)
        Call DrawCurve(pen, points.Select(Function(p) New PointF(p.X, p.Y)).ToArray(), tension)
    End Sub

    Public Overrides Sub DrawCurve(pen As Pen, points() As System.Drawing.PointF, offset As Integer, numberOfSegments As Integer)
        Call DrawCurve(pen, Slice(points, offset, numberOfSegments))
    End Sub

    Public Overrides Sub DrawCurve(pen As Pen, points() As System.Drawing.PointF, offset As Integer, numberOfSegments As Integer, tension As Single)
        Call DrawCurve(pen, Slice(points, offset, numberOfSegments), tension)
    End Sub

    Public Overrides Sub DrawCurve(pen As Pen, points() As System.Drawing.Point, offset As Integer, numberOfSegments As Integer, tension As Single)
        Call DrawCurve(pen, Slice(points, offset, numberOfSegments), tension)
    End Sub

    ' /********************************************************************************/
    '  ellipse
    ' /********************************************************************************/

    Private ReadOnly Property Factory As ID2D1Factory
        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Get
            Return renderTarget.Device.Factory2D
        End Get
    End Property

    Public Overrides Sub DrawEllipse(pen As Pen, rect As System.Drawing.Rectangle)
        Call DrawEllipse(pen, New RectangleF(rect.Left, rect.Top, rect.Width, rect.Height))
    End Sub

    Public Overrides Sub DrawEllipse(pen As Pen, rect As System.Drawing.RectangleF)
        Call EndBatch()

        Dim e As D2D1_ELLIPSE = DxPathBuilder.Ellipse(rect)

        Call renderTarget.Target.DrawEllipse(e, brushes.GetBrush(pen.Color), pen.Width, brushes.GetStrokeStyle(pen))
    End Sub

    Public Overrides Sub DrawEllipse(pen As Pen, x As Single, y As Single, width As Single, height As Single)
        Call DrawEllipse(pen, New RectangleF(x, y, width, height))
    End Sub

    Public Overrides Sub DrawEllipse(pen As Pen, x As Integer, y As Integer, width As Integer, height As Integer)
        Call DrawEllipse(pen, New RectangleF(x, y, width, height))
    End Sub

    Public Overrides Sub FillEllipse(brush As Brush, rect As System.Drawing.Rectangle)
        Call FillEllipse(brush, New RectangleF(rect.Left, rect.Top, rect.Width, rect.Height))
    End Sub

    Public Overrides Sub FillEllipse(brush As Brush, rect As System.Drawing.RectangleF)
        Call EndBatch()

        Dim e As D2D1_ELLIPSE = DxPathBuilder.Ellipse(rect)

        Call renderTarget.Target.FillEllipse(e, brushes.GetBrush(brush))
    End Sub

    Public Overrides Sub FillEllipse(brush As Brush, x As Single, y As Single, width As Single, height As Single)
        Call FillEllipse(brush, New RectangleF(x, y, width, height))
    End Sub

    Public Overrides Sub FillEllipse(brush As Brush, x As Integer, y As Integer, width As Integer, height As Integer)
        Call FillEllipse(brush, New RectangleF(x, y, width, height))
    End Sub

    Public Overrides Sub DrawCircle(center As System.Drawing.PointF, fill As System.Drawing.Color, stroke As Pen, radius As Single)
        Dim rect As New RectangleF(center.X - radius, center.Y - radius, radius * 2.0F, radius * 2.0F)

        If Not fill.IsEmpty AndAlso fill.A > 0 Then
            Call FillEllipse(New SolidBrush(fill), rect)
        End If

        If stroke IsNot Nothing Then
            Call DrawEllipse(stroke, rect)
        End If
    End Sub

    ' /********************************************************************************/
    '  image drawing
    ' /********************************************************************************/

    Public Overrides Sub DrawImage(image As Image, point As System.Drawing.Point)
        Call DrawImage(image, CSng(point.X), CSng(point.Y))
    End Sub

    Public Overrides Sub DrawImage(image As Image, point As System.Drawing.PointF)
        Call DrawImage(image, point.X, point.Y)
    End Sub

    Public Overrides Sub DrawImage(image As Image, x As Integer, y As Integer)
        Call DrawImage(image, CSng(x), CSng(y))
    End Sub

    Public Overrides Sub DrawImage(image As Image, x As Single, y As Single)
        Call DrawImage(image, x, y, CSng(image.Width), CSng(image.Height))
    End Sub

    Public Overrides Sub DrawImage(image As Image, rect As System.Drawing.Rectangle)
        Call DrawImage(image, New RectangleF(rect.Left, rect.Top, rect.Width, rect.Height))
    End Sub

    Public Overrides Sub DrawImage(image As Image, rect As System.Drawing.RectangleF)
        Call DrawImageCore(image, rect)
    End Sub

    Public Overrides Sub DrawImage(image As Image, x As Single, y As Single, width As Single, height As Single)
        Call DrawImageCore(image, New RectangleF(x, y, width, height))
    End Sub

    Public Overrides Sub DrawImage(image As Image, x As Integer, y As Integer, width As Integer, height As Integer)
        Call DrawImageCore(image, New RectangleF(x, y, width, height))
    End Sub

    Public Overrides Sub DrawImage(image As Image, destPoints() As System.Drawing.Point)
        Call DrawImageCore(image, Bounds(destPoints))
    End Sub

    Public Overrides Sub DrawImage(image As Image, destPoints() As System.Drawing.PointF)
        Call DrawImageCore(image, Bounds(destPoints))
    End Sub

    Public Overrides Sub DrawImageUnscaled(image As Image, rect As System.Drawing.Rectangle)
        Call DrawImageCore(image, New RectangleF(rect.Left, rect.Top, CSng(image.Width), CSng(image.Height)))
    End Sub

    Public Overrides Sub DrawImageUnscaled(image As Image, point As System.Drawing.Point)
        Call DrawImage(image, point.X, point.Y)
    End Sub

    Public Overrides Sub DrawImageUnscaled(image As Image, x As Integer, y As Integer)
        Call DrawImage(image, x, y)
    End Sub

    Public Overrides Sub DrawImageUnscaled(image As Image, x As Integer, y As Integer, width As Integer, height As Integer)
        Call DrawImage(image, x, y)
    End Sub

    Public Overrides Sub DrawImageUnscaledAndClipped(image As Image, rect As System.Drawing.Rectangle)
        Call PushClip(New RectangleF(rect.Left, rect.Top, rect.Width, rect.Height))

        Try
            Call DrawImage(image, rect.Left, rect.Top)
        Finally
            Call renderTarget.Target.PopAxisAlignedClip()
            Call clips.RemoveAt(clips.Count - 1)
        End Try
    End Sub

    Private Shared Function Bounds(points As PointF()) As RectangleF
        Dim minX As Single = Single.MaxValue, minY As Single = Single.MaxValue
        Dim maxX As Single = Single.MinValue, maxY As Single = Single.MinValue

        For Each p As PointF In points
            If p.X < minX Then minX = p.X
            If p.Y < minY Then minY = p.Y
            If p.X > maxX Then maxX = p.X
            If p.Y > maxY Then maxY = p.Y
        Next

        Return New RectangleF(minX, minY, maxX - minX, maxY - minY)
    End Function

    Private Shared Function Bounds(points As Point()) As RectangleF
        Return Bounds(points.Select(Function(p) New PointF(p.X, p.Y)).ToArray())
    End Function

    ' /********************************************************************************/
    '  line and polygon
    ' /********************************************************************************/

    Public Overrides Sub DrawLine(pen As Pen, pt1 As System.Drawing.PointF, pt2 As System.Drawing.PointF)
        Call EndBatch()
        Call renderTarget.Target.DrawLine(Pack64(ToPoint2F(pt1)), Pack64(ToPoint2F(pt2)), brushes.GetBrush(pen.Color), pen.Width, brushes.GetStrokeStyle(pen))
    End Sub

    Public Overrides Sub DrawLine(pen As Pen, pt1 As System.Drawing.Point, pt2 As System.Drawing.Point)
        Call DrawLine(pen, New PointF(pt1.X, pt1.Y), New PointF(pt2.X, pt2.Y))
    End Sub

    Public Overrides Sub DrawLine(pen As Pen, x1 As Integer, y1 As Integer, x2 As Integer, y2 As Integer)
        Call DrawLine(pen, New PointF(x1, y1), New PointF(x2, y2))
    End Sub

    Public Overrides Sub DrawLine(pen As Pen, x1 As Single, y1 As Single, x2 As Single, y2 As Single)
        Call DrawLine(pen, New PointF(x1, y1), New PointF(x2, y2))
    End Sub

    Public Overrides Sub DrawLines(pen As Pen, points() As System.Drawing.PointF)
        Call EndBatch()
        Call DrawGeometry(DxPathBuilder.Polygon(Factory, ToPoints(points), closed:=False), pen)
    End Sub

    Public Overrides Sub DrawLines(pen As Pen, points() As System.Drawing.Point)
        Call DrawLines(pen, points.Select(Function(p) New PointF(p.X, p.Y)).ToArray())
    End Sub

    Public Overrides Sub DrawPath(pen As Pen, path As GraphicsPath)
        Call EndBatch()
        Call DrawGeometry(DxPathBuilder.FromGraphicsPath(Factory, path), pen)
    End Sub

    Public Overrides Sub FillPath(brush As Brush, path As GraphicsPath)
        Call EndBatch()
        Call FillGeometry(DxPathBuilder.FromGraphicsPath(Factory, path), brush)
    End Sub

    ''' <summary>
    ''' fill a polygon primitive, the polygon is appended into the batch
    ''' submitter when <see cref="BatchFill"/> is enabled
    ''' </summary>
    Public Overrides Sub FillPolygon(brush As Brush, points() As System.Drawing.PointF)
        If points Is Nothing OrElse points.Length < 3 Then
            Return
        End If

        Dim fill As ID2D1Brush = brushes.GetBrush(brush)
        Dim key As String = Brush.SolidColor(brush).ToArgb().ToString()

        Call PolygonPrimitive(ToPoints(points), stroke:=False, brush:=fill, key:=key)
    End Sub

    Public Overrides Sub FillPolygon(brush As Brush, points() As System.Drawing.Point)
        Call FillPolygon(brush, points.Select(Function(p) New PointF(p.X, p.Y)).ToArray())
    End Sub

    ''' <summary>
    ''' stroke a polygon primitive, the polygon is appended into the batch
    ''' submitter when <see cref="BatchFill"/> is enabled
    ''' </summary>
    Public Overrides Sub DrawPolygon(pen As Pen, points() As System.Drawing.PointF)
        If points Is Nothing OrElse points.Length < 2 Then
            Return
        End If

        Call PolygonPrimitive(
            ToPoints(points),
            stroke:=True,
            brush:=brushes.GetBrush(pen.Color),
            key:=PenKey(pen),
            width:=pen.Width,
            style:=brushes.GetStrokeStyle(pen)
        )
    End Sub

    Public Overrides Sub DrawPolygon(pen As Pen, points() As System.Drawing.Point)
        Call DrawPolygon(pen, points.Select(Function(p) New PointF(p.X, p.Y)).ToArray())
    End Sub

    Public Overrides Sub FillClosedCurve(brush As Brush, points() As System.Drawing.PointF)
        Call EndBatch()
        Call FillGeometry(DxPathBuilder.Curve(Factory, ToPoints(points), 0.5F, closed:=True), brush)
    End Sub

    Public Overrides Sub FillClosedCurve(brush As Brush, points() As System.Drawing.Point)
        Call FillClosedCurve(brush, points.Select(Function(p) New PointF(p.X, p.Y)).ToArray())
    End Sub

    ' /********************************************************************************/
    '  pie
    ' /********************************************************************************/

    Public Overrides Sub DrawPie(pen As Pen, rect As System.Drawing.Rectangle, startAngle As Single, sweepAngle As Single)
        Call DrawPie(pen, New RectangleF(rect.Left, rect.Top, rect.Width, rect.Height), startAngle, sweepAngle)
    End Sub

    Public Overrides Sub DrawPie(pen As Pen, rect As System.Drawing.RectangleF, startAngle As Single, sweepAngle As Single)
        Call EndBatch()
        Call DrawGeometry(DxPathBuilder.Pie(Factory, rect, startAngle, sweepAngle), pen)
    End Sub

    Public Overrides Sub DrawPie(pen As Pen, x As Integer, y As Integer, width As Integer, height As Integer, startAngle As Integer, sweepAngle As Integer)
        Call DrawPie(pen, New RectangleF(x, y, width, height), startAngle, sweepAngle)
    End Sub

    Public Overrides Sub DrawPie(pen As Pen, x As Single, y As Single, width As Single, height As Single, startAngle As Single, sweepAngle As Single)
        Call DrawPie(pen, New RectangleF(x, y, width, height), startAngle, sweepAngle)
    End Sub

    Public Overrides Sub FillPie(brush As Brush, rect As System.Drawing.Rectangle, startAngle As Single, sweepAngle As Single)
        Call FillPie(brush, New RectangleF(rect.Left, rect.Top, rect.Width, rect.Height), startAngle, sweepAngle)
    End Sub

    Public Overrides Sub FillPie(brush As Brush, x As Integer, y As Integer, width As Integer, height As Integer, startAngle As Integer, sweepAngle As Integer)
        Call FillPie(brush, New RectangleF(x, y, width, height), startAngle, sweepAngle)
    End Sub

    Public Overrides Sub FillPie(brush As Brush, x As Single, y As Single, width As Single, height As Single, startAngle As Single, sweepAngle As Single)
        Call FillPie(brush, New RectangleF(x, y, width, height), startAngle, sweepAngle)
    End Sub

    ' /********************************************************************************/
    '  rectangle
    ' /********************************************************************************/

    Public Overrides Sub DrawRectangle(pen As Pen, rect As System.Drawing.Rectangle)
        Call DrawRectangle(pen, New RectangleF(rect.Left, rect.Top, rect.Width, rect.Height))
    End Sub

    Public Overrides Sub DrawRectangle(pen As Pen, rect As System.Drawing.RectangleF)
        Call EndBatch()
        Call renderTarget.Target.DrawRectangle(ToRectF(rect), brushes.GetBrush(pen.Color), pen.Width, brushes.GetStrokeStyle(pen))
    End Sub

    Public Overrides Sub DrawRectangle(pen As Pen, x As Single, y As Single, width As Single, height As Single)
        Call DrawRectangle(pen, New RectangleF(x, y, width, height))
    End Sub

    Public Overrides Sub DrawRectangle(pen As Pen, x As Integer, y As Integer, width As Integer, height As Integer)
        Call DrawRectangle(pen, New RectangleF(x, y, width, height))
    End Sub

    Public Overrides Sub DrawRectangles(pen As Pen, rects() As System.Drawing.RectangleF)
        Call EndBatch()

        Dim fill As ID2D1Brush = brushes.GetBrush(pen.Color)
        Dim style As ID2D1StrokeStyle = brushes.GetStrokeStyle(pen)

        For Each rect As RectangleF In rects
            Call renderTarget.Target.DrawRectangle(ToRectF(rect), fill, pen.Width, style)
        Next
    End Sub

    Public Overrides Sub DrawRectangles(pen As Pen, rects() As System.Drawing.Rectangle)
        Call DrawRectangles(pen, rects.Select(Function(r) New RectangleF(r.Left, r.Top, r.Width, r.Height)).ToArray())
    End Sub

    Public Overrides Sub FillRectangle(brush As Brush, rect As System.Drawing.Rectangle)
        Call FillRectangle(brush, New RectangleF(rect.Left, rect.Top, rect.Width, rect.Height))
    End Sub

    Public Overrides Sub FillRectangle(brush As Brush, rect As System.Drawing.RectangleF)
        Call EndBatch()
        Call renderTarget.Target.FillRectangle(ToRectF(rect), brushes.GetBrush(brush))
    End Sub

    Public Overrides Sub FillRectangle(brush As Brush, x As Single, y As Single, width As Single, height As Single)
        Call FillRectangle(brush, New RectangleF(x, y, width, height))
    End Sub

    Public Overrides Sub FillRectangle(brush As Brush, x As Integer, y As Integer, width As Integer, height As Integer)
        Call FillRectangle(brush, New RectangleF(x, y, width, height))
    End Sub

    ' /********************************************************************************/
    '  text
    ' /********************************************************************************/

    Public Overrides Sub DrawString(s As String, font As Font, brush As Brush, ByRef point As System.Drawing.PointF)
        Call DrawString(s, font, brush, point.X, point.Y)
    End Sub

    Public Overrides Sub DrawString(s As String, font As Font, brush As Brush, layoutRectangle As System.Drawing.RectangleF)
        If String.IsNullOrEmpty(s) Then
            Return
        End If

        Call DrawTextCore(s, font, brush, ToRectF(layoutRectangle), 0.0F, layoutRectangle.Left, layoutRectangle.Top)
    End Sub

    Public Overrides Sub DrawString(s As String, font As Font, brush As Brush, ByRef x As Single, ByRef y As Single, angle As Single)
        If String.IsNullOrEmpty(s) Then
            Return
        End If

        Dim rect As New D2D1_RECT_F With {
            .Left = x, .Top = y,
            .Right = x + 1.0E+07F, .Bottom = y + 1.0E+07F
        }

        Call DrawTextCore(s, font, brush, rect, angle, x, y)
    End Sub

    Public Overrides Sub DrawString(s As String, font As Font, brush As Brush, x As Single, y As Single)
        If String.IsNullOrEmpty(s) Then
            Return
        End If

        Dim rect As New D2D1_RECT_F With {
            .Left = x, .Top = y,
            .Right = x + 1.0E+07F, .Bottom = y + 1.0E+07F
        }

        Call DrawTextCore(s, font, brush, rect, 0.0F, x, y)
    End Sub

    ' /********************************************************************************/
    '  clip and transform
    ' /********************************************************************************/

    Public Overrides Sub SetClip(rect As System.Drawing.RectangleF)
        Call EndBatch()
        Call PopAllClips()
        Call PushClip(rect)
    End Sub

    Public Overrides Sub SetClip(rect As System.Drawing.Rectangle)
        Call SetClip(New RectangleF(rect.Left, rect.Top, rect.Width, rect.Height))
    End Sub

    Public Overrides Sub IntersectClip(rect As System.Drawing.RectangleF)
        Call EndBatch()

        If clips.Count > 0 Then
            Dim merged As RectangleF = RectangleF.Intersect(clips(clips.Count - 1), rect)

            If merged.IsEmpty Then
                merged = RectangleF.Empty
            End If

            Call renderTarget.Target.PopAxisAlignedClip()
            Call clips.RemoveAt(clips.Count - 1)
            Call PushClip(merged)
        Else
            Call PushClip(rect)
        End If
    End Sub

    Public Overrides Sub IntersectClip(rect As System.Drawing.Rectangle)
        Call IntersectClip(New RectangleF(rect.Left, rect.Top, rect.Width, rect.Height))
    End Sub

    ''' <summary>
    ''' Direct2D only supports an axis aligned rectangle clip, the exclude
    ''' clip operation is not available in this backend.
    ''' </summary>
    Public Overrides Sub ExcludeClip(rect As System.Drawing.Rectangle)
        ' there is no exclude clip support in the direct2d api
        Call $"ExcludeClip is not supported by the direct2d canvas, the clip region keeps unchanged.".warning
    End Sub

    Public Overrides Sub ResetClip()
        Call EndBatch()
        Call PopAllClips()
    End Sub

    Public Overrides Sub TranslateClip(dx As Single, dy As Single)
        If clips.Count = 0 Then
            Return
        End If

        Dim rect As RectangleF = clips(clips.Count - 1)

        rect.X += dx
        rect.Y += dy

        Call renderTarget.Target.PopAxisAlignedClip()
        Call clips.RemoveAt(clips.Count - 1)
        Call PushClip(rect)
    End Sub

    Public Overrides Sub TranslateClip(dx As Integer, dy As Integer)
        Call TranslateClip(CSng(dx), CSng(dy))
    End Sub

    Public Overrides Sub ResetTransform()
        Call EndBatch()

        transform = IdentityMatrix()

        Call renderTarget.Target.SetTransform(transform)
    End Sub

    Public Overrides Sub RotateTransform(angle As Single)
        Call EndBatch()

        transform = Multiply(transform, RotationMatrix(angle, 0.0F, 0.0F))

        Call renderTarget.Target.SetTransform(transform)
    End Sub

    Public Overrides Sub ScaleTransform(sx As Single, sy As Single)
        Call EndBatch()

        transform = Multiply(transform, Scaling(sx, sy))

        Call renderTarget.Target.SetTransform(transform)
    End Sub

    Public Overrides Sub TranslateTransform(dx As Single, dy As Single)
        Call EndBatch()

        transform = Multiply(transform, Translation(dx, dy))

        Call renderTarget.Target.SetTransform(transform)
    End Sub

    ' /********************************************************************************/
    '  context and metrics
    ' /********************************************************************************/

    Public Overrides Function GetContextInfo() As GraphicsContextInfo
        Return New GraphicsContextInfo With {
            .Offset = New PointF(transform.dx, transform.dy),
            .Context = renderTarget
        }
    End Function

    Public Overrides Function IsVisible(rect As System.Drawing.Rectangle) As Boolean
        Return IsVisible(New RectangleF(rect.Left, rect.Top, rect.Width, rect.Height))
    End Function

    Public Overrides Function IsVisible(rect As System.Drawing.RectangleF) As Boolean
        Return rect.Right > 0 AndAlso rect.Bottom > 0 AndAlso rect.Left < Width AndAlso rect.Top < Height
    End Function

    Public Overrides Function IsVisible(x As Integer, y As Integer, width As Integer, height As Integer) As Boolean
        Return IsVisible(New RectangleF(x, y, width, height))
    End Function

    Public Overrides Function IsVisible(x As Single, y As Single, width As Single, height As Single) As Boolean
        Return IsVisible(New RectangleF(x, y, width, height))
    End Function

    Public Overrides Function MeasureString(text As String, font As Font) As System.Drawing.SizeF
        Return brushes.MeasureText(text, font)
    End Function

    Public Overrides Function MeasureString(text As String, font As Font, width As Integer) As System.Drawing.SizeF
        Return brushes.MeasureText(text, font, CSng(width))
    End Function

    Public Overrides Function MeasureString(text As String, font As Font, layoutArea As System.Drawing.SizeF) As System.Drawing.SizeF
        Return brushes.MeasureText(text, font, layoutArea.Width)
    End Function

    ''' <summary>
    ''' get the outline path of a text string
    ''' </summary>
    ''' <remarks>
    ''' the directwrite glyph outline streaming requires a IDWriteFontFace
    ''' geometry sink implementation, this backend returns the bounding box
    ''' rectangle of the text layout as the approximated path.
    ''' </remarks>
    Public Overrides Function GetStringPath(s As String, rect As System.Drawing.RectangleF, font As Font) As GraphicsPath
        Dim size As SizeF = MeasureString(s, font)
        Dim path As New GraphicsPath()

        Call path.AddRectangle(New Rectangle(CInt(rect.Left), CInt(rect.Top), CInt(size.Width), CInt(size.Height)))

        Return path
    End Function
End Class
