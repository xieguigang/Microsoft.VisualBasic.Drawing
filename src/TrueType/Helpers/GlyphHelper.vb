Imports System.Linq

Namespace RoyT.TrueType.Helpers
    Public Module GlyphHelper
        ''' <summary>
        ''' Returns the glyph index for the given character, or 0 if the character is not supported by this font
        ''' </summary>        
        Public Function GetGlyphIndex(c As Char, font As TrueTypeFont) As UInteger
            Dim glyphIndex As UInteger = 0

            ' Prefer Windows platform UCS2 glyphs as they are the recommended default on the Windows platform
            Dim preferred = font.CmapTable.EncodingRecords.FirstOrDefault(Function(e) e.PlatformId = Platform.Windows AndAlso e.WindowsEncodingId = WindowsEncoding.UnicodeUCS2)


            If preferred IsNot Nothing Then
                glyphIndex = preferred.Subtable.GetGlyphIndex(c)
            End If

            If glyphIndex <> 0 Then
                Return glyphIndex
            End If

            ' Fall back to using any table to find the match
            For Each record In font.CmapTable.EncodingRecords
                glyphIndex = record.Subtable.GetGlyphIndex(c)
                If glyphIndex <> 0 Then
                    Return glyphIndex
                End If
            Next

            Return 0
        End Function
    End Module

End Namespace
