Imports System.Drawing
Imports Microsoft.VisualBasic.Imaging.Drawing3D
Imports std = System.Math

Namespace Scene3D

    ''' <summary>
    ''' the kind of the element that a hit test found
    ''' </summary>
    Public Enum SceneHitKind

        ''' <summary>the pick found nothing within the pick radius</summary>
        None = 0

        ''' <summary>a point of the point cloud</summary>
        Point = 1

        ''' <summary>a connection line</summary>
        Line = 2

    End Enum

    ''' <summary>
    ''' the result of a hit test: the kind of the element, its index in the scene
    ''' and its distance to the query position in pixels
    ''' </summary>
    Public Structure SceneHitTest

        Sub New(kind As SceneHitKind, index As Integer, distance As Single)
            Me.Kind = kind
            Me.Index = index
            Me.Distance = distance
        End Sub

        ''' <summary>
        ''' what was hit
        ''' </summary>
        Public Property Kind As SceneHitKind

        ''' <summary>
        ''' the index of the element in <see cref="Scene.Points"/> or in
        ''' <see cref="Scene.Lines"/>, or -1 when nothing was hit
        ''' </summary>
        Public Property Index As Integer

        ''' <summary>
        ''' the distance of the element to the query position, in pixels
        ''' </summary>
        Public Property Distance As Single

        ''' <summary>
        ''' did the pick hit an element?
        ''' </summary>
        Public ReadOnly Property HasHit As Boolean
            Get
                Return Kind <> SceneHitKind.None
            End Get
        End Property

        ''' <summary>
        ''' the result of a miss
        ''' </summary>
        Public Shared Function Miss() As SceneHitTest
            Return New SceneHitTest(SceneHitKind.None, -1, Single.MaxValue)
        End Function

        Public Overrides Function ToString() As String
            If Not HasHit Then
                Return "no hit"
            End If

            Return $"{Kind} #{Index} @ {Distance:F1} px"
        End Function

    End Structure

    ''' <summary>
    ''' Picking of the elements of a scene on the screen space of a canvas.
    ''' </summary>
    ''' <remarks>
    ''' The elements are projected with the very same camera formulas that the
    ''' polygon painter back end uses (<c>camera.Rotate</c> + <c>camera.Project</c>),
    ''' so a pick matches the element that the user actually sees on the canvas.
    ''' 
    ''' The test is a linear scan over the projected elements. The point cloud of a
    ''' whole brain holds about 140,000 points, which takes a few milliseconds and
    ''' is fine for a mouse click, while the connection graph of such a brain holds
    ''' millions of lines: the lines are therefore subsampled (see
    ''' <see cref="MaxTestedLines"/>) so that a click can never block the host
    ''' application. A caller that needs an exact pick of the lines can walk
    ''' <see cref="Scene.Lines"/> itself with <see cref="ScreenOf"/>.
    ''' </remarks>
    Public Module SceneHitTester

        ''' <summary>
        ''' the highest number of connection lines that one hit test walks
        ''' </summary>
        Public Const MaxTestedLines As Integer = 400000

        ''' <summary>
        ''' the default pick radius, in pixels
        ''' </summary>
        Public Const DefaultRadius As Integer = 8

        ''' <summary>
        ''' pick the element that is closest to the given canvas position
        ''' </summary>
        ''' <remarks>
        ''' a point of the point cloud wins over a line when both are within the
        ''' pick radius: the neurons are the primary object of interest and a dense
        ''' graph would otherwise make the points unpickable
        ''' </remarks>
        ''' <param name="scene">the scene to pick from</param>
        ''' <param name="camera">the camera that projects the scene</param>
        ''' <param name="x">the horizontal position on the canvas, in pixels</param>
        ''' <param name="y">the vertical position on the canvas, in pixels</param>
        ''' <param name="radius">the pick radius, in pixels</param>
        Public Function HitTest(scene As Scene,
                                camera As Camera,
                                x As Single,
                                y As Single,
                                Optional radius As Integer = DefaultRadius) As SceneHitTest

            If scene Is Nothing OrElse camera Is Nothing Then
                Return SceneHitTest.Miss()
            End If

            Dim pointIndex As Integer = HitTestPoint(scene, camera, x, y, radius)

            If pointIndex >= 0 Then
                Return New SceneHitTest(SceneHitKind.Point, pointIndex, DistancePoint(scene, camera, pointIndex, x, y))
            End If

            Dim lineIndex As Integer = HitTestLine(scene, camera, x, y, radius)

            If lineIndex >= 0 Then
                Return New SceneHitTest(SceneHitKind.Line, lineIndex, DistanceLine(scene, camera, lineIndex, x, y))
            End If

            Return SceneHitTest.Miss()
        End Function

        ''' <summary>
        ''' pick the point of the point cloud that is closest to the given canvas
        ''' position, or -1 when there is none within the pick radius
        ''' </summary>
        ''' <remarks>
        ''' The cloud is rotated in one batch through <c>Camera.Rotate(points)</c>:
        ''' that overload builds the rotation matrix once for the whole cloud and
        ''' runs the hardware accelerated transform in parallel, while rotating the
        ''' points one by one would evaluate six trigonometric functions per point —
        ''' with a whole brain cloud of 139,255 points that difference is the
        ''' latency of a mouse click (tens of milliseconds against a few).
        ''' </remarks>
        Public Function HitTestPoint(scene As Scene,
                                     camera As Camera,
                                     x As Single,
                                     y As Single,
                                     Optional radius As Integer = DefaultRadius) As Integer

            Dim cloud As PointCloudPoint() = If(scene Is Nothing, Nothing, scene.Points)

            If cloud Is Nothing OrElse cloud.Length = 0 Then
                Return -1
            End If

            Dim points As Point3D() = New Point3D(cloud.Length - 1) {}

            For i As Integer = 0 To cloud.Length - 1
                points(i) = New Point3D(cloud(i).X, cloud(i).Y, cloud(i).Z)
            Next

            Dim rotated As Point3D() = camera.Rotate(points)

            Return projectNearest(rotated, camera, x, y, radius)
        End Function

        ''' <summary>
        ''' the index of the rotated point that projects closest to the given canvas
        ''' position within the pick radius
        ''' </summary>
        Private Function projectNearest(rotated As Point3D(), camera As Camera, x As Single, y As Single, radius As Integer) As Integer
            Dim view As Size = camera.Screen
            Dim viewDistance As Single = camera.ViewDistance
            Dim fov As Single = camera.FieldOfView
            Dim offsetX As Single = camera.Offset.X
            Dim offsetY As Single = camera.Offset.Y
            Dim halfWidth As Single = view.Width / 2.0F
            Dim halfHeight As Single = view.Height / 2.0F
            Dim limit As Single = radius * radius
            Dim best As Integer = -1
            Dim bestDistance As Single = radius

            For i As Integer = 0 To rotated.Length - 1
                Dim p As Point3D = rotated(i)
                Dim depth As Single = viewDistance + CSng(p.Z)

                ' a point behind the camera projects through a zero factor, testing
                ' it would produce false positives at the centre of the screen
                If depth <= 0 Then
                    Continue For
                End If

                Dim factor As Single = fov / depth
                Dim screenX As Single = CSng(p.X) * factor + halfWidth + offsetX
                Dim screenY As Single = CSng(p.Y) * factor + halfHeight + offsetY
                Dim dx As Single = screenX - x
                Dim dy As Single = screenY - y
                Dim d2 As Single = dx * dx + dy * dy

                If d2 <= limit AndAlso d2 <= bestDistance * bestDistance Then
                    bestDistance = CSng(std.Sqrt(d2))
                    best = i
                End If
            Next

            Return best
        End Function

        ''' <summary>
        ''' pick the connection line that is closest to the given canvas position,
        ''' or -1 when there is none within the pick radius
        ''' </summary>
        Public Function HitTestLine(scene As Scene,
                                    camera As Camera,
                                    x As Single,
                                    y As Single,
                                    Optional radius As Integer = DefaultRadius) As Integer

            Dim lines As LineSegment() = If(scene Is Nothing, Nothing, scene.Lines)

            If lines Is Nothing OrElse lines.Length = 0 Then
                Return -1
            End If

            ' a graph of a whole brain provides millions of lines, the scan is
            ' subsampled so that a click stays responsive
            Dim stride As Integer = 1

            If lines.Length > MaxTestedLines Then
                stride = CInt(std.Ceiling(lines.Length / CDbl(MaxTestedLines)))
            End If

            Dim best As Integer = -1
            Dim bestDistance As Single = radius

            For i As Integer = 0 To lines.Length - 1 Step stride
                Dim a As PointF
                Dim b As PointF

                If Not ScreenOf(camera, lines(i).A, a) Then
                    Continue For
                End If
                If Not ScreenOf(camera, lines(i).B, b) Then
                    Continue For
                End If

                Dim d As Single = CSng(DistanceToSegment(x, y, a, b))

                If d <= bestDistance Then
                    bestDistance = d
                    best = i
                End If
            Next

            Return best
        End Function

        ''' <summary>
        ''' project one world point of the scene onto the canvas
        ''' </summary>
        ''' <returns>false when the point is behind the camera</returns>
        Public Function ScreenOf(camera As Camera, p As Point3D, ByRef screen As PointF) As Boolean
            Dim rotated As Point3D = camera.Rotate(p)
            Dim depth As Single = camera.ViewDistance + CSng(rotated.Z)

            If depth <= 0 Then
                Return False
            End If

            Dim factor As Single = camera.FieldOfView / depth
            Dim view As Size = camera.Screen

            screen = New PointF(
                CSng(rotated.X) * factor + view.Width / 2.0F + camera.Offset.X,
                CSng(rotated.Y) * factor + view.Height / 2.0F + camera.Offset.Y)

            Return True
        End Function

        ''' <summary>
        ''' the distance of a point of the cloud to a canvas position, in pixels
        ''' </summary>
        Private Function DistancePoint(scene As Scene, camera As Camera, index As Integer, x As Single, y As Single) As Single
            Dim p As PointCloudPoint = scene.Points(index)
            Dim screen As PointF

            If Not ScreenOf(camera, New Point3D(p.X, p.Y, p.Z), screen) Then
                Return Single.MaxValue
            End If

            Return CSng(std.Sqrt((screen.X - x) ^ 2 + (screen.Y - y) ^ 2))
        End Function

        ''' <summary>
        ''' the distance of a connection line to a canvas position, in pixels
        ''' </summary>
        Private Function DistanceLine(scene As Scene, camera As Camera, index As Integer, x As Single, y As Single) As Single
            Dim line As LineSegment = scene.Lines(index)
            Dim a As PointF
            Dim b As PointF

            If Not ScreenOf(camera, line.A, a) Then
                Return Single.MaxValue
            End If
            If Not ScreenOf(camera, line.B, b) Then
                Return Single.MaxValue
            End If

            Return CSng(DistanceToSegment(x, y, a, b))
        End Function

        ''' <summary>
        ''' the distance of a canvas position to the segment between two projected
        ''' end points
        ''' </summary>
        Private Function DistanceToSegment(x As Single, y As Single, a As PointF, b As PointF) As Double
            Dim dx As Double = b.X - a.X
            Dim dy As Double = b.Y - a.Y
            Dim length2 As Double = dx * dx + dy * dy

            If length2 <= 0 Then
                Return std.Sqrt((x - a.X) ^ 2 + (y - a.Y) ^ 2)
            End If

            Dim t As Double = ((x - a.X) * dx + (y - a.Y) * dy) / length2

            If t < 0 Then
                t = 0
            ElseIf t > 1 Then
                t = 1
            End If

            Dim px As Double = a.X + t * dx
            Dim py As Double = a.Y + t * dy

            Return std.Sqrt((x - px) ^ 2 + (y - py) ^ 2)
        End Function

    End Module

End Namespace
