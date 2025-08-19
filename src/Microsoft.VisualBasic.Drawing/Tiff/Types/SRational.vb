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



Namespace Tiff.Types
    Public Structure SRational
        Public Sub New(numerator As Integer, denominator As Integer)
            Me.Numerator = numerator
            Me.Denominator = denominator
        End Sub

        Public ReadOnly Numerator As Integer
        Public ReadOnly Denominator As Integer
        Public Function ToDouble() As Double
            Return Numerator / Denominator
        End Function

        Public Overrides Function ToString() As String
            Return $"{Numerator:N0}/{Denominator:N0}"
        End Function
    End Structure
End Namespace
