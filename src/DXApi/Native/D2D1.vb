Imports System.Runtime.InteropServices

Namespace Native

    ''' <summary>
    ''' The Direct2D (d2d1.dll) interop declarations.
    ''' </summary>
    ''' <remarks>
    ''' Direct2D is the real 2d drawing engine in this project: the polygon,
    ''' rectangle, ellipse, arc, bezier, text and image drawing are all
    ''' rasterized on the gpu through the ID2D1RenderTarget interface.
    ''' </remarks>
    Friend Module D2D1

        ''' <summary>
        ''' create the direct2d factory object
        ''' </summary>
        <DllImport("d2d1.dll", EntryPoint:="D2D1CreateFactory", PreserveSig:=True)>
        Friend Function D2D1CreateFactory(
            factoryType As UInteger,
            <[In]> ByRef riid As Guid,
            ByRef factoryOptions As D2D1_FACTORY_OPTIONS,
            <Out> ByRef factory As ID2D1Factory
        ) As Integer
        End Function

    End Module

    ''' <summary>
    ''' ID2D1Resource: the base object of all of the direct2d resources
    ''' </summary>
    <ComImport>
    <Guid("2cd90691-12e2-11dc-9fed-001143a055f9")>
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Friend Interface ID2D1Resource
        <PreserveSig> Sub GetFactory(<Out> ByRef factory As IntPtr)
    End Interface

    ''' <summary>
    ''' ID2D1Brush, all of the brush objects in this project are created as
    ''' this interface type so that no extra QueryInterface is required when
    ''' the brush is applied on the render target.
    ''' </summary>
    <ComImport>
    <Guid("2cd906a8-12e2-11dc-9fed-001143a055f9")>
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Friend Interface ID2D1Brush : Inherits ID2D1Resource
        <PreserveSig> Sub SetOpacity(opacity As Single)
        <PreserveSig> Sub SetTransform(ByRef transform As D2D1_MATRIX_3X2_F)
        <PreserveSig> Function GetOpacity() As Single
        <PreserveSig> Sub GetTransform(<Out> ByRef transform As D2D1_MATRIX_3X2_F)
    End Interface

    ''' <summary>
    ''' ID2D1GradientStopCollection, an opaque handle that is required by the
    ''' linear gradient brush creation api
    ''' </summary>
    <ComImport>
    <Guid("2cd906a7-12e2-11dc-9fed-001143a055f9")>
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Friend Interface ID2D1GradientStopCollection : Inherits ID2D1Resource
    End Interface

    ''' <summary>
    ''' ID2D1Bitmap, created from the raw pixel buffer for the image drawing
    ''' </summary>
    <ComImport>
    <Guid("a2296057-ea42-4099-983b-539fb6505426")>
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Friend Interface ID2D1Bitmap : Inherits ID2D1Resource
        <PreserveSig> Function GetSize() As D2D1_SIZE_F
        <PreserveSig> Function GetPixelSize() As D2D1_SIZE_U
    End Interface

    ''' <summary>
    ''' ID2D1StrokeStyle, the dash style of the stroke pen
    ''' </summary>
    <ComImport>
    <Guid("2cd9069d-12e2-11dc-9fed-001143a055f9")>
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Friend Interface ID2D1StrokeStyle : Inherits ID2D1Resource
    End Interface

    ' /********************************************************************************/
    '  geometry objects
    ' /********************************************************************************/

    <ComImport>
    <Guid("2cd906a1-12e2-11dc-9fed-001143a055f9")>
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Friend Interface ID2D1Geometry : Inherits ID2D1Resource

        ' slot 4
        <PreserveSig> Function GetBounds(worldTransform As IntPtr, <Out> ByRef bounds As D2D1_RECT_F) As Integer
        ' slot 5
        <PreserveSig> Function GetWidenedBounds(strokeWidth As Single, strokeStyle As ID2D1StrokeStyle, worldTransform As IntPtr, flatteningTolerance As Single, <Out> ByRef bounds As D2D1_RECT_F) As Integer
        ' slot 6
        <PreserveSig> Function StrokeContainsPoint(point As D2D1_POINT_2F, strokeWidth As Single, strokeStyle As ID2D1StrokeStyle, worldTransform As IntPtr, flatteningTolerance As Single, <Out> ByRef contains As Integer) As Integer
        ' slot 7
        <PreserveSig> Function FillContainsPoint(point As D2D1_POINT_2F, worldTransform As IntPtr, flatteningTolerance As Single, <Out> ByRef contains As Integer) As Integer
        ' slot 8
        <PreserveSig> Function CompareWithGeometry(geometry As ID2D1Geometry, worldTransform As IntPtr, flatteningTolerance As Single, <Out> ByRef relation As Integer) As Integer
        ' slot 9
        <PreserveSig> Function Simplify(option As Integer, worldTransform As IntPtr, flatteningTolerance As Single, sink As IntPtr) As Integer
        ' slot 10
        <PreserveSig> Function Tessellate(worldTransform As IntPtr, flatteningTolerance As Single, sink As IntPtr) As Integer
        ' slot 11
        <PreserveSig> Function CombineWithGeometry(geometry As ID2D1Geometry, combineMode As Integer, worldTransform As IntPtr, flatteningTolerance As Single, sink As IntPtr) As Integer
        ' slot 12
        <PreserveSig> Function Outline(worldTransform As IntPtr, flatteningTolerance As Single, sink As IntPtr) As Integer
        ' slot 13
        <PreserveSig> Function ComputeArea(worldTransform As IntPtr, flatteningTolerance As Single, <Out> ByRef area As Single) As Integer
        ' slot 14
        <PreserveSig> Function ComputeLength(worldTransform As IntPtr, flatteningTolerance As Single, <Out> ByRef length As Single) As Integer
        ' slot 15
        <PreserveSig> Function ComputePointAtLength(length As Single, worldTransform As IntPtr, flatteningTolerance As Single, <Out> ByRef point As D2D1_POINT_2F, <Out> ByRef unitTangentVector As D2D1_POINT_2F) As Integer
        ' slot 16
        <PreserveSig> Function Widen(strokeWidth As Single, strokeStyle As ID2D1StrokeStyle, worldTransform As IntPtr, flatteningTolerance As Single, sink As IntPtr) As Integer
    End Interface

    <ComImport>
    <Guid("2cd906a0-12e2-11dc-9fed-001143a055f9")>
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Friend Interface ID2D1RectangleGeometry : Inherits ID2D1Geometry
        ' slot 17
        <PreserveSig> Sub GetRect(<Out> ByRef rect As D2D1_RECT_F)
    End Interface

    <ComImport>
    <Guid("2cd9069b-12e2-11dc-9fed-001143a055f9")>
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Friend Interface ID2D1EllipseGeometry : Inherits ID2D1Geometry
        ' slot 17
        <PreserveSig> Sub GetEllipse(<Out> ByRef ellipse As D2D1_ELLIPSE)
    End Interface

    <ComImport>
    <Guid("2cd906a5-12e2-11dc-9fed-001143a055f9")>
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Friend Interface ID2D1PathGeometry : Inherits ID2D1Geometry
        ' slot 17
        <PreserveSig> Function Open(<Out> ByRef sink As ID2D1GeometrySink) As Integer
    End Interface

    <ComImport>
    <Guid("2cd9069e-12e2-11dc-9fed-001143a055f9")>
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Friend Interface ID2D1SimplifiedGeometrySink
        ' slot 3
        <PreserveSig> Sub SetFillMode(fillMode As D2D1_FILL_MODE)
        ' slot 4
        <PreserveSig> Sub SetSegmentFlags(vertexFlags As Integer)
        ' slot 5
        <PreserveSig> Sub BeginFigure(startPoint As D2D1_POINT_2F, figureBegin As D2D1_FIGURE_BEGIN)
        ' slot 6
        <PreserveSig> Sub AddLines(<[In]> points As D2D1_POINT_2F(), count As UInteger)
        ' slot 7
        <PreserveSig> Sub AddBeziers(<[In]> beziers As D2D1_BEZIER_SEGMENT(), count As UInteger)
        ' slot 8
        <PreserveSig> Sub EndFigure(figureEnd As D2D1_FIGURE_END)
        ' slot 9
        <PreserveSig> Function Close() As Integer
    End Interface

    <ComImport>
    <Guid("2cd9069f-12e2-11dc-9fed-001143a055f9")>
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Friend Interface ID2D1GeometrySink : Inherits ID2D1SimplifiedGeometrySink
        ' slot 10
        <PreserveSig> Sub AddLine(point As D2D1_POINT_2F)
        ' slot 11
        <PreserveSig> Sub AddBezier(ByRef bezier As D2D1_BEZIER_SEGMENT)
        ' slot 12
        <PreserveSig> Sub AddQuadraticBezier(ByRef bezier As D2D1_QUADRATIC_BEZIER_SEGMENT)
        ' slot 13
        <PreserveSig> Sub AddQuadraticBeziers(<[In]> beziers As D2D1_QUADRATIC_BEZIER_SEGMENT(), count As UInteger)
        ' slot 14
        <PreserveSig> Sub AddArc(ByRef arc As D2D1_ARC_SEGMENT)
    End Interface

    ' /********************************************************************************/
    '  the direct2d factory and the render target
    ' /********************************************************************************/

    <ComImport>
    <Guid("06152247-6f50-465a-9245-118bfd3b6007")>
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Friend Interface ID2D1Factory
        ' slot 3
        <PreserveSig> Function ReloadSystemMetrics() As Integer
        ' slot 4
        <PreserveSig> Sub GetDesktopDpi(<Out> ByRef dpiX As Single, <Out> ByRef dpiY As Single)
        ' slot 5
        <PreserveSig> Function CreateRectangleGeometry(ByRef rect As D2D1_RECT_F, <Out> ByRef geometry As ID2D1RectangleGeometry) As Integer
        ' slot 6
        <PreserveSig> Function CreateRoundedRectangleGeometry(ByRef rect As D2D1_ROUNDED_RECT, <Out> ByRef geometry As IntPtr) As Integer
        ' slot 7
        <PreserveSig> Function CreateEllipseGeometry(ByRef ellipse As D2D1_ELLIPSE, <Out> ByRef geometry As ID2D1EllipseGeometry) As Integer
        ' slot 8
        <PreserveSig> Function CreateGeometryGroup(fillMode As Integer, geometries As IntPtr, count As UInteger, <Out> ByRef group As IntPtr) As Integer
        ' slot 9
        <PreserveSig> Function CreateTransformedGeometry(source As ID2D1Geometry, ByRef transform As D2D1_MATRIX_3X2_F, <Out> ByRef geometry As IntPtr) As Integer
        ' slot 10
        <PreserveSig> Function CreatePathGeometry(<Out> ByRef geometry As ID2D1PathGeometry) As Integer
        ' slot 11
        <PreserveSig> Function CreateStrokeStyle(ByRef properties As D2D1_STROKE_STYLE_PROPERTIES, <[In]> dashes As Single(), dashCount As UInteger, <Out> ByRef style As ID2D1StrokeStyle) As Integer
        ' slot 12
        <PreserveSig> Function CreateDrawingStateBlock(description As IntPtr, textRenderingParams As IntPtr, <Out> ByRef block As IntPtr) As Integer
        ' slot 13
        <PreserveSig> Function CreateWicBitmapRenderTarget(wicBitmap As IntPtr, ByRef props As D2D1_RENDER_TARGET_PROPERTIES, <Out> ByRef target As IntPtr) As Integer
        ' slot 14
        <PreserveSig> Function CreateHwndRenderTarget(ByRef props As D2D1_RENDER_TARGET_PROPERTIES, hwndProps As IntPtr, <Out> ByRef target As IntPtr) As Integer
        ' slot 15
        <PreserveSig> Function CreateDxgiSurfaceRenderTarget(surface As IntPtr, ByRef props As D2D1_RENDER_TARGET_PROPERTIES, <Out> ByRef target As ID2D1RenderTarget) As Integer
    End Interface

    <ComImport>
    <Guid("2cd90694-12e2-11dc-9fed-001143a055f9")>
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Friend Interface ID2D1RenderTarget : Inherits ID2D1Resource

        ' slot 4
        <PreserveSig> Function CreateBitmap(size As D2D1_SIZE_U, srcData As IntPtr, pitch As UInteger, ByRef props As D2D1_BITMAP_PROPERTIES, <Out> ByRef bitmap As ID2D1Bitmap) As Integer
        ' slot 5
        <PreserveSig> Function CreateBitmapFromWicBitmap(wicSource As IntPtr, ByRef props As D2D1_BITMAP_PROPERTIES, <Out> ByRef bitmap As ID2D1Bitmap) As Integer
        ' slot 6
        <PreserveSig> Function CreateSharedBitmap(ByRef riid As Guid, data As IntPtr, ByRef props As D2D1_BITMAP_PROPERTIES, <Out> ByRef bitmap As ID2D1Bitmap) As Integer
        ' slot 7
        <PreserveSig> Function CreateBitmapBrush(bitmap As ID2D1Bitmap, brushProps As IntPtr, props As IntPtr, <Out> ByRef brush As ID2D1Brush) As Integer
        ' slot 8
        <PreserveSig> Function CreateSolidColorBrush(ByRef color As D2D1_COLOR_F, props As IntPtr, <Out> ByRef brush As ID2D1Brush) As Integer
        ' slot 9
        <PreserveSig> Function CreateGradientStopCollection(<[In]> stops As D2D1_GRADIENT_STOP(), stopCount As UInteger, gamma As Integer, extendMode As Integer, <Out> ByRef collection As ID2D1GradientStopCollection) As Integer
        ' slot 10
        <PreserveSig> Function CreateLinearGradientBrush(ByRef props As D2D1_LINEAR_GRADIENT_BRUSH_PROPERTIES, brushProps As IntPtr, collection As ID2D1GradientStopCollection, <Out> ByRef brush As ID2D1Brush) As Integer
        ' slot 11
        <PreserveSig> Function CreateRadialGradientBrush(props As IntPtr, brushProps As IntPtr, collection As ID2D1GradientStopCollection, <Out> ByRef brush As ID2D1Brush) As Integer
        ' slot 12
        <PreserveSig> Function CreateCompatibleRenderTarget(size As IntPtr, pixelSize As IntPtr, format As IntPtr, options As Integer, <Out> ByRef target As IntPtr) As Integer
        ' slot 13
        <PreserveSig> Function CreateLayer(size As IntPtr, <Out> ByRef layer As IntPtr) As Integer
        ' slot 14
        <PreserveSig> Function CreateMesh(<Out> ByRef mesh As IntPtr) As Integer

        ' slot 15
        <PreserveSig> Sub DrawLine(p0 As D2D1_POINT_2F, p1 As D2D1_POINT_2F, brush As ID2D1Brush, strokeWidth As Single, strokeStyle As ID2D1StrokeStyle)
        ' slot 16
        <PreserveSig> Sub DrawRectangle(ByRef rect As D2D1_RECT_F, brush As ID2D1Brush, strokeWidth As Single, strokeStyle As ID2D1StrokeStyle)
        ' slot 17
        <PreserveSig> Sub FillRectangle(ByRef rect As D2D1_RECT_F, brush As ID2D1Brush)
        ' slot 18
        <PreserveSig> Sub DrawRoundedRectangle(ByRef rect As D2D1_ROUNDED_RECT, brush As ID2D1Brush, strokeWidth As Single, strokeStyle As ID2D1StrokeStyle)
        ' slot 19
        <PreserveSig> Sub FillRoundedRectangle(ByRef rect As D2D1_ROUNDED_RECT, brush As ID2D1Brush)
        ' slot 20
        <PreserveSig> Sub DrawEllipse(ByRef ellipse As D2D1_ELLIPSE, brush As ID2D1Brush, strokeWidth As Single, strokeStyle As ID2D1StrokeStyle)
        ' slot 21
        <PreserveSig> Sub FillEllipse(ByRef ellipse As D2D1_ELLIPSE, brush As ID2D1Brush)
        ' slot 22
        <PreserveSig> Sub DrawGeometry(geometry As ID2D1Geometry, brush As ID2D1Brush, strokeWidth As Single, strokeStyle As ID2D1StrokeStyle)
        ' slot 23
        <PreserveSig> Sub FillGeometry(geometry As ID2D1Geometry, brush As ID2D1Brush, opacityBrush As ID2D1Brush)
        ' slot 24
        <PreserveSig> Sub FillMesh(mesh As IntPtr, brush As ID2D1Brush)
        ' slot 25
        <PreserveSig> Sub FillOpacityMask(mask As ID2D1Bitmap, brush As ID2D1Brush, content As Integer, dstRect As IntPtr, srcRect As IntPtr)
        ' slot 26
        <PreserveSig> Sub DrawBitmap(bitmap As ID2D1Bitmap, ByRef destRect As D2D1_RECT_F, opacity As Single, interpolationMode As Integer, srcRect As IntPtr)
        ' slot 27
        <PreserveSig> Sub DrawText(<MarshalAs(UnmanagedType.LPWStr)> text As String, length As UInteger, textFormat As IDWriteTextFormat, ByRef layoutRect As D2D1_RECT_F, brush As ID2D1Brush, options As Integer, measuringMode As Integer)
        ' slot 28
        <PreserveSig> Sub DrawTextLayout(origin As D2D1_POINT_2F, layout As IDWriteTextLayout, brush As ID2D1Brush, options As Integer)
        ' slot 29
        <PreserveSig> Sub DrawGlyphRun(origin As D2D1_POINT_2F, glyphRun As IntPtr, brush As ID2D1Brush, measuringMode As Integer)

        ' slot 30
        <PreserveSig> Sub SetTransform(ByRef transform As D2D1_MATRIX_3X2_F)
        ' slot 31
        <PreserveSig> Sub GetTransform(<Out> ByRef transform As D2D1_MATRIX_3X2_F)
        ' slot 32
        <PreserveSig> Sub PushLayer(layerParams As IntPtr, layer As IntPtr)
        ' slot 33
        <PreserveSig> Sub PopLayer()
        ' slot 34
        <PreserveSig> Function Flush(tag1 As IntPtr, tag2 As IntPtr) As Integer
        ' slot 35
        <PreserveSig> Sub SaveDrawingState(block As IntPtr)
        ' slot 36
        <PreserveSig> Sub RestoreDrawingState(block As IntPtr)
        ' slot 37
        <PreserveSig> Sub PushAxisAlignedClip(ByRef clipRect As D2D1_RECT_F, antialiasMode As Integer)
        ' slot 38
        <PreserveSig> Sub PopAxisAlignedClip()
        ' slot 39
        <PreserveSig> Sub Clear(ByRef clearColor As D2D1_COLOR_F)
        ' slot 40
        <PreserveSig> Sub BeginDraw()
        ' slot 41
        <PreserveSig> Function EndDraw(tag1 As IntPtr, tag2 As IntPtr) As Integer
        ' slot 42
        <PreserveSig> Function GetPixelFormat() As D2D1_PIXEL_FORMAT
        ' slot 43
        <PreserveSig> Sub SetDpi(dpiX As Single, dpiY As Single)
        ' slot 44
        <PreserveSig> Sub GetDpi(<Out> ByRef dpiX As Single, <Out> ByRef dpiY As Single)
        ' slot 45
        <PreserveSig> Function GetSize() As D2D1_SIZE_F
        ' slot 46
        <PreserveSig> Function GetPixelSize() As D2D1_SIZE_U
        ' slot 47
        <PreserveSig> Function GetMaximumBitmapSize() As UInteger
        ' slot 48
        <PreserveSig> Function IsSupported(ByRef props As D2D1_RENDER_TARGET_PROPERTIES) As Integer
    End Interface
End Namespace
