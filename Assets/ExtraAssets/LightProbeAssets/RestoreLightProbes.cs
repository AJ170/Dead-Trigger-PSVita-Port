
using UnityEngine;
using UnityEngine.Rendering;

public class RestoreLightProbes : MonoBehaviour
{
    public LightProbes originalProbes;

    void Start()
    {
        if (originalProbes == null)
            return;

        LightProbes probes = Instantiate(originalProbes);
        LightmapSettings.lightProbes = probes;

        SphericalHarmonicsL2[] original = probes.bakedProbes;
        SphericalHarmonicsL2[] converted = new SphericalHarmonicsL2[original.Length];

        for (int i = 0; i < original.Length; i++)
        {
            float[] values = new float[27];

            for (int color = 0; color < 3; color++)
            {
                for (int coefficient = 0; coefficient < 9; coefficient++)
                    values[color * 9 + coefficient] = original[i][color, coefficient];
            }

            SphericalHarmonicsL2 sh = new SphericalHarmonicsL2();

            for (int color = 0; color < 3; color++)
            {
                for (int coefficient = 0; coefficient < 9; coefficient++)
                    sh[color, coefficient] = values[coefficient * 3 + color];
            }

            converted[i] = sh;
        }

        LightmapSettings.lightProbes.bakedProbes = converted;

        Debug.Log("Converted Light Probes: " + converted.Length);
    }
}
