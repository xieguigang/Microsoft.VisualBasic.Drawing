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
        Console.WriteLine(g.Probe())

        Call step_("clear", Sub() g.Clear(Color.White))
        Call step_("setclip", Sub() g.SetClip(New Rectangle(0, 0, 400, 300)))
        Call step_("resetclip", Sub() g.ResetClip())
        Call step_("translate", Sub() g.TranslateTransform(0, 0))
        Call step_("flushbatch", Sub() g.FlushBatch())
        Call step_("flush", Sub() g.Flush())
        Call step_("fillrect", Sub() g.FillRectangle(New SolidBrush(Color.Red), New Rectangle(10, 10, 100, 50)))
        Call step_("fillpolygon", Sub() g.FillPolygon(New SolidBrush(Color.Green), {
            New PointF(120, 20), New PointF(200, 30), New PointF(180, 90), New PointF(130, 80)
        }))
        Call step_("drawpolygon", Sub() g.DrawPolygon(New Pen(Color.Blue, 2), {
            New PointF(220, 20), New PointF(300, 30), New PointF(280, 90), New PointF(230, 80)
        }))
        Call step_("drawline", Sub() g.DrawLine(New Pen(Color.Black, 1), 0, 0, 399, 299))
        Call step_("drawellipse", Sub() g.DrawEllipse(New Pen(Color.Purple, 2), New Rectangle(20, 120, 80, 60)))
        Call step_("readback", Sub() Console.WriteLine("     size = " & g.GetRasterImage().Size.ToString))
        Call step_("save", Sub() g.Save("./dx_smoke.png", ImageFormats.Png))
        Call step_("dispose", Sub() g.Dispose())
    End Sub

    Private Sub step_(name As String, action As Action)
        Try
            Call action()
            Console.WriteLine($" [ok  ] {name}")
        Catch ex As Exception
            Console.WriteLine($" [fail] {name} -> {ex.GetType.Name}: {ex.Message}")
        End Try
    End Sub
End Module
