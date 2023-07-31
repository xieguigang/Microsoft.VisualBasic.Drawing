Imports System.IO
Imports Microsoft.VisualBasic.Imaging.BitmapImage

Namespace ImageFormat

    Public MustInherit Class FileEncoder

        Protected ReadOnly file As Stream

        Sub New(file As Stream)
            Me.file = file
        End Sub

        Public MustOverride Function LoadBuffer() As BitmapBuffer

    End Class
End Namespace