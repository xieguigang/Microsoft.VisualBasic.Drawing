Imports System.IO
Imports System.Text
Imports Microsoft.VisualBasic.Imaging.Driver
Imports SkiaSharp

Public Class SvgGraphics : Inherits SkiaGraphics
    Implements ISvgGraphics

    ReadOnly svgfile As New MemoryStream

    Public Overrides ReadOnly Property Driver As Drivers
        Get
            Return Drivers.SVG
        End Get
    End Property

    Public Sub New(width As Integer, height As Integer, Optional dpi As Integer = 100)
        MyBase.New(width, height, dpi)

        m_canvas = SKSvgCanvas.Create(canvasRect, svgfile)
    End Sub

    Public Sub Close()
        If Not m_isDisposed Then
            ' commit current graphics drawing layer
            Call m_canvas.Flush()
            ' commit the correct xml text
            Call m_canvas.Dispose()
        End If

        m_isDisposed = True
    End Sub

    ''' <summary>
    ''' get the svg file of the current graphics
    ''' </summary>
    ''' <returns></returns>
    Public Function GetSvgText() As String Implements ISvgGraphics.GetSvgText
        Call Close()
        Call svgfile.Flush()

        Return Encoding.UTF8.GetString(svgfile.ToArray)
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="file"></param>
    ''' <remarks>
    ''' this function will release the svg canvas
    ''' </remarks>
    Public Overrides Sub Save(file As Stream)
        Call Close()

        svgfile.Position = Scan0
        svgfile.CopyTo(file)

        Call file.Flush()
    End Sub

    Protected Overrides Sub ReleaseHandle()
        Try
            Call Close()
        Catch ex As Exception

        End Try
    End Sub

    Public Overrides Function Save(Stream As Stream, format As Imaging.ImageFormats) As Boolean
        Try
            Call Save(Stream)
        Catch ex As Exception
            Call App.LogException(ex)
            Return False
        End Try

        Return True
    End Function
End Class
