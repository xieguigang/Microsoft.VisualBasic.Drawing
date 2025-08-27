Imports System.IO
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Imaging.BitmapImage
Imports SkiaSharp
Imports Image = Microsoft.VisualBasic.Drawing.Tiff.Types.Image

Public Module TIFFTools

    <Extension>
    Public Sub SaveTiff(layers As IEnumerable(Of SKBitmap), file As Stream)
        Dim tiff As New Tiff.Tiff()

        For Each layer As SKBitmap In layers
            Call tiff.Images.Add(layer.ToImage)
        Next

        Call tiff.Save(file)
        Call file.Flush()
    End Sub

    <Extension>
    Private Function ToImage(bmp As SKBitmap) As Image
        Dim skimg As New SkiaImage(bmp)
        Dim buffer As BitmapBuffer = skimg.ToBitmap.MemoryBuffer
        Return Image.FromBitmap(buffer)
    End Function
End Module
