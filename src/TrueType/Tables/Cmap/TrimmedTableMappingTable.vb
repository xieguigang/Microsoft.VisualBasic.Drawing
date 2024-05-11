Imports RoyT.TrueType.IO

Namespace RoyT.TrueType.Tables.Cmap
    Public NotInheritable Class TrimmedTableMappingTable
        Implements ICmapSubtable
        Public Shared Function FromReader(reader As FontReader) As TrimmedTableMappingTable
            Dim format = reader.ReadUInt16BigEndian()
            Dim length = reader.ReadUInt16BigEndian()
            Dim language = reader.ReadUInt16BigEndian()
            Dim firstCode = reader.ReadUInt16BigEndian()
            Dim entryCount = reader.ReadUInt16BigEndian()

            Dim glyphIdArray = New UShort(entryCount - 1) {}
            For i = 0 To glyphIdArray.Length - 1
                glyphIdArray(i) = reader.ReadUInt16BigEndian()
            Next

            Return New TrimmedTableMappingTable(format, length, language, firstCode, entryCount, glyphIdArray)
        End Function

        Public Sub New(format As UShort, length As UShort, language As UShort, firstCode As UShort, entryCount As UShort, glyphIdArray As UShort())
            Me.Format = format
            Me.Length = length
            Me.Language = language
            Me.FirstCode = firstCode
            Me.EntryCount = entryCount
            Me.GlyphIdArray = glyphIdArray
        End Sub


        Public ReadOnly Property Format As UShort
        Public ReadOnly Property Length As UShort
        Public ReadOnly Property Language As UShort
        Public ReadOnly Property FirstCode As UShort
        Public ReadOnly Property EntryCount As UShort
        Public ReadOnly Property GlyphIdArray As UShort()


        Public Function GetGlyphIndex(c As Char) As UInteger Implements ICmapSubtable.GetGlyphIndex
            Dim charCode = Microsoft.VisualBasic.AscW(c)

            If FirstCode <= charCode AndAlso charCode < FirstCode + EntryCount Then
                Return GlyphIdArray(charCode - FirstCode)
            End If

            Return 0
        End Function
    End Class
End Namespace
