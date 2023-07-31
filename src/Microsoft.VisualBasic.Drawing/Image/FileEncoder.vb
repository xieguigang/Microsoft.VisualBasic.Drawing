Imports System.IO
Imports Microsoft.VisualBasic.Imaging.BitmapImage

Namespace ImageFormat

    Public MustInherit Class FileEncoder

        Protected ReadOnly file As Stream

        Sub New(file As Stream)
            Me.file = file
        End Sub

        Public MustOverride Function LoadBuffer() As BitmapBuffer

        Public MustOverride Function SaveBuffer(img As BitmapBuffer) As Boolean

    End Class
End Namespace