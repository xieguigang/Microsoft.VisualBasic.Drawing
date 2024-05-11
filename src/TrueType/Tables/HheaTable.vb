Imports Microsoft.VisualBasic.Drawing.Fonts.TrueType.IO

Namespace Tables
    ''' <summary>
    ''' The Horizontal Header Table contains information for horizontal layout
    ''' </summary>
    Public NotInheritable Class HheaTable
        Public Shared Function FromReader(reader As FontReader, entry As TableRecordEntry) As HheaTable
            reader.Seek(entry.Offset)

            Dim majorVersion = reader.ReadUInt16BigEndian()
            Dim minorVersion = reader.ReadUInt16BigEndian()
            Dim ascender = reader.ReadInt16BigEndian()
            Dim descender = reader.ReadInt16BigEndian()
            Dim lineGap = reader.ReadInt16BigEndian()
            Dim advanceWidthMax = reader.ReadUInt16BigEndian()
            Dim minLeftSideBearing = reader.ReadInt16BigEndian()
            Dim minRightSideBearing = reader.ReadInt16BigEndian()
            Dim xMaxExtent = reader.ReadInt16BigEndian()
            Dim caretSlopeRise = reader.ReadInt16BigEndian()
            Dim caretSlopeRun = reader.ReadInt16BigEndian()
            Dim caretOffset = reader.ReadInt16BigEndian()

            ' Seek over 4 reserved int16 fields
            reader.Seek(8, SeekOrigin.Current)

            Dim metricDataFormat = reader.ReadInt16BigEndian()
            Dim numberOfHMetrics = reader.ReadUInt16BigEndian()

            Return New HheaTable() With {
    .MajorVersion = majorVersion,
    .MinorVersion = minorVersion,
    .Ascender = ascender,
    .Descender = descender,
    .LineGap = lineGap,
    .AdvanceWidthMax = advanceWidthMax,
    .MinLeftSideBearing = minLeftSideBearing,
    .MinRightSideBearing = minRightSideBearing,
    .XMaxExtent = xMaxExtent,
    .CaretSlopeRise = caretSlopeRise,
    .CaretSlopeRun = caretSlopeRun,
    .CaretOffset = caretOffset,
    .MetricDataFormat = metricDataFormat,
    .NumberOfHMetrics = numberOfHMetrics
}
        End Function

        Public Shared ReadOnly Property Empty As HheaTable
            Get
                Return New HheaTable()
            End Get
        End Property

        ''' <summary>
        ''' Major version number of the horizontal header table
        ''' </summary>
        Public Property MajorVersion As UShort

        ''' <summary>
        ''' Minor version number of the horizontal header table
        ''' </summary>
        Public Property MinorVersion As UShort

        ''' <summary>
        ''' Typographic ascent—see note below.
        ''' </summary>
        Public Property Ascender As Short

        ''' <summary>
        ''' Typographic descent—see note below.
        ''' </summary>
        Public Property Descender As Short

        ''' <summary>
        ''' Typographic line gap.
        ''' </summary>
        Public Property LineGap As Short

        ''' <summary>
        ''' Maximum advance width value in 'hmtx' table.
        ''' </summary>
        Public Property AdvanceWidthMax As UShort

        ''' <summary>
        ''' Minimum left sidebearing value in 'hmtx' table for glyphs with contours (empty glyphs should be ignored).
        ''' </summary>
        Public Property MinLeftSideBearing As Short

        ''' <summary>
        ''' Minimum right sidebearing value; calculated as min(aw - (lsb + xMax - xMin)) for glyphs with contours(empty glyphs should be ignored).
        ''' </summary>
        Public Property MinRightSideBearing As Short

        ''' <summary>
        ''' Max(lsb + (xMax - xMin)).
        ''' </summary>
        Public Property XMaxExtent As Short

        ''' <summary>
        ''' Used to calculate the slope of the cursor(rise/run); 1 for vertical.
        ''' </summary>
        Public Property CaretSlopeRise As Short

        ''' <summary>
        ''' 0 for vertical.
        ''' </summary>
        Public Property CaretSlopeRun As Short

        ''' <summary>
        ''' The amount by which a slanted highlight on a glyph needs to be shifted to produce the best appearance.Set to 0 for non-slanted fonts
        ''' </summary>
        Public Property CaretOffset As Short

        ''' <summary>
        ''' 0 for current format.
        ''' </summary>
        Public Property MetricDataFormat As Short

        ''' <summary>
        ''' Number of hMetric entries in 'hmtx' table
        ''' </summary>
        Public Property NumberOfHMetrics As UShort
    End Class
End Namespace
