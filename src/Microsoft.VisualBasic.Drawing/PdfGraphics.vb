Imports System.IO
Imports SkiaSharp

Public Class PdfGraphics : Inherits SkiaGraphics

    ReadOnly s As New MemoryStream
    ReadOnly document As SKDocument = SKDocument.CreatePdf(s)

    Public Sub New(width As Integer, height As Integer)
        MyBase.New(width, height)
        m_canvas = document.BeginPage(width, height)
    End Sub

    Public Overrides Sub Save(file As Stream)
        document.EndPage()
        document.Dispose()

        s.Seek(Scan0, SeekOrigin.Begin)
        s.CopyTo(file)
    End Sub
End Class
