using System;
using UnityEngine;

[AddComponentMenu("GUI/Widgets/Widget")]
public class GUIBase_Widget : MonoBehaviour
{
	public enum E_TouchPhase
	{
		E_TP_NONE = 0,
		E_TP_CLICK_BEGIN = 1,
		E_TP_CLICK_RELEASE = 2,
		E_TP_MOUSEOVER_BEGIN = 3,
		E_TP_MOUSEOVER_END = 4,
		E_TP_CLICK_RELEASE_KEYBOARD = 5
	}

	private struct S_Sprite
	{
		public MFGuiSprite m_Sprite;

		public bool m_IsVisible;

		public bool m_ProxyFlag;

		public Vector3 m_Pos;

		public float m_Width;

		public float m_Height;
	}

	public delegate void UpdateDelegate();

	public Material m_Material;

	public int m_FocusID = -1;

	public Vector2 m_InTexPos = new Vector2(0.4f, 0.4f);

	public Vector2 m_InTexSize = new Vector2(0.2f, 0.2f);

	public int m_GuiWidgetLayer;

	public bool m_InitProxyFlag;

	public bool m_VisibleOnLayoutShow = true;

	public float m_FadeAlpha = 1f;

	public Color m_Color = Color.white;

	public float m_Width = 1f;

	public float m_Height = 1f;

	private GUIBase_Layout m_Layout;

	private MFGuiRenderer m_GuiRenderer;

	private Vector3 m_OrigPos = default(Vector3);

	private bool m_IsVisible;

	private GUIBase_Callback m_Callback;

	private UpdateDelegate m_UpdateDelegate;

	private S_Sprite[] m_Sprite;

	private int m_UnusedSpriteIndex;

	private GUIBase_Widget m_ParentWidget;

	private GUIBase_Pivot m_ParentPivot;

	private GUIBase_Layout m_ParentLayout;

	private bool m_IsModified = true;

	[NonSerialized]
	public bool m_TextScaleFix_HACK;

	private Vector3 m_Pos;

	private Vector3 m_Scale;

	private float m_Rot;

	private Color m_ColorCached = Color.clear;

	// OPTIMIZATION: Cache GetComponentsInChildren to avoid repeated allocation
	private GUIBase_Widget[] m_ChildWidgetsCache;
	private bool m_ChildWidgetsCacheDirty = true;

	private int ReservedSpritesSize
	{
		get
		{
			return (m_Sprite != null) ? m_Sprite.Length : 0;
		}
	}

	public void Initialization(GUIBase_Layout parentLayout, Vector2 layoutScale)
	{
		m_Layout = parentLayout;
		m_GuiRenderer = MFGuiManager.Instance.RegisterWidget(this, m_Material, m_Layout.m_LayoutLayer * 10 + m_GuiWidgetLayer);
		if (!m_GuiRenderer)
		{
			Debug.LogError("Material renderer for widget ' " + base.name + "' is missing");
			return;
		}
		m_Material = m_GuiRenderer.GetMaterial();
		AddMainSprite();
		if ((bool)m_Callback && m_Callback.TestFlag(1))
		{
			m_Callback.Callback(GUIBase_Callback.E_CallbackType.E_CT_INIT);
		}
		PrepareParent();
		m_IsModified = true;
	}

	public void ChangeMaterial(Material inMaterial)
	{
		if (!inMaterial)
		{
			Debug.LogError("New Material for widget ' " + base.name + "' is invalid");
			return;
		}
		MFGuiRenderer mFGuiRenderer = MFGuiManager.Instance.RegisterWidget(this, inMaterial, m_Layout.m_LayoutLayer * 10 + m_GuiWidgetLayer);
		if (!mFGuiRenderer)
		{
			Debug.LogError("Material renderer for widget ' " + base.name + "' is missing");
			return;
		}
		if (mFGuiRenderer == m_GuiRenderer)
		{
			m_Material = m_GuiRenderer.GetMaterial();
			return;
		}
		MFGuiManager.Instance.UnRegisterWidget(this, m_GuiRenderer);
		m_GuiRenderer = mFGuiRenderer;
		m_Material = m_GuiRenderer.GetMaterial();
		AddMainSprite();
	}

	private void OnDestroy()
	{
		m_Material = null;
	}

	public void RegisterUpdateDelegate(UpdateDelegate f)
	{
		m_UpdateDelegate = f;
	}

	public float GetWidth()
	{
		return m_Width;
	}

	public float GetHeight()
	{
		return m_Height;
	}

	public MFGuiRenderer GetGuiRenderer()
	{
		return m_GuiRenderer;
	}

	public Vector3 GetOrigPos()
	{
		return m_OrigPos;
	}

	public Material GetMaterial()
	{
		if ((bool)m_Material)
		{
			return m_Material;
		}
		return null;
	}

	public Texture GetTexture()
	{
		if ((bool)m_Material)
		{
			return m_Material.mainTexture;
		}
		return null;
	}

	public void CopyMaterialSettings(GUIBase_Widget otherWidget)
	{
		if (otherWidget == null || otherWidget.m_Material == null || otherWidget.m_Material.mainTexture == null)
		{
			Debug.LogWarning("CopyMaterialSettings - Invalid source widget !!! --- " + otherWidget.GetFullName());
			return;
		}
		if (m_Sprite == null || m_Sprite.Length <= 0)
		{
			Debug.LogWarning("CopyMaterialSettings - Invalid target widget !!! --- " + base.gameObject.GetFullName());
			return;
		}
		if ((bool)m_GuiRenderer)
		{
			for (int i = 0; i < m_Sprite.Length; i++)
			{
				if (m_Sprite[i].m_Sprite != null)
				{
					m_GuiRenderer.RemoveSprite(m_Sprite[i].m_Sprite);
					m_Sprite[i].m_Sprite = null;
				}
			}
		}
		m_Material = otherWidget.m_Material;
		m_InTexPos = otherWidget.m_InTexPos;
		m_InTexSize = otherWidget.m_InTexSize;
		m_GuiRenderer = MFGuiManager.Instance.RegisterWidget(this, m_Material, m_Layout.m_LayoutLayer * 10 + m_GuiWidgetLayer);
		m_Material = m_GuiRenderer.GetMaterial();
		Vector3 lossyScale = base.gameObject.transform.lossyScale;
		Vector3 eulerAngles = base.gameObject.transform.eulerAngles;
		Texture texture = GetTexture();
		float num = texture.width;
		float num2 = texture.height;
		int leftPixelX = (int)(num * m_InTexPos.x);
		int num3 = (int)(num2 * m_InTexPos.y);
		int pixelWidth = (int)(num * m_InTexSize.x);
		int num4 = (int)(num2 * m_InTexSize.y);
		Vector2 vector = new Vector2(m_OrigPos.x, m_OrigPos.y);
		float x = vector.x - 0.5f * m_Width * lossyScale.x;
		float y = vector.y - 0.5f * m_Height * lossyScale.y;
		for (int j = 0; j < m_Sprite.Length; j++)
		{
			MFGuiSprite mFGuiSprite = AddSprite(new Vector2(x, y), m_Width * lossyScale.x, m_Height * lossyScale.y, eulerAngles.z, 0f - ((float)m_Layout.m_LayoutLayer + (float)m_GuiWidgetLayer * 0.1f), leftPixelX, num3 + num4, pixelWidth, num4);
			if (mFGuiSprite != null)
			{
				if (!IsVisible())
				{
					HideSprite(mFGuiSprite);
				}
				m_Sprite[j].m_Sprite = mFGuiSprite;
				m_Sprite[j].m_Pos = vector;
				m_Sprite[j].m_Width = m_Width;
				m_Sprite[j].m_Height = m_Height;
			}
		}
	}

	public int GetLayoutUniqueId()
	{
		return m_Layout ? m_Layout.GetUniqueId() : 0;
	}

	public MFGuiSprite AddSprite(Vector2 leftDown, float width, float height, float rotAngle, float depth, int leftPixelX, int bottomPixelY, int pixelWidth, int pixelHeight)
	{
		return m_GuiRenderer.AddElement(leftDown, width, height, rotAngle, depth, leftPixelX, bottomPixelY, pixelWidth, pixelHeight);
	}

	public void UpdateSprite(MFGuiSprite sprite, float rx, float ry, float width, float height, float rotAngle, float depth)
	{
		m_GuiRenderer.UpdateSpritePosSize(sprite, rx, ry, width, height, rotAngle, depth);
	}

	public void ShowSprite(MFGuiSprite sprite)
	{
		m_GuiRenderer.ShowSprite(sprite);
	}

	public void HideSprite(MFGuiSprite sprite)
	{
		m_GuiRenderer.HideSprite(sprite);
	}

	public void SetScreenSize(float sizeX, float sizeY)
	{
		m_Width = sizeX;
		m_Height = sizeY;
	}

	public void PlaySound(AudioClip audioClip)
	{
		AudioSource component = GetComponent<AudioSource>();
		if (!component)
		{
			base.gameObject.AddComponent<AudioSource>();
		}
		base.GetComponent<AudioSource>().PlayOneShot(audioClip);
	}

	public GUIBase_Layout GetLayout()
	{
		return m_Layout;
	}

	public void GetTextureCoord(out float UVLeft, out float UVTop, out float UVWidth, out float UVHeight)
	{
		UVLeft = m_InTexPos.x;
		UVTop = m_InTexPos.y;
		UVWidth = m_InTexSize.x;
		UVHeight = m_InTexSize.y;
	}

	public Rect GetTextureCoord()
	{
		return new Rect(m_InTexPos.x, m_InTexPos.y, m_InTexSize.x, m_InTexSize.y);
	}

	private int AddMainSprite()
	{
		int result = -1;
		Texture texture = GetTexture();
		if ((bool)texture)
		{
			Vector3 lossyScale = base.gameObject.transform.lossyScale;
			Vector3 eulerAngles = base.gameObject.transform.eulerAngles;
			m_OrigPos = base.gameObject.transform.position;
			int width = texture.width;
			int height = texture.height;
			int texU = (int)((float)width * m_InTexPos.x);
			int num = (int)((float)height * m_InTexPos.y);
			int texW = (int)((float)width * m_InTexSize.x);
			int num2 = (int)((float)height * m_InTexSize.y);
			result = AddSprite(new Vector2(m_OrigPos.x, m_OrigPos.y), m_Width, m_Height, lossyScale.x, lossyScale.y, eulerAngles.z, texU, num + num2, texW, num2);
		}
		return result;
	}

	public void ReserveSprites(int size)
	{
		m_UnusedSpriteIndex = 0;
		m_Sprite = new S_Sprite[size];
	}

	private void ReallocateSprites(int newSize)
	{
		if (m_Sprite == null || ReservedSpritesSize <= 0)
		{
			Debug.LogError("For first allocate use AllocateSprites");
		}
		else if (newSize > ReservedSpritesSize)
		{
			S_Sprite[] sprite = m_Sprite;
			m_Sprite = new S_Sprite[newSize];
			sprite.CopyTo(m_Sprite, 0);
		}
		else
		{
			Debug.LogError("Sorry, reducing not implemented. Use AllocateSprites for creating entirely new buffer without copying, or add code handling reduction here.");
		}
	}

	public int AddSprite(Vector2 centerSpritePos, float width, float height, float scaleWidth, float scaleHeight, float rotAngle, int texU, int texV, int texW, int texH)
	{
		int num = -1;
		float x = centerSpritePos.x - 0.5f * width * scaleWidth;
		float y = centerSpritePos.y - 0.5f * height * scaleHeight;
		MFGuiSprite mFGuiSprite = AddSprite(new Vector2(x, y), width * scaleWidth, height * scaleHeight, rotAngle, 0f - ((float)m_Layout.m_LayoutLayer + (float)m_GuiWidgetLayer * 0.1f), texU, texV, texW, texH);
		if (mFGuiSprite != null)
		{
			if (m_Sprite == null)
			{
				ReserveSprites(1);
				num = m_UnusedSpriteIndex++;
			}
			else if (ReservedSpritesSize > 0 && m_UnusedSpriteIndex < ReservedSpritesSize)
			{
				num = m_UnusedSpriteIndex++;
			}
			else
			{
				ReallocateSprites(m_Sprite.Length + 1);
				num = m_UnusedSpriteIndex++;
			}
			m_Sprite[num].m_Sprite = mFGuiSprite;
			m_Sprite[num].m_IsVisible = m_IsVisible;
			m_Sprite[num].m_ProxyFlag = m_InitProxyFlag;
			m_Sprite[num].m_Pos = centerSpritePos;
			m_Sprite[num].m_Width = width;
			m_Sprite[num].m_Height = height;
			ShowSprite(num, m_IsVisible);
		}
		return num;
	}

	public void HACK_Label_RemoveSprite()
	{
		if (m_Sprite != null)
		{
			for (int i = 0; i < m_Sprite.Length; i++)
			{
				if (m_Sprite[i].m_Sprite != null)
				{
					m_GuiRenderer.RemoveSprite(m_Sprite[i].m_Sprite);
				}
			}
		}
		m_Sprite = null;
	}

	public MFGuiSprite GetSprite(int idx)
	{
		return m_Sprite[idx].m_Sprite;
	}

	public void UpdateSpritePosAndSize(int idx, float posX, float posY, float width, float height)
	{
		m_IsModified = true;
		m_Sprite[idx].m_Pos.x = posX;
		m_Sprite[idx].m_Pos.y = posY;
		m_Sprite[idx].m_Width = width;
		m_Sprite[idx].m_Height = height;
	}

	public void SetSpritePos(Vector2 pos)
	{
		m_IsModified = true;
		m_Sprite[0].m_Pos.x = pos.x;
		m_Sprite[0].m_Pos.y = pos.y;
	}

	public void RegisterCallback(GUIBase_Callback obj, int clbkTypes)
	{
		m_Callback = obj;
		m_Callback.RegisterCallbackType(clbkTypes);
	}

	public void PlayAnim(Animation animation, GUIBase_Widget widget, GUIBase_Platform.AnimFinishedDelegate finishDelegate = null, int customIdx = -1)
	{
		m_Layout.PlayAnim(animation, widget, finishDelegate, customIdx);
	}

	public void StopAnim(Animation animation)
	{
		m_Layout.StopAnim(animation);
	}

	public void SetModify()
	{
		m_IsModified = true;
	}

	public void GUIUpdate(float parentAlpha)
	{
		if (m_UpdateDelegate != null)
		{
			m_UpdateDelegate();
		}
		if (m_Sprite == null || !m_IsVisible)
		{
			return;
		}
		Vector4 vector = m_Color;
		Transform transform = base.gameObject.transform;
		Vector3 position = transform.position;
		Vector3 lossyScale = transform.lossyScale;
		float z = transform.eulerAngles.z;
		vector.w = m_FadeAlpha * parentAlpha;
		bool flag = vector != (Vector4)m_ColorCached;
		bool flag2 = false;
		flag2 = z != m_Rot || position != m_Pos || lossyScale != m_Scale;
		if (!m_IsModified && !flag && !flag2)
		{
			return;
		}
		m_Pos = position;
		m_Scale = lossyScale;
		m_Rot = z;
		if (m_IsModified || flag2)
		{
			float f = (0f - z) * ((float)Math.PI / 180f);
			// PERF: Pre-calculate cos/sin once instead of per sprite
			float cosF = Mathf.Cos(f);
			float sinF = Mathf.Sin(f);

			Vector2 vector2 = default(Vector2);
			vector2.x = position.x - m_OrigPos.x;
			vector2.y = position.y - m_OrigPos.y;
			Vector2 vector3 = default(Vector2);
			Vector2 vector4 = default(Vector2);

			// PERF: Cache lossy scale components to avoid repeated property access
			float lossyScaleX = lossyScale.x;
			float lossyScaleY = lossyScale.y;
			float origPosX = m_OrigPos.x;
			float origPosY = m_OrigPos.y;

			for (int i = 0; i < m_Sprite.Length; i++)
			{
				S_Sprite s_Sprite = m_Sprite[i];
				if (s_Sprite.m_IsVisible && !s_Sprite.m_ProxyFlag)
				{
					float width = s_Sprite.m_Width * lossyScaleX;
					float height = s_Sprite.m_Height * lossyScaleY;
					if (!m_TextScaleFix_HACK)
					{
						vector3.x = s_Sprite.m_Pos.x - origPosX;
						vector3.y = s_Sprite.m_Pos.y - origPosY;
					}
					else
					{
						vector3.x = (s_Sprite.m_Pos.x - origPosX) * lossyScaleX;
						vector3.y = (s_Sprite.m_Pos.y - origPosY) * lossyScaleY;
					}
					// PERF: Use pre-calculated cos/sin
					vector4.x = vector3.x * cosF + vector3.y * sinF;
					vector4.y = (0f - vector3.x) * sinF + vector3.y * cosF;
					float rx = origPosX + vector2.x + vector4.x;
					float ry = origPosY + vector2.y + vector4.y;
					UpdateSprite(s_Sprite.m_Sprite, rx, ry, width, height, z, 0f - (float)m_Layout.m_LayoutLayer);
					s_Sprite.m_Sprite.SetColor(vector);
				}
			}
		}
		else
		{
			for (int j = 0; j < m_Sprite.Length; j++)
			{
				if (m_Sprite[j].m_IsVisible && !m_Sprite[j].m_ProxyFlag)
				{
					m_Sprite[j].m_Sprite.SetColor(vector);
				}
			}
		}
		m_ColorCached = vector;
		m_IsModified = false;
	}

	public void GetScreenCoords(out int x, out int y, out int w, out int h)
	{
		GameObject gameObject = base.gameObject;
		Vector3 position = gameObject.transform.position;
		Vector3 lossyScale = gameObject.transform.lossyScale;
		float x2 = position.x;
		float y2 = position.y;
		float num = m_Width * lossyScale.x;
		float num2 = m_Height * lossyScale.y;
		x2 -= num * 0.5f;
		y2 -= num2 * 0.5f;
		x = (int)x2;
		y = (int)y2;
		w = (int)num;
		h = (int)num2;
	}

	public Rect GetRectInScreenCoords()
	{
		Vector3 position = base.transform.position;
		Vector3 lossyScale = base.transform.lossyScale;
		float num = m_Width * lossyScale.x;
		float num2 = m_Height * lossyScale.y;
		float num3 = position.x - num * 0.5f;
		float num4 = position.y - num2 * 0.5f;
		float left = num3;
		float top = num4;
		float width = num;
		float height = num2;
		return new Rect(left, top, width, height);
	}

	public bool IsTouchSensitive()
	{
		if ((bool)m_Callback)
		{
			return m_Callback.TestFlag(8);
		}
		return false;
	}

	public bool IsVisible()
	{
		return m_IsVisible;
	}

	public void SetSpriteProxyFlag(int idx)
	{
		if (m_Sprite != null && idx >= 0 && idx < m_Sprite.Length)
		{
			m_Sprite[idx].m_ProxyFlag = true;
		}
	}

	public void ClearSpriteProxyFlag(int idx)
	{
		if (m_Sprite != null && idx >= 0 && idx < m_Sprite.Length)
		{
			m_Sprite[idx].m_ProxyFlag = false;
		}
	}

	public void Show(bool v, bool recursive)
	{
		MFGuiManager.Instance.ShowWidget(this, v, recursive);
	}

	public void ShowImmediate(bool v, bool recursive)
	{
		if (recursive)
		{
			// OPTIMIZATION: Cache GetComponentsInChildren result to avoid repeated allocation
			if (m_ChildWidgetsCacheDirty)
			{
				m_ChildWidgetsCache = GetComponentsInChildren<GUIBase_Widget>();
				m_ChildWidgetsCacheDirty = false;
			}

			// OPTIMIZATION: Use for loop instead of foreach to avoid enumerator allocation
			if (m_ChildWidgetsCache != null)
			{
				for (int i = 0; i < m_ChildWidgetsCache.Length; i++)
				{
					m_ChildWidgetsCache[i].ShowImmediate(v, false);
				}
			}
			return;
		}
		if (m_IsVisible != v)
		{
			m_IsVisible = v;
			m_IsModified = true;
			if (m_Sprite != null)
			{
				for (int j = 0; j < m_Sprite.Length; j++)
				{
					ShowSprite(j, v);
				}
			}
		}
		if (v && (bool)m_Callback && m_Callback.TestFlag(2))
		{
			m_Callback.Callback(GUIBase_Callback.E_CallbackType.E_CT_SHOW);
		}
		else if (!v && (bool)m_Callback && m_Callback.TestFlag(4))
		{
			m_Callback.Callback(GUIBase_Callback.E_CallbackType.E_CT_HIDE);
		}
	}

	public void ShowSprite(int idx, bool showFlag)
	{
		if (m_Sprite == null || idx < 0 || idx >= m_Sprite.Length)
		{
			return;
		}
		m_Sprite[idx].m_IsVisible = showFlag;
		if (showFlag && !m_Sprite[idx].m_ProxyFlag)
		{
			if (m_IsVisible)
			{
				ShowSprite(m_Sprite[idx].m_Sprite);
			}
		}
		else
		{
			HideSprite(m_Sprite[idx].m_Sprite);
		}
	}

	public bool IsMouseOver(Vector2 clickPos)
	{
		int x;
		int y;
		int w;
		int h;
		GetScreenCoords(out x, out y, out w, out h);
		float scaleWidth = 1f;
		float scaleHeight = 1f;
		m_Callback.GetTouchAreaScale(out scaleWidth, out scaleHeight);
		int num = (int)((float)w * scaleWidth);
		int num2 = (int)((float)h * scaleHeight);
		x += (w - num) / 2;
		y += (h - num2) / 2;
		w = num;
		h = num2;
		if (clickPos.x >= (float)x && clickPos.x < (float)(x + w) && clickPos.y >= (float)y && clickPos.y < (float)(y + h))
		{
			return true;
		}
		return false;
	}

	public bool HandleTouchEvent(E_TouchPhase touchPhase, bool isMouseOver = true)
	{
		switch (touchPhase)
		{
			case E_TouchPhase.E_TP_CLICK_BEGIN:
				return m_Callback.Callback(GUIBase_Callback.E_CallbackType.E_CT_ON_TOUCH_BEGIN);
			case E_TouchPhase.E_TP_CLICK_RELEASE:
				if (isMouseOver)
				{
					return m_Callback.Callback(GUIBase_Callback.E_CallbackType.E_CT_ON_TOUCH_END);
				}
				return m_Callback.Callback(GUIBase_Callback.E_CallbackType.E_CT_ON_TOUCH_END_OUTSIDE);
			case E_TouchPhase.E_TP_MOUSEOVER_BEGIN:
				return m_Callback.Callback(GUIBase_Callback.E_CallbackType.E_CT_ON_MOUSEOVER_BEGIN);
			case E_TouchPhase.E_TP_MOUSEOVER_END:
				return m_Callback.Callback(GUIBase_Callback.E_CallbackType.E_CT_ON_MOUSEOVER_END);
			case E_TouchPhase.E_TP_CLICK_RELEASE_KEYBOARD:
				return m_Callback.Callback(GUIBase_Callback.E_CallbackType.E_CT_ON_TOUCH_END_KEYBOARD);
			default:
				return false;
		}
	}

	private float GetParentFadeAlpha()
	{
		float result = 1f;
		if ((bool)m_ParentWidget)
		{
			result = m_ParentWidget.GetFadeAlpha(true);
		}
		else if ((bool)m_ParentPivot)
		{
			result = m_ParentPivot.GetFadeAlpha(true);
		}
		else if ((bool)m_ParentLayout)
		{
			result = m_ParentLayout.GetFadeAlpha(true);
		}
		return result;
	}

	public float GetFadeAlpha(bool recursive)
	{
		return m_FadeAlpha * GetParentFadeAlpha();
	}

	private void PrepareParent()
	{
		Transform parent = base.gameObject.transform.parent;
		GameObject gameObject = parent.gameObject;
		if (!gameObject)
		{
			return;
		}
		m_ParentLayout = gameObject.GetComponent<GUIBase_Layout>();
		if (!m_ParentLayout)
		{
			m_ParentPivot = gameObject.GetComponent<GUIBase_Pivot>();
			if (!m_ParentPivot)
			{
				m_ParentWidget = gameObject.GetComponent<GUIBase_Widget>();
			}
		}
	}
}