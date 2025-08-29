Imports System.IO
Imports Microsoft.VisualBasic.Drawing
Imports Microsoft.VisualBasic.Drawing.Tiff

Module tifftest

    Sub run()
        Call writeTest()
        Call readTest()
    End Sub

    Sub writeTest()
        Dim img = SkiaImage.FromFile("E:\Microsoft.VisualBasic.Drawing\demo\flower-true-color.JPG")
        Dim s = "./aaa.tiff".Open(FileMode.OpenOrCreate, doClear:=True, [readOnly]:=False)

        Call TIFFTools.SaveTiff({img}, s)

        '  Pause()
    End Sub

    Sub readTest()
        Dim tiff As New Tiff("F:\IHC1_ch00-tiled.tiff")

        Pause()
    End Sub
End Module
