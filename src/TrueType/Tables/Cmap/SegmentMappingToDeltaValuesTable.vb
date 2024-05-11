Imports Microsoft.VisualBasic.ApplicationServices.Debugging
Imports Microsoft.VisualBasic.Drawing.Fonts.TrueType.IO

Namespace Tables.Cmap
    Public NotInheritable Class SegmentMappingToDeltaValuesTable
        Implements ICmapSubtable
        Public Shared Function FromReader(reader As FontReader) As SegmentMappingToDeltaValuesTable
            Dim start = reader.Position

            Dim format = reader.ReadUInt16BigEndian()
            Dim length = reader.ReadUInt16BigEndian()
            Dim language = reader.ReadUInt16BigEndian()

            Dim segCountX2 = reader.ReadUInt16BigEndian()
            Dim searchRange = reader.ReadUInt16BigEndian()
            Dim entrySelector = reader.ReadUInt16BigEndian()
            Dim rangeShift = reader.ReadUInt16BigEndian()


            Dim segCount = segCountX2 / 2
            Dim endCount = New UShort(segCount - 1) {}
            For i = 0 To endCount.Length - 1
                endCount(i) = reader.ReadUInt16BigEndian()
            Next

            Dim reservedPad = reader.ReadUInt16BigEndian()

            Dim startCount = New UShort(segCount - 1) {}
            For i = 0 To startCount.Length - 1
                startCount(i) = reader.ReadUInt16BigEndian()
            Next

            Dim idDelta = New Short(segCount - 1) {}
            For i = 0 To idDelta.Length - 1
                idDelta(i) = reader.ReadInt16BigEndian()
            Next

            Dim idRangeOffset = New UShort(segCount - 1) {}
            For i = 0 To idRangeOffset.Length - 1
                idRangeOffset(i) = reader.ReadUInt16BigEndian()
            Next

            Dim read = reader.Position - start
            Dim glyphCount = (length - read) / HeapSizeOf.ushort
            Dim glyphIdArray = New UShort(glyphCount - 1) {}
            For i = 0 To glyphIdArray.Length - 1
                glyphIdArray(i) = reader.ReadUInt16BigEndian()
            Next

            Return New SegmentMappingToDeltaValuesTable(format, reservedPad, length, language, segCount, searchRange, entrySelector, rangeShift, endCount, startCount, idDelta, idRangeOffset, glyphCount, glyphIdArray)
        End Function

        Public Sub New(format As UShort, reservedPad As UShort, length As UShort, language As UShort, segCount As Integer, searchRange As UShort, entrySelector As UShort, rangeShift As UShort, endCount As UShort(), startCount As UShort(), idDelta As Short(), idRangeOffset As UShort(), glyphCount As Long, glyphIdArray As UShort())
            Me.Format = format
            Me.ReservedPad = reservedPad
            Me.Length = length
            Me.Language = language
            Me.SegCount = segCount
            Me.SearchRange = searchRange
            Me.EntrySelector = entrySelector
            Me.RangeShift = rangeShift
            Me.EndCount = endCount
            Me.StartCount = startCount
            Me.IdDelta = idDelta
            Me.IdRangeOffset = idRangeOffset
            Me.GlyphCount = glyphCount
            Me.GlyphIdArray = glyphIdArray
        End Sub

        Public ReadOnly Property Format As UShort
        Public ReadOnly Property ReservedPad As UShort
        Public ReadOnly Property Length As UShort
        Public ReadOnly Property Language As UShort
        Public ReadOnly Property SegCount As Integer
        Public ReadOnly Property SearchRange As UShort
        Public ReadOnly Property EntrySelector As UShort
        Public ReadOnly Property RangeShift As UShort
        Public ReadOnly Property EndCount As UShort()
        Public ReadOnly Property StartCount As UShort()
        Public ReadOnly Property IdDelta As Short()
        Public ReadOnly Property IdRangeOffset As UShort()
        Public ReadOnly Property GlyphCount As Long
        Public ReadOnly Property GlyphIdArray As UShort()

        Public Function GetGlyphIndex(c As Char) As UInteger Implements ICmapSubtable.GetGlyphIndex
            Dim charCode = Microsoft.VisualBasic.AscW(c)

            ' Find the first segment that fits the charcode
            For i = 0 To SegCount - 1
                Dim start = StartCount(i)
                Dim [end] = EndCount(i)

                If start <= charCode AndAlso [end] >= charCode Then
                    Dim delta = IdDelta(i)
                    Dim rangeOffset = IdRangeOffset(i)

                    If rangeOffset = 0 Then
                        Return CUShort((delta + c) Mod 65536)
                    End If

                    ' Index depends on the position of rangeOffset in the IdRangeOffset array
                    Dim index = rangeOffset + (c - start) + i
                    Dim glyphId = GlyphIdArray(index)

                    If glyphId <> 0 Then
                        Return CUShort((glyphId + c) Mod 65536)
                    End If
                End If
            Next

            Return 0
        End Function
    End Class
End Namespace
