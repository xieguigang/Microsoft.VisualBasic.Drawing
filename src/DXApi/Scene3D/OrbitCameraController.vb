Imports System.Drawing
Imports Microsoft.VisualBasic.Imaging.Drawing3D
Imports std = System.Math

Namespace Scene3D

    ''' <summary>
    ''' The mouse button of a scene input event.
    ''' </summary>
    ''' <remarks>
    ''' This enum exists so that the controller stays free of any ui framework
    ''' dependency: a host translates its platform specific mouse events into
    ''' this neutral form.
    ''' </remarks>
    Public Enum SceneMouseButton
        Left
        Middle
        Right
    End Enum

    ''' <summary>
    ''' A ui framework independent orbit camera controller.
    ''' </summary>
    ''' <remarks>
    ''' The controller owns the camera of the scene and maintains the view state
    ''' from the input deltas:
    '''
    ''' * dragging with the left button orbits the camera (yaw and pitch)
    ''' * dragging with the right button translates the view in screen space
    ''' * the mouse wheel changes the view distance (zoom)
    '''
    ''' The wheel may be hard to use without a status feedback, so the view
    ''' changed event is raised on every state change and the host may use it to
    ''' refresh its status bar and to request a repaint.
    ''' </remarks>
    Public Class OrbitCameraController

        ''' <summary>
        ''' the rotation speed in degrees per pixel of the mouse movement
        ''' </summary>
        Public Property RotateSensitivity As Single = 0.4F

        ''' <summary>
        ''' the view distance factor of zooming towards the model
        ''' </summary>
        Public Property ZoomInFactor As Single = 0.9F

        ''' <summary>
        ''' the view distance factor of zooming away from the model
        ''' </summary>
        Public Property ZoomOutFactor As Single = 1.1F

        ''' <summary>
        ''' the smallest allowed view distance
        ''' </summary>
        Public Property MinViewDistance As Single = 1.0F

        ''' <summary>
        ''' the view distance that a fresh camera of this controller starts with,
        ''' <see cref="Scene.FitView"/> recomputes it for a loaded model
        ''' </summary>
        Public Property DefaultViewDistance As Single = 100.0F

        Private ReadOnly m_buttons As New HashSet(Of SceneMouseButton)()
        Private m_lastX As Integer = 0
        Private m_lastY As Integer = 0
        Private m_tracking As Boolean = False

        ''' <summary>
        ''' raised when the view state has been changed, the host should repaint
        ''' its canvas when this event is raised
        ''' </summary>
        Public Event ViewChanged As EventHandler

        ''' <summary>
        ''' create a controller that operates on the given camera, a new camera
        ''' is created when nothing is given
        ''' </summary>
        Public Sub New(Optional camera As Camera = Nothing)
            Me.Camera = If(camera, New Camera())

            With Me.Camera
                .AngleX = Scene.DefaultAngleX
                .AngleY = Scene.DefaultAngleY
                .AngleZ = Scene.DefaultAngleZ
                .ViewDistance = Me.DefaultViewDistance
                .FieldOfView = 256
                .Offset = New PointF(0, 0)
                .Screen = New Size(800, 600)
            End With
        End Sub

        ''' <summary>
        ''' the camera that this controller drives
        ''' </summary>
        Public ReadOnly Property Camera As Camera

        ''' <summary>
        ''' is a mouse button currently held down?
        ''' </summary>
        Public ReadOnly Property IsDragging As Boolean
            Get
                Return m_buttons.Count > 0
            End Get
        End Property

        ''' <summary>
        ''' the currently held mouse buttons
        ''' </summary>
        Public ReadOnly Property Buttons As SceneMouseButton()
            Get
                Return m_buttons.ToArray()
            End Get
        End Property

        ''' <summary>
        ''' begin a drag operation
        ''' </summary>
        Public Sub MouseDown(button As SceneMouseButton, x As Integer, y As Integer)
            m_buttons.Add(button)
            m_lastX = x
            m_lastY = y
            m_tracking = True
        End Sub

        ''' <summary>
        ''' continue a drag operation
        ''' </summary>
        Public Sub MouseMove(x As Integer, y As Integer)
            If Not m_tracking OrElse m_buttons.Count = 0 Then
                Return
            End If

            Dim dx As Integer = x - m_lastX
            Dim dy As Integer = y - m_lastY

            m_lastX = x
            m_lastY = y

            If dx = 0 AndAlso dy = 0 Then
                Return
            End If

            Dim changed As Boolean = False

            If m_buttons.Contains(SceneMouseButton.Left) Then
                Me.Camera.AngleY += dx * RotateSensitivity
                Me.Camera.AngleX += dy * RotateSensitivity
                changed = True
            ElseIf m_buttons.Contains(SceneMouseButton.Right) Then
                Me.Camera.Offset = New PointF(
                    Me.Camera.Offset.X + dx,
                    Me.Camera.Offset.Y + dy)
                changed = True
            End If

            If changed Then
                RaiseEvent ViewChanged(Me, EventArgs.Empty)
            End If
        End Sub

        ''' <summary>
        ''' end a drag operation
        ''' </summary>
        Public Sub MouseUp(button As SceneMouseButton)
            Call m_buttons.Remove(button)

            If m_buttons.Count = 0 Then
                m_tracking = False
            End If
        End Sub

        ''' <summary>
        ''' end all of the drag operations
        ''' </summary>
        Public Sub MouseUp()
            m_buttons.Clear()
            m_tracking = False
        End Sub

        ''' <summary>
        ''' zoom with the mouse wheel: a positive delta moves the camera towards
        ''' the model
        ''' </summary>
        Public Sub MouseWheel(delta As Integer)
            If delta = 0 Then
                Return
            End If

            Call ZoomBy(If(delta > 0, ZoomInFactor, ZoomOutFactor))
        End Sub

        ''' <summary>
        ''' multiply the view distance with the given factor
        ''' </summary>
        Public Sub ZoomBy(factor As Single)
            Dim distance As Single = Me.Camera.ViewDistance * factor
            Me.Camera.ViewDistance = std.Max(MinViewDistance, distance)

            RaiseEvent ViewChanged(Me, EventArgs.Empty)
        End Sub

        ''' <summary>
        ''' restore the default rotation angles and the screen space offset, the
        ''' view distance is not touched here
        ''' </summary>
        Public Sub Reset()
            Call Scene.ApplyDefaultAngles(Me.Camera)
            RaiseEvent ViewChanged(Me, EventArgs.Empty)
        End Sub

        ''' <summary>
        ''' apply the camera state onto another camera object
        ''' </summary>
        Public Sub ApplyTo(camera As Camera)
            If camera Is Nothing Then
                Return
            End If

            With Me.Camera
                camera.AngleX = .AngleX
                camera.AngleY = .AngleY
                camera.AngleZ = .AngleZ
                camera.Offset = .Offset
                camera.ViewDistance = .ViewDistance
                camera.FieldOfView = .FieldOfView
                camera.Screen = .Screen
                camera.LightDirection = .LightDirection
                camera.AmbientStrength = .AmbientStrength
                camera.LightColor = .LightColor
            End With
        End Sub
    End Class
End Namespace
