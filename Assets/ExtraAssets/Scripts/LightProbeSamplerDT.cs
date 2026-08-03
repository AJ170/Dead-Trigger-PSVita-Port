using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class LightProbeSamplerDT : MonoBehaviour
{
	[Header("Renderers")]
	public bool populateChildRenderers = false;
	public bool instanceMaterials = true;	//this has become something of a catch given that some objects need to be setup as prefabs, also with the new DT system for everything
	public List<Renderer> renderers = new List<Renderer>();

	[Header("Update Settings")]
	[Tooltip("How far the object must move before resampling probes")]
	public float updateDistance = 0.125f;

	[Tooltip("Speed at which SH lighting lerps to new values after "
		+ "a probe resample. Higher = snappier, lower = smoother.")]
	public float lerpSpeed = 5.0f;

	[Tooltip("How close the lerp needs to be before we stop "
		+ "updating the property block each frame")]
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

	// Current SH values being applied to renderers this frame
	private Vector4[] m_CurrentSH = new Vector4[7];

	// Target SH values from the most recent probe sample
	private Vector4[] m_TargetSH = new Vector4[7];

	// Reusable coefficient buffer - avoids per-update allocation
	private float[] m_Coefficients = new float[27];

	private Vector3 m_LastSamplePosition = Vector3.zero;
	private float m_SquareUpdateDistance;

	// True when current SH differs from target and needs lerping
	private bool m_LerpInProgress = false;

	// True when the property block needs pushing to renderers
	private bool m_Dirty = false;

	// True once we have at least one valid sample
	private bool m_Initialised = false;

	// Track instanced materials so we can clean them up on destroy
	public List<Material> m_InstancedMaterials = new List<Material>();

	// Track which renderers have already been processed to avoid re-instantiation
	private HashSet<Renderer> m_ProcessedRenderers = new HashSet<Renderer>();

	void Start()
	{
		m_SquareUpdateDistance = updateDistance * updateDistance;
		m_PropertyBlock = new MaterialPropertyBlock();

		if (renderers.Count == 0)
		{
			Renderer r = GetComponent<Renderer>();
			if (r != null) renderers.Add(r);
		}

		// Only instance materials on start if the flag is set
		// This allows users to manually configure renderers first
		if (instanceMaterials)
		{
			InstanciateMaterials();
		}

		// Force an immediate sample and apply on start
		// bypassing the lerp so there's no fade-in from black
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
		// Clean up instanced materials to avoid memory leaks
		// Only destroy in play mode - in edit mode Unity manages them
		if (Application.isPlaying)
		{
			for (int i = 0; i < m_InstancedMaterials.Count; i++)
			{
				if (m_InstancedMaterials[i] != null)
					Destroy(m_InstancedMaterials[i]);
			}
		}
		m_InstancedMaterials.Clear();
		m_ProcessedRenderers.Clear();
	}

	void InstanciateMaterials()
	{
		// Only instantiate materials for renderers that haven't been processed yet
		// This prevents scrubbing already-setup materials
		for (int i = 0; i < renderers.Count; i++)
		{
			Renderer r = renderers[i];
			if (r == null || m_ProcessedRenderers.Contains(r))
				continue;

			// Get the shared materials array
			Material[] sharedMats = r.sharedMaterials;
			Material[] instancedMats = new Material[sharedMats.Length];

			for (int m = 0; m < sharedMats.Length; m++)
			{
				if (sharedMats[m] != null)
				{
					// Create a per-object instance of each material
					Material instance = new Material(sharedMats[m]);
					instance.name = sharedMats[m].name + "_Instance_"
						+ gameObject.name;
					instancedMats[m] = instance;
					m_InstancedMaterials.Add(instance);
				}
				else
				{
					instancedMats[m] = null;
				}
			}

			// Assign instanced materials to the renderer
			r.materials = instancedMats;
			m_ProcessedRenderers.Add(r);
		}

		if (m_ProcessedRenderers.Count > 0)
		{
			Debug.Log("LightProbeSamplerDT: Instanced "
				+ m_ProcessedRenderers.Count
				+ " renderers on " + gameObject.name);
		}
	}

	void Update()
	{
		if (populateChildRenderers) {
			CollectChildRenderers (transform);
			populateChildRenderers = false;
		}
		if (instanceMaterials && renderers.Count > 0) {
			if (Application.isPlaying) {
				instanceMaterials = false;
				// Instance materials for any newly added renderers
				// (only processes renderers not already handled)
				InstanciateMaterials ();
			}
		}

		//Junk out early if we don't have anything to apply the effect to
		if (m_InstancedMaterials.Count == 0) {
			return;
		}

		Vector3 samplePos = GetSamplePosition();
		float distSq = (m_LastSamplePosition - samplePos).sqrMagnitude;

		// Resample probes if we've moved past the threshold
		if (distSq > m_SquareUpdateDistance && renderers.Count > 0)
		{
			if (ForceSample())
			{
				m_LastSamplePosition = samplePos;

				if (!m_Initialised)
				{
					// First sample - snap directly, no lerp
					CopyTargetToCurrent();
					PushToRenderers();
					m_Initialised = true;
				}
				else
				{
					// Subsequent samples - start lerping
					m_LerpInProgress = true;
				}
			}
		}

		// Lerp current SH toward target if needed
		if (m_LerpInProgress)
		{
			float t = lerpSpeed * Time.deltaTime;
			bool stillLerping = false;

			for (int i = 0; i < 7; i++)
			{
				m_CurrentSH[i] = Vector4.Lerp(
					m_CurrentSH[i], m_TargetSH[i], t);

				if (!stillLerping)
				{
					Vector4 diff = m_CurrentSH[i] - m_TargetSH[i];
					if (Mathf.Abs(diff.x) > lerpThreshold
						|| Mathf.Abs(diff.y) > lerpThreshold
						|| Mathf.Abs(diff.z) > lerpThreshold
						|| Mathf.Abs(diff.w) > lerpThreshold)
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

	// Sample the LightProbeManager and update m_TargetSH
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

	// Push current SH values to all renderers via property block
	// MaterialPropertyBlock works correctly on instanced materials
	// since each renderer now has its own material copy
	void PushToRenderers()
	{
		if (m_PropertyBlock == null)
			m_PropertyBlock = new MaterialPropertyBlock();

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

	void PackSHCoefficients(float[] sh, Vector4[] shArray)
	{
		if (sh == null || sh.Length < 27) return;

		// YAML SH layout: direction-major, channel-minor
		// sh[0]  = L0.r    sh[1]  = L0.g    sh[2]  = L0.b
		// sh[3]  = L1x.r   sh[4]  = L1x.g   sh[5]  = L1x.b
		// sh[6]  = L1y.r   sh[7]  = L1y.g   sh[8]  = L1y.b
		// sh[9]  = L1z.r   sh[10] = L1z.g   sh[11] = L1z.b
		// sh[12] = L2_0.r  sh[13] = L2_0.g  sh[14] = L2_0.b
		// sh[15] = L2_1.r  sh[16] = L2_1.g  sh[17] = L2_1.b
		// sh[18] = L2_2.r  sh[19] = L2_2.g  sh[20] = L2_2.b
		// sh[21] = L2_3.r  sh[22] = L2_3.g  sh[23] = L2_3.b
		// sh[24] = L2_4.r  sh[25] = L2_4.g  sh[26] = L2_4.b

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