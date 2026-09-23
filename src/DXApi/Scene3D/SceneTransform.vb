Imports System.Drawing
Imports System.Numerics
Imports Microsoft.VisualBasic.Imaging.Drawing3D
Imports std = System.Math

Namespace Scene3D

    ''' <summary>
    ''' The 4x4 transform matrices of one frame of the 3d scene.
    ''' </summary>
    ''' <remarks>
    ''' The matrices are an exact replica of the cpu pipeline of the
    ''' ``Microsoft.VisualBasic.Imaging.Drawing3D`` library, so the gpu pipeline
    ''' generates the very same image as the polygon painter of the direct2d
    ''' rendering back end:
    '''
    ''' 1. the rotation is the same euler x -&gt; y -&gt; z rotation that
    '''    ``Camera.RotationMatrix`` applies, the angles are given in degrees
    ''' 2. the projection is the same pseudo perspective of ``Point3D.Project``:
    '''    ``factor = fov / (viewDistance + z)``, the screen centre and the
    '''    screen space camera offset are added after that projection
    '''
    ''' The .net matrix convention is the row vector one (``result = v * M``),
    ''' so the world view projection matrix is ``rotation * projection``.
    '''
    ''' The matrices of this class are uploaded into the constant buffer as they
    ''' are, and the shader applies them through ``mul(m, v)`` with the default
    ''' column major packing, which matches the row vector transform exactly.
    ''' Note that the opposite - transposing the matrix before the upload - is
    ''' the classic trap here: the transpose would move the w row of the pseudo
    ''' perspective into the depth row and the image would only look "almost"
    ''' right while every depth value is wrong.
    ''' </remarks>
    Public NotInheritable Class SceneTransform

        ''' <summary>
        ''' the rotation of the camera, ready for the constant buffer
        ''' </summary>
        ''' <remarks>
        ''' the shader rotates the face normal of a model with this matrix, this
        ''' is exactly what the cpu pipeline does: the lighting is evaluated on
        ''' the rotated vertices, the face normal is oriented towards the viewer
        ''' in the rotated (camera) space.
        ''' </remarks>
        Public ReadOnly Property Rotation As Matrix4x4

        ''' <summary>
        ''' the projection of the camera, in the row vector convention.
        ''' </summary>
        Public ReadOnly Property Projection As Matrix4x4

        ''' <summary>
        ''' ``rotation * projection``, ready for the constant buffer
        ''' </summary>
        Public ReadOnly Property WorldViewProjection As Matrix4x4

        ''' <summary>
        ''' the distance of the near clip plane from the camera
        ''' </summary>
        Public ReadOnly Property NearPlane As Double

        ''' <summary>
        ''' the size of the viewport in pixels
        ''' </summary>
        Public ReadOnly Property ScreenSize As Size

        ''' <summary>
        ''' the scale that converts one pixel into the clip space of the x axis,
        ''' that is ``2 / width``
        ''' </summary>
        Public ReadOnly Property PixelScaleX As Single

        ''' <summary>
        ''' the scale that converts one pixel into the clip space of the y axis,
        ''' that is ``2 / height``
        ''' </summary>
        Public ReadOnly Property PixelScaleY As Single

        Private Sub New(rotation As Matrix4x4, projection As Matrix4x4, nearPlane As Double, screenSize As Size)
            _Rotation = rotation
            _Projection = projection
            _WorldViewProjection = Matrix4x4.Multiply(rotation, projection)
            _NearPlane = nearPlane
            _ScreenSize = screenSize
            _PixelScaleX = CSng(2.0 / std.Max(screenSize.Width, 1))
            _PixelScaleY = CSng(2.0 / std.Max(screenSize.Height, 1))
        End Sub

        ''' <summary>
        ''' build the transform matrices of one frame
        ''' </summary>
        ''' <param name="camera">the camera of the frame</param>
        ''' <param name="scene">
        ''' the scene that is rendered, it is only used for choosing the near
        ''' clip plane
        ''' </param>
        ''' <param name="screenSize">the size of the viewport in pixels</param>
        Public Shared Function Create(camera As Camera, scene As Scene, screenSize As Size) As SceneTransform
            If screenSize.Width <= 0 OrElse screenSize.Height <= 0 Then
                Throw New ArgumentException($"invalid viewport size: [{screenSize.Width}, {screenSize.Height}]")
            End If

            Dim width As Double = screenSize.Width
            Dim height As Double = screenSize.Height

            ' the cpu pipeline passes the field of view to an integer parameter
            ' of Point3D.Project, so the very same rounding has to be applied
            ' here or the projection scale would be slightly different
            Dim fov As Double = CInt(CDbl(camera.FieldOfView))
            Dim vd As Double = camera.ViewDistance
            Dim offsetX As Double = camera.Offset.X
            Dim offsetY As Double = camera.Offset.Y
            Dim radius As Double = If(scene IsNot Nothing AndAlso scene.Radius > 0, scene.Radius, 1.0)

            ' the near plane only controls the depth resolution, it must stay
            ' outside of the bounding sphere of the scene so that no vertex is
            ' clipped: half of the distance between the camera and the bounding
            ' sphere leaves the whole model inside of the valid depth range
            Dim nearPlane As Double = std.Max((vd - radius) * 0.5, radius * 0.001)

            Dim rotation As Matrix4x4 = RotationMatrix(camera)

            ' x_ndc = 2 * fov / width  * x / (z + vd) + 2 * offset.x / width
            ' y_ndc = -2 * fov / height * y / (z + vd) - 2 * offset.y / height
            ' the depth grows with z, so the default LESS depth test keeps the
            ' nearest face: depth = z_clip / w
            Dim a As Double = 2.0 * fov / width
            Dim b As Double = 2.0 * offsetX / width
            Dim c As Double = 2.0 * fov / height
            Dim d As Double = 2.0 * offsetY / height

            Dim projection As New Matrix4x4()
            ' the x row
            projection.M11 = CSng(a)
            projection.M21 = 0
            projection.M31 = CSng(b)
            projection.M41 = CSng(b * vd)
            ' the y row, the screen y axis points downwards
            projection.M12 = 0
            projection.M22 = CSng(-c)
            projection.M32 = CSng(-d)
            projection.M42 = CSng(-d * vd)
            ' the depth row
            projection.M13 = 0
            projection.M23 = 0
            projection.M33 = 1
            projection.M43 = CSng(vd - nearPlane)
            ' the w row
            projection.M14 = 0
            projection.M24 = 0
            projection.M34 = 1
            projection.M44 = CSng(vd)

            Return New SceneTransform(rotation, projection, nearPlane, screenSize)
        End Function

        ''' <summary>
        ''' the euler rotation matrix of the camera angles.
        ''' </summary>
        ''' <remarks>
        ''' This is a line by line replica of the private ``Camera.RotationMatrix``
        ''' function of the shared imaging library, in the row vector convention
        ''' of ``System.Numerics`` (``result.j = sum over i of v.i * M.i.j``):
        ''' the function is private, so it can not be called from here, but the
        ''' element formulas and their smoke tests - ``(0,0,1)`` rotated by x 90
        ''' becomes ``(0,-1,0)``, ``(1,0,0)`` rotated by y 90 becomes ``(0,0,-1)`` -
        ''' are verified against the cpu pipeline.
        ''' </remarks>
        Public Shared Function RotationMatrix(camera As Camera) As Matrix4x4
            Dim radX As Double = camera.AngleX * std.PI / 180.0
            Dim radY As Double = camera.AngleY * std.PI / 180.0
            Dim radZ As Double = camera.AngleZ * std.PI / 180.0
            Dim cX As Double = std.Cos(radX)
            Dim sX As Double = std.Sin(radX)
            Dim cY As Double = std.Cos(radY)
            Dim sY As Double = std.Sin(radY)
            Dim cZ As Double = std.Cos(radZ)
            Dim sZ As Double = std.Sin(radZ)

            Dim m As New Matrix4x4()
            ' result.x = p.x * M11 + p.y * M21 + p.z * M31
            m.M11 = CSng(cY * cZ)
            m.M21 = CSng(sX * sY * cZ - cX * sZ)
            m.M31 = CSng(cX * sY * cZ + sX * sZ)
            ' result.y = p.x * M12 + p.y * M22 + p.z * M32
            m.M12 = CSng(cY * sZ)
            m.M22 = CSng(sX * sY * sZ + cX * cZ)
            m.M32 = CSng(cX * sY * sZ - sX * cZ)
            ' result.z = p.x * M13 + p.y * M23 + p.z * M33
            m.M13 = CSng(-sY)
            m.M23 = CSng(sX * cY)
            m.M33 = CSng(cX * cY)
            m.M44 = 1

            Return m
        End Function
    End Class
End Namespace
