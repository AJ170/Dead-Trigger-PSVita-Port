#define DEBUG
using System.Collections.Generic;
using UnityEngine;

public class SemanticMaterialManager : MonoBehaviour
{
	private const int MeshMatTable_Dim = 4;

	private const int MeshMatTable_CellsNum = 16;

	private const float MeshMatTable_CellSize = 0.25f;

	private static SemanticMaterialManager m_Instance;

	public SemanticMaterial[] m_Materials;

	private SemanticMaterial[] m_UVTable;

	private List<ResourceCache> m_ImpactCaches;

	private List<Effect> m_Effects = new List<Effect>();

	private Quaternion m_TempQuat = default(Quaternion);

	private GameObject m_AudioObj;

	private AudioSource m_AudioSrc;

	public static SemanticMaterialManager Instance
	{
		get
		{
			return m_Instance;
		}
	}

	public AudioSource Audio
	{
		get
		{
			return m_AudioSrc;
		}
	}

	private void Awake()
	{
		if (!(m_Instance != null))
		{
			m_Instance = this;
			if (m_Materials == null)
			{
			}
			m_AudioObj = new GameObject("AudioEffectSource", typeof(AudioSource));
			m_AudioSrc = m_AudioObj.GetComponent<AudioSource>();
			m_AudioSrc.playOnAwake = false;
			m_AudioSrc.minDistance = 1f;
			m_AudioSrc.maxDistance = 20f;
			m_AudioSrc.rolloffMode = AudioRolloffMode.Linear;
			m_UVTable = new SemanticMaterial[16];
			for (int i = 0; i < 16; i++)
			{
				m_UVTable[i] = null;
			}
			m_UVTable[0] = m_Materials[3];
			m_UVTable[1] = m_Materials[2];
			m_UVTable[2] = m_Materials[9];
			m_UVTable[3] = m_Materials[4];
			m_UVTable[4] = m_Materials[5];
			m_UVTable[5] = m_Materials[6];
			m_UVTable[6] = m_Materials[7];
			m_UVTable[7] = m_Materials[8];
			m_ImpactCaches = new List<ResourceCache>();
			SemanticMaterial[] materials = m_Materials;
			foreach (SemanticMaterial semanticMaterial in materials)
			{
				InitImpactEffectCache(semanticMaterial.m_ProjectileImpact);
			}
			DebugUtils.Assert(m_Materials[0].m_PhyMaterial == null);
		}
	}

	private void InitImpactEffectCache(SemanticMaterial.ProjectileImpact Data)
	{
		if (!(Data.m_Gfx != null))
		{
			return;
		}
		foreach (ResourceCache impactCache in m_ImpactCaches)
		{
			if (impactCache.GetResource() == Data.m_Gfx)
			{
				Data.m_GfxCache = impactCache;
				return;
			}
		}
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		SemanticMaterial[] materials = m_Materials;
		foreach (SemanticMaterial semanticMaterial in materials)
		{
			if (Data.m_Gfx == semanticMaterial.m_ProjectileImpact.m_Gfx)
			{
				num++;
				num2 += semanticMaterial.m_ProjectileImpact.m_ExpectedNum;
				num3 = Mathf.Max(num3, semanticMaterial.m_ProjectileImpact.m_ExpectedNum);
			}
		}
		Data.m_GfxCache = new ResourceCache(Data.m_Gfx, num3, 4);
		m_ImpactCaches.Add(Data.m_GfxCache);
	}

	private void Destroy()
	{
		if (m_Instance == this)
		{
			m_Instance = null;
		}
	}

	private void Update()
	{
		for (int num = m_Effects.Count - 1; num >= 0; num--)
		{
			if (!m_Effects[num].IsActive())
			{
				m_Effects[num].ShutDown();
				m_Effects.RemoveAt(num);
			}
		}
	}

	public void SpawnPlacementEffect(RaycastHit HitInfo)
	{
		SemanticMaterial material = GetMaterial(HitInfo);
		AudioClip gadgetsSticking = material.m_GadgetsSticking;
		SpawnEffect(gadgetsSticking, HitInfo.point);
	}

	public void SpawnPlacementEffect(Vector3 Pos)
	{
		RaycastHit hitInfo;
		if (Physics.Raycast(Pos + Vector3.up * 0.2f, -Vector3.up, out hitInfo, 0.4f))
		{
			SemanticMaterial material = GetMaterial(hitInfo);
			AudioClip gadgetsSticking = material.m_GadgetsSticking;
			SpawnEffect(gadgetsSticking, Pos);
		}
	}

	public void SpawnImpactEffect(Collision Coll)
	{
		float num = 1f;
		if (!(Coll.relativeVelocity.sqrMagnitude < num))
		{
			SemanticMaterial material = GetMaterial(Coll);
			int index = Random.Range(0, material.m_GrenadeImpacts.Count);
			AudioClip sfxEffect = material.m_GrenadeImpacts[index];
			SpawnEffect(sfxEffect, Coll.contacts[0].point);
		}
	}

	public void SpawnImpactEffect(Vector3 Pos, Vector3 Normal, E_SemanticMaterialID ID)
	{
		SemanticMaterial material = GetMaterial(ID);
		int index = Random.Range(0, material.m_GrenadeImpacts.Count);
		AudioClip sfxEffect = material.m_GrenadeImpacts[index];
		ResourceCache gfxEffectCache = ((ID != E_SemanticMaterialID.Water) ? null : material.m_ProjectileImpact.m_GfxCache);
		SpawnEffect(gfxEffectCache, sfxEffect, Pos, Normal);
	}

	public void SpawnProjectileImpactEffect(E_ProjectileType ProjType, RaycastHit HitInfo)
	{
		SemanticMaterial material = GetMaterial(HitInfo);
		// Map surfaces resolve via HitInfo.textureCoord -> m_UVTable, which has
		// null entries (slots 8-15) and null-GFX materials (Fabric/Glass/Paper).
		// Fall back to the Default material so map hits always get an effect.
		if (material == null || material.m_ProjectileImpact.m_GfxCache == null)
		{
			material = m_Materials[0];
		}
		SemanticMaterial.ProjectileImpact projectileImpactData = material.GetProjectileImpactData(ProjType);
		AudioClip sfxEffect = null;
		if (projectileImpactData.m_Sfx != null && projectileImpactData.m_Sfx.Count > 0)
		{
			sfxEffect = projectileImpactData.m_Sfx[Random.Range(0, projectileImpactData.m_Sfx.Count)];
		}
		SpawnEffect(projectileImpactData.m_GfxCache, sfxEffect, HitInfo.point, HitInfo.normal);
	}

	private void SpawnEffect(AudioClip SfxEffect, Vector3 Pos)
	{
		if (SfxEffect != null)
		{
			m_AudioObj.transform.position = Pos;
			m_AudioSrc.PlayOneShot(SfxEffect);
		}
	}

	private void SpawnEffect(ResourceCache GfxEffectCache, AudioClip SfxEffect, Vector3 Pos, Vector3 Dir)
	{
		if (GfxEffectCache != null)
		{
			GameObject gameObject = GfxEffectCache.Get();
			if (gameObject != null)
			{
				m_TempQuat.SetLookRotation(Dir);
				gameObject._SetActiveRecursively(true);
				gameObject.transform.position = Pos;
				gameObject.transform.rotation = m_TempQuat;
				// Don't rely on each prefab's "Play On Awake" - explicitly (re)play.
				ParticleSystem[] particleSystems = gameObject.GetComponentsInChildren<ParticleSystem>(true);
				foreach (ParticleSystem ps in particleSystems)
				{
					ps.Clear(true);
					ps.Play(true);
				}
				m_Effects.Add(new Effect(gameObject, GfxEffectCache));
			}
		}
		if (SfxEffect != null)
		{
			m_AudioObj.transform.position = Pos;
			m_AudioSrc.PlayOneShot(SfxEffect);
		}
	}

	public SemanticMaterial GetMaterial(E_SemanticMaterialID ID)
	{
		return m_Materials[(int)ID];
	}

	public SemanticMaterial GetMaterial(PhysicMaterial PhyMat)
	{
		SemanticMaterial[] materials = m_Materials;
		foreach (SemanticMaterial semanticMaterial in materials)
		{
			if (semanticMaterial.m_PhyMaterial == PhyMat)
			{
				return semanticMaterial;
			}
		}
		return m_Materials[0];
	}

	public SemanticMaterial GetMaterial(Collision Coll)
	{
		SemanticMaterial semanticMaterial = null;
		MeshCollider meshCollider = Coll.collider as MeshCollider;
		if (meshCollider != null)
		{
			ContactPoint contactPoint = Coll.contacts[0];
			Ray ray = new Ray(contactPoint.point + contactPoint.normal, -contactPoint.normal);
			RaycastHit hitInfo;
			if (meshCollider.Raycast(ray, out hitInfo, 2f))
			{
				semanticMaterial = GetMaterial(hitInfo.textureCoord);
			}
		}
		if (semanticMaterial == null)
		{
			semanticMaterial = GetMaterial(Coll.collider.sharedMaterial);
		}
		return semanticMaterial;
	}

	public SemanticMaterial GetMaterial(RaycastHit HitInfo)
	{
		SemanticMaterial semanticMaterial = null;
		if (HitInfo.collider is MeshCollider)
		{
			semanticMaterial = GetMaterial(HitInfo.textureCoord);
		}
		if (semanticMaterial == null)
		{
			semanticMaterial = GetMaterial(HitInfo.collider.sharedMaterial);
		}
		return semanticMaterial;
	}

	private SemanticMaterial GetMaterial(Vector2 UV)
	{
		if (0f <= UV.x && UV.x <= 1f && 0f <= UV.y && UV.y <= 1f)
		{
			float num = 0.2501f;
			int num2 = Mathf.FloorToInt(UV.x / num);
			int num3 = Mathf.FloorToInt((1f - UV.y) / num);
			int num4 = num2 + num3 * 4;
			return m_UVTable[num4];
		}
		return null;
	}
}
