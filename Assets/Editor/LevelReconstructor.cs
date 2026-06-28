using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;

public static class LevelReconstructor
{
    [MenuItem("Assets/Generate Lightmap UVs")]
    static void GenerateLightmapUVs()
    {
        foreach (Object obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null) continue;

            Unwrapping.GenerateSecondaryUVSet(mesh);
            EditorUtility.SetDirty(mesh);
            Debug.Log("Generated lightmap UVs for " + mesh.name);
        }
        AssetDatabase.SaveAssets();
    }

    [MenuItem("GameObject/Reconstruct Level From Prefabs", false, 0)]
    static void ReconstructLevel()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogWarning("LevelReconstructor: No GameObject selected.");
            return;
        }

        // Check it has children to work from
        if (selected.transform.childCount == 0)
        {
            Debug.LogWarning("LevelReconstructor: Selected object has no "
                + "children to reconstruct from.");
            return;
        }

        // Find prefab folder based on scene name
        string scenePath = UnityEngine.SceneManagement
            .SceneManager.GetActiveScene().path;
        string sceneDir = Path.GetDirectoryName(scenePath)
            .Replace("\\", "/");
        string sceneName = Path.GetFileNameWithoutExtension(scenePath);
        string prefabFolder = Path.Combine(sceneDir, sceneName + "_Prefabs")
            .Replace("\\", "/");

        if (!Directory.Exists(prefabFolder))
        {
            Debug.LogWarning("LevelReconstructor: Prefab folder not found at "
                + prefabFolder);
            return;
        }

        Debug.Log("LevelReconstructor: Reading prefabs from " + prefabFolder);

        // Cache all prefabs in the folder by name for quick lookup
        Dictionary<string, GameObject> prefabCache =
            new Dictionary<string, GameObject>();

        string[] prefabPaths = Directory.GetFiles(prefabFolder, "*.prefab");
        foreach (string path in prefabPaths)
        {
            string unityPath = path.Replace("\\", "/");
            GameObject prefab = AssetDatabase
                .LoadAssetAtPath<GameObject>(unityPath);

            if (prefab != null && !prefabCache.ContainsKey(prefab.name))
                prefabCache[prefab.name] = prefab;
        }

        Debug.Log("LevelReconstructor: Loaded " + prefabCache.Count
            + " prefabs from folder.");

        if (prefabCache.Count == 0)
        {
            Debug.LogWarning("LevelReconstructor: No prefabs found in "
                + prefabFolder);
            return;
        }

        // Create rebuilt parent at scene root
        GameObject rebuiltParent = new GameObject(selected.name + "_Rebuilt");
        rebuiltParent.transform.position = Vector3.zero;
        rebuiltParent.transform.rotation = Quaternion.identity;
        rebuiltParent.transform.localScale = Vector3.one;
        Undo.RegisterCreatedObjectUndo(rebuiltParent, "Reconstruct Level");

        int successCount = 0;
        int missingPrefabCount = 0;
        int skipCount = 0;

        foreach (Transform child in selected.transform)
        {
            string childName = child.gameObject.name;

            // Find matching prefab by name
            GameObject prefabAsset = null;
            if (!prefabCache.TryGetValue(childName, out prefabAsset))
            {
                // Try stripping any numeric suffix e.g. "wall_01 (1)"
                string strippedName = childName;
                int parenIndex = childName.IndexOf(" (");
                if (parenIndex > 0)
                    strippedName = childName.Substring(0, parenIndex);

                if (!prefabCache.TryGetValue(strippedName, out prefabAsset))
                {
                    Debug.LogWarning("LevelReconstructor: No prefab found for "
                        + childName + ", skipping.");
                    missingPrefabCount++;
                    continue;
                }
            }

            // Instantiate maintaining prefab connection
            GameObject instance = PrefabUtility.InstantiatePrefab(
                prefabAsset) as GameObject;

            if (instance == null)
            {
                Debug.LogWarning(
                    "LevelReconstructor: Failed to instantiate prefab for "
                    + childName);
                skipCount++;
                continue;
            }

            // Place under rebuilt parent using original child transform
            instance.transform.parent = rebuiltParent.transform;
            instance.transform.localPosition = child.localPosition;
            instance.transform.localRotation = child.localRotation;
            instance.transform.localScale = child.localScale;
            instance.name = childName;

            Undo.RegisterCreatedObjectUndo(instance, "Reconstruct Level");
            successCount++;
        }

        GameObjectUtility.SetStaticEditorFlags(rebuiltParent,
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccluderStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.NavigationStatic |
            StaticEditorFlags.OffMeshLinkGeneration |
            StaticEditorFlags.ReflectionProbeStatic);

        EditorUtility.SetDirty(rebuiltParent);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("LevelReconstructor: Complete."
            + "\n  Placed: " + successCount
            + "\n  Missing prefab: " + missingPrefabCount
            + "\n  Failed: " + skipCount
            + "\n  Total children processed: "
            + selected.transform.childCount);
    }

    [MenuItem("GameObject/Reconstruct Level From Prefabs", true)]
    static bool ReconstructLevelValidate()
    {
        if (Selection.activeGameObject == null) return false;
        return Selection.activeGameObject.transform.childCount > 0;
    }
}