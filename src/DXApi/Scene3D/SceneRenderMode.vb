Namespace Scene3D

    ''' <summary>
    ''' The presentation mode of a 3D scene.
    ''' </summary>
    Public Enum SceneRenderMode

        ''' <summary>
        ''' filled faces with the per face lambert shading (default)
        ''' </summary>
        Surface

        ''' <summary>
        ''' triangle wire frame, outlines every face without any fill
        ''' </summary>
        Mesh

        ''' <summary>
        ''' every vertex is drawn as a heat map colored square point
        ''' </summary>
        PointCloud
    End Enum
End Namespace
