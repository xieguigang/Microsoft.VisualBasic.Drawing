Imports Tables.Kern

Namespace Helpers
    Public Module KerningHelper
        ''' <summary>
        ''' Returns the horizontal kerning between the left and right character scaled by the scale parameter
        ''' or 0 if no kerning information exists for this pair of characters
        ''' </summary>        
        Public Function GetHorizontalKerning(left As Char, right As Char, font As TrueTypeFont) As Single

            Dim value As Short = Nothing
            If font.KernTable.SubtableCount > 0 Then
                Dim leftCode = GetGlyphIndex(left, font)
                Dim rightCode = GetGlyphIndex(right, font)

                For Each subTable In font.KernTable.Subtables
                    If subTable.Format0 IsNot Nothing AndAlso subTable.Direction = Direction.Horizontal AndAlso subTable.Values = Values.Kerning Then
                        Dim pair = New KerningPair(leftCode, rightCode)

                        If subTable.Format0.Map.TryGetValue(pair, value) Then
                            Return value
                        End If
                    End If
                Next
            End If

            Return 0.0F
        End Function
    End Module
End Namespace
