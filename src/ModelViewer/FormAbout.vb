Imports Microsoft.VisualBasic.Imaging.Drawing3D.Models

''' <summary>
''' The about dialog of the viewer, it shows an auto rotating colored cube.
''' </summary>
''' <remarks>
''' The cube is rendered by the very same scene pipeline as the main window, so
''' this dialog also works as a minimal example of how to host the reusable 3d
''' canvas outside of the model viewer.
''' </remarks>
Public Class FormAbout

    ''' <summary>the rotation step of the cube per frame, in degrees</summary>
    Private Const StepX As Single = 3
    Private Const StepY As Single = 2
    Private Const StepZ As Single = 1

    Private Sub FormAbout_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' the cube spins on its own and it is not shaded by the scene light
        Me.canvas.ShowGround = False
        Me.canvas.ShowDebugOverlay = False

        With Me.canvas.Lighting
            .Ambient = 0
            .Intensity = 0
            .ApplyTo(Me.canvas.Controller.Camera)
        End With

        Dim cube As New Cube(1)
        Call Me.canvas.LoadSurfaces(cube.faces)

        Me.timer.Start()
    End Sub

    Private Sub Timer_Tick(sender As Object, e As EventArgs) Handles timer.Tick
        Dim camera = Me.canvas.Controller.Camera

        camera.AngleX += StepX
        camera.AngleY += StepY
        camera.AngleZ += StepZ

        Call Me.canvas.RequestRender()
    End Sub

    Private Sub BtnClose_Click(sender As Object, e As EventArgs) Handles btnClose.Click
        Me.Close()
    End Sub

    Private Sub FormAbout_FormClosed(sender As Object, e As FormClosedEventArgs) Handles MyBase.FormClosed
        Me.timer.Stop()
    End Sub
End Class
