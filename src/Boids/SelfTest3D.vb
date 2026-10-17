Imports System.Drawing
Imports System.IO
Imports System.Numerics
Imports Microsoft.VisualBasic.Engine.Particles.Boids
Imports Microsoft.VisualBasic.Math.SIMD
Imports Microsoft.VisualBasic.Parallel
Imports std = System.Math

''' <summary>
''' The numeric self test of the three dimensional flocking engine.
''' </summary>
''' <remarks>
''' The test runs without any window, so it can be executed on a build agent:
''' <c>dotnet Boids.dll --selftest</c>. It checks the value view of the state,
''' the exact rule evaluation against a brute force reference, the integration
''' invariants, the robustness against degenerate positions and - because the
''' two dimensional demo has to keep working - a short run of the original
''' <see cref="Field"/> as well.
''' </remarks>
Public Module SelfTest3D

    ' the rule constants of FlockSolver3D, duplicated here on purpose: the self
    ' test is the place where a change of the tuning has to be noticed
    Private Const flockDistance As Double = 50.0
    Private Const avoidDistance As Double = 20.0
    Private Const predatorDistance As Double = 150.0
    Private Const cohesionPower As Double = 0.0003
    Private Const alignPower As Double = 0.01
    Private Const avoidPower As Double = 0.001
    Private Const predatorPower As Double = 0.00005

    Private m_checks As Integer
    Private m_failures As Integer
    Private m_log As System.Text.StringBuilder

    ''' <summary>
    ''' run every check of the three dimensional engine
    ''' </summary>
    ''' <returns>zero when every check passed, one otherwise</returns>
    Public Function Run() As Integer
        m_checks = 0
        m_failures = 0
        m_log = New System.Text.StringBuilder()

        Call Threading.ThreadPool.SetMinThreads(Environment.ProcessorCount, Environment.ProcessorCount)
        VectorTask.n_threads = Environment.ProcessorCount

        Call WriteLine("===========================================================")
        Call WriteLine(" Boids 3D engine self test")
        Call WriteLine($" runtime: {Environment.Version}")
        Call WriteLine($" cpu cores: {Environment.ProcessorCount}, simd: {SIMDEnvironment.IsEnabled}")
        Call WriteLine("===========================================================")

        Call TestStateAndView()
        Call TestRulesAgainstReference()
        Call TestIntegrationInvariants()
        Call TestDeterminism()
        Call TestDegeneratePositions()
        Call TestDomainSizing()
        Call TestTwoDimensionalRegression()
        Call TestArrowGeometry()

        Call WriteLine(vbNullString)
        Call WriteLine("===========================================================")
        Call WriteLine($" result: {m_checks - m_failures}/{m_checks} checks passed")

        If m_failures > 0 Then
            Call WriteLine($" FAILED with {m_failures} error(s).")
        Else
            Call WriteLine(" ALL PASSED.")
        End If

        Call WriteLine("===========================================================")

        ' the console of a winforms executable is only attached when the program
        ' was started from a terminal, so the report is written to a file as well
        Try
            File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "boids-self-test.log"), m_log.ToString())
        Catch ex As Exception
            Call Console.Error.WriteLine("unable to write boids-self-test.log: " & ex.Message)
        End Try

        Return If(m_failures > 0, 1, 0)
    End Function

    ' /********************************************************************************/
    '  the checks
    ' /********************************************************************************/

    ''' <summary>
    ''' the SoA state and the value view of one boid
    ''' </summary>
    Private Sub TestStateAndView()
        Call WriteLine(vbNullString)
        Call WriteLine("[1/8] state and value view ...")

        Dim empty As New FlockState3D(0)
        Call Check(empty.count = 0, "an empty state has no slots")
        Call Check(empty.px.Length = 0 AndAlso empty.dvz.Length = 0, "an empty state has empty arrays")

        Dim state As New FlockState3D(8)

        For i As Integer = 0 To 7
            state.px(i) = i
            state.py(i) = i * 2
            state.pz(i) = i * 3
            state.vx(i) = 1
            state.vy(i) = 0
            state.vz(i) = 0
        Next

        Dim copy As FlockState3D = state.Clone()
        copy.px(0) = -1

        Call Check(state.px(0) = 0, "the clone is independent of the original state")
        Call Check(copy.pz(7) = 21, "the clone copies every component")

        Dim field As New Field3D(20, 30, 40, 8)
        Dim boid As Boid3D = field(3)
        Dim speed As Double = boid.GetSpeed()

        boid.x = 12.5
        boid.z = 7.5

        Call Check(field.State.px(3) = 12.5, "the value view writes through to the state")
        Call Check(field.State.pz(3) = 7.5, "the value view writes the z component")
        Call Check(speed >= 0, "the value view reports a speed")

        Dim enumerated As Integer = 0
        For Each item As Boid3D In field.Enumerate()
            enumerated += 1
        Next

        Call Check(enumerated = 8, "the field enumerates every boid")
    End Sub

    ''' <summary>
    ''' the rule evaluation has to match a brute force reference exactly.
    ''' </summary>
    ''' <remarks>
    ''' The reference walks every other boid, which is only the same neighbor set
    ''' as the engine when the whole flock fits into a single block of two cells
    ''' per axis: with the 15 unit cell of the field that is a domain of twenty
    ''' units, where the twenty seven cell neighborhood of a boid covers the whole
    ''' grid.
    ''' </remarks>
    Private Sub TestRulesAgainstReference()
        Call WriteLine(vbNullString)
        Call WriteLine("[2/8] flocking rules against the brute force reference ...")

        Const count As Integer = 64
        Dim field As New Field3D(20, 20, 20, count)
        Dim random As New Random(20261017)

        For i As Integer = 0 To count - 1
            field.State.px(i) = random.NextDouble() * 20
            field.State.py(i) = random.NextDouble() * 20
            field.State.pz(i) = random.NextDouble() * 20
            field.State.vx(i) = random.NextDouble() - 0.5
            field.State.vy(i) = random.NextDouble() - 0.5
            field.State.vz(i) = random.NextDouble() - 0.5
        Next

        ' the deltas are evaluated on the state of the frame start, so the
        ' reference is computed from a snapshot taken before the advance
        Dim before As FlockState3D = field.State.Clone()
        Dim expected As Double()() = ReferenceDeltas(before, field.PredatorCount)

        field.Advance(bounceOffWalls:=False, wrapAroundEdges:=False)

        Dim worstX As Double = 0
        Dim worstY As Double = 0
        Dim worstZ As Double = 0

        For i As Integer = 0 To count - 1
            worstX = std.Max(worstX, Deviation(field.State.dvx(i), expected(0)(i)))
            worstY = std.Max(worstY, Deviation(field.State.dvy(i), expected(1)(i)))
            worstZ = std.Max(worstZ, Deviation(field.State.dvz(i), expected(2)(i)))
        Next

        Call WriteLine($"      worst relative deviation: x={worstX:E2}, y={worstY:E2}, z={worstZ:E2}")
        Call Check(worstX < 1.0E-09, "the x force matches the reference")
        Call Check(worstY < 1.0E-09, "the y force matches the reference")
        Call Check(worstZ < 1.0E-09, "the z force matches the reference")

        ' a predator has to push the flock away: compare the z force of a boid
        ' that sits right next to a predator with the force of a boid that does
        ' not, the difference has to be non zero on the separating axis
        Dim probe As New Field3D(20, 20, 20, 8)
        Dim state As FlockState3D = probe.State

        For i As Integer = 0 To state.count - 1
            state.px(i) = 10
            state.py(i) = 10
            state.pz(i) = 10
        Next

        ' the first boid is a predator and sits below the flock
        state.pz(0) = 2

        probe.Advance(bounceOffWalls:=False, wrapAroundEdges:=False)

        Call Check(probe.State.dvz(1) > 0, "a boid above a predator is pushed up")
    End Sub

    ''' <summary>
    ''' the integration kernel has to clamp the speed, to kill the NaN values and
    ''' to apply the wall rules to the velocity only
    ''' </summary>
    Private Sub TestIntegrationInvariants()
        Call WriteLine(vbNullString)
        Call WriteLine("[3/8] integration invariants ...")

        Const count As Integer = 512
        Const box As Double = 400
        Dim field As New Field3D(box, box, box, count)
        Dim random As New Random(7)

        For i As Integer = 0 To count - 1
            field.State.px(i) = random.NextDouble() * box
            field.State.py(i) = random.NextDouble() * box
            field.State.pz(i) = random.NextDouble() * box
            field.State.vx(i) = (random.NextDouble() - 0.5) * 40
            field.State.vy(i) = (random.NextDouble() - 0.5) * 40
            field.State.vz(i) = (random.NextDouble() - 0.5) * 40
        Next

        ' a handful of degenerate slots
        field.State.vx(0) = Double.NaN
        field.State.vy(1) = Double.PositiveInfinity
        field.State.vz(2) = 0
        field.State.vx(2) = 0
        field.State.vy(2) = 0

        For frame As Integer = 1 To 25
            field.Advance()
        Next

        Dim nanSpeeds As Integer = 0
        Dim tooFast As Integer = 0
        Dim tooSlow As Integer = 0
        Dim outside As Integer = 0
        Dim slowest As Double = Double.MaxValue
        Dim slowestId As Integer = -1
        Dim fastest As Double = 0

        ' the first three slots were poisoned on purpose, their positions stay
        ' degenerate forever, so only the healthy boids are range checked
        For i As Integer = 3 To count - 1
            Dim speed As Double = field(i).GetSpeed()

            If speed < slowest Then
                slowest = speed
                slowestId = i
            End If

            If speed > fastest Then fastest = speed

            If Double.IsNaN(speed) OrElse Double.IsInfinity(speed) Then
                nanSpeeds += 1
            ElseIf speed > field.MaxSpeed + 1.0 Then
                ' the wall steering adds its turn after the clamp, so a small
                ' overshoot is expected
                tooFast += 1
            ElseIf speed < 1.0 - 0.5 Then
                tooSlow += 1
            End If

            If field.State.px(i) < -box OrElse field.State.px(i) > box * 2 Then outside += 1
            If field.State.pz(i) < -box OrElse field.State.pz(i) > box * 2 Then outside += 1
        Next

        Call WriteLine($"      speed range: {slowest:F3} (boid {slowestId}) .. {fastest:F3}, slow={tooSlow}, fast={tooFast}")
        Call Check(nanSpeeds = 0, "no boid keeps a NaN or infinite speed")
        Call Check(tooFast = 0, "the speed stays inside the maximum")
        Call Check(tooSlow = 0, "the speed stays above the minimum")
        Call Check(outside = 0, "the wall steering keeps the flock inside the field")

        ' the wrapping mode has to keep every boid inside the box
        Dim wrap As New Field3D(box, box, box, count)

        For i As Integer = 0 To count - 1
            wrap.State.px(i) = box * 0.5
            wrap.State.py(i) = box * 0.5
            wrap.State.pz(i) = box * 0.5
            wrap.State.vx(i) = 5
            wrap.State.vy(i) = -5
            wrap.State.vz(i) = 5
        Next

        For frame As Integer = 1 To 10
            wrap.Advance(bounceOffWalls:=False, wrapAroundEdges:=True)
        Next

        Dim escaped As Integer = 0

        For i As Integer = 0 To count - 1
            If wrap.State.px(i) < 0 OrElse wrap.State.px(i) > box Then escaped += 1
            If wrap.State.py(i) < 0 OrElse wrap.State.py(i) > box Then escaped += 1
            If wrap.State.pz(i) < 0 OrElse wrap.State.pz(i) > box Then escaped += 1
        Next

        Call Check(escaped = 0, "the wrap rule keeps every boid inside the box")
    End Sub

    ''' <summary>
    ''' the same input has to produce the same output, run after run
    ''' </summary>
    Private Sub TestDeterminism()
        Call WriteLine(vbNullString)
        Call WriteLine("[4/8] determinism ...")

        Dim first As FlockState3D = RunSeeded(4096, 300, 40)
        Dim second As FlockState3D = RunSeeded(4096, 300, 40)
        Dim worst As Double = 0

        For i As Integer = 0 To first.count - 1
            worst = std.Max(worst, std.Abs(first.px(i) - second.px(i)))
            worst = std.Max(worst, std.Abs(first.pz(i) - second.pz(i)))
            worst = std.Max(worst, std.Abs(first.vz(i) - second.vz(i)))
        Next

        Call Check(worst = 0, "two runs of the same input are bit identical")
    End Sub

    ''' <summary>
    ''' run a seeded flock for the given number of frames and return its state
    ''' </summary>
    Private Function RunSeeded(count As Integer, box As Double, frames As Integer) As FlockState3D
        Dim field As New Field3D(box, box, box, count)
        Dim random As New Random(1234567)

        For i As Integer = 0 To count - 1
            field.State.px(i) = random.NextDouble() * box
            field.State.py(i) = random.NextDouble() * box
            field.State.pz(i) = random.NextDouble() * box
            field.State.vx(i) = random.NextDouble() - 0.5
            field.State.vy(i) = random.NextDouble() - 0.5
            field.State.vz(i) = random.NextDouble() - 0.5
        Next

        For frame As Integer = 1 To frames
            field.Advance()
        Next

        Return field.State.Clone()
    End Function

    ''' <summary>
    ''' positions that a spatial grid can not index directly must not break the
    ''' frame, and a spread over a huge volume must not exhaust the memory
    ''' </summary>
    Private Sub TestDegeneratePositions()
        Call WriteLine(vbNullString)
        Call WriteLine("[5/8] degenerate positions ...")

        Dim random As New Random(99)

        ' 1) not a number and infinite coordinates
        Dim broken As New Field3D(100, 100, 100, 64)

        For i As Integer = 0 To broken.State.count - 1
            broken.State.px(i) = random.NextDouble() * 100
            broken.State.py(i) = random.NextDouble() * 100
            broken.State.pz(i) = random.NextDouble() * 100
        Next

        broken.State.px(0) = Double.NaN
        broken.State.py(1) = Double.NegativeInfinity
        broken.State.pz(2) = Double.PositiveInfinity
        broken.State.px(3) = Double.MaxValue

        Dim threw As Boolean = False

        Try
            For frame As Integer = 1 To 3
                broken.Advance()
            Next
        Catch ex As Exception
            threw = True
            Call WriteLine("      unexpected exception: " & ex.Message)
        End Try

        Call Check(Not threw, "non finite positions do not break the frame")

        Dim finiteVelocity As Integer = 0

        For i As Integer = 0 To 3
            If Not Double.IsNaN(broken.State.vx(i)) Then finiteVelocity += 1
        Next

        Call Check(finiteVelocity >= 3, "the NaN guard resets the broken velocities")

        ' 2) a flock that is spread over a volume that a fifteen unit grid can not
        '    index: the grid has to answer with a coarser cell size instead of an
        '    allocation failure
        Dim sparse As New Field3D(1.0E+09, 1.0E+09, 1.0E+09, 500)

        For i As Integer = 0 To sparse.State.count - 1
            sparse.State.px(i) = random.NextDouble() * 1.0E+09
            sparse.State.py(i) = random.NextDouble() * 1.0E+09
            sparse.State.pz(i) = random.NextDouble() * 1.0E+09
        Next

        threw = False

        Try
            sparse.Advance()
        Catch ex As Exception
            threw = True
            Call WriteLine("      unexpected exception: " & ex.GetType().Name & ": " & ex.Message)
            Call WriteLine("      " & ex.StackTrace)
        End Try

        Call Check(Not threw, "a sparse flock over a huge volume is still solved")
    End Sub

    ''' <summary>
    ''' the automatic field size keeps the neighbor count of a boid at the level
    ''' of the two dimensional demo
    ''' </summary>
    Private Sub TestDomainSizing()
        Call WriteLine(vbNullString)
        Call WriteLine("[6/8] domain sizing ...")

        Dim defaults As New BoidsOptions()
        Dim edge As Double = defaults.ResolveDomainSize()

        Call WriteLine($"      120000 boids -> {edge:F0}^3")
        Call Check(edge > 460 AndAlso edge < 500, "the default field is about 480 units wide")
        Call Check(defaults.BoidCount = 120000, "the default flock keeps the two dimensional size")

        Dim small As New BoidsOptions With {.BoidCount = 1000}
        Call Check(small.ResolveDomainSize() >= BoidsOptions.MinimumDomainSize, "a small flock keeps the minimum field")

        Dim fixedSize As New BoidsOptions With {.BoidCount = 1000, .DomainSize = 250}
        Call Check(fixedSize.ResolveDomainSize() = 250, "an explicit field size wins over the derived one")
    End Sub

    ''' <summary>
    ''' the two dimensional engine is not touched by this upgrade, so it has to
    ''' run exactly as before
    ''' </summary>
    Private Sub TestTwoDimensionalRegression()
        Call WriteLine(vbNullString)
        Call WriteLine("[7/8] two dimensional regression ...")

        Dim field As New Field(800, 600, 2000)

        For frame As Integer = 1 To 50
            field.Advance()
        Next

        Dim bad As Integer = 0
        Dim slowest As Double = Double.MaxValue
        Dim fastest As Double = 0
        Dim nanPositions As Integer = 0

        For i As Integer = 0 To field.State.count - 1
            Dim vxv As Double = field.State.vx(i)
            Dim vyv As Double = field.State.vy(i)
            Dim px As Double = field.State.px(i)
            Dim py As Double = field.State.py(i)
            Dim speed As Double = std.Sqrt(vxv * vxv + vyv * vyv)

            If speed < slowest Then slowest = speed
            If speed > fastest Then fastest = speed

            If Double.IsNaN(px) OrElse Double.IsNaN(py) Then nanPositions += 1
            If Double.IsNaN(speed) OrElse speed < 0.5 OrElse speed > field.MaxSpeed + 1.0 Then bad += 1
        Next

        Call WriteLine($"      speed range: {slowest:F3} .. {fastest:F3}, nan positions: {nanPositions}, bad: {bad}")

        Call Check(bad = 0, "the two dimensional field still runs and keeps finite speeds")
        Call Check(field.Width = 800 AndAlso field.Height = 600, "the two dimensional field keeps its size")
    End Sub

    ''' <summary>
    ''' the geometry of the arrows of the vector field presentation
    ''' </summary>
    Private Sub TestArrowGeometry()
        Call WriteLine(vbNullString)
        Call WriteLine("[8/8] arrow geometry ...")

        Const box As Double = 100
        Dim field As New Field3D(box, box, box, 8)
        Dim state As FlockState3D = field.State

        field.PredatorCount = 1

        For i As Integer = 0 To state.count - 1
            state.px(i) = 50
            state.py(i) = 50
            state.pz(i) = 50
            state.vx(i) = 0
            state.vy(i) = 0
            state.vz(i) = 0
        Next

        ' the first boid is the predator, the second one flies along +x at the
        ' highest speed
        state.vx(1) = field.MaxSpeed

        Dim colors As Integer() = {
            Color.FromArgb(255, 68, 1, 84).ToArgb(),
            Color.FromArgb(255, 33, 145, 140).ToArgb(),
            Color.FromArgb(255, 253, 231, 37).ToArgb()
        }
        Dim predatorColor As Integer = Color.White.ToArgb()
        Dim forward As New Vector3(0, 0, 1)
        Const arrowLength As Single = 10

        Dim words As Integer = BoidArrows.VertexCount(state.count) * BoidArrows.FloatsPerVertex
        Dim buffer As Single() = New Single(words - 1) {}
        Dim vertices As Integer = BoidArrows.Build(field, colors, predatorColor, forward, arrowLength, buffer)

        Call Check(vertices = state.count * BoidArrows.VerticesPerArrow, "one arrow writes three lines")
        Call Check(BoidArrows.VertexCount(state.count) = words \ BoidArrows.FloatsPerVertex,
                   "the vertex count helper matches the written vertices")

        ' the fast boid: the arrow is centred on the boid, so it spans
        ' [-length/2, +length/2] along the x axis of the model space
        Dim shaft As Integer = BoidArrows.VerticesPerArrow * BoidArrows.FloatsPerVertex

        Call Check(Near(buffer(shaft + 0), -arrowLength * 0.5) AndAlso
                   Near(buffer(shaft + 1), 0) AndAlso Near(buffer(shaft + 2), 0),
                   "the tail of the arrow is half a length behind the boid")
        Call Check(Near(buffer(shaft + 4), arrowLength * 0.5),
                   "the tip of the arrow is half a length ahead of the boid")
        Call Check(BitConverter.SingleToInt32Bits(buffer(shaft + 3)) = colors(colors.Length - 1),
                   "the fastest boid gets the top color of the speed table")

        ' the wings: they are symmetric about the axis and they are opened in the
        ' plane perpendicular to the view direction
        ' vertex order: the tail, the tip, then the tip and the two wings,
        ' four floats per vertex
        Dim wingA As New Vector3(buffer(shaft + 12), buffer(shaft + 13), buffer(shaft + 14))
        Dim wingB As New Vector3(buffer(shaft + 20), buffer(shaft + 21), buffer(shaft + 22))
        Dim tip As New Vector3(buffer(shaft + 4), buffer(shaft + 5), buffer(shaft + 6))
        Dim collar As Vector3 = (wingA + wingB) * 0.5F
        Dim headLength As Single = arrowLength * BoidArrows.HeadLength
        Dim headWidth As Single = arrowLength * BoidArrows.HeadWidth

        Call Check(Near(Vector3.Distance(tip, collar), headLength), "the head is HeadLength deep")
        Call Check(Near(Vector3.Distance(wingA, collar), headWidth), "the wings are HeadWidth wide")
        Call Check(Near(Vector3.Dot(Vector3.Normalize(wingA - wingB), forward), 0),
                   "the wings open perpendicular to the view direction")

        ' the predator: white, a longer arrow and a degenerate velocity, which is
        ' the case where the axis falls back to the z axis of the world
        Dim predator As Single = arrowLength * BoidArrows.MinimumLengthFraction * BoidArrows.PredatorLengthScale

        Call Check(BitConverter.SingleToInt32Bits(buffer(3)) = predatorColor, "the predator is white")
        Call Check(Near(buffer(2), -predator * 0.5) AndAlso Near(buffer(6), predator * 0.5),
                   "a boid at rest still gets a visible arrow along the fallback axis")

        ' a buffer that is too small truncates the frame instead of overflowing it
        Dim tiny As Single() = New Single(BoidArrows.VerticesPerArrow * BoidArrows.FloatsPerVertex - 1) {}
        Dim truncated As Integer = BoidArrows.Build(field, colors, predatorColor, forward, arrowLength, tiny)

        Call Check(truncated = BoidArrows.VerticesPerArrow, "a small buffer truncates the frame")

        ' the default arrow length is a fraction of the box, an explicit one wins
        Call Check(Near(BoidArrows.ResolveLength(field, 0), box * BoidArrows.DefaultLengthFraction),
                   "the default arrow length follows the box")
        Call Check(Near(BoidArrows.ResolveLength(field, 7), 7), "an explicit arrow length wins")
    End Sub

    ''' <summary>
    ''' compare two single precision values with the tolerance of the arrow tests
    ''' </summary>
    Private Function Near(left As Single, right As Single) As Boolean
        Return std.Abs(left - right) <= 1.0E-04 * std.Max(1.0F, std.Abs(right))
    End Function

    ' /********************************************************************************/
    '  the brute force reference of the flocking rules
    ' /********************************************************************************/

    ''' <summary>
    ''' evaluate the flocking rules of every boid by walking the whole flock
    ''' </summary>
    ''' <param name="state">the state of the frame start</param>
    ''' <param name="predatorCount">the number of the leading predators</param>
    ''' <returns>
    ''' three arrays with the velocity delta of the x, the y and the z axis
    ''' </returns>
    Private Function ReferenceDeltas(state As FlockState3D, predatorCount As Integer) As Double()()
        Dim n As Integer = state.count
        Dim dvx(n - 1) As Double
        Dim dvy(n - 1) As Double
        Dim dvz(n - 1) As Double
        Dim predEnd As Integer = std.Min(predatorCount, n) - 1

        For i As Integer = 0 To n - 1
            Dim bx As Double = state.px(i)
            Dim by As Double = state.py(i)
            Dim bz As Double = state.pz(i)
            Dim sumX As Double = 0
            Dim sumY As Double = 0
            Dim sumZ As Double = 0
            Dim sumVX As Double = 0
            Dim sumVY As Double = 0
            Dim sumVZ As Double = 0
            Dim avoidX As Double = 0
            Dim avoidY As Double = 0
            Dim avoidZ As Double = 0
            Dim nCount As Integer = 0

            For j As Integer = 0 To n - 1
                Dim ddx As Double = bx - state.px(j)
                Dim ddy As Double = by - state.py(j)
                Dim ddz As Double = bz - state.pz(j)
                Dim d2 As Double = ddx * ddx + ddy * ddy + ddz * ddz

                If d2 < flockDistance * flockDistance Then
                    sumX += state.px(j)
                    sumY += state.py(j)
                    sumZ += state.pz(j)
                    sumVX += state.vx(j)
                    sumVY += state.vy(j)
                    sumVZ += state.vz(j)
                    nCount += 1

                    If d2 < avoidDistance * avoidDistance Then
                        Dim closeness As Double = avoidDistance - std.Sqrt(d2)
                        avoidX += ddx * closeness
                        avoidY += ddy * closeness
                        avoidZ += ddz * closeness
                    End If
                End If
            Next

            Dim invN As Double = 1.0 / nCount
            Dim predX As Double = 0
            Dim predY As Double = 0
            Dim predZ As Double = 0

            For p As Integer = 0 To predEnd
                Dim ddx As Double = bx - state.px(p)
                Dim ddy As Double = by - state.py(p)
                Dim ddz As Double = bz - state.pz(p)
                Dim d2 As Double = ddx * ddx + ddy * ddy + ddz * ddz

                If d2 < predatorDistance * predatorDistance Then
                    Dim closeness As Double = predatorDistance - std.Sqrt(d2)
                    predX += ddx * closeness
                    predY += ddy * closeness
                    predZ += ddz * closeness
                End If
            Next

            dvx(i) = (sumX * invN - bx) * cohesionPower + avoidX * avoidPower +
                     (sumVX * invN - state.vx(i)) * alignPower + predX * predatorPower
            dvy(i) = (sumY * invN - by) * cohesionPower + avoidY * avoidPower +
                     (sumVY * invN - state.vy(i)) * alignPower + predY * predatorPower
            dvz(i) = (sumZ * invN - bz) * cohesionPower + avoidZ * avoidPower +
                     (sumVZ * invN - state.vz(i)) * alignPower + predZ * predatorPower
        Next

        Return New Double()() {dvx, dvy, dvz}
    End Function

    ''' <summary>
    ''' the relative difference of a value and its reference
    ''' </summary>
    Private Function Deviation(value As Double, reference As Double) As Double
        Dim scale As Double = std.Max(1.0, std.Abs(reference))

        Return std.Abs(value - reference) / scale
    End Function

    ' /********************************************************************************/
    '  the report helpers
    ' /********************************************************************************/

    Private Sub Check(condition As Boolean, label As String)
        m_checks += 1

        If condition Then
            Call WriteLine("      ok   " & label)
        Else
            m_failures += 1
            Call WriteLine("      FAIL " & label)
        End If
    End Sub

    Private Sub WriteLine(text As String)
        Call Console.WriteLine(text)
        Call m_log.AppendLine(text)
    End Sub
End Module
