# DXApi

A GPU accelerated 2D drawing canvas that is implemented on top of the Direct2D / D3D11 / DXGI / DirectWrite API, written completely in managed VB.NET.

## Installation

```
Install-Package DXApi
```

Requires a Windows target (`net10.0-windows`).

## Why this library

- **Pure managed interop** - this graphics object talks to the DirectX API completely through P/Invoke: there is no unmanaged helper library, no C++/CLI bridge and no `unsafe` code block. The whole interop layer is plain managed VB.NET code.
- **GPU render target** - the drawing commands are submitted to the GPU device through a Direct2D render target that is created on top of a D3D11 texture, and the final raster image is read back from the GPU texture when an image output is required.
- **Drop in replacement** - `DxGraphics` implements the full `Microsoft.VisualBasic.Imaging.IGraphics` contract (and the `GdiRasterGraphics` / `SaveGdiBitmap` contracts), so it is fully API compatible with the SkiaSharp raster canvas and existing drawing code works without any change.
- **Software fallback** - when no usable hardware device is available the engine falls back to the WARP software rasterizer, so the canvas keeps working on machines without a capable GPU.

## Usage

### Off screen canvas

```vbnet
Imports Microsoft.VisualBasic.Drawing.DirectX

Using g As New DxGraphics(800, 600, Color.White)

    Call g.FillRectangle(Brushes.Red, New Rectangle(10, 10, 200, 120))
    Call g.DrawEllipse(Pens.Blue, New Rectangle(240, 10, 200, 120))
    Call g.DrawString("Hello GPU", New Font(FontFace.Consolas, 24), Brushes.Black, New PointF(20, 200))

    ' read the GPU texture back into a raster bitmap
    Dim image = g.GetRasterImage

    Call g.Save("./output.png", ImageFormats.Png)
End Using
```

### Register as the drawing backend of the imaging framework

```vbnet
Imports Microsoft.VisualBasic.Drawing.DirectX

' replace the default GDI raster image driver with the
' directx api based gpu accelerated canvas
Call Dx2DDriver.RegisterDx2D()
```

### Window canvas (swap chain presentation)

`DxWindowCanvas` hosts a DXGI flip model swap chain on a window handle and presents every finished frame onto that window. The frame boundary is explicit: `BeginDraw` opens a frame, the caller draws through `Graphics`, and `EndDraw` submits and presents the frame.

```vbnet
Imports Microsoft.VisualBasic.Drawing.DirectX

Using canvas As New DxWindowCanvas(hwnd, width:=800, height:=600, dpi:=96.0F, vsync:=True)

    Call canvas.BeginDraw
    Call canvas.Graphics.Clear(Color.White)
    Call canvas.Graphics.FillRectangle(Brushes.Green, New Rectangle(0, 0, 400, 300))
    Call canvas.EndDraw

    Console.WriteLine(canvas.DeviceDescription) ' HARDWARE(feature_level=0xB100)
End Using
```

> The `DxCanvas` WinForms control (`Microsoft.VisualBasic.Drawing.DirectX.WinForm`) is the ready to use host of this window canvas.

## Features

- **DirectX interop layer** - managed declarations of the DXGI / D3D11 / Direct2D / DirectWrite COM interfaces, structures and enumerations, with HRESULT validation and safe releasing (`Native/ComBase`, `Native/DXGI`, `Native/D3D11`, `Native/D2D1`, `Native/DWrite`).
- **GPU render targets** - off screen D3D11 texture rendering plus texture read back into a pixel buffer (`DxDevice`, `DxRenderTarget`, `DxRenderSurface`), and a DXGI flip model swap chain target for on screen presentation (`DxSwapChainTarget`, `DxWindowCanvas`).
- **Complete 2D drawing** - polygons, rectangles, ellipses, arcs, bezier, curves, pies, paths, text, images, transforms, clipping and clearing, i.e. every overload of the shared `IGraphics` API.
- **Performance optimizations** - brush cache and text format cache (`DxBrushCache`), and same-colored polygon batching that merges figures into a single path geometry and rasterizes them with one `FillGeometry` call (`DxPolygonBatch`).
- **Pixel format conversion** - BGRA32 premultiplied / unpremultiplied conversion between the Direct2D target and the `Microsoft.VisualBasic.Imaging` bitmap model (`DxPixelFormat`).
- **Device loss recovery** - a removed GPU device and device dependent resources are rebuilt at the beginning of the next frame, so callers do not have to deal with device loss.

## Public API

| Type | Description |
| --- | --- |
| `DxGraphics` | The GPU accelerated 2D drawing canvas, a full `IGraphics` implementation. |
| `DxWindowCanvas` | A GPU canvas that presents its frames onto a window through a DXGI swap chain. |
| `Dx2DDriver` | The device interop driver module; `RegisterDx2D()` registers `DxGraphics` as the `Drivers.GDI` device. |

### `DxGraphics` members

| Member | Description |
| --- | --- |
| `New(width, height, fill, dpi)` | Creates an off screen GPU canvas of the given size and background color. |
| `Driver` | Always returns `Drivers.GDI`. |
| `DeviceDescription` | A short description of the underlying GPU device. |
| `BeginFrame` / `EndFrame` | Opens / submits a frame of a window swap chain canvas. |
| `ResizeCanvas(newWidth, newHeight)` | Resizes the GPU canvas and rebuilds every device dependent resource. |
| `GetRasterImage()` | Reads the GPU texture back into a `Microsoft.VisualBasic.Imaging.Bitmap`. |
| `Save(file, format)` | Saves the current canvas content into an image file. |

## Reusable 3D scene pipeline (`Scene3D`)

The `Microsoft.VisualBasic.Drawing.DirectX.Scene3D` namespace hosts the reusable and ui framework independent pipeline that turns a 3d model into an interactive, gpu accelerated scene. It is built on top of the 3d math and the object model of the `Microsoft.VisualBasic.Imaging.Drawing3D` namespace, which this package therefore depends on.

| Type | Description |
| --- | --- |
| `Scene` | The geometry of the scene: a set of faces and a set of point cloud points. Loading a model translates it so that its centroid becomes the origin, and computes the bounding sphere radius, the lowest Z (the height of the ground grid) and the intensity range of a point cloud. `FitView` computes the view distance for a canvas size. |
| `PointCloudPoint` | A format independent point of a point cloud: position, scalar intensity and an optional html color. |
| `SceneRenderMode` | `Surface`, `Mesh` or `PointCloud`. |
| `SceneRenderOptions` | The presentation options: mode, heat map scheme, point size and alpha, embedded point colors, ground grid and the colors. |
| `ISceneRenderBackend` | The pluggable rendering back end contract: one frame receives the canvas, the scene, the camera and the options. |
| `Direct2DSceneRenderer` | The default back end; it projects the faces, shades them and fills them through the shared `IGraphics` api, so it runs on the `DxGraphics` gpu canvas. |
| `OrbitCameraController` | The ui independent view interaction: orbit on a left drag, screen space pan on a right drag, zoom on the wheel and a reset. It exposes `ViewChanged` and never references winforms. |
| `SceneLighting` | The lighting parameters (azimuth, elevation, ambient strength, intensity and light color) that are applied onto the camera. |
| `SceneColorPalette` | The cached heat map color / brush tables and the cached per point colors. |

```vbnet
Imports Microsoft.VisualBasic.Drawing.DirectX
Imports Microsoft.VisualBasic.Drawing.DirectX.Scene3D

Dim scene As New Scene()
Dim cube As New Microsoft.VisualBasic.Imaging.Drawing3D.Models.Cube(1)
Call scene.LoadSurfaces(cube.faces)

Dim controller As New OrbitCameraController()
Dim lighting As New SceneLighting()
Dim options As New SceneRenderOptions With {.Mode = SceneRenderMode.Surface}

Call scene.FitView(controller.Camera, New Size(800, 600))
Call lighting.ApplyTo(controller.Camera)

Using canvas As New DxGraphics(800, 600, Color.White)
    Dim renderer As New Direct2DSceneRenderer()
    Call renderer.Render(canvas, scene, controller.Camera, options)
End Using
```

The winforms adapter of this pipeline is the `DxScene3DCanvas` control of the `Microsoft.VisualBasic.Drawing.DirectX.WinForm` package.

## Notes

- The GDI image data model (`IGraphicsData`) is not provided by this backend; use `DxGraphics.GetRasterImage` to read the raster image of the canvas instead.
- Because it talks to DirectX directly, this library is Windows only.
- The default `Scene3D` back end follows the projection and the painter's algorithm of the imaging framework exactly (`factor = fov / (viewDistance + z)`), so its output matches the cpu based renderer of that framework.
