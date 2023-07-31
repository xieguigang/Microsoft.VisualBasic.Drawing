Imports System.IO
Imports Microsoft.VisualBasic.Imaging.BitmapImage

Namespace ImageFormat

    Public Class Jpeg : Inherits FileEncoder


        Sub New(file As Stream)
            Call MyBase.New(file)
        End Sub

        Public Overrides Function LoadBuffer() As BitmapBuffer
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace