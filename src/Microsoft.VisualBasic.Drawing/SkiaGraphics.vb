Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Math2D
Imports SkiaSharp

Public MustInherit Class SkiaGraphics : Inherits DrawingGraphics

    Protected ReadOnly canvasRect As SKRect
    Protected m_canvas As SKCanvas

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Sub New(width As Integer, height As Integer)
        Call MyBase.New(width, height)
        canvasRect = SKRect.Create(width, height)
    End Sub

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Overrides Sub Clear(fill As ArgbColor)
        Call m_canvas.Clear(fill.AsSKColor)
    End Sub

    Public Overloads Sub Clear(color As String)
        Call m_canvas.Clear(ArgbColor.TranslateColor(color).AsSKColor)
    End Sub

    Public Overrides Sub DrawString(s As String, fontName As String, fontSize As Single, color As ArgbColor, x As Single, y As Single)
        Dim textPain As New SKPaint With {
           .IsAntialias = True,
           .Style = SKPaintStyle.Fill,
           .Color = color.AsSKColor,
           .TextSize = fontSize,
           .Typeface = SKTypeface.FromFamilyName(fontName)
        }

        m_canvas.DrawText(s, x, y, textPain)
    End Sub

    Public Overrides Sub DrawLine(x1 As Single, y1 As Single, x2 As Single, y2 As Single, color As ArgbColor, width As Single,
                                  Optional dash As Single() = Nothing)

        Using paint As New SKPaint With {
            .Color = color.AsSKColor,
            .StrokeWidth = width
        }
            If Not dash.IsNullOrEmpty Then
                paint.PathEffect = SKPathEffect.CreateDash(dash, 0)
            End If

            Call m_canvas.DrawLine(x1, y1, x2, y2, paint)
        End Using
    End Sub

    Public Overrides Sub DrawPath(path As Polygon2D, color As ArgbColor, width As Single,
                                  Optional fill As ArgbColor? = Nothing,
                                  Optional dash As Single() = Nothing)

        Using skpath As New SKPath
            Dim x = path.xpoints
            Dim y = path.ypoints

            Call skpath.MoveTo(x(0), y(0))

            For i As Integer = 1 To x.Length - 1
                Call skpath.LineTo(x(i), y(i))
            Next

            Call skpath.Close()

            If Not fill Is Nothing Then
                Using paint As New SKPaint With {
                    .Style = SKPaintStyle.Fill,
                    .Color = CType(fill, ArgbColor).AsSKColor
                }
                    Call m_canvas.DrawPath(skpath, paint)
                End Using
            End If

            Using paint As New SKPaint With {
                .Color = color.AsSKColor,
                .StrokeWidth = width,
                .Style = SKPaintStyle.Stroke
            }
                If Not dash.IsNullOrEmpty Then
                    paint.PathEffect = SKPathEffect.CreateDash(dash, 0)
                End If

                Call m_canvas.DrawPath(skpath, paint)
            End Using
        End Using
    End Sub

    Public Overrides Function MeasureString(text As String, fontName As String, fontSize As Single) As (Width As Single, Height As Single)
        Using paint As New SKPaint With {
            .TextSize = fontSize,
            .IsAntialias = True,
            .Typeface = SKTypeface.FromFamilyName(fontName)
        }

            Dim textBounds As New SKRect
            Call paint.MeasureText(text, textBounds)
            Return (textBounds.Width, textBounds.Height)
        End Using
    End Function
End Class
