Imports System.Drawing
Imports System.Numerics
Imports Microsoft.VisualBasic.Drawing.DirectX
Imports Microsoft.VisualBasic.Drawing.DirectX.Scene3D
Imports Microsoft.VisualBasic.Engine.Particles.Boids
Imports Microsoft.VisualBasic.Imaging.Drawing2D.Colors
Imports Microsoft.VisualBasic.Imaging.Drawing3D
Imports Point3D = Microsoft.VisualBasic.Imaging.Drawing3D.Point3D
Imports LineSegment = Microsoft.VisualBasic.Drawing.DirectX.Scene3D.LineSegment
Imports std = System.Math

''' <summary>
''' The directx presentation of a three dimensional boids field: every boid is
''' drawn as an arrow that points along its velocity.
''' </summary>
''' <remarks>
''' <para>
''' The class is the only place of the demo that touches the directx scene
''' pipeline. It owns two things:
''' </para>
''' <para>
''' * the static reference geometry of the scene, the twelve edges of the
'''   simulation box and the floor grid inside of it. It is loaded once through
'''   <see cref="DxScene3DCanvas.LoadConnections"/>, which is also what makes
'''   <see cref="Scene3D.Scene.HasData"/> true - the canvas only calls the render
'''   back end for a scene that holds some geometry - and what gives
'''   <see cref="DxScene3DCanvas.FitView"/> the radius of the model.
''' </para>
''' <para>
''' * the arrows of the flock. The moving vector field can not be loaded through
'''   <see cref="DxScene3DCanvas.LoadConnections"/>: every load raises
'''   <see cref="Scene3D.Scene.Version"/> and the back end answers that by
'''   rebuilding the whole gpu geometry - and by re-measuring the bounding sphere
'''   over every line, which at hundreds of thousands of lines per frame is not
'''   affordable. Instead the arrows travel through the animated line channel of
'''   <see cref="Direct3D11SceneRenderer"/>: the line vertices are refilled in
'''   place and uploaded into one dynamic buffer per frame, exactly like the
'''   external instance cloud of an animated point cloud.
''' </para>
''' <para>
''' The geometry of one arrow is built by <see cref="BoidArrows"/> on the cpu,
''' the color of every arrow is packed into its vertices, so the white predators
''' and the speed colormap of the flock coexist in one line list.
''' </para>
''' </remarks>
Public NotInheritable Class BoidsSceneRenderer

    ''' <summary>
    ''' the number of the colormap levels of the speed coloring, it matches the
    ''' level count of the two dimensional renderer
    ''' </summary>
    Public Const ColorLevels As Integer = 30

    ''' <summary>
    ''' the number of the floor grid cells along one edge of the box
    ''' </summary>
    Public Const FloorGridCells As Integer = 10

    ''' <summary>
    ''' the color of the edges of the simulation box
    ''' </summary>
    Public Shared ReadOnly BoxColor As Color = Color.FromArgb(255, 104, 132, 178)

    ''' <summary>
    ''' the color of the floor grid, dim enough to stay behind the flock
    ''' </summary>
    Public Shared ReadOnly GroundGridColor As Color = Color.FromArgb(255, 58, 68, 90)

    Private ReadOnly m_canvas As DxScene3DCanvas
    Private ReadOnly m_backend As Direct3D11SceneRenderer
    Private ReadOnly m_predatorColor As Integer = Color.White.ToArgb()

    ''' <summary>
    ''' the color table that maps the normalized speed of a boid onto a color,
    ''' exactly like the two dimensional renderer does; it is rebuilt when the
    ''' <see cref="Palette"/> changes
    ''' </summary>
    Private m_lut As Integer()

    ''' <summary>
    ''' the color scale that the speed coloring of the arrows uses
    ''' </summary>
    Private m_palette As ScalerPalette = PaletteMenu.DefaultPalette

    Private m_lines As Single()
    Private m_capacity As Integer = 0
    Private m_boidCount As Integer = 0
    Private m_vertexCount As Integer = 0
    Private m_arrowLength As Single = 0

    ''' <summary>
    ''' create the presenter of the given canvas, the canvas is configured for the
    ''' boids presentation here
    ''' </summary>
    ''' <param name="canvas">the scene canvas that displays the flock</param>
    ''' <param name="arrowLength">
    ''' the length of the arrow of the fastest boid in world units, zero asks for
    ''' the default length that <see cref="BoidArrows.ResolveLength"/> derives from
    ''' the size of the box
    ''' </param>
    Public Sub New(canvas As DxScene3DCanvas, Optional arrowLength As Single = 0)
        If canvas Is Nothing Then
            Throw New ArgumentNullException(NameOf(canvas))
        End If

        m_canvas = canvas

        ' the animated line list is only supported by the off screen back end,
        ' the direct back end has no dynamic overlay buffer at all
        Dim backend As Direct3D11SceneRenderer = TryCast(canvas.Renderer, Direct3D11SceneRenderer)

        If backend Is Nothing Then
            backend = New Direct3D11SceneRenderer()
            canvas.Renderer = backend
        End If

        m_backend = backend
        m_lut = PaletteMenu.ColorTable(m_palette, ColorLevels)
        m_arrowLength = std.Max(0, arrowLength)

        Call Configure(m_arrowLength)
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
    ''' the number of the boids that fit into the current line buffer
    ''' </summary>
    Public ReadOnly Property Capacity As Integer
        Get
            Return m_capacity
        End Get
    End Property

    ''' <summary>
    ''' the number of the arrows of the last packed frame
    ''' </summary>
    Public ReadOnly Property ArrowCount As Integer
        Get
            Return m_boidCount
        End Get
    End Property

    ''' <summary>
    ''' the number of the line vertices of the last packed frame
    ''' </summary>
    Public ReadOnly Property VertexCount As Integer
        Get
            Return m_vertexCount
        End Get
    End Property

    ''' <summary>
    ''' the wall clock time of the last line upload, in milliseconds
    ''' </summary>
    Public ReadOnly Property UploadMs As Double
        Get
            Return m_backend.LastLineUploadMs
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
    ''' the length of the arrow of the fastest boid in world units, zero asks for
    ''' the default length
    ''' </summary>
    Public Property ArrowLength As Single
        Get
            Return m_arrowLength
        End Get
        Set(value As Single)
            m_arrowLength = std.Max(0, value)
        End Set
    End Property

    ''' <summary>
    ''' the color scale that maps the normalized speed of a boid onto the color of
    ''' its arrow, selecting another scale rebuilds the lookup table that the next
    ''' packed frame uses
    ''' </summary>
    Public Property Palette As ScalerPalette
        Get
            Return m_palette
        End Get
        Set(value As ScalerPalette)
            m_palette = value
            m_lut = PaletteMenu.ColorTable(value, ColorLevels)

            ' the arrows travel through the line channel with their own vertex
            ' colors, the option only keeps the scene state of the back end - and
            ' with it the debug overlay - consistent with the selection
            m_canvas.Options.ColorScheme = value
        End Set
    End Property

    ''' <summary>
    ''' the length of the arrow of the fastest boid that the current field uses
    ''' </summary>
    ''' <param name="field">the field of the current frame</param>
    Public Function ResolvedArrowLength(field As Field3D) As Single
        Return BoidArrows.ResolveLength(field, m_arrowLength)
    End Function

    ''' <summary>
    ''' apply the presentation options of the boids demo onto the canvas
    ''' </summary>
    ''' <param name="arrowLength">
    ''' the length of the arrow of the fastest boid in world units, zero asks for
    ''' the default length
    ''' </param>
    Public Sub Configure(Optional arrowLength As Single = 0)
        If arrowLength > 0 Then
            m_arrowLength = arrowLength
        End If

        With m_canvas
            ' the 3d back end clears the canvas itself, the background color of
            ' the canvas control is the single source of truth
            .AutoClear = False
            .BackgroundColor = Color.Black
            .ShowConnections = True
            .ShowDebugOverlay = False
            .EnableKeyboardShortcuts = True

            ' the floor grid of the scene pipeline is only drawn by the passes
            ' that draw a model; the demo draws the floor as a part of its own
            ' reference geometry, so the option stays off
            .ShowGround = False

            ' the arrows are one pixel wide lines, a screen aligned two pixel
            ' square gains nothing from multi sampling while the resolve of the
            ' off screen target costs a full frame copy on its own
            .MultisampleCount = 1
            .CullBackFaces = False

            With .Options
                ' the color of every arrow travels in its own vertices, the
                ' palette and the point options do not apply to the line channel
                .UseEmbeddedColor = True
                .PointAlpha = 255
                .ColorScheme = m_palette
                .BackgroundColor = Color.Black
            End With
        End With
    End Sub

    ' /********************************************************************************/
    '  the static geometry of the scene
    ' /********************************************************************************/

    ''' <summary>
    ''' load the reference geometry of the scene - the twelve edges of the box and
    ''' the floor grid - and fit the view onto it
    ''' </summary>
    ''' <param name="field">the field whose box is drawn</param>
    ''' <remarks>
    ''' the canvas resets the camera and fits the view onto the new geometry after
    ''' the load, so this method is the right place to call after a resize of the
    ''' field as well
    ''' </remarks>
    Public Sub LoadDomain(field As Field3D)
        If field Is Nothing Then
            Throw New ArgumentNullException(NameOf(field))
        End If

        Dim lines As New List(Of LineSegment)()

        Call lines.AddRange(CreateBox(field.Width, field.Height, field.Depth))
        Call lines.AddRange(CreateFloor(field.Width, field.Height, field.Depth, FloorGridCells))

        Call m_canvas.LoadConnections(lines)
    End Sub

    ''' <summary>
    ''' build the twelve edges of the box that is centred on the origin and
    ''' spans <c>[-width/2, width/2]</c> on every axis
    ''' </summary>
    ''' <param name="width">the size of the box along x</param>
    ''' <param name="height">the size of the box along y</param>
    ''' <param name="depth">the size of the box along z</param>
    ''' <remarks>
    ''' the scene canvas frames and orbits the origin, so the reference geometry
    ''' of the scene has to be centred there; the simulation itself runs inside
    ''' the <c>[0, size)</c> box and <see cref="BoidArrows.Build"/> shifts it by
    ''' half of the edge length
    ''' </remarks>
    Public Shared Function CreateBox(width As Double, height As Double, depth As Double) As LineSegment()
        ' the eight corners of the box, the index bits are x = 1, y = 2, z = 4
        Dim corners(7) As Point3D
        Dim halfX As Double = width * 0.5
        Dim halfY As Double = height * 0.5
        Dim halfZ As Double = depth * 0.5

        For i As Integer = 0 To 7
            corners(i) = New Point3D(
                If((i And 1) <> 0, halfX, -halfX),
                If((i And 2) <> 0, halfY, -halfY),
                If((i And 4) <> 0, halfZ, -halfZ))
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
    ''' build the floor grid that lies on the bottom face of the box, it is the
    ''' depth cue that tells how far into the box the flock has travelled
    ''' </summary>
    ''' <param name="width">the size of the box along x</param>
    ''' <param name="height">the size of the box along y</param>
    ''' <param name="depth">the size of the box along z</param>
    ''' <param name="cells">the number of the cells along one edge</param>
    Public Shared Function CreateFloor(width As Double, height As Double, depth As Double,
                                       cells As Integer) As LineSegment()
        If cells <= 0 Then
            Return Array.Empty(Of LineSegment)()
        End If

        Dim halfX As Double = width * 0.5
        Dim halfY As Double = height * 0.5
        Dim z As Double = -depth * 0.5
        Dim lines As New List(Of LineSegment)(cells * 4 + 4)

        For i As Integer = 0 To cells
            Dim t As Double = i / cells
            Dim x As Double = -halfX + width * t
            Dim y As Double = -halfY + height * t

            Call lines.Add(New LineSegment(New Point3D(x, -halfY, z), New Point3D(x, halfY, z), GroundGridColor))
            Call lines.Add(New LineSegment(New Point3D(-halfX, y, z), New Point3D(halfX, y, z), GroundGridColor))
        Next

        Return lines.ToArray()
    End Function

    ''' <summary>
    ''' fit the camera onto the loaded box again, the canvas size has to be known
    ''' for that so the host calls this once its window is shown
    ''' </summary>
    Public Sub FitView()
        Call m_canvas.FitView()
    End Sub

    ' /********************************************************************************/
    '  the animated arrows
    ' /********************************************************************************/

    ''' <summary>
    ''' reserve the line buffer for the given number of boids
    ''' </summary>
    ''' <param name="count">the number of the boids of the flock</param>
    Public Sub EnsureCapacity(count As Integer)
        If count < 0 Then
            count = 0
        End If

        Dim vertices As Integer = BoidArrows.VertexCount(count)
        Dim words As Integer = vertices * BoidArrows.FloatsPerVertex

        If m_lines Is Nothing OrElse m_lines.Length < words Then
            m_lines = New Single(words - 1) {}
        End If

        m_capacity = count
        m_boidCount = 0
        m_vertexCount = 0

        Call m_backend.EnsureLineCapacity(vertices)
    End Sub

    ''' <summary>
    ''' pack the arrows of the current frame into the line buffer
    ''' </summary>
    ''' <param name="field">the field to present</param>
    ''' <returns>the number of the written line vertices</returns>
    Public Function Pack(field As Field3D) As Integer
        If field Is Nothing OrElse m_lines Is Nothing Then
            m_boidCount = 0
            m_vertexCount = 0
            Return 0
        End If

        ' the wings of the arrow heads are opened perpendicular to the view
        ' direction, so they stay visible while the camera orbits
        Dim forward As Vector3 = BoidArrows.ViewForward(m_canvas.Controller.Camera)

        m_vertexCount = BoidArrows.Build(field, m_lut, m_predatorColor, forward, m_arrowLength, m_lines)
        m_boidCount = m_vertexCount \ BoidArrows.VerticesPerArrow

        Return m_vertexCount
    End Function

    ''' <summary>
    ''' hand the packed frame over to the back end, the upload itself happens in
    ''' the next <see cref="DxScene3DCanvas.Render"/> of the canvas
    ''' </summary>
    Public Sub Upload()
        If m_vertexCount <= 0 Then
            Return
        End If

        Call m_backend.UploadLines(m_lines, m_vertexCount)
    End Sub

    ''' <summary>
    ''' drop the arrows of the last frame, the next frame starts empty
    ''' </summary>
    Public Sub Clear()
        m_boidCount = 0
        m_vertexCount = 0
        Call m_backend.ClearLines()
    End Sub

End Class
