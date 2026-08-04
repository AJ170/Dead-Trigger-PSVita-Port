#define DEBUG
using System.Collections.Generic;
using UnityEngine;

public class ScreenDrops : MonoBehaviour
{
	private struct DecalInfo
	{
		public Vector2 m_Pos;

		public Vector2 m_Size;

		public Vector2 m_UVMin;

		public Vector2 m_UVMax;

		public float m_Rot;

		public float m_SpawnTime;

		public float m_Duration;

		public bool m_IsDrop;

		public int m_Id;

		public float m_VelY;
	}

	public static ScreenDrops Instance;

	public Vector2 m_DropMinSize = new Vector2(0.05f, 0.05f);

	public Vector2 m_DropMaxSize = new Vector2(0.26f, 0.26f);

	public float m_DropsDuration = 3.5f;

	public float m_DropsDurationVariation = 0.6f;

	public uint m_MaxVisibleDrops = 24u;

	protected MeshFilter m_MeshFilter;

	protected MeshRenderer m_MeshRenderer;

	private Material m_Material;

	protected Mesh m_Mesh;

	protected uint m_DecalsVersion;

	protected uint m_BuffersVersion;

	protected float m_AspectRatio = 1f;

	protected bool m_PrevDbgBtnState;

	protected uint m_NumSpawnedDrops;

	public int m_DropsTexNumTiles = 2;

	private List<DecalInfo> m_Decals = new List<DecalInfo>();

	private void Awake()
	{
		if (!Instance)
		{
			Instance = this;
			InitMeshes();
		}
	}

	// ScreenDrops is not placed in any scene, so create it on demand (e.g. the
	// first time the player walks under a water-drip trigger).
	public static ScreenDrops EnsureInstance()
	{
		if (Instance == null)
		{
			GameObject gameObject = new GameObject("ScreenDrops");
			gameObject.AddComponent<ScreenDrops>();
		}
		return Instance;
	}

	private void OnDestroy()
	{
		Instance = null;
	}

	private void LateUpdate()
	{
		if (Camera.main == null)
		{
			return;
		}
		m_AspectRatio = (float)Screen.width / (float)Screen.height;
		KillOldDecals();
		if (m_Decals.Count > 0)
		{
			// Rebuild every frame so the per-drop fade (vertex.z) updates smoothly.
			UpdateMeshBuffers();
			m_BuffersVersion = m_DecalsVersion;
		}
		else if (m_BuffersVersion != m_DecalsVersion)
		{
			UpdateMeshBuffers();
			m_BuffersVersion = m_DecalsVersion;
		}
		// Revive the WaterDropletsMgr behaviour: the refraction image effect is only
		// enabled while drops are actually on screen.
		if (MFRefractionEffects.Instance != null)
		{
			bool wantEnabled = m_Decals.Count > 0;
			if (MFRefractionEffects.Instance.enabled != wantEnabled)
			{
				MFRefractionEffects.Instance.enabled = wantEnabled;
			}
		}
	}

	private void InitMeshes()
	{
		base.gameObject.AddComponent<MeshFilter>();
		base.gameObject.AddComponent<MeshRenderer>();
		base.gameObject.name = "ScreenDrops";
		m_MeshFilter = (MeshFilter)GetComponent(typeof(MeshFilter));
		m_MeshRenderer = (MeshRenderer)GetComponent(typeof(MeshRenderer));
		// ScreenDrops only builds the droplet mesh. MFRefractionEffects draws it
		// through the screen-space refraction image effect (so the drops distort
		// the scene behind them, matching the original), so we don't render it here.
		m_MeshRenderer.enabled = false;
		m_Mesh = m_MeshFilter.mesh;
	}

	private void UpdateMeshBuffers()
	{
		int num = m_Decals.Count * 4 + 2;
		int num2 = m_Decals.Count * 2;
		Vector3[] array = new Vector3[num];
		Vector2[] array2 = new Vector2[num];
		Vector2[] array3 = new Vector2[num];
		int[] array4 = new int[num2 * 3];
		int num3 = 0;
		int num4 = 0;
		foreach (DecalInfo decal in m_Decals)
		{
			array4[num4++] = 3 + num3;
			array4[num4++] = 1 + num3;
			array4[num4++] = 0 + num3;
			array4[num4++] = 3 + num3;
			array4[num4++] = 2 + num3;
			array4[num4++] = 1 + num3;
			float num5 = Mathf.Cos(decal.m_Rot);
			float num6 = Mathf.Sin(decal.m_Rot);
			// The refraction shader reads vertex.z as the drop intensity: 1 at spawn,
			// fading to 0 at the end of its lifetime so drops fade out instead of popping.
			float dropElapsed = Time.timeSinceLevelLoad - decal.m_SpawnTime;
			float z = Mathf.Clamp01(1f - dropElapsed / decal.m_Duration);
			Vector2 size = decal.m_Size;
			// Slide the drop down the screen over its lifetime.
			Vector2 dropPos = decal.m_Pos;
			dropPos.y -= decal.m_VelY * dropElapsed;
			Vector2 vector = dropPos - size * 0.5f;
			float y = (decal.m_IsDrop ? 1 : 0);
			float num7 = vector[0] + 0.5f * size[0];
			float num8 = vector[1] + 0.5f * size[1];
			float num9 = vector[0];
			float num10 = vector[1];
			array[num3].x = num7 + (num9 - num7) * num5 - (num10 - num8) * num6;
			array[num3].y = num8 + ((num9 - num7) * num6 + (num10 - num8) * num5) * m_AspectRatio;
			array[num3].z = z;
			array2[num3].x = -1f;
			array2[num3].y = -1f;
			array3[num3].x = decal.m_Duration;
			array3[num3].y = y;
			num3++;
			num9 = vector[0] + size[0];
			num10 = vector[1];
			array[num3].x = num7 + (num9 - num7) * num5 - (num10 - num8) * num6;
			array[num3].y = num8 + ((num9 - num7) * num6 + (num10 - num8) * num5) * m_AspectRatio;
			array[num3].z = z;
			array2[num3].x = 1f;
			array2[num3].y = -1f;
			array3[num3].x = decal.m_Duration;
			array3[num3].y = y;
			num3++;
			num9 = vector[0] + size[0];
			num10 = vector[1] + size[1];
			array[num3].x = num7 + (num9 - num7) * num5 - (num10 - num8) * num6;
			array[num3].y = num8 + ((num9 - num7) * num6 + (num10 - num8) * num5) * m_AspectRatio;
			array[num3].z = z;
			array2[num3].x = 1f;
			array2[num3].y = 1f;
			array3[num3].x = decal.m_Duration;
			array3[num3].y = 0f;
			num3++;
			num9 = vector[0];
			num10 = vector[1] + size[1];
			array[num3].x = num7 + (num9 - num7) * num5 - (num10 - num8) * num6;
			array[num3].y = num8 + ((num9 - num7) * num6 + (num10 - num8) * num5) * m_AspectRatio;
			array[num3].z = z;
			array2[num3].x = -1f;
			array2[num3].y = 1f;
			array3[num3].x = decal.m_Duration;
			array3[num3].y = 0f;
			num3++;
		}
		array[num3].x = -9999f;
		array[num3].y = -9999f;
		array[num3].z = -9999f;
		num3++;
		array[num3].x = 9999f;
		array[num3].y = 9999f;
		array[num3].z = 9999f;
		num3++;
		m_Mesh.Clear();
		m_Mesh.vertices = array;
		m_Mesh.uv = array2;
		m_Mesh.uv2 = array3;
		m_Mesh.triangles = array4;
		m_Mesh.RecalculateBounds();
	}

	private void KillOldDecals()
	{
		float time = Time.timeSinceLevelLoad;
		uint num = 0u;
		for (int num2 = m_Decals.Count - 1; num2 >= 0; num2--)
		{
			if (time - m_Decals[num2].m_SpawnTime > m_Decals[num2].m_Duration)
			{
				if (m_Decals[num2].m_IsDrop)
				{
					DebugUtils.Assert(m_NumSpawnedDrops != 0);
					m_NumSpawnedDrops--;
				}
				m_Decals.RemoveAt(num2);
				num++;
			}
		}
		if (num != 0)
		{
			m_DecalsVersion++;
		}
	}

	private void KillDecalById(int id)
	{
		for (int num = m_Decals.Count - 1; num >= 0; num--)
		{
			if (m_Decals[num].m_Id == id)
			{
				m_Decals.RemoveAt(num);
			}
		}
	}

	public void SpawnDrops(uint cnt)
	{
		float time = Time.timeSinceLevelLoad;
		float num = 1f / (float)m_DropsTexNumTiles;
		for (uint num2 = 0u; num2 < cnt; num2++)
		{
			if (m_NumSpawnedDrops >= m_MaxVisibleDrops)
			{
				break;
			}
			DecalInfo item = default(DecalInfo);
			float value = Random.Range(m_DropMinSize[0], m_DropMaxSize[0]);
			item.m_Pos[0] = Random.Range(-1f, 1f);
			item.m_Pos[1] = Random.Range(-1f, 1f);
			item.m_Size[0] = value;
			// Taller than wide -> teardrop-ish, with per-drop variation.
			item.m_Size[1] = value * Random.Range(1.1f, 1.5f);
			// Downward drift speed (NDC units/sec); bigger drops run faster.
			item.m_VelY = Random.Range(0.14f, 0.3f) + value * 0.6f;
			int num3 = Random.Range(0, m_DropsTexNumTiles);
			int num4 = Random.Range(0, m_DropsTexNumTiles);
			item.m_UVMin[0] = (float)num3 * num;
			item.m_UVMin[1] = (float)num4 * num;
			item.m_UVMax[0] = item.m_UVMin[0] + num;
			item.m_UVMax[1] = item.m_UVMin[1] + num;
			item.m_Rot = 0f;
			item.m_SpawnTime = time;
			item.m_Duration = m_DropsDuration + Random.Range(0f, m_DropsDuration * m_DropsDurationVariation);
			item.m_IsDrop = true;
			item.m_Id = -1;
			m_NumSpawnedDrops++;
			m_Decals.Add(item);
		}
		m_DecalsVersion++;
	}

	private void DbgEmitDecals()
	{
		bool button = Input.GetButton("Fire1");
		if (button && !m_PrevDbgBtnState)
		{
			SpawnDrops(30u);
		}
		m_PrevDbgBtnState = button;
	}

	public int NumDecals()
	{
		return m_Decals.Count;
	}

	public Mesh GetMesh()
	{
		return m_Mesh;
	}
}
