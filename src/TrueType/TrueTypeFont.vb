Imports RoyT.TrueType.IO
Imports RoyT.TrueType.Tables
Imports RoyT.TrueType.Tables.Cmap
Imports RoyT.TrueType.Tables.Hmtx
Imports RoyT.TrueType.Tables.Kern
Imports RoyT.TrueType.Tables.Name
Imports RoyT.TrueType.Tables.Vmtx
Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Runtime.InteropServices

Namespace RoyT.TrueType
    Public NotInheritable Class TrueTypeFont
        Public Shared Function FromFile(path As String) As TrueTypeFont
            Dim fstream = File.OpenRead(path)
            Dim reader = New FontReader(fstream)

            Return FromStream(reader, path)
        End Function

        Public Shared Function FromCollectionFile(path As String) As IReadOnlyList(Of TrueTypeFont)
            Dim fstream = File.OpenRead(path)
            Dim reader = New FontReader(fstream)

            Dim header = TtcHeader.Parse(reader)

            Dim fonts As List(Of TrueTypeFont) = New List(Of TrueTypeFont)()
            For Each offset In header.TableDirectoryOffsets
                reader.Seek(offset)
                fonts.Add(FromStream(reader, path))
            Next

            Return fonts
        End Function

        Public Shared Function FromStream(reader As FontReader, source As String) As TrueTypeFont
            Dim offsetTable = Tables.OffsetTable.FromReader(reader)
            Dim entries = ReadTableRecords(reader, offsetTable)

            Dim cmap = ReadCmapTable(source, reader, entries)
            Dim name = ReadNameTable(source, reader, entries)
            Dim head = ReadHeadTable(source, reader, entries)
            Dim maxp = ReadMaxpTable(source, reader, entries)
            Dim os2 = ReadOs2Table(reader, entries)
            Dim kern = ReadKernTable(reader, entries)
            Dim hhea = ReadHheaTable(reader, entries)
            Dim vhea = ReadVheaTable(reader, entries)
            Dim hmtx = ReadHmtxTable(reader, entries, hhea.NumberOfHMetrics, maxp.NumGlyphs)
            Dim vmtx = ReadVmtxTable(reader, entries, vhea.NumberOfVMetrics, maxp.NumGlyphs)

            Return New TrueTypeFont(source, offsetTable, entries, cmap, name, kern) With {
    .Os2Table = os2,
    .HeadTable = head,
    .MaxpTable = maxp,
    .HheaTable = hhea,
    .HmtxTable = hmtx,
    .VheaTable = vhea,
    .VmtxTable = vmtx
}
        End Function

        Public Shared Function TryGetTablePosition(reader As FontReader, tableName As String, <Out> ByRef offset As Long) As Boolean
            reader.Seek(0)
            Dim offsetTable = Tables.OffsetTable.FromReader(reader)
            Dim entries = ReadTableRecords(reader, offsetTable)

            Dim cmapEntry As TableRecordEntry = Nothing

            If entries.TryGetValue(tableName, cmapEntry) Then
                offset = cmapEntry.Offset
                Return True
            End If

            offset = 0
            Return False
        End Function

        Private Shared Function ReadTableRecords(reader As FontReader, offsetTable As OffsetTable) As IReadOnlyDictionary(Of String, TableRecordEntry)
            Dim entries = New Dictionary(Of String, TableRecordEntry)(offsetTable.Tables)
            For i = 0 To offsetTable.Tables - 1
                Dim entry = TableRecordEntry.FromReader(reader)
                entries.Add(entry.Tag, entry)
            Next

            Return entries
        End Function

        Private Shared Function ReadCmapTable(path As String, reader As FontReader, entries As IReadOnlyDictionary(Of String, TableRecordEntry)) As CmapTable
            Dim cmapEntry As TableRecordEntry = Nothing

            If entries.TryGetValue(TrueTypeTableNames.cmap, cmapEntry) Then
                reader.Seek(cmapEntry.Offset)
                Return CmapTable.FromReader(reader)
            End If

            Throw New Exception($"Font {path} does not contain a Character To Glyph Index Mapping Table (cmap)")

        End Function

        Private Shared Function ReadNameTable(path As String, reader As FontReader, entries As IReadOnlyDictionary(Of String, TableRecordEntry)) As NameTable
            Dim cmapEntry As TableRecordEntry = Nothing

            If entries.TryGetValue(TrueTypeTableNames.name, cmapEntry) Then
                reader.Seek(cmapEntry.Offset)
                Return NameTable.FromReader(reader)
            End If

            Throw New Exception($"Font {path} does not contain a Name Table (name)")
        End Function

        Private Shared Function ReadHeadTable(path As String, reader As FontReader, entries As IReadOnlyDictionary(Of String, TableRecordEntry)) As HeadTable
            Dim entry As TableRecordEntry = Nothing

            If entries.TryGetValue(head, entry) Then
                Return HeadTable.FromReader(reader, entry)
            End If

            Throw New Exception($"Font {path} does not contain a Font Header Table (head)")
        End Function

        Private Shared Function ReadMaxpTable(path As String, reader As FontReader, entries As IReadOnlyDictionary(Of String, TableRecordEntry)) As MaxpTable
            Dim entry As TableRecordEntry = Nothing

            If entries.TryGetValue(maxp, entry) Then
                Return MaxpTable.FromReader(reader, entry)
            End If

            Throw New Exception($"Font {path} does not contain a Maximum Profile Table (maxp)")
        End Function

        Private Shared Function ReadOs2Table(reader As FontReader, entries As IReadOnlyDictionary(Of String, TableRecordEntry)) As Os2Table
            Dim entry As TableRecordEntry = Nothing

            If entries.TryGetValue(os2, entry) Then
                Return Os2Table.FromReader(reader, entry)
            End If

            Return Nothing
        End Function

        Private Shared Function ReadKernTable(reader As FontReader, entries As IReadOnlyDictionary(Of String, TableRecordEntry)) As KernTable
            Dim kernEntry As TableRecordEntry = Nothing

            If entries.TryGetValue(TrueTypeTableNames.kern, kernEntry) Then
                reader.Seek(kernEntry.Offset)
                Return KernTable.FromReader(reader)
            End If

            Return KernTable.Empty
        End Function

        Private Shared Function ReadHheaTable(reader As FontReader, entries As IReadOnlyDictionary(Of String, TableRecordEntry)) As HheaTable
            Dim entry As TableRecordEntry = Nothing

            If entries.TryGetValue(hhea, entry) Then
                Return HheaTable.FromReader(reader, entry)
            End If

            Return HheaTable.Empty
        End Function

        Private Shared Function ReadHmtxTable(reader As FontReader, entries As IReadOnlyDictionary(Of String, TableRecordEntry), metricsCount As UShort, glyphCount As UShort) As HmtxTable
            Dim entry As TableRecordEntry = Nothing

            If entries.TryGetValue(TrueTypeTableNames.hmtx, entry) Then
                Return HmtxTable.FromReader(reader, entry, metricsCount, glyphCount)
            End If

            Return HmtxTable.Empty
        End Function

        Private Shared Function ReadVheaTable(reader As FontReader, entries As IReadOnlyDictionary(Of String, TableRecordEntry)) As VheaTable
            Dim entry As TableRecordEntry = Nothing

            If entries.TryGetValue(vhea, entry) Then
                Return VheaTable.FromReader(reader, entry)
            End If

            Return VheaTable.Empty
        End Function

        Private Shared Function ReadVmtxTable(reader As FontReader, entries As IReadOnlyDictionary(Of String, TableRecordEntry), metricsCount As UShort, glyphCount As Integer) As VmtxTable
            Dim entry As TableRecordEntry = Nothing

            If entries.TryGetValue(TrueTypeTableNames.vmtx, entry) Then
                Return VmtxTable.FromReader(reader, entry, metricsCount, glyphCount)
            End If

            Return VmtxTable.Empty
        End Function

        Private Sub New(source As String, offsetTable As OffsetTable, entries As IReadOnlyDictionary(Of String, TableRecordEntry), cmapTable As CmapTable, nameTable As NameTable, kernTable As KernTable)
            Me.Source = source
            Me.OffsetTable = offsetTable
            TableRecordEntries = entries
            Me.CmapTable = cmapTable
            Me.NameTable = nameTable
            Me.KernTable = kernTable
        End Sub

        Public ReadOnly Property Source As String
        Public ReadOnly Property OffsetTable As OffsetTable
        Public ReadOnly Property TableRecordEntries As IReadOnlyDictionary(Of String, TableRecordEntry)

        ''' <summary>
        ''' Contains information to get the glyph that corresponds to each supported character
        ''' </summary>
        Public ReadOnly Property CmapTable As CmapTable

        ''' <summary>
        ''' Contains the (translated) name of this font, copyright notices, etc...
        ''' </summary>
        Public ReadOnly Property NameTable As NameTable

        ''' <summary>
        ''' Consists of a set of metrics and other data that are required in OpenType fonts
        ''' </summary>
        Public Property Os2Table As Os2Table

        ''' <summary>
        ''' Consists of global information about the font
        ''' </summary>
        Public Property HeadTable As HeadTable

        ''' <summary>
        ''' Contains the memory requirements for this font.
        ''' </summary>
        Public Property MaxpTable As MaxpTable

        ''' <summary>
        ''' Contains adjustment to horizontal/vertical positions between glyphs
        ''' </summary>
        Public ReadOnly Property KernTable As KernTable

        ''' <summary>
        ''' Contains information for horizontal layout
        ''' </summary>
        Public Property HheaTable As HheaTable

        ''' <summary>
        ''' Horizontal Metrics Table
        ''' </summary>
        Public Property HmtxTable As HmtxTable

        ''' <summary>
        ''' Contains information for vertical layout
        ''' </summary>
        Public Property VheaTable As VheaTable

        ''' <summary>
        ''' Vertical Metrics Table
        ''' </summary>
        Public Property VmtxTable As VmtxTable

        Public Overrides Function ToString() As String
            Return Source
        End Function
    End Class
End Namespace
