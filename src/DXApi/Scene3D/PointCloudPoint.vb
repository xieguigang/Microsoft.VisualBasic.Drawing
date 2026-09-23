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
        ''' <param name="sizeScale">
        ''' optional per point size factor (1 = the global point size); it is only
        ''' honoured while the embedded per point colors are in use
        ''' </param>
        Sub New(x As Double, y As Double, z As Double,
                Optional intensity As Double = 0,
                Optional color As String = Nothing,
                Optional sizeScale As Double = 0)

            Me.X = x
            Me.Y = y
            Me.Z = z
            Me.Intensity = intensity
            Me.Color = color
            Me.SizeScale = sizeScale
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

        ''' <summary>
        ''' optional per point size factor, ``&lt;= 0`` means "use the global point size".
        ''' </summary>
        ''' <remarks>
        ''' Per point sizes are for highlighting a subset of the cloud — for example
        ''' the neurons that fired in the current frame of an activity replay, which
        ''' should stand out from the resting cloud of a whole brain model.
        ''' 
        ''' The factor multiplies the global <c>options.PointSize</c> in the vertex
        ''' shader. It is carried in the otherwise unused scalar slot of the point
        ''' instance and therefore costs no extra vertex memory, but it is only
        ''' readable while the point cloud is drawn with the embedded per point colors
        ''' (that scalar slot doubles as the heat map value otherwise, which is what
        ''' the palette lookup needs).
        ''' </remarks>
        Public Property SizeScale As Double
    End Structure
End Namespace
