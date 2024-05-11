Imports Microsoft.VisualBasic.Drawing.Fonts.TrueType.IO

''' <summary>
''' Header for TrueType Collection files
''' </summary>
Public Structure TtcHeader
    ''' <summary>
    ''' Font Collection ID 
    ''' </summary>
    Public Property Tag As String

    ''' <summary>
    ''' Major version of the TTC Header
    ''' </summary>
    Public Property MajorVersion As UShort

    ''' <summary>
    ''' Minor version of the TTC Header, = 0.
    ''' </summary>
    Public Property MinorVersion As UShort

    ''' <summary>
    ''' Number of fonts in TTC
    ''' </summary>
    Public Property NumFonts As UInteger

    ''' <summary>
    ''' Array of offsets to the TableDirectory for each font from the beginning of the file
    ''' </summary>
    Public Property TableDirectoryOffsets As IReadOnlyList(Of UInteger)

    ''' <summary>
    ''' Tag indicating that a DSIG table exists, 0x44534947 (‘DSIG’) (null if no signature)
    ''' </summary>
    Public Property DsigTag As UInteger

    ''' <summary>
    ''' The length(in bytes) of the DSIG table(null if no signature)
    ''' </summary>
    Public Property DsigLength As UInteger

    ''' <summary>
    '''  The offset(in bytes) of the DSIG table from the beginning of the TTC file (null if no signature)
    ''' </summary>
    Public Property DsigOffset As UInteger

    Public Shared Function Parse(reader As FontReader) As TtcHeader
        Dim result As TtcHeader = New TtcHeader() With {
    .Tag = reader.ReadAscii(4),
    .MajorVersion = reader.ReadUInt16BigEndian(),
    .MinorVersion = reader.ReadUInt16BigEndian(),
    .NumFonts = reader.ReadUInt32BigEndian()
}

        Dim offsets As List(Of UInteger) = New List(Of UInteger)()
        For i As Integer = 0 To result.NumFonts - 1
            offsets.Add(reader.ReadUInt32BigEndian())
        Next

        If result.MajorVersion = 1 Then
            result.TableDirectoryOffsets = offsets
            Return result
        Else
            result.TableDirectoryOffsets = offsets
            result.DsigTag = reader.ReadUInt32BigEndian()
            result.DsigLength = reader.ReadUInt32BigEndian()
            result.DsigOffset = reader.ReadUInt32BigEndian()
        End If

        Return result
    End Function
End Structure
