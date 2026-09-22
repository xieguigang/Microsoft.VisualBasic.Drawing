Imports System.Drawing
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports std = System.Math

''' <summary>
''' The common helper api for the directx com interop layer
''' </summary>
    ''' <remarks>
    ''' All of the native resource wrapper in this project is pure managed code:
    ''' the directx api is imported via <see cref="DllImportAttribute"/> and the
    ''' com interface is declared via <see cref="ComImportAttribute"/>, there is
    ''' no any unmanaged code or c++/cli bridge required.
    ''' </remarks>
    Friend Module ComBase

        Friend Const S_OK As Integer = 0
        Friend Const D2DERR_RECREATE_TARGET As Integer = &H88990012
        Friend Const D2DERR_UNSUPPORTED_PIXEL_FORMAT As Integer = &H8899000B
        ''' <summary>the gpu device has been removed, the whole device has to be recreated</summary>
        Friend Const DXGI_ERROR_DEVICE_REMOVED As Integer = &H887A0005
        ''' <summary>the gpu device has been reset</summary>
        Friend Const DXGI_ERROR_DEVICE_RESET As Integer = &H887A000C
        ''' <summary>the gpu is still busy with the previous frame</summary>
        Friend Const DXGI_ERROR_WAS_STILL_DRAWING As Integer = &H887A000B

        ''' <summary>
        ''' does the hresult mean that the rendering device or the render target
        ''' is not usable any more?
        ''' </summary>
        ''' <remarks>
        ''' when this is the case, the swap chain and the direct2d render target
        ''' must be recreated before the next drawing command is submitted.
        ''' </remarks>
        Friend Function IsDeviceLost(hr As Integer) As Boolean
            Return hr = DXGI_ERROR_DEVICE_REMOVED OrElse
                hr = DXGI_ERROR_DEVICE_RESET OrElse
                hr = D2DERR_RECREATE_TARGET
        End Function

        ''' <summary>
        ''' check the HRESULT value of a directx api call
        ''' </summary>
        Friend Sub ThrowIfFailed(hr As Integer, Optional api As String = "DirectX")
            If hr < 0 Then
                Throw New ExternalException($"{api} call failure, HRESULT: 0x{hr:X8}", hr)
            End If
        End Sub

        ''' <summary>
        ''' release a com runtime callable wrapper object in a safe manner
        ''' </summary>
        Friend Sub SafeRelease(ByRef com As Object)
            If com Is Nothing Then
                Return
            End If

            Try
                If Marshal.IsComObject(com) Then
                    Call Marshal.FinalReleaseComObject(com)
                End If
            Catch
                ' the com object may already been released by the finalizer thread
            End Try

            com = Nothing
        End Sub

        ''' <summary>
        ''' QueryInterface on a com object, the returned raw interface pointer
        ''' holds one reference which is owned by the caller.
        ''' </summary>
        Friend Function ComQuery(source As Object, iid As Guid, Optional name As String = "IUnknown") As IntPtr
            Dim unk As IntPtr = Marshal.GetIUnknownForObject(source)
            Dim p As IntPtr = IntPtr.Zero

            Try
                ThrowIfFailed(Marshal.QueryInterface(unk, iid, p), $"QueryInterface({name})")
            Finally
                Call Marshal.Release(unk)
            End Try

            Return p
        End Function

        ''' <summary>
        ''' Wrap a raw com interface pointer as a strongly typed runtime callable
        ''' wrapper object.
        ''' </summary>
        ''' <remarks>
        ''' A com object that is created through the out parameter of another com
        ''' method is wrapped by the clr as an untyped System.__ComObject, and
        ''' such an object can not be dispatched through the interface method
        ''' stub (QueryInterface on an untyped rcw always fails). creating the
        ''' runtime callable wrapper explicitly with the target interface type
        ''' here makes the interface pointer cacheable so that the interface
        ''' method can be dispatched correctly.
        ''' </remarks>
        Friend Function ComObject(Of T As Class)(raw As IntPtr) As T
            If raw = IntPtr.Zero Then
                Return Nothing
            End If

            Return DirectCast(Marshal.GetTypedObjectForIUnknown(raw, GetType(T)), T)
        End Function

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Friend Function ToColorF(color As Color) As D2D1_COLOR_F
            Return New D2D1_COLOR_F With {
                .r = color.R / 255.0F,
                .g = color.G / 255.0F,
                .b = color.B / 255.0F,
                .a = color.A / 255.0F
            }
        End Function

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Friend Function ToRectF(rect As RectangleF) As D2D1_RECT_F
            Return New D2D1_RECT_F With {
                .Left = rect.Left,
                .Top = rect.Top,
                .Right = rect.Right,
                .Bottom = rect.Bottom
            }
        End Function

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Friend Function ToRectF(rect As Rectangle) As D2D1_RECT_F
            Return New D2D1_RECT_F With {
                .Left = rect.Left,
                .Top = rect.Top,
                .Right = rect.Right,
                .Bottom = rect.Bottom
            }
        End Function

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Friend Function ToPoint2F(pt As PointF) As D2D1_POINT_2F
            Return New D2D1_POINT_2F With {.x = pt.X, .y = pt.Y}
        End Function

        ''' <summary>
        ''' pack two 32 bit values into one 64 bit argument slot
        ''' </summary>
        ''' <remarks>
        ''' A com interface method that takes a small struct (8 bytes or less)
        ''' by value can not be declared correctly in the vb.net interop layer,
        ''' such a parameter is declared as a 64 bit integer here and the two
        ''' structure members are packed into it.
        ''' </remarks>
        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Friend Function Pack64(low As Integer, high As Integer) As Long
            Return (CLng(high) << 32) Or (CLng(low) And &HFFFFFFFFL)
        End Function

        ''' <summary>
        ''' pack a D2D1_POINT_2F structure into one 64 bit argument slot
        ''' </summary>
        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Friend Function Pack64(pt As D2D1_POINT_2F) As Long
            Return Pack64(BitConverter.SingleToInt32Bits(pt.x), BitConverter.SingleToInt32Bits(pt.y))
        End Function

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Friend Function IdentityMatrix() As D2D1_MATRIX_3X2_F
            Return New D2D1_MATRIX_3X2_F With {
                .m11 = 1.0F, .m12 = 0.0F,
                .m21 = 0.0F, .m22 = 1.0F,
                .dx = 0.0F, .dy = 0.0F
            }
        End Function

        ''' <summary>
        ''' a * b, the result is applied on the render target after the
        ''' <paramref name="a"/> matrix
        ''' </summary>
        Friend Function Multiply(a As D2D1_MATRIX_3X2_F, b As D2D1_MATRIX_3X2_F) As D2D1_MATRIX_3X2_F
            Return New D2D1_MATRIX_3X2_F With {
                .m11 = a.m11 * b.m11 + a.m12 * b.m21,
                .m12 = a.m11 * b.m12 + a.m12 * b.m22,
                .m21 = a.m21 * b.m11 + a.m22 * b.m21,
                .m22 = a.m21 * b.m12 + a.m22 * b.m22,
                .dx = a.dx * b.m11 + a.dy * b.m21 + b.dx,
                .dy = a.dx * b.m12 + a.dy * b.m22 + b.dy
            }
        End Function

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Friend Function Translation(dx As Single, dy As Single) As D2D1_MATRIX_3X2_F
            Return New D2D1_MATRIX_3X2_F With {
                .m11 = 1.0F, .m12 = 0.0F,
                .m21 = 0.0F, .m22 = 1.0F,
                .dx = dx, .dy = dy
            }
        End Function

        <MethodImpl(MethodImplOptions.AggressiveInlining)>
        Friend Function Scaling(sx As Single, sy As Single) As D2D1_MATRIX_3X2_F
            Return New D2D1_MATRIX_3X2_F With {
                .m11 = sx, .m12 = 0.0F,
                .m21 = 0.0F, .m22 = sy,
                .dx = 0.0F, .dy = 0.0F
            }
        End Function

        ''' <summary>
        ''' make a rotation transform matrix around the given center point
        ''' </summary>
        Friend Function RotationMatrix(angleDegrees As Single, centerX As Single, centerY As Single) As D2D1_MATRIX_3X2_F
            Dim radians As Double = angleDegrees * std.PI / 180.0
            Dim cos As Single = CSng(std.Cos(radians))
            Dim sin As Single = CSng(std.Sin(radians))

            Return New D2D1_MATRIX_3X2_F With {
                .m11 = cos,
                .m12 = sin,
                .m21 = -sin,
                .m22 = cos,
                .dx = centerX - cos * centerX + sin * centerY,
                .dy = centerY - sin * centerX - cos * centerY
            }
        End Function
    End Module

    Friend Module DxConstants

        Friend ReadOnly IID_IDXGISurface As New Guid("cafcb56c-6ac3-4889-bf47-9e23bbd260ec")
        Friend ReadOnly IID_IDXGIFactory2 As New Guid("50c83a1c-e072-4c48-87b0-3630fa36a6d0")
    End Module

    ' /********************************************************************************/
    '  D2D1 basic geometry value types
    ' /********************************************************************************/

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure D2D1_COLOR_F
        Public r As Single
        Public g As Single
        Public b As Single
        Public a As Single
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure D2D1_POINT_2F
        Public x As Single
        Public y As Single
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure D2D1_POINT_2U
        Public x As UInteger
        Public y As UInteger
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure D2D1_RECT_F
        Public Left As Single
        Public Top As Single
        Public Right As Single
        Public Bottom As Single
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure D2D1_RECT_U
        Public Left As UInteger
        Public Top As UInteger
        Public Right As UInteger
        Public Bottom As UInteger
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure D2D1_SIZE_F
        Public width As Single
        Public height As Single
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure D2D1_SIZE_U
        Public width As UInteger
        Public height As UInteger
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure D2D1_ELLIPSE
        Public point As D2D1_POINT_2F
        Public radiusX As Single
        Public radiusY As Single
    End Structure

    ''' <summary>
    ''' D2D1_MATRIX_3X2_F, the field order is _11, _12, _21, _22, _31, _32
    ''' </summary>
    <StructLayout(LayoutKind.Sequential)>
    Friend Structure D2D1_MATRIX_3X2_F
        Public m11 As Single
        Public m12 As Single
        Public m21 As Single
        Public m22 As Single
        Public dx As Single
        Public dy As Single
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure D2D1_PIXEL_FORMAT
        Public format As Integer
        Public alphaMode As Integer
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure D2D1_RENDER_TARGET_PROPERTIES
        Public type As Integer
        Public pixelFormat As D2D1_PIXEL_FORMAT
        Public dpiX As Single
        Public dpiY As Single
        Public usage As Integer
        Public minLevel As Integer
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure D2D1_BITMAP_PROPERTIES
        Public pixelFormat As D2D1_PIXEL_FORMAT
        Public dpiX As Single
        Public dpiY As Single
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure D2D1_BEZIER_SEGMENT
        Public point1 As D2D1_POINT_2F
        Public point2 As D2D1_POINT_2F
        Public point3 As D2D1_POINT_2F
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure D2D1_QUADRATIC_BEZIER_SEGMENT
        Public point1 As D2D1_POINT_2F
        Public point2 As D2D1_POINT_2F
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure D2D1_ARC_SEGMENT
        Public point As D2D1_POINT_2F
        Public size As D2D1_SIZE_F
        Public rotationAngle As Single
        Public sweepDirection As Integer
        Public arcSize As Integer
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure D2D1_ROUNDED_RECT
        Public rect As D2D1_RECT_F
        Public radiusX As Single
        Public radiusY As Single
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure D2D1_STROKE_STYLE_PROPERTIES
        Public startCap As Integer
        Public endCap As Integer
        Public dashCap As Integer
        Public lineJoin As Integer
        Public miterLimit As Single
        Public dashStyle As Integer
        Public dashOffset As Single
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure D2D1_BRUSH_PROPERTIES
        Public opacity As Single
        Public transform As D2D1_MATRIX_3X2_F
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure D2D1_GRADIENT_STOP
        Public position As Single
        Public color As D2D1_COLOR_F
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure D2D1_LINEAR_GRADIENT_BRUSH_PROPERTIES
        Public startPoint As D2D1_POINT_2F
        Public endPoint As D2D1_POINT_2F
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure D2D1_FACTORY_OPTIONS
        Public debugLevel As Integer
    End Structure

    ' /********************************************************************************/
    '  D2D1 enums
    ' /********************************************************************************/

    Friend Enum D2D1_FILL_MODE As Integer
        ALTERNATE = 0
        WINDING = 1
    End Enum

    Friend Enum D2D1_FIGURE_BEGIN As Integer
        FILLED = 0
        HOLLOW = 1
    End Enum

    Friend Enum D2D1_FIGURE_END As Integer
        [OPEN] = 0
        CLOSED = 1
    End Enum

    Friend Enum D2D1_ALPHA_MODE As Integer
        UNKNOWN = 0
        PREMULTIPLIED = 1
        STRAIGHT = 2
        IGNORE = 3
    End Enum

    Friend Enum D2D1_RENDER_TARGET_TYPE As Integer
        [DEFAULT] = 0
        SOFTWARE = 1
        HARDWARE = 2
    End Enum

    Friend Enum D2D1_RENDER_TARGET_USAGE As Integer
        NONE = 0
        FORCE_BITMAP_REMOTING = 1
        GDI_COMPATIBLE = 2
    End Enum

    Friend Enum D2D1_FEATURE_LEVEL As Integer
        [DEFAULT] = 0
        LEVEL_9 = &H9100
        LEVEL_10 = &HA000
    End Enum

    Friend Enum D2D1_ANTIALIAS_MODE As Integer
        PER_PRIMITIVE = 0
        ALIASED = 1
    End Enum

    Friend Enum D2D1_DRAW_TEXT_OPTIONS As Integer
        NONE = 0
        NO_SNAP = 1
        CLIP = 2
    End Enum

    Friend Enum D2D1_DASH_STYLE As Integer
        SOLID = 0
        DASH = 1
        DOT = 2
        DASH_DOT = 3
        DASH_DOT_DOT = 4
        CUSTOM = 5
    End Enum

    Friend Enum D2D1_CAP_STYLE As Integer
        FLAT = 0
        SQUARE = 1
        ROUND = 2
        TRIANGLE = 3
    End Enum

    Friend Enum D2D1_LINE_JOIN As Integer
        MITER = 0
        BEVEL = 1
        ROUND = 2
        MITER_OR_BEVEL = 3
    End Enum

    Friend Enum D2D1_SWEEP_DIRECTION As Integer
        COUNTER_CLOCKWISE = 0
        CLOCKWISE = 1
    End Enum

    Friend Enum D2D1_ARC_SIZE As Integer
        SMALL = 0
        LARGE = 1
    End Enum

    Friend Enum D2D1_PATH_SEGMENT As Integer
        NONE = 0
        FORCE_UNSTROKED = 1
        FORCE_ROUND_LINE_JOIN = 2
    End Enum

    Friend Enum D2D1_BITMAP_INTERPOLATION_MODE As Integer
        NEAREST_NEIGHBOR = 0
        LINEAR = 1
    End Enum

    Friend Enum D2D1_EXTEND_MODE As Integer
        CLAMP = 0
        WRAP = 1
        MIRROR = 2
    End Enum

    Friend Enum D2D1_GAMMA As Integer
        G22 = 0
        G10 = 1
    End Enum

    Friend Enum D2D1_DEBUG_LEVEL As Integer
        NONE = 0
        ERROR_LEVEL = 1
        WARNING = 2
        INFORMATION = 3
    End Enum

    Friend Enum D2D1_FACTORY_TYPE As Integer
        SINGLE_THREADED = 0
        MULTI_THREADED = 1
    End Enum
