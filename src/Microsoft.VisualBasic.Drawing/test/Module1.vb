Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports Microsoft.VisualBasic.Drawing
Imports Microsoft.VisualBasic.Imaging

Module ImagePreview

    Sub Main22222(args As String())
        If args.Length = 0 Then
            Console.WriteLine("Usage: ImagePreview <image_path> [width]")
            Return
        End If

        Dim imagePath As String = args(0)
        Dim terminalWidth As Integer = If(args.Length > 1, Integer.Parse(args(1)), Console.WindowWidth - 1)
        Dim preview As String = GenerateImagePreview(imagePath, terminalWidth)
        Console.Write(preview)
    End Sub

    Function GenerateImagePreview(imagePath As String, terminalWidth As Integer) As String
        Using img As Image = Image.FromFile(imagePath)
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
            Dim ansiBuilder As New Text.StringBuilder()

            For y As Integer = 0 To scaledImg.Height - 1
                For x As Integer = 0 To scaledImg.Width - 1
                    Dim pixel As Color = scaledImg.GetPixel(x, y)
                    ansiBuilder.Append(GetAnsiColorSequence(pixel))
                    ansiBuilder.Append(" ") ' 空格作为像素显示
                Next
                ansiBuilder.AppendLine(vbEscape & "[0m") ' 重置样式并换行
            Next

            Return ansiBuilder.ToString()
        End Using
    End Function

    Private Function GetAnsiColorSequence(color As Color) As String
        ' 使用真彩色 ANSI 序列 (24-bit RGB)
        Return $"{vbEscape}[48;2;{color.R};{color.G};{color.B}m"
    End Function

    ' 备用：256 色模式 (当终端不支持真彩色时)
    Private Function GetAnsi256ColorSequence(color As Color) As String
        Dim index As Integer = RgbToAnsi256(color.R, color.G, color.B)
        Return $"{vbEscape}[48;5;{index}m"
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

    ' ANSI 转义字符
    Private ReadOnly Property vbEscape As String
        Get
            Return ChrW(&H1B)
        End Get
    End Property
End Module