Imports Microsoft.VisualBasic.Drawing.Fonts.TrueType.IO

Namespace Tables.Cmap
    Public NotInheritable Class ByteEncodingTable
        Implements ICmapSubtable
        Public Shared Function FromReader(reader As FontReader) As ByteEncodingTable
            Dim format = reader.ReadUInt16BigEndian()
            Dim length = reader.ReadUInt16BigEndian()
            Dim language = reader.ReadUInt16BigEndian()
            Dim ids = New Byte(255) {}

            For i = 0 To ids.Length - 1
                ids(i) = reader.ReadByte()
            Next

            Return New ByteEncodingTable(format, length, language, ids)
        End Function

        Private Sub New(format As UShort, length As UShort, language As UShort, ids As Byte())
            Me.Format = format
            Me.Length = length
            Me.Language = language
            Me.Ids = ids
        End Sub

        Public ReadOnly Property Format As UShort
        Public ReadOnly Property Length As UShort
        Public ReadOnly Property Language As UShort
        Public ReadOnly Property Ids As Byte()

        Public Function GetGlyphIndex(c As Char) As UInteger Implements ICmapSubtable.GetGlyphIndex
            Dim charCode = Strings.AscW(c)
            Dim ci32 As Integer = charCode

            If ci32 >= 0 AndAlso ci32 < Ids.Length Then
                Return Ids(charCode)
            End If

            Return 0
        End Function
    End Class
End Namespace
