Imports System.IO
Imports Microsoft.VisualBasic.Drawing
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Driver
Imports SkiaSharp
Imports SkiaSharp.Views.Desktop

Public Class SkiaView : Inherits SKControl

    Private Sub SkiaView_PaintSurface(sender As Object, e As SKPaintSurfaceEventArgs) Handles Me.PaintSurface
        Dim canvas As SKSurface = e.Surface
        Dim skia As New Graphics(canvas, Size)

        Call Render(skia)
        Call skia.Flush()
    End Sub

    ''' <summary>
    ''' Overrides and create the UI rendering code at here
    ''' </summary>
    ''' <param name="g"></param>
    Protected Overridable Sub Render(g As IGraphics)

    End Sub

    Private Class Graphics : Inherits SkiaGraphics

        Public Overrides ReadOnly Property Driver As Drivers
            Get
                Return Drivers.GDI
            End Get
        End Property

        Sub New(canvas As SKSurface, size As Size)
            Call MyBase.New(size.Width, size.Height, 100)
            ' hook canvas for control rendering
            m_canvas = canvas.Canvas
        End Sub

        Public Overrides Sub Save(file As Stream)
            Throw New NotImplementedException()
        End Sub

        Protected Overrides Sub ReleaseHandle()
            Throw New NotImplementedException()
        End Sub

        Public Overrides Function Save(Stream As Stream, format As ImageFormats) As Boolean
            Throw New NotImplementedException()
        End Function
    End Class
End Class
