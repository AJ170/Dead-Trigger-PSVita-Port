using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Globalization;

public static class LightProbeDataAssetBuilder
{
    [MenuItem("Assets/Build Light Probe Data Asset From Text", false, 0)]
    static void BuildLightProbeDataAsset()
    {
        Object selected = Selection.activeObject;
        if (selected == null)
        {
            Debug.LogWarning(
                "LightProbeDataAssetBuilder: No file selected.");
            return;
        }

        string inputPath = AssetDatabase.GetAssetPath(selected)
            .Replace("\\", "/");
        string fullPath = Path.GetFullPath(inputPath)
            .Replace("\\", "/");

        if (!File.Exists(fullPath))
        {
            Debug.LogWarning(
                "LightProbeDataAssetBuilder: File not found at "
                + fullPath);
            return;
        }

        Debug.Log("LightProbeDataAssetBuilder: Reading " + fullPath);

        string[] lines = File.ReadAllLines(fullPath);

        // Parse positions and SH
        List<Vector3> positions = ParsePositions(lines);
        List<float[]> shList = ParseSHCoefficients(lines);

        Debug.Log("LightProbeDataAssetBuilder: Parsed "
            + positions.Count + " positions and "
            + shList.Count + " SH entries.");

        if (positions.Count == 0)
        {
            Debug.LogWarning(
                "LightProbeDataAssetBuilder: No positions found.");
            return;
        }

        if (positions.Count != shList.Count)
        {
            Debug.LogWarning(
                "LightProbeDataAssetBuilder: Count mismatch — "
                + "positions: " + positions.Count
                + " SH: " + shList.Count
                + ". Proceeding with available data.");
        }

        // Create the ScriptableObject asset
        LightProbeDataAsset asset =
            ScriptableObject.CreateInstance<LightProbeDataAsset>();

        int count = Mathf.Min(positions.Count, shList.Count > 0
            ? shList.Count
            : positions.Count);

        asset.probes = new LightProbeDataAsset.ProbeEntry[count];

        for (int i = 0; i < count; i++)
        {
            asset.probes[i] = new LightProbeDataAsset.ProbeEntry();
            asset.probes[i].position = positions[i];

            if (i < shList.Count)
                asset.probes[i].shCoefficients = shList[i];
            else
                asset.probes[i].shCoefficients = new float[27];
        }

        // Save alongside the source file
        string outputDir = Path.GetDirectoryName(inputPath)
            .Replace("\\", "/");
        string outputPath = Path.Combine(outputDir, "LightProbeData.asset")
            .Replace("\\", "/");

        AssetDatabase.CreateAsset(asset, outputPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorGUIUtility.PingObject(asset);

        Debug.Log("LightProbeDataAssetBuilder: Created asset at "
            + outputPath
            + "\n  Probes: " + count
            + "\n  SH data: " + (shList.Count > 0 ? "yes" : "no")
            + "\n\nAdd LightProbeRuntimeInjector component to a "
            + "GameObject in your scene and assign this asset to it.");
    }

    [MenuItem("Assets/Build Light Probe Data Asset From Text", true)]
    static bool BuildLightProbeDataAssetValidate()
    {
        if (Selection.activeObject == null) return false;
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        return path.EndsWith(".txt") || path.EndsWith(".asset");
    }

    static List<Vector3> ParsePositions(string[] lines)
    {
        List<Vector3> positions = new List<Vector3>();
        bool inPositions = false;

        foreach (string line in lines)
        {
            string trimmed = line.TrimStart();

            if (trimmed.StartsWith("m_Positions:"))
            {
                inPositions = true;
                continue;
            }

            if (trimmed.StartsWith(
                "m_NonTetrahedralizedProbeSetIndexMap"))
            {
                inPositions = false;
                continue;
            }

            if (inPositions && trimmed.StartsWith("- {x:"))
                positions.Add(ParseInlineVector3(trimmed));
        }

        return positions;
    }

    static List<float[]> ParseSHCoefficients(string[] lines)
    {
        List<float[]> shList = new List<float[]>();
        bool inSH = false;
        float[] current = null;
        int idx = 0;
        bool firstEntry = true;

        foreach (string line in lines)
        {
            string trimmed = line.TrimStart();

            if (trimmed.StartsWith("m_BakedCoefficients:"))
            {
                inSH = true;
                firstEntry = true;
                continue;
            }

            if (trimmed.StartsWith("m_BakedLightOcclusion:"))
            {
                if (inSH && current != null && idx == 27)
                    shList.Add(current);
                inSH = false;
                continue;
            }

            if (!inSH) continue;

            if (trimmed.StartsWith("- sh["))
            {
                if (!firstEntry && current != null && idx == 27)
                    shList.Add(current);

                firstEntry = false;
                current = new float[27];
                idx = 0;

                float val = ParseSHValue(trimmed.Substring(2));
                if (idx < 27) current[idx++] = val;
                continue;
            }

            if (trimmed.StartsWith("sh[") && current != null)
            {
                float val = ParseSHValue(trimmed);
                if (idx < 27) current[idx++] = val;
            }
        }

        if (inSH && current != null && idx == 27)
            shList.Add(current);

        return shList;
    }

    static float ParseSHValue(string line)
    {
        int colonIdx = line.IndexOf(':');
        if (colonIdx < 0) return 0f;

        float result;
        float.TryParse(line.Substring(colonIdx + 1).Trim(),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out result);
        return result;
    }

    static Vector3 ParseInlineVector3(string line)
    {
        int start = line.IndexOf('{');
        int end = line.IndexOf('}');
        if (start < 0 || end < 0) return Vector3.zero;

        string inner = line.Substring(start + 1, end - start - 1);
        string[] parts = inner.Split(',');

        float x = 0, y = 0, z = 0;
        foreach (string part in parts)
        {
            string t = part.Trim();
            if (t.StartsWith("x:"))
                float.TryParse(t.Substring(2).Trim(),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture, out x);
            else if (t.StartsWith("y:"))
                float.TryParse(t.Substring(2).Trim(),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture, out y);
            else if (t.StartsWith("z:"))
                float.TryParse(t.Substring(2).Trim(),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture, out z);
        }

        return new Vector3(x, y, z);
    }
}