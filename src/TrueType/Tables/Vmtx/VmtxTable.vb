Imports Microsoft.VisualBasic.Drawing.Fonts.TrueType.IO

Namespace Tables.Vmtx
    ''' <summary>
    ''' Vertical Metrics Table
    ''' </summary>
    Public NotInheritable Class VmtxTable
        Public Shared Function FromReader(reader As FontReader, entry As TableRecordEntry, metricsCount As Integer, glyphCount As Integer) As VmtxTable
            reader.Seek(entry.Offset)

            Dim topSideBearingCount = glyphCount - metricsCount
            If entry.Length <> metricsCount * 4 + topSideBearingCount * 2 Then
                Throw New Exception("unexpected vmtx table length")
            End If

            Dim vmetrics As List(Of LongVerMetric) = New List(Of LongVerMetric)(metricsCount)
            For i = 0 To metricsCount - 1
                vmetrics.Add(LongVerMetric.FromReader(reader))
            Next

            Dim topSideBearings As List(Of Short) = New List(Of Short)(topSideBearingCount)
            For i = 0 To topSideBearingCount - 1
                topSideBearings.Add(reader.ReadInt16BigEndian())
            Next

            Return New VmtxTable() With {
    .VMetrics = vmetrics,
    .TopSideBearings = topSideBearings
}
        End Function

        Public Shared ReadOnly Property Empty As VmtxTable
            Get
                Return New VmtxTable() With {
                    .VMetrics = New List(Of LongVerMetric)()
                }
            End Get
        End Property

        ''' <summary>
        ''' Paired advance width and left side bearing values for each glyph. Records are indexed by glyph ID.
        ''' </summary>
        Public Property VMetrics As IReadOnlyList(Of LongVerMetric)

        ''' <summary>
        ''' Optional array for glyphs not in VMetrics. Their TopSideBearing is equal to the TopSideBearing
        ''' of the last entry in VMetrics
        ''' </summary>
        Public Property TopSideBearings As IReadOnlyList(Of Short)
    End Class
End Namespace
