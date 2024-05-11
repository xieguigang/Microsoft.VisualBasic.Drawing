Imports Microsoft.VisualBasic.Drawing.Fonts.TrueType.IO

Namespace Tables
    ''' <summary>
    ''' Font Header Table
    ''' </summary>
    Public Structure HeadTable
        Public Shared Function FromReader(reader As FontReader, entry As TableRecordEntry) As HeadTable
            reader.Seek(entry.Offset)

            Dim major = reader.ReadUInt16BigEndian()
            Dim minor = reader.ReadUInt16BigEndian()

            Return New HeadTable() With {
    .MajorVersion = major,
    .MinorVersion = minor,
    .FontRevision = reader.ReadFixedBigEndian(),
    .ChecksumAdjustment = reader.ReadUInt32BigEndian(),
    .MagicNumber = reader.ReadUInt32BigEndian(),
    .Flags = reader.ReadUInt16BigEndian(),
    .UnitsPerEm = reader.ReadUInt16BigEndian(),
    .Created = reader.ReadInt64BigEndian(),
    .Modified = reader.ReadInt64BigEndian(),
    .XMin = reader.ReadInt16BigEndian(),
    .XMax = reader.ReadInt16BigEndian(),
    .YMin = reader.ReadInt16BigEndian(),
    .YMax = reader.ReadInt16BigEndian(),
    .MacStyle = reader.ReadUInt16BigEndian(),
    .LowestRecPPEM = reader.ReadUInt16BigEndian(),
    .FontDirectionHint = reader.ReadInt16BigEndian(),
    .IndexToLocFormat = reader.ReadInt16BigEndian(),
    .GlyphDataFormat = reader.ReadInt16BigEndian()
}
        End Function

        ' Major version number of the font header table — set to 1.
        Public Property MajorVersion As UShort

        ' Minor version number of the font header table — set to 0.
        Public Property MinorVersion As UShort

        ' Set by font manufacturer.
        Public Property FontRevision As Single

        Public Property ChecksumAdjustment As UInteger

        ''' <summary>
        ''' Set to 0x5F0F3CF5.
        ''' </summary>
        Public Property MagicNumber As UInteger

        Public Property Flags As UShort

        ''' <summary>
        ''' Set to a value from 16 to 16384.Any value in this range is valid. In fonts that have TrueType outlines, a power of 2 is recommended as this allows performance optimizations in some rasterizers.
        ''' </summary>
        Public Property UnitsPerEm As UShort

        ''' <summary>
        ''' Number of seconds since 12:00 midnight that started January 1st 1904 in GMT / UTC time zone.
        ''' </summary>
        Public Property Created As Long

        ''' <summary>
        ''' Number of seconds since 12:00 midnight that started January 1st 1904 in GMT / UTC time zone.
        ''' </summary>
        Public Property Modified As Long

        ''' <summary>
        ''' Minimum x coordinate across all glyph bounding boxes.
        ''' </summary>
        Public Property XMin As Short

        ''' <summary>
        ''' Minimum y coordinate across all glyph bounding boxes.
        ''' </summary>
        Public Property YMin As Short

        ''' <summary>
        ''' Maximum x coordinate across all glyph bounding boxes.
        ''' </summary>
        Public Property XMax As Short

        ''' <summary>
        ''' Maximum y coordinate across all glyph bounding boxes.
        ''' </summary>
        Public Property YMax As Short

        Public Property MacStyle As UShort

        ''' <summary>
        ''' Smallest readable size in pixels.
        ''' </summary>
        Public Property LowestRecPPEM As UShort

        Public Property FontDirectionHint As Short
        Public Property IndexToLocFormat As Short
        Public Property GlyphDataFormat As Short
    End Structure
End Namespace
