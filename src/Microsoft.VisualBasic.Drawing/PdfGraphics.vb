Imports System.IO
Imports SkiaSharp

Public Class PdfGraphics : Inherits SkiaGraphics

    ReadOnly s As New MemoryStream
    ReadOnly document As SKDocument = SKDocument.CreatePdf(s)

    Public Sub New(width As Integer, height As Integer, Optional dpi As Integer = 100)
        MyBase.New(width, height, dpi)

        m_canvas = document.BeginPage(width, height)
    End Sub

    Public Overrides Sub Save(file As Stream)
        Call ReleaseHandle()

        s.Seek(Scan0, SeekOrigin.Begin)
        s.CopyTo(file)
    End Sub

    Protected Overrides Sub ReleaseHandle()
        Try
            If Not m_isDisposed Then
                Call document.EndPage()
                Call document.Dispose()
            End If

            m_isDisposed = True
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
