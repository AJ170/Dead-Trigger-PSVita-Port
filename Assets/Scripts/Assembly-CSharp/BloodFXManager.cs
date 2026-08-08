#define DEBUG
using System;
using System.Collections.Generic;
using UnityEngine;

public class BloodFXManager : MonoBehaviour
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
	}

	public static BloodFXManager Instance;

	public GameObject m_BloodOverlayMeshGameObj;

	public Material m_Material;

	public Vector2 m_MinSize = new Vector2(0.5f, 0.5f);

	public Vector2 m_MaxSize = new Vector2(0.6f, 0.6f);

	public Vector2 m_DropMinSize = new Vector2(0.1f, 0.1f);

	public Vector2 m_DropMaxSize = new Vector2(0.2f, 0.2f);

	public float m_Duration = 5f;

	public float m_DropsDuration = 2.5f;

	public float m_DurationVariation = 0.1f;

	public float m_DropsDurationVariation = 0.1f;

	public uint m_MaxVisibleDrops = 10u;

	public uint m_MaxVisibleSplashes = 15u;

	public float m_HurtHealthThreshold = 0.8f;

	public float m_HeartBeatFreqScale = 1f;

	protected MeshFilter m_MeshFilter;

	protected MeshRenderer m_MeshRenderer;

	protected Mesh m_Mesh;

	protected uint m_DecalsVersion;

	protected uint m_BuffersVersion;

	protected float m_AspectRatio = 1f;

	protected bool m_PrevDbgBtnState;

	protected int m_NumSplashesDropTexTilesU = 2;

	protected int m_NumSplashesDropTexTilesV = 2;

	protected uint m_NumSpawnedDrops;

	protected uint m_NumSpawnedSplashes;

	protected int m_CurrHurtLevel;

	protected int m_NumHurtLevels = 6;

	protected float m_CurrHealth = 1f;

	protected int[] m_DecalPosIdxPerHurtLevels = new int[6] { 3, 5, 0, 2, 4, 1 };

	protected int m_DbgHurtLevel = -1;

	protected float m_DbgHealth = 1f;

	protected GameObject m_BloodOverlayMeshGOInst;

	protected int m_BloodDropsTexNumTilesU = 4;

	protected int m_BloodDropsTexNumTilesV = 1;

	private float TimeSinceStart;

	private System.Random m_Random = new System.Random((int)DateTime.Now.Ticks);

	private bool m_PrevDbgKeyState;

	private List<DecalInfo> m_Decals = new List<DecalInfo>();

	private void Awake()
	{
		if (!Instance)
		{
			Instance = this;
			InitMeshes();
			m_AspectRatio = (float)Screen.width / (float)Screen.height;
		}
	}

	private void LateUpdate()
	{
		TimeSinceStart += TimeManager.Instance.GetRealDeltaTime();
		if (Camera.main == null)
		{
			return;
		}
		if ((bool)m_BloodOverlayMeshGOInst)
		{
			Vector4 zero = Vector4.zero;
			zero.x = m_CurrHealth * 0.8f;
			zero.y = m_HeartBeatFreqScale;
			m_BloodOverlayMeshGOInst.transform.position = Camera.main.transform.position;
			for (int i = 0; i < m_BloodOverlayMeshGOInst.GetComponent<Renderer>().materials.Length; i++)
			{
				m_BloodOverlayMeshGOInst.GetComponent<Renderer>().materials[i].SetVector("_Params", zero);
			}
		}
		if ((bool)m_Material)
		{
			m_Material.SetFloat("_UsrTime", TimeSinceStart);
		}
		KillOldDecals();
		if (m_DecalsVersion != m_BuffersVersion)
		{
			UpdateMeshBuffers();
			m_BuffersVersion = m_DecalsVersion;
		}
	}

	private void InitMeshes()
	{
		base.gameObject.AddComponent<MeshFilter>();
		base.gameObject.AddComponent<MeshRenderer>();
		m_MeshFilter = (MeshFilter)GetComponent(typeof(MeshFilter));
		m_MeshRenderer = (MeshRenderer)GetComponent(typeof(MeshRenderer));
		// The scene shipped with m_Material unassigned (lost in the port), so the
		// screen-blood mesh had no material and never rendered. Fall back to the
		// blood_droplets material kept in Resources - it uses the "Blood FX - alpha
		// blended" decal shader (samples the blood splat atlas, drips + fades over
		// the decal's lifetime), which matches this mesh. (m_bloodscreen_2 was the
		// wrong one - that's the full-screen hurt vignette, which rendered as streaks.)
		if (m_Material == null)
		{
			m_Material = Resources.Load("blood_droplets", typeof(Material)) as Material;
		}
		m_MeshRenderer.GetComponent<Renderer>().material = m_Material;
		m_MeshRenderer.GetComponent<Renderer>().enabled = true;
		m_MeshRenderer.GetComponent<Renderer>().castShadows = false;
		m_MeshRenderer.GetComponent<Renderer>().receiveShadows = false;
		m_Mesh = m_MeshFilter.mesh;
		if ((bool)m_BloodOverlayMeshGameObj)
		{
			m_BloodOverlayMeshGOInst = UnityEngine.Object.Instantiate(m_BloodOverlayMeshGameObj, Vector3.zero, Quaternion.identity) as GameObject;
			DebugUtils.Assert(m_BloodOverlayMeshGOInst);
			DebugUtils.Assert(m_BloodOverlayMeshGameObj.GetComponent<Renderer>());
			m_BloodOverlayMeshGOInst.transform.eulerAngles = new Vector3(45f, 45f, 45f);
		}
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
			float z = 0f - decal.m_SpawnTime;
			Vector2 size = decal.m_Size;
			Vector2 vector = decal.m_Pos - size * 0.5f;
			float y = (decal.m_IsDrop ? (-1) : 0);
			float num7 = vector[0] + 0.5f * size[0];
			float num8 = vector[1] + 0.5f * size[1];
			float num9 = vector[0];
			float num10 = vector[1];
			array[num3].x = num7 + (num9 - num7) * num5 - (num10 - num8) * num6;
			array[num3].y = num8 + ((num9 - num7) * num6 + (num10 - num8) * num5) * m_AspectRatio;
			array[num3].z = z;
			array2[num3].x = decal.m_UVMin[0];
			array2[num3].y = decal.m_UVMin[1];
			array3[num3].x = decal.m_Duration;
			array3[num3].y = y;
			num3++;
			num9 = vector[0] + size[0];
			num10 = vector[1];
			array[num3].x = num7 + (num9 - num7) * num5 - (num10 - num8) * num6;
			array[num3].y = num8 + ((num9 - num7) * num6 + (num10 - num8) * num5) * m_AspectRatio;
			array[num3].z = z;
			array2[num3].x = decal.m_UVMax[0];
			array2[num3].y = decal.m_UVMin[1];
			array3[num3].x = decal.m_Duration;
			array3[num3].y = y;
			num3++;
			num9 = vector[0] + size[0];
			num10 = vector[1] + size[1];
			array[num3].x = num7 + (num9 - num7) * num5 - (num10 - num8) * num6;
			array[num3].y = num8 + ((num9 - num7) * num6 + (num10 - num8) * num5) * m_AspectRatio;
			array[num3].z = z;
			array2[num3].x = decal.m_UVMax[0];
			array2[num3].y = decal.m_UVMax[1];
			array3[num3].x = decal.m_Duration;
			array3[num3].y = 0f;
			num3++;
			num9 = vector[0];
			num10 = vector[1] + size[1];
			array[num3].x = num7 + (num9 - num7) * num5 - (num10 - num8) * num6;
			array[num3].y = num8 + ((num9 - num7) * num6 + (num10 - num8) * num5) * m_AspectRatio;
			array[num3].z = z;
			array2[num3].x = decal.m_UVMin[0];
			array2[num3].y = decal.m_UVMax[1];
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
		float timeSinceStart = TimeSinceStart;
		uint num = 0u;
		for (int num2 = m_Decals.Count - 1; num2 >= 0; num2--)
		{
			if (timeSinceStart - m_Decals[num2].m_SpawnTime > m_Decals[num2].m_Duration)
			{
				if (m_Decals[num2].m_IsDrop)
				{
					DebugUtils.Assert(m_NumSpawnedDrops != 0);
					m_NumSpawnedDrops--;
				}
				else
				{
					DebugUtils.Assert(m_NumSpawnedSplashes != 0);
					m_NumSpawnedSplashes--;
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

	private uint GetNumDropsForIntensity(float intensity)
	{
		return (uint)(intensity * 8f) + 1;
	}

	private uint GetNumSplashesForIntensity(float intensity)
	{
		return (uint)(intensity * 3f) + 1;
	}

	private Vector2 GetHeavySplatterPos(int idx)
	{
		DebugUtils.Assert(idx < 6);
		switch (idx)
		{
		case 0:
			return new Vector2(-1f, 1f);
		case 1:
			return new Vector2(0f, 1f);
		case 2:
			return new Vector2(1f, 1f);
		case 3:
			return new Vector2(-1f, -1f);
		case 4:
			return new Vector2(0f, -1f);
		case 5:
			return new Vector2(1f, -1f);
		default:
			return new Vector2(0f, 0f);
		}
	}

	private Vector2 CalcHeavySplatterRandomPos()
	{
		Vector2 result = new Vector2(0f, 0f);
		result[0] = RandomNext(-1f, 1f);
		result[1] = RandomNext(-1f, 1f);
		return result;
	}

	private float RandomNext(float min, float max)
	{
		return min + (float)m_Random.NextDouble() * (max - min);
	}

	public void SpawnBloodDrops(uint cnt)
	{
		float timeSinceStart = TimeSinceStart;
		float num = 1f / (float)m_BloodDropsTexNumTilesU;
		float num2 = 1f / (float)m_BloodDropsTexNumTilesV;
		float num3 = 1.9f / (float)cnt;
		float num4 = -1f;
		for (uint num5 = 0u; num5 < cnt; num5++)
		{
			if (m_NumSpawnedDrops >= m_MaxVisibleDrops)
			{
				break;
			}
			DecalInfo item = default(DecalInfo);
			float value = UnityEngine.Random.Range(m_DropMinSize[0], m_DropMaxSize[0]);
			item.m_Pos[0] = RandomNext(-0.95f + num3 * (float)num5, -0.95f + num3 * (float)(num5 + 1));
			float num6 = 0f;
			for (int i = 0; i < 5; i++)
			{
				num6 = RandomNext(-1.2f, 0.4f);
				if (Mathf.Abs(num6 - num4) > 0.7f)
				{
					break;
				}
			}
			num4 = num6;
			item.m_Pos[1] = num6;
			item.m_Size[0] = value;
			item.m_Size[1] = value;
			int num7 = m_Random.Next(0, m_BloodDropsTexNumTilesU);
			int num8 = m_Random.Next(0, m_BloodDropsTexNumTilesV);
			item.m_UVMin[0] = (float)num7 * num;
			item.m_UVMin[1] = (float)num8 * num2;
			item.m_UVMax[0] = item.m_UVMin[0] + num;
			item.m_UVMax[1] = item.m_UVMin[1] + num2;
			item.m_Rot = 0f;
			item.m_SpawnTime = timeSinceStart;
			item.m_Duration = m_DropsDuration + RandomNext(0f, m_DropsDuration * m_DropsDurationVariation);
			item.m_IsDrop = true;
			item.m_Id = -1;
			m_NumSpawnedDrops++;
			m_Decals.Add(item);
		}
		m_DecalsVersion++;
	}

	public void SpawnBloodSplashes(uint cnt)
	{
		// Original gate required High/UltraHigh performance grade, but the port runs
		// at graphicDetail 0 (Low), which hid the on-screen kill-splatter on PC and
		// Vita entirely. The splashes are cheap screen decals, so always allow them.
		for (uint num = 0u; num < cnt; num++)
		{
			if (m_NumSpawnedSplashes >= m_MaxVisibleSplashes)
			{
				break;
			}
			DecalInfo item = default(DecalInfo);
			float value = RandomNext(m_MinSize[0], m_MaxSize[0]);
			item.m_Pos = CalcHeavySplatterRandomPos();
			float num2 = UnityEngine.Random.Range(0, m_NumSplashesDropTexTilesU);
			float num3 = UnityEngine.Random.Range(0, m_NumSplashesDropTexTilesV);
			float num4 = 1f / (float)m_NumSplashesDropTexTilesU;
			float num5 = 1f / (float)m_NumSplashesDropTexTilesV;
			item.m_Size[0] = value;
			item.m_Size[1] = value;
			item.m_UVMin[0] = num2 * num4;
			item.m_UVMin[1] = num3 * num5;
			item.m_UVMax[0] = item.m_UVMin[0] + num4;
			item.m_UVMax[1] = item.m_UVMin[1] + num5;
			item.m_Rot = RandomNext(0f, (float)Math.PI * 2f);
			item.m_SpawnTime = TimeSinceStart;
			item.m_Duration = m_Duration + UnityEngine.Random.Range(0f, m_Duration * m_DurationVariation);
			item.m_IsDrop = false;
			item.m_Id = -1;
			m_NumSpawnedSplashes++;
			m_Decals.Add(item);
		}
		m_DecalsVersion++;
	}

	public void SpawnBloodSplatterAuto(float intensity)
	{
		uint numDropsForIntensity = GetNumDropsForIntensity(intensity);
		uint numSplashesForIntensity = GetNumSplashesForIntensity(intensity);
		SpawnBloodDrops(numDropsForIntensity);
		SpawnBloodSplashes(numSplashesForIntensity);
		m_DecalsVersion++;
	}

	public void SetSlomoEffectNormalized(float value)
	{
		DebugUtils.Assert(value >= 0f && value <= 1f);
		m_CurrHealth = value;
	}

	private void DbgEmit()
	{
		bool button = Input.GetButton("Fire1");
		if (!m_PrevDbgKeyState && button)
		{
			SpawnBloodSplashes(3u);
		}
		m_PrevDbgKeyState = button;
	}
}
