Imports System
Imports IO

Namespace Tables
    ''' <summary>
    ''' Contains the offset in the file to the major tables (like CMAP, NAME, KERN, etc...) in this font
    ''' </summary>
    Public NotInheritable Class OffsetTable
        Private Shared WindowsAdobe As UInteger = &H00010000UI
        Private Shared OTTO As UInteger = &H4F54544FUI

        ' The Apple specification for TrueType fonts allows for 'true' and 'typ1' for sfnt version. These version tags should not be used for OpenType fonts.
        Private Shared Mac As UInteger = &H74727565UI
        Private Shared Typ1 As UInteger = &H74797031UI

        Public Shared Function FromReader(reader As FontReader) As OffsetTable
            Dim scalarType = reader.ReadUInt32BigEndian() 'reader.ReadFixedBigEndian(out short major, out short minor);

            If scalarType = WindowsAdobe OrElse scalarType = OTTO OrElse scalarType = Typ1 OrElse scalarType = Mac Then
                Dim tables = reader.ReadUInt16BigEndian()
                Dim searchRange = reader.ReadUInt16BigEndian()
                Dim entrySelector = reader.ReadUInt16BigEndian()
                Dim rangeShift = reader.ReadUInt16BigEndian()

                Return New OffsetTable(scalarType, tables, searchRange, entrySelector, rangeShift)
            End If

            Throw New Exception($"Unexpected OffsetTable scalar type. Expected: {WindowsAdobe} or {OTTO}, actual: {scalarType}")
        End Function

        Private Sub New(scalarType As UInteger, tables As UShort, searchRange As UShort, entrySelector As UShort, rangeShift As UShort)
            Me.ScalarType = scalarType
            Me.Tables = tables
            Me.SearchRange = searchRange
            Me.EntrySelector = entrySelector
            Me.RangeShift = rangeShift
        End Sub

        Public ReadOnly Property ScalarType As UInteger
        Public ReadOnly Property Tables As UShort
        Public ReadOnly Property SearchRange As UShort
        Public ReadOnly Property EntrySelector As UShort
        Public ReadOnly Property RangeShift As UShort

        Public Overrides Function ToString() As String
            Return $"Tables: {Tables}"
        End Function
    End Class
End Namespace
