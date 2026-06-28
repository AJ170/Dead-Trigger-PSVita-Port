using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

public static class MeshOBJExporter
{
    [MenuItem("Assets/Export Mesh to OBJ", false, 0)]
    static void ExportSelectedMeshes()
    {
        // Get scene paths for output folder
        string scenePath = UnityEngine.SceneManagement
            .SceneManager.GetActiveScene().path;
        string sceneDir = Path.GetDirectoryName(scenePath)
            .Replace("\\", "/");
        string sceneName = Path.GetFileNameWithoutExtension(scenePath);
        string objFolder = Path.Combine(sceneDir, sceneName + "_OBJExport")
            .Replace("\\", "/");

        if (!Directory.Exists(objFolder))
            Directory.CreateDirectory(objFolder);

        Debug.Log("MeshOBJExporter: Exporting to " + objFolder);

        int exportCount = 0;
        int skipCount = 0;

        foreach (Object obj in Selection.objects)
        {
            string assetPath = AssetDatabase.GetAssetPath(obj)
                .Replace("\\", "/");
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);

            if (mesh == null)
            {
                Debug.LogWarning("MeshOBJExporter: No mesh found at "
                    + assetPath + ", skipping.");
                skipCount++;
                continue;
            }

            string objPath = Path.Combine(objFolder, mesh.name + ".obj")
                .Replace("\\", "/");

            // Don't overwrite existing exports
            if (File.Exists(objPath))
            {
                Debug.Log("MeshOBJExporter: " + mesh.name
                    + " already exported, skipping.");
                skipCount++;
                continue;
            }

            string result = ExportMeshToOBJ(mesh);

            if (string.IsNullOrEmpty(result))
            {
                Debug.LogWarning("MeshOBJExporter: Failed to export "
                    + mesh.name);
                skipCount++;
                continue;
            }

            File.WriteAllText(objPath, result);
            exportCount++;
            Debug.Log("MeshOBJExporter: Exported " + mesh.name
                + " to " + objPath);
        }

        Debug.Log("MeshOBJExporter: Complete."
            + "\n  Exported: " + exportCount
            + "\n  Skipped: " + skipCount);
    }

    [MenuItem("Assets/Export Mesh to OBJ", true)]
    static bool ExportSelectedMeshesValidate()
    {
        foreach (Object obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null)
                return true;
        }
        return false;
    }

    [MenuItem("Assets/Export All Meshes in Folder to OBJ", false, 0)]
    [MenuItem("Assets/Export All Meshes in Folder to OBJ", false, 0)]
    static void ExportAllMeshesInFolder()
    {
        string scenePath = UnityEngine.SceneManagement
            .SceneManager.GetActiveScene().path;
        string sceneDir = Path.GetDirectoryName(scenePath)
            .Replace("\\", "/");
        string sceneName = Path.GetFileNameWithoutExtension(scenePath);
        string meshFolder = Path.Combine(sceneDir, sceneName + "_SplitMeshes")
            .Replace("\\", "/");
        string objFolder = Path.Combine(sceneDir, sceneName + "_OBJExport")
            .Replace("\\", "/");

        if (!Directory.Exists(meshFolder))
        {
            Debug.LogWarning("MeshOBJExporter: Mesh folder not found at "
                + meshFolder);
            return;
        }

        if (!Directory.Exists(objFolder))
            Directory.CreateDirectory(objFolder);

        Debug.Log("MeshOBJExporter: Reading meshes from " + meshFolder);
        Debug.Log("MeshOBJExporter: Exporting to " + objFolder);

        string[] assetPaths = Directory.GetFiles(meshFolder, "*.asset");
        int total = assetPaths.Length;
        int exportCount = 0;
        int skipCount = 0;

        try
        {
            for (int i = 0; i < total; i++)
            {
                string unityPath = assetPaths[i].Replace("\\", "/");
                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(unityPath);

                // Update progress bar
                string progressTitle = "Exporting Meshes to OBJ";
                string progressInfo = mesh != null
                    ? "Exporting: " + mesh.name
                        + " (" + (i + 1) + " of " + total + ")"
                    : "Loading: " + Path.GetFileName(unityPath)
                        + " (" + (i + 1) + " of " + total + ")";

                float progress = (float)i / total;

                // Returns true if the user clicks cancel
                if (EditorUtility.DisplayCancelableProgressBar(
                    progressTitle, progressInfo, progress))
                {
                    Debug.Log("MeshOBJExporter: Export cancelled by user at "
                        + (i + 1) + " of " + total + "."
                        + "\n  Exported so far: " + exportCount
                        + "\n  Skipped so far: " + skipCount);
                    break;
                }

                if (mesh == null)
                {
                    Debug.LogWarning("MeshOBJExporter: No mesh at "
                        + unityPath + ", skipping.");
                    skipCount++;
                    continue;
                }

                string objPath = Path.Combine(objFolder, mesh.name + ".obj")
                    .Replace("\\", "/");

                if (File.Exists(objPath))
                {
                    skipCount++;
                    continue;
                }

                string result = ExportMeshToOBJ(mesh);

                if (string.IsNullOrEmpty(result))
                {
                    Debug.LogWarning("MeshOBJExporter: Failed to export "
                        + mesh.name);
                    skipCount++;
                    continue;
                }

                File.WriteAllText(objPath, result);
                exportCount++;
            }
        }
        finally
        {
            // Always clear the progress bar even if an exception occurs
            EditorUtility.ClearProgressBar();
        }

        Debug.Log("MeshOBJExporter: Complete."
            + "\n  Exported: " + exportCount
            + "\n  Skipped: " + skipCount
            + "\n  Output: " + objFolder);
    }
    [MenuItem("Assets/Export All Meshes in Folder to OBJ", true)]
    static bool ExportAllMeshesInFolderValidate()
    {
        return true;
    }

    static string ExportMeshToOBJ(Mesh mesh)
    {
        if (mesh == null) return null;

        StringBuilder sb = new StringBuilder();

        sb.AppendLine("# Exported from Unity by MeshOBJExporter");
        sb.AppendLine("# Mesh: " + mesh.name);
        sb.AppendLine("# Vertices: " + mesh.vertexCount);
        sb.AppendLine("# Triangles: " + (mesh.triangles.Length / 3));
        sb.AppendLine();

        sb.AppendLine("g " + mesh.name);
        sb.AppendLine();

        // Vertices
        foreach (Vector3 v in mesh.vertices)
            sb.AppendLine("v " + (-v.x).ToString("F6")
                + " " + v.y.ToString("F6")
                + " " + v.z.ToString("F6"));

        sb.AppendLine();

        // UV channel 0
        if (mesh.uv != null && mesh.uv.Length > 0)
        {
            foreach (Vector2 uv in mesh.uv)
                sb.AppendLine("vt " + uv.x.ToString("F6")
                    + " " + uv.y.ToString("F6"));
            sb.AppendLine();
        }

        // Normals
        if (mesh.normals != null && mesh.normals.Length > 0)
        {
            foreach (Vector3 n in mesh.normals)
                sb.AppendLine("vn " + (-n.x).ToString("F6")
                    + " " + n.y.ToString("F6")
                    + " " + n.z.ToString("F6"));
            sb.AppendLine();
        }

        // Triangles — one submesh per material group
        bool hasUV = mesh.uv != null && mesh.uv.Length > 0;
        bool hasNormals = mesh.normals != null && mesh.normals.Length > 0;

        for (int s = 0; s < mesh.subMeshCount; s++)
        {
            sb.AppendLine("usemtl material_" + s);
            sb.AppendLine("s off");

            int[] triangles = mesh.GetTriangles(s);

            // OBJ is 1-indexed
            for (int i = 0; i < triangles.Length; i += 3)
            {
                // Unity is left-handed, OBJ is right-handed
                // so we reverse winding order
                int a = triangles[i] + 1;
                int b = triangles[i + 1] + 1;
                int c = triangles[i + 2] + 1;

                if (hasUV && hasNormals)
                    sb.AppendLine("f "
                        + a + "/" + a + "/" + a + " "
                        + c + "/" + c + "/" + c + " "
                        + b + "/" + b + "/" + b);
                else if (hasUV)
                    sb.AppendLine("f "
                        + a + "/" + a + " "
                        + c + "/" + c + " "
                        + b + "/" + b);
                else if (hasNormals)
                    sb.AppendLine("f "
                        + a + "//" + a + " "
                        + c + "//" + c + " "
                        + b + "//" + b);
                else
                    sb.AppendLine("f " + a + " " + c + " " + b);
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}