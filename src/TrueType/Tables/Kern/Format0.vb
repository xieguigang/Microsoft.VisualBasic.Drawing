Imports Microsoft.VisualBasic.Drawing.Fonts.TrueType.IO

Namespace Tables.Kern
    Public NotInheritable Class Format0
        Public Shared Function FromReader(reader As FontReader) As Format0
            Dim pairs = reader.ReadUInt16BigEndian()
            Dim searchRange = reader.ReadUInt16BigEndian()
            Dim entrySelector = reader.ReadUInt16BigEndian()
            Dim rangeShift = reader.ReadUInt16BigEndian()

            Dim map = New Dictionary(Of KerningPair, Short)()
            For i = 0 To pairs - 1
                Dim left = reader.ReadUInt16BigEndian()
                Dim right = reader.ReadUInt16BigEndian()
                Dim value = reader.ReadInt16BigEndian()

                map.Add(New KerningPair(left, right), value)
            Next

            Return New Format0(pairs, searchRange, entrySelector, rangeShift, map)
        End Function

        Private Sub New(pairs As UShort, searchRange As UShort, entrySelector As UShort, rangeShift As UShort, map As Dictionary(Of KerningPair, Short))
            Me.Pairs = pairs
            Me.SearchRange = searchRange
            Me.EntrySelector = entrySelector
            Me.RangeShift = rangeShift
            Me.Map = map
        End Sub

        Public ReadOnly Property Pairs As UShort
        Public ReadOnly Property SearchRange As UShort
        Public ReadOnly Property EntrySelector As UShort
        Public ReadOnly Property RangeShift As UShort
        Public ReadOnly Property Map As IReadOnlyDictionary(Of KerningPair, Short)
    End Class
End Namespace
