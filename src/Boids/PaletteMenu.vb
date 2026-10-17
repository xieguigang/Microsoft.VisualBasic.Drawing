Imports System.Drawing
Imports System.Windows.Forms
Imports Microsoft.VisualBasic.Imaging.Drawing2D.Colors
Imports PaletteDesigner = Microsoft.VisualBasic.Imaging.Drawing2D.Colors.Designer
Imports std = System.Math

''' <summary>
''' The right click menu that selects the color scale of the boid speed coloring.
''' </summary>
''' <remarks>
''' <para>
''' Both demos color every boid by its normalized speed through a packed ARGB
''' lookup table, and the table is built from one of the
''' <see cref="ScalerPalette"/> scales. This helper fills the
''' <see cref="ContextMenuStrip"/> that the designer of either window creates
''' with one entry per scale - grouped into sub menus and decorated with a
''' gradient swatch of the scale - and it builds the lookup table that the
''' renderers consume.
''' </para>
''' <para>
''' The windows create the menu in their designer file and forward
''' <see cref="SelectionChanged"/> onto their renderer:
''' <c>m_renderer.Palette = m_palettes.SelectedPalette</c> for the directx demo
''' and <c>SDRender.Palette = m_palettes.SelectedPalette</c> for the gdi+ demo.
''' </para>
''' </remarks>
Friend NotInheritable Class PaletteMenu
    Implements IDisposable

    ''' <summary>
    ''' the scale that both demos have always used
    ''' </summary>
    Public Const DefaultPalette As ScalerPalette = ScalerPalette.viridis

    ''' <summary>
    ''' the number of the levels of the speed lookup table, it matches the level
    ''' count of both renderers
    ''' </summary>
    Public Const DefaultLevels As Integer = 30

    ''' <summary>
    ''' the size of the gradient swatch that decorates the menu entries
    ''' </summary>
    Private Const SwatchWidth As Integer = 84
    Private Const SwatchHeight As Integer = 16

    Private ReadOnly m_menu As ContextMenuStrip
    Private ReadOnly m_items As New List(Of ToolStripMenuItem)()
    Private ReadOnly m_images As New List(Of Image)()
    Private ReadOnly m_header As ToolStripMenuItem
    Private m_palette As ScalerPalette = DefaultPalette

    ''' <summary>
    ''' raised after the user has picked another color scale
    ''' </summary>
    Public Event SelectionChanged As EventHandler

    ''' <summary>
    ''' fill the given menu with the color scales of <see cref="ScalerPalette"/>
    ''' </summary>
    ''' <param name="menu">
    ''' the empty menu that the designer of the window created
    ''' </param>
    Public Sub New(menu As ContextMenuStrip)
        If menu Is Nothing Then
            Throw New ArgumentNullException(NameOf(menu))
        End If

        m_menu = menu
        m_header = New ToolStripMenuItem()
        m_header.Enabled = False

        Call m_menu.Items.Add(m_header)
        Call m_menu.Items.Add(New ToolStripSeparator())

        Call Build()
        Call UpdateChecked()

        ' the swatches are owned by this helper, the menu itself is owned by the
        ' components container of the window
        AddHandler m_menu.Disposed, Sub(sender As Object, e As EventArgs) Me.Dispose()
    End Sub

    ''' <summary>
    ''' the menu that this helper fills
    ''' </summary>
    Public ReadOnly Property Menu As ContextMenuStrip
        Get
            Return m_menu
        End Get
    End Property

    ''' <summary>
    ''' the color scale that is currently selected
    ''' </summary>
    Public Property SelectedPalette As ScalerPalette
        Get
            Return m_palette
        End Get
        Set(value As ScalerPalette)
            If value = m_palette Then
                Return
            End If

            m_palette = value

            Call UpdateChecked()
            RaiseEvent SelectionChanged(Me, EventArgs.Empty)
        End Set
    End Property

    ' /********************************************************************************/
    '  the menu structure
    ' /********************************************************************************/

    ''' <summary>
    ''' add one sub menu per family of scales, each one holding its scales in the
    ''' declaration order of the enumeration
    ''' </summary>
    Private Sub Build()
        Dim current As String = Nothing
        Dim group As ToolStripMenuItem = Nothing

        For Each palette As ScalerPalette In PaletteValues()
            Dim family As String = FamilyOf(palette)

            If group Is Nothing OrElse Not String.Equals(family, current, StringComparison.Ordinal) Then
                group = New ToolStripMenuItem(family)
                current = family

                Call m_menu.Items.Add(group)
            End If

            Dim entry As New ToolStripMenuItem(DisplayName(palette))

            entry.Tag = palette
            entry.ImageAlign = ContentAlignment.MiddleLeft
            entry.ImageScaling = ToolStripItemImageScaling.None

            Dim swatch As Image = CreateSwatchImage(palette)

            If swatch IsNot Nothing Then
                Call m_images.Add(swatch)
                entry.Image = swatch
            End If

            AddHandler entry.Click, AddressOf OnPaletteClick

            Call group.DropDownItems.Add(entry)
            Call m_items.Add(entry)
        Next
    End Sub

    ''' <summary>
    ''' the click of one scale entry
    ''' </summary>
    Private Sub OnPaletteClick(sender As Object, e As EventArgs)
        Dim entry As ToolStripMenuItem = TryCast(sender, ToolStripMenuItem)

        If entry Is Nothing OrElse Not TypeOf entry.Tag Is ScalerPalette Then
            Return
        End If

        Dim palette As ScalerPalette = CType(entry.Tag, ScalerPalette)

        ' assigning the property raises the event, also when the very same scale
        ' is clicked again it is skipped, the renderer keeps its table
        Me.SelectedPalette = palette
    End Sub

    ''' <summary>
    ''' show a check mark on the entry of the current scale and name it in the
    ''' disabled header of the menu
    ''' </summary>
    Private Sub UpdateChecked()
        For Each entry As ToolStripMenuItem In m_items
            entry.Checked = (CType(entry.Tag, ScalerPalette) = m_palette)
        Next

        m_header.Text = $"Boid 颜色调色板 (当前: {DisplayName(m_palette)})"
    End Sub

    ' /********************************************************************************/
    '  the color tables
    ' /********************************************************************************/

    ''' <summary>
    ''' build the packed ARGB lookup table of the given scale, it is the table
    ''' that maps the normalized speed of a boid onto the color of its arrow
    ''' </summary>
    ''' <param name="palette">the color scale</param>
    ''' <param name="levels">
    ''' the number of the levels of the table, zero or a negative value returns
    ''' the raw color list of the scale
    ''' </param>
    ''' <returns>
    ''' the packed ARGB values, a single lime entry when the scale can not be
    ''' resolved
    ''' </returns>
    ''' <remarks>
    ''' <see cref="PaletteDesigner.FromSchema(ScalerPalette, Integer)"/> applies a
    ''' cubic spline interpolation and returns <c>levels + 1</c> colors, the very
    ''' same table that the previous hard coded viridis lookup used.
    ''' </remarks>
    Public Shared Function ColorTable(palette As ScalerPalette, levels As Integer) As Integer()
        Dim scale As Color() = Nothing

        Try
            scale = PaletteDesigner.FromSchema(palette, levels)
        Catch ex As Exception
            ' a scale whose resource is missing (the color brewer sets are loaded
            ' from an embedded json file) falls back to a single visible color
            scale = Nothing
        End Try

        If scale Is Nothing OrElse scale.Length = 0 Then
            Return New Integer() {Color.Lime.ToArgb()}
        End If

        Dim table(scale.Length - 1) As Integer

        For i As Integer = 0 To scale.Length - 1
            table(i) = scale(i).ToArgb()
        Next

        Return table
    End Function

    ''' <summary>
    ''' draw the gradient swatch of a scale, the menu entry that shows it can be
    ''' recognized before it is selected
    ''' </summary>
    ''' <param name="palette">the color scale</param>
    ''' <returns>the swatch, or nothing when the scale can not be resolved</returns>
    Public Shared Function CreateSwatchImage(palette As ScalerPalette) As Image
        Dim scale As Color() = Nothing

        Try
            scale = PaletteDesigner.FromSchema(palette, SwatchWidth - 1)
        Catch ex As Exception
            scale = Nothing
        End Try

        If scale Is Nothing OrElse scale.Length = 0 Then
            Return Nothing
        End If

        Dim image As New Bitmap(SwatchWidth, SwatchHeight)

        Using g As Graphics = Graphics.FromImage(image)
            Using pen As New Pen(scale(0))
                For x As Integer = 0 To SwatchWidth - 1
                    Dim index As Integer = CInt(std.Floor(x * (scale.Length - 1) / std.Max(1, SwatchWidth - 1)))
                    pen.Color = scale(index)
                    Call g.DrawLine(pen, x, 0, x, SwatchHeight - 1)
                Next
            End Using

            ' a thin frame so that the light end of a scale does not bleed into
            ' the light menu background
            Using pen As New Pen(Color.FromArgb(96, 96, 104))
                Call g.DrawRectangle(pen, 0, 0, SwatchWidth - 1, SwatchHeight - 1)
            End Using
        End Using

        Return image
    End Function

    ''' <summary>
    ''' the name of one entry: the color brewer prefix is only noise inside the
    ''' sub menu that is named after it
    ''' </summary>
    Private Shared Function DisplayName(palette As ScalerPalette) As String
        Return palette.ToString().Replace("ColorBrewer_", "")
    End Function

    ''' <summary>
    ''' the sub menu that a scale belongs to
    ''' </summary>
    Private Shared Function FamilyOf(palette As ScalerPalette) As String
        Select Case palette
            Case ScalerPalette.Jet, ScalerPalette.Autumn, ScalerPalette.Cool, ScalerPalette.Gray,
                 ScalerPalette.Hot, ScalerPalette.Spring, ScalerPalette.Summer, ScalerPalette.Winter
                Return "经典色图"

            Case ScalerPalette.Red, ScalerPalette.Green, ScalerPalette.Blue
                Return "单通道"

            Case ScalerPalette.Rainbow, ScalerPalette.FlexImaging, ScalerPalette.Typhoon,
                 ScalerPalette.Icefire, ScalerPalette.Seismic
                Return "SciBASIC 色板"

            Case ScalerPalette.viridis, ScalerPalette.magma, ScalerPalette.inferno, ScalerPalette.plasma,
                 ScalerPalette.cividis, ScalerPalette.mako, ScalerPalette.rocket, ScalerPalette.turbo
                Return "Viridis 系列"

            Case Else
                Return "ColorBrewer"
        End Select
    End Function

    ''' <summary>
    ''' every value of <see cref="ScalerPalette"/> in the declaration order
    ''' </summary>
    Private Shared Function PaletteValues() As ScalerPalette()
        Dim values As Array = [Enum].GetValues(GetType(ScalerPalette))
        Dim result(values.Length - 1) As ScalerPalette

        For i As Integer = 0 To values.Length - 1
            result(i) = CType(values.GetValue(i), ScalerPalette)
        Next

        Return result
    End Function

    ''' <summary>
    ''' release the swatch images, the menu owner calls this through its
    ''' <see cref="Component.Disposed"/> event
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose
        For Each image As Image In m_images
            image.Dispose()
        Next

        Call m_images.Clear()
        Call m_items.Clear()
    End Sub

End Class
