// LightProbeData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "LightProbeData",
    menuName = "Lighting/Light Probe Data")]
public class LightProbeData : ScriptableObject
{
    [System.Serializable]
    public struct ProbeEntry
    {
        public Vector3 position;
        public float[] shCoefficients; // 27 floats, flat array
    }

    public ProbeEntry[] probes;

    // Find the index of the closest probe to a world position
    public int FindClosestProbeIndex(Vector3 worldPosition)
    {
        if (probes == null || probes.Length == 0)
            return -1;

        float closestDistSq = float.MaxValue;
        int closestIndex = 0;

        for (int i = 0; i < probes.Length; i++)
        {
            float distSq = (probes[i].position - worldPosition)
                .sqrMagnitude;
            if (distSq < closestDistSq)
            {
                closestDistSq = distSq;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    // Get SH coefficients for a world position
    // using trilinear interpolation between nearest probes
    public void GetInterpolatedSH(Vector3 worldPosition,
        out float[] coefficients)
    {
        coefficients = new float[27];

        if (probes == null || probes.Length == 0)
            return;

        if (probes.Length == 1)
        {
            coefficients = probes[0].shCoefficients;
            return;
        }

        // Find 4 nearest probes and blend by inverse distance
        int[] nearest = FindNearestProbes(worldPosition, 4);
        float[] weights = new float[nearest.Length];
        float totalWeight = 0f;

        for (int i = 0; i < nearest.Length; i++)
        {
            float dist = Vector3.Distance(
                worldPosition, probes[nearest[i]].position);

            // Avoid division by zero if exactly on a probe
            if (dist < 0.001f)
            {
                coefficients = probes[nearest[i]].shCoefficients;
                return;
            }

            weights[i] = 1f / dist;
            totalWeight += weights[i];
        }

        // Normalise weights
        for (int i = 0; i < weights.Length; i++)
            weights[i] /= totalWeight;

        // Blend coefficients
        for (int i = 0; i < nearest.Length; i++)
        {
            float[] sourceCoeffs = probes[nearest[i]].shCoefficients;
            if (sourceCoeffs == null) continue;

            for (int c = 0; c < 27; c++)
                coefficients[c] += sourceCoeffs[c] * weights[i];
        }
    }

    int[] FindNearestProbes(Vector3 position, int count)
    {
        count = Mathf.Min(count, probes.Length);
        int[] indices = new int[count];
        float[] distances = new float[count];

        for (int i = 0; i < count; i++)
            distances[i] = float.MaxValue;

        for (int i = 0; i < probes.Length; i++)
        {
            float dist = (probes[i].position - position).sqrMagnitude;

            for (int j = 0; j < count; j++)
            {
                if (dist < distances[j])
                {
                    // Shift existing entries down
                    for (int k = count - 1; k > j; k--)
                    {
                        distances[k] = distances[k - 1];
                        indices[k] = indices[k - 1];
                    }
                    distances[j] = dist;
                    indices[j] = i;
                    break;
                }
            }
        }

        return indices;
    }
}