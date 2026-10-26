Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Imaging
Imports SkiaSharp

Module Effects

    <Extension>
    Public Function LineDashStyle(stroke As Pen) As SKPathEffect
        Dim strokeWidth As Single = stroke.Width

        ' 虚线间隔严格对齐 GDI+ 的 DashStyle 定义（Dash = 3w/w、Dot = w/w、
        ' DashDot = 3w/w/w/w），否则同一个 CSS 描边在两个后端的疏密程度会明显不同
        Select Case stroke.DashStyle
            Case DashStyle.Dash
                Return SKPathEffect.CreateDash(New Single() {3 * strokeWidth, strokeWidth}, 0)
            Case DashStyle.Dot
                Return SKPathEffect.CreateDash(New Single() {strokeWidth, strokeWidth}, 0)
            Case DashStyle.DashDot
                Return SKPathEffect.CreateDash(New Single() {
                    3 * strokeWidth, ' 实线
                    strokeWidth,     ' 间隙
                    strokeWidth,     ' 点（实线）
                    strokeWidth      ' 间隙
                }, 0)
            Case DashStyle.DashDotDot
                Return SKPathEffect.CreateDash(New Single() {
                    3 * strokeWidth, ' 实线
                    strokeWidth,     ' 间隙
                    strokeWidth,     ' 点1
                    strokeWidth,     ' 间隙
                    strokeWidth,     ' 点2
                    strokeWidth      ' 间隙
                }, 0)
            Case Else
                If stroke.DashStyle = DashStyle.Custom Then
                    Call "custom style for line dash style is not working at here.".Warning
                End If

                Return Nothing
        End Select
    End Function
End Module
