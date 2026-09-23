Imports System.Drawing
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports Microsoft.VisualBasic.Imaging
Imports std = System.Math

Imports Brush = Microsoft.VisualBasic.Imaging.Brush
Imports Font = Microsoft.VisualBasic.Imaging.Font
Imports Image = Microsoft.VisualBasic.Imaging.Image
Imports LinearGradientBrush = Microsoft.VisualBasic.Imaging.LinearGradientBrush
Imports Pen = Microsoft.VisualBasic.Imaging.Pen
Imports SolidBrush = Microsoft.VisualBasic.Imaging.SolidBrush
Imports TextureBrush = Microsoft.VisualBasic.Imaging.TextureBrush

''' <summary>
''' The direct2d resource cache of a render target.
''' </summary>
''' <remarks>
''' Creating a direct2d brush / text format / stroke style object is a
''' relatively expensive com operation, drawing a large amount of the
''' polygon primitives with the same color should reuse the same brush
''' object instead of creating a new one for each primitive.
''' </remarks>
Friend Class DxBrushCache : Implements IDisposable

    Private ReadOnly target As ID2D1RenderTarget
    Private ReadOnly factory As ID2D1Factory
    Private ReadOnly writeFactory As IDWriteFactory

    Private ReadOnly solids As New Dictionary(Of Integer, ID2D1Brush)()
    Private ReadOnly gradients As New Dictionary(Of String, ID2D1Brush)()
    Private ReadOnly styles As New Dictionary(Of String, ID2D1StrokeStyle)()
    Private ReadOnly formats As New Dictionary(Of String, IDWriteTextFormat)()

    Private m_disposed As Boolean = False

    Friend Sub New(renderTarget As ID2D1RenderTarget, factory2d As ID2D1Factory, factoryWrite As IDWriteFactory)
        target = renderTarget
        factory = factory2d
        writeFactory = factoryWrite
    End Sub

    ''' <summary>
    ''' get or create a solid color brush of the given color
    ''' </summary>
    Friend Function GetBrush(color As Color) As ID2D1Brush
        Dim key As Integer = color.ToArgb()
        Dim brush As ID2D1Brush = Nothing

        If solids.TryGetValue(key, brush) Then
            Return brush
        End If

        Dim colorF As D2D1_COLOR_F = ToColorF(color)
        Dim brushPtr As IntPtr = IntPtr.Zero

        Call ThrowIfFailed(
            target.CreateSolidColorBrush(colorF, IntPtr.Zero, brushPtr),
            "ID2D1RenderTarget::CreateSolidColorBrush"
        )

        brush = ComObject(Of ID2D1Brush)(brushPtr)
        solids(key) = brush

        Return brush
    End Function

    ''' <summary>
    ''' convert a gdi+ style brush object into a direct2d brush
    ''' </summary>
    Friend Function GetBrush(brush As Brush) As ID2D1Brush
        If brush Is Nothing Then
            Return GetBrush(Color.Black)
        End If

        If TypeOf brush Is SolidBrush Then
            Return GetBrush(DirectCast(brush, SolidBrush).Color)
        End If

        If TypeOf brush Is LinearGradientBrush Then
            Return GetGradient(DirectCast(brush, LinearGradientBrush))
        End If

        ' the texture brush and the hatch brush are not supported by this
        ' backend yet, degrade to a solid color brush instead of throwing
        Dim fallback As Color = Brush.SolidColor(brush)

        If fallback.IsEmpty Then
            fallback = Color.Black
        End If

        Return GetBrush(fallback)
    End Function

    Private Function GetGradient(brush As LinearGradientBrush) As ID2D1Brush
        Dim colors As Color() = brush.LinearColors

        If colors Is Nothing OrElse colors.Length < 2 Then
            Return GetBrush(Color.Black)
        End If

        Dim rect As RectangleF = brush.Rectangle
        Dim key As String = $"{rect.X},{rect.Y},{rect.Width},{rect.Height},{brush.Angle}"

        For Each c As Color In colors
            key &= "|" & c.ToArgb()
        Next

        Dim found As ID2D1Brush = Nothing

        If gradients.TryGetValue(key, found) Then
            Return found
        End If

        ' the gradient axis, rotated by the given angle around the rect center
        Dim radians As Double = brush.Angle * std.PI / 180.0
        Dim dirX As Single = CSng(std.Cos(radians))
        Dim dirY As Single = CSng(std.Sin(radians))
        Dim half As Single = std.Abs(rect.Width / 2.0F * dirX) + std.Abs(rect.Height / 2.0F * dirY)
        Dim cx As Single = rect.Left + rect.Width / 2.0F
        Dim cy As Single = rect.Top + rect.Height / 2.0F
        Dim stops As D2D1_GRADIENT_STOP() = New D2D1_GRADIENT_STOP(colors.Length - 1) {}

        For i As Integer = 0 To colors.Length - 1
            stops(i) = New D2D1_GRADIENT_STOP With {
                .position = CSng(i) / (colors.Length - 1),
                .color = ToColorF(colors(i))
            }
        Next

        Dim collectionPtr As IntPtr = IntPtr.Zero

        Call ThrowIfFailed(
            target.CreateGradientStopCollection(stops, CUInt(stops.Length), D2D1_GAMMA.G22, D2D1_EXTEND_MODE.CLAMP, collectionPtr),
            "ID2D1RenderTarget::CreateGradientStopCollection"
        )

        Dim collection As ID2D1GradientStopCollection = ComObject(Of ID2D1GradientStopCollection)(collectionPtr)
        Dim props As New D2D1_LINEAR_GRADIENT_BRUSH_PROPERTIES With {
            .startPoint = New D2D1_POINT_2F With {.x = cx - dirX * half, .y = cy - dirY * half},
            .endPoint = New D2D1_POINT_2F With {.x = cx + dirX * half, .y = cy + dirY * half}
        }
        Dim gradientPtr As IntPtr = IntPtr.Zero

        Try
            Call ThrowIfFailed(
                target.CreateLinearGradientBrush(props, IntPtr.Zero, collection, gradientPtr),
                "ID2D1RenderTarget::CreateLinearGradientBrush"
            )
        Finally
            Call SafeRelease(collection)
        End Try

        Dim gradient As ID2D1Brush = ComObject(Of ID2D1Brush)(gradientPtr)

        gradients(key) = gradient

        Return gradient
    End Function

    ''' <summary>
    ''' does the pen describe a plain solid stroke line?
    ''' </summary>
    Friend Shared Function IsDefaultStroke(pen As Pen) As Boolean
        If pen.DashStyle <> DashStyle.Solid Then
            Return False
        End If

        If pen.DashPattern IsNot Nothing AndAlso pen.DashPattern.Length > 0 Then
            Return False
        End If

        If pen.DashOffset <> 0 OrElse pen.MiterLimit > 0 Then
            Return False
        End If

        Return pen.StartCap = LineCap.Flat AndAlso pen.EndCap = LineCap.Flat AndAlso pen.LineJoin = LineJoin.Miter
    End Function

    ''' <summary>
    ''' get or create the direct2d stroke style of the given pen object
    ''' </summary>
    ''' <remarks>
    ''' a default pen (solid line, butt cap and miter join) does not require a
    ''' stroke style object at all, so that the fastest path is taken for the
    ''' most common case.
    ''' </remarks>
    Friend Function GetStrokeStyle(pen As Pen) As ID2D1StrokeStyle
        If pen Is Nothing OrElse IsDefaultStroke(pen) Then
            Return Nothing
        End If

        Dim dash As Single() = If(pen.DashPattern, New Single() {})
        Dim key As String = $"{CInt(pen.DashStyle)}:{CInt(pen.StartCap)}:{CInt(pen.EndCap)}:{CInt(pen.DashCap)}:{CInt(pen.LineJoin)}:{pen.DashOffset}:{String.Join(",", dash)}"
        Dim style As ID2D1StrokeStyle = Nothing

        If styles.TryGetValue(key, style) Then
            Return style
        End If

        Dim props As New D2D1_STROKE_STYLE_PROPERTIES With {
            .startCap = CInt(pen.StartCap),
            .endCap = CInt(pen.EndCap),
            .dashCap = CInt(pen.DashCap),
            .lineJoin = CInt(pen.LineJoin),
            .miterLimit = If(pen.MiterLimit > 0, pen.MiterLimit, 10.0F),
            .dashStyle = CInt(pen.DashStyle),
            .dashOffset = pen.DashOffset
        }
        ' direct2d requires a null dash buffer when the dash array is empty
        Dim dashBuffer As Single() = If(dash.Length > 0, dash, Nothing)
        Dim stylePtr As IntPtr = IntPtr.Zero

        Call ThrowIfFailed(
            factory.CreateStrokeStyle(props, dashBuffer, CUInt(dash.Length), stylePtr),
            "ID2D1Factory::CreateStrokeStyle"
        )

        style = ComObject(Of ID2D1StrokeStyle)(stylePtr)
        styles(key) = style

        Return style
    End Function

    ''' <summary>
    ''' get or create the directwrite text format of the given font
    ''' </summary>
    Friend Function GetTextFormat(font As Font) As IDWriteTextFormat
        Dim key As String = $"{font.Name}|{font.Size}|{If(font.Bold, 1, 0)}|{If(font.Italic, 1, 0)}"
        Dim format As IDWriteTextFormat = Nothing

        If formats.TryGetValue(key, format) Then
            Return format
        End If

        Dim weight As Integer = If(font.Bold, DWRITE_FONT_WEIGHT.BOLD, DWRITE_FONT_WEIGHT.NORMAL)
        Dim style As Integer = If(font.Italic, DWRITE_FONT_STYLE.ITALIC, DWRITE_FONT_STYLE.NORMAL)
        Dim formatPtr As IntPtr = IntPtr.Zero

        Call ThrowIfFailed(
            writeFactory.CreateTextFormat(font.Name, IntPtr.Zero, weight, style, DWRITE_FONT_STRETCH.NORMAL, font.Size, "en-us", formatPtr),
            "IDWriteFactory::CreateTextFormat"
        )

        format = ComObject(Of IDWriteTextFormat)(formatPtr)
        formats(key) = format

        Return format
    End Function

    ''' <summary>
    ''' the vtable slot of <c>IDWriteTextLayout::GetMetrics</c>: the 25 methods of
    ''' <see cref="IDWriteTextFormat"/> occupy slot 3 to slot 27, and the layout
    ''' adds 32 more methods before its metrics getter
    ''' </summary>
    Private Const SLOT_GET_METRICS As Integer = 60

    <UnmanagedFunctionPointer(CallingConvention.StdCall)>
    Private Delegate Function GetMetricsFn(instance As IntPtr, ByRef metrics As DWRITE_TEXT_METRICS) As Integer

    ''' <summary>
    ''' measure the text size through the directwrite text layout api
    ''' </summary>
    ''' <remarks>
    ''' The text layout object can not be wrapped into a typed runtime callable
    ''' wrapper (the cast of <see cref="Marshal.GetTypedObjectForIUnknown(IntPtr, Type)"/>
    ''' fails for it), so its metrics are read through the raw vtable slot instead.
    ''' </remarks>
    Friend Function MeasureText(text As String, font As Font, Optional maxWidth As Single = 1.0E+07F) As SizeF
        If String.IsNullOrEmpty(text) Then
            Return SizeF.Empty
        End If

        Dim format As IDWriteTextFormat = GetTextFormat(font)
        Dim layoutPtr As IntPtr = IntPtr.Zero

        Call ThrowIfFailed(
            writeFactory.CreateTextLayout(text, CUInt(text.Length), format, maxWidth, 1.0E+07F, layoutPtr),
            "IDWriteFactory::CreateTextLayout"
        )

        Try
            Dim getMetrics As GetMetricsFn = ResolveVtable(Of GetMetricsFn)(layoutPtr, SLOT_GET_METRICS)
            Dim metrics As DWRITE_TEXT_METRICS

            Call ThrowIfFailed(getMetrics(layoutPtr, metrics), "IDWriteTextLayout::GetMetrics")

            Return New SizeF(metrics.width, metrics.height)
        Finally
            ' the create call hands out one reference of the layout object that
            ' this method owns
            Call Marshal.Release(layoutPtr)
        End Try
    End Function

    Private Sub Dispose(disposing As Boolean)
        If m_disposed Then
            Return
        End If

        m_disposed = True

        For Each brush As ID2D1Brush In solids.Values
            Call SafeRelease(brush)
        Next
        For Each brush As ID2D1Brush In gradients.Values
            Call SafeRelease(brush)
        Next
        For Each style As ID2D1StrokeStyle In styles.Values
            Call SafeRelease(style)
        Next
        For Each format As IDWriteTextFormat In formats.Values
            Call SafeRelease(format)
        Next

        Call solids.Clear()
        Call gradients.Clear()
        Call styles.Clear()
        Call formats.Clear()
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        Call Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub
End Class
