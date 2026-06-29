using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;
using System.Text;

public static class SceneWideMeshRemapper
{
    [MenuItem("Tools/Step 1 - Extract Meshes and Export OBJs")]
    static void ExtractMeshesAndExportOBJs()
    {
        string assetPath = EditorUtility.OpenFilePanel(
            "Select Combined Mesh Asset To Remap",
            "Assets",
            "asset");

        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogWarning("SceneWideMeshRemapper: No asset selected.");
            return;
        }

        assetPath = assetPath.Replace("\\", "/");
        string projectPath = Application.dataPath.Replace("/Assets", "/");
        assetPath = assetPath.Replace(projectPath, "");

        Mesh combinedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
        if (combinedMesh == null)
        {
            Debug.LogWarning("SceneWideMeshRemapper: No mesh found at "
                + assetPath);
            return;
        }

        Debug.Log("SceneWideMeshRemapper: Processing references to "
            + combinedMesh.name);

        string scenePath = UnityEngine.SceneManagement
            .SceneManager.GetActiveScene().path;
        string sceneDir = Path.GetDirectoryName(scenePath)
            .Replace("\\", "/");
        string sceneName = Path.GetFileNameWithoutExtension(scenePath);
        string meshFolder = Path.Combine(sceneDir, sceneName + "_SplitMeshes")
            .Replace("\\", "/");
        string prefabFolder = Path.Combine(sceneDir, sceneName + "_Prefabs")
            .Replace("\\", "/");
        string objFolder = Path.Combine(sceneDir, sceneName + "_OBJExport")
            .Replace("\\", "/");

        if (!Directory.Exists(meshFolder))
            Directory.CreateDirectory(meshFolder);
        if (!Directory.Exists(prefabFolder))
            Directory.CreateDirectory(prefabFolder);
        if (!Directory.Exists(objFolder))
            Directory.CreateDirectory(objFolder);

        // Cache full mesh data once
        Vector3[] allVertices = combinedMesh.vertices;
        Vector3[] allNormals = combinedMesh.normals;
        Vector4[] allTangents = combinedMesh.tangents;
        Color[] allColors = combinedMesh.colors;
        Vector2[] allUV0 = combinedMesh.uv;
        Vector2[] allUV1 = combinedMesh.uv2;
        Vector2[] allUV2 = combinedMesh.uv3;
        Vector2[] allUV3 = combinedMesh.uv4;
        BoneWeight[] allBoneWeights = combinedMesh.boneWeights;
        Matrix4x4[] allBindposes = combinedMesh.bindposes;

        // Pre-calculate all submesh bounds and cache triangles
        Bounds[] submeshBounds = new Bounds[combinedMesh.subMeshCount];
        int[][] submeshTriangles = new int[combinedMesh.subMeshCount][];

        for (int s = 0; s < combinedMesh.subMeshCount; s++)
        {
            int[] triangles = combinedMesh.GetTriangles(s);
            submeshTriangles[s] = triangles;

            if (triangles.Length == 0) continue;

            Vector3 min = allVertices[triangles[0]];
            Vector3 max = allVertices[triangles[0]];

            for (int t = 1; t < triangles.Length; t++)
            {
                Vector3 v = allVertices[triangles[t]];
                min = Vector3.Min(min, v);
                max = Vector3.Max(max, v);
            }

            submeshBounds[s] = new Bounds(
                (min + max) * 0.5f,
                max - min
            );
        }

        // Find all MeshFilters referencing the combined mesh
        MeshFilter[] allFilters = Object.FindObjectsOfType<MeshFilter>();
        List<MeshFilter> targetFilters = new List<MeshFilter>();

        foreach (MeshFilter mf in allFilters)
        {
            if (mf.sharedMesh == combinedMesh)
                targetFilters.Add(mf);
        }

        Debug.Log("SceneWideMeshRemapper: Found " + targetFilters.Count
            + " references to process.");

        if (targetFilters.Count == 0)
        {
            Debug.Log("SceneWideMeshRemapper: Nothing to process.");
            return;
        }

        Dictionary<string, GameObject> prefabCache =
            new Dictionary<string, GameObject>();

        int newMeshCount = 0;
        int newPrefabCount = 0;
        int reuseCount = 0;
        int noMatchCount = 0;
        int skipCount = 0;
        int objExportCount = 0;
        float matchThreshold = 0.5f;

        try
        {
            int total = targetFilters.Count;
            int current = 0;

            foreach (MeshFilter mf in targetFilters)
            {
                current++;

                if (mf == null || mf.gameObject == null)
                {
                    skipCount++;
                    continue;
                }

                string objectName = mf.gameObject.name;
                Transform objectTransform = mf.transform;
                MeshRenderer mr = mf.GetComponent<MeshRenderer>();
                Material mat = mr != null ? mr.sharedMaterial : null;

                if (EditorUtility.DisplayCancelableProgressBar(
                    "Step 1 - Extracting Meshes and Exporting OBJs",
                    "Processing: " + objectName
                        + " (" + current + " of " + total + ")",
                    (float)current / total))
                {
                    Debug.Log("SceneWideMeshRemapper: Cancelled at "
                        + current + " of " + total);
                    break;
                }

                string meshAssetPath = Path.Combine(
                    meshFolder, objectName + ".asset")
                    .Replace("\\", "/");
                string prefabAssetPath = Path.Combine(
                    prefabFolder, objectName + ".prefab")
                    .Replace("\\", "/");
                string objPath = Path.Combine(
                    objFolder, objectName + ".obj")
                    .Replace("\\", "/");

                GameObject prefabAsset = null;

                // Check if prefab already exists
                if (File.Exists(prefabAssetPath))
                {
                    prefabAsset = AssetDatabase
                        .LoadAssetAtPath<GameObject>(prefabAssetPath);

                    if (!prefabCache.ContainsKey(objectName))
                        prefabCache[objectName] = prefabAsset;

                    reuseCount++;
                }
                else if (prefabCache.ContainsKey(objectName))
                {
                    prefabAsset = prefabCache[objectName];
                    reuseCount++;
                }
                else
                {
                    Mesh newMesh = null;

                    if (File.Exists(meshAssetPath))
                    {
                        newMesh = AssetDatabase
                            .LoadAssetAtPath<Mesh>(meshAssetPath);
                        Debug.Log("SceneWideMeshRemapper: Reusing mesh: "
                            + objectName);
                    }
                    else
                    {
                        // Find closest submesh by bounds center
                        Vector3 objectCenter = mr != null
                            ? mr.bounds.center
                            : objectTransform.position;

                        float closestDistance = float.MaxValue;
                        int closestSubmesh = -1;

                        for (int s = 0; s < combinedMesh.subMeshCount; s++)
                        {
                            if (submeshTriangles[s].Length == 0) continue;

                            float distance = Vector3.Distance(
                                objectCenter, submeshBounds[s].center);

                            if (distance < closestDistance)
                            {
                                closestDistance = distance;
                                closestSubmesh = s;
                            }
                        }

                        if (closestSubmesh < 0
                            || closestDistance > matchThreshold)
                        {
                            Debug.LogWarning(
                                "SceneWideMeshRemapper: No submesh match for "
                                + objectName
                                + " (closest: " + closestDistance + ")",
                                mf.gameObject);
                            noMatchCount++;
                            continue;
                        }

                        int[] triangles = submeshTriangles[closestSubmesh];

                        HashSet<int> usedIndices = new HashSet<int>();
                        foreach (int idx in triangles)
                            usedIndices.Add(idx);

                        Dictionary<int, int> indexRemap =
                            new Dictionary<int, int>();
                        List<int> orderedIndices = new List<int>(usedIndices);
                        orderedIndices.Sort();

                        for (int i = 0; i < orderedIndices.Count; i++)
                            indexRemap[orderedIndices[i]] = i;

                        int vertCount = orderedIndices.Count;

                        Vector3[] newVertices = new Vector3[vertCount];
                        Vector3[] newNormals = allNormals.Length > 0 ?
                            new Vector3[vertCount] : null;
                        Vector4[] newTangents = allTangents.Length > 0 ?
                            new Vector4[vertCount] : null;
                        Color[] newColors = allColors.Length > 0 ?
                            new Color[vertCount] : null;
                        Vector2[] newUV0 = allUV0.Length > 0 ?
                            new Vector2[vertCount] : null;
                        Vector2[] newUV1 = allUV1.Length > 0 ?
                            new Vector2[vertCount] : null;
                        Vector2[] newUV2 = allUV2.Length > 0 ?
                            new Vector2[vertCount] : null;
                        Vector2[] newUV3 = allUV3.Length > 0 ?
                            new Vector2[vertCount] : null;
                        BoneWeight[] newBoneWeights =
                            allBoneWeights.Length > 0 ?
                            new BoneWeight[vertCount] : null;

                        for (int i = 0; i < orderedIndices.Count; i++)
                        {
                            int oldIdx = orderedIndices[i];

                            newVertices[i] = objectTransform
                                .InverseTransformPoint(allVertices[oldIdx]);

                            if (newNormals != null)
                                newNormals[i] = objectTransform
                                    .InverseTransformDirection(
                                        allNormals[oldIdx]);

                            if (newTangents != null)
                            {
                                Vector3 tangentXYZ = objectTransform
                                    .InverseTransformDirection(
                                        new Vector3(
                                            allTangents[oldIdx].x,
                                            allTangents[oldIdx].y,
                                            allTangents[oldIdx].z));
                                newTangents[i] = new Vector4(
                                    tangentXYZ.x,
                                    tangentXYZ.y,
                                    tangentXYZ.z,
                                    allTangents[oldIdx].w);
                            }

                            if (newColors != null)
                                newColors[i] = allColors[oldIdx];
                            if (newUV0 != null)
                                newUV0[i] = allUV0[oldIdx];
                            if (newUV1 != null)
                                newUV1[i] = allUV1[oldIdx];
                            if (newUV2 != null)
                                newUV2[i] = allUV2[oldIdx];
                            if (newUV3 != null)
                                newUV3[i] = allUV3[oldIdx];
                            if (newBoneWeights != null)
                                newBoneWeights[i] = allBoneWeights[oldIdx];
                        }

                        int[] newTriangles = new int[triangles.Length];
                        for (int i = 0; i < triangles.Length; i++)
                            newTriangles[i] = indexRemap[triangles[i]];

                        newMesh = new Mesh();
                        newMesh.name = objectName;
                        newMesh.vertices = newVertices;
                        if (newNormals != null) newMesh.normals = newNormals;
                        if (newTangents != null) newMesh.tangents = newTangents;
                        if (newColors != null) newMesh.colors = newColors;
                        if (newUV0 != null) newMesh.uv = newUV0;
                        if (newUV1 != null) newMesh.uv2 = newUV1;
                        if (newUV2 != null) newMesh.uv3 = newUV2;
                        if (newUV3 != null) newMesh.uv4 = newUV3;
                        if (newBoneWeights != null)
                            newMesh.boneWeights = newBoneWeights;
                        if (allBindposes.Length > 0)
                            newMesh.bindposes = allBindposes;
                        newMesh.triangles = newTriangles;
                        newMesh.RecalculateBounds();

                        AssetDatabase.CreateAsset(newMesh, meshAssetPath);
                        newMeshCount++;
                    }

                    if (newMesh == null)
                    {
                        Debug.LogWarning(
                            "SceneWideMeshRemapper: Failed to get mesh for "
                            + objectName + ", skipping.",
                            mf.gameObject);
                        skipCount++;
                        continue;
                    }

                    // Export OBJ if not already exported
                    Debug.Log("SceneWideMeshRemapper: Attempting OBJ export for "
                        + objectName
                        + "\n  OBJ path: " + objPath
                        + "\n  File exists already: " + File.Exists(objPath)
                        + "\n  Mesh null: " + (newMesh == null)
                        + "\n  Vertex count: " + (newMesh != null ? newMesh.vertexCount.ToString() : "N/A"));

                    if (!File.Exists(objPath))
                    {
                        string objContent = ExportMeshToOBJ(newMesh);

                        Debug.Log("SceneWideMeshRemapper: OBJ content for " + objectName
                            + "\n  Content null: " + (objContent == null)
                            + "\n  Content empty: " + string.IsNullOrEmpty(objContent)
                            + "\n  Content length: " + (objContent != null ? objContent.Length.ToString() : "N/A"));

                        if (!string.IsNullOrEmpty(objContent))
                        {
                            try
                            {
                                File.WriteAllText(objPath, objContent);
                                Debug.Log("SceneWideMeshRemapper: File written successfully to "
                                    + objPath
                                    + "\n  File exists after write: " + File.Exists(objPath));
                                objExportCount++;
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogError("SceneWideMeshRemapper: File write failed for "
                                    + objectName
                                    + "\n  Error: " + e.Message
                                    + "\n  Path: " + objPath);
                            }
                        }
                    }
                    // Create prefab using asset mesh for now
                    // Step 2 will swap to OBJ mesh
                    GameObject tempGO = new GameObject(objectName);
                    MeshFilter tempMF = tempGO.AddComponent<MeshFilter>();
                    tempMF.sharedMesh = newMesh;

                    MeshRenderer tempMR = tempGO.AddComponent<MeshRenderer>();
                    if (mat != null)
                        tempMR.sharedMaterial = mat;

                    GameObjectUtility.SetStaticEditorFlags(tempGO,
                        StaticEditorFlags.BatchingStatic |
                        StaticEditorFlags.OccluderStatic |
                        StaticEditorFlags.OccludeeStatic |
                        StaticEditorFlags.NavigationStatic |
                        StaticEditorFlags.OffMeshLinkGeneration |
                        StaticEditorFlags.ReflectionProbeStatic);

                    prefabAsset = PrefabUtility.CreatePrefab(
                        prefabAssetPath,
                        tempGO,
                        ReplacePrefabOptions.Default);

                    Object.DestroyImmediate(tempGO);

                    if (prefabAsset == null)
                    {
                        Debug.LogWarning(
                            "SceneWideMeshRemapper: Failed to create prefab "
                            + "for " + objectName,
                            mf.gameObject);
                        skipCount++;
                        continue;
                    }

                    prefabCache[objectName] = prefabAsset;
                    newPrefabCount++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        // Full refresh at the end so Unity imports all OBJs at once
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("SceneWideMeshRemapper Step 1 Complete."
            + "\n  New meshes: " + newMeshCount
            + "\n  OBJs exported: " + objExportCount
            + "\n  New prefabs: " + newPrefabCount
            + "\n  Reused: " + reuseCount
            + "\n  No match: " + noMatchCount
            + "\n  Skipped: " + skipCount
            + "\n  Total processed: " + targetFilters.Count
            + "\n  Now let Unity finish importing, then run Step 2.");
    }

    [MenuItem("Tools/Step 2 - Remap Scene Objects To OBJ Meshes")]
    static void RemapSceneObjectsToOBJMeshes()
    {
        string scenePath = UnityEngine.SceneManagement
            .SceneManager.GetActiveScene().path;
        string sceneDir = Path.GetDirectoryName(scenePath)
            .Replace("\\", "/");
        string sceneName = Path.GetFileNameWithoutExtension(scenePath);
        string prefabFolder = Path.Combine(sceneDir, sceneName + "_Prefabs")
            .Replace("\\", "/");
        string objFolder = Path.Combine(sceneDir, sceneName + "_OBJExport")
            .Replace("\\", "/");

        if (!Directory.Exists(objFolder))
        {
            Debug.LogWarning("SceneWideMeshRemapper: OBJ folder not found at "
                + objFolder
                + ". Run Step 1 first.");
            return;
        }

        if (!Directory.Exists(prefabFolder))
        {
            Debug.LogWarning(
                "SceneWideMeshRemapper: Prefab folder not found at "
                + prefabFolder
                + ". Run Step 1 first.");
            return;
        }

        // Cache all imported OBJ meshes by filename
        Dictionary<string, Mesh> objMeshCache =
            new Dictionary<string, Mesh>();

        string[] objPaths = Directory.GetFiles(objFolder, "*.obj");
        foreach (string path in objPaths)
        {
            string unityPath = path.Replace("\\", "/");
            string fileName = Path.GetFileNameWithoutExtension(unityPath);

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(unityPath);
            foreach (Object asset in assets)
            {
                Mesh mesh = asset as Mesh;
                if (mesh != null && !objMeshCache.ContainsKey(fileName))
                {
                    objMeshCache[fileName] = mesh;
                    break;
                }
            }
        }

        Debug.Log("SceneWideMeshRemapper: Loaded " + objMeshCache.Count
            + " imported OBJ meshes from " + objFolder);

        if (objMeshCache.Count == 0)
        {
            Debug.LogWarning(
                "SceneWideMeshRemapper: No imported OBJ meshes found. "
                + "Make sure Unity has finished importing them after Step 1.");
            return;
        }

        // Find all MeshFilters in scene that have a mesh
        // whose name matches an OBJ we exported
        MeshFilter[] allFilters = Object.FindObjectsOfType<MeshFilter>();
        List<MeshFilter> targetFilters = new List<MeshFilter>();

        foreach (MeshFilter mf in allFilters)
        {
            if (mf.sharedMesh == null) continue;
            if (objMeshCache.ContainsKey(mf.gameObject.name))
                targetFilters.Add(mf);
        }

        Debug.Log("SceneWideMeshRemapper: Found " + targetFilters.Count
            + " scene objects to remap to OBJ meshes.");

        if (targetFilters.Count == 0)
        {
            Debug.LogWarning(
                "SceneWideMeshRemapper: No matching scene objects found. "
                + "Make sure Step 1 has been run and OBJs are imported.");
            return;
        }

        int remappedCount = 0;
        int skippedCount = 0;
        int noMatchCount = 0;

        try
        {
            int total = targetFilters.Count;
            int current = 0;

            foreach (MeshFilter mf in targetFilters)
            {
                current++;

                if (mf == null || mf.gameObject == null)
                {
                    skippedCount++;
                    continue;
                }

                string objectName = mf.gameObject.name;

                if (EditorUtility.DisplayCancelableProgressBar(
                    "Step 2 - Remapping To OBJ Meshes",
                    "Processing: " + objectName
                        + " (" + current + " of " + total + ")",
                    (float)current / total))
                {
                    Debug.Log("SceneWideMeshRemapper: Cancelled at "
                        + current + " of " + total);
                    break;
                }

                // Find matching OBJ mesh by object name
                Mesh objMesh = null;
                if (!objMeshCache.TryGetValue(objectName, out objMesh))
                {
                    Debug.LogWarning(
                        "SceneWideMeshRemapper: No OBJ mesh found for "
                        + objectName);
                    noMatchCount++;
                    continue;
                }

                // Skip if already pointing at OBJ mesh
                if (mf.sharedMesh == objMesh)
                {
                    skippedCount++;
                    continue;
                }

                // Swap mesh reference on scene object
                Undo.RecordObject(mf, "Remap To OBJ Mesh");
                mf.sharedMesh = objMesh;
                EditorUtility.SetDirty(mf.gameObject);

                // Also update the prefab if it exists
                string prefabPath = Path.Combine(
                    prefabFolder, objectName + ".prefab")
                    .Replace("\\", "/");

                if (File.Exists(prefabPath))
                {
                    GameObject prefab = AssetDatabase
                        .LoadAssetAtPath<GameObject>(prefabPath);

                    if (prefab != null)
                    {
                        MeshFilter prefabMF =
                            prefab.GetComponent<MeshFilter>();

                        if (prefabMF != null && prefabMF.sharedMesh != objMesh)
                        {
                            prefabMF.sharedMesh = objMesh;
                            EditorUtility.SetDirty(prefab);

                            PrefabUtility.ReplacePrefab(
                                prefab,
                                prefab,
                                ReplacePrefabOptions.Default);
                        }
                    }
                }

                remappedCount++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("SceneWideMeshRemapper Step 2 Complete."
            + "\n  Remapped: " + remappedCount
            + "\n  No match: " + noMatchCount
            + "\n  Skipped: " + skippedCount
            + "\n  Total processed: " + targetFilters.Count);
    }

    static string ExportMeshToOBJ(Mesh mesh)
    {
        if (mesh == null) return null;

        StringBuilder sb = new StringBuilder();

        sb.AppendLine("# Exported from Unity by SceneWideMeshRemapper");
        sb.AppendLine("# Mesh: " + mesh.name);
        sb.AppendLine("# Vertices: " + mesh.vertexCount);
        sb.AppendLine("# Triangles: " + (mesh.triangles.Length / 3));
        sb.AppendLine();
        sb.AppendLine("g " + mesh.name);
        sb.AppendLine();

        foreach (Vector3 v in mesh.vertices)
            sb.AppendLine("v " + (-v.x).ToString("F6")
                + " " + v.y.ToString("F6")
                + " " + v.z.ToString("F6"));

        sb.AppendLine();

        if (mesh.uv != null && mesh.uv.Length > 0)
        {
            foreach (Vector2 uv in mesh.uv)
                sb.AppendLine("vt " + uv.x.ToString("F6")
                    + " " + uv.y.ToString("F6"));
            sb.AppendLine();
        }

        if (mesh.normals != null && mesh.normals.Length > 0)
        {
            foreach (Vector3 n in mesh.normals)
                sb.AppendLine("vn " + (-n.x).ToString("F6")
                    + " " + n.y.ToString("F6")
                    + " " + n.z.ToString("F6"));
            sb.AppendLine();
        }

        bool hasUV = mesh.uv != null && mesh.uv.Length > 0;
        bool hasNormals = mesh.normals != null && mesh.normals.Length > 0;

        for (int s = 0; s < mesh.subMeshCount; s++)
        {
            sb.AppendLine("usemtl material_" + s);
            sb.AppendLine("s off");

            int[] triangles = mesh.GetTriangles(s);

            for (int i = 0; i < triangles.Length; i += 3)
            {
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