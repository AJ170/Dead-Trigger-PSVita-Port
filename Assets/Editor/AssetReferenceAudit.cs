using UnityEngine;
using UnityEditor;
using System.IO;

public static class AssetReferenceAudit
{
    [MenuItem("Tools/Audit Scene Mesh Asset References")]
    static void AuditMeshAssetReferences()
    {
        // Ask user to select the asset to search for
        string assetPath = EditorUtility.OpenFilePanel(
            "Select Mesh Asset to Search For",
            "Assets",
            "asset");

        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogWarning("AssetReferenceAudit: No asset selected.");
            return;
        }

        // Convert absolute path to Unity relative path
        assetPath = assetPath.Replace("\\", "/");
        string projectPath = Application.dataPath.Replace("/Assets", "/");
        assetPath = assetPath.Replace(projectPath, "");

        Mesh targetMesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
        if (targetMesh == null)
        {
            Debug.LogWarning("AssetReferenceAudit: No mesh found at "
                + assetPath);
            return;
        }

        Debug.Log("AssetReferenceAudit: Searching for references to "
            + targetMesh.name + " at " + assetPath);

        // Search all MeshFilters in scene
        MeshFilter[] meshFilters = Object.FindObjectsOfType<MeshFilter>();
        int foundCount = 0;
        int checkedCount = 0;

        foreach (MeshFilter mf in meshFilters)
        {
            checkedCount++;
            if (mf.sharedMesh == null) continue;

            string meshPath = AssetDatabase
                .GetAssetPath(mf.sharedMesh)
                .Replace("\\", "/");

            if (meshPath == assetPath)
            {
                foundCount++;
                Debug.Log("AssetReferenceAudit: Reference found on "
                    + GetFullPath(mf.transform)
                    + "\n  Mesh name: " + mf.sharedMesh.name
                    + "\n  Asset path: " + meshPath,
                    mf.gameObject);
            }
        }

        // Also check SkinnedMeshRenderers
        SkinnedMeshRenderer[] skinnedRenderers =
            Object.FindObjectsOfType<SkinnedMeshRenderer>();

        foreach (SkinnedMeshRenderer smr in skinnedRenderers)
        {
            checkedCount++;
            if (smr.sharedMesh == null) continue;

            string meshPath = AssetDatabase
                .GetAssetPath(smr.sharedMesh)
                .Replace("\\", "/");

            if (meshPath == assetPath)
            {
                foundCount++;
                Debug.Log("AssetReferenceAudit: Reference found on "
                    + "SkinnedMeshRenderer: "
                    + GetFullPath(smr.transform)
                    + "\n  Mesh name: " + smr.sharedMesh.name
                    + "\n  Asset path: " + meshPath,
                    smr.gameObject);
            }
        }

        Debug.Log("AssetReferenceAudit: Complete."
            + "\n  Checked: " + checkedCount
            + "\n  References found: " + foundCount
            + "\n  Asset: " + assetPath);
    }

    static string GetFullPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}