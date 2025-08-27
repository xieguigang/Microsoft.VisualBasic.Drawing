Imports System.IO
Imports Microsoft.VisualBasic.Drawing

Module tifftest

    Sub run()
        Dim img = SkiaImage.FromFile("E:\Microsoft.VisualBasic.Drawing\demo\flower-true-color.JPG")
        Dim s = "./aaa.tiff".Open(FileMode.OpenOrCreate, doClear:=True, [readOnly]:=False)

        Call TIFFTools.SaveTiff({img}, s)

        Pause()
    End Sub
End Module
