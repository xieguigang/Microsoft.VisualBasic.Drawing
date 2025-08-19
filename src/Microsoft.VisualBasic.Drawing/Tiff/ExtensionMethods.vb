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


Imports System.Runtime.CompilerServices

Namespace Tiff

    <HideModuleName>
    Public Module ExtensionMethods
        ''' <summary>
        ''' Gets the length, in bytes, of a given TIFF tag datatype.
        ''' </summary>
        ''' <param name="type"></param>
        ''' <returns></returns>
        <Extension()>
        Public Function AtomicLength(type As TagDataType) As Integer
            Select Case type
                Case TagDataType.ASCII, TagDataType.Byte, TagDataType.SByte, TagDataType.Undefined
                    Return 1
                Case TagDataType.Short, TagDataType.SShort
                    Return 2
                Case TagDataType.Float, TagDataType.Long, TagDataType.SLong, TagDataType.IFD
                    Return 4
                Case TagDataType.Double, TagDataType.Rational, TagDataType.SRational
                    Return 8
                Case Else
                    Throw New ArgumentException($"Unknown tag datatype {type}")
            End Select
        End Function

        ''' <summary>
        ''' Returns a string as a nul-terminated ASCII char array
        ''' </summary>
        ''' <param name="str"></param>
        ''' <returns></returns>
        <Extension()>
        Public Function ToASCIIArray(str As String) As Char()
            Return (str & Microsoft.VisualBasic.Constants.vbNullChar).ToCharArray()
        End Function

        <Extension()>
        Public Function PrettyPrint(bytes As Byte(), Optional maxLength As Integer = 20) As String
            Dim str = BitConverter.ToString(bytes).Replace("-", "").ToLower()
            If str.Length > maxLength Then
                str = str.Substring(0, 20) & "..."
            End If
            Return str
        End Function
    End Module
End Namespace
