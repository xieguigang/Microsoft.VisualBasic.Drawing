﻿Imports System.Drawing
Imports System.IO
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.BitmapImage
Imports SkiaSharp

Public Class SkiaImage : Inherits Image

    Public Overrides ReadOnly Property Size As Size
    Public ReadOnly Property Image As SKBitmap

    Sub New(canvas As Graphics)
        Call canvas.Dispose()

        Size = canvas.Size
        Image = canvas.m_surface
    End Sub

    Sub New(bitmap As SKBitmap)
        Me.Size = New Size(bitmap.Width, bitmap.Height)
        Me.Image = bitmap
    End Sub

    Sub New(size As Size, image As SKImage)
        Me.Size = size
        Me.Image = SKBitmap.FromImage(image)
    End Sub

    ''' <summary>
    ''' this method will try to replace the black pixel to transparent
    ''' </summary>
    ''' <returns></returns>
    Public Function SetTransparent() As SkiaImage
        Dim transparent As New SKColor(0, 0, 0, 0)

        For x As Integer = 0 To Size.Width - 1
            For y As Integer = 0 To Size.Height - 1
                Dim p As SKColor = Image.GetPixel(x, y)

                If p.Alpha = 255 AndAlso p.Red = 0 AndAlso p.Green = 0 AndAlso p.Blue = 0 Then
                    Call Image.SetPixel(x, y, transparent)
                End If
            Next
        Next

        Return Me
    End Function

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Overrides Sub Save(s As Stream, format As ImageFormats)
        Call SaveRasterImage(Image, s, format)
    End Sub

    Public Shared Function SaveRasterImage(image As SKBitmap, s As Stream, format As ImageFormats) As Boolean
        If format = ImageFormats.Bmp Then
            Dim m_data As New BitmapBuffer(image.Bytes, New Size(image.Width, image.Height), channel:=4)
            Dim bitmap As New Bitmap(m_data)

            Try
                Call bitmap.Save(s, ImageFormats.Bmp)
                Call s.Flush()
            Catch ex As Exception
                Call App.LogException(ex)
                Return False
            End Try
        Else
            Dim m_data = image.Encode(format.GetSkiaEncodeFormat, 100)

            Try
                Call m_data.SaveTo(s)
                Call s.Flush()
            Catch ex As Exception
                Call App.LogException(ex)
                Return False
            End Try
        End If

        Return True
    End Function

    Protected Overrides Function ConvertToBitmapStream() As MemoryStream
        Dim s As New MemoryStream
        Dim m_data As New BitmapBuffer(Image.Bytes, Size, channel:=4)
        Dim bitmap As New Bitmap(m_data)
        Call bitmap.Save(s, ImageFormats.Bmp)
        Call s.Seek(Scan0, SeekOrigin.Begin)
        Return s
    End Function

    Public Shared Narrowing Operator CType(img As SkiaImage) As Bitmap
        Return FromStream(img.ConvertToBitmapStream)
    End Operator
End Class
