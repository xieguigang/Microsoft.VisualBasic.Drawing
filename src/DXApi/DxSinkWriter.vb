Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

''' <summary>
''' A allocation free writer of the ID2D1GeometrySink object.
''' </summary>
''' <remarks>
''' Appending a figure into a path geometry is done once per polygon
''' primitive, and such a hot loop should not pay for the runtime callable
''' wrapper dispatch of the clr for every single call.
'''
''' This writer resolves the vtable entries of the sink object once and
''' invokes them through a function pointer, which removes the interface
''' pointer resolution of the clr from the per polygon code path.
''' </remarks>
Friend NotInheritable Class DxSinkWriter

    <UnmanagedFunctionPointer(CallingConvention.StdCall)>
    Friend Delegate Sub SetFillModeFn(this As IntPtr, fillMode As Integer)

    <UnmanagedFunctionPointer(CallingConvention.StdCall)>
    Friend Delegate Sub BeginFigureFn(this As IntPtr, startPoint As D2D1_POINT_2F, figureBegin As Integer)

    <UnmanagedFunctionPointer(CallingConvention.StdCall)>
    Friend Delegate Sub AddLinesFn(this As IntPtr, points As IntPtr, count As UInteger)

    <UnmanagedFunctionPointer(CallingConvention.StdCall)>
    Friend Delegate Sub EndFigureFn(this As IntPtr, figureEnd As Integer)

    <UnmanagedFunctionPointer(CallingConvention.StdCall)>
    Friend Delegate Function CloseFn(this As IntPtr) As Integer

    Private ReadOnly handle As IntPtr

    Private ReadOnly setFillModeApi As SetFillModeFn
    Private ReadOnly beginFigureApi As BeginFigureFn
    Private ReadOnly addLinesApi As AddLinesFn
    Private ReadOnly endFigureApi As EndFigureFn
    Private ReadOnly closeApi As CloseFn

    Friend Sub New(sink As ID2D1GeometrySink)
        handle = ComQuery(sink, GetType(ID2D1GeometrySink).GUID, "ID2D1GeometrySink")

        Dim vt As IntPtr = Marshal.ReadIntPtr(handle)

        setFillModeApi = Marshal.GetDelegateForFunctionPointer(Of SetFillModeFn)(Marshal.ReadIntPtr(vt, 3 * IntPtr.Size))
        beginFigureApi = Marshal.GetDelegateForFunctionPointer(Of BeginFigureFn)(Marshal.ReadIntPtr(vt, 5 * IntPtr.Size))
        addLinesApi = Marshal.GetDelegateForFunctionPointer(Of AddLinesFn)(Marshal.ReadIntPtr(vt, 6 * IntPtr.Size))
        endFigureApi = Marshal.GetDelegateForFunctionPointer(Of EndFigureFn)(Marshal.ReadIntPtr(vt, 8 * IntPtr.Size))
        closeApi = Marshal.GetDelegateForFunctionPointer(Of CloseFn)(Marshal.ReadIntPtr(vt, 9 * IntPtr.Size))
    End Sub

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Friend Sub SetFillMode(fillMode As Integer)
        Call setFillModeApi(handle, fillMode)
    End Sub

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Friend Sub BeginFigure(startPoint As D2D1_POINT_2F)
        Call beginFigureApi(handle, startPoint, D2D1_FIGURE_BEGIN.FILLED)
    End Sub

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Friend Sub AddLines(points As IntPtr, count As UInteger)
        Call addLinesApi(handle, points, count)
    End Sub

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Friend Sub EndFigureClosed()
        Call endFigureApi(handle, D2D1_FIGURE_END.CLOSED)
    End Sub

    Friend Sub Close()
        Call ThrowIfFailed(closeApi(handle), "ID2D1GeometrySink::Close")
    End Sub

    Friend Sub Release()
        If handle <> IntPtr.Zero Then
            Call Marshal.Release(handle)
        End If
    End Sub
End Class
