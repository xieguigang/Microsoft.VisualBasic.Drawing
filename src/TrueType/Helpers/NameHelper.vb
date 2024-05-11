Imports System.Globalization
Imports System.Linq
Imports RoyT.TrueType.Tables.Name

Namespace RoyT.TrueType.Helpers
    Public Module NameHelper
        Public Function GetName(nameId As NameId, culture As CultureInfo, font As TrueTypeFont) As String
            Dim candidates = font.NameTable.NameRecords.Where(Function(r) r.NameId = nameId AndAlso r.Language.Equals(culture)).ToList()

            If candidates.Any() Then

                ' Prefer the Windows platform as the text has a rich encoding (UTF-16)
                If candidates.Any(Function(x) x.PlatformId = Platform.Windows) Then
                    Return Enumerable.First(candidates, Function(x) x.PlatformId = Platform.Windows).Text
                End If

                ' Fallback to any platform
                Return Enumerable.First(candidates).Text

            End If

            Return String.Empty
        End Function
    End Module
End Namespace
