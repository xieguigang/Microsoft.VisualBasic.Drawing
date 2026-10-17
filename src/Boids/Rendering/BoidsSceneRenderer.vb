Imports System.Drawing
Imports Microsoft.VisualBasic.Drawing.DirectX
Imports Microsoft.VisualBasic.Drawing.DirectX.Scene3D
Imports Microsoft.VisualBasic.Engine.Particles.Boids
Imports Microsoft.VisualBasic.Imaging.Drawing2D.Colors
Imports Microsoft.VisualBasic.Imaging.Drawing3D
Imports Point3D = Microsoft.VisualBasic.Imaging.Drawing3D.Point3D
Imports LineSegment = Microsoft.VisualBasic.Drawing.DirectX.Scene3D.LineSegment
Imports PaletteDesigner = Microsoft.VisualBasic.Imaging.Drawing2D.Colors.Designer
Imports std = System.Math

''' <summary>
''' The directx presentation of a three dimensional boids field.
''' </summary>
''' <remarks>
''' <para>
''' The class is the only place of the demo that touches the directx scene
''' pipeline. It owns two things:
''' </para>
''' <para>
''' * the static reference geometry of the scene, the twelve edges of the
'''   simulation box. The box is loaded once through
'''   <see cref="DxScene3DCanvas.LoadConnections"/>, which is also what makes
'''   <see cref="Scene3D.Scene.HasData"/> true - the canvas only calls the render
'''   back end for a scene that holds some geometry - and what gives
'''   <see cref="Scene3D.Scene.FitView"/> the radius of the model and the ground
'''   grid its height.
''' </para>
''' <para>
''' * the per frame point instances of the boids. The moving flock can not be
'''   loaded through <see cref="DxScene3DCanvas.LoadPointCloud"/>: every load
'''   raises <see cref="Scene3D.Scene.Version"/>, and the back end answers that
'''   by rebuilding the whole gpu geometry, which at 120000 points per frame is
'''   not affordable. Instead the flock travels through the external instance
'''   buffer of <see cref="Direct3D11SceneRenderer"/>: one 32 byte record per
'''   boid is refilled in place and uploaded as a single
'''   <c>DrawInstanced</c> call, without touching the scene revision.
''' </para>
''' <para>
''' The colors are packed on the cpu and handed over through the embedded color
''' channel (<see cref="SceneRenderOptions.UseEmbeddedColor"/>), because that is
''' the only mode in which the palette is bypassed and every point keeps its own
''' color: the palette mode would paint the white predators with the heat map
''' color of their speed just like every other boid. The embedded color mode
''' reads the per point size factor from the heat slot of the instance record,
''' so the predators are simply packed a bit larger.
''' </para>
''' </remarks>
Public NotInheritable Class BoidsSceneRenderer

    ''' <summary>
    ''' the number of the colormap levels of the speed coloring, it matches the
    ''' level count of the two dimensional renderer
    ''' </summary>
    Public Const ColorLevels As Integer = 30

    ''' <summary>
    ''' the size factor of a predator, relative to <see cref="PointSize"/>
    ''' </summary>
    Public Const PredatorScale As Single = 1.8F

    ''' <summary>
    ''' the number of the <see cref="Single"/> words of one point instance, this
    ''' is <c>Scene3DInputLayout.PointInstanceStride / 4</c>
    ''' </summary>
    Public Const InstanceWords As Integer = 8

    ''' <summary>
    ''' the color of the edges of the simulation box
    ''' </summary>
    Public Shared ReadOnly BoxColor As Color = Color.FromArgb(255, 74, 96, 128)

    Private ReadOnly m_canvas As DxScene3DCanvas
    Private ReadOnly m_backend As Direct3D11SceneRenderer
    Private ReadOnly m_predatorColor As Integer = Color.White.ToArgb()

    ''' <summary>
    ''' the viridis color table that maps the normalized speed of a boid onto a
    ''' color, exactly like the two dimensional renderer does
    ''' </summary>
    Private ReadOnly m_lut As Integer()
    Private m_instances As Single()
    Private m_capacity As Integer = 0
    Private m_packed As Integer = 0
    Private m_uploadMs As Double = 0

    ''' <summary>
    ''' create the presenter of the given canvas, the canvas is configured for the
    ''' boids presentation here
    ''' </summary>
    ''' <param name="canvas">the scene canvas that displays the flock</param>
    ''' <param name="pointSize">the edge length in pixels of one boid</param>
    Public Sub New(canvas As DxScene3DCanvas, Optional pointSize As Integer = 2)
        If canvas Is Nothing Then
            Throw New ArgumentNullException(NameOf(canvas))
        End If

        m_canvas = canvas

        ' the animated point cloud is only supported by the off screen back end,
        ' the direct back end has no external instance buffer at all
        Dim backend As Direct3D11SceneRenderer = TryCast(canvas.Renderer, Direct3D11SceneRenderer)

        If backend Is Nothing Then
            backend = New Direct3D11SceneRenderer()
            canvas.Renderer = backend
        End If

        m_backend = backend
        m_lut = CreateColorTable(ColorLevels)

        Call Configure(pointSize)
    End Sub

    ''' <summary>
    ''' the direct3d 11 back end that draws the flock
    ''' </summary>
    Public ReadOnly Property Backend As Direct3D11SceneRenderer
        Get
            Return m_backend
        End Get
    End Property

    ''' <summary>
    ''' the number of the boids that fit into the current instance buffer
    ''' </summary>
    Public ReadOnly Property Capacity As Integer
        Get
            Return m_capacity
        End Get
    End Property

    ''' <summary>
    ''' the wall clock time of the last instance upload, in milliseconds
    ''' </summary>
    Public ReadOnly Property UploadMs As Double
        Get
            Return If(m_uploadMs > 0, m_uploadMs, m_backend.LastUploadMs)
        End Get
    End Property

    ''' <summary>
    ''' is the frame really drawn by the gpu pipeline?
    ''' </summary>
    Public ReadOnly Property IsGpuPipelineActive As Boolean
        Get
            Return m_backend.IsGpuAvailable AndAlso m_canvas.IsGpuPipelineActive
        End Get
    End Property

    ''' <summary>
    ''' why the frame was handed over to the polygon painter, an empty text means
    ''' that the gpu pipeline is in use
    ''' </summary>
    Public ReadOnly Property FallbackReason As String
        Get
            Return m_canvas.RendererFallbackReason
        End Get
    End Property

    ''' <summary>
    ''' a short description of the gpu device and the pipeline state
    ''' </summary>
    Public ReadOnly Property DeviceDescription As String
        Get
            Return m_canvas.DeviceDescription
        End Get
    End Property

    ''' <summary>
    ''' the edge length in pixels of one boid
    ''' </summary>
    Public Property PointSize As Integer
        Get
            Return m_canvas.PointSize
        End Get
        Set(value As Integer)
            m_canvas.PointSize = std.Max(1, value)
        End Set
    End Property

    ''' <summary>
    ''' apply the presentation options of the boids demo onto the canvas
    ''' </summary>
    ''' <param name="pointSize">the edge length in pixels of one boid</param>
    Public Sub Configure(Optional pointSize As Integer = 2)
        With m_canvas
            ' the 3d back end clears the canvas itself, the background color of
            ' the canvas control is the single source of truth
            .AutoClear = False
            .BackgroundColor = Color.Black
            .PointSize = std.Max(1, pointSize)
            .ShowGround = True
            .ShowConnections = True
            .ShowDebugOverlay = False
            .EnableKeyboardShortcuts = True

            ' a screen aligned two pixel square gains nothing from multi
            ' sampling, and the resolve of the off screen target costs a full
            ' frame copy on its own
            .MultisampleCount = 1
            .CullBackFaces = False

            With .Options
                ' the per point color is the only way to paint the predators
                ' white while every other boid follows the speed colormap
                .UseEmbeddedColor = True
                .PointAlpha = 255
                .ColorScheme = ScalerPalette.viridis
                .BackgroundColor = Color.Black
            End With
        End With
    End Sub

    ' /********************************************************************************/
    '  the static geometry of the scene
    ' /********************************************************************************/

    ''' <summary>
    ''' load the twelve edges of the simulation box and fit the view onto it
    ''' </summary>
    ''' <param name="field">the field whose box is drawn</param>
    ''' <remarks>
    ''' the box is the only geometry of the <see cref="Scene3D.Scene"/>; the
    ''' canvas resets the camera and fits the view onto the new geometry after the
    ''' load, so this method is the right place to call after a resize of the
    ''' field as well
    ''' </remarks>
    Public Sub LoadDomain(field As Field3D)
        If field Is Nothing Then
            Throw New ArgumentNullException(NameOf(field))
        End If

        Call m_canvas.LoadConnections(CreateBox(field.Width, field.Height, field.Depth))
    End Sub

    ''' <summary>
    ''' build the twelve edges of the box that spans
    ''' <c>[0, width] x [0, height] x [0, depth]</c>
    ''' </summary>
    ''' <param name="width">the size of the box along x</param>
    ''' <param name="height">the size of the box along y</param>
    ''' <param name="depth">the size of the box along z</param>
    Public Shared Function CreateBox(width As Double, height As Double, depth As Double) As LineSegment()
        ' the eight corners of the box, the index bits are x = 1, y = 2, z = 4
        Dim corners(7) As Point3D

        For i As Integer = 0 To 7
            corners(i) = New Point3D(
                If((i And 1) <> 0, width, 0),
                If((i And 2) <> 0, height, 0),
                If((i And 4) <> 0, depth, 0))
        Next

        ' the four edges of the bottom face, the four edges of the top face and
        ' the four vertical edges
        Dim pairs As Integer()() = {
            New Integer() {0, 1}, New Integer() {1, 3}, New Integer() {3, 2}, New Integer() {2, 0},
            New Integer() {4, 5}, New Integer() {5, 7}, New Integer() {7, 6}, New Integer() {6, 4},
            New Integer() {0, 4}, New Integer() {1, 5}, New Integer() {2, 6}, New Integer() {3, 7}
        }

        Dim lines(pairs.Length - 1) As LineSegment

        For i As Integer = 0 To pairs.Length - 1
            lines(i) = New LineSegment(corners(pairs(i)(0)), corners(pairs(i)(1)), BoxColor)
        Next

        Return lines
    End Function

    ''' <summary>
    ''' fit the camera onto the loaded box again, the canvas size has to be known
    ''' for that so the host calls this once its window is shown
    ''' </summary>
    Public Sub FitView()
        Call m_canvas.FitView()
    End Sub

    ' /********************************************************************************/
    '  the animated point cloud
    ' /********************************************************************************/

    ''' <summary>
    ''' reserve the instance buffer for the given number of boids
    ''' </summary>
    ''' <param name="count">the number of the boids of the flock</param>
    Public Sub EnsureCapacity(count As Integer)
        If count < 0 Then
            count = 0
        End If

        If m_instances Is Nothing OrElse m_instances.Length < count * InstanceWords Then
            m_instances = New Single(count * InstanceWords - 1) {}
        End If

        m_capacity = count
        m_packed = 0

        Call m_backend.EnsureInstanceCapacity(count)
    End Sub

    ''' <summary>
    ''' pack the current frame of the flock into the instance buffer
    ''' </summary>
    ''' <param name="field">the field to present</param>
    ''' <returns>the number of the packed instances</returns>
    ''' <remarks>
    ''' The layout of one instance is the 32 byte point instance record of the
    ''' scene pipeline: the position, the normal (unused by the embedded color
    ''' mode), the per point size factor and the packed RGBA color. No matrix is
    ''' involved here, the color look up and the speed are the only per boid math
    ''' besides the square root.
    ''' </remarks>
    Public Function Pack(field As Field3D) As Integer
        If field Is Nothing Then
            m_packed = 0
            Return 0
        End If

        Dim state As FlockState3D = field.State
        Dim n As Integer = std.Min(state.count, m_capacity)

        If n <= 0 Then
            m_packed = 0
            Return 0
        End If

        Dim buffer As Single() = m_instances
        Dim px As Double() = state.px
        Dim py As Double() = state.py
        Dim pz As Double() = state.pz
        Dim vx As Double() = state.vx
        Dim vy As Double() = state.vy
        Dim vz As Double() = state.vz
        Dim maxSpeed As Double = field.MaxSpeed
        Dim predCount As Integer = std.Min(field.PredatorCount, n)
        Dim levels As Integer = m_lut.Length
        Dim white As Integer = m_predatorColor

        For i As Integer = 0 To n - 1
            Dim vxv As Double = vx(i)
            Dim vyv As Double = vy(i)
            Dim vzv As Double = vz(i)
            Dim speed As Double = std.Sqrt(vxv * vxv + vyv * vyv + vzv * vzv)

            Dim argb As Integer
            Dim sizeFactor As Single

            If i < predCount Then
                ' the predators are the white, slightly larger leaders of the
                ' flock, exactly like in the two dimensional presentation
                argb = white
                sizeFactor = PredatorScale
            Else
                Dim level As Integer = BoidMath.Round(speed / maxSpeed * levels) - 1

                If level < 0 Then
                    level = 0
                ElseIf level >= levels Then
                    level = levels - 1
                End If

                argb = m_lut(level)
                sizeFactor = 1.0F
            End If

            Dim o As Integer = i * InstanceWords

            buffer(o + 0) = CSng(px(i))
            buffer(o + 1) = CSng(py(i))
            buffer(o + 2) = CSng(pz(i))
            ' the embedded color mode reads the size factor from the heat slot,
            ' the normal slot stays free and is kept at a unit length value so
            ' that a future lit presentation of the same buffer stays sane
            buffer(o + 3) = 1.0F
            buffer(o + 4) = 0.0F
            buffer(o + 5) = 0.0F
            buffer(o + 6) = sizeFactor
            buffer(o + 7) = BitConverter.Int32BitsToSingle(argb)
        Next

        m_packed = n

        Return n
    End Function

    ''' <summary>
    ''' hand the packed frame over to the back end, the upload itself happens in
    ''' the next <see cref="DxScene3DCanvas.Render"/> of the canvas
    ''' </summary>
    Public Sub Upload()
        If m_packed <= 0 Then
            m_uploadMs = 0
            Return
        End If

        Call m_backend.UploadInstances(m_instances, m_packed)
    End Sub

    ''' <summary>
    ''' drop the instances of the last frame, the next frame starts empty
    ''' </summary>
    Public Sub Clear()
        m_packed = 0
        Call m_backend.ClearInstances()
    End Sub

    ' /********************************************************************************/
    '  private helpers
    ' /********************************************************************************/

    ''' <summary>
    ''' build the packed ARGB table of the speed colormap, the very same palette
    ''' and level count that the gdi+ renderer of the two dimensional demo uses
    ''' </summary>
    Private Shared Function CreateColorTable(levels As Integer) As Integer()
        Dim lut As Color() = PaletteDesigner.GetColors(ScalerPalette.viridis.Description, levels)
        Dim table As Integer()

        If lut Is Nothing OrElse lut.Length = 0 Then
            Return New Integer() {Color.Lime.ToArgb()}
        End If

        table = New Integer(lut.Length - 1) {}

        For i As Integer = 0 To lut.Length - 1
            table(i) = lut(i).ToArgb()
        Next

        Return table
    End Function
End Class
