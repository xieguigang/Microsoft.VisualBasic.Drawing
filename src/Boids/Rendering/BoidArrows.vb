Imports System.Numerics
Imports Microsoft.VisualBasic.Engine.Particles.Boids
Imports Microsoft.VisualBasic.Imaging.Drawing3D
Imports std = System.Math

''' <summary>
''' Turns the state of a <see cref="Field3D"/> into the line vertices of a
''' vector field: one arrow per boid, pointing along its velocity.
''' </summary>
''' <remarks>
''' <para>
''' An arrow is three lines of a line list: the shaft from the tail to the tip
''' and the two wings of the V shaped head. Three lines are six vertices, so the
''' vertex count of a frame is <c>boidCount * 6</c>.
''' </para>
''' <para>
''' The wings are opened in the plane that faces the camera
''' (<paramref name="forward"/> is the view direction), so the head is visible
''' from every orbit angle; a head that was laid out in a fixed world plane would
''' disappear edge on for half of the directions. The length of the arrow follows
''' the speed of the boid, which is what makes the picture read as a vector field
''' instead of a bag of arrow glyphs.
''' </para>
''' <para>
''' The layout of one vertex is the layout of the line channel of the scene
''' pipeline: three <see cref="Single"/> of position and one word that carries
''' the RGBA color of the vertex (see
''' <c>Scene3DInputLayout.LineStride</c>).
''' </para>
''' </remarks>
Public Module BoidArrows

    ''' <summary>
    ''' the number of the <see cref="Single"/> of one line vertex
    ''' </summary>
    Public Const FloatsPerVertex As Integer = 4

    ''' <summary>
    ''' the number of the lines of one arrow: the shaft and the two wings of the
    ''' head
    ''' </summary>
    Public Const LinesPerArrow As Integer = 3

    ''' <summary>
    ''' the number of the vertices of one arrow
    ''' </summary>
    Public Const VerticesPerArrow As Integer = LinesPerArrow * 2

    ''' <summary>
    ''' the length of the head, as a fraction of the length of the arrow
    ''' </summary>
    Public Const HeadLength As Single = 0.42F

    ''' <summary>
    ''' the half width of the head, as a fraction of the length of the arrow
    ''' </summary>
    Public Const HeadWidth As Single = 0.26F

    ''' <summary>
    ''' how much of the arrow length the slowest boid gets, the fastest one gets
    ''' the full length
    ''' </summary>
    Public Const MinimumLengthFraction As Single = 0.35F

    ''' <summary>
    ''' the default length of the arrow of the fastest boid, as a fraction of the
    ''' edge length of the simulation box
    ''' </summary>
    Public Const DefaultLengthFraction As Single = 0.022F

    ''' <summary>
    ''' the predators get a slightly longer arrow, so that they stand out of the
    ''' flock even when their white color is hard to tell apart
    ''' </summary>
    Public Const PredatorLengthScale As Single = 1.35F

    ''' <summary>
    ''' the number of the vertices that the given number of boids needs
    ''' </summary>
    ''' <param name="boidCount">the number of the boids of the frame</param>
    Public Function VertexCount(boidCount As Integer) As Integer
        If boidCount <= 0 Then
            Return 0
        End If

        Return boidCount * VerticesPerArrow
    End Function

    ''' <summary>
    ''' the length of the arrow of the fastest boid, in world units
    ''' </summary>
    ''' <param name="field">the field that supplies the size of the box</param>
    ''' <param name="length">a fixed length, zero asks for the default one</param>
    Public Function ResolveLength(field As Field3D, length As Single) As Single
        If length > 0 Then
            Return length
        End If

        If field Is Nothing Then
            Return 1
        End If

        Return CSng(std.Max(field.Width, std.Max(field.Height, field.Depth)) * DefaultLengthFraction)
    End Function

    ''' <summary>
    ''' fill the line vertices of the arrows of one frame
    ''' </summary>
    ''' <param name="field">the flock of the frame</param>
    ''' <param name="lut">
    ''' the packed ARGB table of the speed colormap, the color of a boid is looked
    ''' up with its normalized speed
    ''' </param>
    ''' <param name="predatorColor">the packed ARGB color of a predator</param>
    ''' <param name="forward">
    ''' the view direction of the camera, the wings of the head are opened
    ''' perpendicular to it
    ''' </param>
    ''' <param name="length">the length of the arrow of the fastest boid, in world units</param>
    ''' <param name="buffer">the destination of the line vertices</param>
    ''' <returns>the number of the written vertices, two per line</returns>
    ''' <remarks>
    ''' The positions are shifted by half of the edge length of the box, so the
    ''' arrows are expressed in the model space of the scene, whose origin is the
    ''' centre of the box (the very same shift that the box wire frame uses).
    ''' </remarks>
    Public Function Build(field As Field3D,
                          lut As Integer(),
                          predatorColor As Integer,
                          forward As Vector3,
                          length As Single,
                          buffer As Single()) As Integer

        If field Is Nothing OrElse buffer Is Nothing OrElse lut Is Nothing OrElse lut.Length = 0 Then
            Return 0
        End If

        Dim state As FlockState3D = field.State
        Dim n As Integer = state.count
        Dim room As Integer = buffer.Length \ FloatsPerVertex

        If n * VerticesPerArrow > room Then
            n = room \ VerticesPerArrow
        End If

        If n <= 0 Then
            Return 0
        End If

        Dim px As Double() = state.px
        Dim py As Double() = state.py
        Dim pz As Double() = state.pz
        Dim vx As Double() = state.vx
        Dim vy As Double() = state.vy
        Dim vz As Double() = state.vz

        Dim halfX As Double = field.Width * 0.5
        Dim halfY As Double = field.Height * 0.5
        Dim halfZ As Double = field.Depth * 0.5
        Dim maxSpeed As Double = std.Max(field.MaxSpeed, 1.0E-06)
        Dim predCount As Integer = std.Min(field.PredatorCount, n)
        Dim maxLength As Single = ResolveLength(field, length)
        Dim levels As Integer = lut.Length

        ' the wing axis is perpendicular to the arrow and to the view direction;
        ' a valid view direction is required, so a degenerate one falls back to
        ' the z axis of the world
        Dim fx As Single = forward.X
        Dim fy As Single = forward.Y
        Dim fz As Single = forward.Z
        Dim viewLength As Single = CSng(std.Sqrt(fx * fx + fy * fy + fz * fz))

        If Not Single.IsFinite(viewLength) OrElse viewLength < 1.0E-06F Then
            fx = 0
            fy = 0
            fz = 1
        Else
            fx /= viewLength
            fy /= viewLength
            fz /= viewLength
        End If

        Dim cursor As Integer = 0

        For i As Integer = 0 To n - 1
            Dim dx As Double = vx(i)
            Dim dy As Double = vy(i)
            Dim dz As Double = vz(i)
            Dim speed As Double = std.Sqrt(dx * dx + dy * dy + dz * dz)

            Dim argb As Integer

            If i < predCount Then
                argb = predatorColor
            Else
                Dim level As Integer = BoidMath.Round(speed / maxSpeed * levels) - 1

                If level < 0 Then
                    level = 0
                ElseIf level >= levels Then
                    level = levels - 1
                End If

                argb = lut(level)
            End If

            ' the direction of a boid that is not moving is taken from the z axis
            Dim ax As Single
            Dim ay As Single
            Dim az As Single

            If speed > 1.0E-09 Then
                ax = CSng(dx / speed)
                ay = CSng(dy / speed)
                az = CSng(dz / speed)
            Else
                ax = 0
                ay = 0
                az = 1
            End If

            Dim fraction As Single = CSng(std.Min(1.0, speed / maxSpeed))
            Dim len As Single = maxLength * (MinimumLengthFraction + (1.0F - MinimumLengthFraction) * fraction)

            If i < predCount Then
                len *= PredatorLengthScale
            End If

            ' the arrow is centred on the boid: the same half of it points ahead
            ' of the flock member and the same half trails it
            Dim cx As Single = CSng(px(i) - halfX)
            Dim cy As Single = CSng(py(i) - halfY)
            Dim cz As Single = CSng(pz(i) - halfZ)
            Dim tailX As Single = cx - ax * len * 0.5F
            Dim tailY As Single = cy - ay * len * 0.5F
            Dim tailZ As Single = cz - az * len * 0.5F
            Dim tipX As Single = cx + ax * len * 0.5F
            Dim tipY As Single = cy + ay * len * 0.5F
            Dim tipZ As Single = cz + az * len * 0.5F

            ' side = normalize(cross(axis, view direction))
            Dim sideX As Single = ay * fz - az * fy
            Dim sideY As Single = az * fx - ax * fz
            Dim sideZ As Single = ax * fy - ay * fx
            Dim sideLength As Single = CSng(std.Sqrt(sideX * sideX + sideY * sideY + sideZ * sideZ))

            If sideLength < 1.0E-05F Then
                ' the arrow points straight at the camera: any perpendicular
                ' will do, the head is seen head on either way
                sideX = 1
                sideY = 0
                sideZ = 0
                sideLength = 1
            Else
                sideX /= sideLength
                sideY /= sideLength
                sideZ /= sideLength
            End If

            ' the locals must not be named headLength/headWidth: VB is case
            ' insensitive, so they would shadow the HeadLength/HeadWidth
            ' constants and read themselves (zero) instead of the tuning values
            Dim head As Single = len * HeadLength
            Dim wing As Single = len * HeadWidth
            Dim collarX As Single = tipX - ax * head
            Dim collarY As Single = tipY - ay * head
            Dim collarZ As Single = tipZ - az * head

            Dim color As Single = BitConverter.Int32BitsToSingle(argb)

            ' the shaft
            buffer(cursor) = tailX : buffer(cursor + 1) = tailY : buffer(cursor + 2) = tailZ : buffer(cursor + 3) = color
            cursor += FloatsPerVertex
            buffer(cursor) = tipX : buffer(cursor + 1) = tipY : buffer(cursor + 2) = tipZ : buffer(cursor + 3) = color
            cursor += FloatsPerVertex

            ' the two wings of the head, they are both anchored on the tip
            buffer(cursor) = tipX : buffer(cursor + 1) = tipY : buffer(cursor + 2) = tipZ : buffer(cursor + 3) = color
            cursor += FloatsPerVertex
            buffer(cursor) = collarX + sideX * wing
            buffer(cursor + 1) = collarY + sideY * wing
            buffer(cursor + 2) = collarZ + sideZ * wing
            buffer(cursor + 3) = color
            cursor += FloatsPerVertex

            buffer(cursor) = tipX : buffer(cursor + 1) = tipY : buffer(cursor + 2) = tipZ : buffer(cursor + 3) = color
            cursor += FloatsPerVertex
            buffer(cursor) = collarX - sideX * wing
            buffer(cursor + 1) = collarY - sideY * wing
            buffer(cursor + 2) = collarZ - sideZ * wing
            buffer(cursor + 3) = color
            cursor += FloatsPerVertex
        Next

        Return cursor \ FloatsPerVertex
    End Function

    ''' <summary>
    ''' the world space direction that the camera looks along
    ''' </summary>
    ''' <param name="camera">the camera of the frame</param>
    ''' <remarks>
    ''' The projection of the scene pipeline is ``p' = p * R`` followed by the
    ''' depth of ``p'.z + viewDistance``, so the camera looks along the world
    ''' vector that maps onto ``(0, 0, 1)``: the third column of the rotation
    ''' matrix of <see cref="SceneTransform.RotationMatrix"/>, which is exactly
    ''' what this function rebuilds.
    ''' </remarks>
    Public Function ViewForward(camera As Camera) As Vector3
        If camera Is Nothing Then
            Return New Vector3(0, 0, 1)
        End If

        Dim radX As Double = camera.AngleX * std.PI / 180.0
        Dim radY As Double = camera.AngleY * std.PI / 180.0
        Dim sX As Single = CSng(std.Sin(radX))
        Dim cX As Single = CSng(std.Cos(radX))
        Dim sY As Single = CSng(std.Sin(radY))
        Dim cY As Single = CSng(std.Cos(radY))

        Return New Vector3(-sY, sX * cY, cX * cY)
    End Function
End Module
