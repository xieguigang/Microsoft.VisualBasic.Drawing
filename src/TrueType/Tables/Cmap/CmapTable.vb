Imports Microsoft.VisualBasic.Drawing.Fonts.TrueType.IO

Namespace Tables.Cmap
    Public NotInheritable Class CmapTable
        ''' <summary>
        ''' Contains information to get the glyph that corresponds to each supported character
        ''' </summary>
        Public Shared Function FromReader(reader As FontReader) As CmapTable
            Dim cmapOffset = reader.Position
            Dim version = reader.ReadUInt16BigEndian()
            If version <> 0 Then
                Throw New Exception($"Unexpected Cmap tab;e version. Expected: 0, actual: {version}")
            End If

            Dim tables = reader.ReadUInt16BigEndian()

            Dim encodingRecords = New EncodingRecord(tables - 1) {}
            For i = 0 To tables - 1
                encodingRecords(i) = EncodingRecord.FromReader(reader, cmapOffset)
            Next

            Return New CmapTable(version, encodingRecords)
        End Function

        Private Sub New(version As UShort, encodingRecords As IReadOnlyList(Of EncodingRecord))
            Me.Version = version
            Me.EncodingRecords = encodingRecords
        End Sub

        Public ReadOnly Property Version As UShort
        Public ReadOnly Property EncodingRecords As IReadOnlyList(Of EncodingRecord)
    End Class
End Namespace
