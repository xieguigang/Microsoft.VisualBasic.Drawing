Imports System.Drawing
Imports System.IO
Imports Microsoft.VisualBasic.Imaging.Driver
Imports Microsoft.VisualBasic.MIME.Html.CSS
Imports Microsoft.VisualBasic.Net.Http

Public Class PdfImage : Inherits GraphicsData

    Public Overrides ReadOnly Property Driver As Drivers
        Get
            Return Drivers.PDF
        End Get
    End Property

    Dim g As PdfGraphics

    Public Sub New(img As Object, size As Size, padding As Padding)
        MyBase.New(img, size, padding)

        g = img
    End Sub

    Public Overrides Function GetDataURI() As DataURI
        Using s As New MemoryStream
            Call Save(s)
            Call s.Flush()
            Call s.Seek(Scan0, SeekOrigin.Begin)

            Return New DataURI(s, mime:="application/pdf")
        End Using
    End Function

    Public Overrides Function Save(path As String) As Boolean
        Using s As Stream = path.Open(FileMode.OpenOrCreate, doClear:=True, [readOnly]:=False)
            Return Save(s)
        End Using
    End Function

    Public Overrides Function Save(out As Stream) As Boolean
        Call g.Save(out)
        Return True
    End Function
End Class
