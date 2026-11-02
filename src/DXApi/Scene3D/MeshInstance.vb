Imports System.Drawing
Imports System.Numerics
Imports System.Runtime.InteropServices

Namespace Scene3D

    ''' <summary>
    ''' One vertex of a mesh that the host uploads into the 3d pipeline through
    ''' <see cref="Direct3D11SceneRenderer.UploadMesh"/>.
    ''' </summary>
    ''' <remarks>
    ''' The layout is the very same 28 byte one that the cached geometry of a
    ''' <see cref="Scene"/> uses (<c>Scene3DInputLayout.SurfaceStride</c>): the
    ''' position, the face normal and the packed RGBA color of the corner. One
    ''' triangle is three consecutive corners that repeat the face normal and the
    ''' face color, which is what reproduces the flat shading of the cpu painter
    ''' through the interpolator of the gpu.
    '''
    ''' The mesh is uploaded once and is drawn through one draw call per
    ''' submitted instance, so the vertex data of a rigid part never travels over
    ''' the bus again: only the world matrix of the part changes per frame. The
    ''' mesh data lives in the local space of the part, the instance supplies the
    ''' placement.
    ''' </remarks>
    <StructLayout(LayoutKind.Sequential)>
    Public Structure SceneMeshVertex
        Public X As Single
        Public Y As Single
        Public Z As Single
        Public NX As Single
        Public NY As Single
        Public NZ As Single
        Public R As Byte
        Public G As Byte
        Public B As Byte
        Public A As Byte

        ''' <summary>
        ''' the size of one vertex in bytes, the stride of the vertex buffer
        ''' </summary>
        Public Const Stride As Integer = 28

        ''' <summary>
        ''' create one face corner
        ''' </summary>
        ''' <param name="position">the position of the corner, in the local space of the part</param>
        ''' <param name="normal">the normal of the face that the corner belongs to</param>
        ''' <param name="color">the color of the face</param>
        Public Sub New(position As Vector3, normal As Vector3, color As Color)
            X = position.X
            Y = position.Y
            Z = position.Z
            NX = normal.X
            NY = normal.Y
            NZ = normal.Z
            R = color.R
            G = color.G
            B = color.B
            A = color.A
        End Sub
    End Structure

    ''' <summary>
    ''' One placement of an uploaded mesh: which mesh and where.
    ''' </summary>
    ''' <remarks>
    ''' A host that animates a rigid body chain (a robot, a skeleton, a vehicle)
    ''' keeps one mesh per rigid part in <see cref="Direct3D11SceneRenderer"/> and
    ''' submits one instance per part and per frame. The per instance state is a
    ''' single matrix plus the two clipping words of the materialization effect,
    ''' so an animation of a few dozen parts stays one draw call per part and no
    ''' vertex is ever transformed on the cpu.
    ''' </remarks>
    Public Structure MeshInstance
        ''' <summary>the key that the mesh was uploaded with</summary>
        Public Key As String
        ''' <summary>the world matrix of the part, row vector convention (translation in the fourth row)</summary>
        Public World As Matrix4x4
        ''' <summary>the opacity of the instance, 1 is opaque</summary>
        Public Alpha As Single
        ''' <summary>
        ''' the world space height below which the fragments are dropped, only
        ''' read when <see cref="Clip"/> is on
        ''' </summary>
        Public ClipY As Single
        ''' <summary>drop the fragments that are below <see cref="ClipY"/></summary>
        Public Clip As Boolean
    End Structure
End Namespace
