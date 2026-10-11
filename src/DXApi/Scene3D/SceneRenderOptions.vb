Imports System.Drawing
Imports System.Numerics
Imports Microsoft.VisualBasic.Imaging.Drawing2D.Colors

Namespace Scene3D

    ''' <summary>
    ''' The shape that one point of a point cloud is expanded into by the gpu
    ''' back end.
    ''' </summary>
    Public Enum ScenePointShape

        ''' <summary>
        ''' a screen aligned square of <see cref="SceneRenderOptions.PointSize"/> pixels
        ''' (the classic billboard, the default for backwards compatibility)
        ''' </summary>
        Square

        ''' <summary>
        ''' a world space cube whose edge lengths are
        ''' <see cref="SceneRenderOptions.CubeEdge"/> world units: voxels keep
        ''' their volume from every camera angle and occlude each other through
        ''' the depth buffer. The cube mode reads the per point embedded colors.
        ''' </summary>
        Cube
    End Enum

    ''' <summary>
    ''' The presentation options of a 3D scene.
    ''' </summary>
    ''' <remarks>
    ''' The options are separated from the scene data so that the same scene can
    ''' be rendered by different back ends with different presentation settings.
    ''' </remarks>
    Public Class SceneRenderOptions

        ''' <summary>
        ''' the presentation mode of the scene
        ''' </summary>
        Public Property Mode As SceneRenderMode = SceneRenderMode.Surface

        ''' <summary>
        ''' the heat map color scheme name that is passed to the color designer,
        ''' for example ``viridis`` or ``magma``
        ''' </summary>
        Public Property ColorScheme As ScalerPalette = ScalerPalette.viridis

        ''' <summary>
        ''' the edge length in pixels of a point cloud point
        ''' </summary>
        Public Property PointSize As Integer = 2

        ''' <summary>
        ''' the alpha channel of the heat map colors, in the range [0, 255]
        ''' </summary>
        Public Property PointAlpha As Integer = 255

        ''' <summary>
        ''' use the per point color of the point cloud instead of the heat map
        ''' coloring when the source file provides one
        ''' </summary>
        Public Property UseEmbeddedColor As Boolean = False

        ''' <summary>
        ''' the global opacity of the voxel cubes of the cube point shape, in
        ''' the range [0, 1]; one means opaque, a lower value blends the cubes
        ''' semi transparent over the scene behind them
        ''' </summary>
        Public Property PointOpacity As Single = 1.0F

        ''' <summary>
        ''' draw the ground grid below the model
        ''' </summary>
        Public Property ShowGround As Boolean = True

        ''' <summary>
        ''' the color of the ground grid lines
        ''' </summary>
        Public Property GroundColor As Color = Color.Gray

        ''' <summary>
        ''' draw the connection lines of the scene
        ''' </summary>
        ''' <remarks>
        ''' The lines are an overlay: the point cloud of the neurons and the network
        ''' graph of the connections are two views of the same data, so both can be
        ''' shown at the same time.
        ''' Toggling this option does not rebuild the gpu geometry (see
        ''' <see cref="GeometrySignature"/>): a connectome of a whole brain uploads
        ''' hundreds of megabytes of line vertices, a visibility switch must not pay
        ''' that price again.
        ''' </remarks>
        Public Property ShowConnections As Boolean = True

        ''' <summary>
        ''' the background color of the canvas
        ''' </summary>
        Public Property BackgroundColor As Color = Color.White

        ''' <summary>
        ''' the number of the samples of the multi sample anti aliasing of the
        ''' gpu rendering back end, one disables the anti aliasing.
        ''' </summary>
        ''' <remarks>
        ''' The default is one: the strict mode renders exactly the same picture
        ''' as the polygon painter back end does. A value of four or eight
        ''' smooths the model edges, and the back end silently falls back to one
        ''' when the gpu device does not support the requested sample count.
        ''' </remarks>
        Public Property MultisampleCount As Integer = 1

        ''' <summary>
        ''' discard the faces that point away from the viewer
        ''' </summary>
        ''' <remarks>
        ''' The default is false because the winding of the faces of a scanned
        ''' model is not always consistent: with the culling enabled such a model
        ''' shows holes.
        ''' </remarks>
        Public Property CullBackFaces As Boolean = False

        ''' <summary>
        ''' the shape that one point of the point cloud is expanded into by the
        ''' gpu back end, squares by default (see <see cref="ScenePointShape"/>)
        ''' </summary>
        Public Property PointShape As ScenePointShape = ScenePointShape.Square

        ''' <summary>
        ''' the world space edge lengths of one voxel cube of the cube point
        ''' shape, one world unit per axis by default
        ''' </summary>
        ''' <remarks>
        ''' The value is a per frame constant only: it does not change the vertex
        ''' data, so it is not part of <see cref="GeometrySignature"/> and the
        ''' cached geometry is not rebuilt when it changes.
        ''' </remarks>
        Public Property CubeEdge As Vector3 = Vector3.One

        ''' <summary>
        ''' draw a screen space grid onto the background of the canvas, the grid
        ''' is not part of the 3d scene and never moves with the camera
        ''' </summary>
        Public Property ShowBackgroundGrid As Boolean = False

        ''' <summary>
        ''' the color of the background grid lines
        ''' </summary>
        Public Property BackgroundGridColor As Color = Color.White

        ''' <summary>
        ''' the edge length in pixels of one cell of the background grid
        ''' </summary>
        Public Property BackgroundGridCellSize As Integer = 32

        ''' <summary>
        ''' create a copy of the current options
        ''' </summary>
        Public Function Clone() As SceneRenderOptions
            Return New SceneRenderOptions With {
                .Mode = Mode,
                .ColorScheme = ColorScheme,
                .PointSize = PointSize,
                .PointAlpha = PointAlpha,
                .UseEmbeddedColor = UseEmbeddedColor,
                .PointOpacity = PointOpacity,
                .ShowGround = ShowGround,
                .GroundColor = GroundColor,
                .ShowConnections = ShowConnections,
                .BackgroundColor = BackgroundColor,
                .MultisampleCount = MultisampleCount,
                .CullBackFaces = CullBackFaces,
                .PointShape = PointShape,
                .CubeEdge = CubeEdge,
                .ShowBackgroundGrid = ShowBackgroundGrid,
                .BackgroundGridColor = BackgroundGridColor,
                .BackgroundGridCellSize = BackgroundGridCellSize
            }
        End Function

        ''' <summary>
        ''' the part of the options that the gpu geometry cache has to observe:
        ''' when this text changes the cached geometry has to be rebuilt.
        ''' </summary>
        ''' <remarks>
        ''' the camera and the lighting are not part of this signature because
        ''' they do not change the vertex data, they are uniform values only.
        ''' </remarks>
        Friend Function GeometrySignature() As String
            Return $"{Mode}|{ColorScheme}|{PointSize}|{PointAlpha}|{UseEmbeddedColor}|{ShowGround}|{GroundColor.ToArgb()}"
        End Function
    End Class
End Namespace
