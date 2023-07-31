Namespace ImageFormat

    Public Class Bitmap : Inherits FileEncoder

        Public Overrides Function LoadBuffer() As Imaging.BitmapImage.BitmapBuffer
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace