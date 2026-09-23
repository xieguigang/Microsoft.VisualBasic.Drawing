Imports System.Drawing
Imports System.Numerics
Imports System.Runtime.InteropServices
Imports Microsoft.VisualBasic.Imaging.Drawing3D
Imports std = System.Math

Namespace Scene3D

    ''' <summary>
    ''' The gpu state and the draw batches of the direct3d 11 back end of the 3d
    ''' scene pipeline.
    ''' </summary>
    ''' <remarks>
    ''' The pipeline owns everything that is independent of the render target: the
    ''' shaders, the input layouts, the rasterizer / depth / blend states, the
    ''' constant buffer of the frame and the cached gpu geometry of the scene.
    '''
    ''' A frame is one call of <see cref="BeginFrame"/>, a handful of draw
    ''' batches and the final <see cref="EndFrame"/>. The per frame work of the
    ''' cpu is therefore a constant: one constant buffer update per batch and the
    ''' draw calls themselves, no projection, no polygon path and no sorting.
    ''' </remarks>
    Friend NotInheritable Class D3D11ScenePipeline : Implements IDisposable

        Private ReadOnly m_device As DxDevice

        Private ReadOnly m_surfaceLayout As IntPtr
        Private ReadOnly m_positionLayout As IntPtr
        Private ReadOnly m_pointLayout As IntPtr

        Private ReadOnly m_surfaceVertex As IntPtr
        Private ReadOnly m_surfacePixel As IntPtr
        Private ReadOnly m_positionVertex As IntPtr
        Private ReadOnly m_unlitPixel As IntPtr
        Private ReadOnly m_pointVertex As IntPtr
        Private ReadOnly m_pointPixel As IntPtr
        Private ReadOnly m_blitVertex As IntPtr
        Private ReadOnly m_blitPixel As IntPtr

        Private ReadOnly m_constantBuffer As IntPtr
        Private ReadOnly m_sampler As IntPtr

        Private ReadOnly m_blendAlpha As IntPtr
        Private ReadOnly m_depthWrite As IntPtr
        Private ReadOnly m_depthDisabled As IntPtr

        Private ReadOnly m_rasterSolid As IntPtr
        Private ReadOnly m_rasterSolidCulled As IntPtr
        Private ReadOnly m_rasterWireframe As IntPtr

        Private ReadOnly m_scratchSingle As IntPtr
        Private ReadOnly m_scratchBuffers As IntPtr
        Private ReadOnly m_scratchStrides As IntPtr
        Private ReadOnly m_scratchOffsets As IntPtr
        Private ReadOnly m_scratchClear As IntPtr
        Private ReadOnly m_scratchViewport As IntPtr

        Private m_constants As SceneConstants
        Private m_geometry As GpuSceneGeometry = Nothing
        Private m_geometryScene As Scene = Nothing
        Private m_sampleProbe As String = ""
        Private m_disposed As Boolean = False

        ''' <summary>
        ''' the outcome of the multi sample probe of the gpu device, for
        ''' diagnostics
        ''' </summary>
        Friend ReadOnly Property SampleProbe As String
            Get
                Return m_sampleProbe
            End Get
        End Property

        ''' <summary>
        ''' the gpu device that this pipeline belongs to
        ''' </summary>
        Friend ReadOnly Property Device As DxDevice
            Get
                Return m_device
            End Get
        End Property

        Friend Sub New(device As DxDevice)
            m_device = device

            ' the shaders are compiled at run time, a failure here means that the
            ' gpu back end is not available on this machine and the caller has
            ' to fall back to the polygon painter
            Dim surfaceVertexCode As Byte() = Scene3DShaders.Compile(Scene3DShaders.EntrySurfaceVertex, Scene3DShaders.VertexProfile)
            Dim surfacePixelCode As Byte() = Scene3DShaders.Compile(Scene3DShaders.EntrySurfacePixel, Scene3DShaders.PixelProfile)
            Dim positionVertexCode As Byte() = Scene3DShaders.Compile(Scene3DShaders.EntryPositionVertex, Scene3DShaders.VertexProfile)
            Dim unlitPixelCode As Byte() = Scene3DShaders.Compile(Scene3DShaders.EntryUnlitPixel, Scene3DShaders.PixelProfile)
            Dim pointVertexCode As Byte() = Scene3DShaders.Compile(Scene3DShaders.EntryPointVertex, Scene3DShaders.VertexProfile)
            Dim pointPixelCode As Byte() = Scene3DShaders.Compile(Scene3DShaders.EntryPointPixel, Scene3DShaders.PixelProfile)
            Dim blitVertexCode As Byte() = Scene3DShaders.Compile(Scene3DShaders.EntryBlitVertex, Scene3DShaders.VertexProfile)
            Dim blitPixelCode As Byte() = Scene3DShaders.Compile(Scene3DShaders.EntryBlitPixel, Scene3DShaders.PixelProfile)

            m_surfaceVertex = CreateVertexShader(surfaceVertexCode)
            m_surfacePixel = CreatePixelShader(surfacePixelCode)
            m_positionVertex = CreateVertexShader(positionVertexCode)
            m_unlitPixel = CreatePixelShader(unlitPixelCode)
            m_pointVertex = CreateVertexShader(pointVertexCode)
            m_pointPixel = CreatePixelShader(pointPixelCode)
            m_blitVertex = CreateVertexShader(blitVertexCode)
            m_blitPixel = CreatePixelShader(blitPixelCode)

            m_surfaceLayout = CreateInputLayout(Scene3DInputLayout.SurfaceElements, surfaceVertexCode)
            m_positionLayout = CreateInputLayout(Scene3DInputLayout.PositionElements, positionVertexCode)
            m_pointLayout = CreateInputLayout(Scene3DInputLayout.PointElements, pointVertexCode)

            m_constantBuffer = CreateConstantBuffer(Marshal.SizeOf(GetType(SceneConstants)))
            m_sampler = CreateSampler()
            m_blendAlpha = CreateBlendState()
            m_depthWrite = CreateDepthStencilState(True)
            m_depthDisabled = CreateDepthStencilState(False)
            m_rasterSolid = CreateRasterizerState(D3D11_FILL_MODE.SOLID, D3D11_CULL_MODE.NONE)
            m_rasterSolidCulled = CreateRasterizerState(D3D11_FILL_MODE.SOLID, D3D11_CULL_MODE.BACK)
            m_rasterWireframe = CreateRasterizerState(D3D11_FILL_MODE.WIREFRAME, D3D11_CULL_MODE.NONE)

            m_scratchSingle = Marshal.AllocHGlobal(IntPtr.Size)
            m_scratchBuffers = Marshal.AllocHGlobal(IntPtr.Size * 2)
            m_scratchStrides = Marshal.AllocHGlobal(4 * 2)
            m_scratchOffsets = Marshal.AllocHGlobal(4 * 2)
            m_scratchClear = Marshal.AllocHGlobal(4 * 4)
            m_scratchViewport = Marshal.AllocHGlobal(6 * 4)
        End Sub

        ''' <summary>
        ''' the gpu geometry of the scene, it is rebuilt only when the revision of
        ''' the scene, the scene itself or the coloring options change
        ''' </summary>
        ''' <remarks>
        ''' the scene object is part of the identity as well: a shared back end
        ''' instance that serves two canvases must not rebuild the geometry of one
        ''' canvas with the model of the other one
        ''' </remarks>
        Friend Function GeometryOf(scene As Scene, options As SceneRenderOptions) As GpuSceneGeometry
            Dim signature As String = $"{scene.Version}|{options.GeometrySignature()}"

            If m_geometry IsNot Nothing AndAlso m_geometryScene Is scene AndAlso m_geometry.Signature = signature Then
                Return m_geometry
            End If

            Dim stale As GpuSceneGeometry = m_geometry

            m_geometry = New GpuSceneGeometry(m_device, scene, options, signature)
            m_geometryScene = scene

            If stale IsNot Nothing Then
                stale.Dispose()
            End If

            Return m_geometry
        End Function

        ''' <summary>
        ''' the number of the samples that the gpu device supports for the color
        ''' buffer of the 3d pipeline
        ''' </summary>
        ''' <param name="requested">the requested sample count</param>
        ''' <returns>the granted sample count, one when the request can not be met</returns>
        Friend Function ResolveSampleCount(requested As Integer) As Integer
            m_sampleProbe = $"requested={requested}"

            If requested <= 1 Then
                Return 1
            End If

            Dim levels As UInteger = 0

            Try
                Call ThrowIfFailed(
                    m_device.Device.CheckMultisampleQualityLevels(
                        CInt(DXGI_FORMAT.B8G8R8A8_UNORM), CUInt(requested), levels),
                    "ID3D11Device::CheckMultisampleQualityLevels")
            Catch ex As Exception
                m_sampleProbe &= $", the device rejected the probe: {ex.Message}"

                Return 1
            End Try

            m_sampleProbe &= $", quality levels={levels}"

            If levels = 0 Then
                m_sampleProbe &= ", not supported by the device"

                Return 1
            End If

            Return requested
        End Function

        ''' <summary>
        ''' open one frame: the viewport and the constant buffer are bound to the
        ''' pipeline, the geometry is prepared
        ''' </summary>
        Friend Sub BeginFrame(screenSize As Size)
            Dim context As ID3D11DeviceContext = m_device.Context
            Dim viewport As New D3D11_VIEWPORT With {
                .TopLeftX = 0,
                .TopLeftY = 0,
                .Width = screenSize.Width,
                .Height = screenSize.Height,
                .MinDepth = 0,
                .MaxDepth = 1
            }

            Call Marshal.StructureToPtr(viewport, m_scratchViewport, False)
            Call context.RSSetViewports(1UI, m_scratchViewport)

            ' the constant buffer is bound to both the vertex and the pixel stage:
            ' the pixel shader evaluates the lambert term and the heat map lookup
            Call Marshal.WriteIntPtr(m_scratchSingle, 0, m_constantBuffer)
            Call context.VSSetConstantBuffers(0UI, 1UI, m_scratchSingle)
            Call context.PSSetConstantBuffers(0UI, 1UI, m_scratchSingle)
            Call Marshal.WriteIntPtr(m_scratchSingle, 0, m_sampler)
            Call context.PSSetSamplers(0UI, 1UI, m_scratchSingle)
        End Sub

        ''' <summary>
        ''' finish the frame: every stage is unbound again so that the direct2d
        ''' overlay that follows is not affected by the 3d state
        ''' </summary>
        Friend Sub EndFrame(restoreSize As Size)
            Dim context As ID3D11DeviceContext = m_device.Context

            Call context.IASetInputLayout(IntPtr.Zero)
            Call context.IASetPrimitiveTopology(CInt(D3D11_PRIMITIVE_TOPOLOGY.UNDEFINED))
            Call context.VSSetShader(IntPtr.Zero, IntPtr.Zero, 0UI)
            Call context.PSSetShader(IntPtr.Zero, IntPtr.Zero, 0UI)
            Call context.OMSetBlendState(IntPtr.Zero, IntPtr.Zero, &HFFFFFFFFUI)
            Call context.OMSetDepthStencilState(IntPtr.Zero, 0UI)
            Call context.RSSetState(IntPtr.Zero)

            Dim viewport As New D3D11_VIEWPORT With {
                .TopLeftX = 0,
                .TopLeftY = 0,
                .Width = restoreSize.Width,
                .Height = restoreSize.Height,
                .MinDepth = 0,
                .MaxDepth = 1
            }

            Call Marshal.StructureToPtr(viewport, m_scratchViewport, False)
            Call context.RSSetViewports(1UI, m_scratchViewport)
        End Sub

        ''' <summary>
        ''' fill the constant buffer with the values of the current frame
        ''' </summary>
        ''' <param name="geometry">the geometry that is about to be drawn</param>
        ''' <param name="transform">the camera matrices of the frame</param>
        ''' <param name="camera">the camera of the frame</param>
        ''' <param name="options">the presentation options of the frame</param>
        ''' <param name="heatRange">
        ''' the lowest heat value and the inverse of the heat range, the lit point
        ''' mode of a model normalizes its lambert factor with it
        ''' </param>
        Friend Sub SetScene(geometry As GpuSceneGeometry, transform As SceneTransform, camera As Camera,
                            options As SceneRenderOptions, heatRange As Vector2)

            Dim light As Point3D = camera.LightDirection
            Dim lightColor As Color = camera.LightColor

            m_constants.WorldViewProjection = transform.WorldViewProjection
            m_constants.WorldRotation = transform.Rotation
            m_constants.LightDirection = New Vector4(CSng(light.X), CSng(light.Y), CSng(light.Z), 0)
            m_constants.LightColor = New Vector4(
                CSng(lightColor.R / 255.0),
                CSng(lightColor.G / 255.0),
                CSng(lightColor.B / 255.0),
                CSng(camera.AmbientStrength))
            m_constants.ShadingParams = New Vector4(
                std.Max(1, options.PointSize),
                geometry.PointMode,
                geometry.PaletteLevels,
                If(options.UseEmbeddedColor, 1.0F, 0.0F))
            m_constants.ViewportScale = New Vector4(transform.PixelScaleX, -transform.PixelScaleY, 0, 0)
            m_constants.HeatParams = New Vector4(heatRange.X, heatRange.Y, 0, 0)
        End Sub

        ''' <summary>
        ''' draw the faces of the model as a wire frame
        ''' </summary>
        Friend Sub DrawWireframe(geometry As GpuSceneGeometry, color As Color)
            If geometry.SurfaceVertexCount = 0 Then
                Return
            End If

            Dim context As ID3D11DeviceContext = m_device.Context

            m_constants.UnlitColor = ToVector4(color)

            Call UploadConstants()

            Call context.IASetPrimitiveTopology(CInt(D3D11_PRIMITIVE_TOPOLOGY.TRIANGLELIST))
            Call context.IASetInputLayout(m_positionLayout)
            Call context.VSSetShader(m_positionVertex, IntPtr.Zero, 0UI)
            Call context.PSSetShader(m_unlitPixel, IntPtr.Zero, 0UI)
            Call context.RSSetState(m_rasterWireframe)
            Call context.OMSetDepthStencilState(m_depthWrite, 0UI)
            Call context.OMSetBlendState(m_blendAlpha, IntPtr.Zero, &HFFFFFFFFUI)
            Call BindSingleBuffer(0, geometry.SurfaceBuffer, Scene3DInputLayout.SurfaceStride)
            Call context.Draw(CUInt(geometry.SurfaceVertexCount), 0UI)
        End Sub

        ''' <summary>
        ''' draw the square ground grid below the model
        ''' </summary>
        Friend Sub DrawGround(geometry As GpuSceneGeometry, color As Color)
            If geometry.GroundVertexCount = 0 Then
                Return
            End If

            Dim context As ID3D11DeviceContext = m_device.Context

            m_constants.UnlitColor = ToVector4(color)

            Call UploadConstants()

            Call context.IASetPrimitiveTopology(CInt(D3D11_PRIMITIVE_TOPOLOGY.LINELIST))
            Call context.IASetInputLayout(m_positionLayout)
            Call context.VSSetShader(m_positionVertex, IntPtr.Zero, 0UI)
            Call context.PSSetShader(m_unlitPixel, IntPtr.Zero, 0UI)
            Call context.RSSetState(m_rasterSolid)
            Call context.OMSetDepthStencilState(m_depthWrite, 0UI)
            Call context.OMSetBlendState(m_blendAlpha, IntPtr.Zero, &HFFFFFFFFUI)
            Call BindSingleBuffer(0, geometry.GroundBuffer, Scene3DInputLayout.PositionStride)
            Call context.Draw(CUInt(geometry.GroundVertexCount), 0UI)
        End Sub

        ''' <summary>
        ''' draw the shaded faces of the model
        ''' </summary>
        Friend Sub DrawSurfaces(geometry As GpuSceneGeometry, options As SceneRenderOptions)
            If geometry.SurfaceVertexCount = 0 Then
                Return
            End If

            Dim context As ID3D11DeviceContext = m_device.Context

            Call UploadConstants()

            Call context.IASetPrimitiveTopology(CInt(D3D11_PRIMITIVE_TOPOLOGY.TRIANGLELIST))
            Call context.IASetInputLayout(m_surfaceLayout)
            Call context.VSSetShader(m_surfaceVertex, IntPtr.Zero, 0UI)
            Call context.PSSetShader(m_surfacePixel, IntPtr.Zero, 0UI)
            Call context.RSSetState(If(options.CullBackFaces, m_rasterSolidCulled, m_rasterSolid))
            Call context.OMSetDepthStencilState(m_depthWrite, 0UI)
            Call context.OMSetBlendState(m_blendAlpha, IntPtr.Zero, &HFFFFFFFFUI)
            Call BindSingleBuffer(0, geometry.SurfaceBuffer, Scene3DInputLayout.SurfaceStride)
            Call context.Draw(CUInt(geometry.SurfaceVertexCount), 0UI)
        End Sub

        ''' <summary>
        ''' draw the points of the point cloud, every point is expanded into a
        ''' square of the requested pixel size by the geometry shader free
        ''' instancing of the gpu
        ''' </summary>
        Friend Sub DrawPoints(scene As Scene, geometry As GpuSceneGeometry, options As SceneRenderOptions)
            Dim instances As IntPtr = geometry.EnsureInstances(scene, options)

            If instances = IntPtr.Zero OrElse geometry.InstanceCount = 0 Then
                Return
            End If

            Dim context As ID3D11DeviceContext = m_device.Context
            Dim palette As IntPtr = geometry.PaletteView

            Call UploadConstants()

            Call context.IASetPrimitiveTopology(CInt(D3D11_PRIMITIVE_TOPOLOGY.TRIANGLELIST))
            Call context.IASetInputLayout(m_pointLayout)
            Call context.VSSetShader(m_pointVertex, IntPtr.Zero, 0UI)
            Call context.PSSetShader(m_pointPixel, IntPtr.Zero, 0UI)
            Call context.RSSetState(m_rasterSolid)

            ' the painter of the cpu draws the point cloud without any depth
            ' relation between the points, the gpu keeps that behaviour
            Call context.OMSetDepthStencilState(m_depthDisabled, 0UI)
            Call context.OMSetBlendState(m_blendAlpha, IntPtr.Zero, &HFFFFFFFFUI)

            If palette <> IntPtr.Zero Then
                Call Marshal.WriteIntPtr(m_scratchSingle, 0, palette)
                Call context.PSSetShaderResources(0UI, 1UI, m_scratchSingle)
            End If

            Call BindVertexBuffers(geometry.QuadBuffer, Scene3DInputLayout.PointQuadStride,
                                   instances, Scene3DInputLayout.PointInstanceStride)
            Call context.DrawInstanced(6UI, CUInt(geometry.InstanceCount), 0UI, 0UI)

            Call UnbindShaderResource()
        End Sub

        ''' <summary>
        ''' copy the rendered color buffer onto the render target of the canvas
        ''' through one full screen triangle
        ''' </summary>
        Friend Sub BlitColor(sourceView As IntPtr, targetView As IntPtr, screenSize As Size)
            Dim context As ID3D11DeviceContext = m_device.Context
            Dim viewport As New D3D11_VIEWPORT With {
                .TopLeftX = 0,
                .TopLeftY = 0,
                .Width = screenSize.Width,
                .Height = screenSize.Height,
                .MinDepth = 0,
                .MaxDepth = 1
            }

            Call Marshal.StructureToPtr(viewport, m_scratchViewport, False)
            Call SetRenderTarget(targetView, IntPtr.Zero)
            Call context.RSSetViewports(1UI, m_scratchViewport)

            Call context.IASetPrimitiveTopology(CInt(D3D11_PRIMITIVE_TOPOLOGY.TRIANGLELIST))
            Call context.VSSetShader(m_blitVertex, IntPtr.Zero, 0UI)
            Call context.PSSetShader(m_blitPixel, IntPtr.Zero, 0UI)
            Call context.RSSetState(m_rasterSolid)
            Call context.OMSetDepthStencilState(m_depthDisabled, 0UI)
            Call context.OMSetBlendState(IntPtr.Zero, IntPtr.Zero, &HFFFFFFFFUI)

            Call Marshal.WriteIntPtr(m_scratchSingle, 0, sourceView)
            Call context.PSSetShaderResources(0UI, 1UI, m_scratchSingle)

            Call context.Draw(3UI, 0UI)

            Call UnbindShaderResource()
        End Sub

        ''' <summary>
        ''' release the texture of the pixel stage
        ''' </summary>
        ''' <remarks>
        ''' the pointer of a null view is passed instead of a null array pointer,
        ''' so that no driver has to guess what an empty array means. The color
        ''' buffer of the 3d pipeline is also a render target of the next frame,
        ''' so it must not stay bound to the pixel stage.
        ''' </remarks>
        Private Sub UnbindShaderResource()
            Call Marshal.WriteIntPtr(m_scratchSingle, 0, IntPtr.Zero)
            Call m_device.Context.PSSetShaderResources(0UI, 1UI, m_scratchSingle)
        End Sub

        ''' <summary>
        ''' create a render target view on a native texture, the caller owns the
        ''' returned view
        ''' </summary>
        ''' <param name="texture">
        ''' the d3d11 texture, for example the texture that direct2d draws on
        ''' </param>
        Friend Function CreateRenderTargetView(texture As IntPtr) As IntPtr
            Dim view As IntPtr = IntPtr.Zero

            If texture = IntPtr.Zero Then
                Return IntPtr.Zero
            End If

            Call ThrowIfFailed(
                m_device.Device.CreateRenderTargetView(texture, IntPtr.Zero, view),
                "ID3D11Device::CreateRenderTargetView(canvas)")

            Return view
        End Function

        ''' <summary>
        ''' create a depth stencil view on a native texture, the caller owns the
        ''' returned view
        ''' </summary>
        Friend Function CreateDepthStencilView(texture As IntPtr) As IntPtr
            Dim view As IntPtr = IntPtr.Zero

            If texture = IntPtr.Zero Then
                Return IntPtr.Zero
            End If

            Call ThrowIfFailed(
                m_device.Device.CreateDepthStencilView(texture, IntPtr.Zero, view),
                "ID3D11Device::CreateDepthStencilView(canvas)")

            Return view
        End Function

        ''' <summary>
        ''' create a texture that can be used as a color or depth buffer of the
        ''' given size and sample count
        ''' </summary>
        Friend Function CreateTexture(width As Integer, height As Integer, sampleCount As Integer,
                                      bind As UInteger, format As DXGI_FORMAT) As IntPtr
            Dim texture As IntPtr = IntPtr.Zero
            Dim desc As New D3D11_TEXTURE2D_DESC With {
                .Width = CUInt(width),
                .Height = CUInt(height),
                .MipLevels = 1,
                .ArraySize = 1,
                .Format = CInt(format),
                .SampleDesc = New DXGI_SAMPLE_DESC With {.Count = CUInt(sampleCount), .Quality = 0},
                .Usage = CInt(D3D11_USAGE.DEFAULT),
                .BindFlags = bind,
                .CPUAccessFlags = 0,
                .MiscFlags = 0
            }

            Call ThrowIfFailed(
                m_device.Device.CreateTexture2D(desc, IntPtr.Zero, texture),
                "ID3D11Device::CreateTexture2D(3d)")

            Return texture
        End Function

        ''' <summary>
        ''' bind a color buffer and an optional depth buffer as the output of the
        ''' pipeline
        ''' </summary>
        Friend Sub SetRenderTarget(renderTargetView As IntPtr, depthStencilView As IntPtr)
            Dim context As ID3D11DeviceContext = m_device.Context

            If renderTargetView = IntPtr.Zero Then
                Call context.OMSetRenderTargets(0UI, IntPtr.Zero, depthStencilView)
                Return
            End If

            Call Marshal.WriteIntPtr(m_scratchSingle, 0, renderTargetView)
            Call context.OMSetRenderTargets(1UI, m_scratchSingle, depthStencilView)
        End Sub

        ''' <summary>
        ''' clear the color buffer and the depth buffer of the current frame
        ''' </summary>
        Friend Sub Clear(renderTargetView As IntPtr, depthStencilView As IntPtr, background As Color)
            Dim context As ID3D11DeviceContext = m_device.Context

            If renderTargetView <> IntPtr.Zero Then
                ' the color buffer stays opaque: the canvas of the host expects a
                ' premultiplied alpha surface, an alpha of one keeps the blend
                ' result of the points valid without any extra work
                Call Marshal.Copy(New Single() {background.R / 255.0F, background.G / 255.0F, background.B / 255.0F, 1.0F},
                                  0, m_scratchClear, 4)
                Call context.ClearRenderTargetView(renderTargetView, m_scratchClear)
            End If

            If depthStencilView <> IntPtr.Zero Then
                Call context.ClearDepthStencilView(depthStencilView,
                                                   CUInt(D3D11_CLEAR_FLAG.DEPTH) Or CUInt(D3D11_CLEAR_FLAG.STENCIL),
                                                   1.0F, 0)
            End If
        End Sub

        ''' <summary>
        ''' resolve a multi sampled color buffer into the single sampled texture
        ''' that can be sampled afterwards
        ''' </summary>
        Friend Sub ResolveColor(destination As IntPtr, source As IntPtr)
            Call m_device.Context.ResolveSubresource(
                destination, 0UI, source, 0UI, CInt(DXGI_FORMAT.B8G8R8A8_UNORM))
        End Sub

        ''' <summary>
        ''' upload the current constant buffer content to the gpu
        ''' </summary>
        Private Sub UploadConstants()
            Dim pinned As GCHandle = GCHandle.Alloc(m_constants, GCHandleType.Pinned)

            Try
                Call m_device.Context.UpdateSubresource(m_constantBuffer, 0UI, IntPtr.Zero,
                                                        pinned.AddrOfPinnedObject(), 0UI, 0UI)
            Finally
                pinned.Free()
            End Try
        End Sub

        Private Sub BindSingleBuffer(slot As UInteger, buffer As IntPtr, stride As UInteger)
            Call Marshal.WriteIntPtr(m_scratchBuffers, 0, buffer)
            Call Marshal.WriteInt32(m_scratchStrides, 0, CInt(stride))
            Call Marshal.WriteInt32(m_scratchOffsets, 0, 0)
            Call m_device.Context.IASetVertexBuffers(slot, 1UI, m_scratchBuffers, m_scratchStrides, m_scratchOffsets)
        End Sub

        Private Sub BindVertexBuffers(buffer0 As IntPtr, stride0 As UInteger, buffer1 As IntPtr, stride1 As UInteger)
            Call Marshal.WriteIntPtr(m_scratchBuffers, 0, buffer0)
            Call Marshal.WriteIntPtr(m_scratchBuffers, IntPtr.Size, buffer1)
            Call Marshal.WriteInt32(m_scratchStrides, 0, CInt(stride0))
            Call Marshal.WriteInt32(m_scratchStrides, 4, CInt(stride1))
            Call Marshal.WriteInt32(m_scratchOffsets, 0, 0)
            Call Marshal.WriteInt32(m_scratchOffsets, 4, 0)
            Call m_device.Context.IASetVertexBuffers(0UI, 2UI, m_scratchBuffers, m_scratchStrides, m_scratchOffsets)
        End Sub

        Private Shared Function ToVector4(color As Color) As Vector4
            Return New Vector4(
                CSng(color.R / 255.0),
                CSng(color.G / 255.0),
                CSng(color.B / 255.0),
                CSng(color.A / 255.0))
        End Function

        Private Function CreateVertexShader(code As Byte()) As IntPtr
            Dim shader As IntPtr = IntPtr.Zero
            Dim pinned As GCHandle = GCHandle.Alloc(code, GCHandleType.Pinned)

            Try
                Call ThrowIfFailed(
                    m_device.Device.CreateVertexShader(pinned.AddrOfPinnedObject(), CUInt(code.Length), IntPtr.Zero, shader),
                    "ID3D11Device::CreateVertexShader")
            Finally
                pinned.Free()
            End Try

            Return shader
        End Function

        Private Function CreatePixelShader(code As Byte()) As IntPtr
            Dim shader As IntPtr = IntPtr.Zero
            Dim pinned As GCHandle = GCHandle.Alloc(code, GCHandleType.Pinned)

            Try
                Call ThrowIfFailed(
                    m_device.Device.CreatePixelShader(pinned.AddrOfPinnedObject(), CUInt(code.Length), IntPtr.Zero, shader),
                    "ID3D11Device::CreatePixelShader")
            Finally
                pinned.Free()
            End Try

            Return shader
        End Function

        Private Function CreateInputLayout(elements As D3D11_INPUT_ELEMENT_DESC(), code As Byte()) As IntPtr
            Dim layout As IntPtr = IntPtr.Zero
            Dim pinned As GCHandle = GCHandle.Alloc(code, GCHandleType.Pinned)

            Try
                Call ThrowIfFailed(
                    m_device.Device.CreateInputLayout(elements, CUInt(elements.Length),
                                                      pinned.AddrOfPinnedObject(), CUInt(code.Length), layout),
                    "ID3D11Device::CreateInputLayout")
            Finally
                pinned.Free()
            End Try

            Return layout
        End Function

        Private Function CreateConstantBuffer(byteWidth As Integer) As IntPtr
            Dim buffer As IntPtr = IntPtr.Zero
            Dim desc As New D3D11_BUFFER_DESC With {
                .ByteWidth = CUInt(byteWidth),
                .Usage = CInt(D3D11_USAGE.DEFAULT),
                .BindFlags = CUInt(D3D11_BIND_FLAG.CONSTANT_BUFFER),
                .CPUAccessFlags = 0,
                .MiscFlags = 0,
                .StructureByteStride = 0
            }
            Dim pinned As GCHandle = GCHandle.Alloc(desc, GCHandleType.Pinned)

            Try
                Call ThrowIfFailed(
                    m_device.Device.CreateBuffer(pinned.AddrOfPinnedObject(), IntPtr.Zero, buffer),
                    "ID3D11Device::CreateBuffer(constant)")
            Finally
                pinned.Free()
            End Try

            Return buffer
        End Function

        Private Function CreateSampler() As IntPtr
            Dim sampler As IntPtr = IntPtr.Zero
            Dim desc As New D3D11_SAMPLER_DESC With {
                .Filter = CInt(D3D11_FILTER.MIN_MAG_MIP_POINT),
                .AddressU = CInt(D3D11_TEXTURE_ADDRESS_MODE.CLAMP),
                .AddressV = CInt(D3D11_TEXTURE_ADDRESS_MODE.CLAMP),
                .AddressW = CInt(D3D11_TEXTURE_ADDRESS_MODE.CLAMP),
                .MipLODBias = 0,
                .MaxAnisotropy = 1,
                .ComparisonFunc = CInt(D3D11_COMPARISON_FUNC.NEVER),
                .BorderColor0 = 0,
                .BorderColor1 = 0,
                .BorderColor2 = 0,
                .BorderColor3 = 0,
                .MinLOD = 0,
                .MaxLOD = Single.MaxValue
            }

            Call ThrowIfFailed(
                m_device.Device.CreateSamplerState(desc, sampler),
                "ID3D11Device::CreateSamplerState")

            Return sampler
        End Function

        ''' <summary>
        ''' the source alpha blending of the painter: it is a no operation for the
        ''' opaque colors and it reproduces the alpha of a semi transparent face
        ''' or of a wire frame pen
        ''' </summary>
        Private Function CreateBlendState() As IntPtr
            Dim state As IntPtr = IntPtr.Zero
            Dim targets(7) As D3D11_RENDER_TARGET_BLEND_DESC
            Dim alphaBlend As New D3D11_RENDER_TARGET_BLEND_DESC With {
                .BlendEnable = 1,
                .SrcBlend = CInt(D3D11_BLEND.SRC_ALPHA),
                .DestBlend = CInt(D3D11_BLEND.INV_SRC_ALPHA),
                .BlendOp = CInt(D3D11_BLEND_OP.ADD),
                .SrcBlendAlpha = CInt(D3D11_BLEND.ONE),
                .DestBlendAlpha = CInt(D3D11_BLEND.INV_SRC_ALPHA),
                .BlendOpAlpha = CInt(D3D11_BLEND_OP.ADD),
                .RenderTargetWriteMask = CByte(D3D11_COLOR_WRITE_ENABLE.ALL)
            }

            For i As Integer = 0 To targets.Length - 1
                targets(i) = alphaBlend
            Next

            Dim desc As New D3D11_BLEND_DESC With {
                .AlphaToCoverageEnable = 0,
                .IndependentBlendEnable = 0,
                .RenderTarget = targets
            }

            Call ThrowIfFailed(
                m_device.Device.CreateBlendState(desc, state),
                "ID3D11Device::CreateBlendState")

            Return state
        End Function

        ''' <summary>
        ''' the depth test of the faces, the nearest face wins which is what the
        ''' painter's algorithm of the cpu approximates by sorting the faces
        ''' </summary>
        Private Function CreateDepthStencilState(depthEnabled As Boolean) As IntPtr
            Dim state As IntPtr = IntPtr.Zero
            Dim desc As New D3D11_DEPTH_STENCIL_DESC With {
                .DepthEnable = If(depthEnabled, 1, 0),
                .DepthWriteMask = If(depthEnabled, CInt(D3D11_DEPTH_WRITE_MASK.ALL), CInt(D3D11_DEPTH_WRITE_MASK.ZERO)),
                .DepthFunc = CInt(D3D11_COMPARISON_FUNC.LESS),
                .StencilEnable = 0,
                .StencilReadMask = &HFF,
                .StencilWriteMask = &HFF
            }

            desc.FrontFace = New D3D11_DEPTH_STENCILOP_DESC With {
                .StencilFailOp = CInt(D3D11_STENCIL_OP.KEEP),
                .StencilDepthFailOp = CInt(D3D11_STENCIL_OP.KEEP),
                .StencilPassOp = CInt(D3D11_STENCIL_OP.KEEP),
                .StencilFunc = CInt(D3D11_COMPARISON_FUNC.ALWAYS)
            }
            desc.BackFace = desc.FrontFace

            Call ThrowIfFailed(
                m_device.Device.CreateDepthStencilState(desc, state),
                "ID3D11Device::CreateDepthStencilState")

            Return state
        End Function

        ''' <remarks>
        ''' the lines stay one pixel wide (``AntialiasedLineEnable`` is off),
        ''' exactly like the one pixel gdi pen of the cpu painter draws them
        ''' </remarks>
        Private Function CreateRasterizerState(fillMode As D3D11_FILL_MODE, cullMode As D3D11_CULL_MODE) As IntPtr
            Dim state As IntPtr = IntPtr.Zero
            Dim desc As New D3D11_RASTERIZER_DESC With {
                .FillMode = CInt(fillMode),
                .CullMode = CInt(cullMode),
                .FrontCounterClockwise = 0,
                .DepthBias = 0,
                .DepthBiasClamp = 0,
                .SlopeScaledDepthBias = 0,
                .DepthClipEnable = 1,
                .ScissorEnable = 0,
                .MultisampleEnable = 1,
                .AntialiasedLineEnable = 0
            }

            Call ThrowIfFailed(
                m_device.Device.CreateRasterizerState(desc, state),
                "ID3D11Device::CreateRasterizerState")

            Return state
        End Function

        Private Sub Dispose(disposing As Boolean)
            If m_disposed Then
                Return
            End If

            m_disposed = True

            If m_geometry IsNot Nothing Then
                m_geometry.Dispose()
                m_geometry = Nothing
            End If

            Call ReleaseHandle(m_rasterWireframe)
            Call ReleaseHandle(m_rasterSolidCulled)
            Call ReleaseHandle(m_rasterSolid)
            Call ReleaseHandle(m_depthDisabled)
            Call ReleaseHandle(m_depthWrite)
            Call ReleaseHandle(m_blendAlpha)
            Call ReleaseHandle(m_sampler)
            Call ReleaseHandle(m_constantBuffer)
            Call ReleaseHandle(m_blitPixel)
            Call ReleaseHandle(m_blitVertex)
            Call ReleaseHandle(m_pointPixel)
            Call ReleaseHandle(m_pointVertex)
            Call ReleaseHandle(m_unlitPixel)
            Call ReleaseHandle(m_positionVertex)
            Call ReleaseHandle(m_surfacePixel)
            Call ReleaseHandle(m_surfaceVertex)
            Call ReleaseHandle(m_pointLayout)
            Call ReleaseHandle(m_positionLayout)
            Call ReleaseHandle(m_surfaceLayout)

            Call FreeScratch(m_scratchViewport)
            Call FreeScratch(m_scratchClear)
            Call FreeScratch(m_scratchOffsets)
            Call FreeScratch(m_scratchStrides)
            Call FreeScratch(m_scratchBuffers)
            Call FreeScratch(m_scratchSingle)
        End Sub

        Private Shared Sub ReleaseHandle(ByRef handle As IntPtr)
            If handle = IntPtr.Zero Then
                Return
            End If

            Call Marshal.Release(handle)
            handle = IntPtr.Zero
        End Sub

        Private Shared Sub FreeScratch(ByRef block As IntPtr)
            If block = IntPtr.Zero Then
                Return
            End If

            Call Marshal.FreeHGlobal(block)
            block = IntPtr.Zero
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

    ''' <summary>
    ''' The off screen color and depth buffers of the multi sampled 3d canvas.
    ''' </summary>
    ''' <remarks>
    ''' The multi sampled color buffer can not be sampled, so a resolve texture
    ''' is created as well: the frame is rendered into the multi sampled buffer
    ''' and copied into the resolve texture in one gpu operation, and the resolve
    ''' texture is then sampled by the blit pass that puts the picture onto the
    ''' canvas of the host.
    '''
    ''' With a sample count of one the color buffer itself is sampleable and no
    ''' resolve step is taken at all.
    ''' </remarks>
    Friend NotInheritable Class D3D11OffscreenTarget : Implements IDisposable

        Private ReadOnly m_device As DxDevice
        Private ReadOnly m_width As Integer
        Private ReadOnly m_height As Integer
        Private ReadOnly m_sampleCount As Integer

        ' the resources below are filled through the out parameter of the
        ' factory calls, and visual basic silently takes a copy of a read only
        ' field that is passed by reference (the assignments would be lost), so
        ' these fields are plain private fields instead
        Private m_colorTexture As IntPtr
        Private m_resolveTexture As IntPtr
        Private m_depthTexture As IntPtr
        Private m_renderTargetView As IntPtr
        Private m_colorView As IntPtr
        Private m_depthView As IntPtr

        Private m_disposed As Boolean = False

        ''' <summary>
        ''' the granted sample count of the color buffer, one when the anti
        ''' aliasing is disabled
        ''' </summary>
        Friend ReadOnly Property SampleCount As Integer
            Get
                Return m_sampleCount
            End Get
        End Property

        Friend ReadOnly Property Width As Integer
            Get
                Return m_width
            End Get
        End Property

        Friend ReadOnly Property Height As Integer
            Get
                Return m_height
            End Get
        End Property

        ''' <summary>
        ''' the color buffer that receives the 3d rendering
        ''' </summary>
        Friend ReadOnly Property RenderTargetView As IntPtr
            Get
                Return m_renderTargetView
            End Get
        End Property

        ''' <summary>
        ''' the depth buffer of the 3d rendering
        ''' </summary>
        Friend ReadOnly Property DepthStencilView As IntPtr
            Get
                Return m_depthView
            End Get
        End Property

        ''' <summary>
        ''' the single sampled texture that carries the finished picture
        ''' </summary>
        Friend ReadOnly Property ColorShaderResource As IntPtr
            Get
                Return m_colorView
            End Get
        End Property

        Friend Sub New(device As DxDevice, width As Integer, height As Integer, sampleCount As Integer)
            m_device = device
            m_width = width
            m_height = height
            m_sampleCount = std.Max(1, sampleCount)

            ' the color buffer of the 3d pipeline: it is sampled by the blit pass
            ' when the multi sampling is off, and it is only a render target when
            ' a resolve step feeds the blit pass instead
            Dim colorBind As UInteger = CUInt(D3D11_BIND_FLAG.RENDER_TARGET) Or
                                        CUInt(D3D11_BIND_FLAG.SHADER_RESOURCE)

            m_colorTexture = CreateTexture(width, height, m_sampleCount, colorBind,
                                           DXGI_FORMAT.B8G8R8A8_UNORM)
            Call ThrowIfFailed(
                device.Device.CreateRenderTargetView(m_colorTexture, IntPtr.Zero, m_renderTargetView),
                "ID3D11Device::CreateRenderTargetView(3d)")

            If m_sampleCount > 1 Then
                m_resolveTexture = CreateTexture(width, height, 1, colorBind,
                                                 DXGI_FORMAT.B8G8R8A8_UNORM)
                Call ThrowIfFailed(
                    device.Device.CreateShaderResourceView(m_resolveTexture, IntPtr.Zero, m_colorView),
                    "ID3D11Device::CreateShaderResourceView(resolve)")
            Else
                m_resolveTexture = IntPtr.Zero
                Call ThrowIfFailed(
                    device.Device.CreateShaderResourceView(m_colorTexture, IntPtr.Zero, m_colorView),
                    "ID3D11Device::CreateShaderResourceView(3d)")
            End If

            ' the depth buffer must use a depth format and the very same sample
            ' count as the color buffer that it is bound with
            m_depthTexture = CreateTexture(width, height, m_sampleCount,
                                           CUInt(D3D11_BIND_FLAG.DEPTH_STENCIL),
                                           DXGI_FORMAT.D24_UNORM_S8_UINT)
            Call ThrowIfFailed(
                device.Device.CreateDepthStencilView(m_depthTexture, IntPtr.Zero, m_depthView),
                "ID3D11Device::CreateDepthStencilView(3d)")
        End Sub

        ''' <summary>
        ''' copy the multi sampled color buffer into the resolvable texture
        ''' </summary>
        Friend Sub Resolve(pipeline As D3D11ScenePipeline)
            If m_sampleCount <= 1 OrElse m_resolveTexture = IntPtr.Zero Then
                Return
            End If

            Call pipeline.ResolveColor(m_resolveTexture, m_colorTexture)
        End Sub

        Private Function CreateTexture(width As Integer, height As Integer, sampleCount As Integer,
                                       bind As UInteger, format As DXGI_FORMAT) As IntPtr
            Dim texture As IntPtr = IntPtr.Zero
            Dim desc As New D3D11_TEXTURE2D_DESC With {
                .Width = CUInt(width),
                .Height = CUInt(height),
                .MipLevels = 1,
                .ArraySize = 1,
                .Format = CInt(format),
                .SampleDesc = New DXGI_SAMPLE_DESC With {.Count = CUInt(sampleCount), .Quality = 0},
                .Usage = CInt(D3D11_USAGE.DEFAULT),
                .BindFlags = bind,
                .CPUAccessFlags = 0,
                .MiscFlags = 0
            }

            Call ThrowIfFailed(
                m_device.Device.CreateTexture2D(desc, IntPtr.Zero, texture),
                "ID3D11Device::CreateTexture2D(3d)")

            Return texture
        End Function

        Private Sub Dispose(disposing As Boolean)
            If m_disposed Then
                Return
            End If

            m_disposed = True

            Call ReleaseHandle(m_depthView)
            Call ReleaseHandle(m_colorView)
            Call ReleaseHandle(m_renderTargetView)
            Call ReleaseHandle(m_depthTexture)
            Call ReleaseHandle(m_resolveTexture)
            Call ReleaseHandle(m_colorTexture)
        End Sub

        Private Shared Sub ReleaseHandle(ByRef handle As IntPtr)
            If handle = IntPtr.Zero Then
                Return
            End If

            Call Marshal.Release(handle)
            handle = IntPtr.Zero
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
End Namespace
