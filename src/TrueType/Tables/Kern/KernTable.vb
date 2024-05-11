Imports Microsoft.VisualBasic.Drawing.Fonts.TrueType.IO

Namespace Tables.Kern
    ''' <summary>
    ''' Contains adjustment to horizontal/vertical positions between glyphs
    ''' </summary>
    Public NotInheritable Class KernTable
        Public Shared Function FromReader(reader As FontReader) As KernTable
            Dim version = reader.ReadUInt16BigEndian()
            Dim subtableCount = reader.ReadUInt16BigEndian()

            Dim subtables = New List(Of KernSubtable)()
            For i = 0 To subtableCount - 1
                Dim subTable = KernSubtable.FromReader(reader)
                subtables.Add(subTable)
            Next

            Return New KernTable(version, subtableCount, subtables)
        End Function

        Public Shared ReadOnly Property Empty As KernTable
            Get
                Return New KernTable(0, 0, New List(Of KernSubtable)())
            End Get
        End Property

        Private Sub New(version As UShort, subtableCount As UShort, subtables As IReadOnlyList(Of KernSubtable))
            Me.Version = version
            Me.SubtableCount = subtableCount
            Me.Subtables = subtables
        End Sub

        Public ReadOnly Property Version As UShort
        Public ReadOnly Property SubtableCount As UShort
        Public ReadOnly Property Subtables As IReadOnlyList(Of KernSubtable)
    End Class
End Namespace
