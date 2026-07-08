using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static class LightProbesAssetGenerator
{
    [MenuItem("Assets/Generate Compatible LightProbes Asset", false, 0)]
    static void GenerateCompatibleLightProbesAsset()
    {
        // Get the selected file path
        Object selected = Selection.activeObject;
        if (selected == null)
        {
            Debug.LogWarning("LightProbesAssetGenerator: No file selected.");
            return;
        }

        string inputPath = AssetDatabase.GetAssetPath(selected)
            .Replace("\\", "/");

        if (string.IsNullOrEmpty(inputPath))
        {
            Debug.LogWarning(
                "LightProbesAssetGenerator: Could not get asset path.");
            return;
        }

        string fullInputPath = Path.GetFullPath(inputPath)
            .Replace("\\", "/");

        if (!File.Exists(fullInputPath))
        {
            Debug.LogWarning(
                "LightProbesAssetGenerator: File not found at "
                + fullInputPath);
            return;
        }

        Debug.Log("LightProbesAssetGenerator: Reading " + fullInputPath);

        string[] lines = File.ReadAllLines(fullInputPath);

        // Parse positions
        List<Vector3> positions = ParsePositions(lines);
        Debug.Log("LightProbesAssetGenerator: Parsed "
            + positions.Count + " positions.");

        // Parse SH coefficients
        List<float[]> shList = ParseSHCoefficients(lines);
        Debug.Log("LightProbesAssetGenerator: Parsed "
            + shList.Count + " SH entries.");

        if (positions.Count == 0)
        {
            Debug.LogWarning(
                "LightProbesAssetGenerator: No positions found. "
                + "Is this a valid LightProbes export file?");
            return;
        }

        if (shList.Count == 0)
        {
            Debug.LogWarning(
                "LightProbesAssetGenerator: No SH data found. "
                + "Positions will be written without lighting data.");
        }

        if (positions.Count != shList.Count && shList.Count > 0)
        {
            Debug.LogWarning(
                "LightProbesAssetGenerator: Position count ("
                + positions.Count + ") does not match SH count ("
                + shList.Count + "). Output may be incorrect.");
        }

        // Generate output path alongside the input file
        string outputDir = Path.GetDirectoryName(inputPath)
            .Replace("\\", "/");
        string outputPath = Path.Combine(
            outputDir, "LightProbes.asset")
            .Replace("\\", "/");

        // If output would overwrite the input, add a suffix
        if (outputPath == inputPath)
        {
            outputPath = Path.Combine(
                outputDir, "LightProbes_Generated.asset")
                .Replace("\\", "/");
        }

        // Generate YAML content
        string yaml = GenerateLightProbesYAML(positions, shList);

        // Write to disk
        string fullOutputPath = Path.GetFullPath(outputPath)
            .Replace("\\", "/");

        try
        {
            File.WriteAllText(fullOutputPath, yaml, Encoding.UTF8);
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                "LightProbesAssetGenerator: Failed to write file: "
                + e.Message);
            return;
        }

        AssetDatabase.Refresh();

        Debug.Log("LightProbesAssetGenerator: Successfully generated "
            + outputPath
            + "\n  Positions: " + positions.Count
            + "\n  SH entries: " + shList.Count
            + "\n\n  Copy this file into your scene's lighting folder "
            + "alongside LightingData.asset and refresh the asset database.");

        // Ping the generated file in the project window
        Object generated = AssetDatabase.LoadAssetAtPath<Object>(outputPath);
        if (generated != null)
            EditorGUIUtility.PingObject(generated);
    }

    [MenuItem("Assets/Generate Compatible LightProbes Asset", true)]
    static bool GenerateCompatibleLightProbesAssetValidate()
    {
        if (Selection.activeObject == null) return false;
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        // Show for .asset and .txt files
        return path.EndsWith(".asset") || path.EndsWith(".txt");
    }

    static List<Vector3> ParsePositions(string[] lines)
    {
        List<Vector3> positions = new List<Vector3>();
        bool inPositions = false;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].TrimStart();

            if (line.StartsWith("m_Positions:"))
            {
                inPositions = true;
                continue;
            }

            if (inPositions)
            {
                if (line.StartsWith("m_NonTetrahedralizedProbeSetIndexMap"))
                {
                    break;
                }

                if (line.StartsWith("- {x:"))
                {
                    Vector3 pos = ParseInlineVector3(line);
                    positions.Add(pos);
                }
            }
        }

        return positions;
    }

    static List<float[]> ParseSHCoefficients(string[] lines)
    {
        List<float[]> shList = new List<float[]>();
        bool inSH = false;
        float[] current = null;
        int currentIndex = 0;
        bool firstEntry = true;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].TrimStart();

            if (line.StartsWith("m_BakedCoefficients:"))
            {
                inSH = true;
                firstEntry = true;
                continue;
            }

            if (line.StartsWith("m_BakedLightOcclusion:"))
            {
                if (inSH && current != null && currentIndex == 27)
                    shList.Add(current);
                inSH = false;
                continue;
            }

            if (!inSH) continue;

            // New probe entry starts with "- sh[ 0]:"
            if (line.StartsWith("- sh["))
            {
                if (!firstEntry && current != null && currentIndex == 27)
                    shList.Add(current);

                firstEntry = false;
                current = new float[27];
                currentIndex = 0;

                float val = ParseSHValue(line.Substring(2));
                if (currentIndex < 27)
                    current[currentIndex++] = val;

                continue;
            }

            // Continuation coefficient
            if (line.StartsWith("sh[") && current != null)
            {
                float val = ParseSHValue(line);
                if (currentIndex < 27)
                    current[currentIndex++] = val;
            }
        }

        // Catch last probe
        if (inSH && current != null && currentIndex == 27)
            shList.Add(current);

        return shList;
    }

    static string GenerateLightProbesYAML(List<Vector3> positions,
        List<float[]> shList)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("%YAML 1.1");
        sb.AppendLine("%TAG !u! tag:unity3d.com,2011:");
        sb.AppendLine("--- !u!197 &19700000");
        sb.AppendLine("LightProbes:");
        sb.AppendLine("  m_ObjectHideFlags: 0");
        sb.AppendLine("  m_PrefabParentObject: {fileID: 0}");
        sb.AppendLine("  m_PrefabInternal: {fileID: 0}");
        sb.AppendLine("  m_Name: LightProbes");
        sb.AppendLine("  m_Data:");
        sb.AppendLine("    m_Tetrahedralization:");
        sb.AppendLine("      m_Tetrahedra: []");
        sb.AppendLine("      m_HullRays: []");
        sb.AppendLine("      m_ProbeSets: []");
        sb.AppendLine("    m_Positions:");

        foreach (Vector3 pos in positions)
        {
            sb.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "    - {{x: {0:F7}, y: {1:F7}, z: {2:F7}}}",
                pos.x, pos.y, pos.z));
        }

        sb.AppendLine("    m_NonTetrahedralizedProbeSetIndexMap: []");
        sb.AppendLine("  m_BakedCoefficients:");

        for (int p = 0; p < shList.Count; p++)
        {
            float[] sh = shList[p];
            for (int i = 0; i < 27; i++)
            {
                string prefix = (i == 0) ? "  - " : "    ";
                sb.AppendLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}sh[{1,2}]: {2}",
                    prefix, i, sh[i]));
            }
        }

        sb.AppendLine("  m_BakedLightOcclusion: []");

        return sb.ToString();
    }

    static float ParseSHValue(string line)
    {
        int colonIdx = line.IndexOf(':');
        if (colonIdx < 0) return 0f;

        string valueStr = line.Substring(colonIdx + 1).Trim();
        float result;
        float.TryParse(valueStr,
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
            string trimmed = part.Trim();
            if (trimmed.StartsWith("x:"))
                float.TryParse(trimmed.Substring(2).Trim(),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture, out x);
            else if (trimmed.StartsWith("y:"))
                float.TryParse(trimmed.Substring(2).Trim(),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture, out y);
            else if (trimmed.StartsWith("z:"))
                float.TryParse(trimmed.Substring(2).Trim(),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture, out z);
        }

        return new Vector3(x, y, z);
    }
}