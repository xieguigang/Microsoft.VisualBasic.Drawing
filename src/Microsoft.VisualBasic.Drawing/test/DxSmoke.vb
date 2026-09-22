Imports Microsoft.VisualBasic.Drawing.DirectX

Module DxSmoke

    Sub Smoke()
        Dim argv As String() = Environment.GetCommandLineArgs()
        Dim slot As Integer = 0
        Dim mode As String = "clear"

        For i As Integer = 0 To argv.Length - 1
            If argv(i) = "--slot" AndAlso i + 1 < argv.Length Then
                Integer.TryParse(argv(i + 1), slot)
            ElseIf argv(i) = "--mode" AndAlso i + 1 < argv.Length Then
                mode = argv(i + 1)
            End If
        Next

        If slot > 0 Then
            Console.WriteLine(DxDebugProbe.Probe(slot, mode))
        Else
            Call Console.WriteLine(DxGraphics.DebugScan())
        End If
    End Sub
End Module
