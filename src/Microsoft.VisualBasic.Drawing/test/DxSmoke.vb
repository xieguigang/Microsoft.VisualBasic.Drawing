Imports System.Drawing
Imports Microsoft.VisualBasic.Drawing.DirectX
Imports Microsoft.VisualBasic.Imaging

Imports Font = Microsoft.VisualBasic.Imaging.Font
Imports Pen = Microsoft.VisualBasic.Imaging.Pen
Imports SolidBrush = Microsoft.VisualBasic.Imaging.SolidBrush

Module DxSmoke

    Sub Smoke()
        Console.WriteLine("[1] create canvas ...")

        Dim g As New DxGraphics(400, 300, "#ffffff")

        Console.WriteLine("[2] canvas created, device = " & g.DeviceDescription)

        Call step_("pixel background", Sub() Console.WriteLine("     (200,150) = " & g.GetRasterImage().GetPixel(200, 150).ToString))
        Call step_("fillrect", Sub() g.FillRectangle(New SolidBrush(Color.Red), New Rectangle(10, 10, 100, 50)))
        Call step_("fillpolygon", Sub() g.FillPolygon(New SolidBrush(Color.Green), {
            New PointF(120, 20), New PointF(200, 30), New PointF(180, 90), New PointF(130, 80)
        }))
        Call step_("drawpolygon", Sub() g.DrawPolygon(New Pen(Color.Blue, 2), {
            New PointF(220, 20), New PointF(300, 30), New PointF(280, 90), New PointF(230, 80)
        }))
        Call step_("drawline", Sub() g.DrawLine(New Pen(Color.Black, 1), 0, 0, 399, 299))
        Call step_("drawellipse", Sub() g.DrawEllipse(New Pen(Color.Purple, 2), New Rectangle(20, 120, 80, 60)))
        Call step_("fillellipse", Sub() g.FillEllipse(New SolidBrush(Color.Orange), New Rectangle(120, 120, 80, 60)))
        Call step_("drawstring", Sub() g.DrawString("DirectX 2D", New Font(FontFace.Consolas, 20), New SolidBrush(Color.Black), 20.0F, 220.0F))
        Call step_("setclip", Sub() g.SetClip(New Rectangle(0, 0, 400, 300)))
        Call step_("resetclip", Sub() g.ResetClip())
        Call step_("translate", Sub() g.TranslateTransform(0, 0))
        Call step_("flush", Sub() g.Flush())
        Call step_("pixel after fill", Sub() Console.WriteLine("     (50,30) = " & g.GetRasterImage().GetPixel(50, 30).ToString))
        Call step_("clear", Sub() g.Clear(Color.White))
        Call step_("pixel after clear", Sub() Console.WriteLine("     (50,30) = " & g.GetRasterImage().GetPixel(50, 30).ToString))
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
