﻿Imports System.Drawing
Imports System.IO
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Driver
Imports SkiaSharp

''' <summary>
''' A wrapper of the skia sharp canvas object for raster image drawing
''' </summary>
Public Class Graphics : Inherits SkiaGraphics
    Implements GdiRasterGraphics

    Friend ReadOnly m_info As SKImageInfo
    Friend ReadOnly m_surface As SKSurface

    Public ReadOnly Property ImageResource As Image Implements GdiRasterGraphics.ImageResource
        Get
            Return New SkiaImage(Me)
        End Get
    End Property

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Sub New(width As Integer, height As Integer, Optional fill As String = "#ffffff")
        Call Me.New(width, height, TranslateColor(fill))
    End Sub

    Sub New(width As Integer, height As Integer, Optional fill As Color? = Nothing, Optional dpi As Integer = 100)
        Call MyBase.New(width, height, dpi)

        m_info = New SKImageInfo(width, height)
        m_surface = SKSurface.Create(m_info)
        m_canvas = m_surface.Canvas

        If fill Is Nothing Then
            Call Clear(Color.Transparent)
        Else
            Call Clear(fill)
        End If
    End Sub

    Public Function GetPixel(x As Integer, y As Integer) As Color
        Throw New NotImplementedException
    End Function

    Public Sub SetPixel(x As Integer, y As Integer, pixel As Color)
        Throw New NotImplementedException
    End Sub

    Public Overrides Function ToString() As String
        Return "raster_buffer: " & StringFormats.Lanudry(Width * Height * 4&)
    End Function

    Public Overloads Function Save(file As String, Optional format As ImageFormats = ImageFormats.Png) As Boolean
        Using s As Stream = file.Open(FileMode.OpenOrCreate, doClear:=True,)
            Return Save(s, format)
        End Using
    End Function

    Public Overloads Function Save(file As Stream, format As ImageFormats) As Boolean
        Dim image = m_surface.Snapshot
        Dim data = image.Encode(SKEncodedImageFormat.Png, 100)

        Try
            Call data.SaveTo(file)
            Call file.Flush()
        Catch ex As Exception
            Call App.LogException(ex)
            Return False
        End Try

        Return True
    End Function

    Public Overrides Sub Save(file As Stream)
        Call Save(file, format:=ImageFormats.Png)
    End Sub

    Public Overrides Sub Dispose()
        Call m_canvas.Dispose()
    End Sub
End Class
