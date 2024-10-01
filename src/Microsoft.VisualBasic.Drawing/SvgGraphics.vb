Imports System.IO
Imports SkiaSharp

Public Class SvgGraphics : Inherits SkiaGraphics

    ReadOnly svgfile As New MemoryStream

    Public Sub New(width As Integer, height As Integer, Optional dpi As Integer = 100)
        MyBase.New(width, height, dpi)

        m_canvas = SKSvgCanvas.Create(canvasRect, svgfile)
    End Sub

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="file"></param>
    ''' <remarks>
    ''' this function will release the svg canvas
    ''' </remarks>
    Public Overrides Sub Save(file As Stream)
        Call m_canvas.Flush()
        Call m_canvas.Dispose()

        svgfile.Position = Scan0
        svgfile.CopyTo(file)

        Call file.Flush()
    End Sub

    Public Overrides Sub Dispose()
        Try
            Call m_canvas.Flush()
            Call m_canvas.Dispose()
        Catch ex As Exception

        End Try
    End Sub
End Class
