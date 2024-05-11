Imports Microsoft.VisualBasic.Drawing.Fonts.TrueType.IO

Namespace Tables.Hmtx
    ''' <summary>
    ''' Horizontal Metrics Table
    ''' </summary>
    Public NotInheritable Class HmtxTable
        Public Shared Function FromReader(reader As FontReader, entry As TableRecordEntry, metricsCount As Integer, glyphCount As Integer) As HmtxTable
            reader.Seek(entry.Offset)

            Dim leftSideBearingCount = glyphCount - metricsCount
            Dim expected = 4 * metricsCount + 2 * leftSideBearingCount
            If entry.Length <> expected Then
                Throw New Exception("unexpected hmtx table length")
            End If

            Dim hmetrics As List(Of LongHorMetric) = New List(Of LongHorMetric)(metricsCount)
            For i = 0 To metricsCount - 1
                hmetrics.Add(LongHorMetric.FromReader(reader))
            Next

            Dim leftSideBearings As List(Of Short) = New List(Of Short)(leftSideBearingCount)
            For i = 0 To leftSideBearingCount - 1
                leftSideBearings.Add(reader.ReadInt16BigEndian())
            Next

            Return New HmtxTable() With {
    .HMetrics = hmetrics,
    .LeftSideBearings = leftSideBearings
}
        End Function

        Public Shared ReadOnly Property Empty As HmtxTable
            Get
                Return New HmtxTable() With {
                    .HMetrics = New List(Of LongHorMetric)()
                }
            End Get
        End Property

        ''' <summary>
        ''' Paired advance width and left side bearing values for each glyph. Records are indexed by glyph ID.
        ''' </summary>
        Public Property HMetrics As IReadOnlyList(Of LongHorMetric)

        ''' <summary>
        ''' Optional array for glyphs not in HMetrics. Their Lsb is equal to the Lsb
        ''' of the last entry in HMetrics
        ''' </summary>
        Public Property LeftSideBearings As IReadOnlyList(Of Short)
    End Class
End Namespace
