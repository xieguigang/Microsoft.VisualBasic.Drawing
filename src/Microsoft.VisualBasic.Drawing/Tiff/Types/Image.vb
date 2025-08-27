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

Imports Microsoft.VisualBasic.Drawing.Tiff.Tags
Imports Microsoft.VisualBasic.Imaging.BitmapImage
Imports std = System.Math

Namespace Tiff.Types

    Public Class Image

        Public Property Tags As New List(Of Tag)()
        Public Property Strips As New List(Of Strip)()
        Public Property SubImages As New List(Of Image)()
        Public Property Exif As New List(Of Tag)

        Public Shared Function FromBitmap(bitmap As BitmapBuffer) As Image
            Dim strips As New List(Of Strip)
            Dim totalRows As Integer = bitmap.Height
            Dim rowsPerStrip As Integer = 32 ' 自定义行数
            Dim stripCount As Integer = CInt(std.Ceiling(totalRows / CDbl(rowsPerStrip)))
            Dim currentOffset As UInteger = 0
            Dim stripByteCounts As New List(Of UInteger)

            For i As Integer = 0 To stripCount - 1
                Dim startRow As Integer = i * rowsPerStrip
                Dim rowsInThisStrip As Integer = std.Min(rowsPerStrip, totalRows - startRow)
                Dim stripBytes As Integer = rowsInThisStrip * bitmap.Stride
                Dim stripData(stripBytes - 1) As Byte

                Array.Copy(bitmap.RawBuffer, startRow * bitmap.Stride, stripData, 0, stripBytes)

                strips.Add(New Strip() With {
                    .ImageData = stripData,
                    .StripNumber = CUShort(i),
                    .StripOffset = currentOffset ' 临时占位，实际偏移需在写入文件时计算
                })
                currentOffset += CUInt(stripBytes) ' 更新下一条带偏移
                stripByteCounts.Add(stripBytes)
            Next

            Dim lengthTag As New TagType(Of UInteger) With {
                .ID = CUShort(BaselineTags.StripByteCounts),
                .Length = stripCount,
                .DataType = TagDataType.Long,
                .Values = stripByteCounts.ToArray
            }

            Return New Image With {
                .Strips = strips,
                .Tags = New List(Of Tag) From {lengthTag}
            }
        End Function

    End Class
End Namespace
