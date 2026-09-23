Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Drawing3D

Namespace Scene3D

    ''' <summary>
    ''' The rendering back end of the 3D scene pipeline.
    ''' </summary>
    ''' <remarks>
    ''' The contract is intentionally small: the back end receives the canvas,
    ''' the scene, the camera and the presentation options of one frame.
    '''
    ''' The default implementation renders the projected polygons of the scene
    ''' through the shared ``IGraphics`` api, so it can run on top of the gpu
    ''' accelerated direct2d canvas of this library as well as on any other
    ''' ``IGraphics`` implementation (for example the skia raster or the svg
    ''' canvas). A future implementation may render the very same scene through
    ''' a real 3d pipeline (vertex and index buffers, depth buffer, shaders) and
    ''' replace the default back end by implementing this interface.
    ''' </remarks>
    Public Interface ISceneRenderBackend

        ''' <summary>
        ''' a short display name of this back end
        ''' </summary>
        ReadOnly Property Name As String

        ''' <summary>
        ''' a readable description of this back end
        ''' </summary>
        ReadOnly Property Description As String

        ''' <summary>
        ''' render one frame of the given scene
        ''' </summary>
        ''' <param name="canvas">
        ''' the drawing canvas of the frame, the canvas size defines the viewport
        ''' </param>
        ''' <param name="scene">the geometry of the scene</param>
        ''' <param name="camera">
        ''' the camera of the frame, the back end is allowed to update
        ''' <see cref="Camera.Screen"/>
        ''' </param>
        ''' <param name="options">the presentation options of the frame</param>
        Sub Render(canvas As IGraphics, scene As Scene, camera As Camera, options As SceneRenderOptions)
    End Interface
End Namespace
