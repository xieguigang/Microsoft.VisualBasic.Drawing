Imports System.Runtime.InteropServices

''' <summary>
''' The d3dcompiler (d3dcompiler_47.dll) interop declarations: the hlsl
''' compiler that turns the shader source of the 3d pipeline into byte code.
''' </summary>
''' <remarks>
''' d3dcompiler_47.dll is a system component of windows 8.1 and higher, it is
''' present in the system directory of every supported windows installation.
''' The library is loaded lazily by the p/invoke runtime, so a system without
''' the compiler only fails at the very moment when a shader is compiled: the
''' caller catches that failure and falls back to the direct2d rendering back
''' end, the viewer keeps working.
''' </remarks>
Friend Module D3DCompiler

    ''' <summary>
    ''' compile a hlsl shader into byte code
    ''' </summary>
    ''' <param name="srcData">the pointer of the ansi shader source text</param>
    ''' <param name="srcDataSize">the length of the shader source text in bytes</param>
    ''' <param name="sourceName">the name of the source, only used in error messages</param>
    ''' <param name="defines">a <c>D3D_SHADER_MACRO</c> array, or zero</param>
    ''' <param name="include">the include handler interface, or zero</param>
    ''' <param name="entryPoint">the name of the entry point function</param>
    ''' <param name="target">the shader profile, for example ``vs_4_0`` or ``ps_4_0``</param>
    ''' <param name="flags1">a combination of <see cref="D3DCOMPILE_FLAG"/></param>
    ''' <param name="flags2">the effect compiler flags, zero for a plain shader</param>
    ''' <param name="code">the compiled byte code blob</param>
    ''' <param name="errorMessages">the error blob, it must be released even on success</param>
    <DllImport("d3dcompiler_47.dll", EntryPoint:="D3DCompile", PreserveSig:=True, CharSet:=CharSet.Ansi)>
    Friend Function D3DCompile(
        srcData As IntPtr,
        srcDataSize As IntPtr,
        sourceName As String,
        defines As IntPtr,
        include As IntPtr,
        entryPoint As String,
        target As String,
        flags1 As UInteger,
        flags2 As UInteger,
        <Out> ByRef code As IntPtr,
        <Out> ByRef errorMessages As IntPtr
    ) As Integer
    End Function

    <Flags>
    Friend Enum D3DCOMPILE_FLAG As UInteger
        NONE = 0
        ''' <summary>the debug information is generated</summary>
        DEBUG = &H1
        SKIP_VALIDATION = &H2
        SKIP_OPTIMIZATION = &H4
        ''' <summary>the matrix constants are packed in row major order</summary>
        PACK_MATRIX_ROW_MAJOR = &H8
        ''' <summary>the matrix constants are packed in column major order, this is the default of the hlsl compiler</summary>
        PACK_MATRIX_COLUMN_MAJOR = &H10
        ''' <summary>the compiler rejects the code that is not strictly conforming</summary>
        ENABLE_STRICTNESS = &H800
        OPTIMIZATION_LEVEL0 = &H4000
        OPTIMIZATION_LEVEL1 = 0
        OPTIMIZATION_LEVEL2 = &HC000
        OPTIMIZATION_LEVEL3 = &H8000
        WARNINGS_ARE_ERRORS = &H40000
    End Enum
End Module

''' <summary>
''' ID3DBlob, the memory block that carries the compiled shader byte code or
''' the compiler error message.
''' </summary>
<ComImport>
<Guid("8ba5fb08-5195-40e2-ac58-0d989c3a0102")>
<InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
Friend Interface ID3DBlob

    ''' <summary>
    ''' the pointer of the data block, it is only valid while the blob is alive
    ''' </summary>
    <PreserveSig> Function GetBufferPointer() As IntPtr

    ''' <summary>
    ''' the size of the data block in bytes
    ''' </summary>
    <PreserveSig> Function GetBufferSize() As UInteger
End Interface
