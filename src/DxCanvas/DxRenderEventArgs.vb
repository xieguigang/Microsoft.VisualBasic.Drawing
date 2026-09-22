Imports System.Drawing
Imports IGraphics = Microsoft.VisualBasic.Imaging.IGraphics

''' <summary>
''' The event data of the <see cref="DxCanvas.Render"/> event.
''' </summary>
''' <remarks>
''' The drawing canvas that is passed here is the very same
''' <c>Microsoft.VisualBasic.Drawing.DirectX.DxGraphics</c> object that the
''' other rendering backend drivers of this solution create, so every existing
''' drawing code works without any change.
''' </remarks>
Public Class DxRenderEventArgs : Inherits EventArgs

    ''' <summary>
    ''' the gpu accelerated drawing canvas of the frame that is being rendered
    ''' </summary>
    Public ReadOnly Property Graphics As IGraphics

    ''' <summary>
    ''' the size of the canvas in pixels
    ''' </summary>
    Public ReadOnly Property Size As Size

    ''' <summary>
    ''' the width of the canvas in pixels
    ''' </summary>
    Public ReadOnly Property Width As Integer
        Get
            Return Size.Width
        End Get
    End Property

    ''' <summary>
    ''' the height of the canvas in pixels
    ''' </summary>
    Public ReadOnly Property Height As Integer
        Get
            Return Size.Height
        End Get
    End Property

    Friend Sub New(graphics As IGraphics, size As Size)
        _Graphics = graphics
        _Size = size
    End Sub
End Class
