#define DEBUG
using UnityEngine;

[AddComponentMenu("Image Effects/MADFINGER Refraction Effects")]
public class MFRefractionEffects : ImageEffectBase
{
	private int m_ScreenGridXRes = 2;

	private int m_ScreenGridYRes = 2;

	private MeshFilter m_MeshFilter;

	private MeshRenderer m_MeshRenderer;

	private MeshFilter m_ScreenDropsSimMeshFilter;

	//private WaterDroplets m_WaterDroplets;

	private Mesh m_Mesh;

	private bool m_InitOK;

	private GameObject m_GameObj;

	public Material m_WaterScreenRefractionMat;

	public float m_WaterDropSizeMin = 1f;

	public float m_WaterDropSizeMax = 2f;

	public static MFRefractionEffects Instance;

	private void Awake()
	{
		DebugUtils.Assert(!Instance);
		Instance = this;
		m_InitOK = DoInit();
	}

	private void OnDestroy()
	{
		Instance = null;
	}

	public void Reset()
	{
	}

	public void SetDropletsColor(Color col)
	{
		if ((bool)m_WaterScreenRefractionMat)
		{
			m_WaterScreenRefractionMat.SetColor("_Color", col);
		}
	}

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		// Always copy the scene first, so this effect can never black-screen even
		// if there are no drops or the material is missing.
		Graphics.Blit(source, destination);
		if (!m_InitOK || !(bool)m_WaterScreenRefractionMat)
		{
			return;
		}
		// Drops are produced by ScreenDrops (fed by the water-drip triggers).
		if (ScreenDrops.Instance == null || ScreenDrops.Instance.NumDecals() <= 0)
		{
			return;
		}
		Mesh mesh = ScreenDrops.Instance.GetMesh();
		if (mesh == null)
		{
			return;
		}
		// Draw the droplet quads on top of the copied scene. The refraction shader
		// samples the source screen at an offset per vertex, so each drop bends and
		// magnifies what's behind it (the original wet-lens look).
		RenderTexture active = RenderTexture.active;
		RenderTexture.active = destination;
		m_WaterScreenRefractionMat.mainTexture = source;
		if (m_WaterScreenRefractionMat.SetPass(0))
		{
			Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
		}
		RenderTexture.active = active;
	}

	private bool DoInit()
	{
		m_GameObj = new GameObject();
		if (!InitMeshes())
		{
			return false;
		}
		m_GameObj.SetActive(false);
		m_ScreenDropsSimMeshFilter = base.gameObject.GetComponent(typeof(MeshFilter)) as MeshFilter;
		//m_WaterDroplets = base.gameObject.GetComponent(typeof(WaterDroplets)) as WaterDroplets;
		return true;
	}

	private bool InitMeshes()
	{
		DebugUtils.Assert(m_ScreenGridXRes > 1);
		DebugUtils.Assert(m_ScreenGridYRes > 1);
		m_GameObj.AddComponent<MeshFilter>();
		m_GameObj.AddComponent<MeshRenderer>();
		m_MeshFilter = (MeshFilter)m_GameObj.GetComponent(typeof(MeshFilter));
		m_MeshRenderer = (MeshRenderer)m_GameObj.GetComponent(typeof(MeshRenderer));
		DebugUtils.Assert(m_MeshFilter);
		DebugUtils.Assert(m_MeshRenderer);
		m_MeshRenderer.GetComponent<Renderer>().material = base.material;
		m_MeshRenderer.GetComponent<Renderer>().enabled = true;
		m_MeshRenderer.GetComponent<Renderer>().castShadows = false;
		m_MeshRenderer.GetComponent<Renderer>().receiveShadows = false;
		m_Mesh = m_MeshFilter.mesh;
		int num = m_ScreenGridXRes * m_ScreenGridYRes;
		int num2 = (m_ScreenGridXRes - 1) * (m_ScreenGridYRes - 1) * 2;
		Vector3[] array = new Vector3[num];
		Vector2[] array2 = new Vector2[num];
		int[] array3 = new int[num2 * 3];
		for (int i = 0; i < m_ScreenGridYRes; i++)
		{
			for (int j = 0; j < m_ScreenGridXRes; j++)
			{
				int num3 = i * m_ScreenGridXRes + j;
				array[num3].x = (float)j / (float)(m_ScreenGridXRes - 1);
				array[num3].y = (float)i / (float)(m_ScreenGridYRes - 1);
				array[num3].z = 0f;
				array2[num3].x = array[num3].x;
				array2[num3].y = array[num3].y;
			}
		}
		int num4 = 0;
		for (int k = 0; k < m_ScreenGridYRes - 1; k++)
		{
			for (int l = 0; l < m_ScreenGridXRes - 1; l++)
			{
				int num5 = l + k * m_ScreenGridXRes;
				int num6 = l + 1 + k * m_ScreenGridXRes;
				int num7 = l + 1 + (k + 1) * m_ScreenGridXRes;
				int num8 = l + (k + 1) * m_ScreenGridXRes;
				array3[num4++] = num8;
				array3[num4++] = num6;
				array3[num4++] = num5;
				array3[num4++] = num8;
				array3[num4++] = num7;
				array3[num4++] = num6;
			}
		}
		m_Mesh.vertices = array;
		m_Mesh.uv = array2;
		m_Mesh.triangles = array3;
		m_Mesh.name = "screenspace grid";
		return true;
	}

	public void AddWaterDropsToScreen(int num)
	{
		/*if ((bool)m_WaterDroplets)
		{
			Vector2 pos = default(Vector2);
			for (int i = 0; i < num; i++)
			{
				pos.x = Random.Range(0f, 1f);
				pos.y = Random.Range(0f, 1f);
				m_WaterDroplets.AddDroplet(pos, Random.Range(m_WaterDropSizeMin, m_WaterDropSizeMax));
			}
		}*/
	}
}
