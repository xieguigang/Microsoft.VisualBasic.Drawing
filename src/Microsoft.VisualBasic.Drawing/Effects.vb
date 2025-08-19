Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Imaging
Imports SkiaSharp

Module Effects

    <Extension>
    Public Function LineDashStyle(stroke As Pen) As SKPathEffect
        Dim strokeWidth As Single = stroke.Width

        Select Case stroke.DashStyle
            Case DashStyle.Dash
                ' 虚线：长实线段 +中等间隙
                Return SKPathEffect.CreateDash(New Single() {4 * strokeWidth, 2 * strokeWidth}, 0)
            Case DashStyle.Dot
                ' 点线：圆点（需配合 StrokeCap.Round）
                Return SKPathEffect.CreateDash(New Single() {0, 2 * strokeWidth}, 0)
            Case DashStyle.DashDot
                ' 点划线：长实线 +短间隙 + 点 + 短间隙
                Return SKPathEffect.CreateDash(New Single() {
                    4 * strokeWidth, ' 实线
                    2 * strokeWidth, ' 间隙
                    strokeWidth,     ' 点（实线）
                    2 * strokeWidth  ' 间隙
                }, 0)
            Case DashStyle.DashDotDot
                ' 双点划线：长实线 +短间隙 + 点 + 短间隙 + 点 + 短间隙
                Return SKPathEffect.CreateDash(New Single() {
                    4 * strokeWidth, ' 实线
                    2 * strokeWidth, ' 间隙
                    strokeWidth,     ' 点1
                    2 * strokeWidth, ' 间隙
                    strokeWidth,     ' 点2
                    2 * strokeWidth  ' 间隙
                }, 0)
            Case Else
                If stroke.DashStyle = DashStyle.Custom Then
                    Call "custom style for line dash style is not working at here.".Warning
                End If

                Return Nothing
        End Select
    End Function
End Module
