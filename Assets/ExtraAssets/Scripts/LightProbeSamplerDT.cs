using System.Collections.Generic;
using UnityEngine;

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

	// Use Vector4 arrays for SH coefficients
	private Vector4[] m_CurrentSH = new Vector4[7];
	private Vector4[] m_TargetSH = new Vector4[7];

	// Reusable coefficient buffer
	private float[] m_Coefficients = new float[27];

	private Vector3 m_LastSamplePosition = Vector3.zero;
	private Vector3 m_CachedSamplePos = Vector3.zero;
	private float m_SquareUpdateDistance;

	private bool m_LerpInProgress = false;
	private bool m_Dirty = false;
	private bool m_Initialised = false;

	public List<Material> m_InstancedMaterials = new List<Material>();
	private HashSet<Renderer> m_ProcessedRenderers = new HashSet<Renderer>();

	private Material[] m_SharedMaterialsCache;
	private Material[] m_InstancedMaterialsCache;

	// PERF: Track how many renderers we've processed to support dynamic spawning
	private int m_LastProcessedRendererCount = 0;

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
	}

	void InstanciateMaterials()
	{
		// PERF: Process all renderers, skipping ones we've already processed
		for (int i = 0; i < renderers.Count; i++)
		{
			Renderer r = renderers[i];
			if (r == null || m_ProcessedRenderers.Contains(r))
				continue;

			// PERF: Cache to avoid repeated property access
			m_SharedMaterialsCache = r.sharedMaterials;
			int matCount = m_SharedMaterialsCache.Length;

			// PERF: Reuse cached array instead of allocating new one
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

		// PERF: Track how many renderers we've processed
		m_LastProcessedRendererCount = renderers.Count;

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

		// PERF: Check if we have new renderers to process (dynamic spawning support)
		// Don't set instanceMaterials = false; keep it true to handle new spawns
		if (instanceMaterials && Application.isPlaying && renderers.Count > m_LastProcessedRendererCount)
		{
			InstanciateMaterials();
		}

		if (m_InstancedMaterials.Count == 0)
			return;

		m_CachedSamplePos = GetSamplePosition();

		// PERF: Calculate squared distance to avoid expensive sqrt
		Vector3 positionDelta = m_LastSamplePosition - m_CachedSamplePos;
		float distSq = positionDelta.x * positionDelta.x + 
			positionDelta.y * positionDelta.y + 
			positionDelta.z * positionDelta.z;

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

		// Lerp lighting data
		if (m_LerpInProgress)
		{
			float t = lerpSpeed * Time.deltaTime;
			bool stillLerping = false;

			for (int i = 0; i < 7; i++)
			{
				// Lerp each Vector4 component
				m_CurrentSH[i] = Vector4.Lerp(m_CurrentSH[i], m_TargetSH[i], t);

				// Check convergence
				Vector4 diff = new Vector4(
					Mathf.Abs(m_CurrentSH[i].x - m_TargetSH[i].x),
					Mathf.Abs(m_CurrentSH[i].y - m_TargetSH[i].y),
					Mathf.Abs(m_CurrentSH[i].z - m_TargetSH[i].z),
					Mathf.Abs(m_CurrentSH[i].w - m_TargetSH[i].w)
				);

				if (!stillLerping)
				{
					// Check if ANY component exceeds threshold
					if (diff.x > lerpThreshold || diff.y > lerpThreshold || 
						diff.z > lerpThreshold || diff.w > lerpThreshold)
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

		// Set SH coefficients directly from Vector4 array
		m_PropertyBlock.SetVector(SHAr_ID, m_CurrentSH[0]);
		m_PropertyBlock.SetVector(SHAg_ID, m_CurrentSH[1]);
		m_PropertyBlock.SetVector(SHAb_ID, m_CurrentSH[2]);
		m_PropertyBlock.SetVector(SHBr_ID, m_CurrentSH[3]);
		m_PropertyBlock.SetVector(SHBg_ID, m_CurrentSH[4]);
		m_PropertyBlock.SetVector(SHBb_ID, m_CurrentSH[5]);
		m_PropertyBlock.SetVector(SHC_ID, m_CurrentSH[6]);

		for (int i = 0; i < renderers.Count; i++)
		{
			if (renderers[i] != null)
				renderers[i].SetPropertyBlock(m_PropertyBlock);
		}
	}

	// Pack SH coefficients from float array to Vector4 array
	void PackSHCoefficients(float[] sh, Vector4[] shArray)
	{
		if (sh == null || sh.Length < 27)
			return;

		// Pack SH coefficients into Vector4s
		shArray[0] = new Vector4(sh[3], sh[6], sh[9], sh[0] - sh[18]);
		shArray[1] = new Vector4(sh[4], sh[7], sh[10], sh[1] - sh[19]);
		shArray[2] = new Vector4(sh[5], sh[8], sh[11], sh[2] - sh[20]);
		shArray[3] = new Vector4(sh[12], sh[18], sh[15] * 3f, sh[21]);
		shArray[4] = new Vector4(sh[13], sh[19], sh[16] * 3f, sh[22]);
		shArray[5] = new Vector4(sh[14], sh[20], sh[17] * 3f, sh[23]);
		shArray[6] = new Vector4(sh[24], sh[25], sh[26], 1.0f);
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