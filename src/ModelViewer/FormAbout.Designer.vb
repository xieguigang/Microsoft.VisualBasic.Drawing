<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormAbout
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.canvas = New Microsoft.VisualBasic.Drawing.DirectX.DxScene3DCanvas()
        Me.panelInfo = New System.Windows.Forms.Panel()
        Me.lblTitle = New System.Windows.Forms.Label()
        Me.lblDescription = New System.Windows.Forms.Label()
        Me.btnClose = New System.Windows.Forms.Button()
        Me.timer = New System.Windows.Forms.Timer(Me.components)
        Me.panelInfo.SuspendLayout()
        Me.SuspendLayout()
        ' 
        ' canvas
        ' 
        Me.canvas.AutoClear = False
        Me.canvas.BackColor = System.Drawing.Color.SkyBlue
        Me.canvas.Dock = System.Windows.Forms.DockStyle.Fill
        Me.canvas.Location = New System.Drawing.Point(0, 0)
        Me.canvas.Name = "canvas"
        Me.canvas.Size = New System.Drawing.Size(420, 320)
        Me.canvas.TabIndex = 0
        ' 
        ' panelInfo
        ' 
        Me.panelInfo.Controls.Add(Me.lblTitle)
        Me.panelInfo.Controls.Add(Me.lblDescription)
        Me.panelInfo.Controls.Add(Me.btnClose)
        Me.panelInfo.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.panelInfo.Location = New System.Drawing.Point(0, 320)
        Me.panelInfo.Name = "panelInfo"
        Me.panelInfo.Size = New System.Drawing.Size(420, 120)
        Me.panelInfo.TabIndex = 1
        ' 
        ' lblTitle
        ' 
        Me.lblTitle.AutoSize = True
        Me.lblTitle.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Bold)
        Me.lblTitle.Location = New System.Drawing.Point(12, 10)
        Me.lblTitle.Name = "lblTitle"
        Me.lblTitle.Size = New System.Drawing.Size(180, 21)
        Me.lblTitle.TabIndex = 0
        Me.lblTitle.Text = "三维模型查看器 (DirectX)"
        ' 
        ' lblDescription
        ' 
        Me.lblDescription.Location = New System.Drawing.Point(14, 42)
        Me.lblDescription.Name = "lblDescription"
        Me.lblDescription.Size = New System.Drawing.Size(392, 40)
        Me.lblDescription.TabIndex = 1
        Me.lblDescription.Text = "基于 DirectX 加速的交互式三维场景渲染管线" & Microsoft.VisualBasic.ChrW(13) & Microsoft.VisualBasic.ChrW(10) & "左键拖拽旋转 · 右键拖拽平移 · 滚轮缩放"
        ' 
        ' btnClose
        ' 
        Me.btnClose.Location = New System.Drawing.Point(12, 84)
        Me.btnClose.Name = "btnClose"
        Me.btnClose.Size = New System.Drawing.Size(100, 28)
        Me.btnClose.TabIndex = 2
        Me.btnClose.Text = "关闭"
        Me.btnClose.UseVisualStyleBackColor = True
        ' 
        ' timer
        ' 
        Me.timer.Interval = 15
        ' 
        ' FormAbout
        ' 
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(420, 440)
        Me.Controls.Add(Me.canvas)
        Me.Controls.Add(Me.panelInfo)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormAbout"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "关于"
        Me.panelInfo.ResumeLayout(False)
        Me.ResumeLayout(False)
    End Sub

    Friend WithEvents canvas As Microsoft.VisualBasic.Drawing.DirectX.DxScene3DCanvas
    Friend WithEvents panelInfo As System.Windows.Forms.Panel
    Friend WithEvents lblTitle As System.Windows.Forms.Label
    Friend WithEvents lblDescription As System.Windows.Forms.Label
    Friend WithEvents btnClose As System.Windows.Forms.Button
    Friend WithEvents timer As System.Windows.Forms.Timer
End Class
