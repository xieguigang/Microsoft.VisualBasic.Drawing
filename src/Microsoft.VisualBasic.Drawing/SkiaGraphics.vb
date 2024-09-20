Imports System.Runtime.CompilerServices

Public MustInherit Class SkiaGraphics

    Protected ReadOnly w As Integer
    Protected ReadOnly h As Integer

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Sub New(width As Integer, height As Integer)
        w = width
        h = height
    End Sub

    Public MustOverride Sub Clear(color As String)

End Class
