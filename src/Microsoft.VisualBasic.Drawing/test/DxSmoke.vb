Imports System.Drawing
Imports Microsoft.VisualBasic.Drawing.DirectX
Imports Microsoft.VisualBasic.Imaging

Imports Pen = Microsoft.VisualBasic.Imaging.Pen
Imports SolidBrush = Microsoft.VisualBasic.Imaging.SolidBrush

Module DxSmoke

    Sub Smoke()
        Console.WriteLine("[1] create canvas ...")

        Dim g As New DxGraphics(400, 300, "#ffffff")

        Console.WriteLine("[2] canvas created, device = " & g.DeviceDescription)

        Call g.Flush()
        Console.WriteLine("[2.1] flush ok (no com out param)")

        Call g.SetClip(New Rectangle(0, 0, 400, 300))
        Console.WriteLine("[2.2] clip ok")

        Call g.ResetClip()
        Console.WriteLine("[2.3] reset clip ok")

        Call g.FillRectangle(New SolidBrush(Color.Red), New Rectangle(10, 10, 100, 50))
        Console.WriteLine("[3] fill rectangle ok")

        Call g.FillPolygon(New SolidBrush(Color.Green), {
            New PointF(120, 20), New PointF(200, 30), New PointF(180, 90), New PointF(130, 80)
        })
        Console.WriteLine("[4] fill polygon ok")

        Call g.DrawPolygon(New Pen(Color.Blue, 2), {
            New PointF(220, 20), New PointF(300, 30), New PointF(280, 90), New PointF(230, 80)
        })
        Console.WriteLine("[5] draw polygon ok")

        Call g.FlushBatch()
        Console.WriteLine("[6] batch flushed")

        Dim img = g.GetRasterImage()

        Console.WriteLine("[7] read back ok, size = " & img.Size.ToString)

        Call img.Save("./dx_smoke.png", ImageFormats.Png)
        Console.WriteLine("[8] saved ./dx_smoke.png")

        Call g.Dispose()
        Console.WriteLine("[9] done")
    End Sub
End Module
