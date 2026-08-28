using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[ExecuteInEditMode]
public class LightProbeSamplerDT : MonoBehaviour
{
	[Header("Renderers")]
	public bool populateChildRenderers = false;
	public bool instanceMaterials = true;
	public List<Renderer> renderers = new List<Renderer>();

	[Header("Update Settings")]
	[Tooltip("How far the object must move before resampling probes")]
	public float updateDistance = 0.125f;

	[Tooltip("Speed at which SH lighting lerps to new values after a probe resample. Higher = snappier, lower = smoother.")]
	public float lerpSpeed = 5.0f;

	[Tooltip("How close the lerp needs to be before we stop updating the property block each frame")]
	[Range(0.0001f, 0.01f)]
	public float lerpThreshold = 0.001f;

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

	// Use float4 for math operations - SIMD optimized
	private float4[] m_CurrentSH = new float4[7];
	private float4[] m_TargetSH = new float4[7];

	// Reusable coefficient buffer
	private float[] m_Coefficients = new float[27];

	private float3 m_LastSamplePosition = float3(0,0,0);
	private float3 m_CachedSamplePos = float3(0, 0, 0);
	private float m_SquareUpdateDistance;

	private bool m_LerpInProgress = false;
	private bool m_Dirty = false;
	private bool m_Initialised = false;

	public List<Material> m_InstancedMaterials = new List<Material>();
	private HashSet<Renderer> m_ProcessedRenderers = new HashSet<Renderer>();

	private Material[] m_SharedMaterialsCache;
	private Material[] m_InstancedMaterialsCache;

	// Reusable Vector4 array for conversion back to renderer
	private Vector4[] m_Vector4Buffer = new Vector4[7];

	void Start()
	{
		m_SquareUpdateDistance = updateDistance * updateDistance;
		m_PropertyBlock = new MaterialPropertyBlock();

		if (renderers.Count == 0)
		{
			Renderer r = GetComponent<Renderer>();
			if (r != null) renderers.Add(r);
		}

		if (instanceMaterials)
		{
			InstanciateMaterials();
		}

		if (ForceSample())
		{
			CopyTargetToCurrent();
			PushToRenderers();
			m_Initialised = true;
		}
	}

	void OnEnable()
	{
		if (!Application.isPlaying)
		{
			ForceSample();
			CopyTargetToCurrent();
			PushToRenderers();
		}
	}

	void OnDestroy()
	{
		m_InstancedMaterials.Clear();
		m_ProcessedRenderers.Clear();
		m_SharedMaterialsCache = null;
		m_InstancedMaterialsCache = null;
		m_Vector4Buffer = null;
	}

	void InstanciateMaterials()
	{
		for (int i = 0; i < renderers.Count; i++)
		{
			Renderer r = renderers[i];
			if (r == null || m_ProcessedRenderers.Contains(r))
				continue;

			m_SharedMaterialsCache = r.sharedMaterials;
			int matCount = m_SharedMaterialsCache.Length;

			if (m_InstancedMaterialsCache == null || m_InstancedMaterialsCache.Length != matCount)
				m_InstancedMaterialsCache = new Material[matCount];

			for (int m = 0; m < matCount; m++)
			{
				if (m_SharedMaterialsCache[m] != null)
				{
					Material instance = new Material(m_SharedMaterialsCache[m]);
					instance.name = m_SharedMaterialsCache[m].name + "_Instance_" + gameObject.name;
					m_InstancedMaterialsCache[m] = instance;
					m_InstancedMaterials.Add(instance);
				}
				else
				{
					m_InstancedMaterialsCache[m] = null;
				}
			}

			r.materials = m_InstancedMaterialsCache;
			m_ProcessedRenderers.Add(r);
		}

#if UNITY_EDITOR
		if (m_ProcessedRenderers.Count > 0)
		{
			Debug.Log("LightProbeSamplerDT: Instanced " + m_ProcessedRenderers.Count + " renderers on " + gameObject.name);
		}
#endif
	}

	void Update()
	{
		if (populateChildRenderers)
		{
			CollectChildRenderers(transform);
			populateChildRenderers = false;
		}

		if (instanceMaterials && renderers.Count > 0)
		{
			if (Application.isPlaying)
			{
				instanceMaterials = false;
				InstanciateMaterials();
			}
		}

		if (m_InstancedMaterials.Count == 0)
			return;

		m_CachedSamplePos = (float3)GetSamplePosition();
		float distSq = lengthsq(m_LastSamplePosition - m_CachedSamplePos);

		// Resample probes if we've moved past the threshold
		if (distSq > m_SquareUpdateDistance && renderers.Count > 0)
		{
			if (ForceSample())
			{
				m_LastSamplePosition = m_CachedSamplePos;

				if (!m_Initialised)
				{
					CopyTargetToCurrent();
					PushToRenderers();
					m_Initialised = true;
				}
				else
				{
					m_LerpInProgress = true;
				}
			}
		}

		// Lerp using float4 math - SIMD vectorized
		if (m_LerpInProgress)
		{
			float t = lerpSpeed * Time.deltaTime;
			bool stillLerping = false;

			for (int i = 0; i < 7; i++)
			{
				// Use math.lerp on float4 - processes all components at once
				m_CurrentSH[i] = lerp(m_CurrentSH[i], m_TargetSH[i], t);

				// Check convergence using float4 abs - vectorized
				float4 diff = abs(m_CurrentSH[i] - m_TargetSH[i]);

				if (!stillLerping)
				{
					// Use any() to check if ANY component exceeds threshold
					if (any(diff > lerpThreshold))
					{
						stillLerping = true;
					}
				}
			}

			if (!stillLerping)
			{
				CopyTargetToCurrent();
				m_LerpInProgress = false;
			}

			m_Dirty = true;
		}

		if (m_Dirty)
		{
			PushToRenderers();
			m_Dirty = false;
		}
	}

	bool ForceSample()
	{
		LightProbeManager manager = LightProbeManager.Instance;

		if (manager == null || !manager.IsReady)
			return false;

		Vector3 samplePos = GetSamplePosition();

		if (!manager.GetInterpolatedSH(samplePos, out m_Coefficients))
			return false;

		PackSHCoefficients(m_Coefficients, m_TargetSH);
		return true;
	}

	void CopyTargetToCurrent()
	{
		for (int i = 0; i < 7; i++)
			m_CurrentSH[i] = m_TargetSH[i];
	}

	void PushToRenderers()
	{
		if (m_PropertyBlock == null)
			m_PropertyBlock = new MaterialPropertyBlock();

		// Convert float4 array to Vector4 buffer once
		// This is the ONLY place we allocate Vector4s, and we reuse the buffer
		for (int i = 0; i < 7; i++)
		{
			m_Vector4Buffer[i] = (Vector4)m_CurrentSH[i];
		}

		m_PropertyBlock.SetVector(SHAr_ID, m_Vector4Buffer[0]);
		m_PropertyBlock.SetVector(SHAg_ID, m_Vector4Buffer[1]);
		m_PropertyBlock.SetVector(SHAb_ID, m_Vector4Buffer[2]);
		m_PropertyBlock.SetVector(SHBr_ID, m_Vector4Buffer[3]);
		m_PropertyBlock.SetVector(SHBg_ID, m_Vector4Buffer[4]);
		m_PropertyBlock.SetVector(SHBb_ID, m_Vector4Buffer[5]);
		m_PropertyBlock.SetVector(SHC_ID, m_Vector4Buffer[6]);

		for (int i = 0; i < renderers.Count; i++)
		{
			if (renderers[i] != null)
				renderers[i].SetPropertyBlock(m_PropertyBlock);
		}
	}

	// Pack SH coefficients using float4 - no Vector4 allocations
	void PackSHCoefficients(float[] sh, float4[] shArray)
	{
		if (sh == null || sh.Length < 27)
			return;

		// Use float4 constructor for vectorized packing
		shArray[0] = new float4(sh[3], sh[6], sh[9], sh[0] - sh[18]);
		shArray[1] = new float4(sh[4], sh[7], sh[10], sh[1] - sh[19]);
		shArray[2] = new float4(sh[5], sh[8], sh[11], sh[2] - sh[20]);
		shArray[3] = new float4(sh[12], sh[18], sh[15] * 3f, sh[21]);
		shArray[4] = new float4(sh[13], sh[19], sh[16] * 3f, sh[22]);
		shArray[5] = new float4(sh[14], sh[20], sh[17] * 3f, sh[23]);
		shArray[6] = new float4(sh[24], sh[25], sh[26], 1.0f);
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