Imports IO

Namespace Tables.Name
    Public NotInheritable Class NameTable
        ''' <summary>
        ''' Contains the (translated) name of this font, copyright notices, etc...
        ''' </summary>
        Public Shared Function FromReader(reader As FontReader) As NameTable
            Dim start = reader.Position

            Dim format = reader.ReadUInt16BigEndian()
            Dim count = reader.ReadUInt16BigEndian()
            Dim stringOffset = reader.ReadUInt16BigEndian()

            Dim nameRecords = New NameRecord(count - 1) {}
            For i = 0 To nameRecords.Length - 1
                nameRecords(i) = NameRecord.FromReader(reader, start + stringOffset)
            Next

            Return New NameTable(format, count, stringOffset, nameRecords)
        End Function

        Public Sub New(format As UShort, count As UShort, stringOffset As UShort, nameRecords As NameRecord())
            Me.Format = format
            Me.Count = count
            Me.StringOffset = stringOffset
            Me.NameRecords = nameRecords
        End Sub

        Public ReadOnly Property Format As UShort
        Public ReadOnly Property Count As UShort
        Public ReadOnly Property StringOffset As UShort
        Public ReadOnly Property NameRecords As NameRecord()
    End Class
End Namespace
