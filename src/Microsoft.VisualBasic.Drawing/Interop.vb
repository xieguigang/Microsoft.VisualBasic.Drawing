﻿Imports System.Drawing
Imports System.IO
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Imaging
Imports SkiaSharp

''' <summary>
''' Helper for interop with gdi+ in .net-windows
''' </summary>
Public Module Interop

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    <Extension>
    Public Function AsSKColor(color As Color) As SKColor
        Return New SKColor(color.R, color.G, color.B, color.A)
    End Function

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    <Extension>
    Public Function TranslateSKColor(color As String) As SKColor
        Return color.TranslateColor.AsSKColor
    End Function

#If WINDOWS Then
    <Extension>
    Public Function SkiaToGdiPlusImage(skImage As SKImage) As Image
        Using data As SKData = skImage.Encode(SKEncodedImageFormat.Png, 100)
            Dim bytes As Byte() = data.ToArray

            Using ms As New MemoryStream(bytes)
                Return Image.FromFile(ms)
            End Using
        End Using
    End Function
#End If
End Module
