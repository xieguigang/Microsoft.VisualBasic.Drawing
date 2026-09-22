Imports System.Drawing
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.BitmapImage
Imports Bitmap = Microsoft.VisualBasic.Imaging.Bitmap
Imports Image = Microsoft.VisualBasic.Imaging.Image

''' <summary>
''' The pixel buffer converter between the direct2d gpu texture and the
''' managed raster image model.
''' </summary>
''' <remarks>
''' The pixel buffer that is read back from the gpu texture is a BGRA
''' ordered buffer with the premultiplied alpha value, and the raster image
''' buffer of <see cref="Bitmap"/> is also a BGRA ordered buffer, so the
''' only conversion that is required here is the alpha un-premultiply.
''' </remarks>
Friend Module DxPixelFormat

    ''' <summary>
    ''' convert the premultiplied BGRA pixel buffer into a straight BGRA buffer
    ''' </summary>
    ''' <param name="bgra">
    ''' the raw pixel buffer that is read back from the gpu texture, the
    ''' channel order of each pixel is [blue, green, red, alpha]
    ''' </param>
    ''' <remarks>
    ''' premultiplied = straight * alpha / 255, so that
    ''' straight = premultiplied * 255 / alpha
    ''' </remarks>
    Friend Function Unpremultiply(bgra As Byte()) As Byte()
        For i As Integer = 0 To bgra.Length - 4 Step 4
            Dim a As Integer = bgra(i + 3)

            If a = 255 OrElse a = 0 Then
                Continue For
            End If

            Dim scale As Integer = 255 * 256 \ a

            For c As Integer = 0 To 2
                Dim v As Integer = (bgra(i + c) * scale) >> 8

                If v > 255 Then
                    v = 255
                End If

                bgra(i + c) = CByte(v)
            Next
        Next

        Return bgra
    End Function

    ''' <summary>
    ''' wrap the raw BGRA pixel buffer as a managed raster image object
    ''' </summary>
    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Friend Function CreateBitmap(bgra As Byte(), width As Integer, height As Integer) As Bitmap
        Return New Bitmap(New BitmapBuffer(bgra, New Size(width, height), BitmapBuffer.TYPE_INT_ARGB))
    End Function

    ''' <summary>
    ''' extract the raw BGRA pixel buffer from a raster image object,
    ''' returns nothing when the image model is not supported.
    ''' </summary>
    Friend Function GetPixelBuffer(image As Image) As Byte()
        If image Is Nothing Then
            Return Nothing
        End If

        If TypeOf image Is Bitmap Then
            Dim buffer As BitmapBuffer = DirectCast(image, Bitmap).MemoryBuffer

            If buffer.GetPixelChannels() <> 4 Then
                Return Nothing
            End If

            Return buffer.RawBuffer
        End If

        ' the other image model (svg / pdf image wrapper) is not a raster buffer
        Dim raster = TryCast(image.GetMemoryBitmap(), BitmapBuffer)

        If raster Is Nothing Then
            Return Nothing
        End If

        Return raster.RawBuffer
    End Function
End Module
