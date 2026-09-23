Imports Microsoft.VisualBasic.Drawing.DirectX.Scene3D
Imports Microsoft.VisualBasic.Imaging.Landscape.Data
Imports Microsoft.VisualBasic.Imaging.Landscape.Ply
Imports DataSurface = Microsoft.VisualBasic.Imaging.Landscape.Data.Surface
Imports D3Surface = Microsoft.VisualBasic.Imaging.Drawing3D.Surface
Imports PlyPointCloud = Microsoft.VisualBasic.Imaging.Landscape.Ply.PointCloud

''' <summary>
''' Loads the 3d model files of the Landscape library into the neutral scene
''' data model of the directx scene pipeline.
''' </summary>
''' <remarks>
''' Every supported file format is mapped into one of the two scene data kinds:
''' a set of faces or a set of point cloud points. The viewer therefore does not
''' have to know anything about the file formats themselves.
''' </remarks>
Public Module ModelSceneLoader

    ''' <summary>
    ''' the file dialog filter of all of the supported model and point cloud files
    ''' </summary>
    Public Const FileDialogFilter As String =
        "3D 模型 (*.stl;*.obj;*.gltf;*.glb;*.dae;*.3ds;*.3mf)|*.stl;*.obj;*.gltf;*.glb;*.dae;*.3ds;*.3mf|" &
        "PLY 点云 (*.ply)|*.ply|" &
        "所有文件 (*.*)|*.*"

    ''' <summary>
    ''' is the given file a point cloud instead of a solid model?
    ''' </summary>
    Public Function IsPointCloudFile(filePath As String) As Boolean
        Return String.Equals(System.IO.Path.GetExtension(filePath), ".ply", StringComparison.OrdinalIgnoreCase)
    End Function

    ''' <summary>
    ''' load a solid model file (stl, obj, gltf, glb, dae, 3ds or 3mf) into a set
    ''' of faces, degenerate faces are dropped
    ''' </summary>
    Public Function LoadSurfaces(filePath As String) As D3Surface()
        Dim model As SceneModel = ModelLoader.LoadModel(filePath)
        Dim faces As New List(Of D3Surface)()

        If model IsNot Nothing AndAlso model.Surfaces IsNot Nothing Then
            For Each source As DataSurface In model.Surfaces
                Dim face As D3Surface = source.CreateObject()

                If face IsNot Nothing AndAlso face.vertices IsNot Nothing AndAlso face.vertices.Length >= 3 Then
                    faces.Add(face)
                End If
            Next
        End If

        Return faces.ToArray()
    End Function

    ''' <summary>
    ''' load a PLY point cloud file, the per point color and the scalar intensity
    ''' are preserved
    ''' </summary>
    Public Function LoadPointCloud(filePath As String) As PointCloudPoint()
        Dim cloud As PlyPointCloud() = PlyReader.ReadFile(filePath)

        If cloud Is Nothing OrElse cloud.Length = 0 Then
            Return New PointCloudPoint() {}
        End If

        Dim points(cloud.Length - 1) As PointCloudPoint

        For i As Integer = 0 To cloud.Length - 1
            points(i) = New PointCloudPoint(
                cloud(i).x,
                cloud(i).y,
                cloud(i).z,
                cloud(i).intensity,
                cloud(i).color)
        Next

        Return points
    End Function
End Module
