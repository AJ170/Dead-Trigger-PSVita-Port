using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Globalization;

public class LightProbeManager : MonoBehaviour
{
    public static LightProbeManager Instance { get; private set; }
    /*
    [Header("Data Source")]
    [Tooltip("Assign the LightProbeData ScriptableObject for this level")]
    public LightProbeData probeData;*/

    [Tooltip("Alternatively load directly from a text file at runtime")]
    public TextAsset probeDataTextAsset;

    [Header("Probe Processing")]
    [Tooltip("Overall brightness of the probe lighting")]
    [Range(0, 2)]
    float SHIntensity = 0.5f;

    [Tooltip("How much the probe colour tints vs pure luminance. "
        + "0 = luminance only, 1 = full colour")]
    [Range(0, 1)]
    float SHColorAmount = 0.4f;

    [Tooltip("Bias subtracted from dark areas to increase contrast. "
        + "Scaled by luminance so bright areas are unaffected.")]
    [Range(0, 1)]
    float SHBias = 0.0f;

    // Runtime probe data
    private Vector3[] m_Positions;

    // Pre-processed coefficients ready to pack directly into shader
    // vectors without any per-frame math in the sampler or shader
    private float[][] m_ProcessedCoefficients;

    private bool m_DataReady = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("LightProbeManager: Duplicate instance "
                + "found on " + gameObject.name + ", destroying.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        /*
        if (probeData != null && probeData.probes != null
            && probeData.probes.Length > 0)
        {
            LoadFromScriptableObject();
        }*/
        if (probeDataTextAsset != null)
        {
            LoadFromTextAsset();
        }
        else
        {
            Debug.LogWarning("LightProbeManager: No probe data source "
                + "assigned.");
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /*
    void LoadFromScriptableObject()
    {
        int count = probeData.probes.Length;
        m_Positions = new Vector3[count];
        m_ProcessedCoefficients = new float[count][];

        for (int i = 0; i < count; i++)
        {
            m_Positions[i] = probeData.probes[i].position;
            m_ProcessedCoefficients[i] = ProcessCoefficients(
                probeData.probes[i].shCoefficients);
        }

        m_DataReady = true;
        Debug.Log("LightProbeManager: Loaded and processed "
            + count + " probes from ScriptableObject.");
    }*/

    void LoadFromTextAsset()
    {
        if (probeDataTextAsset == null) return;

        string[] lines = probeDataTextAsset.text.Split('\n');
        List<Vector3> positions = new List<Vector3>();
        List<float[]> rawCoefficients = new List<float[]>();

        bool inPositions = false;
        bool inSH = false;
        float[] current = null;
        int idx = 0;
        bool firstEntry = true;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].TrimStart();

            if (line.StartsWith("m_Positions:"))
            {
                inPositions = true;
                inSH = false;
                continue;
            }
            if (line.StartsWith("m_NonTetrahedralizedProbeSetIndexMap"))
            {
                inPositions = false;
                continue;
            }
            if (line.StartsWith("m_BakedCoefficients:"))
            {
                inSH = true;
                inPositions = false;
                firstEntry = true;
                continue;
            }
            if (line.StartsWith("m_BakedLightOcclusion:"))
            {
                if (inSH && current != null && idx == 27)
                    rawCoefficients.Add(current);
                inSH = false;
                continue;
            }

            if (inPositions && line.StartsWith("- {x:"))
            {
                positions.Add(ParseVector3(line));
                continue;
            }

            if (inSH)
            {
                if (line.StartsWith("- sh["))
                {
                    if (!firstEntry && current != null && idx == 27)
                        rawCoefficients.Add(current);

                    firstEntry = false;
                    current = new float[27];
                    idx = 0;

                    float val = ParseSHValue(line.Substring(2));
                    if (idx < 27) current[idx++] = val;
                    continue;
                }

                if (line.StartsWith("sh[") && current != null)
                {
                    float val = ParseSHValue(line);
                    if (idx < 27) current[idx++] = val;
                }
            }
        }

        if (inSH && current != null && idx == 27)
            rawCoefficients.Add(current);

        if (positions.Count == 0)
        {
            Debug.LogWarning("LightProbeManager: No positions found.");
            return;
        }

        m_Positions = positions.ToArray();
        m_ProcessedCoefficients = new float[rawCoefficients.Count][];

        // Process all coefficients at load time
        // so samplers get pre-baked values with no per-frame math
        for (int i = 0; i < rawCoefficients.Count; i++)
            m_ProcessedCoefficients[i] =
                ProcessCoefficients(rawCoefficients[i]);

        m_DataReady = true;
        Debug.Log("LightProbeManager: Loaded and processed "
            + m_Positions.Length + " probes from TextAsset.");
    }

    // Apply SHIntensity, SHColorAmount and SHBias to the raw
    // coefficients once at load time rather than every pixel every frame
    float[] ProcessCoefficients(float[] raw)
    {
        if (raw == null || raw.Length < 27)
            return new float[27];

        float[] processed = new float[27];

        // Process each group of 3 (RGB per direction)
        for (int i = 0; i < 27; i += 3)
        {
            float r = raw[i];
            float g = raw[i + 1];
            float b = raw[i + 2];

            // Luminance of this direction's colour
            float lum = r * 0.2126f + g * 0.7152f + b * 0.0722f;

            // Lerp between full colour and luminance-only
            // SHColorAmount controls how much colour tint comes through
            float pr = Mathf.Lerp(lum, r, SHColorAmount);
            float pg = Mathf.Lerp(lum, g, SHColorAmount);
            float pb = Mathf.Lerp(lum, b, SHColorAmount);

            // Apply intensity scale
            pr *= SHIntensity;
            pg *= SHIntensity;
            pb *= SHIntensity;

            // Apply bias scaled by luminance so dark probe directions
            // drop toward black while bright directions are preserved
            float scaledBias = SHBias * lum;
            pr -= scaledBias;
            pg -= scaledBias;
            pb -= scaledBias;

            processed[i] = pr;
            processed[i + 1] = pg;
            processed[i + 2] = pb;
        }

        return processed;
    }

    // Main query — interpolates pre-processed SH for a world position
    public bool GetInterpolatedSH(Vector3 worldPosition,
        out float[] coefficients)
    {
        coefficients = new float[27];

        if (!m_DataReady || m_Positions == null
            || m_Positions.Length == 0)
            return false;

        if (m_Positions.Length == 1)
        {
            coefficients = m_ProcessedCoefficients[0];
            return true;
        }

        // Find 4 nearest probes
        int nearCount = Mathf.Min(4, m_Positions.Length);
        int[] nearest = new int[nearCount];
        float[] distances = new float[nearCount];

        for (int i = 0; i < nearCount; i++)
            distances[i] = float.MaxValue;

        for (int i = 0; i < m_Positions.Length; i++)
        {
            float dist = (m_Positions[i] - worldPosition).sqrMagnitude;

            for (int j = 0; j < nearCount; j++)
            {
                if (dist < distances[j])
                {
                    for (int k = nearCount - 1; k > j; k--)
                    {
                        distances[k] = distances[k - 1];
                        nearest[k] = nearest[k - 1];
                    }
                    distances[j] = dist;
                    nearest[j] = i;
                    break;
                }
            }
        }

        // Inverse distance weighting blend
        float totalWeight = 0f;
        float[] weights = new float[nearCount];

        for (int i = 0; i < nearCount; i++)
        {
            float dist = Mathf.Sqrt(distances[i]);

            if (dist < 0.001f)
            {
                // Exactly on a probe — use directly
                coefficients = m_ProcessedCoefficients[nearest[i]];
                return true;
            }

            weights[i] = 1f / dist;
            totalWeight += weights[i];
        }

        // Blend pre-processed coefficients
        for (int i = 0; i < nearCount; i++)
        {
            float w = weights[i] / totalWeight;
            float[] src = m_ProcessedCoefficients[nearest[i]];
            if (src == null) continue;

            for (int c = 0; c < 27; c++)
                coefficients[c] += src[c] * w;
        }

        return true;
    }

    public bool IsReady { get { return m_DataReady; } }

    static Vector3 ParseVector3(string line)
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
}