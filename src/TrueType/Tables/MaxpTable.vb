Imports RoyT.TrueType.IO

Namespace RoyT.TrueType.Tables
    ''' <summary>
    ''' Maximum Profile Table
    ''' </summary>
    Public NotInheritable Class MaxpTable
        Public Shared Function FromReader(reader As FontReader, entry As TableRecordEntry) As MaxpTable
            reader.Seek(entry.Offset)

            Dim version = reader.ReadFixedBigEndian()
            Dim numGlyphcs As UShort = reader.ReadUInt16BigEndian()

            If version = 0.5F Then
                Return
            End If

            Return
        End Function

        Public Property Version As Single

        ''' <summary>
        ''' The number of glyphs in the font.
        ''' </summary>
        Public Property NumGlyphs As UShort

        ''' <summary>
        ''' Maximum points in a non-composite glyph.
        ''' </summary>
        Public Property MaxPoints As UShort

        ''' <summary>
        ''' Maximum contours in a non-composite glyph.
        ''' </summary>
        Public Property MaxContours As UShort

        ''' <summary>
        ''' Maximum points in a composite glyph.
        ''' </summary>
        Public Property MaxCompositePoints As UShort

        ''' <summary>
        ''' Maximum contours in a composite glyph.
        ''' </summary>
        Public Property MaxCompositeContours As UShort

        ''' <summary>
        ''' 1 if instructions do not use the twilight zone (Z0), or 2 if instructions do use Z0; should be set to 2 in most cases.
        ''' </summary>
        Public Property MaxZones As UShort

        ''' <summary>
        ''' Maximum points used in Z0.
        ''' </summary>
        Public Property MaxTwilightPoints As UShort

        ''' <summary>
        ''' Number of Storage Area locations.
        ''' </summary>
        Public Property MaxStorage As UShort

        ''' <summary>
        ''' Number of FDEFs, equal to the highest function number + 1.
        ''' </summary>
        Public Property MaxFunctionDefs As UShort

        ''' <summary>
        ''' Number of IDEFs.
        ''' </summary>
        Public Property MaxInstructionDefs As UShort

        ''' <summary>
        ''' Maximum stack depth across Font Program ('fpgm' table), CVT Program('prep' table) and all glyph instructions(in the 'glyf' table).
        ''' </summary>
        Public Property MaxStackElements As UShort

        ''' <summary>
        ''' Maximum byte count for glyph instructions.
        ''' </summary>
        Public Property MaxSizeOfInstructions As UShort

        ''' <summary>
        ''' Maximum number of components referenced at “top level” for any composite glyph.
        ''' </summary>
        Public Property MaxComponentElements As UShort

        ''' <summary>
        ''' Maximum levels of recursion; 1 for simple components.
        ''' </summary>
        Public Property MaxComponentDepth As UShort
    End Class
End Namespace
