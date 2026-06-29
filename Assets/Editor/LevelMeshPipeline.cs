using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;
using System.Text;

public static class LevelMeshPipeline
{
    [MenuItem("GameObject/Level Mesh Pipeline/Step 1 - Build Level Prefabs", false, 0)]
    static void BuildLevelPrefabs()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogWarning("LevelMeshPipeline: No GameObject selected.");
            return;
        }

        // Find combined mesh from children
        Mesh combinedMesh = null;
        MeshFilter combinedMeshFilter = null;
        foreach (MeshFilter f in selected.GetComponentsInChildren<MeshFilter>(true))
        {
            if (f.sharedMesh != null && f.sharedMesh.subMeshCount > 1)
            {
                combinedMesh = f.sharedMesh;
                combinedMeshFilter = f;
                break;
            }
        }

        if (combinedMesh == null)
        {
            Debug.LogWarning("LevelMeshPipeline: No combined mesh found under "
                + selected.name);
            return;
        }

        Debug.Log("LevelMeshPipeline: Found combined mesh: "
            + combinedMesh.name
            + " with " + combinedMesh.subMeshCount + " submeshes.");

        // Set up output folders
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

        Debug.Log("LevelMeshPipeline: Mesh folder: " + meshFolder);
        Debug.Log("LevelMeshPipeline: Prefab folder: " + prefabFolder);
        Debug.Log("LevelMeshPipeline: OBJ folder: " + objFolder);

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

        // Get all child renderers
        MeshRenderer[] childRenderers =
            selected.GetComponentsInChildren<MeshRenderer>(true);

        // Find or create rebuilt parent
        string rebuiltName = selected.name + "_Rebuilt";
        GameObject rebuiltParent = GameObject.Find(rebuiltName);
        if (rebuiltParent != null)
        {
            // Add numbered suffix if already exists
            int suffix = 2;
            while (GameObject.Find(rebuiltName + "_" + suffix) != null)
                suffix++;
            rebuiltName = rebuiltName + "_" + suffix;
        }

        rebuiltParent = new GameObject(rebuiltName);
        rebuiltParent.transform.position = Vector3.zero;
        rebuiltParent.transform.rotation = Quaternion.identity;
        rebuiltParent.transform.localScale = Vector3.one;
        Undo.RegisterCreatedObjectUndo(rebuiltParent, "Build Level Prefabs");

        // Track prefabs created in this run
        Dictionary<string, GameObject> prefabCache =
            new Dictionary<string, GameObject>();

        int newMeshCount = 0;
        int newPrefabCount = 0;
        int objExportCount = 0;
        int reuseCount = 0;
        int noMatchCount = 0;
        int skipCount = 0;
        float matchThreshold = 0.5f;

        try
        {
            int total = childRenderers.Length;
            int current = 0;

            foreach (MeshRenderer childMR in childRenderers)
            {
                current++;

                if (childMR == null || childMR.gameObject == null)
                {
                    skipCount++;
                    continue;
                }

                string childName = childMR.gameObject.name;
                Transform childTransform = childMR.transform;
                Material mat = childMR.sharedMaterial;

                if (EditorUtility.DisplayCancelableProgressBar(
                    "Step 1 - Building Level Prefabs",
                    "Processing: " + childName
                        + " (" + current + " of " + total + ")",
                    (float)current / total))
                {
                    Debug.Log("LevelMeshPipeline: Cancelled at "
                        + current + " of " + total);
                    break;
                }

                string meshAssetPath = Path.Combine(
                    meshFolder, childName + ".asset")
                    .Replace("\\", "/");
                string prefabAssetPath = Path.Combine(
                    prefabFolder, childName + ".prefab")
                    .Replace("\\", "/");
                string objPath = Path.Combine(
                    objFolder, childName + ".obj")
                    .Replace("\\", "/");

                GameObject prefabAsset = null;

                // Check if prefab already exists on disk
                if (File.Exists(prefabAssetPath))
                {
                    prefabAsset = AssetDatabase
                        .LoadAssetAtPath<GameObject>(prefabAssetPath);

                    if (!prefabCache.ContainsKey(childName))
                        prefabCache[childName] = prefabAsset;

                    // Export OBJ if missing even when prefab exists
                    if (!File.Exists(objPath))
                    {
                        Mesh existingMesh = null;
                        if (File.Exists(meshAssetPath))
                            existingMesh = AssetDatabase
                                .LoadAssetAtPath<Mesh>(meshAssetPath);
                        else if (prefabAsset != null)
                        {
                            MeshFilter pMF = prefabAsset
                                .GetComponent<MeshFilter>();
                            if (pMF != null)
                                existingMesh = pMF.sharedMesh;
                        }

                        if (existingMesh != null)
                        {
                            string objContent = ExportMeshToOBJ(existingMesh);
                            if (!string.IsNullOrEmpty(objContent))
                            {
                                try
                                {
                                    File.WriteAllText(objPath, objContent);
                                    objExportCount++;
                                    Debug.Log(
                                        "LevelMeshPipeline: Exported missing "
                                        + "OBJ for " + childName);
                                }
                                catch (System.Exception e)
                                {
                                    Debug.LogError(
                                        "LevelMeshPipeline: OBJ write failed "
                                        + "for " + childName
                                        + "\n  Error: " + e.Message);
                                }
                            }
                        }
                    }

                    reuseCount++;
                }
                else if (prefabCache.ContainsKey(childName))
                {
                    prefabAsset = prefabCache[childName];
                    reuseCount++;
                }
                else
                {
                    // Extract mesh
                    Mesh newMesh = null;

                    if (File.Exists(meshAssetPath))
                    {
                        newMesh = AssetDatabase
                            .LoadAssetAtPath<Mesh>(meshAssetPath);
                        Debug.Log("LevelMeshPipeline: Reusing mesh: "
                            + childName);
                    }
                    else
                    {
                        // Find closest submesh by bounds center
                        Vector3 childCenter = childMR.bounds.center;
                        float closestDistance = float.MaxValue;
                        int closestSubmesh = -1;

                        for (int s = 0; s < combinedMesh.subMeshCount; s++)
                        {
                            if (submeshTriangles[s].Length == 0) continue;

                            float distance = Vector3.Distance(
                                childCenter, submeshBounds[s].center);

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
                                "LevelMeshPipeline: No submesh match for "
                                + childName
                                + " (closest: " + closestDistance + ")",
                                childMR.gameObject);
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

                            newVertices[i] = childTransform
                                .InverseTransformPoint(allVertices[oldIdx]);

                            if (newNormals != null)
                                newNormals[i] = childTransform
                                    .InverseTransformDirection(
                                        allNormals[oldIdx]);

                            if (newTangents != null)
                            {
                                Vector3 tangentXYZ = childTransform
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
                        newMesh.name = childName;
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
                            "LevelMeshPipeline: Failed to get mesh for "
                            + childName + ", skipping.",
                            childMR.gameObject);
                        skipCount++;
                        continue;
                    }

                    // Export OBJ
                    if (!File.Exists(objPath))
                    {
                        string objContent = ExportMeshToOBJ(newMesh);
                        if (!string.IsNullOrEmpty(objContent))
                        {
                            try
                            {
                                File.WriteAllText(objPath, objContent);
                                objExportCount++;
                                Debug.Log(
                                    "LevelMeshPipeline: Exported OBJ for "
                                    + childName);
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogError(
                                    "LevelMeshPipeline: OBJ write failed for "
                                    + childName
                                    + "\n  Error: " + e.Message);
                            }
                        }
                    }

                    // Create prefab
                    GameObject tempGO = new GameObject(childName);
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
                            "LevelMeshPipeline: Failed to create prefab for "
                            + childName,
                            childMR.gameObject);
                        skipCount++;
                        continue;
                    }

                    prefabCache[childName] = prefabAsset;
                    newPrefabCount++;
                }

                if (prefabAsset == null)
                {
                    Debug.LogWarning(
                        "LevelMeshPipeline: No prefab for "
                        + childName + ", skipping.",
                        childMR.gameObject);
                    skipCount++;
                    continue;
                }

                // Instantiate prefab into rebuilt parent
                GameObject instance = PrefabUtility.InstantiatePrefab(
                    prefabAsset) as GameObject;

                if (instance == null)
                {
                    Debug.LogWarning(
                        "LevelMeshPipeline: Failed to instantiate prefab for "
                        + childName);
                    skipCount++;
                    continue;
                }

                instance.transform.parent = rebuiltParent.transform;
                instance.transform.localPosition = childTransform.localPosition;
                instance.transform.localRotation = childTransform.localRotation;
                instance.transform.localScale = childTransform.localScale;
                instance.name = childName;

                Undo.RegisterCreatedObjectUndo(instance, "Build Level Prefabs");
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        // Set static flags on rebuilt parent
        GameObjectUtility.SetStaticEditorFlags(rebuiltParent,
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccluderStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.NavigationStatic |
            StaticEditorFlags.OffMeshLinkGeneration |
            StaticEditorFlags.ReflectionProbeStatic);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("LevelMeshPipeline Step 1 Complete."
            + "\n  New meshes: " + newMeshCount
            + "\n  OBJs exported: " + objExportCount
            + "\n  New prefabs: " + newPrefabCount
            + "\n  Reused: " + reuseCount
            + "\n  No match: " + noMatchCount
            + "\n  Skipped: " + skipCount
            + "\n  Total children processed: " + childRenderers.Length
            + "\n  Rebuilt parent: " + rebuiltName
            + "\n  Now let Unity finish importing OBJs, then run Step 2.");
    }

    [MenuItem("GameObject/Level Mesh Pipeline/Step 1 - Build Level Prefabs", true)]
    static bool BuildLevelPrefabsValidate()
    {
        if (Selection.activeGameObject == null) return false;
        foreach (MeshFilter f in Selection.activeGameObject
            .GetComponentsInChildren<MeshFilter>(true))
        {
            if (f.sharedMesh != null && f.sharedMesh.subMeshCount > 1)
                return true;
        }
        return false;
    }

    [MenuItem("GameObject/Level Mesh Pipeline/Step 2 - Remap To OBJ Meshes", false, 0)]
    static void RemapToOBJMeshes()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogWarning("LevelMeshPipeline: No GameObject selected.");
            return;
        }

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
            Debug.LogWarning("LevelMeshPipeline: OBJ folder not found at "
                + objFolder + ". Run Step 1 first.");
            return;
        }

        if (!Directory.Exists(prefabFolder))
        {
            Debug.LogWarning("LevelMeshPipeline: Prefab folder not found at "
                + prefabFolder + ". Run Step 1 first.");
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

        Debug.Log("LevelMeshPipeline: Loaded " + objMeshCache.Count
            + " imported OBJ meshes.");

        if (objMeshCache.Count == 0)
        {
            Debug.LogWarning(
                "LevelMeshPipeline: No imported OBJ meshes found. "
                + "Make sure Unity has finished importing after Step 1.");
            return;
        }

        // Find rebuilt parent — look for _Rebuilt suffix
        string rebuiltName = selected.name + "_Rebuilt";
        GameObject rebuiltParent = GameObject.Find(rebuiltName);

        if (rebuiltParent == null)
        {
            // Try finding any _Rebuilt variant
            GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
            foreach (GameObject go in allObjects)
            {
                if (go.name.StartsWith(selected.name + "_Rebuilt"))
                {
                    rebuiltParent = go;
                    break;
                }
            }
        }

        if (rebuiltParent == null)
        {
            Debug.LogWarning(
                "LevelMeshPipeline: Could not find rebuilt parent object. "
                + "Run Step 1 first.");
            return;
        }

        Debug.Log("LevelMeshPipeline: Remapping instances under "
            + rebuiltParent.name);

        int remappedCount = 0;
        int skippedCount = 0;
        int noMatchCount = 0;

        MeshFilter[] rebuiltFilters = rebuiltParent
            .GetComponentsInChildren<MeshFilter>(true);

        try
        {
            int total = rebuiltFilters.Length;
            int current = 0;

            foreach (MeshFilter mf in rebuiltFilters)
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
                    Debug.Log("LevelMeshPipeline: Cancelled at "
                        + current + " of " + total);
                    break;
                }

                Mesh objMesh = null;
                if (!objMeshCache.TryGetValue(objectName, out objMesh))
                {
                    Debug.LogWarning(
                        "LevelMeshPipeline: No OBJ mesh found for "
                        + objectName);
                    noMatchCount++;
                    continue;
                }

                if (mf.sharedMesh == objMesh)
                {
                    skippedCount++;
                    continue;
                }

                // Swap mesh on scene instance
                Undo.RecordObject(mf, "Remap To OBJ Mesh");
                mf.sharedMesh = objMesh;
                EditorUtility.SetDirty(mf.gameObject);

                // Also update the prefab
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

        Debug.Log("LevelMeshPipeline Step 2 Complete."
            + "\n  Remapped: " + remappedCount
            + "\n  No match: " + noMatchCount
            + "\n  Skipped: " + skippedCount
            + "\n  Total processed: " + rebuiltFilters.Length);
    }

    [MenuItem("GameObject/Level Mesh Pipeline/Step 2 - Remap To OBJ Meshes", true)]
    static bool RemapToOBJMeshesValidate()
    {
        return Selection.activeGameObject != null;
    }

    static string ExportMeshToOBJ(Mesh mesh)
    {
        if (mesh == null) return null;

        StringBuilder sb = new StringBuilder();

        sb.AppendLine("# Exported from Unity by LevelMeshPipeline");
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