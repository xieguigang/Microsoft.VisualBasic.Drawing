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
    ''' <summary>
    ''' Supports basic read/write functionality for a TIFF stream.
    ''' </summary>
    Public Class TiffStreamWriter
        Inherits TiffStreamReader
        Implements IDisposable
        ''' <summary>
        ''' Initializes a new TiffStream.
        ''' </summary>
        ''' <param name="forceBigEndian">Optionally, force TiffStream to use Big-Endian mode for writing a new TIFF.</param>
        Public Sub New(Optional forceBigEndian As Boolean = False)
            MyBase.New(forceBigEndian)
        End Sub


        ''' <summary>
        ''' Initializes a new TiffStream from the given stream.
        ''' </summary>
        ''' <param name="stream"></param>
        Public Sub New(stream As Stream)
            MyBase.New(stream)
        End Sub

#Region "image I/O"

        ''' <summary>
        ''' Write and finalize the given tags to a new IFD at the end of the stream.
        ''' </summary>
        ''' <param name="tags"></param>
        ''' <returns></returns>
        Friend Function WriteIFD(tags As IEnumerable(Of Tag)) As UInteger
            Dim offset = CUInt(SeekWord())
            WriteWord(CUShort(tags.Count()))
            ' write all placeholder tags first
            For Each tag In tags.OrderBy(Function(t) t.ID)
                WriteTagPlaceholder(tag)
            Next
            ' write placeholder value for next IFD pointer
            WriteDWord(0)
            ' then finalize each tag, writing long values after the IFD
            For Each tag In tags.OrderBy(Function(t) t.ID)
                FinalizeTag(tag)
            Next
            _Stream.Seek(0, SeekOrigin.End)
            Return offset
        End Function

        ''' <summary>
        ''' Write image strip data for the given IFD, and update strip tags
        ''' </summary>
        ''' <param name="ifdOffset"></param>
        ''' <param name="strips"></param>
        Friend Sub WriteStrips(ifdOffset As UInteger, strips As Strip())
            Dim ifd = ReadIFD(ifdOffset)
            Dim stripCount = strips.Length

            Dim lengthTag = ifd.tags.Where(Function(t) t.ID = CUShort(BaselineTags.StripByteCounts)).FirstOrDefault()
            Dim offsetTag = ifd.tags.Where(Function(t) t.ID = CUShort(BaselineTags.StripOffsets)).FirstOrDefault()
            If offsetTag.Length <> stripCount OrElse lengthTag.Length <> stripCount Then
                Throw New ArgumentException("The given IFD does not contain valid tags for StripByteCounts or StripOffsets. Data may be corrupt.")
            End If
            lengthTag = MyBase.ParseTag(lengthTag)
            offsetTag = MyBase.ParseTag(offsetTag)

            For i = 0 To stripCount - 1
                Dim strip = strips(i)

                Dim stripOffset = CUInt(SeekWord(0, SeekOrigin.End))
                Write(strip.ImageData, 0, strip.ImageData.Length)

                ' update strip tag values
                If TypeOf lengthTag Is TagType(Of UShort) Then
                    CType(lengthTag, TagType(Of UShort)).Values(i) = CUShort(strip.ImageData.Length)
                ElseIf TypeOf lengthTag Is TagType(Of UInteger) Then
                    CType(lengthTag, TagType(Of UInteger)).Values(i) = CUInt(strip.ImageData.Length)
                End If
                If TypeOf offsetTag Is TagType(Of UShort) Then
                    CType(offsetTag, TagType(Of UShort)).Values(i) = CUShort(stripOffset)
                ElseIf TypeOf offsetTag Is TagType(Of UInteger) Then
                    CType(offsetTag, TagType(Of UInteger)).Values(i) = stripOffset
                End If
            Next

            Me.UpdateTags(offsetTag, lengthTag)
        End Sub

        ''' <summary>
        ''' Update a finalized tag. Method will throw if the number or type of items has changed.
        ''' </summary>
        ''' <param name="tags"></param>
        Friend Sub UpdateTags(ParamArray tags As Tag())
            For Each tag In tags
                Seek(tag.Offset, SeekOrigin.Begin)
                Dim readTag = MyBase.ReadTag()
                Dim parsedTag = ParseTag(readTag)
                If tag.Length <> parsedTag.Length OrElse tag.DataType <> parsedTag.DataType Then
                    Throw New ArgumentException("Tag length and/or type has changed, and is not updateable.")
                Else
                    FinalizeTag(tag, rewrite:=True)
                End If
            Next
        End Sub

        ''' <summary>
        ''' Updates the pointer for the next IFD record.
        ''' </summary>
        Friend Sub UpdateIFDPointer(ifdOffset As UInteger, nextIfdOffset As UInteger)
            ' read first word from this IFD
            Seek(ifdOffset, SeekOrigin.Begin)
            Dim tagCount = ReadWord()
            ' skip to the end of the IFD
            Seek(tagCount * 12, SeekOrigin.Current)
            ' write the new pointer
            WriteDWord(nextIfdOffset)
        End Sub

#Region "tag I/O"

        ''' <summary>
        ''' Writes an initial tag placeholder, and updates the offset property if the tag object.
        ''' </summary>
        ''' <param name="tag"></param>
        Public Sub WriteTagPlaceholder(tag As Tag)
            Dim offset = _Stream.Position
            WriteWord(tag.ID)
            WriteWord(tag.DataType)
            WriteDWord(tag.Length)
            WriteDWord(0)

            tag.Offset = CUInt(offset)
        End Sub

        ''' <summary>
        ''' Writes the actual data payload for a tag and updates the placeholder value (if necessary)
        ''' </summary>
        ''' <param name="tag"></param>
        ''' <param name="rewrite">Signals that a previously finalized tag should be overwritten without allocating more space.</param>
        Public Sub FinalizeTag(tag As Tag, Optional rewrite As Boolean = False)
            If TypeOf tag Is TagType(Of Char) Then
                FinalizeTag(CType(tag, TagType(Of Char)), rewrite)
            ElseIf TypeOf tag Is TagType(Of Byte) Then
                FinalizeTag(CType(tag, TagType(Of Byte)), rewrite)
            ElseIf TypeOf tag Is TagType(Of UShort) Then
                FinalizeTag(CType(tag, TagType(Of UShort)), rewrite)
            ElseIf TypeOf tag Is TagType(Of Short) Then
                FinalizeTag(CType(tag, TagType(Of Short)), rewrite)
            ElseIf TypeOf tag Is TagType(Of UInteger) Then
                FinalizeTag(CType(tag, TagType(Of UInteger)), rewrite)
            ElseIf TypeOf tag Is TagType(Of Integer) Then
                FinalizeTag(CType(tag, TagType(Of Integer)), rewrite)
            ElseIf TypeOf tag Is TagType(Of Single) Then
                FinalizeTag(CType(tag, TagType(Of Single)), rewrite)
            ElseIf TypeOf tag Is TagType(Of Double) Then
                FinalizeTag(CType(tag, TagType(Of Double)), rewrite)
            ElseIf TypeOf tag Is TagType(Of Rational) Then
                FinalizeTag(CType(tag, TagType(Of Rational)), rewrite)
            ElseIf TypeOf tag Is TagType(Of SRational) Then
                FinalizeTag(CType(tag, TagType(Of SRational)), rewrite)
            Else
                Throw New ArgumentException($"Don't know how to finalize a tag of type {tag.GetType()}")
            End If
        End Sub

        ''' <summary>
        ''' Writes the actual data payload for a tag and updates the pointer value (if necessary)
        ''' </summary>
        ''' <param name="tag"></param>
        Public Sub FinalizeTag(Of T As Structure)(tag As TagType(Of T), rewrite As Boolean)
            If tag.Length <> tag.Values.Length Then
                Throw New DataMisalignedException("Tag Length property and the length of the Values array do not match.")
            End If

            Dim atomicLength = tag.DataType.AtomicLength()
            _Stream.Seek(tag.Offset + 4, SeekOrigin.Begin)
            WriteDWord(tag.Length)
            If atomicLength * tag.Length > 4 Then
                Dim pointer As UInteger = If(rewrite, ReadDWord(), CUInt(SeekWord(0, SeekOrigin.End)))
                Seek(pointer, SeekOrigin.Begin)
                For Each value In tag.Values
                    WriteValue(value)
                Next
                _Stream.Seek(tag.Offset + 8, SeekOrigin.Begin)
                Dim bytes = BitConverter.GetBytes(pointer)
                CheckEndian(bytes)
                _Stream.Write(bytes, 0, 4)
                tag.RawValue = bytes
            Else
                ' zero out the data field in case we don't have enough values to fill it.
                WriteDWord(0)
                _Stream.Seek(-4, SeekOrigin.Current)
                For Each value In tag.Values
                    ' arrays written to the Tag data field are always left-justified, even in little-endian systems
                    WriteValue(value)
                Next
                'reread raw value
                _Stream.Seek(tag.Offset + 8, SeekOrigin.Begin)
                _Stream.Read(tag.RawValue, 0, 4)
            End If

        End Sub

#End Region




#End Region

#Region "basic I/O"


        ''' <summary>
        ''' Writes a standard TIFF header at the beginning of the stream.
        ''' </summary>
        Public Sub WriteHeader()
            _Stream.Seek(0, SeekOrigin.Begin)
            If IsBigEndian Then
                _Stream.Write(New Byte() {&H4D, &H4D}, 0, 2)
            Else
                _Stream.Write(New Byte() {&H49, &H49}, 0, 2)
            End If

            WriteWord(42)
        End Sub

        Private Sub WriteValue(Of T As Structure)(value As T)
            If GetType(T) Is GetType(Byte) Then
                MyBase.WriteByte(CObj(value))
            ElseIf GetType(T) Is GetType(Char) Then
                MyBase.WriteByte(Microsoft.VisualBasic.AscW(CChar(CObj(value))))
            ElseIf GetType(T) Is GetType(Double) Then
                WriteDouble(CObj(value))
            ElseIf GetType(T) Is GetType(Single) Then
                WriteFloat(CObj(value))
            ElseIf GetType(T) Is GetType(UInteger) Then
                WriteDWord(CObj(value))
            ElseIf GetType(T) Is GetType(Integer) Then
                WriteLong(CObj(value))
            ElseIf GetType(T) Is GetType(Short) Then
                WriteShort(CObj(value))
            ElseIf GetType(T) Is GetType(UShort) Then
                WriteWord(CObj(value))
            ElseIf GetType(T) Is GetType(Rational) Then
                WriteRational(CObj(value))
            ElseIf GetType(T) Is GetType(SRational) Then
                WriteSRational(CObj(value))
            Else
                Throw New ArgumentException($"Can't write value of type {GetType(T).ToString()}")
            End If
        End Sub

        Private Sub WriteSRational(value As SRational)
            WriteLong(value.Numerator)
            WriteLong(value.Denominator)
        End Sub

        Private Sub WriteRational(value As Rational)
            WriteDWord(value.Numerator)
            WriteDWord(value.Denominator)
        End Sub

        Private Sub WriteShort(value As Short)
            Dim bytes = BitConverter.GetBytes(value)
            WriteEndianBytes(bytes)
        End Sub

        Private Sub WriteLong(value As Integer)
            Dim bytes = BitConverter.GetBytes(value)
            WriteEndianBytes(bytes)
        End Sub

        Private Sub WriteFloat(value As Single)
            Dim bytes = BitConverter.GetBytes(value)
            WriteEndianBytes(bytes)
        End Sub

        Private Sub WriteDouble(value As Double)
            Dim bytes = BitConverter.GetBytes(value)
            WriteEndianBytes(bytes)
        End Sub

        ''' <summary>
        ''' Advance the stream (if necessary) to the next word boundary.
        ''' </summary>
        Public Function SeekWord() As Long
            ' if we're on an odd byte, advance one position
            If _Stream.Position Mod 2 <> 0 Then
                ' if we're at the end, pad with a 0
                If _Stream.Position = _Stream.Length - 1 Then
                    MyBase.WriteByte(&H0)
                Else
                    _Stream.Seek(1, SeekOrigin.Current)
                End If
            End If
            Return _Stream.Position
        End Function

        ''' <summary>
        ''' Seeks to the given offset, and then +1 if the offset is odd.
        ''' </summary>
        ''' <param name="offset"></param>
        ''' <param name="origin"></param>
        ''' <returns></returns>
        Public Function SeekWord(offset As UInteger, origin As SeekOrigin) As Long
            Seek(offset, origin)
            Return SeekWord()
        End Function

        Public Sub WriteWord(value As UShort)
            Dim bytes = BitConverter.GetBytes(value)
            WriteEndianBytes(bytes)
        End Sub

        Public Sub WriteDWord(value As UInteger)
            Dim bytes = BitConverter.GetBytes(value)
            WriteEndianBytes(bytes)
        End Sub

        Private Sub WriteEndianBytes(bytes As Byte())
            CheckEndian(bytes)
            _Stream.Write(bytes, 0, bytes.Length)
        End Sub

#End Region

    End Class
End Namespace
