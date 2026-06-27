using System.IO;
using UnityEditor;
using UnityEngine;

public static class ModifyReadWrite
{
    [MenuItem("Assets/Modify Read/Write To False")]
    static void ModifySelectedAssetsToFalse()
    {
        foreach (Object selectedObject in Selection.objects)
        {
            if (selectedObject == null) continue;

            string assetPath = AssetDatabase.GetAssetPath(selectedObject);
            string[] lines = File.ReadAllLines(assetPath);
            bool modified = false;

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("m_IsReadable: 1"))
                {
                    lines[i] = lines[i].Replace("m_IsReadable: 1", "m_IsReadable: 0");
                    modified = true;
                }
            }

            if (modified)
            {
                CreateBackup(assetPath);
                File.WriteAllLines(assetPath, lines);
                AssetDatabase.Refresh();
                Debug.Log("ModifyReadWrite: Set readable to false on " + selectedObject.name);
            }
            else
            {
                Debug.Log("ModifyReadWrite: " + selectedObject.name + " is already false, skipping.");
            }
        }
    }

    [MenuItem("Assets/Modify Read/Write To False", true)]
    static bool ModifySelectedAssetsToFalseValidate()
    {
        return HasValidAssetSelected();
    }

    [MenuItem("Assets/Modify Read/Write To True")]
    static void ModifySelectedAssetsToTrue()
    {
        foreach (Object selectedObject in Selection.objects)
        {
            if (selectedObject == null) continue;

            string assetPath = AssetDatabase.GetAssetPath(selectedObject);
            string[] lines = File.ReadAllLines(assetPath);
            bool modified = false;

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("m_IsReadable: 0"))
                {
                    lines[i] = lines[i].Replace("m_IsReadable: 0", "m_IsReadable: 1");
                    modified = true;
                }
            }

            if (modified)
            {
                CreateBackup(assetPath);
                File.WriteAllLines(assetPath, lines);
                AssetDatabase.Refresh();
                Debug.Log("ModifyReadWrite: Set readable to true on " + selectedObject.name);
            }
            else
            {
                Debug.Log("ModifyReadWrite: " + selectedObject.name + " is already true, skipping.");
            }
        }
    }

    [MenuItem("Assets/Modify Read/Write To True", true)]
    static bool ModifySelectedAssetsToTrueValidate()
    {
        return HasValidAssetSelected();
    }

    private static void CreateBackup(string assetPath)
    {
        string backupPath = assetPath.Replace(".asset", "_backup.asset");

        if (File.Exists(backupPath))
        {
            Debug.Log("ModifyReadWrite: Backup already exists at " + backupPath + ", preserving original.");
            return;
        }

        File.Copy(assetPath, backupPath);
        AssetDatabase.Refresh();
        Debug.Log("ModifyReadWrite: Backup created at " + backupPath);
    }

    private static bool HasValidAssetSelected()
    {
        foreach (Object obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path) && path.EndsWith(".asset"))
                return true;
        }
        return false;
    }
}