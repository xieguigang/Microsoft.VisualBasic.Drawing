Namespace Scene3D

    ''' <summary>
    ''' A format independent point of a 3D point cloud.
    ''' </summary>
    ''' <remarks>
    ''' The point cloud loaders of the different file formats (for example the
    ''' PLY reader of the Landscape library) are mapped into this structure so
    ''' that the scene pipeline does not depend on any specific file format.
    ''' </remarks>
    Public Structure PointCloudPoint

        ''' <summary>
        ''' create a new point of a 3D point cloud
        ''' </summary>
        ''' <param name="x">X coordinate in the model space</param>
        ''' <param name="y">Y coordinate in the model space</param>
        ''' <param name="z">Z coordinate in the model space</param>
        ''' <param name="intensity">
        ''' the scalar field value that drives the heat map coloring, zero means
        ''' that the Z coordinate is used as the scalar value instead
        ''' </param>
        ''' <param name="color">optional per point color as an html color string</param>
        Sub New(x As Double, y As Double, z As Double,
                Optional intensity As Double = 0,
                Optional color As String = Nothing)

            Me.X = x
            Me.Y = y
            Me.Z = z
            Me.Intensity = intensity
            Me.Color = color
        End Sub

        ''' <summary>X coordinate in the model space.</summary>
        Public Property X As Double

        ''' <summary>Y coordinate in the model space.</summary>
        Public Property Y As Double

        ''' <summary>Z coordinate in the model space.</summary>
        Public Property Z As Double

        ''' <summary>
        ''' the scalar field value that drives the heat map coloring
        ''' </summary>
        Public Property Intensity As Double

        ''' <summary>
        ''' optional per point color as an html color string, for example ``#ff0000``
        ''' </summary>
        Public Property Color As String
    End Structure
End Namespace
