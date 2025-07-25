Imports System.Drawing
Imports System.Runtime.CompilerServices
Imports System.Text
Imports Microsoft.VisualBasic.ApplicationServices.Terminal
Imports Microsoft.VisualBasic.Imaging

Public Module ANSI

    ' 在 GetAnsiColorSequence 中添加颜色模式切换
    Public Property useTrueColor As Boolean = True ' 根据终端支持切换

    Public Function GenerateImagePreview(imagePath As String, terminalWidth As Integer) As String
        Using img As Image = Image.FromFile(imagePath)
            Return img.GenerateImagePreview(terminalWidth)
        End Using
    End Function

    ''' <summary>
    ''' previews of the image on the terminal using ANSI escape codes.
    ''' </summary>
    ''' <param name="img"></param>
    ''' <param name="terminalWidth"></param>
    ''' <returns>This function generates a string that represents the image in a format suitable for terminal display.</returns>
    <Extension>
    Public Function GenerateImagePreview(img As Image, terminalWidth As Integer) As String
        ' 计算新尺寸 (保持宽高比)
        Dim aspectRatio As Single = img.Height / CSng(img.Width)
        Dim newHeight As Integer = CInt(terminalWidth * aspectRatio)
        Dim scaledImg As Bitmap

        ' 创建缩放后的位图
        Using gfx As New Graphics(terminalWidth, newHeight)
            gfx.DrawImage(img, 0, 0, terminalWidth, newHeight)
            gfx.Flush()
            scaledImg = DirectCast(gfx.ImageResource, SkiaImage).ToBitmap
        End Using

        ' 生成 ANSI 序列
        Dim ansiBuilder As New StringBuilder()
        Dim pixel As Color

        For y As Integer = 0 To scaledImg.Height - 1
            For x As Integer = 0 To scaledImg.Width - 1
                pixel = scaledImg.GetPixel(x, y)
                ansiBuilder.Append(If(useTrueColor, GetAnsiColorSequence(pixel), GetAnsi256ColorSequence(pixel)))
                ansiBuilder.Append(" ") ' 空格作为像素显示
            Next
            ansiBuilder.AppendLine(AnsiEscapeCodes.Escape & "[0m") ' 重置样式并换行
        Next

        Return ansiBuilder.ToString()
    End Function

    Private Function GetAnsiColorSequence(color As Color) As String
        ' 使用真彩色 ANSI 序列 (24-bit RGB)
        Return $"{AnsiEscapeCodes.Escape}[48;2;{color.R};{color.G};{color.B}m"
    End Function

    ' 备用：256 色模式 (当终端不支持真彩色时)
    Private Function GetAnsi256ColorSequence(color As Color) As String
        Dim index As Integer = RgbToAnsi256(color.R, color.G, color.B)
        Return $"{AnsiEscapeCodes.Escape}[48;5;{index}m"
    End Function

    Private Function RgbToAnsi256(r As Byte, g As Byte, b As Byte) As Byte
        ' 转换为 ANSI 256 色索引
        If r = g AndAlso g = b Then
            ' 灰度处理
            If r < 8 Then Return 16
            If r > 248 Then Return 231
            Return CByte(231 + (r - 8) / 10)
        Else
            ' RGB 立方体映射 (6x6x6)
            Return CByte(16 +
                (CInt(r / 51) * 36) +
                (CInt(g / 51) * 6) +
                CInt(b / 51))
        End If
    End Function
End Module
