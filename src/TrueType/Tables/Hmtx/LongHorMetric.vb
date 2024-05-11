Imports RoyT.TrueType.IO

Namespace RoyT.TrueType.Tables.Hmtx
    ''' <summary>
    ''' Horizontal layout metrics for a glyph
    ''' </summary>
    Public Class LongHorMetric
        Public Shared Function FromReader(reader As FontReader) As LongHorMetric
            Return
        End Function

        ''' <summary>
        ''' Advance width, in font design units.
        ''' </summary>
        Public Property AdvanceHeight As UShort

        ''' <summary>
        ''' Glyph left side bearing, in font design units.
        ''' </summary>
        Public Property Lsb As Short
    End Class
End Namespace
