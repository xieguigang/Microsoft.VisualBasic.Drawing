Imports System.IO
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Imaging
Imports SkiaSharp

Public MustInherit Class SkiaGraphics

    Protected ReadOnly w As Integer
    Protected ReadOnly h As Integer

    Protected ReadOnly canvasRect As SKRect
    Protected m_canvas As SKCanvas

    Public ReadOnly Property Width As Integer
        Get
            Return w
        End Get
    End Property

    Public ReadOnly Property Height As Integer
        Get
            Return h
        End Get
    End Property

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Sub New(width As Integer, height As Integer)
        w = width
        h = height
        canvasRect = SKRect.Create(width, height)
    End Sub

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Sub Clear(fill As ArgbColor)
        Call m_canvas.Clear(fill.AsSKColor)
    End Sub

    Public Sub Clear(color As String)
        Call m_canvas.Clear(ArgbColor.TranslateColor(color).AsSKColor)
    End Sub

    Public Sub DrawString(s As String, fontName As String, fontSize As Single, color As ArgbColor, x As Single, y As Single)
        Dim textPain As New SKPaint With {
           .IsAntialias = True,
           .Style = SKPaintStyle.Fill,
           .Color = color.AsSKColor,
           .TextSize = fontSize,
           .Typeface = SKTypeface.FromFamilyName(fontName)
        }

        m_canvas.DrawText(s, x, y, textPain)
    End Sub

    Public Function MeasureString(text As String, fontName As String, fontSize As Single) As (Width As Single, Height As Single)
        Using paint As New SKPaint With {
            .TextSize = fontSize,
            .IsAntialias = True,
            .Typeface = SKTypeface.FromFamilyName(fontName)
        }

            Dim textBounds As New SKRect
            Dim w = paint.MeasureText(text, textBounds)

            Return (w, textBounds.Height)
        End Using
    End Function

    ''' <summary>
    ''' save default image file
    ''' </summary>
    ''' <param name="file"></param>
    Public Sub Save(file As String)
        Using s As Stream = file.Open(FileMode.OpenOrCreate, doClear:=True,)
            Call Save(s)
        End Using
    End Sub

    Public MustOverride Sub Save(file As Stream)

End Class
