#Region "Microsoft.VisualBasic::4f1fc988606e86f9d6c77cc65996528b, gr\physics\test\SDRender.vb"

' Author:
' 
'       asuka (amethyst.asuka@gcmodeller.org)
'       xie (genetics@smrucc.org)
'       xieguigang (xie.guigang@live.com)
' 
' Copyright (c) 2018 GPL3 Licensed
' 
' 
' GNU GENERAL PUBLIC LICENSE (GPL3)
' 
' 
' This program is free software: you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation, either version 3 of the License, or
' (at your option) any later version.
' 
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY; without even the implied warranty of
' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
' GNU General PUBLIC License for more details.
' 
' You should have received a copy of the GNU General Public License
' along with this program. If not, see <http://www.gnu.org/licenses/>.


' /********************************************************************************/

' Summaries:

' Direct pixel renderer for the flocking demo. The legacy implementation issued
' one GDI+ <c>FillClosedCurve</c>/<c>DrawLine</c> call per boid (80k vector
' calls per frame at the demo scale, ~100ms+), and allocated a brand new bitmap
' on every tick. This renderer keeps one persistent 32bpp bitmap, clears it with
' a single <c>Unsafe.InitBlock</c> and draws every boid directly into the locked
' pixel buffer, reproducing the legacy arrow geometry (3px shaft + 10x7 triangle
' head, viridis speed LUT, white predators) at roughly two orders of magnitude
' lower cost.

'     Module SDRender
' 
'         Constructor: (+1 Overloads) Sub New
'         Function: RenderField
'         Sub: DrawArrow, DrawBoids, FillTriangle, PutPixel, RenderTo
' 
' 
' /********************************************************************************/

#End Region

Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Runtime.CompilerServices
Imports System.Windows.Forms
Imports Microsoft.VisualBasic.Drawing
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Drawing2D
Imports Microsoft.VisualBasic.Imaging.Drawing2D.Colors
Imports Microsoft.VisualBasic.Imaging.Physics.Boids
Imports std = System.Math

Public Module SDRender

    Dim colors As Integer()
    Dim n As Integer = 30
    Dim whiteArgb As Integer

    ' persistent back buffer; recreated only when the canvas size changes so that
    ' a simulation frame performs zero bitmap allocation
    Dim bmp As Bitmap
    Dim bmpW As Integer
    Dim bmpH As Integer

    ' legacy arrow geometry (RenderShape.RenderBoid)
    Const maxArrowLength As Double = 8.0
    Const headLength As Double = 10.0
    Const headWidth As Double = 7.0

    Sub New()
        Dim lut = Designer.GetColors(ScalerPalette.viridis.Description, n)
        colors = New Integer(lut.Length - 1) {}

        For i As Integer = 0 To lut.Length - 1
            colors(i) = lut(i).ToArgb()
        Next

        whiteArgb = Color.White.ToArgb()
    End Sub

    ''' <summary>
    ''' Renders the whole field into the internal persistent bitmap and returns it.
    ''' </summary>
    Public Function RenderField(field As Field) As Bitmap
        Dim w As Integer = CInt(field.Width)
        Dim h As Integer = CInt(field.Height)

        If bmp Is Nothing OrElse bmpW <> w OrElse bmpH <> h Then
            If bmp IsNot Nothing Then
                bmp.Dispose()
            End If

            bmp = New Bitmap(w, h, PixelFormat.Format32bppPArgb)
            bmpW = w
            bmpH = h
        End If

        DrawBoids(field, w, h)

        Return bmp
    End Function

    ''' <summary>
    ''' Renders the field and shows the result in a picture box without 
    ''' reallocating the bitmap on every frame.
    ''' </summary>
    Public Sub RenderTo(field As Field, target As PictureBox)
        RenderField(field)

        If target.Image Is bmp Then
            ' the bitmap instance did not change, just ask for a repaint
            target.Invalidate()
        Else
            target.Image = bmp
        End If
    End Sub

    Private Sub DrawBoids(field As Field, w As Integer, h As Integer)
        Dim state = field.State
        Dim px = state.px
        Dim py = state.py
        Dim vx = state.vx
        Dim vy = state.vy
        Dim count = state.count
        Dim maxSpeed = field.MaxSpeed
        Dim predCount = field.PredatorCount
        Dim ncolors = colors.Length

        Dim rect As New Rectangle(0, 0, w, h)
        Dim data As BitmapData = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb)

        Try
            Dim strideInts As Integer = data.Stride \ 4
            Dim buf As Integer* = CType(data.Scan0.ToPointer(), Integer*)

            ' transparent background (the picture box back color shows through)
            Unsafe.InitBlock(CType(data.Scan0.ToPointer(), Byte*), 0, CUInt(data.Stride * h))

            For i As Integer = 0 To count - 1
                Dim bx = px(i)
                Dim by = py(i)
                Dim vxv = vx(i)
                Dim vyv = vy(i)
                Dim speed = std.Sqrt(vxv * vxv + vyv * vyv)

                Dim c As Integer

                If i < predCount Then
                    c = whiteArgb
                Else
                    ' same speed -> color mapping as the legacy renderer
                    Dim lv As Integer = CInt(speed / maxSpeed * ncolors) - 1

                    If lv < 0 Then
                        lv = 0
                    ElseIf lv >= ncolors Then
                        lv = ncolors - 1
                    End If

                    c = colors(lv)
                End If

                ' arrow scale, identical to RenderShape.RenderBoid(maxL := 8)
                Dim scaleFactor As Double

                If speed > maxArrowLength Then
                    scaleFactor = maxArrowLength / speed
                Else
                    scaleFactor = 1.0
                End If

                DrawArrow(buf, strideInts, w, h,
                         bx, by,
                         bx + vxv * scaleFactor,
                         by + vyv * scaleFactor,
                         c)
            Next
        Finally
            bmp.UnlockBits(data)
        End Try
    End Sub

    ''' <summary>
    ''' Draws one legacy-style boid arrow: a 3px wide shaft from (x0,y0) to 
    ''' (x1,y1) plus a filled triangle head at the tip.
    ''' </summary>
    Private Sub DrawArrow(ByVal buf As Integer*, ByVal strideInts As Integer,
                          w As Integer, h As Integer,
                          x0 As Double, y0 As Double, x1 As Double, y1 As Double,
                          c As Integer)
        Dim dx As Double = x1 - x0
        Dim dy As Double = y1 - y0
        Dim len As Double = std.Sqrt(dx * dx + dy * dy)

        ' shaft
        If len > 0.0 Then
            Dim steps As Integer = CInt(len)

            If steps < 1 Then steps = 1

            Dim sx As Double = dx / steps
            Dim sy As Double = dy / steps
            Dim cxp As Double = x0
            Dim cyp As Double = y0

            For s As Integer = 0 To steps
                Dim xi As Integer = CInt(cxp)
                Dim yi As Integer = CInt(cyp)

                PutPixel(buf, strideInts, w, h, xi, yi, c)
                PutPixel(buf, strideInts, w, h, xi + 1, yi, c)
                PutPixel(buf, strideInts, w, h, xi, yi + 1, c)

                cxp += sx
                cyp += sy
            Next
        End If

        ' head triangle: tip at (x1,y1), length 10, width 7
        Dim ux As Double, uy As Double

        If len > 0.0 Then
            ux = dx / len
            uy = dy / len
        Else
            ux = 1.0
            uy = 0.0
        End If

        Dim baseX As Double = x1 - headLength * ux
        Dim baseY As Double = y1 - headLength * uy

        FillTriangle(buf, strideInts, w, h,
                     x1, y1,
                     baseX + headWidth * uy, baseY - headWidth * ux,
                     baseX - headWidth * uy, baseY + headWidth * ux,
                     c)
    End Sub

    ''' <summary>
    ''' Scanline filled triangle (same vertex order as the legacy head).
    ''' </summary>
    Private Sub FillTriangle(ByVal buf As Integer*, ByVal strideInts As Integer,
                             w As Integer, h As Integer,
                             ax As Double, ay As Double,
                             bx As Double, by As Double,
                             cx As Double, cy As Double,
                             c As Integer)
        Dim minY As Integer = CInt(std.Floor(std.Min(ay, std.Min(by, cy))))
        Dim maxY As Integer = CInt(std.Ceiling(std.Max(ay, std.Max(by, cy)))) - 1

        If minY < 0 Then minY = 0
        If maxY >= h Then maxY = h - 1
        If minY > maxY Then Return

        For yi As Integer = minY To maxY
            Dim yc As Double = yi + 0.5

            ' horizontal span of the triangle at this scanline
            Dim x0 As Double = Double.MaxValue
            Dim x1 As Double = Double.MinValue
            Dim found As Integer = 0

            found += ScanEdge(ax, ay, bx, by, yc, x0, x1)
            found += ScanEdge(bx, by, cx, cy, yc, x0, x1)
            found += ScanEdge(cx, cy, ax, ay, yc, x0, x1)

            If found < 2 Then Continue For

            Dim fromX As Integer = CInt(std.Ceiling(x0 - 0.5))
            Dim toX As Integer = CInt(std.Floor(x1 - 0.5))

            If fromX < 0 Then fromX = 0
            If toX >= w Then toX = w - 1

            For xi As Integer = fromX To toX
                buf(yi * strideInts + xi) = c
            Next
        Next
    End Sub

    ''' <summary>
    ''' Tests one triangle edge against the horizontal scanline and merges the 
    ''' crossing x into the [x0, x1] span. Returns 1 when the edge crosses.
    ''' </summary>
    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Private Function ScanEdge(ax As Double, ay As Double, bx As Double, by As Double,
                              yc As Double, ByRef x0 As Double, ByRef x1 As Double) As Integer
        If (ay <= yc AndAlso by > yc) OrElse (by <= yc AndAlso ay > yc) Then
            Dim t As Double = (yc - ay) / (by - ay)
            Dim x As Double = ax + t * (bx - ax)

            If x < x0 Then x0 = x
            If x > x1 Then x1 = x

            Return 1
        End If

        Return 0
    End Function

    ''' <summary>
    ''' Writes one pixel with bounds clipping.
    ''' </summary>
    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Private Sub PutPixel(ByVal buf As Integer*, ByVal strideInts As Integer,
                         w As Integer, h As Integer, x As Integer, y As Integer, c As Integer)
        If x < 0 OrElse x >= w OrElse y < 0 OrElse y >= h Then Return

        buf(y * strideInts + x) = c
    End Sub

End Module
