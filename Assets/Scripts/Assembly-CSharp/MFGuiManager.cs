#define DEBUG
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("GUI/Hiearchy/MFGuiManager")]
public class MFGuiManager : MonoBehaviour
{
	public enum E_Fading
	{
		None = 0,
		IntoTransparent = 1,
		OutToBlack = 2,
		Black = 3
	}

	public struct S_ObjectToChangeVisibility
	{
		public GameObject m_GObj;

		public bool m_Visible;

		public bool m_Recursive;

		public S_ObjectToChangeVisibility(GameObject gObj, bool show, bool recursive)
		{
			m_GObj = gObj;
			m_Visible = show;
			m_Recursive = recursive;
		}
	}

	public LayerMask UILayer = 0;

	public int DrawDepth = 10;

	public float m_FadeDepth = 10f;

	public bool m_FadeOnStart;

	public float m_FadeDuration = 1f;

	public static MFGuiManager Instance;

	public static bool ControlEnabled = true;

	private static int DEF_ALLOC_SIZE = 16;

	private static float s_ALPHA_LIMIT_TO_HIDE_FADE_SPRITE = 0.01f;

	private Dictionary<GUIBase_Layout, GUIBase_Platform> m_LayoutPlatformMapping = new Dictionary<GUIBase_Layout, GUIBase_Platform>();

	private Dictionary<GUIBase_Pivot, GUIBase_Platform> m_PivotPlatformMapping = new Dictionary<GUIBase_Pivot, GUIBase_Platform>();

	private GUIBase_Layout[] m_Layouts = new GUIBase_Layout[DEF_ALLOC_SIZE];

	private int m_LastLayoutIdx;

	public Camera m_UiCamera;

	private GameObject m_UiCameraHolder;

	private Material m_FadeMaterial;

	private MFGuiRenderer m_FadeGuiRenderer;

	private MFGuiSprite m_FadeSprite;

	private float m_TimeToFade;

	private float m_TotalFadeTime;

	private float m_CurrentFade;

	private float m_FromFade;

	private float m_TargetFade;

	private bool m_FadeInProgress;

	private Dictionary<ulong, MFGuiRenderer> m_GUIRenderers;

	private ArrayList m_ObjectsToChangeVisibility;

	public float FadeRemainingTime
	{
		get
		{
			return m_TimeToFade - Time.realtimeSinceStartup;
		}
	}

	public E_Fading FadeState
	{
		get
		{
			if (m_FadeInProgress)
			{
				if (m_CurrentFade < m_TargetFade)
				{
					return E_Fading.OutToBlack;
				}
				return E_Fading.IntoTransparent;
			}
			if (m_CurrentFade > 0.95f)
			{
				return E_Fading.Black;
			}
			return E_Fading.None;
		}
	}

	private void Awake()
	{
		if ((bool)Instance)
		{
			Object.Destroy(base.gameObject);
			return;
		}
		Instance = this;
		m_UiCameraHolder = new GameObject("UI Camera");
		m_UiCameraHolder.AddComponent<Camera>();
		m_UiCamera = m_UiCameraHolder.GetComponent<Camera>();
		m_UiCamera.clearFlags = CameraClearFlags.Depth;
		m_UiCamera.nearClipPlane = 0.3f;
		m_UiCamera.farClipPlane = 200f;
		m_UiCamera.depth = DrawDepth;
		m_UiCamera.rect = new Rect(0f, 0f, 1f, 1f);
		m_UiCamera.orthographic = true;
		m_UiCamera.orthographicSize = (float)Screen.height * 0.5f;
		m_UiCamera.cullingMask = UILayer;
		m_UiCamera.transform.position = new Vector3(0f, 0f, -100f);
		m_GUIRenderers = new Dictionary<ulong, MFGuiRenderer>();
		PrepareFadeSprite();
	}

	private void OnDestroy()
	{
		if (m_ObjectsToChangeVisibility != null)
		{
			m_ObjectsToChangeVisibility.Clear();
		}
		m_LayoutPlatformMapping.Clear();
		m_PivotPlatformMapping.Clear();
		m_Layouts = null;
		if (m_FadeGuiRenderer != null)
		{
			m_FadeGuiRenderer.SetMaterial(null);
		}
		m_FadeMaterial = null;
		m_FadeGuiRenderer = null;
		m_FadeSprite = null;
		if (m_GUIRenderers != null)
		{
			m_GUIRenderers.Clear();
		}
	}

	private void Start()
	{
		m_ObjectsToChangeVisibility = new ArrayList();
	}

	public MFGuiRenderer RegisterWidget(GUIBase_Widget w, Material material, int renderQueueIdx)
	{
		MFGuiRenderer value = null;
		if ((bool)material)
		{
			ulong key = CalcRendererKey(renderQueueIdx, w.GetLayoutUniqueId(), material);
			if (!m_GUIRenderers.TryGetValue(key, out value))
			{
				GameObject gameObject = new GameObject("MF Gui Rend-" + material.GetInstanceID() + "-" + renderQueueIdx + "-" + w.name);
				value = gameObject.AddComponent<MFGuiRenderer>() as MFGuiRenderer;
				value.plane = MFGuiRenderer.SPRITE_PLANE.XY;
				value.UILayer = UILayer;
				value.ZeroLocation = MFGuiRenderer.ZeroLocationEnum.UpperLeft;
				for (int i = 0; i < 32; i++)
				{
					if ((UILayer.value & (1 << i)) == 1 << i)
					{
						gameObject.layer = i;
						break;
					}
				}
				Material material2 = (Material)Object.Instantiate(material);
				material2.renderQueue = renderQueueIdx;
				value.SetMaterial(material2);
				m_GUIRenderers.Add(key, value);
			}
		}
		if (value != null && value != w.GetGuiRenderer())
		{
			value.RegisterWidget(w);
		}
		return value;
	}

	public bool UnRegisterWidget(GUIBase_Widget inWidget, MFGuiRenderer inGuiRenderer)
	{
		if (inGuiRenderer != null)
		{
			if (inGuiRenderer.UnRegisterWidget(inWidget) == 0)
			{
				foreach (KeyValuePair<ulong, MFGuiRenderer> gUIRenderer in m_GUIRenderers)
				{
					if (gUIRenderer.Value == inGuiRenderer)
					{
						m_GUIRenderers.Remove(gUIRenderer.Key);
						Object.Destroy(inGuiRenderer.gameObject);
						break;
					}
				}
			}
			return true;
		}
		return false;
	}

	public void RegisterLayout(GUIBase_Layout l)
	{
		int num = m_Layouts.Length;
		if (m_LastLayoutIdx <= num)
		{
			GUIBase_Layout[] layouts = m_Layouts;
			m_Layouts = new GUIBase_Layout[num + DEF_ALLOC_SIZE];
			layouts.CopyTo(m_Layouts, 0);
		}
		m_Layouts[m_LastLayoutIdx] = l;
		m_LastLayoutIdx++;
	}

	public void RegisterPlatform(GUIBase_Platform platform)
	{
		Vector3 localScale = new Vector3((float)Screen.width / (float)platform.m_Width, (float)Screen.height / (float)platform.m_Height, 1f);
		Vector3 vector = default(Vector3);
		vector = platform.transform.position;
		vector.x = (localScale.x - localScale.y) * (float)platform.m_Width;
		vector.y = (localScale.x - localScale.y) * (float)platform.m_Height;
		if (SystemInfo.operatingSystem.Contains("iPhone") && (float)Screen.width / (float)Screen.height <= 1.5f)
		{
			localScale.y = localScale.x;
		}
		if ((float)Screen.width / (float)Screen.height <= 1.4f)
		{
			localScale.y = localScale.x;
		}
		platform.gameObject.transform.localScale = localScale;
		GUIBase_Layout[] componentsInChildren = platform.GetComponentsInChildren<GUIBase_Layout>();
		if (componentsInChildren != null)
		{
			GUIBase_Layout[] array = componentsInChildren;
			foreach (GUIBase_Layout gUIBase_Layout in array)
			{
				m_LayoutPlatformMapping.Add(gUIBase_Layout, platform);
				gUIBase_Layout.SetPlatformSize(platform.m_Width, platform.m_Height, localScale.x, localScale.y);
			}
		}
		GUIBase_Pivot[] componentsInChildren2 = platform.GetComponentsInChildren<GUIBase_Pivot>();
		if (componentsInChildren2 != null)
		{
			GUIBase_Pivot[] array2 = componentsInChildren2;
			foreach (GUIBase_Pivot key in array2)
			{
				m_PivotPlatformMapping.Add(key, platform);
			}
		}
	}

	public GUIBase_Platform FindPlatform(string platformName)
	{
		GameObject gameObject = GameObject.Find(platformName);
		if ((bool)gameObject)
		{
			return gameObject.GetComponent<GUIBase_Platform>();
		}
		return null;
	}

	public GUIBase_Platform GetPlatform(GUIBase_Layout layout)
	{
		return m_LayoutPlatformMapping[layout];
	}

	public GUIBase_Platform GetPlatform(GUIBase_Pivot pivot)
	{
		return m_PivotPlatformMapping[pivot];
	}

	public GUIBase_Layout GetLayout(string lName)
	{
		for (int i = 0; i < m_LastLayoutIdx; i++)
		{
			if ((bool)m_Layouts[i] && m_Layouts[i].name == lName)
			{
				return m_Layouts[i];
			}
		}
		return null;
	}
	private void LateUpdate()
	{
#if UNITY_EDITOR
		if (Input.GetKeyDown(KeyCode.Keypad7))
		{
			Debug.Log("Input.GetKeyDown(KeyCode.Keypad7)");
			TextDatabase.instance.Reload(SystemLanguage.English);
			OnLanguageChanged("English.Old");
		}
		else if (Input.GetKeyDown(KeyCode.Keypad8))
		{
			Debug.Log("Input.GetKeyDown(KeyCode.Keypad8)");
			TextDatabase.instance.Reload(SystemLanguage.English);
			OnLanguageChanged("English");
		}
		else if (Input.GetKeyDown(KeyCode.Keypad9))
		{
			Debug.Log("Input.GetKeyDown(KeyCode.Keypad9)");
			TextDatabase.instance.Reload(SystemLanguage.Korean);
			OnLanguageChanged("Korean");
		}
#endif
		if (m_FadeInProgress)
		{
			m_CurrentFade = Mathf.Lerp(m_FromFade, m_TargetFade, (m_TotalFadeTime - (m_TimeToFade - Time.realtimeSinceStartup)) / m_TotalFadeTime);
			m_FadeSprite.SetAlpha(m_CurrentFade);
			if (Time.realtimeSinceStartup >= m_TimeToFade)
			{
				m_FadeInProgress = false;
				if (m_CurrentFade <= s_ALPHA_LIMIT_TO_HIDE_FADE_SPRITE)
				{
					m_FadeGuiRenderer.HideSprite(m_FadeSprite);
				}
			}
		}
		if (m_ObjectsToChangeVisibility != null && m_ObjectsToChangeVisibility.Count > 0)
		{
			for (int i = 0; i < m_ObjectsToChangeVisibility.Count; i++)
			{
				S_ObjectToChangeVisibility s_ObjectToChangeVisibility = (S_ObjectToChangeVisibility)m_ObjectsToChangeVisibility[i];
				GameObject targetObj = s_ObjectToChangeVisibility.m_GObj;
				GUIBase_Layout component = targetObj.GetComponent<GUIBase_Layout>();
				if ((bool)component)
				{
					component.ShowImmediate(s_ObjectToChangeVisibility.m_Visible);
					continue;
				}
				GUIBase_Widget component2 = targetObj.GetComponent<GUIBase_Widget>();
				if ((bool)component2)
				{
					component2.ShowImmediate(s_ObjectToChangeVisibility.m_Visible, s_ObjectToChangeVisibility.m_Recursive);
				}
			}
			m_ObjectsToChangeVisibility.RemoveRange(0, m_ObjectsToChangeVisibility.Count);
		}
		bool flag = false;
		for (int j = 0; j < m_LastLayoutIdx; j++)
		{
			GUIBase_Layout gUIBase_Layout = m_Layouts[j];
			if ((bool)gUIBase_Layout)
			{
				gUIBase_Layout.GUIUpdate(gUIBase_Layout.GetParentFadeAlpha());
			}
			else
			{
				flag = true;
			}
		}
		if (flag)
		{
			DefragmentLayouts();
		}
		foreach (KeyValuePair<ulong, MFGuiRenderer> gUIRenderer in m_GUIRenderers)
		{
			MFGuiRenderer value = gUIRenderer.Value;
			bool isActive = value.IsAnySpriteActive();
			if (isActive != value.gameObject.activeSelf)
			{
				value.gameObject.SetActive(isActive);
			}
		}
	}
	private void DefragmentLayouts()
	{
		for (int i = 0; i < m_LastLayoutIdx; i++)
		{
			GUIBase_Layout gUIBase_Layout = m_Layouts[i];
			if (!gUIBase_Layout)
			{
				m_Layouts[i] = m_Layouts[m_LastLayoutIdx - 1];
				m_Layouts[m_LastLayoutIdx - 1] = null;
				m_LastLayoutIdx--;
			}
		}
	}

	public void HideAllLayouts()
	{
		for (int i = 0; i < m_LastLayoutIdx; i++)
		{
			GUIBase_Layout layout = m_Layouts[i];
			ShowLayout(layout, false);
		}
	}

	public void ShowLayout(GUIBase_Layout layout, bool show)
	{
		if ((bool)layout)
		{
			S_ObjectToChangeVisibility s_ObjectToChangeVisibility = new S_ObjectToChangeVisibility(layout.gameObject, show, true);
			m_ObjectsToChangeVisibility.Add(s_ObjectToChangeVisibility);
		}
	}

	public void ShowWidget(GUIBase_Widget widget, bool show, bool recursive)
	{
		S_ObjectToChangeVisibility s_ObjectToChangeVisibility = new S_ObjectToChangeVisibility(widget.gameObject, show, recursive);
		m_ObjectsToChangeVisibility.Add(s_ObjectToChangeVisibility);
	}

	private void ShowLayout(string name, bool show)
	{
		for (int i = 0; i < m_LastLayoutIdx; i++)
		{
			GUIBase_Layout gUIBase_Layout = m_Layouts[i];
			if (gUIBase_Layout.name == name)
			{
				S_ObjectToChangeVisibility s_ObjectToChangeVisibility = new S_ObjectToChangeVisibility(gUIBase_Layout.gameObject, show, true);
				m_ObjectsToChangeVisibility.Add(s_ObjectToChangeVisibility);
				break;
			}
		}
	}

	public void ShowPivot(GUIBase_Pivot pivot, bool show)
	{
		if ((bool)pivot)
		{
			pivot.Show(show);
		}
	}

	public GUIBase_Pivot GetPivot(string name)
	{
		GameObject gameObject = GameObject.Find(name);
		if ((bool)gameObject)
		{
			GUIBase_Pivot component = gameObject.GetComponent<GUIBase_Pivot>();
			if (component == null)
			{
				Debug.LogError("Can't find PIVOT '" + name + "'. There is object with that name in the scene, but it is not GUIBase_Pivot.");
			}
			return component;
		}
		Debug.LogError("Can't find PIVOT '" + name + "'");
		return null;
	}

	private IEnumerator StartFade(float time, float targetAlpha)
	{
		yield return new WaitForEndOfFrame();
		m_FadeInProgress = true;
		m_TimeToFade = Time.realtimeSinceStartup + time;
		m_TotalFadeTime = time;
		m_FromFade = m_CurrentFade;
		m_TargetFade = targetAlpha;
		m_FadeGuiRenderer.ShowSprite(m_FadeSprite);
	}

	private void StartFadeEx(float time, float targetAlpha)
	{
		m_FadeInProgress = true;
		m_TimeToFade = Time.realtimeSinceStartup + time;
		m_TotalFadeTime = time;
		m_FromFade = m_CurrentFade;
		m_TargetFade = targetAlpha;
		m_FadeGuiRenderer.ShowSprite(m_FadeSprite);
	}

	public void FadeIn(float duration, float to)
	{
		Game.Instance.TryToCleanSomeMemory();
		m_TargetFade = to;
		StartCoroutine(StartFade(duration, to));
	}

	public void SetFadeOut(float fadeOut)
	{
		m_CurrentFade = fadeOut;
		m_FromFade = fadeOut;
		m_TargetFade = fadeOut;
		m_FadeSprite.SetAlpha(m_CurrentFade);
		if (m_CurrentFade <= s_ALPHA_LIMIT_TO_HIDE_FADE_SPRITE)
		{
			m_FadeGuiRenderer.HideSprite(m_FadeSprite);
		}
		else
		{
			m_FadeGuiRenderer.ShowSprite(m_FadeSprite);
		}
	}

	public void FadeIn()
	{
		FadeIn(m_FadeDuration, 0f);
	}

	public void FadeIn(float duration)
	{
		FadeIn(duration, 0f);
	}

	public void FadeOut(float duration, float to)
	{
		m_TargetFade = to;
		StartFadeEx(duration, to);
	}

	public void FadeOut()
	{
		FadeOut(m_FadeDuration, 1f);
	}

	public void FadeOut(float duration)
	{
		FadeOut(duration, 1f);
	}

	public void ShowBloodSplash()
	{
	}

	public void SetHealthPercent(float currentHealth, float maxHealth)
	{
	}

	private void PrepareFadeSprite()
	{
		m_FadeMaterial = (Material)Resources.Load("Gui/FadeMaterial", typeof(Material));
		if (m_FadeMaterial == null)
		{
			Debug.LogError("V Resourcech chybi material 'FadeMaterial'");
			return;
		}
		m_FadeMaterial.mainTexture = (Texture2D)Resources.Load("Gui/Fade", typeof(Texture2D));
		if (m_FadeMaterial.mainTexture == null)
		{
			Debug.LogError("V Resourcech chybi texture 'Gui/Fade'");
			return;
		}
		GameObject gameObject = new GameObject("MF Gui Renderer - PrepareFadeSprite");
		m_FadeGuiRenderer = gameObject.AddComponent<MFGuiRenderer>() as MFGuiRenderer;
		m_FadeGuiRenderer.plane = MFGuiRenderer.SPRITE_PLANE.XY;
		m_FadeGuiRenderer.UILayer = UILayer;
		m_FadeGuiRenderer.ZeroLocation = MFGuiRenderer.ZeroLocationEnum.UpperLeft;
		for (int i = 0; i < 32; i++)
		{
			if ((UILayer.value & (1 << i)) == 1 << i)
			{
				gameObject.layer = i;
				break;
			}
		}
		m_FadeGuiRenderer.SetMaterial(m_FadeMaterial);
		m_FadeSprite = m_FadeGuiRenderer.AddElement(new Vector2(0f, 0f), Screen.width, Screen.height, 0f, m_FadeDepth, 0, 0, 1, 1);
		if (m_FadeOnStart)
		{
			SetFadeOut(1f);
		}
		else
		{
			SetFadeOut(0f);
		}
	}

	public void PlayOneShot(AudioClip c)
	{
		base.GetComponent<AudioSource>().PlayOneShot(c);
	}

	private ulong CalcRendererKey(int queueIdx, int layoutId, Material mat)
	{
		DebugUtils.Assert(layoutId < 65536 && queueIdx < 65535);
		return (ulong)(((long)mat.GetInstanceID() << 32) + ((long)layoutId << 16) + queueIdx);
	}

	public static void OnLanguageChanged(string inNewLanguage)
	{
		GUIBase_Label[] array = Object.FindObjectsOfType(typeof(GUIBase_Label)) as GUIBase_Label[];
		GUIBase_Label[] array2 = array;
		foreach (GUIBase_Label gUIBase_Label in array2)
		{
			gUIBase_Label.OnLanguageChanged(inNewLanguage);
		}
		GUIBase_TextArea[] array3 = Object.FindObjectsOfType(typeof(GUIBase_TextArea)) as GUIBase_TextArea[];
		GUIBase_TextArea[] array4 = array3;
		foreach (GUIBase_TextArea gUIBase_TextArea in array4)
		{
			gUIBase_TextArea.OnLanguageChanged(inNewLanguage);
		}
	}
}
