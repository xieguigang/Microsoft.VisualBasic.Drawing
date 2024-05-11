Imports RoyT.TrueType.IO

Namespace RoyT.TrueType.Tables
    ''' <summary>
    ''' The Vertical Header Table contains information for vertical layout
    ''' </summary>
    Public NotInheritable Class VheaTable
        Public Shared Function FromReader(reader As FontReader, entry As TableRecordEntry) As VheaTable
            reader.Seek(entry.Offset)

            Dim version = reader.ReadFixedBigEndian()

            Dim ascender = reader.ReadInt16BigEndian()
            Dim descender = reader.ReadInt16BigEndian()
            Dim lineGap = reader.ReadInt16BigEndian()
            Dim advanceHeightMax = reader.ReadUInt16BigEndian()
            Dim minTopSideBearing = reader.ReadInt16BigEndian()
            Dim minBottomSideBearing = reader.ReadInt16BigEndian()
            Dim yMaxExtent = reader.ReadInt16BigEndian()
            Dim caretSlopeRise = reader.ReadInt16BigEndian()
            Dim caretSlopeRun = reader.ReadInt16BigEndian()
            Dim caretOffset = reader.ReadInt16BigEndian()

            ' Seek over 4 reserved int16 fields
            reader.Seek(8, SeekOrigin.Current)

            Dim metricDataFormat = reader.ReadInt16BigEndian()
            Dim numberOfVMetrics = reader.ReadUInt16BigEndian()

            Return New VheaTable() With {
    .Version = version,
    .Ascender = ascender,
    .Descender = descender,
    .LineGap = lineGap,
    .AdvanceHeightMax = advanceHeightMax,
    .MinTopSideBearing = minTopSideBearing,
    .MinBottomSideBearing = minBottomSideBearing,
    .YMaxExtent = yMaxExtent,
    .CaretSlopeRise = caretSlopeRise,
    .CaretSlopeRun = caretSlopeRun,
    .CaretOffset = caretOffset,
    .MetricDataFormat = metricDataFormat,
    .NumberOfVMetrics = numberOfVMetrics
}
        End Function

        Public Shared ReadOnly Property Empty As VheaTable
            Get
                Return New VheaTable()
            End Get
        End Property

        ''' <summary>
        ''' Version number of the vertical header table
        ''' </summary>
        Public Property Version As Single

        ''' <summary>
        ''' The vertical typographic ascender for this font. It is the distance in FUnits from the vertical
        ''' center baseline to the right edge of the design space for CJK / ideographic glyphs (or “ideographic em box”).
        ''' 
        ''' It is usually set to(head.unitsPerEm)/2. For example, a font with an em of 1000 FUnits will set this field
        ''' to 500. See the Baseline tags section of the OpenType Layout Tag Registry for a description of the ideographic em-box.
        ''' </summary>
        ''' <remarks>
        ''' Named vertTypoAscender in table version 1.1
        ''' </remarks>
        Public Property Ascender As Short

        ''' <summary>
        ''' The vertical typographic descender for this font. It is the distance in FUnits from the vertical center
        ''' baseline to the left edge of the design space for CJK / ideographic glyphs.
        ''' 
        ''' It is usually set to -(head.unitsPerEm/2). For example, a font with an em of 1000 FUnits will set this field to -500.
        ''' </summary>
        ''' <remarks>
        ''' Named vertTypoDescender in table version 1.1
        ''' </remarks>
        Public Property Descender As Short

        ''' <summary>
        ''' The vertical typographic gap for this font. An application can determine the recommended line spacing for single
        ''' spaced vertical text for an OpenType font by the following expression: ideo embox width + vhea.vertTypoLineGap
        ''' </summary>
        ''' <remarks>
        ''' Named vertTypoLineGap in table version 1.1
        ''' </remarks>
        Public Property LineGap As Short

        ''' <summary>
        ''' Maximum advance height measurement in FUnits.
        ''' </summary>
        Public Property AdvanceHeightMax As UShort

        ''' <summary>
        ''' Minimum top sidebearing value in FUnits.
        ''' </summary>
        Public Property MinTopSideBearing As Short

        ''' <summary>
        ''' Minimum bottom sidebearing value in FUnits.
        ''' </summary>
        Public Property MinBottomSideBearing As Short

        ''' <summary>
        ''' Defined as yMaxExtent = max(tsb + (yMax - yMin)).
        ''' </summary>
        Public Property YMaxExtent As Short

        ''' <summary>
        ''' The value of the caretSlopeRise field divided by the value of the caretSlopeRun Field 
        ''' determines the slope of the caret. A value of 0 for the rise and a value of 1 for the
        ''' run specifies a horizontal caret. A value of 1 for the rise and a value of 0 for the
        ''' run specifies a vertical caret. Intermediate values are desirable for fonts whose glyphs
        ''' are oblique or italic. For a vertical font, a horizontal caret is best.
        ''' </summary>
        Public Property CaretSlopeRise As Short

        ''' <summary>
        '''  See the caretSlopeRise field. Value=1 for nonslanted vertical fonts.
        ''' </summary>
        Public Property CaretSlopeRun As Short

        ''' <summary>
        ''' The amount by which the highlight on a slanted glyph needs to be shifted away from the 
        ''' glyph in order to produce the best appearance. Set value equal to 0 for nonslanted fonts.
        ''' </summary>
        Public Property CaretOffset As Short

        ''' <summary>
        ''' 0 for current format.
        ''' </summary>
        Public Property MetricDataFormat As Short

        ''' <summary>
        ''' Number of advance heights in the 'vmtx' table
        ''' </summary>
        Public Property NumberOfVMetrics As UShort
    End Class
End Namespace
