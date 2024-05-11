Imports System

Namespace RoyT.TrueType
    Public Enum Platform As UShort
        Unicode = 0
        Macintosh = 1

        <Obsolete>
        ISO = 2

        Windows = 3
        Custom = 4
    End Enum
End Namespace
