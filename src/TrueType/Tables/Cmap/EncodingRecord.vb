Imports RoyT.TrueType.IO
Imports System.Runtime.InteropServices

Namespace RoyT.TrueType.Tables.Cmap
    Public NotInheritable Class EncodingRecord
        Public Shared Function FromReader(reader As FontReader, cmapOffset As Long) As EncodingRecord
            Dim platformId = CType(reader.ReadUInt16BigEndian(), Platform)
            Dim encodingId = CType(reader.ReadUInt16BigEndian(), WindowsEncoding)
            Dim offset = reader.ReadUInt32BigEndian()

            Dim lastPosition = reader.Position
            reader.Seek(cmapOffset + offset)
            Dim format As CMapSubtableFormat = Nothing
            Dim subTable = ReadSubtable(reader, format)
            reader.Seek(lastPosition)

            Return New EncodingRecord(platformId, encodingId, offset, subTable, format)
        End Function

        Private Shared Function ReadSubtable(reader As FontReader, <Out> ByRef format As CMapSubtableFormat) As ICmapSubtable
            Dim start = reader.Position
            format = CType(reader.ReadUInt16BigEndian(), CMapSubtableFormat)
            reader.Seek(start)
            Select Case format
                ' Ordered from most to least used, on the Windows platform                
                Case CMapSubtableFormat.SegmentMappingToDeltaValues ' Format 4
                    Return SegmentMappingToDeltaValuesTable.FromReader(reader)

                Case CMapSubtableFormat.TrimmedTableMapping ' Format 6
                    Return TrimmedTableMappingTable.FromReader(reader)


                Case CMapSubtableFormat.SegmentedCoverage  ' Format 12
                    Return SegmentedCoverageTable.FromReader(reader)

                Case CMapSubtableFormat.ByteEncodingTable ' Format 0
                    Return ByteEncodingTable.FromReader(reader)

                ' Used for specifying variations of the same glyph in a single font
                Case CMapSubtableFormat.UnicodeVariationSequences ' Format 14
                    Return Nothing

                ' The following formats are not used by any of the default fonts in Windows 10
                Case CMapSubtableFormat.HighByteMappingThroughTable
                    Return Nothing

                Case CMapSubtableFormat.MixedCoverage
                    Return Nothing

                Case CMapSubtableFormat.TrimmedArray
                    Return Nothing

                Case CMapSubtableFormat.ManyToOneRangeMappings
                    Return Nothing
                Case Else
                    Return Nothing
            End Select
        End Function

        Private Sub New(platformId As Platform, encodingId As WindowsEncoding, offset As UInteger, subtable As ICmapSubtable, format As CMapSubtableFormat)
            Me.PlatformId = platformId
            WindowsEncodingId = encodingId
            Me.Offset = offset
            Me.Subtable = subtable
            Me.Format = format
        End Sub

        Public ReadOnly Property PlatformId As Platform
        ''' <summary>
        ''' Encoding, only valid if platform is Windows
        ''' </summary>
        Public ReadOnly Property WindowsEncodingId As WindowsEncoding
        Public ReadOnly Property Offset As UInteger
        Public ReadOnly Property Subtable As ICmapSubtable
        Public ReadOnly Property Format As CMapSubtableFormat
    End Class
End Namespace
