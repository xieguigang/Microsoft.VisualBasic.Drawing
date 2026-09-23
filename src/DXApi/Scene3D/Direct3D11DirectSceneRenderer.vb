Imports System.Drawing
Imports System.Numerics
Imports System.Runtime.InteropServices
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Drawing3D

Namespace Scene3D

    ''' <summary>
    ''' the depth buffer that the direct back end uses for its own rendering
    ''' </summary>
    Friend NotInheritable Class D3D11DepthBuffer : Implements IDisposable

        Private ReadOnly m_width As Integer
        Private ReadOnly m_height As Integer
        Private ReadOnly m_texture As IntPtr
        Private ReadOnly m_view As IntPtr
        Private m_disposed As Boolean = False

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
        ''' the depth stencil view of this depth buffer
        ''' </summary>
        Friend ReadOnly Property View As IntPtr
            Get
                Return m_view
            End Get
        End Property

        Friend Sub New(pipeline As D3D11ScenePipeline, width As Integer, height As Integer)
            m_width = width
            m_height = height
            m_texture = pipeline.CreateTexture(width, height, 1,
                                               CUInt(D3D11_BIND_FLAG.DEPTH_STENCIL),
                                               DXGI_FORMAT.D24_UNORM_S8_UINT)
            m_view = pipeline.CreateDepthStencilView(m_texture)
        End Sub

        Private Sub Dispose(disposing As Boolean)
            If m_disposed Then
                Return
            End If

            m_disposed = True

            If m_view <> IntPtr.Zero Then
                Call Marshal.Release(m_view)
            End If

            If m_texture <> IntPtr.Zero Then
                Call Marshal.Release(m_texture)
            End If
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
    ''' The direct gpu scene render back end: the 3d scene is rendered by the real
    ''' direct3d 11 pipeline straight into the texture that the canvas draws on,
    ''' without any intermediate buffer or copy.
    ''' </summary>
    ''' <remarks>
    ''' This back end is the shortest possible path from the scene to the screen:
    ''' the per frame cost is one constant buffer update, the draw calls and the
    ''' clear, and the 2d overlay of the host is drawn after the 3d scene by
    ''' direct2d, exactly like in the default back end.
    '''
    ''' The price of the zero copy path is that the multi sampled anti aliasing is
    ''' not available: a swap chain back buffer can not be created multi sampled,
    ''' so <see cref="SceneRenderOptions.MultisampleCount"/> is ignored here. The
    ''' default back end (<see cref="Direct3D11SceneRenderer"/>) renders into an
    ''' off screen buffer and supports the anti aliasing.
    ''' </remarks>
    Public NotInheritable Class Direct3D11DirectSceneRenderer
        Implements ISceneRenderBackend
        Implements IDisposable

        ''' <summary>
        ''' a shared default instance of this back end
        ''' </summary>
        Public Shared ReadOnly [Default] As New Direct3D11DirectSceneRenderer()

        ''' <summary>
        ''' the color of the wire frame edges of <see cref="SceneRenderMode.Mesh"/>
        ''' </summary>
        Public Shared ReadOnly MeshEdgeColor As Color = Direct2DSceneRenderer.MeshEdgeColor

        Private ReadOnly m_fallback As New Direct2DSceneRenderer()

        Private m_pipeline As D3D11ScenePipeline = Nothing
        Private m_depth As D3D11DepthBuffer = Nothing
        Private m_canvasView As IntPtr = IntPtr.Zero
        Private m_canvasViewSource As IntPtr = IntPtr.Zero
        Private m_unavailable As Boolean = False
        Private m_lastError As String = ""
        Private m_lastErrorDetail As String = ""
        Private m_disposed As Boolean = False

        Public ReadOnly Property Name As String Implements ISceneRenderBackend.Name
            Get
                Return "Direct3D 11 (direct)"
            End Get
        End Property

        Public ReadOnly Property Description As String Implements ISceneRenderBackend.Description
            Get
                If m_unavailable Then
                    Return $"Direct3D 11 3d pipeline is not available, the polygon painter is used instead: {m_lastError}"
                End If

                Return "Direct3D 11 3d pipeline rendered directly into the canvas back buffer (no copy, no anti aliasing)"
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
        ''' the reason of the last fall back to the polygon painter
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
        ''' the back end that renders the frame when the gpu pipeline can not be
        ''' used
        ''' </summary>
        Public ReadOnly Property FallbackRenderer As ISceneRenderBackend
            Get
                Return m_fallback
            End Get
        End Property

        ''' <summary>
        ''' release every gpu resource and clear the unavailable flag
        ''' </summary>
        Public Sub Reset()
            Call ReleaseGpu()
            m_unavailable = False
            m_lastError = ""
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
                Call ReleaseGpu()

                m_lastError = ex.Message
                m_lastErrorDetail = ex.ToString()

                If TypeOf ex Is InvalidOperationException AndAlso ex.Message.Contains("does not compile") Then
                    m_unavailable = True
                End If

                Call UseFallback(canvas, scene, camera, options, ex.Message)
            End Try
        End Sub

        Private Sub RenderGpu(canvas As IGraphics, surface As DxRenderSurface, scene As Scene,
                              camera As Camera, options As SceneRenderOptions)

            Dim size As Size = canvas.Size

            Call EnsurePipeline(surface.Device)

            Dim canvasView As IntPtr = EnsureCanvasTarget(surface)

            If canvasView = IntPtr.Zero Then
                Throw New InvalidOperationException("unable to create a render target view on the canvas texture")
            End If

            Call EnsureDepth(size)

            Dim transform As SceneTransform = SceneTransform.Create(camera, scene, size)
            Dim geometry As GpuSceneGeometry = m_pipeline.GeometryOf(scene, options)
            Dim heatRange As New Vector2(0, 1)

            If options.Mode = SceneRenderMode.PointCloud AndAlso geometry.PointMode = Scene3DShaders.PointModeLit Then
                heatRange = geometry.ComputeLitHeatRange(camera)
            End If

            Call surface.Flush()

            Call m_pipeline.BeginFrame(size)
            Call m_pipeline.SetRenderTarget(canvasView, m_depth.View)
            Call m_pipeline.Clear(canvasView, m_depth.View, options.BackgroundColor)
            Call m_pipeline.SetScene(geometry, transform, camera, options, heatRange)

            Call DrawScene(scene, geometry, options)

            ' the depth buffer is unbound again before the direct2d overlay of the
            ' host is drawn, otherwise the 2d text of the overlay would be
            ' rejected by the depth test of the 3d scene
            Call m_pipeline.SetRenderTarget(canvasView, IntPtr.Zero)
            Call m_pipeline.EndFrame(size)
        End Sub

        Private Sub DrawScene(scene As Scene, geometry As GpuSceneGeometry, options As SceneRenderOptions)
            If scene.SurfaceCount > 0 Then
                If options.ShowGround Then
                    Call m_pipeline.DrawGround(geometry, options.GroundColor)
                End If

                Select Case options.Mode
                    Case SceneRenderMode.Mesh
                        Call m_pipeline.DrawWireframe(geometry, MeshEdgeColor)
                    Case SceneRenderMode.PointCloud
                        Call m_pipeline.DrawPoints(scene, geometry, options)
                    Case Else
                        Call m_pipeline.DrawSurfaces(geometry, options)
                End Select
            ElseIf scene.PointCount > 0 Then
                If options.ShowGround Then
                    Call m_pipeline.DrawGround(geometry, options.GroundColor)
                End If

                Call m_pipeline.DrawPoints(scene, geometry, options)
            End If
        End Sub

        Private Sub EnsurePipeline(device As DxDevice)
            If m_pipeline IsNot Nothing AndAlso m_pipeline.Device Is device Then
                Return
            End If

            Dim stale As D3D11ScenePipeline = m_pipeline

            m_pipeline = New D3D11ScenePipeline(device)

            If stale IsNot Nothing Then
                stale.Dispose()
            End If

            Call ReleaseCanvasTarget()
        End Sub

        Private Sub EnsureDepth(size As Size)
            If m_depth IsNot Nothing AndAlso m_depth.Width = size.Width AndAlso m_depth.Height = size.Height Then
                Return
            End If

            If m_depth IsNot Nothing Then
                m_depth.Dispose()
                m_depth = Nothing
            End If

            m_depth = New D3D11DepthBuffer(m_pipeline, size.Width, size.Height)
        End Sub

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

        Private Sub ReleaseGpu()
            Call ReleaseCanvasTarget()

            If m_depth IsNot Nothing Then
                m_depth.Dispose()
                m_depth = Nothing
            End If

            If m_pipeline IsNot Nothing Then
                m_pipeline.Dispose()
                m_pipeline = Nothing
            End If
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
