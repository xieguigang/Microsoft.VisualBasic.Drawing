Imports System.IO
Imports SkiaSharp

Public Class SvgGraphics : Inherits SkiaGraphics

    ReadOnly svg As New MemoryStream
    ReadOnly wstream As New SKManagedWStream(svg)
    ReadOnly writer As New SKXmlStreamWriter(wstream)

    Public Sub New(width As Integer, height As Integer)
        MyBase.New(width, height)
    End Sub

    Public Overrides Sub Clear(color As String)
        Throw New NotImplementedException()
    End Sub
End Class
