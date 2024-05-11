Imports System.Collections.Generic
Imports RoyT.TrueType.IO

Namespace RoyT.TrueType.Tables.Cmap
    Public NotInheritable Class SegmentedCoverageTable
        Implements ICmapSubtable
        Public Shared Function FromReader(reader As FontReader) As SegmentedCoverageTable
            Dim format = reader.ReadUInt16BigEndian()
            Dim reserved = reader.ReadUInt16BigEndian()
            Dim length = reader.ReadUInt32BigEndian()
            Dim language = reader.ReadUInt32BigEndian()
            Dim numGroups = reader.ReadUInt32BigEndian()

            Dim groups = New SequentialMapGroup(numGroups - 1) {}

            For i = 0 To groups.Length - 1
                groups(i) = SequentialMapGroup.FromReader(reader)
            Next

            Return New SegmentedCoverageTable(format, reserved, length, language, numGroups, groups)
        End Function

        Private Sub New(format As UShort, reserved As UShort, length As UInteger, language As UInteger, numGroups As UInteger, groups As IReadOnlyList(Of SequentialMapGroup))
            Me.Format = format
            Me.Reserved = reserved
            Me.Length = length
            Me.Language = language
            Me.NumGroups = numGroups
            Me.Groups = groups
        End Sub

        Public ReadOnly Property Format As UShort
        Public ReadOnly Property Reserved As UShort
        Public ReadOnly Property Length As UInteger
        Public ReadOnly Property Language As UInteger
        Public ReadOnly Property NumGroups As UInteger
        Public ReadOnly Property Groups As IReadOnlyList(Of SequentialMapGroup)


        Public Function GetGlyphIndex(c As Char) As UInteger Implements ICmapSubtable.GetGlyphIndex
            Dim charCode = Microsoft.VisualBasic.AscW(c)

            For Each group In Groups
                If group.StartCharCode <= charCode AndAlso charCode <= group.EndCharCode Then
                    Return charCode - group.StartCharCode + group.StartGlyphId
                End If
            Next

            Return 0
        End Function
    End Class
End Namespace
