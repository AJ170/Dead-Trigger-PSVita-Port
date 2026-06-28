using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public static class SubmeshSplitter
{
    [MenuItem("GameObject/Split Submeshes To Individual Meshes", false, 0)]
    static void SplitSubmeshes()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogWarning("SubmeshSplitter: No GameObject selected.");
            return;
        }

        // Find combined mesh from children
        Mesh combinedMesh = null;
        foreach (MeshFilter f in selected.GetComponentsInChildren<MeshFilter>(true))
        {
            if (f.sharedMesh != null && f.sharedMesh.subMeshCount > 1)
            {
                combinedMesh = f.sharedMesh;
                break;
            }
        }

        if (combinedMesh == null)
        {
            Debug.LogWarning("SubmeshSplitter: No combined mesh found under "
                + selected.name);
            return;
        }

        // Create output folders next to scene
        string scenePath = UnityEngine.SceneManagement
            .SceneManager.GetActiveScene().path;
        string sceneDir = Path.GetDirectoryName(scenePath);
        string sceneName = Path.GetFileNameWithoutExtension(scenePath);
        //string meshFolder = Path.Combine(sceneDir, sceneName + "_SplitMeshes");
        //string prefabFolder = Path.Combine(sceneDir, sceneName + "_Prefabs");

        string meshFolder = Path.Combine(sceneDir, sceneName + "_SplitMeshes")
    .Replace("\\", "/");
        string prefabFolder = Path.Combine(sceneDir, sceneName + "_Prefabs")
            .Replace("\\", "/");

        if (!Directory.Exists(meshFolder))
            Directory.CreateDirectory(meshFolder);

        if (!Directory.Exists(prefabFolder))
            Directory.CreateDirectory(prefabFolder);

        Debug.Log("SubmeshSplitter: Mesh folder: " + meshFolder);
        Debug.Log("SubmeshSplitter: Prefab folder: " + prefabFolder);

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

        // Create clone parent to hold prefab instances
        GameObject cloneParent = new GameObject(selected.name + "_Clone");
        cloneParent.transform.position = Vector3.zero;
        cloneParent.transform.rotation = Quaternion.identity;
        cloneParent.transform.localScale = Vector3.one;
        Undo.RegisterCreatedObjectUndo(cloneParent, "Split Submeshes");

        // Track unique prefabs created this run
        // Key is child name, value is the prefab asset
        Dictionary<string, GameObject> createdPrefabs =
            new Dictionary<string, GameObject>();

        int newMeshCount = 0;
        int newPrefabCount = 0;
        int reuseCount = 0;
        int noMatchCount = 0;
        int skipCount = 0;
        float matchThreshold = 0.5f;

        foreach (MeshRenderer childMR in childRenderers)
        {


            string childName = childMR.gameObject.name;
            Material mat = childMR.sharedMaterial;
            Transform childTransform = childMR.transform;

            //string meshAssetPath = Path.Combine(meshFolder, childName + ".asset");
            //string prefabAssetPath = Path.Combine(prefabFolder, childName + ".prefab");

            string meshAssetPath = Path.Combine(meshFolder, childName + ".asset")
    .Replace("\\", "/");
            string prefabAssetPath = Path.Combine(prefabFolder, childName + ".prefab")
                .Replace("\\", "/");

            Debug.Log("SubmeshSplitter: Processing child: " + childName
    + "\n  Prefab exists on disk: " + File.Exists(prefabAssetPath)
    + "\n  Already in dict: " + createdPrefabs.ContainsKey(childName));

            Mesh newMesh = null;
            GameObject prefabAsset = null;

            // Check if prefab already exists on disk
            if (File.Exists(prefabAssetPath))
            {
                prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(
                    prefabAssetPath);

                if (!createdPrefabs.ContainsKey(childName))
                    createdPrefabs[childName] = prefabAsset;

                reuseCount++;
            }
            // Check if already created in this run
            else if (createdPrefabs.ContainsKey(childName))
            {
                prefabAsset = createdPrefabs[childName];
                reuseCount++;
            }
            else
            {
                // Check if mesh asset already exists
                if (File.Exists(meshAssetPath))
                {
                    newMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshAssetPath);
                    Debug.Log("SubmeshSplitter: Reusing existing mesh: "
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

                    if (closestSubmesh < 0 || closestDistance > matchThreshold)
                    {
                        Debug.LogWarning("SubmeshSplitter: No submesh match for "
                            + childName
                            + " (closest distance: " + closestDistance + ")");
                        noMatchCount++;
                        continue;
                    }

                    int[] triangles = submeshTriangles[closestSubmesh];

                    // Find unique vertices for this submesh
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

                    // Build new vertex arrays in child local space
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
                    BoneWeight[] newBoneWeights = allBoneWeights.Length > 0 ?
                        new BoneWeight[vertCount] : null;

                    for (int i = 0; i < orderedIndices.Count; i++)
                    {
                        int oldIdx = orderedIndices[i];

                        newVertices[i] = childTransform
                            .InverseTransformPoint(allVertices[oldIdx]);

                        if (newNormals != null)
                            newNormals[i] = childTransform
                                .InverseTransformDirection(allNormals[oldIdx]);

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

                        if (newColors != null) newColors[i] = allColors[oldIdx];
                        if (newUV0 != null) newUV0[i] = allUV0[oldIdx];
                        if (newUV1 != null) newUV1[i] = allUV1[oldIdx];
                        if (newUV2 != null) newUV2[i] = allUV2[oldIdx];
                        if (newUV3 != null) newUV3[i] = allUV3[oldIdx];
                        if (newBoneWeights != null)
                            newBoneWeights[i] = allBoneWeights[oldIdx];
                    }

                    // Remap triangles
                    int[] newTriangles = new int[triangles.Length];
                    for (int i = 0; i < triangles.Length; i++)
                        newTriangles[i] = indexRemap[triangles[i]];

                    // Build mesh
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
                    Debug.LogWarning("SubmeshSplitter: Failed to get mesh for "
                        + childName + ", skipping.");
                    skipCount++;
                    continue;
                }

                // Create temporary GO for prefab
                GameObject tempGO = new GameObject(childName);
                MeshFilter newMF = tempGO.AddComponent<MeshFilter>();
                newMF.sharedMesh = newMesh;

                MeshRenderer newMR = tempGO.AddComponent<MeshRenderer>();
                if (mat != null)
                    newMR.sharedMaterial = mat;

                GameObjectUtility.SetStaticEditorFlags(tempGO,
                    StaticEditorFlags.BatchingStatic |
                    StaticEditorFlags.OccluderStatic |
                    StaticEditorFlags.OccludeeStatic |
                    StaticEditorFlags.NavigationStatic |
                    StaticEditorFlags.OffMeshLinkGeneration |
                    StaticEditorFlags.ReflectionProbeStatic);

                Debug.Log("SubmeshSplitter: Attempting prefab creation for " + childName
                    + "\n  Path: " + prefabAssetPath
                    + "\n  Mesh null: " + (newMesh == null)
                    + "\n  Mat null: " + (mat == null)
                    + "\n  TempGO null: " + (tempGO == null));

                prefabAsset = PrefabUtility.CreatePrefab(
                    prefabAssetPath,
                    tempGO,
                    ReplacePrefabOptions.Default);

                Debug.Log("SubmeshSplitter: Prefab creation result for " + childName
                    + "\n  Prefab null: " + (prefabAsset == null)
                    + "\n  File exists after: " + File.Exists(prefabAssetPath));

                Object.DestroyImmediate(tempGO);

                if (prefabAsset == null)
                {
                    Debug.LogWarning(
                        "SubmeshSplitter: Failed to create prefab for "
                        + childName);
                    skipCount++;
                    continue;
                }

                createdPrefabs[childName] = prefabAsset;
                newPrefabCount++;
                Debug.Log("SubmeshSplitter: Created prefab: " + childName);
            }

            if (prefabAsset == null)
            {
                Debug.LogWarning("SubmeshSplitter: No prefab available for "
                    + childName + ", skipping instance.");
                skipCount++;
                continue;
            }

            // Instantiate prefab into clone hierarchy at original transform
            GameObject instance = PrefabUtility.InstantiatePrefab(
                prefabAsset) as GameObject;

            if (instance == null)
            {
                Debug.LogWarning(
                    "SubmeshSplitter: Failed to instantiate prefab for "
                    + childName);
                skipCount++;
                continue;
            }

            instance.transform.parent = cloneParent.transform;
            instance.transform.localPosition = childTransform.localPosition;
            instance.transform.localRotation = childTransform.localRotation;
            instance.transform.localScale = childTransform.localScale;

            Undo.RegisterCreatedObjectUndo(instance, "Split Submeshes");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("SubmeshSplitter: Complete."
            + "\n  New meshes: " + newMeshCount
            + "\n  New prefabs: " + newPrefabCount
            + "\n  Reused: " + reuseCount
            + "\n  No match: " + noMatchCount
            + "\n  Skipped: " + skipCount
            + "\n  Total children processed: " + childRenderers.Length
            + "\n  Meshes: " + meshFolder
            + "\n  Prefabs: " + prefabFolder);
    }

    [MenuItem("GameObject/Split Submeshes To Individual Meshes", true)]
    static bool SplitSubmeshesValidate()
    {
        return Selection.activeGameObject != null;
    }
}