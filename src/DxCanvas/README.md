# DxCanvas

A WinForms control that draws its whole content through the GPU accelerated DirectX 2D pipeline.

## Installation

```
Install-Package Microsoft.VisualBasic.Drawing.DirectX.WinForm
```

Requires a Windows target (`net10.0-windows`) with WinForms enabled.

## Overview

`DxCanvas` owns a DXGI flip model swap chain that is bound to its own window handle, so the WinForms paint cycle drives the GPU presentation directly:

1. `OnPaint` opens a drawing frame on the swap chain
2. the `Render` event is raised, and the handler draws through the `IGraphics` of the event data
3. `OnPaint` finishes the frame and presents it onto the window

The control never paints through GDI+ - the GPU canvas owns every pixel of the client area - so the background painting is suppressed and the opaque style is set, which removes the flickering frame of a GDI+ filled control.

The canvas is recreated together with the window handle, so docking, re-parenting and a DPI change are handled transparently. A removed GPU device is handled by the backend itself: the swap chain is rebuilt at the beginning of the next frame.

## Usage

### Handle the `Render` event

```vbnet
Imports System.Drawing
Imports Microsoft.VisualBasic.Drawing.DirectX

Public Class MainForm

    Private Sub DxCanvas1_Render(sender As Object, e As DxRenderEventArgs) Handles DxCanvas1.Render
        Dim g = e.Graphics

        Call g.FillRectangle(Brushes.White, New Rectangle(0, 0, e.Width, e.Height))
        Call g.FillEllipse(Brushes.OrangeRed, New Rectangle(20, 20, 200, 120))
        Call g.DrawString("GPU canvas", New Font(FontFace.Consolas, 16), Brushes.Black, New PointF(30, 160))
    End Sub
End Class
```

The frame is already opened when the event is raised: everything that is drawn in the handler becomes visible when the frame is presented right after the event returns.

### Draw on demand through the `Graphics` property

```vbnet
' the canvas is only available after the window handle has been created
DxCanvas1.Graphics.Clear(Color.White)
DxCanvas1.Graphics.FillRectangle(Brushes.SteelBlue, New Rectangle(40, 40, 300, 200))

' content drawn outside of the Render event is presented
' by the next paint request
Call DxCanvas1.Invalidate()
```

### Export the current frame

```vbnet
' save the current canvas content into an image file
Call DxCanvas1.SaveImage("./snapshot.png", ImageFormats.Png)

' or get the raster image object directly
Dim frame As Microsoft.VisualBasic.Imaging.Bitmap = DxCanvas1.CaptureFrame()
```

Both methods force a synchronous repaint of the control and capture the pixels of that frame, because the back buffer of a window swap chain only holds valid pixels while the frame is being submitted. They therefore raise the `Render` event one more time, and they can not be called from inside the `Render` event.

## Members

### Properties

| Property | Default | Description |
| --- | --- | --- |
| `Graphics` | - | The GPU accelerated drawing canvas of this control. Only available after the window handle has been created. |
| `AutoClear` | `True` | Clears the canvas with `BackgroundColor` at the beginning of every frame. |
| `BackgroundColor` | `White` | The color of the canvas background. |
| `VSync` | `True` | Waits for the vertical blank on every frame submission. |
| `DeviceDescription` | - | A short description of the GPU device that is in use. |
| `IsDeviceLost` | `False` | Whether the GPU device was removed; the swap chain is rebuilt on the next frame. |
| `LastError` | `Nothing` | The message of the last rendering failure; `Nothing` means every frame was drawn without an error. |

### Events

| Event | Description |
| --- | --- |
| `Render(sender, e)` | Raised when the control needs to redraw its content. The event data exposes the frame `Graphics`, its `Size`, `Width` and `Height`. |

### Methods

| Method | Description |
| --- | --- |
| `SaveImage(file, format)` | Saves the current canvas content into an image file. |
| `CaptureFrame()` | Exports the current canvas content as a raster image. |

## DxScene3DCanvas

`DxScene3DCanvas` derives from `DxCanvas` and renders an interactive 3d scene, so it is the ready to use control for any window that has to display a 3d model. It is a thin winforms adapter of the reusable scene pipeline: the geometry lives in `Scene3D.Scene`, the view interaction in `Scene3D.OrbitCameraController`, the lighting in `Scene3D.SceneLighting` and the rendering in a `Scene3D.ISceneRenderBackend` (the default back end renders through the gpu canvas of the base class).

### Input

| Input | Action |
| --- | --- |
| Left drag | Orbit the camera (yaw and pitch, 0.4 degrees per pixel) |
| Right drag | Translate the view in screen space |
| Mouse wheel | Change the view distance (zoom) |
| `R` | Reset the view |
| `F` | Fit the view onto the model |
| `M` | Cycle the presentation mode |
| `G` | Toggle the ground grid |
| `D` | Toggle the debug overlay |
| `S` | Request a snapshot, the host saves it |
| `+` / `-` / `Up` / `Down` | Zoom in / out |

### Members

| Member | Description |
| --- | --- |
| `Scene` | The geometry of the scene, it is never `Nothing`. |
| `LoadSurfaces(faces)` / `LoadPointCloud(points)` | Replace the geometry, reset the view and fit it onto the new model. |
| `ClearScene()` | Drop the geometry. |
| `Renderer` | The rendering back end, assign another `ISceneRenderBackend` to render the same scene differently. |
| `Controller` / `Lighting` / `Options` | The view interaction, the lighting and the presentation options. |
| `RenderMode`, `ColorScheme`, `PointSize`, `PointAlpha`, `UseEmbeddedColor` | The presentation options as designer friendly properties. |
| `ShowGround`, `GroundColor`, `BackgroundColor` | The ground grid and the background. |
| `ShowDebugOverlay`, `DebugText`, `ExtraDebugText` | The camera parameters and the frame rate of the host. |
| `FitView`, `ResetView`, `ZoomIn`, `ZoomOut`, `CycleRenderMode` | The commands behind the shortcuts. |
| `Snapshot()` / `SaveSnapshot(file, format)` | Export the current frame; both force a synchronous repaint and must not be called from inside a render handler. |
| `DeviceDescription`, `IsDeviceLost`, `LastError` | The gpu device state, inherited from `DxCanvas`. |
| `LastSceneError` | The message of the last failed frame; a broken frame does not take the host application down. |

### Example

```vbnet
Imports Microsoft.VisualBasic.Drawing.DirectX

Private Sub FormMain_Load(sender As Object, e As EventArgs) Handles MyBase.Load
    DxScene3DCanvas1.ShowGround = True
    DxScene3DCanvas1.ColorScheme = "viridis"
    DxScene3DCanvas1.LoadSurfaces(myModelFaces)
    DxScene3DCanvas1.ShowDebugOverlay = True
End Sub

Private Sub DxScene3DCanvas1_SnapshotRequested(sender As Object, e As EventArgs) Handles DxScene3DCanvas1.SnapshotRequested
    Call DxScene3DCanvas1.SaveSnapshot("./snapshot.png")
End Sub
```

## Notes

- Disable `AutoClear` only when the render handler covers the whole canvas itself: the back buffer of a flip model swap chain holds undefined pixels after the frame has been presented. `DxScene3DCanvas` disables it because the scene back end clears the canvas itself.
- Waiting for the vertical blank keeps the presentation smooth, but it blocks the UI thread for up to one screen refresh interval per frame. Turn `VSync` off for a more responsive UI when smoothness is not required.
- The control uses the pixel coordinate system of the window, so one drawing unit is exactly one pixel of the client area.
