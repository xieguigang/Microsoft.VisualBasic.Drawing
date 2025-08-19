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

Namespace Tiff.Types
    Public Class Image
        Public Property Tags As List(Of Tag) = New List(Of Tag)()
        Public Property Strips As List(Of Strip) = New List(Of Strip)()
        Public Property SubImages As List(Of Image) = New List(Of Image)()
        Public Property Exif As List(Of Tag)
    End Class
End Namespace
