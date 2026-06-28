using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public static class PrefabMeshRemapper
{
    [MenuItem("Assets/Remap Prefab Mesh To Imported OBJ", false, 0)]
    static void RemapSelectedPrefabs()
    {
        // Get scene paths for folder locations
        string scenePath = UnityEngine.SceneManagement
            .SceneManager.GetActiveScene().path;
        string sceneDir = Path.GetDirectoryName(scenePath)
            .Replace("\\", "/");
        string sceneName = Path.GetFileNameWithoutExtension(scenePath);
        string objFolder = Path.Combine(sceneDir, sceneName + "_OBJExport")
            .Replace("\\", "/");

        if (!Directory.Exists(objFolder))
        {
            Debug.LogWarning("PrefabMeshRemapper: OBJ export folder not found at "
                + objFolder);
            return;
        }

        // Cache all imported OBJ meshes by filename for quick lookup
        // Key is filename without extension e.g. "wall_01"
        // Value is the first mesh found inside that imported asset
        Dictionary<string, Mesh> importedMeshCache =
            new Dictionary<string, Mesh>();

        string[] objPaths = Directory.GetFiles(objFolder, "*.obj");
        foreach (string path in objPaths)
        {
            string unityPath = path.Replace("\\", "/");
            string fileName = Path.GetFileNameWithoutExtension(unityPath);

            // Load all assets inside the imported OBJ
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(unityPath);
            foreach (Object asset in assets)
            {
                Mesh mesh = asset as Mesh;
                if (mesh == null) continue;

                if (!importedMeshCache.ContainsKey(fileName))
                {
                    importedMeshCache[fileName] = mesh;
                    break;
                }
            }
        }

        Debug.Log("PrefabMeshRemapper: Found " + importedMeshCache.Count
            + " imported OBJ meshes in " + objFolder);

        if (importedMeshCache.Count == 0)
        {
            Debug.LogWarning("PrefabMeshRemapper: No imported OBJ meshes found."
                + " Make sure OBJs have been imported into Unity.");
            return;
        }

        int remappedCount = 0;
        int skippedCount = 0;
        int noMatchCount = 0;

        try
        {
            int total = Selection.objects.Length;
            int current = 0;

            foreach (Object obj in Selection.objects)
            {
                current++;
                string assetPath = AssetDatabase.GetAssetPath(obj)
                    .Replace("\\", "/");

                // Update progress bar
                if (EditorUtility.DisplayCancelableProgressBar(
                    "Remapping Prefab Meshes",
                    "Processing: " + obj.name
                        + " (" + current + " of " + total + ")",
                    (float)current / total))
                {
                    Debug.Log("PrefabMeshRemapper: Cancelled by user at "
                        + current + " of " + total
                        + "\n  Remapped so far: " + remappedCount);
                    break;
                }

                // Must be a prefab
                GameObject prefab = AssetDatabase
                    .LoadAssetAtPath<GameObject>(assetPath);
                if (prefab == null)
                {
                    Debug.LogWarning("PrefabMeshRemapper: Not a prefab: "
                        + assetPath + ", skipping.");
                    skippedCount++;
                    continue;
                }

                // Get the prefab name to look up in our cache
                string prefabName = Path.GetFileNameWithoutExtension(assetPath);

                // Find matching imported mesh by filename
                Mesh importedMesh = null;
                if (!importedMeshCache.TryGetValue(prefabName, out importedMesh))
                {
                    Debug.LogWarning("PrefabMeshRemapper: No imported OBJ found "
                        + "matching prefab name: " + prefabName);
                    noMatchCount++;
                    continue;
                }

                // Find the MeshFilter on the prefab
                MeshFilter mf = prefab.GetComponent<MeshFilter>();
                if (mf == null)
                {
                    Debug.LogWarning("PrefabMeshRemapper: No MeshFilter on "
                        + prefabName + ", skipping.");
                    skippedCount++;
                    continue;
                }

                // Check if already remapped to avoid unnecessary work
                if (mf.sharedMesh == importedMesh)
                {
                    Debug.Log("PrefabMeshRemapper: " + prefabName
                        + " already remapped, skipping.");
                    skippedCount++;
                    continue;
                }

                // Log what we're replacing
                string oldMeshName = mf.sharedMesh != null
                    ? mf.sharedMesh.name : "null";

                // Remap the mesh
                mf.sharedMesh = importedMesh;

                // Also remap any child MeshFilters if present
                MeshFilter[] childFilters = prefab
                    .GetComponentsInChildren<MeshFilter>(true);
                foreach (MeshFilter childMF in childFilters)
                {
                    if (childMF == mf) continue;
                    if (childMF.sharedMesh != null
                        && childMF.sharedMesh.name == oldMeshName)
                    {
                        childMF.sharedMesh = importedMesh;
                    }
                }

                EditorUtility.SetDirty(prefab);

                // Save the prefab
                PrefabUtility.ReplacePrefab(
                    prefab,
                    prefab,
                    ReplacePrefabOptions.Default);

                remappedCount++;
                Debug.Log("PrefabMeshRemapper: Remapped " + prefabName
                    + "\n  Old mesh: " + oldMeshName
                    + "\n  New mesh: " + importedMesh.name);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("PrefabMeshRemapper: Complete."
            + "\n  Remapped: " + remappedCount
            + "\n  No match: " + noMatchCount
            + "\n  Skipped: " + skippedCount
            + "\n  Total processed: " + Selection.objects.Length);
    }

    [MenuItem("Assets/Remap Prefab Mesh To Imported OBJ", true)]
    static bool RemapSelectedPrefabsValidate()
    {
        foreach (Object obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (path.EndsWith(".prefab"))
                return true;
        }
        return false;
    }

    [MenuItem("Assets/Remap All Prefabs To Imported OBJs", false, 0)]
    static void RemapAllPrefabs()
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

        if (!Directory.Exists(prefabFolder))
        {
            Debug.LogWarning("PrefabMeshRemapper: Prefab folder not found at "
                + prefabFolder);
            return;
        }

        if (!Directory.Exists(objFolder))
        {
            Debug.LogWarning("PrefabMeshRemapper: OBJ folder not found at "
                + objFolder);
            return;
        }

        // Cache all imported OBJ meshes by filename
        Dictionary<string, Mesh> importedMeshCache =
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
                if (mesh == null) continue;

                if (!importedMeshCache.ContainsKey(fileName))
                {
                    importedMeshCache[fileName] = mesh;
                    break;
                }
            }
        }

        Debug.Log("PrefabMeshRemapper: Found " + importedMeshCache.Count
            + " imported OBJ meshes.");

        if (importedMeshCache.Count == 0)
        {
            Debug.LogWarning("PrefabMeshRemapper: No imported OBJ meshes found.");
            return;
        }

        string[] prefabPaths = Directory.GetFiles(prefabFolder, "*.prefab");
        int total = prefabPaths.Length;
        int remappedCount = 0;
        int skippedCount = 0;
        int noMatchCount = 0;

        try
        {
            for (int i = 0; i < total; i++)
            {
                string unityPath = prefabPaths[i].Replace("\\", "/");
                string prefabName = Path.GetFileNameWithoutExtension(unityPath);

                if (EditorUtility.DisplayCancelableProgressBar(
                    "Remapping All Prefab Meshes",
                    "Processing: " + prefabName
                        + " (" + (i + 1) + " of " + total + ")",
                    (float)i / total))
                {
                    Debug.Log("PrefabMeshRemapper: Cancelled by user at "
                        + (i + 1) + " of " + total
                        + "\n  Remapped so far: " + remappedCount);
                    break;
                }

                GameObject prefab = AssetDatabase
                    .LoadAssetAtPath<GameObject>(unityPath);
                if (prefab == null)
                {
                    skippedCount++;
                    continue;
                }

                Mesh importedMesh = null;
                if (!importedMeshCache.TryGetValue(prefabName, out importedMesh))
                {
                    Debug.LogWarning("PrefabMeshRemapper: No OBJ match for "
                        + prefabName);
                    noMatchCount++;
                    continue;
                }

                MeshFilter mf = prefab.GetComponent<MeshFilter>();
                if (mf == null)
                {
                    skippedCount++;
                    continue;
                }

                if (mf.sharedMesh == importedMesh)
                {
                    skippedCount++;
                    continue;
                }

                mf.sharedMesh = importedMesh;
                EditorUtility.SetDirty(prefab);

                PrefabUtility.ReplacePrefab(
                    prefab,
                    prefab,
                    ReplacePrefabOptions.Default);

                remappedCount++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("PrefabMeshRemapper: Complete."
            + "\n  Remapped: " + remappedCount
            + "\n  No match: " + noMatchCount
            + "\n  Skipped: " + skippedCount
            + "\n  Total prefabs: " + total);
    }

    [MenuItem("Assets/Remap All Prefabs To Imported OBJs", true)]
    static bool RemapAllPrefabsValidate()
    {
        return true;
    }
}