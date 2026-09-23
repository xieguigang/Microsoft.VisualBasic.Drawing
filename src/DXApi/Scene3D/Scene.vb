Imports System.Drawing
Imports Microsoft.VisualBasic.Imaging.Drawing3D
Imports std = System.Math

Namespace Scene3D

    ''' <summary>
    ''' A renderable 3D scene: a set of faces and a set of point cloud points.
    ''' </summary>
    ''' <remarks>
    ''' The scene owns the model data only, the camera and the lighting are
    ''' separate objects (<see cref="OrbitCameraController"/>, <see cref="SceneLighting"/>).
    '''
    ''' Every model that is loaded into the scene is translated so that its
    ''' centroid becomes the origin of the model space: the model therefore
    ''' rotates around its own centre. The bounding sphere radius and the lowest
    ''' Z coordinate (the height of the ground grid) are computed as well.
    ''' </remarks>
    Public Class Scene

        ''' <summary>the default pitch angle of the camera, in degrees</summary>
        Public Const DefaultAngleX As Single = 20
        ''' <summary>the default yaw angle of the camera, in degrees</summary>
        Public Const DefaultAngleY As Single = -30
        ''' <summary>the default roll angle of the camera, in degrees</summary>
        Public Const DefaultAngleZ As Single = 0

        Private m_surfaces As Surface() = New Surface() {}
        Private m_points As PointCloudPoint() = New PointCloudPoint() {}
        Private m_lines As LineSegment() = New LineSegment() {}
        Private m_center As Point3D = New Point3D(0, 0, 0)
        Private m_radius As Double = 1
        Private m_groundZ As Double = 0
        Private m_intensityMin As Double = 0
        Private m_intensityMax As Double = 1
        Private m_version As Integer = 0

        ''' <summary>
        ''' the faces of the model, an empty array means that the scene holds a
        ''' point cloud only
        ''' </summary>
        Public ReadOnly Property Surfaces As Surface()
            Get
                Return m_surfaces
            End Get
        End Property

        ''' <summary>
        ''' the points of the point cloud, an empty array means that the scene
        ''' holds faces only
        ''' </summary>
        Public ReadOnly Property Points As PointCloudPoint()
            Get
                Return m_points
            End Get
        End Property

        ''' <summary>
        ''' the connection lines of the scene, an empty array means that the scene
        ''' has no connections to draw
        ''' </summary>
        ''' <remarks>
        ''' the lines are an overlay: they are stored next to the faces and the
        ''' points instead of replacing them, so a network graph can be drawn on
        ''' top of the point cloud of its neurons
        ''' </remarks>
        Public ReadOnly Property Lines As LineSegment()
            Get
                Return m_lines
            End Get
        End Property

        ''' <summary>
        ''' the centroid of the loaded model, the model is always translated so
        ''' that this centroid becomes the origin
        ''' </summary>
        Public ReadOnly Property Center As Point3D
            Get
                Return m_center
            End Get
        End Property

        ''' <summary>
        ''' the radius of the bounding sphere around the model centroid
        ''' </summary>
        Public ReadOnly Property Radius As Double
            Get
                Return m_radius
            End Get
        End Property

        ''' <summary>
        ''' the Z coordinate of the lowest model point, the ground grid is drawn
        ''' at this height so that it supports the model
        ''' </summary>
        Public ReadOnly Property GroundZ As Double
            Get
                Return m_groundZ
            End Get
        End Property

        ''' <summary>
        ''' the lowest scalar value of the point cloud
        ''' </summary>
        Public ReadOnly Property IntensityMin As Double
            Get
                Return m_intensityMin
            End Get
        End Property

        ''' <summary>
        ''' the highest scalar value of the point cloud
        ''' </summary>
        Public ReadOnly Property IntensityMax As Double
            Get
                Return m_intensityMax
            End Get
        End Property

        ''' <summary>
        ''' the number of the faces in this scene
        ''' </summary>
        Public ReadOnly Property SurfaceCount As Integer
            Get
                Return m_surfaces.Length
            End Get
        End Property

        ''' <summary>
        ''' the number of the points in this scene
        ''' </summary>
        Public ReadOnly Property PointCount As Integer
            Get
                Return m_points.Length
            End Get
        End Property

        ''' <summary>
        ''' the number of the connection lines in this scene
        ''' </summary>
        Public ReadOnly Property LineCount As Integer
            Get
                Return m_lines.Length
            End Get
        End Property

        ''' <summary>
        ''' does the scene hold any geometry at all?
        ''' </summary>
        Public ReadOnly Property HasData As Boolean
            Get
                Return m_surfaces.Length > 0 OrElse m_points.Length > 0 OrElse m_lines.Length > 0
            End Get
        End Property

        ''' <summary>
        ''' the revision number of the geometry of this scene.
        ''' </summary>
        ''' <remarks>
        ''' The counter is raised every time the geometry is replaced, so a
        ''' rendering back end that keeps a copy of the geometry on the gpu can
        ''' detect that its copy became stale without comparing the whole model.
        ''' </remarks>
        Public ReadOnly Property Version As Integer
            Get
                Return m_version
            End Get
        End Property

        ''' <summary>
        ''' drop all of the geometry of the scene
        ''' </summary>
        Public Sub Clear()
            m_surfaces = New Surface() {}
            m_points = New PointCloudPoint() {}
            m_lines = New LineSegment() {}
            m_center = New Point3D(0, 0, 0)
            m_radius = 1
            m_groundZ = 0
            m_intensityMin = 0
            m_intensityMax = 1
            m_version += 1
        End Sub

        ''' <summary>
        ''' load a set of faces into this scene, the faces are translated so that
        ''' the model centroid becomes the origin
        ''' </summary>
        ''' <param name="faces">
        ''' the faces of the model, degenerate faces (less than three vertices)
        ''' are ignored
        ''' </param>
        Public Sub LoadSurfaces(faces As IEnumerable(Of Surface))
            m_points = New PointCloudPoint() {}
            m_intensityMin = 0
            m_intensityMax = 1

            Dim previous As Point3D = m_center
            Dim accepted As New List(Of Surface)()
            Dim allPoints As New List(Of Point3D)()

            If faces IsNot Nothing Then
                For Each face As Surface In faces
                    If face IsNot Nothing AndAlso face.vertices IsNot Nothing AndAlso face.vertices.Length >= 3 Then
                        accepted.Add(face)
                        allPoints.AddRange(face.vertices)
                    End If
                Next
            End If

            m_center = CentroidOf(allPoints)

            ' the connection lines share the model space, so they follow the new
            ' centre; the bounding sphere grows with them so that FitView keeps
            ' the end points of the lines inside the view
            Call TranslateLines(previous, m_center)

            m_radius = RadiusWithLines(RadiusOf(allPoints, m_center), m_center)

            Dim centered(accepted.Count - 1) As Surface

            For i As Integer = 0 To accepted.Count - 1
                Dim face As Surface = accepted(i)
                centered(i) = New Surface With {
                    .brush = face.brush,
                    .vertices = face.vertices _
                        .Select(Function(p As Point3D) p - m_center) _
                        .ToArray()
                }
            Next

            m_surfaces = centered
            m_groundZ = LowestZ(m_surfaces, m_points, m_lines)
            m_version += 1
        End Sub

        ''' <summary>
        ''' load a point cloud into this scene, the points are translated so that
        ''' the model centroid becomes the origin
        ''' </summary>
        ''' <param name="points">the points of the cloud</param>
        Public Sub LoadPointCloud(points As IEnumerable(Of PointCloudPoint))
            m_surfaces = New Surface() {}

            If points Is Nothing Then
                m_points = New PointCloudPoint() {}
            Else
                m_points = points.ToArray()
            End If

            Dim previous As Point3D = m_center
            Dim source As New List(Of Point3D)(m_points.Length)
            For Each p As PointCloudPoint In m_points
                source.Add(New Point3D(p.X, p.Y, p.Z))
            Next

            m_center = CentroidOf(source)

            ' see LoadSurfaces: the lines follow the centre of the primary
            ' geometry and participate in the bounding sphere
            Call TranslateLines(previous, m_center)

            m_radius = RadiusWithLines(RadiusOf(source, m_center), m_center)

            Dim minIntensity As Double = Double.MaxValue
            Dim maxIntensity As Double = Double.MinValue

            For i As Integer = 0 To m_points.Length - 1
                Dim p As PointCloudPoint = m_points(i)
                p.X -= m_center.X
                p.Y -= m_center.Y
                p.Z -= m_center.Z
                m_points(i) = p

                If p.Intensity < minIntensity Then minIntensity = p.Intensity
                If p.Intensity > maxIntensity Then maxIntensity = p.Intensity
            Next

            If maxIntensity <= minIntensity Then
                minIntensity = 0
                maxIntensity = 1
            End If

            m_intensityMin = minIntensity
            m_intensityMax = maxIntensity
            m_groundZ = LowestZ(m_surfaces, m_points, m_lines)
            m_version += 1
        End Sub

        ''' <summary>
        ''' load the connection lines of this scene, they are drawn on top of the
        ''' faces and of the point cloud
        ''' </summary>
        ''' <remarks>
        ''' The lines share the model space of the primary geometry: they are
        ''' translated by the centre of the point cloud (or of the faces) that was
        ''' loaded before them, so the natural order "load the cloud, then load its
        ''' connections" puts both into the same space. Loading the primary geometry
        ''' afterwards re-translates the lines, so the reverse order works as well.
        ''' The bounding sphere grows with the lines: without that FitView would clip
        ''' the end points of the connections that stick out of the point cloud.
        ''' </remarks>
        ''' <param name="lines">
        ''' the lines of the graph, <c>Nothing</c> or an empty sequence drops the
        ''' lines of the scene
        ''' </param>
        Public Sub LoadLineSegments(lines As IEnumerable(Of LineSegment))
            If lines Is Nothing Then
                m_lines = New LineSegment() {}
            Else
                m_lines = lines.ToArray()
            End If

            Dim center As Point3D = m_center

            For i As Integer = 0 To m_lines.Length - 1
                Dim line As LineSegment = m_lines(i)

                line.A = New Point3D(line.A.X - center.X, line.A.Y - center.Y, line.A.Z - center.Z)
                line.B = New Point3D(line.B.X - center.X, line.B.Y - center.Y, line.B.Z - center.Z)
                m_lines(i) = line
            Next

            m_radius = RadiusWithLines(m_radius, center)
            m_groundZ = LowestZ(m_surfaces, m_points, m_lines)
            m_version += 1
        End Sub

        ''' <summary>
        ''' drop the connection lines of the scene and keep the faces and the point
        ''' cloud
        ''' </summary>
        Public Sub ClearLines()
            If m_lines.Length = 0 Then
                Return
            End If

            m_lines = New LineSegment() {}
            m_version += 1
        End Sub

        ''' <summary>
        ''' restore the default rotation angles and the screen space offset of the
        ''' given camera
        ''' </summary>
        Public Shared Sub ApplyDefaultAngles(camera As Camera)
            If camera Is Nothing Then
                Return
            End If

            camera.AngleX = DefaultAngleX
            camera.AngleY = DefaultAngleY
            camera.AngleZ = DefaultAngleZ
            camera.Offset = New PointF(0, 0)
        End Sub

        ''' <summary>
        ''' compute the view distance that makes the whole model visible on the
        ''' given canvas size
        ''' </summary>
        ''' <remarks>
        ''' The projection of this engine is ``factor = fov / (viewDistance + z)``,
        ''' so the ratio ``radius * fov / viewDistance`` decides how much of the
        ''' screen the model covers. Scaling both the field of view and the view
        ''' distance by the same perspective factor keeps the coverage unchanged
        ''' while the perspective distortion is weakened.
        ''' </remarks>
        Public Sub FitView(camera As Camera, screenSize As Size)
            If camera Is Nothing Then
                Return
            End If

            Dim minDim As Integer = std.Min(screenSize.Width, screenSize.Height)
            If minDim <= 0 Then
                minDim = 600
            End If

            Const perspectiveK As Double = 8

            camera.Screen = screenSize
            camera.FieldOfView = CSng(256 * perspectiveK)

            Dim distance As Double = m_radius * camera.FieldOfView / (0.4 * minDim)
            If distance < 1 Then
                distance = 1
            End If

            camera.ViewDistance = CSng(distance)
        End Sub

        ''' <summary>
        ''' the mean location of the given points, or the origin when there is no
        ''' point at all
        ''' </summary>
        Public Shared Function CentroidOf(points As IReadOnlyList(Of Point3D)) As Point3D
            If points Is Nothing OrElse points.Count = 0 Then
                Return New Point3D(0, 0, 0)
            End If

            Dim x As Double = 0
            Dim y As Double = 0
            Dim z As Double = 0

            For Each p As Point3D In points
                x += p.X
                y += p.Y
                z += p.Z
            Next

            Return New Point3D(x / points.Count, y / points.Count, z / points.Count)
        End Function

        ''' <summary>
        ''' the maximum distance of the given points from the given centre, at
        ''' least one
        ''' </summary>
        Public Shared Function RadiusOf(points As IReadOnlyList(Of Point3D), center As Point3D) As Double
            Dim max As Double = 0

            If points IsNot Nothing Then
                For Each p As Point3D In points
                    Dim dx As Double = p.X - center.X
                    Dim dy As Double = p.Y - center.Y
                    Dim dz As Double = p.Z - center.Z
                    Dim d As Double = std.Sqrt(dx * dx + dy * dy + dz * dz)

                    If d > max Then
                        max = d
                    End If
                Next
            End If

            If max <= 0 Then
                Return 1
            End If

            Return max
        End Function

        ''' <summary>
        ''' translate the connection lines so that they stay in the same model space
        ''' when the centre of the primary geometry moves
        ''' </summary>
        ''' <remarks>
        ''' the model space is "world - centre", so a line that is already expressed
        ''' in the previous space has to be shifted by the change of the centre
        ''' </remarks>
        Private Sub TranslateLines(previous As Point3D, current As Point3D)
            If m_lines.Length = 0 Then
                Return
            End If

            Dim dx As Double = current.X - previous.X
            Dim dy As Double = current.Y - previous.Y
            Dim dz As Double = current.Z - previous.Z

            If dx = 0 AndAlso dy = 0 AndAlso dz = 0 Then
                Return
            End If

            For i As Integer = 0 To m_lines.Length - 1
                Dim line As LineSegment = m_lines(i)

                line.A = New Point3D(line.A.X - dx, line.A.Y - dy, line.A.Z - dz)
                line.B = New Point3D(line.B.X - dx, line.B.Y - dy, line.B.Z - dz)
                m_lines(i) = line
            Next
        End Sub

        ''' <summary>
        ''' grow the bounding radius so that it covers the end points of the
        ''' connection lines as well
        ''' </summary>
        ''' <remarks>
        ''' The radius is computed without allocating a point list: a connectome
        ''' provides millions of lines, so the end points are walked in place.
        ''' </remarks>
        Private Function RadiusWithLines(radius As Double, center As Point3D) As Double
            For i As Integer = 0 To m_lines.Length - 1
                Dim line As LineSegment = m_lines(i)
                Dim a As Double = Distance(line.A, center)
                Dim b As Double = Distance(line.B, center)

                If a > radius Then
                    radius = a
                End If
                If b > radius Then
                    radius = b
                End If
            Next

            Return radius
        End Function

        Private Shared Function Distance(p As Point3D, center As Point3D) As Double
            Dim dx As Double = p.X - center.X
            Dim dy As Double = p.Y - center.Y
            Dim dz As Double = p.Z - center.Z

            Return std.Sqrt(dx * dx + dy * dy + dz * dz)
        End Function

        ''' <summary>
        ''' the lowest Z coordinate of all of the loaded geometry
        ''' </summary>
        Private Function LowestZ(faces As Surface(), points As PointCloudPoint(), lines As LineSegment()) As Double
            Dim lowest As Double = Double.MaxValue

            For Each face As Surface In faces
                For Each p As Point3D In face.vertices
                    If p.Z < lowest Then
                        lowest = p.Z
                    End If
                Next
            Next

            For Each p As PointCloudPoint In points
                If p.Z < lowest Then
                    lowest = p.Z
                End If
            Next

            For Each line As LineSegment In lines
                If line.A.Z < lowest Then
                    lowest = line.A.Z
                End If
                If line.B.Z < lowest Then
                    lowest = line.B.Z
                End If
            Next

            If lowest = Double.MaxValue Then
                Return -m_radius
            End If

            Return lowest
        End Function
    End Class
End Namespace
