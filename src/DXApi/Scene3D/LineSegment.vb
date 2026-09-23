Imports System.Drawing
Imports Microsoft.VisualBasic.Imaging.Drawing3D

Namespace Scene3D

    ''' <summary>
    ''' One connection line of a <see cref="Scene"/>: the two end points of the
    ''' line and the color that is used to draw it.
    ''' </summary>
    ''' <remarks>
    ''' The record is format agnostic exactly like <see cref="PointCloudPoint"/>:
    ''' the scene stores whatever the caller provides and the back ends decide how
    ''' to draw it.
    ''' 
    ''' * the gpu back end expands one record into two <c>LineVertex</c> items of a
    '''   line list (see <c>GpuSceneGeometry.BuildLines</c>);
    ''' * the direct2d back end projects the two end points and draws one
    '''   <c>IGraphics</c> line.
    ''' 
    ''' A color with an alpha of zero means "the caller did not assign a color",
    ''' the back ends replace it with their default line color.
    ''' </remarks>
    Public Structure LineSegment

        ''' <summary>
        ''' the color that is used for the lines that did not assign a color of
        ''' their own (an alpha of zero)
        ''' </summary>
        ''' <remarks>
        ''' A half transparent gray: a connection graph of a whole brain has
        ''' hundreds of thousands of lines, a fully opaque color turns the dense
        ''' parts of the graph into a solid block.
        ''' </remarks>
        Public Shared ReadOnly DefaultColor As Color = Color.FromArgb(&H80, 120, 120, 120)

        ''' <summary>
        ''' create one connection line from its two end points
        ''' </summary>
        ''' <param name="a">the start point of the line</param>
        ''' <param name="b">the end point of the line</param>
        ''' <param name="color">
        ''' the color of the line, a color with an alpha of zero means that the
        ''' caller wants the default line color of the back end
        ''' </param>
        Sub New(a As Point3D, b As Point3D, Optional color As Color = Nothing)
            Me.A = a
            Me.B = b
            Me.Color = color
        End Sub

        ''' <summary>
        ''' the start point of the line
        ''' </summary>
        Public Property A As Point3D

        ''' <summary>
        ''' the end point of the line
        ''' </summary>
        Public Property B As Point3D

        ''' <summary>
        ''' the color of the line
        ''' </summary>
        ''' <remarks>
        ''' an alpha of zero means that the line has no color of its own
        ''' </remarks>
        Public Property Color As Color

        ''' <summary>
        ''' the mid point of the line
        ''' </summary>
        Friend ReadOnly Property Center As Point3D
            Get
                Return New Point3D((A.X + B.X) / 2, (A.Y + B.Y) / 2, (A.Z + B.Z) / 2)
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return $"({A.X:N0}, {A.Y:N0}, {A.Z:N0}) -> ({B.X:N0}, {B.Y:N0}, {B.Z:N0})"
        End Function

    End Structure

End Namespace
