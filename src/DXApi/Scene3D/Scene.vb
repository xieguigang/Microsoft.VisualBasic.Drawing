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
        ''' does the scene hold any geometry at all?
        ''' </summary>
        Public ReadOnly Property HasData As Boolean
            Get
                Return m_surfaces.Length > 0 OrElse m_points.Length > 0
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
            m_radius = RadiusOf(allPoints, m_center)

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
            m_groundZ = LowestZ(m_surfaces, m_points)
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

            Dim source As New List(Of Point3D)(m_points.Length)
            For Each p As PointCloudPoint In m_points
                source.Add(New Point3D(p.X, p.Y, p.Z))
            Next

            m_center = CentroidOf(source)
            m_radius = RadiusOf(source, m_center)

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
            m_groundZ = LowestZ(m_surfaces, m_points)
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
        ''' the lowest Z coordinate of all of the loaded geometry
        ''' </summary>
        Private Function LowestZ(faces As Surface(), points As PointCloudPoint()) As Double
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

            If lowest = Double.MaxValue Then
                Return -m_radius
            End If

            Return lowest
        End Function
    End Class
End Namespace
