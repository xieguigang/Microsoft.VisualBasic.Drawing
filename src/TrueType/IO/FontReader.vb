Imports System
Imports System.Collections
Imports System.IO
Imports System.Text

Namespace RoyT.TrueType.IO
    ''' <summary>
    ''' Extended BinaryReader with helper methods for reading number and text formats
    ''' common in TrueType files
    ''' </summary>
    Public NotInheritable Class FontReader
        Inherits BinaryReader
        Public Sub New(stream As Stream)
            MyBase.New(stream)
        End Sub

        Public ReadOnly Property Position As Long
            Get
                Return BaseStream.Position
            End Get
        End Property

        Public Function ReadInt16BigEndian() As Short
            Dim bytes = ReadBigEndian(2)
            Return BitConverter.ToInt16(bytes, 0)
        End Function

        Public Function ReadUInt16BigEndian() As UShort
            Dim bytes = ReadBigEndian(2)
            Return BitConverter.ToUInt16(bytes, 0)
        End Function

        Public Function ReadUInt32BigEndian() As UInteger
            Dim bytes = ReadBigEndian(4)
            Return BitConverter.ToUInt32(bytes, 0)
        End Function

        Public Function ReadInt64BigEndian() As Long
            Dim bytes = ReadBigEndian(8)
            Return BitConverter.ToInt64(bytes, 0)
        End Function

        Public Function ReadAscii(length As Integer) As String
            Dim bytes = ReadBytes(length)
            Return Encoding.ASCII.GetString(bytes)
        End Function

        Public Function ReadBigEndianUnicode(length As Integer) As String
            Dim bytes = ReadBytes(length)
            Return Encoding.BigEndianUnicode.GetString(CType(bytes, Byte())).Replace(Microsoft.VisualBasic.Constants.vbNullChar, String.Empty)
        End Function

        Public Function ReadBitsBigEndian(numberOfBytes As Integer) As BitArray
            Dim bytes = ReadBigEndian(numberOfBytes)
            Return New BitArray(bytes)
        End Function

        ''' <summary>
        ''' Reads a 32 bit signed fixed point number (16.16)        
        ''' </summary>
        Public Function ReadFixedBigEndian() As Single
            Dim dec = ReadInt16BigEndian()
            Dim frac = ReadUInt16BigEndian()
            Return dec + frac / 65535F
        End Function

        Private Function ReadBigEndian(count As Integer) As Byte()
            Dim data = ReadBytes(count)

            If BitConverter.IsLittleEndian Then
                Array.Reverse(data)
            End If

            Return data
        End Function

        Public Sub Seek(offset As Long, Optional seekOrigin As SeekOrigin = SeekOrigin.Begin)
            BaseStream.Seek(offset, seekOrigin)
        End Sub
    End Class
End Namespace
