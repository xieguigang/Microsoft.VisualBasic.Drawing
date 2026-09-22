Imports System.Runtime.InteropServices

''' <summary>
''' The gpu device wrapper of the directx engine: a d3d11 device with the
''' BGRA support flag, its immediate context, the direct2d factory and the
''' directwrite factory.
''' </summary>
''' <remarks>
''' The device object is an expensive resource, so it is shared inside the
''' current process: every <see cref="DxGraphics"/> canvas just creates its
''' own off-screen render target on top of the shared device.
'''
''' All of the native api here is imported via p/invoke, this module is
''' 100% pure managed code.
''' </remarks>
Friend Class DxDevice : Implements IDisposable

    Friend ReadOnly Property Device As ID3D11Device
    Friend ReadOnly Property Context As ID3D11DeviceContext
    Friend ReadOnly Property Factory2D As ID2D1Factory
    Friend ReadOnly Property FactoryWrite As IDWriteFactory
    Friend ReadOnly Property DriverType As D3D_DRIVER_TYPE
    Friend ReadOnly Property FeatureLevel As UInteger

    ''' <summary>
    ''' a short description of the current gpu device, for debug and benchmark
    ''' </summary>
    Friend ReadOnly Property Description As String

    Private m_disposed As Boolean = False

    Private Shared m_default As DxDevice = Nothing
    Private Shared ReadOnly syncRoot As New Object

    ''' <summary>
    ''' get the process wide shared directx device, the hardware driver is
    ''' preferred and the WARP software rasterizer is used as the fallback
    ''' when there is no gpu device available.
    ''' </summary>
    Friend Shared ReadOnly Property [Default] As DxDevice
        Get
            If m_default Is Nothing Then
                SyncLock syncRoot
                    If m_default Is Nothing Then
                        m_default = New DxDevice()
                    End If
                End SyncLock
            End If

            Return m_default
        End Get
    End Property

    Private Sub New()
        Dim levels As UInteger() = {
            CUInt(D3D_FEATURE_LEVEL.LEVEL_11_1),
            CUInt(D3D_FEATURE_LEVEL.LEVEL_11_0),
            CUInt(D3D_FEATURE_LEVEL.LEVEL_10_1),
            CUInt(D3D_FEATURE_LEVEL.LEVEL_10_0),
            CUInt(D3D_FEATURE_LEVEL.LEVEL_9_3),
            CUInt(D3D_FEATURE_LEVEL.LEVEL_9_2),
            CUInt(D3D_FEATURE_LEVEL.LEVEL_9_1)
        }
        Dim device As ID3D11Device = Nothing
        Dim context As ID3D11DeviceContext = Nothing
        Dim featureLevel As UInteger = 0
        Dim hr As Integer = D3D11.D3D11CreateDevice(
            IntPtr.Zero,
            D3D_DRIVER_TYPE.HARDWARE,
            IntPtr.Zero,
            CUInt(D3D11_CREATE_DEVICE_FLAG.BGRA_SUPPORT),
            levels, CUInt(levels.Length),
            D3D11.D3D11_SDK_VERSION,
            device, featureLevel, context
        )

        If hr < 0 Then
            ' no gpu device available, fallback to the WARP software rasterizer
            device = Nothing
            context = Nothing

            hr = D3D11.D3D11CreateDevice(
                IntPtr.Zero,
                D3D_DRIVER_TYPE.WARP,
                IntPtr.Zero,
                CUInt(D3D11_CREATE_DEVICE_FLAG.BGRA_SUPPORT),
                levels, CUInt(levels.Length),
                D3D11.D3D11_SDK_VERSION,
                device, featureLevel, context
            )
        End If

        Call ThrowIfFailed(hr, "D3D11CreateDevice")

        _Device = device
        _Context = context
        _FeatureLevel = featureLevel
        _DriverType = If(featureLevel >= &HB000, D3D_DRIVER_TYPE.HARDWARE, D3D_DRIVER_TYPE.WARP)

        ' create the direct2d factory
        Dim factory2d As ID2D1Factory = Nothing
        Dim opts As New D2D1_FACTORY_OPTIONS With {.debugLevel = D2D1_DEBUG_LEVEL.NONE}
        Dim iid2d As Guid = GetType(ID2D1Factory).GUID

        Call ThrowIfFailed(
            D2D1.D2D1CreateFactory(D2D1_FACTORY_TYPE.MULTI_THREADED, iid2d, opts, factory2d),
            "D2D1CreateFactory"
        )

        _Factory2D = factory2d

        ' create the directwrite factory
        Dim factoryWrite As IDWriteFactory = Nothing

        Call ThrowIfFailed(
            DWrite.DWriteCreateFactory(DWRITE_FACTORY_TYPE.ISOLATED, DWrite.IID_IDWriteFactory, factoryWrite),
            "DWriteCreateFactory"
        )

        _FactoryWrite = factoryWrite
        _Description = $"{DriverType.ToString}(feature_level=0x{featureLevel:X4})"
    End Sub

    Private Sub Dispose(disposing As Boolean)
        If m_disposed Then
            Return
        End If

        m_disposed = True

        SafeRelease(_FactoryWrite)
        SafeRelease(_Factory2D)
        SafeRelease(_Context)
        SafeRelease(_Device)
    End Sub

    Protected Overrides Sub Finalize()
        Call Dispose(False)
        MyBase.Finalize()
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        Call Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub
End Class
