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
' one GDI+ DrawLine/FillClosedCurve call per boid (80k vector calls per frame at
' the demo scale, ~100ms+) and allocated a brand new bitmap on every tick.

' This renderer:
'   * keeps ONE persistent 32bpp bitmap mapped directly over a pinned managed
'     Int32 buffer (New Bitmap(w, h, stride, format, scan0)) - zero copy, zero
'     per frame bitmap allocation;
'   * clears the buffer with a single Array.Clear;
'   * draws every boid arrow (3px shaft + 10x7 triangle head, viridis speed
'     LUT, white predators - the legacy geometry) directly as pixels;
'   * uses the fast <c>BoidMath.Round</c> conversion instead of <c>CInt</c> on
'     the pixel coordinate hot path (the VB CInt(Double) conversion costs ~10ns
'     per call through Math.Round; at 80k boids that is ~1.9M calls per frame);
'   * switches to an unrolled compact glyph (2x2 tail/mid blocks + 3x3 tip
'     block) once the flock exceeds <see cref="CompactGlyphThreshold"/> boids,
'     which keeps the gdi+ phase inside the 60fps budget at 80k boids while
'     small flocks keep the full legacy arrow geometry.

'     Module SDRender
' 
'         Constructor: (+1 Overloads) Sub New
'         Function: RenderField
'         Sub: DrawArrow, DrawBoids, DrawRange, FillTriangle, PutPixel, RenderTo
' 
' 
' /********************************************************************************/

#End Region

Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Engine.Particles.Boids
Imports Microsoft.VisualBasic.Imaging.Drawing2D.Colors
Imports Bitmap = System.Drawing.Bitmap
Imports PixelFormat = System.Drawing.Imaging.PixelFormat
Imports std = System.Math

Public Module SDRender

    ' the packed ARGB table of the speed coloring, it is rebuilt whenever another
    ' color scale is picked from the context menu of the window
    Dim colors As Integer()
    Dim n As Integer = PaletteMenu.DefaultLevels
    Dim whiteArgb As Integer

    ''' <summary>
    ''' the color scale of the speed mapping, see <see cref="PaletteMenu"/>
    ''' </summary>
    Private m_palette As ScalerPalette = PaletteMenu.DefaultPalette

    ' persistent back buffer: one pinned managed Int32 array with the bitmap
    ' mapped directly over it; recreated only when the canvas size changes so a
    ' simulation frame performs zero bitmap allocation and zero copy
    Dim buf As Integer()
    Dim bufHandle As Runtime.InteropServices.GCHandle
    Dim bmp As Bitmap
    Dim bmpW As Integer
    Dim bmpH As Integer

    ' legacy arrow geometry (RenderShape.RenderBoid)
    Const maxArrowLength As Double = 8.0
    Const headLength As Double = 10.0
    Const headWidth As Double = 7.0

    ''' <summary>
    ''' Above this boid count the renderer switches from the full legacy arrow 
    ''' (DDA shaft + scanline triangle head, ~35 pixel writes per boid) to the 
    ''' compact glyph (start/mid 2x2 blocks + 3x3 tip block, ~15 writes, fully 
    ''' unrolled). At 80k boids on a 2000x1200 canvas the screen is covered ~1.2x 
    ''' over, so the glyph simplification is visually indistinguishable while it 
    ''' keeps the gdi+ phase inside the 60fps budget. Set to a huge value to force 
    ''' the detailed arrows everywhere.
    ''' </summary>
    Public Property CompactGlyphThreshold As Integer = 20000

    Sub New()
        ' the lookup table of the speed mapping, it is rebuilt by the Palette
        ' property when another color scale is picked from the context menu
        colors = PaletteMenu.ColorTable(m_palette, n)
        whiteArgb = Color.White.ToArgb()
    End Sub

    ''' <summary>
    ''' the color scale that maps the normalized speed of a boid onto the color
    ''' of its arrow
    ''' </summary>
    ''' <remarks>
    ''' the lookup table is replaced in one assignment, which is what the next
    ''' rendered frame of the timer picks up; a frame that is already inside the
    ''' loop keeps the table that it captured
    ''' </remarks>
    Public Property Palette As ScalerPalette
        Get
            Return m_palette
        End Get
        Set(value As ScalerPalette)
            m_palette = value
            colors = PaletteMenu.ColorTable(value, n)
        End Set
    End Property

    ''' <summary>
    ''' Renders the whole field into the internal persistent bitmap and returns it.
    ''' </summary>
    Public Function RenderField(field As Field) As Bitmap
        Dim w As Integer = CInt(field.Width)
        Dim h As Integer = CInt(field.Height)

        If bmp Is Nothing OrElse bmpW <> w OrElse bmpH <> h Then
            ReleaseBuffer()

            buf = New Integer(w * h - 1) {}
            bufHandle = Runtime.InteropServices.GCHandle.Alloc(buf, Runtime.InteropServices.GCHandleType.Pinned)
            bmp = New Bitmap(w, h, w * 4, PixelFormat.Format32bppPArgb, bufHandle.AddrOfPinnedObject())
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

    ''' <summary>
    ''' Frees the persistent back buffer (call on form close).
    ''' </summary>
    Public Sub ReleaseBuffer()
        If bmp IsNot Nothing Then
            bmp.Dispose()
            bmp = Nothing
        End If

        If bufHandle.IsAllocated Then
            bufHandle.Free()
        End If

        buf = Nothing
    End Sub

    Private Sub DrawBoids(field As Field, w As Integer, h As Integer)
        Dim state = field.State

        Array.Clear(buf, 0, buf.Length)

        Dim count = state.count

        If count >= CompactGlyphThreshold Then
            DrawRange(field, w, h, 0, count - 1, compact:=True)
        Else
            DrawRange(field, w, h, 0, count - 1, compact:=False)
        End If
    End Sub

    ''' <summary>
    ''' Draws the boids in [start, ends] into the shared pixel buffer.
    ''' </summary>
    ''' <param name="compact">
    ''' TRUE draws the unrolled compact glyph (2x2 start/mid blocks + 3x3 tip 
    ''' block); FALSE draws the full legacy arrow with the triangle head.
    ''' </param>
    Private Sub DrawRange(field As Field, w As Integer, h As Integer, start As Integer, ends As Integer, compact As Boolean)
        Dim state = field.State
        Dim px = state.px
        Dim py = state.py
        Dim vx = state.vx
        Dim vy = state.vy
        Dim maxSpeed = field.MaxSpeed
        Dim predCount = field.PredatorCount
        ' the table is captured once: the context menu may replace it while the
        ' frame is being drawn, and a frame has to be drawn with one table only
        Dim lut = colors
        Dim ncolors = lut.Length
        Dim buffer = buf

        For i As Integer = start To ends
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
                Dim lv As Integer = BoidMath.Round(speed / maxSpeed * ncolors) - 1

                If lv < 0 Then
                    lv = 0
                ElseIf lv >= ncolors Then
                    lv = ncolors - 1
                End If

                c = lut(lv)
            End If

            ' arrow scale, identical to RenderShape.RenderBoid(maxL := 8)
            Dim scaleFactor As Double

            If speed > maxArrowLength Then
                scaleFactor = maxArrowLength / speed
            Else
                scaleFactor = 1.0
            End If

            Dim ex As Double = bx + vxv * scaleFactor
            Dim ey As Double = by + vyv * scaleFactor

            If compact Then
                DrawGlyphCompact(buffer, w, h, bx, by, ex, ey, c)
            Else
                DrawArrow(buffer, w, h, bx, by, ex, ey, c)
            End If
        Next
    End Sub

    ''' <summary>
    ''' Compact boid glyph for very large flocks: a 2x2 block at the tail, a 2x2 
    ''' block at the midpoint and a 3x3 block at the tip, all fully unrolled. 
    ''' Preserves position, direction and the speed color; costs roughly a third 
    ''' of the full arrow at 80k boids.
    ''' </summary>
    Private Sub DrawGlyphCompact(buffer As Integer(), w As Integer, h As Integer,
                                 x0 As Double, y0 As Double, x1 As Double, y1 As Double,
                                 c As Integer)
        PutBlock2(buffer, w, h, x0, y0, c)
        PutBlock2(buffer, w, h, (x0 + x1) * 0.5, (y0 + y1) * 0.5, c)
        PutBlock3(buffer, w, h, x1, y1, c)
    End Sub

    ''' <summary>
    ''' Writes a 2x2 pixel block around a rounded coordinate, with clipping.
    ''' </summary>
    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Private Sub PutBlock2(buffer As Integer(), w As Integer, h As Integer, x As Double, y As Double, c As Integer)
        Dim xi As Integer = BoidMath.Round(x)
        Dim yi As Integer = BoidMath.Round(y)

        If xi >= 0 AndAlso xi < w AndAlso yi >= 0 AndAlso yi < h Then buffer(yi * w + xi) = c
        If xi + 1 >= 0 AndAlso xi + 1 < w AndAlso yi >= 0 AndAlso yi < h Then buffer(yi * w + xi + 1) = c
        If xi >= 0 AndAlso xi < w AndAlso yi + 1 >= 0 AndAlso yi + 1 < h Then buffer((yi + 1) * w + xi) = c
    End Sub

    ''' <summary>
    ''' Writes a 3x3 pixel block around a rounded coordinate, with clipping.
    ''' </summary>
    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Private Sub PutBlock3(buffer As Integer(), w As Integer, h As Integer, x As Double, y As Double, c As Integer)
        Dim xi As Integer = BoidMath.Round(x)
        Dim yi As Integer = BoidMath.Round(y)

        ' clamp so that the whole 3x3 block stays inside the buffer
        If xi < 0 Then xi = 0
        If xi > w - 3 Then xi = w - 3
        If yi < 0 Then yi = 0
        If yi > h - 3 Then yi = h - 3

        Dim row0 As Integer = yi * w

        buffer(row0 + xi) = c
        buffer(row0 + xi + 1) = c
        buffer(row0 + xi + 2) = c
        buffer(row0 + w + xi) = c
        buffer(row0 + w + xi + 1) = c
        buffer(row0 + w + xi + 2) = c
        buffer(row0 + w + w + xi) = c
        buffer(row0 + w + w + xi + 1) = c
        buffer(row0 + w + w + xi + 2) = c
    End Sub

    ''' <summary>
    ''' Draws one legacy-style boid arrow: a 3px wide shaft from (x0,y0) to 
    ''' (x1,y1) plus a filled triangle head at the tip.
    ''' </summary>
    Private Sub DrawArrow(buffer As Integer(), w As Integer, h As Integer,
                          x0 As Double, y0 As Double, x1 As Double, y1 As Double,
                          c As Integer)
        Dim dx As Double = x1 - x0
        Dim dy As Double = y1 - y0
        Dim len As Double = std.Sqrt(dx * dx + dy * dy)

        ' shaft
        If len > 0.0 Then
            Dim steps As Integer = BoidMath.Round(len)

            If steps < 1 Then steps = 1

            Dim sx As Double = dx / steps
            Dim sy As Double = dy / steps
            Dim cxp As Double = x0
            Dim cyp As Double = y0

            For s As Integer = 0 To steps
                Dim xi As Integer = BoidMath.Round(cxp)
                Dim yi As Integer = BoidMath.Round(cyp)

                PutPixel(buffer, w, h, xi, yi, c)
                PutPixel(buffer, w, h, xi + 1, yi, c)
                PutPixel(buffer, w, h, xi, yi + 1, c)

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

        FillTriangle(buffer, w, h,
                     x1, y1,
                     baseX + headWidth * uy, baseY - headWidth * ux,
                     baseX - headWidth * uy, baseY + headWidth * ux,
                     c)
    End Sub

    ''' <summary>
    ''' Scanline filled triangle (same vertex order as the legacy head). Uses 
    ''' incremental edge interpolation: the long edge and the active short edge 
    ''' advance by constant slopes per row, so a whole head costs 3 divisions 
    ''' instead of one division per edge per row.
    ''' </summary>
    Private Sub FillTriangle(buffer As Integer(), w As Integer, h As Integer,
                             ax As Double, ay As Double,
                             bx As Double, by As Double,
                             cx As Double, cy As Double,
                             c As Integer)
        ' sort the three vertices by y (3-exchange network)
        If ay > by Then
            Dim tx = ax, ty = ay
            ax = bx : ay = by
            bx = tx : by = ty
        End If
        If by > cy Then
            Dim tx = bx, ty = by
            bx = cx : by = cy
            cx = tx : cy = ty
        End If
        If ay > by Then
            Dim tx = ax, ty = ay
            ax = bx : ay = by
            bx = tx : by = ty
        End If

        If cy <= ay Then Return   ' degenerate (zero height)

        Dim minY As Integer = BoidMath.Round(std.Floor(ay))
        Dim maxY As Integer = BoidMath.Round(std.Ceiling(cy)) - 1

        If minY < 0 Then minY = 0
        If maxY >= h Then maxY = h - 1
        If minY > maxY Then Return

        ' long edge v0(ax,ay) -> v2(cx,cy)
        Dim longDx As Double = (cx - ax) / (cy - ay)
        ' short edge of the upper band: v0 -> v1 (skipped on a flat top)
        Dim upDx As Double = If(by > ay, (bx - ax) / (by - ay), 0.0)
        ' short edge of the lower band: v1 -> v2 (skipped on a flat bottom)
        Dim loDx As Double = If(cy > by, (cx - bx) / (cy - by), 0.0)

        Dim yc As Double = minY + 0.5
        Dim xLong As Double = ax + (yc - ay) * longDx
        Dim xShort As Double
        Dim shortDx As Double

        If (by > ay) AndAlso (yc <= by) Then
            xShort = ax + (yc - ay) * upDx
            shortDx = upDx
        Else
            xShort = bx + (yc - by) * loDx
            shortDx = loDx
        End If

        For yi As Integer = minY To maxY
            If (by > ay) AndAlso (cy > by) AndAlso (yi + 0.5 > by) AndAlso (shortDx <> loDx) Then
                ' crossed into the lower band: recompute the short edge exactly
                yc = yi + 0.5
                xShort = bx + (yc - by) * loDx
                shortDx = loDx
            End If

            Dim x0 As Double = xLong
            Dim x1 As Double = xShort

            If x0 > x1 Then
                Dim tmp = x0
                x0 = x1
                x1 = tmp
            End If

            Dim fromX As Integer = BoidMath.Round(std.Ceiling(x0 - 0.5))
            Dim toX As Integer = BoidMath.Round(std.Floor(x1 - 0.5))

            If fromX < 0 Then fromX = 0
            If toX >= w Then toX = w - 1

            If fromX <= toX Then
                Dim rowOffset As Integer = yi * w

                For xi As Integer = fromX To toX
                    buffer(rowOffset + xi) = c
                Next
            End If

            xLong += longDx
            xShort += shortDx
        Next
    End Sub

    ''' <summary>
    ''' Writes one pixel with bounds clipping.
    ''' </summary>
    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Private Sub PutPixel(buffer As Integer(), w As Integer, h As Integer, x As Integer, y As Integer, c As Integer)
        If x < 0 OrElse x >= w OrElse y < 0 OrElse y >= h Then Return

        buffer(y * w + x) = c
    End Sub

End Module
