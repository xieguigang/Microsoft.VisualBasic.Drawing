<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormMain
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
        Me.lightPanel = New System.Windows.Forms.Panel()
        Me.lblLightTitle = New System.Windows.Forms.Label()
        Me.lblAzimuthCaption = New System.Windows.Forms.Label()
        Me.trkAzimuth = New System.Windows.Forms.TrackBar()
        Me.lblAzimuthValue = New System.Windows.Forms.Label()
        Me.lblElevationCaption = New System.Windows.Forms.Label()
        Me.trkElevation = New System.Windows.Forms.TrackBar()
        Me.lblElevationValue = New System.Windows.Forms.Label()
        Me.lblAmbientCaption = New System.Windows.Forms.Label()
        Me.trkAmbient = New System.Windows.Forms.TrackBar()
        Me.lblAmbientValue = New System.Windows.Forms.Label()
        Me.lblIntensityCaption = New System.Windows.Forms.Label()
        Me.trkIntensity = New System.Windows.Forms.TrackBar()
        Me.lblIntensityValue = New System.Windows.Forms.Label()
        Me.btnLightColor = New System.Windows.Forms.Button()
        Me.lblLightColor = New System.Windows.Forms.Label()
        Me.btnResetLight = New System.Windows.Forms.Button()
        Me.statusStrip = New System.Windows.Forms.StatusStrip()
        Me.lblStatus = New System.Windows.Forms.ToolStripStatusLabel()
        Me.lblDevice = New System.Windows.Forms.ToolStripStatusLabel()
        Me.toolStrip = New System.Windows.Forms.ToolStrip()
        Me.cboMode = New System.Windows.Forms.ToolStripComboBox()
        Me.cboScheme = New System.Windows.Forms.ToolStripComboBox()
        Me.chkEmbedded = New System.Windows.Forms.ToolStripButton()
        Me.numPointSize = New System.Windows.Forms.ToolStripComboBox()
        Me.toolSep1 = New System.Windows.Forms.ToolStripSeparator()
        Me.btnReset = New System.Windows.Forms.ToolStripButton()
        Me.chkShowGround = New System.Windows.Forms.ToolStripButton()
        Me.btnBgColor = New System.Windows.Forms.ToolStripButton()
        Me.chkShowDebug = New System.Windows.Forms.ToolStripButton()
        Me.toolSep2 = New System.Windows.Forms.ToolStripSeparator()
        Me.btnSnapshot = New System.Windows.Forms.ToolStripButton()
        Me.menuStrip = New System.Windows.Forms.MenuStrip()
        Me.fileMenu = New System.Windows.Forms.ToolStripMenuItem()
        Me.openItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.sepFile = New System.Windows.Forms.ToolStripSeparator()
        Me.exitItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.viewMenu = New System.Windows.Forms.ToolStripMenuItem()
        Me.resetViewItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.fitViewItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.groundItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.debugItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.sepView = New System.Windows.Forms.ToolStripSeparator()
        Me.bgColorItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.snapshotItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.helpMenu = New System.Windows.Forms.ToolStripMenuItem()
        Me.aboutItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.lightPanel.SuspendLayout()
        CType(Me.trkAzimuth, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.trkElevation, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.trkAmbient, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.trkIntensity, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.statusStrip.SuspendLayout()
        Me.toolStrip.SuspendLayout()
        Me.menuStrip.SuspendLayout()
        Me.SuspendLayout()
        ' 
        ' canvas
        ' 
        Me.canvas.AutoClear = False
        Me.canvas.BackColor = System.Drawing.Color.White
        Me.canvas.Dock = System.Windows.Forms.DockStyle.Fill
        Me.canvas.Location = New System.Drawing.Point(0, 49)
        Me.canvas.Name = "canvas"
        Me.canvas.Size = New System.Drawing.Size(671, 477)
        Me.canvas.TabIndex = 0
        ' 
        ' lightPanel
        ' 
        Me.lightPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.lightPanel.Controls.Add(Me.lblLightTitle)
        Me.lightPanel.Controls.Add(Me.lblAzimuthCaption)
        Me.lightPanel.Controls.Add(Me.trkAzimuth)
        Me.lightPanel.Controls.Add(Me.lblAzimuthValue)
        Me.lightPanel.Controls.Add(Me.lblElevationCaption)
        Me.lightPanel.Controls.Add(Me.trkElevation)
        Me.lightPanel.Controls.Add(Me.lblElevationValue)
        Me.lightPanel.Controls.Add(Me.lblAmbientCaption)
        Me.lightPanel.Controls.Add(Me.trkAmbient)
        Me.lightPanel.Controls.Add(Me.lblAmbientValue)
        Me.lightPanel.Controls.Add(Me.lblIntensityCaption)
        Me.lightPanel.Controls.Add(Me.trkIntensity)
        Me.lightPanel.Controls.Add(Me.lblIntensityValue)
        Me.lightPanel.Controls.Add(Me.btnLightColor)
        Me.lightPanel.Controls.Add(Me.lblLightColor)
        Me.lightPanel.Controls.Add(Me.btnResetLight)
        Me.lightPanel.Dock = System.Windows.Forms.DockStyle.Right
        Me.lightPanel.Location = New System.Drawing.Point(671, 49)
        Me.lightPanel.Name = "lightPanel"
        Me.lightPanel.Size = New System.Drawing.Size(250, 477)
        Me.lightPanel.TabIndex = 1
        ' 
        ' lblLightTitle
        ' 
        Me.lblLightTitle.AutoSize = True
        Me.lblLightTitle.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
        Me.lblLightTitle.Location = New System.Drawing.Point(12, 12)
        Me.lblLightTitle.Name = "lblLightTitle"
        Me.lblLightTitle.Size = New System.Drawing.Size(70, 15)
        Me.lblLightTitle.TabIndex = 0
        Me.lblLightTitle.Text = "光照参数"
        ' 
        ' lblAzimuthCaption
        ' 
        Me.lblAzimuthCaption.AutoSize = True
        Me.lblAzimuthCaption.Location = New System.Drawing.Point(12, 46)
        Me.lblAzimuthCaption.Name = "lblAzimuthCaption"
        Me.lblAzimuthCaption.Size = New System.Drawing.Size(70, 15)
        Me.lblAzimuthCaption.TabIndex = 1
        Me.lblAzimuthCaption.Text = "光源方位"
        ' 
        ' trkAzimuth
        ' 
        Me.trkAzimuth.LargeChange = 10
        Me.trkAzimuth.Location = New System.Drawing.Point(96, 40)
        Me.trkAzimuth.Maximum = 360
        Me.trkAzimuth.Minimum = -360
        Me.trkAzimuth.Name = "trkAzimuth"
        Me.trkAzimuth.Size = New System.Drawing.Size(110, 45)
        Me.trkAzimuth.SmallChange = 5
        Me.trkAzimuth.TabIndex = 2
        Me.trkAzimuth.TickFrequency = 30
        Me.trkAzimuth.Value = -30
        ' 
        ' lblAzimuthValue
        ' 
        Me.lblAzimuthValue.AutoSize = True
        Me.lblAzimuthValue.Location = New System.Drawing.Point(212, 46)
        Me.lblAzimuthValue.Name = "lblAzimuthValue"
        Me.lblAzimuthValue.Size = New System.Drawing.Size(22, 15)
        Me.lblAzimuthValue.TabIndex = 3
        Me.lblAzimuthValue.Text = "-30"
        ' 
        ' lblElevationCaption
        ' 
        Me.lblElevationCaption.AutoSize = True
        Me.lblElevationCaption.Location = New System.Drawing.Point(12, 88)
        Me.lblElevationCaption.Name = "lblElevationCaption"
        Me.lblElevationCaption.Size = New System.Drawing.Size(70, 15)
        Me.lblElevationCaption.TabIndex = 4
        Me.lblElevationCaption.Text = "光源仰角"
        ' 
        ' trkElevation
        ' 
        Me.trkElevation.LargeChange = 10
        Me.trkElevation.Location = New System.Drawing.Point(96, 82)
        Me.trkElevation.Maximum = 90
        Me.trkElevation.Minimum = -90
        Me.trkElevation.Name = "trkElevation"
        Me.trkElevation.Size = New System.Drawing.Size(110, 45)
        Me.trkElevation.SmallChange = 5
        Me.trkElevation.TabIndex = 5
        Me.trkElevation.TickFrequency = 15
        Me.trkElevation.Value = 45
        ' 
        ' lblElevationValue
        ' 
        Me.lblElevationValue.AutoSize = True
        Me.lblElevationValue.Location = New System.Drawing.Point(212, 88)
        Me.lblElevationValue.Name = "lblElevationValue"
        Me.lblElevationValue.Size = New System.Drawing.Size(22, 15)
        Me.lblElevationValue.TabIndex = 6
        Me.lblElevationValue.Text = "45"
        ' 
        ' lblAmbientCaption
        ' 
        Me.lblAmbientCaption.AutoSize = True
        Me.lblAmbientCaption.Location = New System.Drawing.Point(12, 130)
        Me.lblAmbientCaption.Name = "lblAmbientCaption"
        Me.lblAmbientCaption.Size = New System.Drawing.Size(70, 15)
        Me.lblAmbientCaption.TabIndex = 7
        Me.lblAmbientCaption.Text = "环境光"
        ' 
        ' trkAmbient
        ' 
        Me.trkAmbient.LargeChange = 10
        Me.trkAmbient.Location = New System.Drawing.Point(96, 124)
        Me.trkAmbient.Maximum = 100
        Me.trkAmbient.Minimum = 0
        Me.trkAmbient.Name = "trkAmbient"
        Me.trkAmbient.Size = New System.Drawing.Size(110, 45)
        Me.trkAmbient.SmallChange = 5
        Me.trkAmbient.TabIndex = 8
        Me.trkAmbient.TickFrequency = 10
        Me.trkAmbient.Value = 25
        ' 
        ' lblAmbientValue
        ' 
        Me.lblAmbientValue.AutoSize = True
        Me.lblAmbientValue.Location = New System.Drawing.Point(212, 130)
        Me.lblAmbientValue.Name = "lblAmbientValue"
        Me.lblAmbientValue.Size = New System.Drawing.Size(22, 15)
        Me.lblAmbientValue.TabIndex = 9
        Me.lblAmbientValue.Text = "25"
        ' 
        ' lblIntensityCaption
        ' 
        Me.lblIntensityCaption.AutoSize = True
        Me.lblIntensityCaption.Location = New System.Drawing.Point(12, 172)
        Me.lblIntensityCaption.Name = "lblIntensityCaption"
        Me.lblIntensityCaption.Size = New System.Drawing.Size(70, 15)
        Me.lblIntensityCaption.TabIndex = 10
        Me.lblIntensityCaption.Text = "光照亮度"
        ' 
        ' trkIntensity
        ' 
        Me.trkIntensity.LargeChange = 10
        Me.trkIntensity.Location = New System.Drawing.Point(96, 166)
        Me.trkIntensity.Maximum = 100
        Me.trkIntensity.Minimum = 0
        Me.trkIntensity.Name = "trkIntensity"
        Me.trkIntensity.Size = New System.Drawing.Size(110, 45)
        Me.trkIntensity.SmallChange = 5
        Me.trkIntensity.TabIndex = 11
        Me.trkIntensity.TickFrequency = 10
        Me.trkIntensity.Value = 65
        ' 
        ' lblIntensityValue
        ' 
        Me.lblIntensityValue.AutoSize = True
        Me.lblIntensityValue.Location = New System.Drawing.Point(212, 172)
        Me.lblIntensityValue.Name = "lblIntensityValue"
        Me.lblIntensityValue.Size = New System.Drawing.Size(22, 15)
        Me.lblIntensityValue.TabIndex = 12
        Me.lblIntensityValue.Text = "65"
        ' 
        ' btnLightColor
        ' 
        Me.btnLightColor.Location = New System.Drawing.Point(12, 214)
        Me.btnLightColor.Name = "btnLightColor"
        Me.btnLightColor.Size = New System.Drawing.Size(110, 28)
        Me.btnLightColor.TabIndex = 13
        Me.btnLightColor.Text = "灯光颜色"
        Me.btnLightColor.UseVisualStyleBackColor = True
        ' 
        ' lblLightColor
        ' 
        Me.lblLightColor.BackColor = System.Drawing.Color.White
        Me.lblLightColor.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.lblLightColor.Location = New System.Drawing.Point(130, 214)
        Me.lblLightColor.Name = "lblLightColor"
        Me.lblLightColor.Size = New System.Drawing.Size(60, 28)
        Me.lblLightColor.TabIndex = 14
        ' 
        ' btnResetLight
        ' 
        Me.btnResetLight.Location = New System.Drawing.Point(12, 252)
        Me.btnResetLight.Name = "btnResetLight"
        Me.btnResetLight.Size = New System.Drawing.Size(178, 28)
        Me.btnResetLight.TabIndex = 15
        Me.btnResetLight.Text = "重置光照"
        Me.btnResetLight.UseVisualStyleBackColor = True
        ' 
        ' statusStrip
        ' 
        Me.statusStrip.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.lblStatus, Me.lblDevice})
        Me.statusStrip.Location = New System.Drawing.Point(0, 526)
        Me.statusStrip.Name = "statusStrip"
        Me.statusStrip.Size = New System.Drawing.Size(921, 27)
        Me.statusStrip.TabIndex = 2
        ' 
        ' lblStatus
        ' 
        Me.lblStatus.Name = "lblStatus"
        Me.lblStatus.Size = New System.Drawing.Size(700, 22)
        Me.lblStatus.Spring = True
        Me.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        ' 
        ' lblDevice
        ' 
        Me.lblDevice.Name = "lblDevice"
        Me.lblDevice.Size = New System.Drawing.Size(120, 22)
        ' 
        ' toolStrip
        ' 
        Me.toolStrip.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.cboMode, Me.cboScheme, Me.chkEmbedded, Me.numPointSize, Me.toolSep1, Me.btnReset, Me.chkShowGround, Me.btnBgColor, Me.chkShowDebug, Me.toolSep2, Me.btnSnapshot})
        Me.toolStrip.Location = New System.Drawing.Point(0, 24)
        Me.toolStrip.Name = "toolStrip"
        Me.toolStrip.Padding = New System.Windows.Forms.Padding(4, 0, 1, 0)
        Me.toolStrip.Size = New System.Drawing.Size(921, 25)
        Me.toolStrip.TabIndex = 3
        ' 
        ' cboMode
        ' 
        Me.cboMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboMode.Name = "cboMode"
        Me.cboMode.Size = New System.Drawing.Size(120, 25)
        ' 
        ' cboScheme
        ' 
        Me.cboScheme.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboScheme.Name = "cboScheme"
        Me.cboScheme.Size = New System.Drawing.Size(130, 25)
        ' 
        ' chkEmbedded
        ' 
        Me.chkEmbedded.CheckOnClick = True
        Me.chkEmbedded.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.chkEmbedded.Name = "chkEmbedded"
        Me.chkEmbedded.Size = New System.Drawing.Size(150, 22)
        Me.chkEmbedded.Text = "使用点云自带颜色"
        ' 
        ' numPointSize
        ' 
        Me.numPointSize.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.numPointSize.Name = "numPointSize"
        Me.numPointSize.Size = New System.Drawing.Size(60, 25)
        ' 
        ' toolSep1
        ' 
        Me.toolSep1.Name = "toolSep1"
        Me.toolSep1.Size = New System.Drawing.Size(6, 25)
        ' 
        ' btnReset
        ' 
        Me.btnReset.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.btnReset.Name = "btnReset"
        Me.btnReset.Size = New System.Drawing.Size(70, 22)
        Me.btnReset.Text = "重置视角"
        ' 
        ' chkShowGround
        ' 
        Me.chkShowGround.Checked = True
        Me.chkShowGround.CheckOnClick = True
        Me.chkShowGround.CheckState = System.Windows.Forms.CheckState.Checked
        Me.chkShowGround.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.chkShowGround.Name = "chkShowGround"
        Me.chkShowGround.Size = New System.Drawing.Size(70, 22)
        Me.chkShowGround.Text = "显示地面"
        ' 
        ' btnBgColor
        ' 
        Me.btnBgColor.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.btnBgColor.Name = "btnBgColor"
        Me.btnBgColor.Size = New System.Drawing.Size(55, 22)
        Me.btnBgColor.Text = "背景色"
        ' 
        ' chkShowDebug
        ' 
        Me.chkShowDebug.CheckOnClick = True
        Me.chkShowDebug.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.chkShowDebug.Name = "chkShowDebug"
        Me.chkShowDebug.Size = New System.Drawing.Size(70, 22)
        Me.chkShowDebug.Text = "调试信息"
        ' 
        ' toolSep2
        ' 
        Me.toolSep2.Name = "toolSep2"
        Me.toolSep2.Size = New System.Drawing.Size(6, 25)
        ' 
        ' btnSnapshot
        ' 
        Me.btnSnapshot.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.btnSnapshot.Name = "btnSnapshot"
        Me.btnSnapshot.Size = New System.Drawing.Size(45, 22)
        Me.btnSnapshot.Text = "截图"
        ' 
        ' menuStrip
        ' 
        Me.menuStrip.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.fileMenu, Me.viewMenu, Me.helpMenu})
        Me.menuStrip.Location = New System.Drawing.Point(0, 0)
        Me.menuStrip.Name = "menuStrip"
        Me.menuStrip.Size = New System.Drawing.Size(921, 24)
        Me.menuStrip.TabIndex = 4
        ' 
        ' fileMenu
        ' 
        Me.fileMenu.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.openItem, Me.sepFile, Me.exitItem})
        Me.fileMenu.Name = "fileMenu"
        Me.fileMenu.Size = New System.Drawing.Size(58, 20)
        Me.fileMenu.Text = "文件(&F)"
        ' 
        ' openItem
        ' 
        Me.openItem.Name = "openItem"
        Me.openItem.ShortcutKeys = CType((System.Windows.Forms.Keys.Control Or System.Windows.Forms.Keys.O), System.Windows.Forms.Keys)
        Me.openItem.Size = New System.Drawing.Size(200, 22)
        Me.openItem.Text = "打开模型/点云..."
        ' 
        ' sepFile
        ' 
        Me.sepFile.Name = "sepFile"
        Me.sepFile.Size = New System.Drawing.Size(197, 6)
        ' 
        ' exitItem
        ' 
        Me.exitItem.Name = "exitItem"
        Me.exitItem.Size = New System.Drawing.Size(200, 22)
        Me.exitItem.Text = "关闭"
        ' 
        ' viewMenu
        ' 
        Me.viewMenu.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.resetViewItem, Me.fitViewItem, Me.groundItem, Me.debugItem, Me.sepView, Me.bgColorItem, Me.snapshotItem})
        Me.viewMenu.Name = "viewMenu"
        Me.viewMenu.Size = New System.Drawing.Size(58, 20)
        Me.viewMenu.Text = "视图(&V)"
        ' 
        ' resetViewItem
        ' 
        Me.resetViewItem.Name = "resetViewItem"
        Me.resetViewItem.ShortcutKeys = CType((System.Windows.Forms.Keys.Control Or System.Windows.Forms.Keys.R), System.Windows.Forms.Keys)
        Me.resetViewItem.Size = New System.Drawing.Size(200, 22)
        Me.resetViewItem.Text = "重置视角"
        ' 
        ' fitViewItem
        ' 
        Me.fitViewItem.Name = "fitViewItem"
        Me.fitViewItem.ShortcutKeys = CType((System.Windows.Forms.Keys.Control Or System.Windows.Forms.Keys.F), System.Windows.Forms.Keys)
        Me.fitViewItem.Size = New System.Drawing.Size(200, 22)
        Me.fitViewItem.Text = "适应窗口"
        ' 
        ' groundItem
        ' 
        Me.groundItem.Checked = True
        Me.groundItem.CheckOnClick = True
        Me.groundItem.CheckState = System.Windows.Forms.CheckState.Checked
        Me.groundItem.Name = "groundItem"
        Me.groundItem.Size = New System.Drawing.Size(200, 22)
        Me.groundItem.Text = "显示地面"
        ' 
        ' debugItem
        ' 
        Me.debugItem.CheckOnClick = True
        Me.debugItem.Name = "debugItem"
        Me.debugItem.Size = New System.Drawing.Size(200, 22)
        Me.debugItem.Text = "调试信息"
        ' 
        ' sepView
        ' 
        Me.sepView.Name = "sepView"
        Me.sepView.Size = New System.Drawing.Size(197, 6)
        ' 
        ' bgColorItem
        ' 
        Me.bgColorItem.Name = "bgColorItem"
        Me.bgColorItem.Size = New System.Drawing.Size(200, 22)
        Me.bgColorItem.Text = "背景色..."
        ' 
        ' snapshotItem
        ' 
        Me.snapshotItem.Name = "snapshotItem"
        Me.snapshotItem.ShortcutKeys = CType((System.Windows.Forms.Keys.Control Or System.Windows.Forms.Keys.S), System.Windows.Forms.Keys)
        Me.snapshotItem.Size = New System.Drawing.Size(200, 22)
        Me.snapshotItem.Text = "保存截图..."
        ' 
        ' helpMenu
        ' 
        Me.helpMenu.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.aboutItem})
        Me.helpMenu.Name = "helpMenu"
        Me.helpMenu.Size = New System.Drawing.Size(58, 20)
        Me.helpMenu.Text = "帮助(&H)"
        ' 
        ' aboutItem
        ' 
        Me.aboutItem.Name = "aboutItem"
        Me.aboutItem.Size = New System.Drawing.Size(200, 22)
        Me.aboutItem.Text = "关于"
        ' 
        ' FormMain
        ' 
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(921, 553)
        ' the fill control is added first so that every docked edge control is
        ' laid out before it and the canvas receives the remaining area
        Me.Controls.Add(Me.canvas)
        Me.Controls.Add(Me.lightPanel)
        Me.Controls.Add(Me.statusStrip)
        Me.Controls.Add(Me.toolStrip)
        Me.Controls.Add(Me.menuStrip)
        Me.MainMenuStrip = Me.menuStrip
        Me.Name = "FormMain"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "三维模型查看器 - DirectX"
        Me.lightPanel.ResumeLayout(False)
        CType(Me.trkAzimuth, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.trkElevation, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.trkAmbient, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.trkIntensity, System.ComponentModel.ISupportInitialize).EndInit()
        Me.statusStrip.ResumeLayout(False)
        Me.toolStrip.ResumeLayout(False)
        Me.menuStrip.ResumeLayout(False)
        Me.ResumeLayout(False)
    End Sub

    Friend WithEvents canvas As Microsoft.VisualBasic.Drawing.DirectX.DxScene3DCanvas
    Friend WithEvents lightPanel As System.Windows.Forms.Panel
    Friend WithEvents lblLightTitle As System.Windows.Forms.Label
    Friend WithEvents lblAzimuthCaption As System.Windows.Forms.Label
    Friend WithEvents trkAzimuth As System.Windows.Forms.TrackBar
    Friend WithEvents lblAzimuthValue As System.Windows.Forms.Label
    Friend WithEvents lblElevationCaption As System.Windows.Forms.Label
    Friend WithEvents trkElevation As System.Windows.Forms.TrackBar
    Friend WithEvents lblElevationValue As System.Windows.Forms.Label
    Friend WithEvents lblAmbientCaption As System.Windows.Forms.Label
    Friend WithEvents trkAmbient As System.Windows.Forms.TrackBar
    Friend WithEvents lblAmbientValue As System.Windows.Forms.Label
    Friend WithEvents lblIntensityCaption As System.Windows.Forms.Label
    Friend WithEvents trkIntensity As System.Windows.Forms.TrackBar
    Friend WithEvents lblIntensityValue As System.Windows.Forms.Label
    Friend WithEvents btnLightColor As System.Windows.Forms.Button
    Friend WithEvents lblLightColor As System.Windows.Forms.Label
    Friend WithEvents btnResetLight As System.Windows.Forms.Button
    Friend WithEvents statusStrip As System.Windows.Forms.StatusStrip
    Friend WithEvents lblStatus As System.Windows.Forms.ToolStripStatusLabel
    Friend WithEvents lblDevice As System.Windows.Forms.ToolStripStatusLabel
    Friend WithEvents toolStrip As System.Windows.Forms.ToolStrip
    Friend WithEvents cboMode As System.Windows.Forms.ToolStripComboBox
    Friend WithEvents cboScheme As System.Windows.Forms.ToolStripComboBox
    Friend WithEvents chkEmbedded As System.Windows.Forms.ToolStripButton
    Friend WithEvents numPointSize As System.Windows.Forms.ToolStripComboBox
    Friend WithEvents toolSep1 As System.Windows.Forms.ToolStripSeparator
    Friend WithEvents btnReset As System.Windows.Forms.ToolStripButton
    Friend WithEvents chkShowGround As System.Windows.Forms.ToolStripButton
    Friend WithEvents btnBgColor As System.Windows.Forms.ToolStripButton
    Friend WithEvents chkShowDebug As System.Windows.Forms.ToolStripButton
    Friend WithEvents toolSep2 As System.Windows.Forms.ToolStripSeparator
    Friend WithEvents btnSnapshot As System.Windows.Forms.ToolStripButton
    Friend WithEvents menuStrip As System.Windows.Forms.MenuStrip
    Friend WithEvents fileMenu As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents openItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents sepFile As System.Windows.Forms.ToolStripSeparator
    Friend WithEvents exitItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents viewMenu As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents resetViewItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents fitViewItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents groundItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents debugItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents sepView As System.Windows.Forms.ToolStripSeparator
    Friend WithEvents bgColorItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents snapshotItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents helpMenu As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents aboutItem As System.Windows.Forms.ToolStripMenuItem
End Class
