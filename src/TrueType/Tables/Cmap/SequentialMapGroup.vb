Imports Microsoft.VisualBasic.Drawing.Fonts.TrueType.IO

Namespace Tables.Cmap
    Public NotInheritable Class SequentialMapGroup
        Public Shared Function FromReader(reader As FontReader) As SequentialMapGroup
            Dim startCharCode = reader.ReadUInt32BigEndian()
            Dim endCharCode = reader.ReadUInt32BigEndian()
            Dim startGlyphId = reader.ReadUInt32BigEndian()

            Return New SequentialMapGroup(startCharCode, endCharCode, startGlyphId)
        End Function

        Public Sub New(startCharCode As UInteger, endCharCode As UInteger, startGlyphId As UInteger)
            Me.StartCharCode = startCharCode
            Me.EndCharCode = endCharCode
            Me.StartGlyphId = startGlyphId
        End Sub

        Public ReadOnly Property StartCharCode As UInteger
        Public ReadOnly Property EndCharCode As UInteger
        Public ReadOnly Property StartGlyphId As UInteger
    End Class
End Namespace
