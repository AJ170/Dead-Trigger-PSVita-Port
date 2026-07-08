using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LightProbeData",
    menuName = "Lighting/Light Probe Data Asset")]
public class LightProbeDataAsset : ScriptableObject
{
    [System.Serializable]
    public struct ProbeEntry
    {
        public Vector3 position;
        // 27 SH coefficients stored flat
        // sh[0..8] = Red, sh[9..17] = Green, sh[18..26] = Blue
        public float[] shCoefficients;
    }

    public ProbeEntry[] probes;

    public int ProbeCount
    {
        get { return probes != null ? probes.Length : 0; }
    }
}