using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Mathematics.math;
public class HudRadar : HudComponent
{
	private class SpecialObject
	{
		public GUIBase_Sprite sprite;

		public GuiHUD.E_RadarObjectType objType;

		public GameObject obj;

		public bool highlight;

		public bool used
		{
			get
			{
				return obj != null;
			}
		}

		public SpecialObject()
		{
			Free();
		}

		public void Use(GameObject inObj)
		{
			obj = inObj;
		}

		public void Free()
		{
			obj = null;
		}
	}

	public const float RadarMaxRange = 15f;

	public const float RadarLowRange = 10f;

	public const float ZombieBehindRange = 3.3f;

	public const float ZombieBehindMinAngle = 1.8707964f;

	public const float ZombieBehindMaxAngle = 4.412389f;

	private const float PulseTime = 0.8f;

	private const float PulseFadeTime = 1f;

	private const float DelayBetweenPulses = 2f;

	private const float PulseRadarHighlightBlendInStart = 0.6f;

	private const float PulseRadarHighlightBlendInStop = 1f;

	private const float PulseRadarHighlightTime = 1.1f;

	private const float PulseRadarHighlightFade = 1.5f;

	private List<SpecialObject> SpecialObjects = new List<SpecialObject>();

	private GUIBase_Sprite Radar;

	private GUIBase_Sprite RadarCenter;

	private GUIBase_Sprite Pulse;

	private GUIBase_Widget EnemyBehind;

	private GUIBase_Sprite[] RadarEnemies;

	private float PulseTimer;

	private float RadarRange;

	private float RadarScreenRadius;

	private float RadarCenterRadius;

	private string s_PivotMainName = "MainHUD";

	private string s_LayoutMainName = "HUD_Layout";

	private string s_RadarName = "Radar";

	private string s_RadarCenterName = "Center";

	private string s_PulseName = "Pulse";

	private string s_CarryObjectSourceName = "CarryObjectSource";

	private string s_CarryObjectTargetName = "CarryObjectTarget";

	private string s_RadarTargetName = "RadarTarget";

	private string s_ProtectObjectName1 = "ProtectObject1";

	private string s_ProtectObjectName2 = "ProtectObject2";

	private string[] s_RadarEnemyNames = new string[9] { "Enemy1", "Enemy2", "Enemy3", "Enemy4", "Enemy5", "Enemy6", "Enemy7", "Enemy8", "Enemy9" };

	public override bool VisibleOnStart()
	{
		return true;
	}

	public override void Init()
	{
		GUIBase_Pivot pivot = MFGuiManager.Instance.GetPivot(s_PivotMainName);
		if (!pivot)
		{
			Debug.LogError("'" + s_PivotMainName + "' not found!!! Assert should come now");
			return;
		}
		GUIBase_Layout layout = pivot.GetLayout(s_LayoutMainName);
		if (!layout)
		{
			Debug.LogError("'" + s_LayoutMainName + "' not found!!! Assert should come now");
			return;
		}
		Radar = layout.GetWidget(s_RadarName).GetComponent<GUIBase_Sprite>();
		RadarCenter = layout.GetWidget(s_RadarCenterName).GetComponent<GUIBase_Sprite>();
		Pulse = layout.GetWidget(s_PulseName).GetComponent<GUIBase_Sprite>();
		Pulse.transform.localScale = Vector3.zero;
		PulseTimer = 0f;
		RadarEnemies = new GUIBase_Sprite[s_RadarEnemyNames.Length];
		int num = 0;
		string[] array = s_RadarEnemyNames;
		foreach (string wName in array)
		{
			RadarEnemies[num++] = layout.GetWidget(wName).GetComponent<GUIBase_Sprite>();
		}
		Vector2 vector = new Vector2(Radar.Widget.m_Width - 44f, Radar.Widget.m_Width - 44f);
		vector.x *= Radar.transform.lossyScale.x;
		vector.y *= Radar.transform.lossyScale.y;
		RadarScreenRadius = vector.x / 2f;
		vector = new Vector2(RadarCenter.Widget.m_Width, RadarCenter.Widget.m_Width);
		vector.x *= Radar.transform.lossyScale.x;
		vector.y *= Radar.transform.lossyScale.y;
		RadarCenterRadius = vector.x / 2f;
		RadarScreenRadius -= RadarCenterRadius;
		SpecialObject specialObject = new SpecialObject();
		specialObject.sprite = layout.GetWidget(s_CarryObjectSourceName).GetComponent<GUIBase_Sprite>();
		specialObject.objType = GuiHUD.E_RadarObjectType.CarryObjectSource;
		SpecialObjects.Add(specialObject);
		specialObject = new SpecialObject();
		specialObject.sprite = layout.GetWidget(s_CarryObjectTargetName).GetComponent<GUIBase_Sprite>();
		specialObject.objType = GuiHUD.E_RadarObjectType.CarryObjectTarget;
		SpecialObjects.Add(specialObject);
		specialObject = new SpecialObject();
		specialObject.sprite = layout.GetWidget(s_RadarTargetName).GetComponent<GUIBase_Sprite>();
		specialObject.objType = GuiHUD.E_RadarObjectType.Target;
		SpecialObjects.Add(specialObject);
		specialObject = new SpecialObject();
		specialObject.sprite = layout.GetWidget(s_ProtectObjectName1).GetComponent<GUIBase_Sprite>();
		specialObject.objType = GuiHUD.E_RadarObjectType.ProtectObject;
		SpecialObjects.Add(specialObject);
		specialObject = new SpecialObject();
		specialObject.sprite = layout.GetWidget(s_ProtectObjectName2).GetComponent<GUIBase_Sprite>();
		specialObject.objType = GuiHUD.E_RadarObjectType.ProtectObject;
		SpecialObjects.Add(specialObject);
		GUIBase_Label childByName = GetChildByName<GUIBase_Label>(Radar.gameObject, "Text");
		GUIBase_Sprite component = layout.GetWidget("RadarLow").GetComponent<GUIBase_Sprite>();
		if (Game.Instance.PlayerPersistentInfo.Upgrades.ContainsUpgrade(E_UpgradeID.ImproveRadar))
		{
			RadarRange = 15f;
			childByName.SetNewText(3000110);
			EnemyBehind = layout.GetWidget("ZombiesBehind").GetComponent<GUIBase_Widget>();
		}
		else
		{
			Radar.Widget.CopyMaterialSettings(component.Widget);
			RadarRange = 10f;
			childByName.SetNewText(3000100);
			EnemyBehind = layout.GetWidget("ZombiesBehind").GetComponent<GUIBase_Widget>();
			EnemyBehind.m_FadeAlpha = 0f;
			EnemyBehind = null;
			GUIBase_Widget component2 = layout.GetWidget("ZombiesBehindBck").GetComponent<GUIBase_Widget>();
			component2.m_FadeAlpha = 0f;
		}
		component.Widget.Show(false, true);
	}

	public override void Reset()
	{
	}

	public override void LateUpdate(float deltaTime)
	{
		UpdatePulse(deltaTime);
	}

	public override void LateUpdate100ms()
	{
		UpdateRadarInternal();
	}

	protected override void ShowWidgets(bool on)
	{
		Radar.Widget.Show(on, true);
		if (on)
		{
			UpdateRadarInternal(true);
		}
	}

	public void RegisterObject(GameObject obj, GuiHUD.E_RadarObjectType objType)
	{
		foreach (SpecialObject specialObject in SpecialObjects)
		{
			if (specialObject.objType == objType && !specialObject.used)
			{
				specialObject.Use(obj);
				return;
			}
		}
		Debug.LogWarning("RegisterObject - can't find free sprite for type: " + objType);
	}

	public void HighlightObject(GameObject obj, bool highlight)
	{
		foreach (SpecialObject specialObject in SpecialObjects)
		{
			if (!(specialObject.obj == obj))
			{
				continue;
			}
			if (specialObject.highlight != highlight)
			{
				specialObject.highlight = highlight;
				if (!highlight)
				{
					specialObject.sprite.StopAllCoroutines();
				}
				else
				{
					specialObject.sprite.StartCoroutine(HighlightObject(specialObject.sprite));
				}
			}
			return;
		}
		Debug.LogWarning("HighlightObject - can't find object: " + obj);
	}

	public void UnregisterObject(GameObject obj, GuiHUD.E_RadarObjectType objType)
	{
		foreach (SpecialObject specialObject in SpecialObjects)
		{
			if (specialObject.objType == objType && specialObject.obj == obj)
			{
				if (specialObject.highlight)
				{
					specialObject.sprite.StopAllCoroutines();
				}
				specialObject.Free();
			}
		}
	}

	private IEnumerator HighlightObject(GUIBase_Sprite sprite)
	{
		while (true)
		{
			sprite.Widget.m_FadeAlpha = 0.4f;
			yield return new WaitForSeconds(0.3f);
			sprite.Widget.m_FadeAlpha = 1f;
			yield return new WaitForSeconds(0.3f);
		}
	}

	private T GetChildByName<T>(GameObject obj, string name) where T : Component
	{
		Transform transform = obj.transform.Find(name);
		return (!(transform != null)) ? ((T)null) : transform.GetComponent<T>();
	}

	private void UpdatePulse(float deltaTime)
	{
		PulseTimer += deltaTime;
		if (PulseTimer > 2f)
		{
			PulseTimer -= 2f;
			Pulse.Widget.Show(true, false);
			Pulse.Widget.m_FadeAlpha = 0.5f;
		}
		if (PulseTimer <= 0.8f)
		{
			float num = 1f - Mathf.Cos(PulseTimer / 0.8f * (float)Math.PI / 2f);
			Pulse.transform.localScale = new Vector3(num, num, num);
		}
		else if (PulseTimer <= 1f)
		{
			Pulse.Widget.m_FadeAlpha = 0.5f * (1f - (PulseTimer - 0.8f) / 0.19999999f);
		}
		else if (PulseTimer <= 2f)
		{
			Pulse.Widget.Show(false, false);
		}
	}

	private float GetPulseModificator()
	{
		if (PulseTimer < 0.6f)
		{
			return 0.5f;
		}
		if (PulseTimer < 1f)
		{
			return 0.5f + (PulseTimer - 0.6f) / 0.39999998f * 0.5f;
		}
		if (PulseTimer <= 1.1f)
		{
			return 1f;
		}
		if (PulseTimer <= 1.5f)
		{
			return 0.5f + (1f - (PulseTimer - 1.1f) / 0.39999998f) * 0.5f;
		}
		return 0.5f;
	}

	private void UpdateRadarInternal(bool forced = false)
	{
		if (Camera.main == null || !Player.Instance || (!forced && (!IsVisible() || !IsEnabled())))
		{
			return;
		}
		Vector3 position = Player.Instance.transform.position;
		Vector3 forward = Player.Instance.transform.forward;
		bool v = false;
		forward.y = 0f;
		int i = 0;
		float pulseModificator = GetPulseModificator();
		foreach (Agent enemy in Mission.Instance.CurrentGameZone.Enemies)
		{
			Vector3 position2 = enemy.transform.position;
			Vector3 vector = position2 - position;
			float magnitude = length(vector);
			vector.y = 0f;
			float magnitude2 = length(vector);
			float num = magnitude2;
			if (magnitude2 < RadarRange)
			{
				vector.Normalize();
				float f = Mathf.Atan2(0f - vector.z, vector.x) - Mathf.Atan2(0f - forward.z, forward.x);
				if (magnitude < 3.3f && Mathf.Abs(f) > 1.8707964f && Mathf.Abs(f) < 4.412389f)
				{
					v = true;
				}
				magnitude2 /= RadarRange;
				num /= RadarRange;
				float num2 = Mathf.Clamp((1f - num) * 2f, 0.9f, 1.3f);
				float num3 = 1f;
				num3 *= pulseModificator;
				RadarEnemies[i].transform.position = new Vector3(Mathf.Sin(f), 0f - Mathf.Cos(f), 0f);
				RadarEnemies[i].transform.position *= magnitude2 * RadarScreenRadius + RadarCenterRadius;
				RadarEnemies[i].transform.position += RadarCenter.transform.position;
				RadarEnemies[i].transform.localScale = new Vector3(num2, num2, 0f);
				if (!RadarEnemies[i].Widget.IsVisible())
				{
					RadarEnemies[i].Widget.Show(true, false);
				}
				RadarEnemies[i].Widget.m_Color = new Color(num3, 0f, 0f);
				i++;
			}
		}
		for (; i < RadarEnemies.Length; i++)
		{
			if (RadarEnemies[i].Widget.IsVisible() || forced)
			{
				RadarEnemies[i].Widget.Show(false, false);
			}
		}
		if ((bool)EnemyBehind)
		{
			EnemyBehind.Show(v, true);
		}
		foreach (SpecialObject specialObject in SpecialObjects)
		{
			if (specialObject.used)
			{
				Vector3 position3 = specialObject.obj.transform.position;
				Vector3 vector2 = position3 - position;
				vector2.y = 0f;
				float magnitude3 = length(vector2);
				vector2.Normalize();
				magnitude3 = Mathf.Clamp(magnitude3 / RadarRange, 0f, 1f);
				float f2 = Mathf.Atan2(0f - vector2.z, vector2.x) - Mathf.Atan2(0f - forward.z, forward.x);
				specialObject.sprite.transform.position = new Vector3(Mathf.Sin(f2), 0f - Mathf.Cos(f2), 0f);
				specialObject.sprite.transform.position *= magnitude3 * RadarScreenRadius + RadarCenterRadius;
				specialObject.sprite.transform.position += RadarCenter.transform.position;
				if (!specialObject.sprite.Widget.IsVisible())
				{
					specialObject.sprite.Widget.Show(true, false);
				}
				if (!specialObject.highlight)
				{
					specialObject.sprite.Widget.m_FadeAlpha = 1f;
				}
			}
			else if (specialObject.sprite.Widget.IsVisible() || forced)
			{
				specialObject.sprite.Widget.Show(false, false);
			}
		}
	}
}
