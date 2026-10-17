Imports System
Imports Microsoft.VisualBasic.Drawing.DirectX

Partial Class FormRender3D

    ''' <summary>
    ''' Required designer variable.
    ''' </summary>
    Private components As System.ComponentModel.IContainer = Nothing

    ''' <summary>
    ''' Clean up any resources being used.
    ''' </summary>
    ''' <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing AndAlso components IsNot Nothing Then
            components.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub

#Region "Windows Form Designer generated code"

    ''' <summary>
    ''' Required method for Designer support - do not modify
    ''' the contents of this method with the code editor.
    ''' </summary>
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.m_canvas = New Microsoft.VisualBasic.Drawing.DirectX.DxScene3DCanvas()
        Me.timer1 = New System.Windows.Forms.Timer(Me.components)
        Me.SuspendLayout()
        '
        'm_canvas
        '
        Me.m_canvas.Dock = System.Windows.Forms.DockStyle.Fill
        Me.m_canvas.Location = New System.Drawing.Point(0, 0)
        Me.m_canvas.Name = "m_canvas"
        Me.m_canvas.Size = New System.Drawing.Size(1107, 665)
        Me.m_canvas.TabIndex = 0
        '
        'timer1
        '
        ' the timer is enabled by the form once the flock has been created
        Me.timer1.Interval = 1
        '
        'FormRender3D
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 12.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1107, 665)
        Me.Controls.Add(Me.m_canvas)
        Me.KeyPreview = True
        Me.Name = "FormRender3D"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Boids Algorithm 3D"
        Me.ResumeLayout(False)

    End Sub

#End Region

    Friend WithEvents m_canvas As Microsoft.VisualBasic.Drawing.DirectX.DxScene3DCanvas
    Friend WithEvents timer1 As System.Windows.Forms.Timer
End Class
