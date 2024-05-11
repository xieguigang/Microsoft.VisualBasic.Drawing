Imports RoyT.TrueType.IO

Namespace RoyT.TrueType.Tables.Vmtx
    ''' <summary>
    ''' Horizontal layout metrics for a glyph
    ''' </summary>
    Public Class LongVerMetric
        Public Shared Function FromReader(reader As FontReader) As LongVerMetric
            Return
        End Function

        ''' <summary>
        ''' Advance height, in font design units.
        ''' </summary>
        Public Property AdvanceHeight As UShort

        ''' <summary>
        ''' Glyph left side bearing, in font design units.
        ''' </summary>
        Public Property TopSideBearing As Short
    End Class
End Namespace
