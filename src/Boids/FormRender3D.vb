Imports System.Diagnostics
Imports System.Drawing
Imports System.Windows.Forms
Imports Microsoft.VisualBasic.ApplicationServices
Imports Microsoft.VisualBasic.Drawing.DirectX
Imports Microsoft.VisualBasic.Engine.Particles.Boids
Imports Microsoft.VisualBasic.Parallel
Imports PerformanceCounter = Microsoft.VisualBasic.ApplicationServices.PerformanceCounter
Imports TimeCounter = Microsoft.VisualBasic.ApplicationServices.TimeCounter
Imports std = System.Math

''' <summary>
''' The three dimensional boids demo: a directx scene canvas that presents the
''' flock of <see cref="Field3D"/> through the external instance buffer of the
''' direct3d 11 pipeline.
''' </summary>
''' <remarks>
''' <para>
''' The two dimensional demo (<see cref="FormRender"/>) is kept untouched: it
''' renders through gdi+ and it owns its own timer, field and pixel buffer. This
''' window is the directx counterpart and both of them can be started from the
''' same executable (see <see cref="Program"/>).
''' </para>
''' <para>
''' One frame is three steps, and only the first one is expensive:
''' <see cref="Field3D.Advance"/> solves the flocking rules in parallel,
''' <see cref="BoidsSceneRenderer.Pack"/> turns the SoA state into the point
''' instances and <see cref="BoidsSceneRenderer.Upload"/> hands them to the back
''' end. The upload itself happens inside the next paint of the canvas, which
''' is why the tick only requests a repaint afterwards.
''' </para>
''' </remarks>
Partial Public Class FormRender3D
    Inherits Form

    ''' <summary>the simulation that is presented by this window</summary>
    Private m_field As Field3D
    ''' <summary>the directx presentation of the flock</summary>
    Private m_renderer As BoidsSceneRenderer
    ''' <summary>the per phase timer of the frame, it feeds the window title</summary>
    Private ReadOnly m_counter As New PerformanceCounter()
    ''' <summary>the wall clock of the statistics update, the title is throttled</summary>
    Private ReadOnly m_statisticsWatch As Stopwatch = Stopwatch.StartNew()

    Private m_frames As Integer = 0
    Private m_fps As Double = 0
    Private m_settings As BoidsOptions

    Public Sub New()
        Call InitializeComponent()

        ' the parallel physics solve runs on the .NET thread pool; without
        ' raising the pool's minimum the pool only injects threads lazily and the
        ' solve would run on roughly half of the cores
        Call Threading.ThreadPool.SetMinThreads(Environment.ProcessorCount, Environment.ProcessorCount)
    End Sub

    ''' <summary>
    ''' the command line options of this window, they are read when the flock is
    ''' created
    ''' </summary>
    <System.ComponentModel.Browsable(False)>
    <System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)>
    Public Property Settings As BoidsOptions
        Get
            If m_settings Is Nothing Then
                m_settings = New BoidsOptions()
            End If

            Return m_settings
        End Get
        Set(value As BoidsOptions)
            m_settings = If(value, New BoidsOptions())
        End Set
    End Property

    ''' <summary>
    ''' the simulation that this window presents, nothing before the window is
    ''' loaded
    ''' </summary>
    Public ReadOnly Property Simulation As Field3D
        Get
            Return m_field
        End Get
    End Property

    ''' <summary>
    ''' the directx presentation of the flock
    ''' </summary>
    Public ReadOnly Property Presenter As BoidsSceneRenderer
        Get
            Return m_renderer
        End Get
    End Property

    ' /********************************************************************************/
    '  the life cycle
    ' /********************************************************************************/

    Private Sub FormRender3D_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' the box of the scene and the camera fit need the real client size, so
        ' the flock is created here and not in the constructor
        Call Reset()

        Me.timer1.Enabled = True
    End Sub

    ''' <summary>
    ''' (re)create the flock, load the reference geometry of the scene and warm up
    ''' the jit and the thread pool
    ''' </summary>
    Private Sub Reset()
        VectorTask.n_threads = App.CPUCoreNumbers

        Dim count As Integer = std.Max(1, Me.Settings.BoidCount)
        Dim edge As Double = Me.Settings.ResolveDomainSize()

        m_field = New Field3D(edge, edge, edge, count)

        ' warm up the jit and the thread pool so that the first measured frame
        ' is not dominated by one-off compilation costs
        For i As Integer = 1 To 5
            m_field.Advance()
        Next

        If m_renderer Is Nothing Then
            m_renderer = New BoidsSceneRenderer(m_canvas, Me.Settings.ArrowLength)
        Else
            Call m_renderer.Configure(Me.Settings.ArrowLength)
        End If

        Call m_renderer.EnsureCapacity(count)

        ' the edges of the box and the floor grid inside of it: they are the only
        ' geometry of the scene, and they define the view that the camera fits
        ' onto
        Call m_renderer.LoadDomain(m_field)

        m_canvas.MultisampleCount = std.Max(1, Me.Settings.MultisampleCount)
        m_canvas.ShowDebugOverlay = True

        ' present the warmed up frame right away instead of waiting for the
        ' first tick, so the window never shows an empty canvas
        Call m_renderer.Pack(m_field)
        Call m_renderer.Upload()
        Call m_canvas.RequestRender()

        m_statisticsWatch.Restart()
        m_frames = 0

        Call UpdateStatistics(force:=True)
    End Sub

    ' /********************************************************************************/
    '  the frame
    ' /********************************************************************************/

    Private Sub timer1_Tick(sender As Object, e As EventArgs) Handles timer1.Tick
        If m_field Is Nothing OrElse m_renderer Is Nothing Then
            Return
        End If

        Call m_counter.Set()

        m_field.Advance()
        Call m_counter.Mark("physics simulation")

        Call m_renderer.Pack(m_field)
        Call m_counter.Mark("arrow packing")

        Call m_renderer.Upload()
        Call m_counter.Mark("arrow upload")

        ' the upload of the next frame is consumed by the paint that this
        ' request schedules, so the vertex array is not rewritten between the
        ' registration and the upload
        Call m_canvas.RequestRender()
        Call m_counter.Mark("present request")

        Call UpdateStatistics(force:=False)
    End Sub

    ''' <summary>
    ''' refresh the window title and the debug overlay of the canvas
    ''' </summary>
    ''' <param name="force">
    ''' true writes the text unconditionally; otherwise the text is only written
    ''' about four times a second, because a per frame WM_SETTEXT of the window
    ''' costs more than the whole instance packing
    ''' </param>
    Private Sub UpdateStatistics(force As Boolean)
        m_frames += 1

        Dim seconds As Double = m_statisticsWatch.Elapsed.TotalSeconds

        If Not force AndAlso seconds < 0.25 Then
            Return
        End If

        ' a forced update right after the reset has no measurable window, so the
        ' frame rate is only averaged over a real interval
        If seconds >= 0.1 Then
            Dim instant As Double = m_frames / seconds
            m_fps = If(m_fps <= 0, instant, m_fps * 0.75 + instant * 0.25)
        End If

        m_frames = 0
        m_statisticsWatch.Restart()

        Dim pipeline As String

        If m_renderer Is Nothing OrElse Not m_renderer.IsGpuPipelineActive Then
            Dim reason As String = If(m_renderer Is Nothing, "", m_renderer.FallbackReason)

            pipeline = If(String.IsNullOrEmpty(reason), "Direct2D fallback", "Direct2D fallback: " & reason)
        Else
            pipeline = $"Direct3D 11 {m_canvas.ActiveMultisampleCount}x"
        End If

        Me.Text = $"Boids Algorithm 3D - {m_field.BoidCount:N0} arrows, {m_fps:F1} fps, " &
                  $"physics {PhaseMs("physics simulation"):F1} ms, pack {PhaseMs("arrow packing"):F2} ms"

        ' the debug overlay of the canvas already shows the camera; the extra
        ' text adds the frame statistics of the host
        m_canvas.ExtraDebugText =
            $"fps       : {m_fps:F1}" & Environment.NewLine &
            $"boids     : {m_field.BoidCount:N0} in {m_field.Width:F0}^3" & Environment.NewLine &
            $"arrows    : {m_renderer.VertexCount \ BoidArrows.VerticesPerArrow:N0} x 3 lines, " &
            $"length {m_renderer.ResolvedArrowLength(m_field):F1} units" & Environment.NewLine &
            $"physics   : {PhaseMs("physics simulation"):F1} ms" & Environment.NewLine &
            $"packing   : {PhaseMs("arrow packing"):F2} ms" & Environment.NewLine &
            $"upload    : {m_renderer.UploadMs:F2} ms" & Environment.NewLine &
            $"pipeline  : {pipeline}"
    End Sub

    ''' <summary>
    ''' the time that the given phase of the last frame has taken, in milliseconds
    ''' </summary>
    Private Function PhaseMs(name As String) As Double
        For Each item As TimeCounter In m_counter.GetCounters()
            If item.task = name Then
                Return item.span1.TotalMilliseconds
            End If
        Next

        Return 0
    End Function

    ' /********************************************************************************/
    '  the commands
    ' /********************************************************************************/

    ''' <summary>
    ''' export the frame that is currently displayed into an image file
    ''' </summary>
    Private Sub Canvas_SnapshotRequested(sender As Object, e As EventArgs) Handles m_canvas.SnapshotRequested
        Dim file As String = IO.Path.Combine(
            Environment.CurrentDirectory,
            $"boids3d-{DateTime.Now:yyyyMMdd-HHmmss}.png")

        Try
            ' the export forces a synchronous repaint of the canvas, so it runs
            ' outside of the render event
            If m_canvas.SaveSnapshot(file) Then
                Call MessageBox.Show(Me, "画面已保存: " & file, "截图",
                                     MessageBoxButtons.OK, MessageBoxIcon.Information)
            Else
                Call MessageBox.Show(Me, "画面导出失败。", "截图",
                                     MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
        Catch ex As Exception
            Call MessageBox.Show(Me, "截图失败: " & ex.Message, "错误",
                                 MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' create a fresh flock with the current settings
    ''' </summary>
    ''' <remarks>
    ''' the shortcuts of the canvas itself (R, F, G, D, S) never reach the form,
    ''' F5 is not one of them, so it arrives here
    ''' </remarks>
    Private Sub FormRender3D_KeyDown(sender As Object, e As KeyEventArgs) Handles MyBase.KeyDown
        If e.KeyCode = Keys.F5 Then
            Call Reset()
            e.Handled = True
        End If
    End Sub

    ' /********************************************************************************/
    '  the resources
    ' /********************************************************************************/

    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        Me.timer1.Enabled = False

        ' drop the instance buffer while the gpu device of the canvas is still
        ' alive; the back end itself belongs to the canvas and is released by it
        If m_renderer IsNot Nothing Then
            Call m_renderer.Clear()
        End If

        MyBase.OnFormClosed(e)
    End Sub
End Class
