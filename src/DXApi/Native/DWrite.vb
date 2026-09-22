Imports System.Runtime.InteropServices

Namespace Native

    ''' <summary>
    ''' The DirectWrite (dwrite.dll) interop declarations, used for the text
    ''' layout and the text metrics calculation.
    ''' </summary>
    Friend Module DWrite

        Friend ReadOnly IID_IDWriteFactory As New Guid("b859ee5a-d838-4b5b-a2e8-1adc7d93db48")

        Friend Enum DWRITE_FACTORY_TYPE As Integer
            SHARED = 0
            ISOLATED = 1
        End Enum

        Friend Enum DWRITE_FONT_WEIGHT As Integer
            THIN = 100
            EXTRA_LIGHT = 200
            LIGHT = 300
            NORMAL = 400
            MEDIUM = 500
            SEMI_BOLD = 600
            BOLD = 700
            EXTRA_BOLD = 800
            BLACK = 900
        End Enum

        Friend Enum DWRITE_FONT_STYLE As Integer
            NORMAL = 0
            OBLIQUE = 1
            ITALIC = 2
        End Enum

        Friend Enum DWRITE_FONT_STRETCH As Integer
            UNDEFINED = 0
            ULTRA_CONDENSED = 1
            EXTRA_CONDENSED = 2
            CONDENSED = 3
            SEMI_CONDENSED = 4
            NORMAL = 5
            SEMI_EXPANDED = 6
            EXPANDED = 7
            EXTRA_EXPANDED = 8
            ULTRA_EXPANDED = 9
        End Enum

        Friend Enum DWRITE_TEXT_ALIGNMENT As Integer
            LEADING = 0
            TRAILING = 1
            CENTER = 2
            JUSTIFIED = 3
        End Enum

        Friend Enum DWRITE_PARAGRAPH_ALIGNMENT As Integer
            NEAR = 0
            FAR = 1
            CENTER = 2
        End Enum

        Friend Enum DWRITE_WORD_WRAPPING As Integer
            WRAP = 0
            NO_WRAP = 1
            EMERGENCY_BREAK = 2
            WHOLE_WORD = 3
            CHARACTER = 4
        End Enum

        Friend Enum DWRITE_MEASURING_MODE As Integer
            NATURAL = 0
            GDI_CLASSIC = 1
            GDI_NATURAL = 2
        End Enum

        ''' <summary>
        ''' create the directwrite factory object
        ''' </summary>
        <DllImport("dwrite.dll", EntryPoint:="DWriteCreateFactory", PreserveSig:=True)>
        Friend Function DWriteCreateFactory(
            factoryType As DWRITE_FACTORY_TYPE,
            <[In]> ByRef riid As Guid,
            <Out> ByRef factory As IDWriteFactory
        ) As Integer
        End Function

    End Module

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure DWRITE_TEXT_METRICS
        Public left As Single
        Public top As Single
        Public width As Single
        Public height As Single
        Public widthIncludingTrailingWhitespace As Single
        Public heightIncludingTrailingWhitespace As Single
        Public layoutWidth As Single
        Public layoutHeight As Single
        Public maxBidiReorderingDepth As UInteger
        Public lineCount As UInteger
    End Structure

    <ComImport>
    <Guid("9c906818-31d7-4fd3-a151-7c5e225db55a")>
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Friend Interface IDWriteTextFormat
        ' slot 3
        <PreserveSig> Function SetTextAlignment(alignment As Integer) As Integer
        ' slot 4
        <PreserveSig> Function SetParagraphAlignment(alignment As Integer) As Integer
        ' slot 5
        <PreserveSig> Function SetWordWrapping(wrapping As Integer) As Integer
        ' slot 6
        <PreserveSig> Function SetReadingDirection(direction As Integer) As Integer
        ' slot 7
        <PreserveSig> Function SetFlowDirection(direction As Integer) As Integer
        ' slot 8
        <PreserveSig> Function SetIncrementalTabStop(tabStop As Single) As Integer
        ' slot 9
        <PreserveSig> Function SetTrimming(trimming As IntPtr, trimmingSign As IntPtr) As Integer
        ' slot 10
        <PreserveSig> Function SetLineSpacing(method As Integer, lineSpacing As Single, baseline As Single) As Integer
        ' slot 11
        <PreserveSig> Function GetTextAlignment() As Integer
        ' slot 12
        <PreserveSig> Function GetParagraphAlignment() As Integer
        ' slot 13
        <PreserveSig> Function GetWordWrapping() As Integer
        ' slot 14
        <PreserveSig> Function GetReadingDirection() As Integer
        ' slot 15
        <PreserveSig> Function GetFlowDirection() As Integer
        ' slot 16
        <PreserveSig> Function GetIncrementalTabStop() As Single
        ' slot 17
        <PreserveSig> Function GetTrimming(trimming As IntPtr, ByRef sign As IntPtr) As Integer
        ' slot 18
        <PreserveSig> Function GetLineSpacing(ByRef method As Integer, ByRef lineSpacing As Single, ByRef baseline As Single) As Integer
        ' slot 19
        <PreserveSig> Function GetFontCollection(ByRef collection As IntPtr) As Integer
        ' slot 20
        <PreserveSig> Function GetFontFamilyNameLength() As UInteger
        ' slot 21
        <PreserveSig> Function GetFontFamilyName(fontFamilyName As IntPtr, nameSize As UInteger) As Integer
        ' slot 22
        <PreserveSig> Function GetFontWeight() As Integer
        ' slot 23
        <PreserveSig> Function GetFontStyle() As Integer
        ' slot 24
        <PreserveSig> Function GetFontStretch() As Integer
        ' slot 25
        <PreserveSig> Function GetFontSize() As Single
        ' slot 26
        <PreserveSig> Function GetLocaleNameLength() As UInteger
        ' slot 27
        <PreserveSig> Function GetLocaleName(localeName As IntPtr, nameSize As UInteger) As Integer
    End Interface

    <ComImport>
    <Guid("53737037-6d14-410b-9bfe-0b182fb709a6")>
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Friend Interface IDWriteTextLayout : Inherits IDWriteTextFormat

        ' slot 28
        <PreserveSig> Function SetMaxWidth(maxWidth As Single) As Integer
        ' slot 29
        <PreserveSig> Function SetMaxHeight(maxHeight As Single) As Integer
        ' slot 30
        <PreserveSig> Function SetFontCollection(collection As IntPtr, range As IntPtr) As Integer
        ' slot 31
        <PreserveSig> Function SetFontFamilyName(<MarshalAs(UnmanagedType.LPWStr)> name As String, range As IntPtr) As Integer
        ' slot 32
        <PreserveSig> Function SetFontWeight(weight As Integer, range As IntPtr) As Integer
        ' slot 33
        <PreserveSig> Function SetFontStyle(style As Integer, range As IntPtr) As Integer
        ' slot 34
        <PreserveSig> Function SetFontStretch(stretch As Integer, range As IntPtr) As Integer
        ' slot 35
        <PreserveSig> Function SetFontSize(fontSize As Single, range As IntPtr) As Integer
        ' slot 36
        <PreserveSig> Function SetUnderline(hasUnderline As Integer, range As IntPtr) As Integer
        ' slot 37
        <PreserveSig> Function SetStrikethrough(hasStrikethrough As Integer, range As IntPtr) As Integer
        ' slot 38
        <PreserveSig> Function SetDrawingEffect(drawingEffect As IntPtr, range As IntPtr) As Integer
        ' slot 39
        <PreserveSig> Function SetInlineObject(inlineObject As IntPtr, range As IntPtr) As Integer
        ' slot 40
        <PreserveSig> Function SetTypography(typography As IntPtr, range As IntPtr) As Integer
        ' slot 41
        <PreserveSig> Function SetLocaleName(<MarshalAs(UnmanagedType.LPWStr)> name As String, range As IntPtr) As Integer
        ' slot 42
        <PreserveSig> Function GetMaxWidth() As Single
        ' slot 43
        <PreserveSig> Function GetMaxHeight() As Single
        ' slot 44
        <PreserveSig> Function GetFontCollection(position As UInteger, ByRef collection As IntPtr, ByRef range As IntPtr) As Integer
        ' slot 45
        <PreserveSig> Function GetFontFamilyNameLength(position As UInteger, ByRef length As UInteger, ByRef range As IntPtr) As Integer
        ' slot 46
        <PreserveSig> Function GetFontFamilyName(position As UInteger, name As IntPtr, nameSize As UInteger, ByRef range As IntPtr) As Integer
        ' slot 47
        <PreserveSig> Function GetFontWeight(position As UInteger, ByRef weight As Integer, ByRef range As IntPtr) As Integer
        ' slot 48
        <PreserveSig> Function GetFontStyle(position As UInteger, ByRef style As Integer, ByRef range As IntPtr) As Integer
        ' slot 49
        <PreserveSig> Function GetFontStretch(position As UInteger, ByRef stretch As Integer, ByRef range As IntPtr) As Integer
        ' slot 50
        <PreserveSig> Function GetFontSize(position As UInteger, ByRef size As Single, ByRef range As IntPtr) As Integer
        ' slot 51
        <PreserveSig> Function GetUnderline(position As UInteger, ByRef hasUnderline As Integer, ByRef range As IntPtr) As Integer
        ' slot 52
        <PreserveSig> Function GetStrikethrough(position As UInteger, ByRef hasStrikethrough As Integer, ByRef range As IntPtr) As Integer
        ' slot 53
        <PreserveSig> Function GetDrawingEffect(position As UInteger, ByRef effect As IntPtr, ByRef range As IntPtr) As Integer
        ' slot 54
        <PreserveSig> Function GetInlineObject(position As UInteger, ByRef inlineObject As IntPtr, ByRef range As IntPtr) As Integer
        ' slot 55
        <PreserveSig> Function GetTypography(position As UInteger, ByRef typography As IntPtr, ByRef range As IntPtr) As Integer
        ' slot 56
        <PreserveSig> Function GetLocaleNameLength(position As UInteger, ByRef length As UInteger, ByRef range As IntPtr) As Integer
        ' slot 57
        <PreserveSig> Function GetLocaleName(position As UInteger, localeName As IntPtr, nameSize As UInteger, ByRef range As IntPtr) As Integer
        ' slot 58
        <PreserveSig> Function Draw(clientDrawingContext As IntPtr, renderer As IntPtr, originX As Single, originY As Single) As Integer
        ' slot 59
        <PreserveSig> Function GetLineMetrics(lineMetrics As IntPtr, maxLineCount As UInteger, ByRef actualLineCount As UInteger) As Integer
        ' slot 60
        <PreserveSig> Function GetMetrics(ByRef metrics As DWRITE_TEXT_METRICS) As Integer
    End Interface

    <ComImport>
    <Guid("b859ee5a-d838-4b5b-a2e8-1adc7d93db48")>
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Friend Interface IDWriteFactory

        ' slot 3
        <PreserveSig> Function GetSystemFontCollection(ByRef collection As IntPtr, checkForUpdates As Integer) As Integer
        ' slot 4
        <PreserveSig> Function CreateCustomFontCollection(loader As IntPtr, key As IntPtr, keySize As UInteger, ByRef collection As IntPtr) As Integer
        ' slot 5
        <PreserveSig> Function RegisterFontCollectionLoader(loader As IntPtr) As Integer
        ' slot 6
        <PreserveSig> Function UnregisterFontCollectionLoader(loader As IntPtr) As Integer
        ' slot 7
        <PreserveSig> Function CreateFontFileReference(<MarshalAs(UnmanagedType.LPWStr)> path As String, fileTime As IntPtr, ByRef fontFile As IntPtr) As Integer
        ' slot 8
        <PreserveSig> Function CreateCustomFontFileReference(key As IntPtr, keySize As UInteger, loader As IntPtr, ByRef fontFile As IntPtr) As Integer
        ' slot 9
        <PreserveSig> Function CreateFontFace(faceType As Integer, numberOfFiles As UInteger, fontFiles As IntPtr, faceIndex As UInteger, simulations As Integer, ByRef fontFace As IntPtr) As Integer
        ' slot 10
        <PreserveSig> Function CreateRenderingParams(ByRef renderingParams As IntPtr) As Integer
        ' slot 11
        <PreserveSig> Function CreateMonitorRenderingParams(monitor As IntPtr, ByRef renderingParams As IntPtr) As Integer
        ' slot 12
        <PreserveSig> Function CreateCustomRenderingParams(gamma As Single, enhancedContrast As Single, clearTypeLevel As Single, pixelGeometry As Integer, renderingMode As Integer, ByRef renderingParams As IntPtr) As Integer
        ' slot 13
        <PreserveSig> Function RegisterFontFileLoader(loader As IntPtr) As Integer
        ' slot 14
        <PreserveSig> Function UnregisterFontFileLoader(loader As IntPtr) As Integer
        ' slot 15
        <PreserveSig> Function CreateTextFormat(
            <MarshalAs(UnmanagedType.LPWStr)> fontFamilyName As String,
            fontCollection As IntPtr,
            weight As Integer,
            style As Integer,
            stretch As Integer,
            fontSize As Single,
            <MarshalAs(UnmanagedType.LPWStr)> localeName As String,
            <Out> ByRef textFormat As IDWriteTextFormat) As Integer
        ' slot 16
        <PreserveSig> Function CreateTypography(ByRef typography As IntPtr) As Integer
        ' slot 17
        <PreserveSig> Function GetGdiInterop(ByRef gdiInterop As IntPtr) As Integer
        ' slot 18
        <PreserveSig> Function CreateTextLayout(
            <MarshalAs(UnmanagedType.LPWStr)> text As String,
            length As UInteger,
            textFormat As IDWriteTextFormat,
            maxWidth As Single,
            maxHeight As Single,
            <Out> ByRef textLayout As IDWriteTextLayout) As Integer
    End Interface
End Namespace
