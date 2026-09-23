Imports System.Drawing
Imports Microsoft.VisualBasic.Imaging.Drawing3D

Namespace Scene3D

    ''' <summary>
    ''' The lighting parameters of a 3D scene, expressed as the values of the user
    ''' interface so that they can be bound to sliders directly.
    ''' </summary>
    ''' <remarks>
    ''' The parameters are applied onto the camera because the light setup of the
    ''' painter algorithm lives on the camera object.
    ''' </remarks>
    Public Class SceneLighting

        ''' <summary>the default azimuth angle of the light source, in degrees</summary>
        Public Const DefaultAzimuth As Integer = -30
        ''' <summary>the default elevation angle of the light source, in degrees</summary>
        Public Const DefaultElevation As Integer = 45
        ''' <summary>the default ambient strength, in percent</summary>
        Public Const DefaultAmbient As Integer = 25
        ''' <summary>the default light intensity, in percent</summary>
        Public Const DefaultIntensity As Integer = 65

        ''' <summary>
        ''' the azimuth angle of the light source, in degrees
        ''' </summary>
        Public Property Azimuth As Integer = DefaultAzimuth

        ''' <summary>
        ''' the elevation angle of the light source, in degrees
        ''' </summary>
        Public Property Elevation As Integer = DefaultElevation

        ''' <summary>
        ''' the ambient light strength, in percent
        ''' </summary>
        Public Property Ambient As Integer = DefaultAmbient

        ''' <summary>
        ''' the light intensity, in percent, it scales the light color
        ''' </summary>
        Public Property Intensity As Integer = DefaultIntensity

        ''' <summary>
        ''' the color of the light source
        ''' </summary>
        Public Property LightColor As Color = Color.White

        ''' <summary>
        ''' restore all of the parameters to their default value
        ''' </summary>
        Public Sub Reset()
            Me.Azimuth = DefaultAzimuth
            Me.Elevation = DefaultElevation
            Me.Ambient = DefaultAmbient
            Me.Intensity = DefaultIntensity
            Me.LightColor = Color.White
        End Sub

        ''' <summary>
        ''' apply the lighting parameters onto the given camera
        ''' </summary>
        Public Sub ApplyTo(camera As Camera)
            If camera Is Nothing Then
                Return
            End If

            camera.AmbientStrength = Me.Ambient / 100.0
            camera.LightColor = Light.ScaleLightColor(Me.LightColor, Me.Intensity / 100.0)
            camera.LightDirection = Light.LightDirFromAngles(Me.Elevation, Me.Azimuth)
        End Sub

        ''' <summary>
        ''' create a copy of the current lighting parameters
        ''' </summary>
        Public Function Clone() As SceneLighting
            Return New SceneLighting With {
                .Azimuth = Me.Azimuth,
                .Elevation = Me.Elevation,
                .Ambient = Me.Ambient,
                .Intensity = Me.Intensity,
                .LightColor = Me.LightColor
            }
        End Function
    End Class
End Namespace
