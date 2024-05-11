Imports System
Imports System.Globalization
Imports IO

Namespace Tables.Name
    Public Class NameRecord
        Public Shared Function FromReader(reader As FontReader, storageStart As Long) As NameRecord
            ' Currently always 0
            Dim platformId = CType(reader.ReadUInt16BigEndian(), Platform)

            Dim encodingId = reader.ReadUInt16BigEndian()

            If platformId = Platform.Windows Then
                If encodingId <> 10 OrElse encodingId <> 1 OrElse encodingId <> 0 Then ' Should be 0 for symbol fonts, 1 for unicode fonts
                    Throw New Exception("Unexpected encoding in name record")
                End If
            End If

            If platformId = Platform.Macintosh Then
                If encodingId <> 0 Then
                    Throw New Exception("Unexpected encoding in name record")
                End If
            End If

            Dim languageId = reader.ReadUInt16BigEndian()
            Dim language = ToCulture(platformId, languageId)
            Dim nameId = CType(reader.ReadUInt16BigEndian(), NameId)
            Dim length = reader.ReadUInt16BigEndian()
            Dim offset = reader.ReadUInt16BigEndian()

            Dim [end] = reader.Position

            reader.Seek(storageStart + offset)
            Dim text = ReadString(reader, platformId, encodingId, length)
            reader.Seek([end])



            Return New NameRecord(platformId, encodingId, language, nameId, length, offset, text)
        End Function

        Private Shared Function ReadString(reader As FontReader, platform As Platform, encodingId As UShort, length As UShort) As String

            If platform = Platform.Windows OrElse platform = Platform.Unicode Then
                ' All unicode fonts on windows encode their name in the naming table using UTF16 Big Endian unicode.
                Return reader.ReadBigEndianUnicode(length)
            End If

            ' platform is Mac

            ' Mac uses the MacRoman character set for the names in a font. Unfortunately they
            ' differ slightly from the ANSII character set, but for characters 32-126 they are identical
            ' so we use ANSII encoding as a best effort to read the names
            Return reader.ReadAscii(length)
        End Function

        Public Sub New(platformId As Platform, encodingId As UShort, language As CultureInfo, nameId As NameId, length As UShort, offset As UShort, text As String)
            Me.PlatformId = platformId
            Me.EncodingId = encodingId
            Me.Language = language
            Me.NameId = nameId
            Me.Length = length
            Me.Offset = offset
            Me.Text = text
        End Sub

        Public ReadOnly Property PlatformId As Platform
        Public ReadOnly Property EncodingId As UShort
        Public ReadOnly Property Language As CultureInfo
        Public ReadOnly Property NameId As NameId
        Public ReadOnly Property Length As UShort
        Public ReadOnly Property Offset As UShort
        Public ReadOnly Property Text As String

        Public Overrides Function ToString() As String
            Return $"{NameId}:  {Text}"
        End Function
    End Class
End Namespace
