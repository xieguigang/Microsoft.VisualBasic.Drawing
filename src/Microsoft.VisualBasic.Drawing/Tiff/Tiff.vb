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
    Public Class Tiff
        Public Property Images As New List(Of Image)()
        Public Property IsBigEndian As Boolean

        Public Sub New()
        End Sub

        ''' <summary>
        ''' Loads a TIFF file from disk.
        ''' </summary>
        ''' <param name="fileName"></param>
        Public Sub New(fileName As String)
            Using stream = New FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read)
                Using tiffStream = New TiffStreamReader(stream)
                    LoadImage(tiffStream)
                End Using
            End Using
        End Sub

        Public Sub New(stream As Stream)
            Using tiffStream As New TiffStreamReader(stream)
                LoadImage(tiffStream)
            End Using
        End Sub

        Private Sub LoadImage(stream As TiffStreamReader)
            Dim image As Image
            Dim offset As UInteger
            Dim offsets = New HashSet(Of UInteger)() ' keep track of read offsets so we can detect loops

            offset = stream.ReadHeader()

            ' set endianness now that the stream header has been validated
            IsBigEndian = stream.IsBigEndian

            ' read all images from file
            While offset <> 0
                If offsets.Contains(offset) Then
                    Throw New InvalidDataException("Circular image reference detected in file. Aborting load.")
                Else
                    offsets.Add(offset)

                    With stream.ReadImage(offset)
                        offset = .offset
                        image = .image

                        Call Images.Add(image)
                    End With
                End If
            End While
        End Sub

        Public Sub Save(stream As Stream)
            Using tiffStream = New TiffStreamWriter(stream, forceBigEndian:=IsBigEndian)
                Call WriteTo(tiffStream)
                Call stream.Flush()
            End Using
        End Sub

        Public Sub Save(fileName As String)
            Call Save(fileName.Open(FileMode.OpenOrCreate, doClear:=True, [readOnly]:=False))
        End Sub

        ''' <summary>
        ''' Advance the stream to a new word boundary, padding if necessary
        ''' </summary>
        ''' <param name="tiffStream"></param>
        ''' <returns></returns>
        Private Shared Function SeekToNewWord(tiffStream As TiffStreamWriter) As UInteger
            Dim offset = CUInt(tiffStream.SeekWord(0, SeekOrigin.End))
            If offset Mod 2 > 0 Then
                tiffStream.WriteByte(&H0)
                offset += 1UI
            End If ' advance if not on word boundary
            Return offset
        End Function

        Private Function WriteImage(image As Image, tiffStream As TiffStreamWriter) As UInteger
            If image.SubImages.Count > 0 Then
                Throw New TiffWriteException("Writing pyramid/subimage files is not currently supported")
            End If

            Dim imageOffset = SeekToNewWord(tiffStream)

            ' prep the current set of tags, and write the initial IFD
            If image.Exif?.Count > 0 AndAlso Not image.Tags.Any(Function(t) t.ID = PrivateTags.ExifIFD) Then
                ' We have EXIF tags to write, and no current ExifIFD pointer
                image.Tags.Add(New TagType(Of UInteger)() With {
                    .ID = PrivateTags.ExifIFD,
                    .DataType = TagDataType.Long,
                    .Length = 1,
                    .Values = New UInteger() {0}
                })
            ElseIf If(image.Exif?.Count, 0) = 0 AndAlso image.Tags.Any(Function(t) t.ID = PrivateTags.ExifIFD) Then
                ' There are no EXIF tags to write, get rid of the superfluous pointer
                image.Tags.RemoveAll(Function(t) t.ID = PrivateTags.ExifIFD)
            End If
            tiffStream.WriteIFD(image.Tags)

            ' write image strip data
            tiffStream.WriteStrips(imageOffset, image.Strips.ToArray())


            ' write Exif block, if necessary, and update the original tag pointer
            If image.Exif?.Count > 0 Then
                Dim exifIfdOffset = SeekToNewWord(tiffStream)
                tiffStream.WriteIFD(image.Exif)
                Dim exifTag = TryCast(image.Tags.Where(Function(t) t.ID = PrivateTags.ExifIFD).First(), TagType(Of UInteger))
                exifTag.Values(0) = exifIfdOffset
                tiffStream.UpdateTags(exifTag)
            End If

            Return imageOffset
        End Function

        Private Sub WriteTo(tiffStream As TiffStreamWriter)
            tiffStream.WriteHeader()
            tiffStream.WriteDWord(8) ' IFD0 will always be immediately after the header, at offset 0x08
            Dim previousOffset As UInteger = 0

            For Each image In Images
                Dim imageOffset = WriteImage(image, tiffStream)

                ' update the pointer from the previous image, then seek back to the current offset
                If previousOffset <> 0 Then
                    tiffStream.UpdateIFDPointer(previousOffset, imageOffset)
                    tiffStream.Seek(imageOffset, SeekOrigin.Begin)
                End If

                previousOffset = imageOffset
            Next
        End Sub
    End Class
End Namespace
