Imports RoyT.TrueType.IO

Namespace RoyT.TrueType.Tables
    Public NotInheritable Class TableRecordEntry
        Public Shared Function FromReader(reader As FontReader) As TableRecordEntry
            Dim tag = reader.ReadAscii(4)
            Dim checksum = reader.ReadUInt32BigEndian()
            Dim offset = reader.ReadUInt32BigEndian()
            Dim length = reader.ReadUInt32BigEndian()

            Return New TableRecordEntry(tag, checksum, offset, length)
        End Function


        Private Sub New(tag As String, checksum As UInteger, offset As UInteger, length As UInteger)
            Me.Tag = tag
            Me.Checksum = checksum
            Me.Offset = offset
            Me.Length = length
        End Sub

        Public ReadOnly Property Tag As String
        Public ReadOnly Property Checksum As UInteger
        Public ReadOnly Property Offset As UInteger
        Public ReadOnly Property Length As UInteger


        Public Overrides Function ToString() As String
            Return Tag
        End Function
    End Class
End Namespace
