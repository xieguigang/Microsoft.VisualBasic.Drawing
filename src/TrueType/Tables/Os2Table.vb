Imports RoyT.TrueType.IO
Imports System.Collections.Generic

Namespace RoyT.TrueType.Tables
    ''' <summary>
    ''' OS/2 and Windows Metrics Table
    ''' </summary>
    Public Class Os2Table
        Public Shared Function FromReader(reader As FontReader, tableEntry As TableRecordEntry) As Os2Table
            reader.Seek(tableEntry.Offset)
            Dim table As Os2Table = New Os2Table() With {
    .Version = reader.ReadUInt16BigEndian(),
    .XAvgCharWidth = reader.ReadInt16BigEndian(),
    .UsWeightClass = reader.ReadUInt16BigEndian(),
    .UsWidthClass = reader.ReadUInt16BigEndian(),
    .FsType = reader.ReadUInt16BigEndian(),
    .YSubscriptXSize = reader.ReadInt16BigEndian(),
    .YSubscriptYSize = reader.ReadInt16BigEndian(),
    .YSubscriptXOffset = reader.ReadInt16BigEndian(),
    .YSubscriptYOffset = reader.ReadInt16BigEndian(),
    .YSuperscriptXSize = reader.ReadInt16BigEndian(),
    .YSuperscriptYSize = reader.ReadInt16BigEndian(),
    .YSuperscriptXOffset = reader.ReadInt16BigEndian(),
    .YSuperscriptYOffset = reader.ReadInt16BigEndian(),
    .YStrikeoutSize = reader.ReadInt16BigEndian(),
    .YStrikeoutPosition = reader.ReadInt16BigEndian(),
    .SFamilyClass = reader.ReadInt16BigEndian(),
    .Panose = reader.ReadBytes(10),
    .UlUnicodeRange1 = reader.ReadUInt32BigEndian(),
    .UlUnicodeRange2 = reader.ReadUInt32BigEndian(),
    .UlUnicodeRange3 = reader.ReadUInt32BigEndian(),
    .UlUnicodeRange4 = reader.ReadUInt32BigEndian(),
    .AchVendID = reader.ReadAscii(4),
    .FsSelection = reader.ReadUInt16BigEndian(),
    .UsFirstCharIndex = reader.ReadUInt16BigEndian(),
    .UsLastCharIndex = reader.ReadUInt16BigEndian()
}

            If table.Version = 0 AndAlso tableEntry.Length = reader.Position - tableEntry.Offset Then
                Return table
            End If

            ' Some version 0 fonts on Windows include the following extra fields

            table.STypoAscender = reader.ReadInt16BigEndian()
            table.STypoDescender = reader.ReadInt16BigEndian()
            table.STypoLineGap = reader.ReadInt16BigEndian()
            table.UsWinAscent = reader.ReadUInt16BigEndian()
            table.UsWinDescent = reader.ReadUInt16BigEndian()

            If table.Version = 0 Then
                Return table
            End If

            table.UlCodePageRange1 = reader.ReadUInt32BigEndian()
            table.UlCodePageRange2 = reader.ReadUInt32BigEndian()


            If table.Version = 1 Then
                Return table
            End If

            table.SxHeight = reader.ReadInt16BigEndian()
            table.SCapHeight = reader.ReadInt16BigEndian()
            table.UsDefaultChar = reader.ReadUInt16BigEndian()
            table.UsBreakChar = reader.ReadUInt16BigEndian()
            table.UsMaxContext = reader.ReadUInt16BigEndian()


            If table.Version < 5 Then
                Return table
            End If

            table.UsLowerOpticalPointSize = reader.ReadUInt16BigEndian()
            table.UsUpperOpticalPointSize = reader.ReadUInt16BigEndian()

            Return table
        End Function

        Public Property Version As UShort
        Public Property XAvgCharWidth As Short
        Public Property UsWeightClass As UShort
        Public Property UsWidthClass As UShort
        Public Property FsType As UShort
        Public Property YSubscriptXSize As Short
        Public Property YSubscriptYSize As Short
        Public Property YSubscriptXOffset As Short
        Public Property YSubscriptYOffset As Short
        Public Property YSuperscriptXSize As Short
        Public Property YSuperscriptYSize As Short
        Public Property YSuperscriptXOffset As Short
        Public Property YSuperscriptYOffset As Short
        Public Property YStrikeoutSize As Short
        Public Property YStrikeoutPosition As Short
        Public Property SFamilyClass As Short
        Public Property Panose As IReadOnlyList(Of Byte)
        Public Property UlUnicodeRange1 As UInteger
        Public Property UlUnicodeRange2 As UInteger
        Public Property UlUnicodeRange3 As UInteger
        Public Property UlUnicodeRange4 As UInteger
        Public Property AchVendID As String
        Public Property FsSelection As UShort
        Public Property UsFirstCharIndex As UShort
        Public Property UsLastCharIndex As UShort
        Public Property STypoAscender As Short
        Public Property STypoDescender As Short
        Public Property STypoLineGap As Short
        Public Property UsWinAscent As UShort
        Public Property UsWinDescent As UShort
        Public Property UlCodePageRange1 As UInteger
        Public Property UlCodePageRange2 As UInteger
        Public Property SxHeight As Short
        Public Property SCapHeight As Short
        Public Property UsDefaultChar As UShort
        Public Property UsBreakChar As UShort
        Public Property UsMaxContext As UShort
        Public Property UsLowerOpticalPointSize As UShort
        Public Property UsUpperOpticalPointSize As UShort
    End Class
End Namespace
