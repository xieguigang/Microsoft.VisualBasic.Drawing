Imports System

Namespace RoyT.TrueType.Tables.Kern
    Public Structure KerningPair
        Implements IEquatable(Of KerningPair)
        Public ReadOnly Property LeftGlyphCode As UShort
        Public ReadOnly Property RightGlyphCode As UShort

        Public Sub New(leftGlyphCode As UShort, rightGlyphCode As UShort)
            Me.LeftGlyphCode = leftGlyphCode
            Me.RightGlyphCode = rightGlyphCode
        End Sub

        Public Overloads Function Equals(other As KerningPair) As Boolean Implements IEquatable(Of KerningPair).Equals
            Return LeftGlyphCode = other.LeftGlyphCode AndAlso RightGlyphCode = other.RightGlyphCode
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Dim pair As KerningPair = Nothing

            If CSharpImpl.__Assign(pair, TryCast(obj, KerningPair)) IsNot Nothing Then
                Return Equals(pair)
            End If

            Return False
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return (LeftGlyphCode.GetHashCode() * 397) Xor RightGlyphCode.GetHashCode()
        End Function

        Private Class CSharpImpl
            <Obsolete("Please refactor calling code to use normal Visual Basic assignment")>
            Shared Function __Assign(Of T)(ByRef target As T, value As T) As T
                target = value
                Return value
            End Function
        End Class
    End Structure
End Namespace
