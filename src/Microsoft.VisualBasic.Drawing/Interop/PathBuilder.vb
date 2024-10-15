Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Linq
Imports SkiaSharp
Imports Microsoft.VisualBasic.Math
Imports Microsoft.VisualBasic.Math.SIMD.SIMDIntrinsics

Module PathBuilder

    <Extension>
    Public Function CreatePath(path As GraphicsPath) As SKPath
        Dim skia As New SKPath

        For Each op As GraphicsPath.op In path.AsEnumerable
            Select Case op.GetType
                Case GetType(GraphicsPath.op_AddArc) : Call skia.AddArc(op)
                Case Else
                    Throw New NotImplementedException(op.GetType.FullName)
            End Select
        Next

        Return skia
    End Function

    <Extension>
    Private Sub AddArc(skia As SKPath, arc As GraphicsPath.op_AddArc)
        Dim startAngleRad = Trigonometric.ToRadians(arc.startAngle)
        Dim sweepAngleRad = Trigonometric.ToRadians(arc.sweepAngle)
        Dim rx = arc.rect.Width / 2.0F
        Dim ry = arc.rect.Height / 2.0F
        Dim cx = arc.rect.Left + rx
        Dim cy = arc.rect.Top + ry

        Call skia.ArcTo(
            New SKRect(arc.rect.Left, arc.rect.Top, arc.rect.Right, arc.rect.Bottom),
            startAngleRad,
            sweepAngleRad,
            False
        )
    End Sub
End Module
