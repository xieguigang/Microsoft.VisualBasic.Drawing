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
        canvas = New Drawing.DirectX.DxScene3DCanvas()
        lightPanel = New Panel()
        lblLightTitle = New Label()
        lblAzimuthCaption = New Label()
        trkAzimuth = New TrackBar()
        lblAzimuthValue = New Label()
        lblElevationCaption = New Label()
        trkElevation = New TrackBar()
        lblElevationValue = New Label()
        lblAmbientCaption = New Label()
        trkAmbient = New TrackBar()
        lblAmbientValue = New Label()
        lblIntensityCaption = New Label()
        trkIntensity = New TrackBar()
        lblIntensityValue = New Label()
        btnLightColor = New Button()
        lblLightColor = New Label()
        btnResetLight = New Button()
        statusStrip = New StatusStrip()
        lblStatus = New ToolStripStatusLabel()
        lblDevice = New ToolStripStatusLabel()
        toolStrip = New ToolStrip()
        cboMode = New ToolStripComboBox()
        cboScheme = New ToolStripComboBox()
        chkEmbedded = New ToolStripButton()
        numPointSize = New ToolStripComboBox()
        toolSep1 = New ToolStripSeparator()
        btnReset = New ToolStripButton()
        chkShowGround = New ToolStripButton()
        btnBgColor = New ToolStripButton()
        chkShowDebug = New ToolStripButton()
        toolSep2 = New ToolStripSeparator()
        btnSnapshot = New ToolStripButton()
        toolSep3 = New ToolStripSeparator()
        cboPipeline = New ToolStripComboBox()
        chkAntiAlias = New ToolStripButton()
        chkCull = New ToolStripButton()
        menuStrip = New MenuStrip()
        fileMenu = New ToolStripMenuItem()
        openItem = New ToolStripMenuItem()
        sepFile = New ToolStripSeparator()
        exitItem = New ToolStripMenuItem()
        viewMenu = New ToolStripMenuItem()
        resetViewItem = New ToolStripMenuItem()
        fitViewItem = New ToolStripMenuItem()
        groundItem = New ToolStripMenuItem()
        debugItem = New ToolStripMenuItem()
        sepView = New ToolStripSeparator()
        bgColorItem = New ToolStripMenuItem()
        snapshotItem = New ToolStripMenuItem()
        helpMenu = New ToolStripMenuItem()
        aboutItem = New ToolStripMenuItem()
        复制设备错误信息ToolStripMenuItem = New ToolStripMenuItem()
        lightPanel.SuspendLayout()
        CType(trkAzimuth, ComponentModel.ISupportInitialize).BeginInit()
        CType(trkElevation, ComponentModel.ISupportInitialize).BeginInit()
        CType(trkAmbient, ComponentModel.ISupportInitialize).BeginInit()
        CType(trkIntensity, ComponentModel.ISupportInitialize).BeginInit()
        statusStrip.SuspendLayout()
        toolStrip.SuspendLayout()
        menuStrip.SuspendLayout()
        SuspendLayout()
        ' 
        ' canvas
        ' 
        canvas.AutoClear = False
        canvas.BackColor = Color.White
        canvas.Dock = DockStyle.Fill
        canvas.Location = New Point(0, 24)
        canvas.Name = "canvas"
        canvas.Size = New Size(671, 507)
        canvas.TabIndex = 0
        ' 
        ' lightPanel
        ' 
        lightPanel.BorderStyle = BorderStyle.FixedSingle
        lightPanel.Controls.Add(lblLightTitle)
        lightPanel.Controls.Add(lblAzimuthCaption)
        lightPanel.Controls.Add(trkAzimuth)
        lightPanel.Controls.Add(lblAzimuthValue)
        lightPanel.Controls.Add(lblElevationCaption)
        lightPanel.Controls.Add(trkElevation)
        lightPanel.Controls.Add(lblElevationValue)
        lightPanel.Controls.Add(lblAmbientCaption)
        lightPanel.Controls.Add(trkAmbient)
        lightPanel.Controls.Add(lblAmbientValue)
        lightPanel.Controls.Add(lblIntensityCaption)
        lightPanel.Controls.Add(trkIntensity)
        lightPanel.Controls.Add(lblIntensityValue)
        lightPanel.Controls.Add(btnLightColor)
        lightPanel.Controls.Add(lblLightColor)
        lightPanel.Controls.Add(btnResetLight)
        lightPanel.Dock = DockStyle.Right
        lightPanel.Location = New Point(671, 24)
        lightPanel.Name = "lightPanel"
        lightPanel.Size = New Size(250, 507)
        lightPanel.TabIndex = 1
        ' 
        ' lblLightTitle
        ' 
        lblLightTitle.AutoSize = True
        lblLightTitle.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        lblLightTitle.Location = New Point(12, 12)
        lblLightTitle.Name = "lblLightTitle"
        lblLightTitle.Size = New Size(63, 15)
        lblLightTitle.TabIndex = 0
        lblLightTitle.Text = "光照参数"
        ' 
        ' lblAzimuthCaption
        ' 
        lblAzimuthCaption.AutoSize = True
        lblAzimuthCaption.Location = New Point(12, 46)
        lblAzimuthCaption.Name = "lblAzimuthCaption"
        lblAzimuthCaption.Size = New Size(59, 15)
        lblAzimuthCaption.TabIndex = 1
        lblAzimuthCaption.Text = "光源方位"
        ' 
        ' trkAzimuth
        ' 
        trkAzimuth.LargeChange = 10
        trkAzimuth.Location = New Point(96, 40)
        trkAzimuth.Maximum = 360
        trkAzimuth.Minimum = -360
        trkAzimuth.Name = "trkAzimuth"
        trkAzimuth.Size = New Size(110, 45)
        trkAzimuth.SmallChange = 5
        trkAzimuth.TabIndex = 2
        trkAzimuth.TickFrequency = 30
        trkAzimuth.Value = -30
        ' 
        ' lblAzimuthValue
        ' 
        lblAzimuthValue.AutoSize = True
        lblAzimuthValue.Location = New Point(212, 46)
        lblAzimuthValue.Name = "lblAzimuthValue"
        lblAzimuthValue.Size = New Size(24, 15)
        lblAzimuthValue.TabIndex = 3
        lblAzimuthValue.Text = "-30"
        ' 
        ' lblElevationCaption
        ' 
        lblElevationCaption.AutoSize = True
        lblElevationCaption.Location = New Point(12, 88)
        lblElevationCaption.Name = "lblElevationCaption"
        lblElevationCaption.Size = New Size(59, 15)
        lblElevationCaption.TabIndex = 4
        lblElevationCaption.Text = "光源仰角"
        ' 
        ' trkElevation
        ' 
        trkElevation.LargeChange = 10
        trkElevation.Location = New Point(96, 82)
        trkElevation.Maximum = 90
        trkElevation.Minimum = -90
        trkElevation.Name = "trkElevation"
        trkElevation.Size = New Size(110, 45)
        trkElevation.SmallChange = 5
        trkElevation.TabIndex = 5
        trkElevation.TickFrequency = 15
        trkElevation.Value = 45
        ' 
        ' lblElevationValue
        ' 
        lblElevationValue.AutoSize = True
        lblElevationValue.Location = New Point(212, 88)
        lblElevationValue.Name = "lblElevationValue"
        lblElevationValue.Size = New Size(19, 15)
        lblElevationValue.TabIndex = 6
        lblElevationValue.Text = "45"
        ' 
        ' lblAmbientCaption
        ' 
        lblAmbientCaption.AutoSize = True
        lblAmbientCaption.Location = New Point(12, 130)
        lblAmbientCaption.Name = "lblAmbientCaption"
        lblAmbientCaption.Size = New Size(46, 15)
        lblAmbientCaption.TabIndex = 7
        lblAmbientCaption.Text = "环境光"
        ' 
        ' trkAmbient
        ' 
        trkAmbient.LargeChange = 10
        trkAmbient.Location = New Point(96, 124)
        trkAmbient.Maximum = 100
        trkAmbient.Name = "trkAmbient"
        trkAmbient.Size = New Size(110, 45)
        trkAmbient.SmallChange = 5
        trkAmbient.TabIndex = 8
        trkAmbient.TickFrequency = 10
        trkAmbient.Value = 25
        ' 
        ' lblAmbientValue
        ' 
        lblAmbientValue.AutoSize = True
        lblAmbientValue.Location = New Point(212, 130)
        lblAmbientValue.Name = "lblAmbientValue"
        lblAmbientValue.Size = New Size(19, 15)
        lblAmbientValue.TabIndex = 9
        lblAmbientValue.Text = "25"
        ' 
        ' lblIntensityCaption
        ' 
        lblIntensityCaption.AutoSize = True
        lblIntensityCaption.Location = New Point(12, 172)
        lblIntensityCaption.Name = "lblIntensityCaption"
        lblIntensityCaption.Size = New Size(59, 15)
        lblIntensityCaption.TabIndex = 10
        lblIntensityCaption.Text = "光照亮度"
        ' 
        ' trkIntensity
        ' 
        trkIntensity.LargeChange = 10
        trkIntensity.Location = New Point(96, 166)
        trkIntensity.Maximum = 100
        trkIntensity.Name = "trkIntensity"
        trkIntensity.Size = New Size(110, 45)
        trkIntensity.SmallChange = 5
        trkIntensity.TabIndex = 11
        trkIntensity.TickFrequency = 10
        trkIntensity.Value = 65
        ' 
        ' lblIntensityValue
        ' 
        lblIntensityValue.AutoSize = True
        lblIntensityValue.Location = New Point(212, 172)
        lblIntensityValue.Name = "lblIntensityValue"
        lblIntensityValue.Size = New Size(19, 15)
        lblIntensityValue.TabIndex = 12
        lblIntensityValue.Text = "65"
        ' 
        ' btnLightColor
        ' 
        btnLightColor.Location = New Point(12, 214)
        btnLightColor.Name = "btnLightColor"
        btnLightColor.Size = New Size(110, 28)
        btnLightColor.TabIndex = 13
        btnLightColor.Text = "灯光颜色"
        btnLightColor.UseVisualStyleBackColor = True
        ' 
        ' lblLightColor
        ' 
        lblLightColor.BackColor = Color.White
        lblLightColor.BorderStyle = BorderStyle.FixedSingle
        lblLightColor.Location = New Point(130, 214)
        lblLightColor.Name = "lblLightColor"
        lblLightColor.Size = New Size(60, 28)
        lblLightColor.TabIndex = 14
        ' 
        ' btnResetLight
        ' 
        btnResetLight.Location = New Point(12, 252)
        btnResetLight.Name = "btnResetLight"
        btnResetLight.Size = New Size(178, 28)
        btnResetLight.TabIndex = 15
        btnResetLight.Text = "重置光照"
        btnResetLight.UseVisualStyleBackColor = True
        ' 
        ' statusStrip
        ' 
        statusStrip.Items.AddRange(New ToolStripItem() {lblStatus, lblDevice})
        statusStrip.Location = New Point(0, 531)
        statusStrip.Name = "statusStrip"
        statusStrip.Size = New Size(921, 22)
        statusStrip.TabIndex = 2
        ' 
        ' lblStatus
        ' 
        lblStatus.Name = "lblStatus"
        lblStatus.Size = New Size(834, 17)
        lblStatus.Spring = True
        lblStatus.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' lblDevice
        ' 
        lblDevice.Name = "lblDevice"
        lblDevice.Size = New Size(72, 17)
        lblDevice.Text = "准备就绪！"
        ' 
        ' toolStrip
        ' 
        toolStrip.Items.AddRange(New ToolStripItem() {cboMode, cboScheme, chkEmbedded, numPointSize, toolSep1, btnReset, chkShowGround, btnBgColor, chkShowDebug, toolSep2, btnSnapshot, toolSep3, cboPipeline, chkAntiAlias, chkCull})
        toolStrip.Location = New Point(0, 24)
        toolStrip.Name = "toolStrip"
        toolStrip.Padding = New Padding(4, 0, 1, 0)
        toolStrip.Size = New Size(671, 25)
        toolStrip.TabIndex = 3
        ' 
        ' cboMode
        ' 
        cboMode.DropDownStyle = ComboBoxStyle.DropDownList
        cboMode.Name = "cboMode"
        cboMode.Size = New Size(120, 25)
        ' 
        ' cboScheme
        ' 
        cboScheme.DropDownStyle = ComboBoxStyle.DropDownList
        cboScheme.Name = "cboScheme"
        cboScheme.Size = New Size(130, 25)
        ' 
        ' chkEmbedded
        ' 
        chkEmbedded.CheckOnClick = True
        chkEmbedded.DisplayStyle = ToolStripItemDisplayStyle.Text
        chkEmbedded.Name = "chkEmbedded"
        chkEmbedded.Size = New Size(115, 22)
        chkEmbedded.Text = "使用点云自带颜色"
        ' 
        ' numPointSize
        ' 
        numPointSize.DropDownStyle = ComboBoxStyle.DropDownList
        numPointSize.Name = "numPointSize"
        numPointSize.Size = New Size(75, 25)
        ' 
        ' toolSep1
        ' 
        toolSep1.Name = "toolSep1"
        toolSep1.Size = New Size(6, 25)
        ' 
        ' btnReset
        ' 
        btnReset.DisplayStyle = ToolStripItemDisplayStyle.Text
        btnReset.Name = "btnReset"
        btnReset.Size = New Size(63, 22)
        btnReset.Text = "重置视角"
        ' 
        ' chkShowGround
        ' 
        chkShowGround.Checked = True
        chkShowGround.CheckOnClick = True
        chkShowGround.CheckState = CheckState.Checked
        chkShowGround.DisplayStyle = ToolStripItemDisplayStyle.Text
        chkShowGround.Name = "chkShowGround"
        chkShowGround.Size = New Size(63, 22)
        chkShowGround.Text = "显示地面"
        ' 
        ' btnBgColor
        ' 
        btnBgColor.DisplayStyle = ToolStripItemDisplayStyle.Text
        btnBgColor.Name = "btnBgColor"
        btnBgColor.Size = New Size(50, 22)
        btnBgColor.Text = "背景色"
        ' 
        ' chkShowDebug
        ' 
        chkShowDebug.CheckOnClick = True
        chkShowDebug.DisplayStyle = ToolStripItemDisplayStyle.Text
        chkShowDebug.Name = "chkShowDebug"
        chkShowDebug.Size = New Size(63, 19)
        chkShowDebug.Text = "调试信息"
        ' 
        ' toolSep2
        ' 
        toolSep2.Name = "toolSep2"
        toolSep2.Size = New Size(6, 25)
        ' 
        ' btnSnapshot
        ' 
        btnSnapshot.DisplayStyle = ToolStripItemDisplayStyle.Text
        btnSnapshot.Name = "btnSnapshot"
        btnSnapshot.Size = New Size(37, 19)
        btnSnapshot.Text = "截图"
        ' 
        ' toolSep3
        ' 
        toolSep3.Name = "toolSep3"
        toolSep3.Size = New Size(6, 25)
        ' 
        ' cboPipeline
        ' 
        cboPipeline.DropDownStyle = ComboBoxStyle.DropDownList
        cboPipeline.Name = "cboPipeline"
        cboPipeline.Size = New Size(170, 25)
        ' 
        ' chkAntiAlias
        ' 
        chkAntiAlias.CheckOnClick = True
        chkAntiAlias.DisplayStyle = ToolStripItemDisplayStyle.Text
        chkAntiAlias.Name = "chkAntiAlias"
        chkAntiAlias.Size = New Size(75, 22)
        chkAntiAlias.Text = "抗锯齿 4x"
        ' 
        ' chkCull
        ' 
        chkCull.CheckOnClick = True
        chkCull.DisplayStyle = ToolStripItemDisplayStyle.Text
        chkCull.Name = "chkCull"
        chkCull.Size = New Size(75, 22)
        chkCull.Text = "背面剔除"
        ' 
        ' menuStrip
        ' 
        menuStrip.Items.AddRange(New ToolStripItem() {fileMenu, viewMenu, helpMenu})
        menuStrip.Location = New Point(0, 0)
        menuStrip.Name = "menuStrip"
        menuStrip.Size = New Size(921, 24)
        menuStrip.TabIndex = 4
        ' 
        ' fileMenu
        ' 
        fileMenu.DropDownItems.AddRange(New ToolStripItem() {openItem, sepFile, exitItem})
        fileMenu.Name = "fileMenu"
        fileMenu.Size = New Size(59, 20)
        fileMenu.Text = "文件(&F)"
        ' 
        ' openItem
        ' 
        openItem.Name = "openItem"
        openItem.ShortcutKeys = Keys.Control Or Keys.O
        openItem.Size = New Size(209, 22)
        openItem.Text = "打开模型/点云..."
        ' 
        ' sepFile
        ' 
        sepFile.Name = "sepFile"
        sepFile.Size = New Size(206, 6)
        ' 
        ' exitItem
        ' 
        exitItem.Name = "exitItem"
        exitItem.Size = New Size(209, 22)
        exitItem.Text = "关闭"
        ' 
        ' viewMenu
        ' 
        viewMenu.DropDownItems.AddRange(New ToolStripItem() {resetViewItem, fitViewItem, groundItem, debugItem, sepView, bgColorItem, snapshotItem})
        viewMenu.Name = "viewMenu"
        viewMenu.Size = New Size(60, 20)
        viewMenu.Text = "视图(&V)"
        ' 
        ' resetViewItem
        ' 
        resetViewItem.Name = "resetViewItem"
        resetViewItem.ShortcutKeys = Keys.Control Or Keys.R
        resetViewItem.Size = New Size(175, 22)
        resetViewItem.Text = "重置视角"
        ' 
        ' fitViewItem
        ' 
        fitViewItem.Name = "fitViewItem"
        fitViewItem.ShortcutKeys = Keys.Control Or Keys.F
        fitViewItem.Size = New Size(175, 22)
        fitViewItem.Text = "适应窗口"
        ' 
        ' groundItem
        ' 
        groundItem.Checked = True
        groundItem.CheckOnClick = True
        groundItem.CheckState = CheckState.Checked
        groundItem.Name = "groundItem"
        groundItem.Size = New Size(175, 22)
        groundItem.Text = "显示地面"
        ' 
        ' debugItem
        ' 
        debugItem.CheckOnClick = True
        debugItem.Name = "debugItem"
        debugItem.Size = New Size(175, 22)
        debugItem.Text = "调试信息"
        ' 
        ' sepView
        ' 
        sepView.Name = "sepView"
        sepView.Size = New Size(172, 6)
        ' 
        ' bgColorItem
        ' 
        bgColorItem.Name = "bgColorItem"
        bgColorItem.Size = New Size(175, 22)
        bgColorItem.Text = "背景色..."
        ' 
        ' snapshotItem
        ' 
        snapshotItem.Name = "snapshotItem"
        snapshotItem.ShortcutKeys = Keys.Control Or Keys.S
        snapshotItem.Size = New Size(175, 22)
        snapshotItem.Text = "保存截图..."
        ' 
        ' helpMenu
        ' 
        helpMenu.DropDownItems.AddRange(New ToolStripItem() {aboutItem, 复制设备错误信息ToolStripMenuItem})
        helpMenu.Name = "helpMenu"
        helpMenu.Size = New Size(62, 20)
        helpMenu.Text = "帮助(&H)"
        ' 
        ' aboutItem
        ' 
        aboutItem.Name = "aboutItem"
        aboutItem.Size = New Size(180, 22)
        aboutItem.Text = "关于"
        ' 
        ' 复制设备错误信息ToolStripMenuItem
        ' 
        复制设备错误信息ToolStripMenuItem.Name = "复制设备错误信息ToolStripMenuItem"
        复制设备错误信息ToolStripMenuItem.Size = New Size(180, 22)
        复制设备错误信息ToolStripMenuItem.Text = "复制设备错误信息"
        ' 
        ' FormMain
        ' 
        AutoScaleDimensions = New SizeF(7.0F, 15.0F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(1180, 660)
        MinimumSize = New Size(900, 520)
        Controls.Add(toolStrip)
        Controls.Add(canvas)
        Controls.Add(lightPanel)
        Controls.Add(statusStrip)
        Controls.Add(menuStrip)
        MainMenuStrip = menuStrip
        Name = "FormMain"
        StartPosition = FormStartPosition.CenterScreen
        Text = "三维模型查看器 - DirectX"
        lightPanel.ResumeLayout(False)
        lightPanel.PerformLayout()
        CType(trkAzimuth, ComponentModel.ISupportInitialize).EndInit()
        CType(trkElevation, ComponentModel.ISupportInitialize).EndInit()
        CType(trkAmbient, ComponentModel.ISupportInitialize).EndInit()
        CType(trkIntensity, ComponentModel.ISupportInitialize).EndInit()
        statusStrip.ResumeLayout(False)
        statusStrip.PerformLayout()
        toolStrip.ResumeLayout(False)
        toolStrip.PerformLayout()
        menuStrip.ResumeLayout(False)
        menuStrip.PerformLayout()
        ResumeLayout(False)
        PerformLayout()
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
    Friend WithEvents toolSep3 As System.Windows.Forms.ToolStripSeparator
    Friend WithEvents cboPipeline As System.Windows.Forms.ToolStripComboBox
    Friend WithEvents chkAntiAlias As System.Windows.Forms.ToolStripButton
    Friend WithEvents chkCull As System.Windows.Forms.ToolStripButton
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
    Friend WithEvents 复制设备错误信息ToolStripMenuItem As ToolStripMenuItem
End Class
