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



Namespace Tiff.Tags
    Public MustInherit Class TagBase
        Public Property Offset As UInteger
        Public Property ID As UShort
        Public Property DataType As TagDataType
        Public Property Length As UInteger
        Public Property RawValue As Byte() = New Byte(3) {}

        Public MustOverride Function GetValue(Of T)(index As Integer) As T
        Public MustOverride Function GetString() As String

        Public Overrides Function ToString() As String
            Return $"{ID}:{DataType}:{Length}:{GetString()}"
        End Function
    End Class
End Namespace
