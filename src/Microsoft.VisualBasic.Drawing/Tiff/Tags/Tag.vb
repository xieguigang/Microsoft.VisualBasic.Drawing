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

Imports Microsoft.VisualBasic.Text

Namespace Tiff.Tags

    ''' <summary>
    ''' Represents a raw un-parsed tag as read from a TIFF IFD
    ''' </summary>
    Public Class Tag : Inherits TagBase

        Public Overrides Function GetString() As String
            Return "0x" & RawValue.PrettyPrint()
        End Function

        Public Overrides Function GetValue(Of T)(index As Integer) As T
            Throw New IndexOutOfRangeException("Tag must be parsed to access values by index")
        End Function
    End Class

    ''' <summary>
    ''' Represents a parsed tag with a defined type and fetched pointer values (if any).
    ''' </summary>
    ''' <typeparam name="T"></typeparam>
    Public Class TagType(Of T) : Inherits Tag

        ''' <summary>
        ''' Attempts to fetch a value and cast it to the given type.
        ''' </summary>
        ''' <typeparam name="V"></typeparam>
        ''' <param name="index"></param>
        ''' <returns></returns>
        Public Overrides Function GetValue(Of V)(index As Integer) As V
            Return CObj(Values(index))
        End Function

        Public Overrides Function GetString() As String
            Dim valueStr = String.Empty
            Select Case DataType
                Case TagDataType.ASCII
                    valueStr = New String(CType(CType(CObj(Enumerable.ToArray(Values)), Char()), Char())).Replace(ASCII.NUL, "")
                Case TagDataType.Byte, TagDataType.Undefined
                    valueStr = CType(CObj(Enumerable.ToArray(Values)), Byte()).PrettyPrint()
                Case Else
                    valueStr = String.Join(", ", Values)
            End Select
            Return valueStr
        End Function

        Public Property Values As T() = New T(-1) {}

        Public Overrides Function ToString() As String
            Dim tagName As String
            If [Enum].IsDefined(GetType(BaselineTags), ID) Then
                tagName = CType(ID, BaselineTags).ToString()
            ElseIf [Enum].IsDefined(GetType(ExtensionTags), ID) Then
                tagName = CType(ID, ExtensionTags).ToString()
            ElseIf [Enum].IsDefined(GetType(PrivateTags), ID) Then
                tagName = CType(ID, PrivateTags).ToString()
            ElseIf [Enum].IsDefined(GetType(ExifTags), ID) Then
                tagName = CType(ID, ExifTags).ToString()
            ElseIf [Enum].IsDefined(GetType(GPSTags), ID) Then
                tagName = CType(ID, GPSTags).ToString()
            Else
                tagName = ID.ToString()
            End If
            Return $"{tagName}:{DataType}:{Length}:{GetString()}"
        End Function

    End Class
End Namespace
