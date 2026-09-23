Imports System.Drawing
Imports System.IO
Imports Microsoft.VisualBasic.Drawing.DirectX
Imports Microsoft.VisualBasic.Drawing.DirectX.Scene3D
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Drawing3D
Imports Microsoft.VisualBasic.Imaging.Drawing3D.Models
Imports Bitmap = Microsoft.VisualBasic.Imaging.Bitmap
Imports Brush = Microsoft.VisualBasic.Imaging.Brush
Imports ImageFormats = Microsoft.VisualBasic.Imaging.ImageFormats
Imports SolidBrush = Microsoft.VisualBasic.Imaging.SolidBrush
Imports std = System.Math

''' <summary>
''' A throw away smoke test of the reusable 3d scene pipeline: it exercises the
''' scene bounds, the view interaction, the lighting, the color palette and the
''' three presentation modes of the default render back end.
''' </summary>
Module Program

    Private ReadOnly schemes As String() = {
        "viridis", "magma", "inferno", "plasma", "turbo", "jet",
        "rainbow", "cividis", "mako", "rocket", "viridis:rocket"
    }

    Private failures As Integer = 0

    Sub Main()
        Dim outDir As String = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..\..\..\out"))
        Call Directory.CreateDirectory(outDir)

        Console.WriteLine("output folder: " & outDir)

        Call TestSceneBounds()
        Call TestViewInteraction()
        Call TestLighting()
        Call TestPalette()
        Call TestRenderModes(outDir)
        Call TestPointCloud(outDir)

        Console.WriteLine()

        If failures = 0 Then
            Console.WriteLine("ALL CHECKS PASSED")
        Else
            Console.WriteLine($"{failures} CHECK(S) FAILED")
        End If
    End Sub

    Private Sub Check(condition As Boolean, name As String)
        If condition Then
            Console.WriteLine("  ok   " & name)
        Else
            failures += 1
            Console.WriteLine("  FAIL " & name)
        End If
    End Sub

    Private Function NonBackgroundPixels(image As Bitmap, background As Color) As Integer
        Dim count As Integer = 0

        For y As Integer = 0 To image.Height - 1
            For x As Integer = 0 To image.Width - 1
                Dim c As Color = image.GetPixel(x, y)

                If c.R <> background.R OrElse c.G <> background.G OrElse c.B <> background.B Then
                    count += 1
                End If
            Next
        Next

        Return count
    End Function

    Private Sub TestSceneBounds()
        Console.WriteLine("scene bounds")

        Dim scene As New Scene()
        Dim cube As New Cube(1)
        Call scene.LoadSurfaces(cube.faces)

        Call Check(scene.SurfaceCount = 6, "the cube contributes 6 faces")
        Call Check(std.Abs(scene.Center.X) < 0.0000001 AndAlso
                   std.Abs(scene.Center.Y) < 0.0000001 AndAlso
                   std.Abs(scene.Center.Z) < 0.0000001, "the centroid becomes the origin")
        Call Check(std.Abs(scene.Radius - std.Sqrt(3)) < 0.0001, "the bounding sphere radius is sqrt(3)")
        Call Check(std.Abs(scene.GroundZ + 1) < 0.0001, "the ground is placed at the lowest vertex")
        Call Check(scene.HasData, "the scene reports data")

        Dim controller As New OrbitCameraController()
        Call scene.FitView(controller.Camera, New Size(800, 600))

        Call Check(controller.Camera.FieldOfView = 2048, "fit view scales the field of view")
        Call Check(std.Abs(controller.Camera.ViewDistance - scene.Radius * 2048 / (0.4 * 600)) < 0.01,
                   "fit view computes the view distance from the radius")
        Call Check(controller.Camera.Screen.Width = 800 AndAlso controller.Camera.Screen.Height = 600,
                   "fit view stores the viewport")

        ' degenerate faces must be dropped and must not break the bounds
        Dim empty As New Scene()
        Call empty.LoadSurfaces(New Surface() {
            New Surface With {
                .vertices = {New Point3D(0, 0, 0), New Point3D(1, 0, 0)},
                .brush = New SolidBrush(Color.Red)
            }
        })

        Call Check(empty.SurfaceCount = 0, "a degenerate face is dropped")
        Call Check(Not empty.HasData, "a scene without faces has no data")

        Dim cleared As New Scene()
        Call cleared.LoadSurfaces(cube.faces)
        Call cleared.Clear()
        Call Check(Not cleared.HasData AndAlso cleared.SurfaceCount = 0, "clear drops the geometry")
    End Sub

    Private Sub TestViewInteraction()
        Console.WriteLine("view interaction")

        Dim controller As New OrbitCameraController()
        Dim camera As Camera = controller.Camera
        Dim startX As Single = camera.AngleX
        Dim startY As Single = camera.AngleY

        Call controller.MouseDown(SceneMouseButton.Left, 100, 100)
        Call Check(controller.IsDragging, "the controller tracks the held button")

        Call controller.MouseMove(150, 130)
        Call Check(std.Abs(camera.AngleY - (startY + 50 * 0.4)) < 0.001, "the left drag yaws the camera by dx * 0.4")
        Call Check(std.Abs(camera.AngleX - (startX + 30 * 0.4)) < 0.001, "the left drag pitches the camera by dy * 0.4")

        Call controller.MouseUp(SceneMouseButton.Left)
        Call Check(Not controller.IsDragging, "the release clears the drag state")

        Call controller.MouseDown(SceneMouseButton.Right, 10, 10)
        Call controller.MouseMove(30, 25)
        Call Check(camera.Offset.X = 20 AndAlso camera.Offset.Y = 15, "the right drag pans the view in screen space")
        Call controller.MouseUp(SceneMouseButton.Right)

        Dim distance As Single = camera.ViewDistance
        Call controller.MouseWheel(120)
        Call Check(std.Abs(camera.ViewDistance - distance * 0.9) < 0.001, "the wheel up zooms in by 0.9")

        Call controller.MouseWheel(-120)
        Call Check(std.Abs(camera.ViewDistance - distance * 0.9 * 1.1) < 0.001, "the wheel down zooms out by 1.1")

        For i As Integer = 1 To 500
            Call controller.MouseWheel(120)
        Next

        Call Check(camera.ViewDistance = controller.MinViewDistance, "the view distance is clamped at the minimum")

        Call controller.Reset()
        Call Check(camera.AngleX = Scene.DefaultAngleX AndAlso camera.AngleY = Scene.DefaultAngleY, "the reset restores the default angles")
        Call Check(camera.Offset.X = 0 AndAlso camera.Offset.Y = 0, "the reset restores the screen offset")
    End Sub

    Private Sub TestLighting()
        Console.WriteLine("lighting")

        Dim lighting As New SceneLighting()
        Dim camera As New Camera()

        Call lighting.ApplyTo(camera)
        Call Check(std.Abs(camera.AmbientStrength - 0.25) < 0.0001, "the default ambient strength is applied")
        Call Check(camera.LightColor.R >= 164 AndAlso camera.LightColor.R <= 166,
                   "the light color is scaled by the default intensity")
        Call Check(std.Abs(camera.LightDirection.Z - std.Sin(45 * std.PI / 180)) < 0.001,
                   "the default elevation is applied to the light direction")

        lighting.Ambient = 50
        lighting.Intensity = 100
        lighting.Elevation = 0
        lighting.Azimuth = 0
        Call lighting.ApplyTo(camera)

        Call Check(std.Abs(camera.AmbientStrength - 0.5) < 0.0001, "the ambient slider drives the ambient strength")
        Call Check(camera.LightColor = Color.White, "a full intensity keeps the white light color")
        Call Check(std.Abs(camera.LightDirection.X - 1) < 0.001 AndAlso std.Abs(camera.LightDirection.Y) < 0.001,
                   "a zero azimuth and elevation points the light along +X")

        Call lighting.Reset()
        Call Check(lighting.Azimuth = SceneLighting.DefaultAzimuth AndAlso
                   lighting.Elevation = SceneLighting.DefaultElevation AndAlso
                   lighting.Ambient = SceneLighting.DefaultAmbient AndAlso
                   lighting.Intensity = SceneLighting.DefaultIntensity AndAlso
                   lighting.LightColor = Color.White, "the reset restores every default")

        Dim clone As SceneLighting = lighting.Clone()
        clone.Azimuth = 123
        Call Check(lighting.Azimuth <> clone.Azimuth, "the clone is an independent copy")
    End Sub

    Private Sub TestPalette()
        Console.WriteLine("color palette")

        Dim palette As New SceneColorPalette()
        Dim table As Color() = palette.GetTable("viridis", 255)

        Call Check(table.Length = SceneColorPalette.Levels, "the color table has 256 levels")
        Call Check(table(0) <> table(table.Length - 1), "the two ends of the scheme differ")

        Dim brushes As Brush() = palette.GetBrushes("viridis", 255)
        Call Check(brushes.Length = table.Length, "the brush table matches the color table")
        Call Check(palette.GetHeatBrush(-1, "viridis", 255) Is brushes(0), "a value below zero clamps to the coldest color")
        Call Check(palette.GetHeatBrush(2, "viridis", 255) Is brushes(brushes.Length - 1), "a value above one clamps to the hottest color")

        Dim unknown As Color() = palette.GetTable("this-is-not-a-scheme", 255)
        Call Check(unknown.Length >= 1, "an unknown scheme falls back to a solid color table")

        Call Check(palette.GetEmbeddedBrush("#00ff00") IsNot Nothing, "an html color maps to a brush")
        Call Check(palette.GetEmbeddedBrush("#00ff00") Is palette.GetEmbeddedBrush("#00ff00"), "the embedded color brush is cached")
        Call Check(palette.GetEmbeddedBrush("not-a-color") IsNot Nothing, "an invalid html color does not throw")
        Call Check(palette.GetEmbeddedBrush(Nothing) Is Nothing, "an empty html color has no brush")
    End Sub

    Private Sub TestRenderModes(outDir As String)
        Console.WriteLine("render modes")

        Dim scene As New Scene()
        Dim cube As New Cube(1)
        Call scene.LoadSurfaces(cube.faces)

        Dim controller As New OrbitCameraController()
        Dim lighting As New SceneLighting()
        Dim renderer As New Direct2DSceneRenderer()
        Dim options As New SceneRenderOptions()

        Call scene.FitView(controller.Camera, New Size(320, 240))
        Call lighting.ApplyTo(controller.Camera)

        ' an empty scene must stay at the background color
        Using canvas As New DxGraphics(320, 240, Color.White)
            Dim empty As New Scene()
            Call renderer.Render(canvas, empty, controller.Camera, options)

            Dim blank As Bitmap = canvas.GetRasterImage()
            Call Check(NonBackgroundPixels(blank, Color.White) = 0, "an empty scene renders a plain background")
        End Using

        ' the surface mode shades and fills the model
        Using canvas As New DxGraphics(320, 240, Color.White)
            options.Mode = SceneRenderMode.Surface
            options.ShowGround = True
            Call renderer.Render(canvas, scene, controller.Camera, options)

            Dim image As Bitmap = canvas.GetRasterImage()
            Dim drawn As Integer = NonBackgroundPixels(image, Color.White)

            Call Check(drawn > 500, $"the surface mode draws the shaded model ({drawn} pixels)")
            Call image.Save(Path.Combine(outDir, "01-surface.png"), ImageFormats.Png)
            Call Check(File.Exists(Path.Combine(outDir, "01-surface.png")), "the surface frame can be exported as a png file")
        End Using

        ' the mesh mode outlines the faces
        Using canvas As New DxGraphics(320, 240, Color.White)
            options.Mode = SceneRenderMode.Mesh
            Call renderer.Render(canvas, scene, controller.Camera, options)

            Dim image As Bitmap = canvas.GetRasterImage()
            Dim drawn As Integer = NonBackgroundPixels(image, Color.White)

            Call Check(drawn > 100, $"the mesh mode draws the wire frame ({drawn} pixels)")
            Call image.Save(Path.Combine(outDir, "02-mesh.png"), ImageFormats.Png)
        End Using

        ' the point mode draws every vertex
        Using canvas As New DxGraphics(320, 240, Color.White)
            options.Mode = SceneRenderMode.PointCloud
            Call renderer.Render(canvas, scene, controller.Camera, options)

            Dim image As Bitmap = canvas.GetRasterImage()
            Dim drawn As Integer = NonBackgroundPixels(image, Color.White)

            Call Check(drawn > 50, $"the point mode draws the vertices ({drawn} pixels)")
            Call image.Save(Path.Combine(outDir, "03-points.png"), ImageFormats.Png)
        End Using

        ' the ground grid can be disabled and the background can be changed
        Using canvas As New DxGraphics(320, 240, Color.Black)
            options.Mode = SceneRenderMode.Surface
            options.ShowGround = False
            options.BackgroundColor = Color.Black
            Call renderer.Render(canvas, scene, controller.Camera, options)

            Dim image As Bitmap = canvas.GetRasterImage()
            Dim drawn As Integer = NonBackgroundPixels(image, Color.Black)

            Call Check(drawn > 500, $"a custom background still renders the model ({drawn} pixels)")
            Call image.Save(Path.Combine(outDir, "04-surface-black.png"), ImageFormats.Png)
        End Using

        ' the ground grid is drawn when it is enabled
        Dim withGround As Integer = 0
        Dim withoutGround As Integer = 0

        options.BackgroundColor = Color.White
        options.Mode = SceneRenderMode.Surface

        options.ShowGround = True
        Using canvas As New DxGraphics(320, 240, Color.White)
            Call renderer.Render(canvas, scene, controller.Camera, options)
            withGround = NonBackgroundPixels(canvas.GetRasterImage(), Color.White)
        End Using

        options.ShowGround = False
        Using canvas As New DxGraphics(320, 240, Color.White)
            Call renderer.Render(canvas, scene, controller.Camera, options)
            withoutGround = NonBackgroundPixels(canvas.GetRasterImage(), Color.White)
        End Using

        Call Check(withGround > withoutGround, "the ground grid adds pixels to the frame")
        options.ShowGround = True

        ' every heat map scheme must render without an error
        Dim rendered As Integer = 0

        For Each scheme As String In schemes
            options.ColorScheme = scheme
            options.Mode = SceneRenderMode.PointCloud

            Using canvas As New DxGraphics(64, 48, Color.White)
                Call renderer.Render(canvas, scene, controller.Camera, options)
                rendered += 1
            End Using
        Next

        Call Check(rendered = schemes.Length, $"every heat map scheme renders ({rendered})")
        Call Check(renderer.BrushCacheCount > 0 AndAlso renderer.BrushCacheCount <= Direct2DSceneRenderer.BrushCacheLimit,
                   "the shading brush cache is bounded")

        ' a broken back end must stay replaceable
        Dim backend As ISceneRenderBackend = renderer
        Call Check(backend.Name = "Direct2D" AndAlso Not String.IsNullOrEmpty(backend.Description),
                   "the default back end reports its name and description")
        Call Check(Direct2DSceneRenderer.Default IsNot Nothing, "a shared default back end is provided")
    End Sub

    Private Sub TestPointCloud(outDir As String)
        Console.WriteLine("point cloud")

        Dim scene As New Scene()
        Dim points(999) As PointCloudPoint

        For i As Integer = 0 To 999
            Dim t As Double = i / 999.0
            points(i) = New PointCloudPoint(
                std.Cos(t * 6.28) * 2,
                std.Sin(t * 6.28) * 2,
                t,
                i * 0.1,
                Nothing)
        Next

        Call scene.LoadPointCloud(points)

        Call Check(scene.PointCount = 1000, "one thousand points are loaded")
        Call Check(scene.SurfaceCount = 0, "a point cloud scene holds no faces")
        Call Check(scene.IntensityMax > scene.IntensityMin, "the intensity range is detected")
        Call Check(scene.HasData, "the point cloud scene reports data")

        Dim renderer As New Direct2DSceneRenderer()
        Dim controller As New OrbitCameraController()
        Dim options As New SceneRenderOptions With {
            .Mode = SceneRenderMode.PointCloud,
            .PointSize = 3
        }

        Call scene.FitView(controller.Camera, New Size(320, 240))

        Using canvas As New DxGraphics(320, 240, Color.White)
            Call renderer.Render(canvas, scene, controller.Camera, options)

            Dim image As Bitmap = canvas.GetRasterImage()
            Dim drawn As Integer = NonBackgroundPixels(image, Color.White)

            Call Check(drawn > 200, $"the point cloud is drawn ({drawn} pixels)")
            Call image.Save(Path.Combine(outDir, "05-pointcloud.png"), ImageFormats.Png)
        End Using

        ' the per point colors of the source file can be used instead of the heat map
        Dim colored(9) As PointCloudPoint

        For i As Integer = 0 To 9
            colored(i) = New PointCloudPoint(i - 5, 0, 0, 0, "#ff0000")
        Next

        Dim coloredScene As New Scene()
        Call coloredScene.LoadPointCloud(colored)

        options.UseEmbeddedColor = True
        options.ColorScheme = "viridis"

        Using canvas As New DxGraphics(64, 64, Color.White)
            Call renderer.Render(canvas, coloredScene, controller.Camera, options)
            Call renderer.Render(canvas, coloredScene, controller.Camera, options)

            Call Check(renderer.Palette.EmbeddedColorCount = 1, "the embedded point color is cached once")
        End Using

        Call Check(renderer.Palette.GetEmbeddedBrush("#ff0000") IsNot Nothing, "the embedded color brush is resolvable")
    End Sub
End Module
