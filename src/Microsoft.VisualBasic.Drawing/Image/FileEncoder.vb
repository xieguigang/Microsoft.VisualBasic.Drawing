Imports Microsoft.VisualBasic.Imaging.BitmapImage

Namespace ImageFormat

    Public MustInherit Class FileEncoder

        Public MustOverride Function LoadBuffer() As BitmapBuffer

    End Class
End Namespace