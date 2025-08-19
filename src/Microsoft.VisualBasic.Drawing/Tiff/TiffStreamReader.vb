' 
'    Copyright 2018 Digimarc, Inc

'    Licensed under the Apache License, Version 2.0 (the "License");
'    you may not use this file except in compliance with the License.
'    You may obtain a copy of the License at

'        http://www.apache.org/licenses/LICENSE-2.0

'    Unless required by applicable law or agreed to in writing, software
'    distributed under the License is distributed on an "AS IS" BASIS,
'    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
'    See the License for the specific language governing permissions and
'    limitations under the License.

'    SPDX-License-Identifier: Apache-2.0


Imports System.IO
Imports Microsoft.VisualBasic.Drawing.Tiff.Tags
Imports Microsoft.VisualBasic.Drawing.Tiff.Types

Namespace Tiff
    Public Class TiffStreamReader
        Inherits TiffStreamBase
        ''' <summary>
        ''' Initializes a new TiffStream.
        ''' </summary>
        ''' <param name="forceBigEndian">Optionally, force TiffStream to use Big-Endian mode for writing a new TIFF.</param>
        Public Sub New(Optional forceBigEndian As Boolean = False)
            _Stream = New MemoryStream()
            IsBigEndian = forceBigEndian
            _IsWritable = True
        End Sub

        ''' <summary>
        ''' Initializes a new TiffStream from the given stream.
        ''' </summary>
        ''' <param name="stream"></param>
        Public Sub New(stream As Stream)
            _Stream = stream
            _IsWritable = _Stream.CanWrite
        End Sub

        Public Function ReadImage(offset As UInteger) As (image As Image, offset As UInteger)
            Dim rawIFD = ParseIFD(offset)

            Dim image = New Image()

            ' get image tags
            image.Tags = rawIFD.tags.ToList()

            ' get image data
            image.Strips = ReadStrips(offset).OrderBy(Function(s) s.StripNumber).ToList()

            ' get subimages, if any
            Dim subIfds = image.Tags.FirstOrDefault(Function(t) t.ID = CUInt(ExtensionTags.SubIFDs))
            If subIfds IsNot Nothing Then
                For Each subOffset In CType(subIfds, TagType(Of UInteger)).Values
                    image.SubImages.Add(ReadImage(CUInt(subOffset)).image)
                Next
            End If

            ' load Exif, if present
            Dim exifOffset = rawIFD.tags.Where(Function(t) t.ID = CUShort(PrivateTags.ExifIFD)).FirstOrDefault()
            If exifOffset IsNot Nothing Then
                image.Exif = Me.ParseIFD(CUInt(exifOffset.GetValue(Of UInteger)(CInt(0)))).tags.ToList()
            End If


            Return (image, rawIFD.nextIfd)
        End Function

        ''' <summary>
        ''' Read all image strips from the given IFD
        ''' </summary>
        ''' <param name="ifdOffset"></param>
        ''' <returns></returns>
        Public Function ReadStrips(ifdOffset As UInteger) As Strip()
            Dim tags = ReadIFD(ifdOffset).tags
            Dim offsets = tags.Where(Function(t) t.ID = CUShort(BaselineTags.StripOffsets)).FirstOrDefault()
            Dim byteLengths = tags.Where(Function(t) t.ID = CUShort(BaselineTags.StripByteCounts)).FirstOrDefault()

            If offsets Is Nothing Then
                Throw New IOException("IFD is missing StripOffsets tag.")
            End If
            If byteLengths Is Nothing Then
                Throw New IOException("IFD is missing StripByteCounts tag.")
            End If

            ' Offsets and lengths can be either ushort or uint, which complicates things with our Tag<T> generics
            offsets = Me.ParseTag(offsets)
            byteLengths = Me.ParseTag(byteLengths)

            Dim strips = New Strip(offsets.Length - 1) {}

            For i As UShort = 0 To offsets.Length - 1
                Dim offset = offsets.GetValue(Of UInteger)(i)
                Dim len = byteLengths.GetValue(Of UInteger)(i)
                Dim bytes = New Byte(len - 1) {}
                _Stream.Seek(offset, SeekOrigin.Begin)
                _Stream.Read(bytes, 0, CInt(len))

                strips(i) = New Strip With {
    .ImageData = bytes,
    .StripNumber = i,
    .StripOffset = offset
}
            Next

            Return strips
        End Function

        Public Function ReadIFD(offset As UInteger) As (nextIfd As UInteger, tags As Tag())
            _Stream.Seek(offset, SeekOrigin.Begin)

            Dim tagCount = ReadWord()
            Dim tags = New Tag(tagCount - 1) {}

            For i = 0 To tagCount - 1
                tags(i) = ReadTag()
            Next

            Dim nextIfd = ReadDWord()
            Return (nextIfd, tags)
        End Function

        Public Function ParseIFD(offset As UInteger) As (nextIfd As UInteger, tags As Tag())
            Dim result = ReadIFD(offset)
            For i As Integer = 0 To result.tags.Length - 1
                result.tags(i) = Me.ParseTag(result.tags(i))
            Next
            Return result
        End Function

        ''' <summary>
        ''' read a general tag data
        ''' </summary>
        ''' <returns></returns>
        Public Function ReadTag() As Tag
            Dim tag = New Tag()
            tag.Offset = CUInt(_Stream.Position)
            tag.ID = ReadWord()
            tag.DataType = CType(ReadWord(), TagDataType)
            tag.Length = ReadDWord()
            _Stream.Read(tag.RawValue, 0, 4)

            Return tag
        End Function

        ''' <summary>
        ''' Attempts to parse the given Tag into an appropriate Tag<T> and returns a boxed Tag value
        ''' </summary>
        ''' <param name="tag"></param>
        ''' <returns></returns></summary>       
        Protected Function ParseTag(tag As Tag) As Tag
            Select Case tag.DataType
                Case TagDataType.ASCII
                    Return ParseTag(Of Char)(tag)
                Case TagDataType.Byte, TagDataType.SByte, TagDataType.Undefined
                    Return ParseTag(Of Byte)(tag)
                Case TagDataType.Double
                    Return ParseTag(Of Double)(tag)
                Case TagDataType.Float
                    Return ParseTag(Of Single)(tag)
                Case TagDataType.IFD, TagDataType.Long
                    Return ParseTag(Of UInteger)(tag)
                Case TagDataType.Rational
                    Return ParseTag(Of Rational)(tag)
                Case TagDataType.SRational
                    Return ParseTag(Of SRational)(tag)
                Case TagDataType.Short
                    Return ParseTag(Of UShort)(tag)
                Case TagDataType.SLong
                    Return ParseTag(Of Integer)(tag)
                Case TagDataType.SShort
                    Return ParseTag(Of Short)(tag)
                Case Else
                    Return tag
            End Select
        End Function

        Protected Function ParseTag(Of T As Structure)(tag As Tag) As TagType(Of T)
            Dim startingOffet = _Stream.Position

            Dim parsedTag = New TagType(Of T)() With {
    .ID = tag.ID,
    .Length = tag.Length,
    .Offset = tag.Offset,
    .RawValue = tag.RawValue,
    .DataType = tag.DataType,
    .Values = New T(tag.Length - 1) {}
}

            ' seek data portion of the raw tag
            _Stream.Seek(tag.Offset + 8, SeekOrigin.Begin)

            ' if tag data is too long, read a pointer and seek data block
            Dim atomicLen = tag.DataType.AtomicLength()
            If atomicLen * tag.Length > 4 Then
                Dim pointer = ReadDWord()
                _Stream.Seek(pointer, SeekOrigin.Begin)
            End If

            ' read actual values into tag.Values
            For i As Integer = 0 To tag.Length - 1
                parsedTag.Values(i) = ReadValue(Of T)()
            Next

            _Stream.Seek(startingOffet, SeekOrigin.Begin)
            Return parsedTag
        End Function


        ''' <summary>
        ''' Reads and validates the TIFF header from the underlying stream, and returns the offset of the first IFD (IFD0).
        ''' </summary>
        ''' <returns></returns>
        Public Function ReadHeader() As UInteger
            _Stream.Seek(0, SeekOrigin.Begin)
            Dim magic = ReadWord() ' header bytes are paired so endianness does not matter

            If magic = &H4D4D Then
                IsBigEndian = True
            ElseIf magic = &H4949 Then
                IsBigEndian = False
            Else
                Throw New FormatException("Stream does not contain a valid TIFF header.")
            End If

            Dim version = ReadWord()
            If version <> 42 Then
                Throw New FormatException($"Invalid TIFF version: {version}")
            End If

            Dim ifd0 = ReadDWord()
            If ifd0 >= 8 AndAlso ifd0 < _Stream.Length Then
                Return ifd0
            Else
                Throw New IOException($"Value for IFD0 {ifd0} is not valid.")
            End If
        End Function


        Private Function ReadValue(Of T As Structure)() As T
            Dim obj As Object = Nothing
            If GetType(T) Is GetType(Char) Then
                obj = Microsoft.VisualBasic.ChrW(_Stream.ReadByte())
            ElseIf GetType(T) Is GetType(Byte) Then
                obj = CByte(_Stream.ReadByte())
            ElseIf GetType(T) Is GetType(Double) Then
                obj = ReadDouble()
            ElseIf GetType(T) Is GetType(Single) Then
                obj = ReadFloat()
            ElseIf GetType(T) Is GetType(UInteger) Then
                obj = ReadDWord()
            ElseIf GetType(T) Is GetType(Integer) Then
                obj = ReadLong()
            ElseIf GetType(T) Is GetType(Short) Then
                obj = ReadShort()
            ElseIf GetType(T) Is GetType(UShort) Then
                obj = ReadWord()
            ElseIf GetType(T) Is GetType(Rational) Then
                obj = ReadRational()
            ElseIf GetType(T) Is GetType(SRational) Then
                obj = ReadSRational()
            End If

            If obj IsNot Nothing Then
                Return obj
            Else
                Throw New ArgumentException($"Can't read value of type {GetType(T).ToString()}")
            End If
        End Function



        Public Function ReadWord() As UShort
            Return BitConverter.ToUInt16(ReadEndianBytes(2), 0)
        End Function
        Public Function ReadDWord() As UInteger
            Return BitConverter.ToUInt32(ReadEndianBytes(4), 0)
        End Function
        Public Function ReadShort() As Short
            Return BitConverter.ToInt16(ReadEndianBytes(2), 0)
        End Function
        Public Function ReadLong() As Integer
            Return BitConverter.ToInt32(ReadEndianBytes(4), 0)
        End Function
        Public Function ReadFloat() As Single
            Return BitConverter.ToSingle(ReadEndianBytes(4), 0)
        End Function
        Public Function ReadDouble() As Double
            Return BitConverter.ToDouble(ReadEndianBytes(4), 0)
        End Function
        Public Function ReadRational() As Rational
            Return New Rational(ReadDWord(), ReadDWord())
        End Function
        Public Function ReadSRational() As SRational
            Return New SRational(ReadLong(), ReadLong())
        End Function

        Protected Function ReadEndianBytes(count As Integer) As Byte()
            Dim bytes = New Byte(count - 1) {}
            _Stream.Read(bytes, 0, count)
            CheckEndian(bytes)
            Return bytes
        End Function
    End Class
End Namespace
