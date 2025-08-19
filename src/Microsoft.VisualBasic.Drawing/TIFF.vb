Imports System.IO
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Drawing.Tiff.Types
Imports SkiaSharp

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
        Dim image As New Image

        Return image
    End Function
End Module
