Imports System.Drawing
Imports System.Runtime.InteropServices

''' <summary>
''' DEBUG ONLY helper: probe a single direct2d vtable slot in an isolated
''' process so that a crash does not hide the result of the other slots.
''' </summary>
Public Module DxDebugProbe

    ''' <summary>
    ''' probe one vtable slot of the ID2D1RenderTarget object
    ''' </summary>
    ''' <param name="slot">the vtable slot index</param>
    ''' <param name="mode">
    ''' "clear" for the Clear(const D2D1_COLOR_F*) signature, "fill" for the
    ''' FillRectangle(const D2D1_RECT_F*, ID2D1Brush*) signature
    ''' </param>
    Public Function Probe(slot As Integer, mode As String) As String
        Dim rt As New DxRenderTarget(DxDevice.Default, 64, 64)
        Dim raw As IntPtr = Marshal.GetIUnknownForObject(rt.Target)
        Dim vt As IntPtr = Marshal.ReadIntPtr(raw)
        Dim fn As IntPtr = Marshal.ReadIntPtr(vt, slot * IntPtr.Size)

        Console.WriteLine($"probe slot {slot} mode={mode} fn=0x{fn.ToInt64():X}")

        If mode = "fill" Then
            Dim brushPtr As IntPtr = IntPtr.Zero
            Dim mk = Marshal.GetDelegateForFunctionPointer(Of DxCreateSolidBrush)(Marshal.ReadIntPtr(vt, 8 * IntPtr.Size))
            Dim red As D2D1_COLOR_F = ToColorF(Color.Red)

            mk(raw, red, IntPtr.Zero, brushPtr)
            Console.WriteLine($"  brush = 0x{brushPtr.ToInt64():X}")

            Dim fill = Marshal.GetDelegateForFunctionPointer(Of DxFillRectangle)(fn)
            Dim rect As D2D1_RECT_F = ToRectF(New Rectangle(0, 0, 64, 64))

            fill(raw, rect, brushPtr)
        Else
            Dim clear = Marshal.GetDelegateForFunctionPointer(Of DxVoidRefColor)(fn)
            Dim white As D2D1_COLOR_F = ToColorF(Color.White)

            clear(raw, white)
        End If

        Console.WriteLine("  call returned")

        Dim px As Byte() = rt.ReadPixels()

        Marshal.Release(raw)
        rt.Dispose()

        Return $"slot {slot} mode={mode} pixel = {px(0)},{px(1)},{px(2)},{px(3)}"
    End Function
End Module
