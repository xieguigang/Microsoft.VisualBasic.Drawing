Imports System
Imports System.Collections.Generic
Imports System.Globalization

Namespace Tables.Name
    Public Module LanguageIdConverter
        Private ReadOnly LCIDMap As Dictionary(Of Integer, String) = ReadMapFromStringResource(Properties.Resources.LCIDMap)
        Private ReadOnly MacLanguageCodeMap As Dictionary(Of Integer, String) = ReadMapFromStringResource(Properties.Resources.MacLanguageCodeMap)


        Public Function ToCulture(platform As Platform, languageId As UShort) As CultureInfo
            Dim culture As String = Nothing
            If platform = Platform.Windows Then
                If LCIDMap.TryGetValue(languageId, culture) Then
                    Return New CultureInfo(culture)
                End If
            End If
            Dim culture As String = Nothing

            If platform = Platform.Macintosh Then
                If MacLanguageCodeMap.TryGetValue(languageId, culture) Then
                    Return New CultureInfo(culture)
                End If
            End If

            Return CultureInfo.InvariantCulture
        End Function

        Private Function ReadMapFromStringResource(resouce As String) As Dictionary(Of Integer, String)
            Dim map = New Dictionary(Of Integer, String)()

            Dim lines = resouce.Split({Microsoft.VisualBasic.Constants.vbCrLf, Microsoft.VisualBasic.Constants.vbLf}, StringSplitOptions.RemoveEmptyEntries)
            For Each line In lines
                Dim parts = line.Split(" "c)
                Dim code As Integer

                If parts(0).StartsWith("0x", StringComparison.OrdinalIgnoreCase) Then
                    code = Convert.ToInt32(parts(0).Substring(2), 16)
                Else
                    code = Integer.Parse(parts(0))
                End If

                map.Add(code, parts(1))
            Next

            Return map
        End Function
    End Module
End Namespace
