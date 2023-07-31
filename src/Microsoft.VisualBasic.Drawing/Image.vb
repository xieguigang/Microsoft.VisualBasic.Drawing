Imports Microsoft.VisualBasic.Drawing.ImageFormat
Imports Microsoft.VisualBasic.Imaging.BitmapImage

Public Class Image

    ReadOnly core As BitmapBuffer

    Sub New(file As FileEncoder)
        core = file.LoadBuffer
    End Sub

End Class
