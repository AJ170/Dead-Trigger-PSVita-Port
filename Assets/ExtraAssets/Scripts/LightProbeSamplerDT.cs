// LightProbeSamplerDT.cs
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class LightProbeSamplerDT : MonoBehaviour
{
    [Header("Renderers")]
    public bool populateChildRenderers = false;
    public List<Renderer> renderers = new List<Renderer>();

    [Header("Settings")]
    public float updateDistance = 0.125f;
    public bool useCustomSamplePosition = false;
    public Transform customSamplePosition;

    // Cached property IDs
    private static readonly int SHAr_ID = Shader.PropertyToID("_SHAr");
    private static readonly int SHAg_ID = Shader.PropertyToID("_SHAg");
    private static readonly int SHAb_ID = Shader.PropertyToID("_SHAb");
    private static readonly int SHBr_ID = Shader.PropertyToID("_SHBr");
    private static readonly int SHBg_ID = Shader.PropertyToID("_SHBg");
    private static readonly int SHBb_ID = Shader.PropertyToID("_SHBb");
    private static readonly int SHC_ID = Shader.PropertyToID("_SHC");

    private MaterialPropertyBlock m_PropertyBlock;
    private Vector4[] m_SHArray = new Vector4[7];
    private Vector3 m_LastUpdatePosition = Vector3.zero;
    private float m_SquareUpdateDistance;

    // Reusable coefficient array to avoid per-update allocation
    private float[] m_Coefficients = new float[27];

    void Start()
    {
        m_SquareUpdateDistance = updateDistance * updateDistance;
        m_PropertyBlock = new MaterialPropertyBlock();

        if (renderers.Count == 0)
        {
            Renderer r = GetComponent<Renderer>();
            if (r != null) renderers.Add(r);
        }

        UpdateLighting();
    }

    void OnEnable()
    {
        if (!Application.isPlaying)
            UpdateLighting();
    }

    void Update()
    {
        if (populateChildRenderers)
        {
            CollectChildRenderers(transform);
            populateChildRenderers = false;
        }

        Vector3 samplePos = GetSamplePosition();
        float distSq = (m_LastUpdatePosition - samplePos).sqrMagnitude;

        if (distSq > m_SquareUpdateDistance
            && renderers.Count > 0)
        {
            UpdateLighting();
            m_LastUpdatePosition = samplePos;
        }
    }

    void UpdateLighting()
    {
        // Find manager in scene — no direct reference needed
        LightProbeManager manager = LightProbeManager.Instance;

        if (manager == null || !manager.IsReady)
        {
            // Fallback to Unity's built in system if available
            // otherwise leave as default
            return;
        }

        Vector3 samplePos = GetSamplePosition();

        if (manager.GetInterpolatedSH(samplePos, out m_Coefficients))
        {
            PackSHCoefficients(m_Coefficients, m_SHArray);
            ApplyToRenderers();
        }
    }

    void PackSHCoefficients(float[] sh, Vector4[] shArray)
    {
        if (sh == null || sh.Length < 27) return;

        // YAML SH layout is interleaved: direction-major, channel-minor
        // sh[0]  = L0.r    sh[1]  = L0.g    sh[2]  = L0.b
        // sh[3]  = L1x.r   sh[4]  = L1x.g   sh[5]  = L1x.b
        // sh[6]  = L1y.r   sh[7]  = L1y.g   sh[8]  = L1y.b
        // sh[9]  = L1z.r   sh[10] = L1z.g   sh[11] = L1z.b
        // sh[12] = L2_0.r  sh[13] = L2_0.g  sh[14] = L2_0.b
        // sh[15] = L2_1.r  sh[16] = L2_1.g  sh[17] = L2_1.b
        // sh[18] = L2_2.r  sh[19] = L2_2.g  sh[20] = L2_2.b
        // sh[21] = L2_3.r  sh[22] = L2_3.g  sh[23] = L2_3.b
        // sh[24] = L2_4.r  sh[25] = L2_4.g  sh[26] = L2_4.b

        // Remapping to match Unity's ShadeSH9 shader function:
        // SHAr = (L1x.r, L1y.r, L1z.r, L0.r - L2_2.r)
        // SHAg = (L1x.g, L1y.g, L1z.g, L0.g - L2_2.g)
        // SHAb = (L1x.b, L1y.b, L1z.b, L0.b - L2_2.b)
        // SHBr = (L2_0.r, L2_2.r, L2_1.r*3, L2_3.r)
        // SHBg = (L2_0.g, L2_2.g, L2_1.g*3, L2_3.g)
        // SHBb = (L2_0.b, L2_2.b, L2_1.b*3, L2_3.b)
        // SHC  = (L2_4.r, L2_4.g, L2_4.b, 1)

        shArray[0] = new Vector4(
            sh[3], sh[6], sh[9], sh[0] - sh[18]); // SHAr
        shArray[1] = new Vector4(
            sh[4], sh[7], sh[10], sh[1] - sh[19]); // SHAg
        shArray[2] = new Vector4(
            sh[5], sh[8], sh[11], sh[2] - sh[20]); // SHAb

        shArray[3] = new Vector4(
            sh[12], sh[18], sh[15] * 3f, sh[21]);      // SHBr
        shArray[4] = new Vector4(
            sh[13], sh[19], sh[16] * 3f, sh[22]);      // SHBg
        shArray[5] = new Vector4(
            sh[14], sh[20], sh[17] * 3f, sh[23]);      // SHBb

        shArray[6] = new Vector4(
            sh[24], sh[25], sh[26], 1.0f);             // SHC
    }

    void ApplyToRenderers()
    {
        if (m_PropertyBlock == null)
            m_PropertyBlock = new MaterialPropertyBlock();

        m_PropertyBlock.SetVector(SHAr_ID, m_SHArray[0]);
        m_PropertyBlock.SetVector(SHAg_ID, m_SHArray[1]);
        m_PropertyBlock.SetVector(SHAb_ID, m_SHArray[2]);
        m_PropertyBlock.SetVector(SHBr_ID, m_SHArray[3]);
        m_PropertyBlock.SetVector(SHBg_ID, m_SHArray[4]);
        m_PropertyBlock.SetVector(SHBb_ID, m_SHArray[5]);
        m_PropertyBlock.SetVector(SHC_ID, m_SHArray[6]);

        for (int i = 0; i < renderers.Count; i++)
        {
            if (renderers[i] != null)
                renderers[i].SetPropertyBlock(m_PropertyBlock);
        }
    }

    Vector3 GetSamplePosition()
    {
        return useCustomSamplePosition && customSamplePosition != null
            ? customSamplePosition.position
            : transform.position;
    }

    void CollectChildRenderers(Transform t)
    {
        Renderer r = t.GetComponent<Renderer>();
        if (r != null && !renderers.Contains(r))
            renderers.Add(r);

        foreach (Transform child in t)
            CollectChildRenderers(child);
    }
}