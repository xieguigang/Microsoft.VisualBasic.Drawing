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

Namespace Tiff
    Public MustInherit Class TiffStreamBase
        Inherits Stream
        Implements IDisposable
        Private _IsBigEndian As Boolean
        Protected _Stream As Stream
        Protected _IsWritable As Boolean

        Public Property IsBigEndian As Boolean
            Get
                Return _IsBigEndian
            End Get
            Protected Set(value As Boolean)
                _IsBigEndian = value
            End Set
        End Property

        ''' <summary>
        ''' If the active endianness conflicts with our architecture, reverse the entire byte array.
        ''' </summary>
        ''' <param name="bytes"></param>
        Protected Sub CheckEndian(bytes As Byte())
            If IsBigEndian = BitConverter.IsLittleEndian Then
                Array.Reverse(bytes)
            End If
        End Sub

        Public Function ToArray() As Byte()
            _Stream.Seek(0, SeekOrigin.Begin)
            Dim bytes = New Byte(_Stream.Length - 1) {}
            _Stream.Read(bytes, 0, _Stream.Length)
            Return bytes
        End Function

#Region "abstracts inherited from Stream"

        Public Overrides ReadOnly Property CanRead As Boolean
            Get
                Return _Stream.CanRead
            End Get
        End Property

        Public Overrides ReadOnly Property CanSeek As Boolean
            Get
                Return _Stream.CanSeek
            End Get
        End Property

        Public Overrides ReadOnly Property CanWrite As Boolean
            Get
                Return _IsWritable AndAlso _Stream.CanWrite
            End Get
        End Property

        Public Overrides ReadOnly Property Length As Long
            Get
                Return _Stream.Length
            End Get
        End Property

        Public Overrides Property Position As Long
            Get
                Return _Stream.Position
            End Get
            Set(value As Long)
                _Stream.Position = value
            End Set
        End Property

        Public Overrides Sub Flush()
            _Stream.Flush()
        End Sub

        Public Overrides Function Read(buffer As Byte(), offset As Integer, count As Integer) As Integer
            Return _Stream.Read(buffer, offset, count)
        End Function

        Public Overrides Function Seek(offset As Long, origin As SeekOrigin) As Long
            Return _Stream.Seek(offset, origin)
        End Function

        Public Overrides Sub SetLength(value As Long)
            If CanWrite Then
                _Stream.SetLength(value)
            Else
                Throw New IOException("Stream is in a read-only state.")
            End If
        End Sub

        Public Overrides Sub Write(buffer As Byte(), offset As Integer, count As Integer)
            If CanWrite Then
                _Stream.Write(buffer, offset, count)
            Else
                Throw New IOException("Stream is not writable.")
            End If
        End Sub

#End Region

#Region "IDisposable Support"
        Private disposedValue As Boolean = False ' To detect redundant calls

        Protected Overrides Sub Dispose(disposing As Boolean)
            If Not disposedValue Then
                If disposing Then
                    _Stream = Nothing
                End If

                ' TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                ' TODO: set large fields to null.

                disposedValue = True
            End If
            MyBase.Dispose(disposing)
        End Sub

        ' TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ' ~TiffStreamBase() {
        '   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        '   Dispose(false);
        ' }

#End Region
    End Class
End Namespace
