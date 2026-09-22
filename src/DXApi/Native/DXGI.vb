Imports System.Runtime.InteropServices

Namespace Native

    ''' <summary>
    ''' The DXGI (dxgi.dll) interop declarations, only the subset that is required
    ''' for creating a shared surface between D3D11 and Direct2D is declared here.
    ''' </summary>
    Friend Module DXGI

        ''' <summary>
        ''' DXGI_FORMAT enumeration (subset)
        ''' </summary>
        Friend Enum DXGI_FORMAT As Integer
            UNKNOWN = 0
            R8G8B8A8_UNORM = 28
            B8G8R8A8_UNORM = 87
            B8G8R8X8_UNORM = 88
        End Enum

    End Module

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure DXGI_SAMPLE_DESC
        Public Count As UInteger
        Public Quality As UInteger
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure DXGI_SURFACE_DESC
        Public Width As UInteger
        Public Height As UInteger
        Public Format As Integer
        Public SampleDesc As DXGI_SAMPLE_DESC
    End Structure

    ''' <summary>
    ''' IDXGISurface: the dxgi surface object that is queried from the
    ''' d3d11 texture2d resource, Direct2D renders on top of this surface.
    ''' </summary>
    ''' <remarks>
    ''' this project never calls any method of this interface, it only
    ''' needs the interface pointer for <see cref="D2D1.CreateDxgiSurfaceRenderTarget"/>
    ''' so the vtable declaration is kept empty on purpose.
    ''' </remarks>
    <ComImport>
    <Guid("cafcb56c-6ac3-4889-bf47-9e23bbd260ec")>
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Friend Interface IDXGISurface
    End Interface
End Namespace
