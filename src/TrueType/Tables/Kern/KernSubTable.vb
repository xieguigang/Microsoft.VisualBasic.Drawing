Imports RoyT.TrueType.IO

Namespace RoyT.TrueType.Tables.Kern
    Public NotInheritable Class KernSubtable
        Public Shared Function FromReader(reader As FontReader) As KernSubtable
            Dim version = reader.ReadUInt16BigEndian()
            Dim length = reader.ReadUInt16BigEndian()
            Dim format = reader.ReadByte()
            Dim coverage = reader.ReadBitsBigEndian(1)


            Dim direction = If(coverage.Get(0), Kern.Direction.Horizontal, Kern.Direction.Vertical)
            Dim values = If(coverage.Get(1), Kern.Values.Minimum, Kern.Values.Kerning)
            Dim isCrossStream = coverage.Get(2)
            Dim isOverride = coverage.Get(3)

            ' The only format that is properly interpreted by Windows
            Dim format0 As Format0 = Nothing
            If format = 0 Then
                format0 = Format0.FromReader(reader)
            End If

            Return New KernSubtable(version, length, format, direction, values, isCrossStream, isOverride, format0)
        End Function

        Private Sub New(version As UShort, length As UShort, format As Byte, direction As Direction, values As Values, isCrossStream As Boolean, isOverride As Boolean, format0 As Format0)
            Me.Version = version
            Me.Length = length
            Me.Format = format
            Me.Direction = direction
            Me.Values = values
            Me.IsCrossStream = isCrossStream
            Me.IsOverride = isOverride
            Me.Format0 = format0
        End Sub

        Public ReadOnly Property Version As UShort
        Public ReadOnly Property Length As UShort
        Public ReadOnly Property Format As Byte
        Public ReadOnly Property Direction As Direction
        Public ReadOnly Property Values As Values
        Public ReadOnly Property IsCrossStream As Boolean
        Public ReadOnly Property IsOverride As Boolean
        Public ReadOnly Property Format0 As Format0

        Public Function GetKerning(pair As KerningPair) As Short
            Dim value As Short = Nothing
            If Format0 IsNot Nothing Then
                If Format0.Map.TryGetValue(pair, value) Then
                    Return value
                End If
            End If

            Return 0
        End Function
    End Class
End Namespace
