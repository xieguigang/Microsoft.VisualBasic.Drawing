Imports System.Diagnostics
Imports System.Drawing
Imports System.Numerics
Imports System.Runtime.InteropServices
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Drawing3D

Namespace Scene3D

    ''' <summary>
    ''' The gpu scene render back end: the 3d scene is rendered by a real
    ''' direct3d 11 pipeline (vertex buffers, depth buffer, hlsl shaders) into an
    ''' off screen color buffer and is then copied onto the canvas of the host.
    ''' </summary>
    ''' <remarks>
    ''' This is the default back end of the 3d scene pipeline. Compared with the
    ''' polygon painter back end the per frame cpu work disappears: the geometry
    ''' is uploaded once and every frame only updates one constant buffer and
    ''' submits a handful of draw calls.
    '''
    ''' The frame of this back end is:
    '''
    ''' 1. render the whole scene into the multi sampled off screen color buffer
    '''    with the depth test enabled
    ''' 2. resolve the multi sampled buffer into a sampleable texture
    ''' 3. blit that texture onto the texture that direct2d draws on, through one
    '''    full screen triangle
    ''' 4. the host draws its 2d overlay on top of the result
    '''
    ''' The 2d overlay is therefore always above the 3d scene, and the multi
    ''' sampled buffer gives the model edges the smooth look that a gdi painter
    ''' can not provide.
    '''
    ''' The back end degrades gracefully: a canvas that is not a directx canvas, a
    ''' missing hlsl compiler, a shader that does not compile or a render target
    ''' that can not be created all make this back end hand the frame over to the
    ''' polygon painter of <see cref="Direct2DSceneRenderer"/> instead, and the
    ''' reason of the fall back is reported through <see cref="LastError"/>.
    ''' </remarks>
    Public NotInheritable Class Direct3D11SceneRenderer
        Implements ISceneRenderBackend
        Implements IDisposable

        ''' <summary>
        ''' a shared default instance of this back end
        ''' </summary>
        Public Shared ReadOnly [Default] As New Direct3D11SceneRenderer()

        ''' <summary>
        ''' the color of the wire frame edges of <see cref="SceneRenderMode.Mesh"/>
        ''' </summary>
        Public Shared ReadOnly MeshEdgeColor As Color = Direct2DSceneRenderer.MeshEdgeColor

        ''' <summary>
        ''' the back end that takes over when the gpu pipeline is not available
        ''' </summary>
        Private ReadOnly m_fallback As New Direct2DSceneRenderer()

        Private m_pipeline As D3D11ScenePipeline = Nothing
        Private m_target As D3D11OffscreenTarget = Nothing
        Private m_canvasView As IntPtr = IntPtr.Zero
        Private m_canvasViewSource As IntPtr = IntPtr.Zero
        Private m_sampleCount As Integer = 1
        Private m_unavailable As Boolean = False
        Private m_lastError As String = ""
        Private m_lastErrorDetail As String = ""
        Private m_disposed As Boolean = False

        ' the instance buffer of a point cloud that lives outside of the scene,
        ' see UploadInstances
        Private m_cloudBuffer As IntPtr = IntPtr.Zero
        Private m_cloudBytes As Integer = 0
        Private m_cloudCapacity As Integer = 0
        Private m_cloudCount As Integer = 0
        Private m_pendingCloud As Single() = Nothing
        Private m_pendingCount As Integer = 0

        Public ReadOnly Property Name As String Implements ISceneRenderBackend.Name
            Get
                Return "Direct3D 11"
            End Get
        End Property

        Public ReadOnly Property Description As String Implements ISceneRenderBackend.Description
            Get
                If m_unavailable Then
                    Return $"Direct3D 11 3d pipeline is not available, the polygon painter is used instead: {m_lastError}"
                End If

                Return $"Direct3D 11 3d pipeline ({m_sampleCount}x msaa)"
            End Get
        End Property

        ''' <summary>
        ''' the sample count that the gpu device granted, one when the anti
        ''' aliasing is disabled or not supported
        ''' </summary>
        Public ReadOnly Property MultisampleCount As Integer
            Get
                Return m_sampleCount
            End Get
        End Property

        ''' <summary>
        ''' is the gpu pipeline usable?
        ''' </summary>
        Public ReadOnly Property IsGpuAvailable As Boolean
            Get
                Return Not m_unavailable
            End Get
        End Property

        ''' <summary>
        ''' the reason of the last fall back to the polygon painter, an empty
        ''' text means that the gpu pipeline is used
        ''' </summary>
        Public ReadOnly Property LastError As String
            Get
                Return m_lastError
            End Get
        End Property

        ''' <summary>
        ''' the full detail (including the stack trace) of the last failure of
        ''' the gpu pipeline, for diagnostics
        ''' </summary>
        Public ReadOnly Property LastErrorDetail As String
            Get
                Return m_lastErrorDetail
            End Get
        End Property

        ''' <summary>
        ''' the gpu device of the canvas and the anti aliasing state that the
        ''' device granted, for the status bar
        ''' </summary>
        Public ReadOnly Property DeviceDiagnostics As String
            Get
                If m_pipeline Is Nothing Then
                    Return "(the gpu pipeline was not started yet)"
                End If

                Return $"{m_pipeline.Device.Description}, msaa={m_sampleCount}, {m_pipeline.SampleProbe}"
            End Get
        End Property

        ''' <summary>
        ''' the back end that renders the frame when the gpu pipeline can not be
        ''' used
        ''' </summary>
        Public ReadOnly Property FallbackRenderer As ISceneRenderBackend
            Get
                Return m_fallback
            End Get
        End Property

        ''' <summary>
        ''' release every gpu resource and clear the unavailable flag, the next
        ''' frame tries the gpu pipeline again
        ''' </summary>
        Public Sub Reset()
            Call ReleaseGpu()
            m_unavailable = False
            m_lastError = ""
        End Sub

        ''' <summary>
        ''' reserve the per instance buffer of an animated point cloud that lives
        ''' outside of <see cref="Scene"/>
        ''' </summary>
        ''' <param name="pointCount">
        ''' the largest number of points that <see cref="UploadInstances"/> is
        ''' going to submit
        ''' </param>
        ''' <remarks>
        ''' A host that animates a point cloud of millions of points can not go
        ''' through <see cref="Scene.LoadPointCloud"/>: every call raises
        ''' <see cref="Scene.Version"/> and therefore drops the cached
        ''' <see cref="GpuSceneGeometry"/> of this back end, which rebuilds the
        ''' whole immutable instance buffer of the cloud on every single frame.
        '''
        ''' The three members <see cref="EnsureInstanceCapacity"/>,
        ''' <see cref="UploadInstances"/> and <see cref="ClearInstances"/> keep
        ''' the cloud outside of the scene instead: the scene then only carries
        ''' the static geometry of the frame (for example the wire frame of a
        ''' container) while the moving points are uploaded into one buffer that
        ''' is refilled by the host once per frame. As long as these members are
        ''' never called the behaviour of this back end does not change at all.
        ''' </remarks>
        Public Sub EnsureInstanceCapacity(pointCount As Integer)
            If pointCount < 0 Then
                pointCount = 0
            End If

            m_cloudCapacity = pointCount
        End Sub

        ''' <summary>
        ''' hand the instance data of the next frame over to this back end
        ''' </summary>
        ''' <param name="data">
        ''' eight <see cref="Single"/> per point, the layout of one instance:
        ''' the position x/y/z, the normal x/y/z (the x component is the per
        ''' point size factor of the palette mode), the heat value of the point
        ''' in [0,1] and one unused padding word. The eight words are the 32
        ''' bytes of <c>Scene3DInputLayout.PointInstanceStride</c>.
        ''' </param>
        ''' <param name="pointCount">how many points of <paramref name="data"/> are valid</param>
        ''' <remarks>
        ''' The array is uploaded by the next <see cref="Render"/> and must stay
        ''' untouched until that frame has been drawn.
        ''' </remarks>
        Public Sub UploadInstances(data As Single(), pointCount As Integer)
            If data Is Nothing OrElse pointCount <= 0 Then
                Call ClearInstances()
                Return
            End If

            If pointCount * 8 > data.Length Then
                pointCount = data.Length \ 8
            End If

            m_pendingCloud = data
            m_pendingCount = pointCount
        End Sub

        ''' <summary>
        ''' drop the point cloud that was handed over by
        ''' <see cref="UploadInstances"/>, the scene keeps its own point cloud
        ''' </summary>
        Public Sub ClearInstances()
            m_pendingCloud = Nothing
            m_pendingCount = 0
            m_cloudCount = 0
        End Sub

        ''' <summary>
        ''' how many points the external instance buffer can hold
        ''' </summary>
        Public ReadOnly Property ExternalInstanceCapacity As Integer
            Get
                Return m_cloudCapacity
            End Get
        End Property

        ''' <summary>
        ''' how many external points the last frame has drawn
        ''' </summary>
        Public ReadOnly Property ExternalInstanceCount As Integer
            Get
                Return m_cloudCount
            End Get
        End Property

        ''' <summary>
        ''' fill the instance buffer of the external point cloud through
        ''' <c>Map(WRITE_DISCARD)</c> instead of <c>UpdateSubresource</c>
        ''' </summary>
        ''' <remarks>
        ''' The map avoids the staging copy that
        ''' <see cref="D3D11ScenePipeline.FillBuffer"/> pays for, but the
        ''' <c>Unmap</c> has to wait for the frame that still reads the buffer,
        ''' which serializes the cpu with the gpu. Measured on a cloud of half a
        ''' million points the update through <c>UpdateSubresource</c> was
        ''' faster overall, so it stays the default and this switch is there for
        ''' a comparison on other hardware.
        ''' </remarks>
        Public Shared Property UseDynamicCloud As Boolean = False

        ''' <summary>
        ''' the wall clock time of the last upload of the external point cloud
        ''' in milliseconds
        ''' </summary>
        Public ReadOnly Property LastUploadMs As Double
            Get
                Return m_uploadMs
            End Get
        End Property

        Private m_uploadMs As Double = 0

        ''' <summary>
        ''' upload the pending instance data of the external point cloud, the
        ''' gpu device of the canvas is only known inside of a frame so the
        ''' buffer is created here
        ''' </summary>
        Private Sub SyncExternalCloud()
            Dim wanted As Integer = Global.System.Math.Max(m_cloudCapacity, m_pendingCount)
            Dim stride As Integer = CInt(Scene3DInputLayout.PointInstanceStride)
            Dim bytes As Integer = wanted * stride

            If bytes <> m_cloudBytes Then
                If m_cloudBuffer <> IntPtr.Zero Then
                    Call m_pipeline.ReleaseBuffer(m_cloudBuffer)
                End If

                m_cloudBytes = bytes
            End If

            If m_pendingCloud Is Nothing OrElse m_pendingCount <= 0 Then
                Return
            End If

            If m_cloudBuffer = IntPtr.Zero Then
                ' the cloud is rewritten every frame: the dynamic usage lets the
                ' driver hand out a fresh block on the map instead of copying
                ' the data into a staging buffer of its own
                If UseDynamicCloud Then
                    m_cloudBuffer = m_pipeline.CreateBuffer(
                        m_cloudBytes,
                        D3D11_BIND_FLAG.VERTEX_BUFFER,
                        D3D11_USAGE.DYNAMIC,
                        D3D11_CPU_ACCESS_FLAG.WRITE)
                Else
                    m_cloudBuffer = m_pipeline.CreateBuffer(
                        m_cloudBytes, D3D11_BIND_FLAG.VERTEX_BUFFER)
                End If
            End If

            If m_cloudBuffer = IntPtr.Zero Then
                m_cloudCount = 0
                Return
            End If

            Dim clock As Stopwatch = Stopwatch.StartNew()

            If UseDynamicCloud Then
                Call m_pipeline.FillDynamicBuffer(m_cloudBuffer, m_pendingCloud, m_cloudBytes \ 4)
            Else
                Call m_pipeline.FillBuffer(m_cloudBuffer, m_pendingCloud)
            End If

            clock.Stop()
            m_uploadMs = clock.Elapsed.TotalMilliseconds

            m_cloudCount = m_pendingCount
            m_pendingCloud = Nothing
            m_pendingCount = 0
        End Sub

        Public Sub Render(canvas As IGraphics,
                          scene As Scene,
                          camera As Camera,
                          options As SceneRenderOptions) Implements ISceneRenderBackend.Render

            If canvas Is Nothing OrElse scene Is Nothing OrElse camera Is Nothing OrElse options Is Nothing Then
                Return
            End If

            camera.Screen = canvas.Size

            Dim graphics As DxGraphics = TryCast(canvas, DxGraphics)

            If graphics Is Nothing Then
                Call UseFallback(canvas, scene, camera, options, "the canvas is not a directx canvas")
                Return
            End If

            Dim surface As DxRenderSurface = graphics.renderTarget

            If surface Is Nothing OrElse surface.RenderTargetTexture = IntPtr.Zero Then
                Call UseFallback(canvas, scene, camera, options, "the canvas has no direct3d texture that can be shared")
                Return
            End If

            If m_unavailable Then
                Call UseFallback(canvas, scene, camera, options, m_lastError)
                Return
            End If

            Try
                Call RenderGpu(canvas, surface, scene, camera, options)

                m_lastError = ""
            Catch ex As Exception
                ' the gpu path failed: the resources are dropped so that the next
                ' frame starts from a clean state, and the frame is finished by
                ' the polygon painter so that the canvas never stays blank
                Call ReleaseGpu()

                m_lastError = ex.Message
                m_lastErrorDetail = ex.ToString()

                If TypeOf ex Is InvalidOperationException AndAlso ex.Message.Contains("does not compile") Then
                    ' a shader that does not compile can not be fixed by retrying,
                    ' while a lost device recovers through a recreation
                    m_unavailable = True
                End If

                Call UseFallback(canvas, scene, camera, options, ex.Message)
            End Try
        End Sub

        ''' <summary>
        ''' render one frame of the scene through the direct3d 11 pipeline
        ''' </summary>
        Private Sub RenderGpu(canvas As IGraphics, surface As DxRenderSurface, scene As Scene,
                              camera As Camera, options As SceneRenderOptions)

            Dim size As Size = canvas.Size

            Call EnsurePipeline(surface.Device)
            Call EnsureTarget(size, options)
            Call SyncExternalCloud()

            Dim transform As SceneTransform = SceneTransform.Create(camera, scene, size)
            Dim geometry As GpuSceneGeometry = m_pipeline.GeometryOf(scene, options)
            Dim heatRange As New Vector2(0, 1)

            If options.Mode = SceneRenderMode.PointCloud AndAlso geometry.PointMode = Scene3DShaders.PointModeLit Then
                heatRange = geometry.ComputeLitHeatRange(camera)
            End If

            ' the direct2d commands that are still pending are submitted first: the
            ' raw direct3d work of the 3d pipeline must not be reordered with the
            ' batched 2d commands of the host
            Call surface.Flush()

            Call m_pipeline.BeginFrame(size)
            Call m_pipeline.SetRenderTarget(m_target.RenderTargetView, m_target.DepthStencilView)
            Call m_pipeline.Clear(m_target.RenderTargetView, m_target.DepthStencilView, options.BackgroundColor)

            ' the screen space background grid goes onto the cleared buffer and
            ' below the scene geometry, so the model occludes it through the
            ' depth buffer while the grid itself never moves with the camera
            Call m_pipeline.DrawBackgroundGrid(size, options)

            Call m_pipeline.SetScene(geometry, transform, camera, options, heatRange)

            Call DrawScene(scene, geometry, options)

            If m_target.SampleCount > 1 Then
                Call m_target.Resolve(m_pipeline)
            End If

            Dim canvasView As IntPtr = EnsureCanvasTarget(surface)

            If canvasView = IntPtr.Zero Then
                Throw New InvalidOperationException("unable to create a render target view on the canvas texture")
            End If

            Call m_pipeline.BlitColor(m_target.ColorShaderResource, canvasView, size)
            Call m_pipeline.EndFrame(size)
        End Sub

        ''' <summary>
        ''' the content of one frame, it follows the order of the polygon painter
        ''' back end
        ''' </summary>
        Private Sub DrawScene(scene As Scene, geometry As GpuSceneGeometry, options As SceneRenderOptions)
            If scene.SurfaceCount > 0 Then
                If options.ShowGround Then
                    Call m_pipeline.DrawGround(geometry, options.GroundColor)
                End If

                Select Case options.Mode
                    Case SceneRenderMode.Mesh
                        Call m_pipeline.DrawWireframe(geometry, MeshEdgeColor)
                    Case SceneRenderMode.PointCloud
                        ' even a solid model can be presented as a point cloud:
                        ' every vertex becomes a heat map colored point and the
                        ' color is driven by the light intensity of its face
                        Call DrawPointCloud(scene, geometry, options)
                    Case Else
                        Call m_pipeline.DrawSurfaces(geometry, options)
                End Select
            ElseIf scene.PointCount > 0 OrElse m_cloudCount > 0 Then
                If options.ShowGround Then
                    Call m_pipeline.DrawGround(geometry, options.GroundColor)
                End If

                Call DrawPointCloud(scene, geometry, options)
            End If

            ' the connection lines are an overlay of the scene: they are drawn last
            ' so that the whole graph stays visible on top of the point cloud of its
            ' neurons (the point cloud itself is drawn without any depth relation,
            ' see D3D11ScenePipeline.DrawPoints)
            If scene.LineCount > 0 AndAlso options.ShowConnections Then
                Call m_pipeline.DrawLines(geometry)
            End If
        End Sub

        ''' <summary>
        ''' draw the point cloud in the point shape that the options request:
        ''' screen aligned squares (the classic billboard) or world space voxel
        ''' cubes that occlude each other through the depth buffer
        ''' </summary>
        Private Sub DrawPointCloud(scene As Scene, geometry As GpuSceneGeometry, options As SceneRenderOptions)
            ' a cloud that the host uploaded through UploadInstances wins over
            ' the point cloud of the scene
            If m_cloudCount > 0 AndAlso m_cloudBuffer <> IntPtr.Zero Then
                If options.PointShape = ScenePointShape.Cube AndAlso geometry.CubeBuffer <> IntPtr.Zero Then
                    Call m_pipeline.DrawPointsCubes(scene, geometry, options)
                Else
                    Call m_pipeline.DrawPointInstances(geometry, options, m_cloudBuffer, m_cloudCount)
                End If

                Return
            End If

            If options.PointShape = ScenePointShape.Cube AndAlso geometry.CubeBuffer <> IntPtr.Zero Then
                Call m_pipeline.DrawPointsCubes(scene, geometry, options)
            Else
                Call m_pipeline.DrawPoints(scene, geometry, options)
            End If
        End Sub

        Private Sub EnsurePipeline(device As DxDevice)
            If m_pipeline IsNot Nothing AndAlso m_pipeline.Device Is device Then
                Return
            End If

            Dim stale As D3D11ScenePipeline = m_pipeline

            ' the external cloud belongs to the device of the old pipeline
            Call ReleaseExternalCloud()

            m_pipeline = New D3D11ScenePipeline(device)

            If stale IsNot Nothing Then
                stale.Dispose()
            End If

            ' the canvas view belongs to the device that was released above
            Call ReleaseCanvasTarget()
        End Sub

        Private Sub EnsureTarget(size As Size, options As SceneRenderOptions)
            Dim samples As Integer = m_pipeline.ResolveSampleCount(options.MultisampleCount)

            If m_target IsNot Nothing AndAlso
               m_target.Width = size.Width AndAlso
               m_target.Height = size.Height AndAlso
               m_target.SampleCount = samples Then

                Return
            End If

            If m_target IsNot Nothing Then
                m_target.Dispose()
                m_target = Nothing
            End If

            m_target = New D3D11OffscreenTarget(m_pipeline.Device, size.Width, size.Height, samples)
            m_sampleCount = m_target.SampleCount

            ' the render target of the canvas survives a resize, but keeping the
            ' view of the old size bound to a new target would be wrong
            Call ReleaseCanvasTarget()
        End Sub

        ''' <summary>
        ''' the render target view of the texture that direct2d uses, it is
        ''' created once per canvas texture
        ''' </summary>
        Private Function EnsureCanvasTarget(surface As DxRenderSurface) As IntPtr
            Dim texture As IntPtr = surface.RenderTargetTexture

            If texture = IntPtr.Zero Then
                Return IntPtr.Zero
            End If

            If m_canvasView <> IntPtr.Zero AndAlso m_canvasViewSource = texture Then
                Return m_canvasView
            End If

            Call ReleaseCanvasTarget()

            m_canvasView = m_pipeline.CreateRenderTargetView(texture)
            m_canvasViewSource = texture

            Return m_canvasView
        End Function

        Private Sub UseFallback(canvas As IGraphics, scene As Scene, camera As Camera,
                                options As SceneRenderOptions, reason As String)
            If m_unavailable AndAlso String.IsNullOrEmpty(m_lastError) Then
                m_lastError = reason
            End If

            Call m_fallback.Render(canvas, scene, camera, options)
        End Sub

        Private Sub ReleaseCanvasTarget()
            If m_canvasView <> IntPtr.Zero Then
                Call Marshal.Release(m_canvasView)
                m_canvasView = IntPtr.Zero
            End If

            m_canvasViewSource = IntPtr.Zero
        End Sub

        Private Sub ReleaseExternalCloud()
            If m_cloudBuffer <> IntPtr.Zero Then
                If m_pipeline IsNot Nothing Then
                    Call m_pipeline.ReleaseBuffer(m_cloudBuffer)
                Else
                    Call Marshal.Release(m_cloudBuffer)
                    m_cloudBuffer = IntPtr.Zero
                End If
            End If

            m_cloudBuffer = IntPtr.Zero
            m_cloudBytes = 0
            m_cloudCount = 0
        End Sub

        Private Sub ReleaseGpu()
            Call ReleaseCanvasTarget()
            Call ReleaseExternalCloud()

            If m_target IsNot Nothing Then
                m_target.Dispose()
                m_target = Nothing
            End If

            If m_pipeline IsNot Nothing Then
                m_pipeline.Dispose()
                m_pipeline = Nothing
            End If

            m_sampleCount = 1
        End Sub

        Private Sub Dispose(disposing As Boolean)
            If m_disposed Then
                Return
            End If

            m_disposed = True

            Call ReleaseGpu()
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
