Imports System.Drawing

Namespace Scene3D

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
        Public Property ColorScheme As String = "viridis"

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
        ''' draw the ground grid below the model
        ''' </summary>
        Public Property ShowGround As Boolean = True

        ''' <summary>
        ''' the color of the ground grid lines
        ''' </summary>
        Public Property GroundColor As Color = Color.Gray

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
        ''' create a copy of the current options
        ''' </summary>
        Public Function Clone() As SceneRenderOptions
            Return New SceneRenderOptions With {
                .Mode = Mode,
                .ColorScheme = ColorScheme,
                .PointSize = PointSize,
                .PointAlpha = PointAlpha,
                .UseEmbeddedColor = UseEmbeddedColor,
                .ShowGround = ShowGround,
                .GroundColor = GroundColor,
                .BackgroundColor = BackgroundColor,
                .MultisampleCount = MultisampleCount,
                .CullBackFaces = CullBackFaces
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
