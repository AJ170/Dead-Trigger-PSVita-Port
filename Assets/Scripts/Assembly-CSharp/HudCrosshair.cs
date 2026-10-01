using UnityEngine;

public class HudCrosshair : HudComponent
{
	private float TimeToFinishIronSight;

	private GUIBase_Widget CrosshairParent;

	private GUIBase_Sprite CrosshairCenter;

	private GUIBase_Sprite CrosshairLeft;

	private GUIBase_Sprite CrosshairRight;

	private GUIBase_Sprite CrosshairUp;

	private GUIBase_Sprite CrosshairDown;

	private GUIBase_Sprite HitLeftUp;

	private GUIBase_Sprite HitLeftDown;

	private GUIBase_Sprite HitRightUp;

	private GUIBase_Sprite HitRightDown;

	private bool m_TargetHit;

	private float m_TargetHitTimer;

	private Color m_NeutralEffective = new Color(1f, 1f, 1f, 0f);

	private Color m_EnemyEffective = new Color(1f, 0.1f, 0.1f, 0f);

	private Color m_EnemyIneffective = new Color(0.2f, 0.1f, 0.05f, 0.25f);

	private string s_PivotMainName = "MainHUD";

	private string s_LayoutMainName = "HUD_Layout";

	private string s_CrosshairParentName = "Crosshair";

	private string[] s_CrosshairName = new string[9] { "CrosshairCenter", "CrosshairLeft", "CrosshairRight", "CrosshairUp", "CrosshairDown", "HitLeftUp", "HitLeftDown", "HitRightUp", "HitRightDown" };

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
		CrosshairParent = layout.GetWidget(s_CrosshairParentName).GetComponent<GUIBase_Widget>();
		CrosshairCenter = layout.GetWidget(s_CrosshairName[0]).GetComponent<GUIBase_Sprite>();
		CrosshairLeft = layout.GetWidget(s_CrosshairName[1]).GetComponent<GUIBase_Sprite>();
		CrosshairRight = layout.GetWidget(s_CrosshairName[2]).GetComponent<GUIBase_Sprite>();
		CrosshairUp = layout.GetWidget(s_CrosshairName[3]).GetComponent<GUIBase_Sprite>();
		CrosshairDown = layout.GetWidget(s_CrosshairName[4]).GetComponent<GUIBase_Sprite>();
		HitLeftUp = layout.GetWidget(s_CrosshairName[5]).GetComponent<GUIBase_Sprite>();
		HitLeftDown = layout.GetWidget(s_CrosshairName[6]).GetComponent<GUIBase_Sprite>();
		HitRightUp = layout.GetWidget(s_CrosshairName[7]).GetComponent<GUIBase_Sprite>();
		HitRightDown = layout.GetWidget(s_CrosshairName[8]).GetComponent<GUIBase_Sprite>();
	}

	public override void Reset()
	{
	}

	public override void LateUpdate(float deltaTime)
	{
		if (IsVisible())
		{
			// The option rides the same zero-scale hide the melee and iron-sight cases
			// already use. Re-asserted every frame, so nothing else can turn it back on.
			if (!GuiOptions.crosshair || (Player.Instance != null && Player.Instance.Owner.WeaponComponent.CurrentWeapon != 0 && (Player.Instance.Owner.WeaponComponent.GetCurrentWeapon().WeaponType == E_WeaponType.Melee || (!Player.Instance.Owner.WeaponComponent.GetCurrentWeapon().IronSightCrosshair() && Player.Instance.Owner.BlackBoard.Desires.WeaponIronSight))))
			{
				CrosshairParent.transform.localScale = Vector3.zero;
			}
			else
			{
				CrosshairParent.transform.localScale = Vector3.one;
			}
			UpdateCrosshairMovement(deltaTime);
		}
	}

	public override void LateUpdate100ms()
	{
		UpdateCrosshairInternal();
	}

	protected override void ShowWidgets(bool on)
	{
		CrosshairParent.Show(on, true);
		if (on && !m_TargetHit)
		{
			HideHitIndicator();
		}
	}

	public void TargetHit()
	{
		if (!m_TargetHit && IsEnabled())
		{
			m_TargetHit = true;
			HitLeftUp.Widget.m_FadeAlpha = 1f;
			HitLeftDown.Widget.m_FadeAlpha = 1f;
			HitRightUp.Widget.m_FadeAlpha = 1f;
			HitRightDown.Widget.m_FadeAlpha = 1f;
			HitLeftUp.Widget.Show(true, true);
			HitLeftDown.Widget.Show(true, true);
			HitRightUp.Widget.Show(true, true);
			HitRightDown.Widget.Show(true, true);
		}
		m_TargetHitTimer = 0f;
	}

	private void HideHitIndicator()
	{
		HitLeftUp.Widget.Show(false, true);
		HitLeftDown.Widget.Show(false, true);
		HitRightUp.Widget.Show(false, true);
		HitRightDown.Widget.Show(false, true);
	}

	private void UpdateCrosshairInternal()
	{
		if (!IsVisible() || !IsEnabled() || Camera.main == null || !Player.Instance)
		{
			return;
		}
		bool targetFound = false;
		WeaponBase weapon = Player.Instance.Owner.WeaponComponent.GetWeapon(Player.Instance.Owner.WeaponComponent.CurrentWeapon);
		HitUtils.HitData hitData = default(HitUtils.HitData);
		if ((bool)weapon)
		{
			weapon.ComputeAimAssistDir(out targetFound, out hitData);
		}
		else
		{
			hitData.hitPos = Vector3.zero;
			hitData.distance = 1000f;
			targetFound = false;
		}
		if (targetFound)
		{
		}
		if ((bool)CrosshairLeft && (bool)CrosshairCenter && (bool)CrosshairRight)
		{
			float num = 1f;
			if (Player.Instance != null && Player.Instance.Owner != null && Player.Instance.Owner.WeaponComponent != null && Player.Instance.Owner.WeaponComponent.CurrentWeapon != 0 && (bool)weapon && targetFound)
			{
				num = weapon.DamageByRangeRatio(hitData.distance);
			}
			Color color = ((!targetFound) ? m_NeutralEffective : ((num > 0.95f) ? m_EnemyEffective : ((!(num < 0.1f)) ? (m_EnemyEffective * 0.5f * num + m_EnemyIneffective * (1f - num)) : m_EnemyIneffective)));
			CrosshairCenter.Widget.m_Color = color;
			CrosshairLeft.Widget.m_Color = color;
			CrosshairRight.Widget.m_Color = color;
			CrosshairUp.Widget.m_Color = color;
			CrosshairDown.Widget.m_Color = color;
			if (m_TargetHit)
			{
				HitLeftUp.Widget.m_Color = color;
				HitLeftDown.Widget.m_Color = color;
				HitRightUp.Widget.m_Color = color;
				HitRightDown.Widget.m_Color = color;
			}
		}
	}

	private void UpdateCrosshairMovement(float deltaTime)
	{
		if (Camera.main == null)
		{
			return;
		}
		if ((bool)CrosshairLeft && (bool)CrosshairCenter && (bool)CrosshairRight)
		{
			float num = 0f;
			float num2 = 0f;
			if (Player.Instance != null && Player.Instance.Owner != null && Player.Instance.Owner.WeaponComponent != null && Player.Instance.Owner.WeaponComponent.CurrentWeapon != 0)
			{
				WeaponBase currentWeapon = Player.Instance.Owner.WeaponComponent.GetCurrentWeapon();
				if ((bool)currentWeapon)
				{
					num = Mathf.Clamp(currentWeapon.FinalDispersion, 0f, 25f);
					num = num * (float)Screen.width * 0.006f;
					num2 = num;
				}
			}
			float num3 = CrosshairLeft.Widget.GetWidth() / 3f;
			num += num3;
			num2 += num3;
			Vector3 position = CrosshairCenter.transform.position;
			position.x -= num;
			if (CrosshairLeft.transform.position != position)
			{
				CrosshairLeft.transform.position = position;
				position.x += num + num;
				CrosshairRight.transform.position = position;
				position.x -= num;
				position.y -= num2;
				CrosshairUp.transform.position = position;
				position.y += num2 + num2;
				CrosshairDown.transform.position = position;
			}
		}
		if (m_TargetHit)
		{
			m_TargetHitTimer += deltaTime;
			if (m_TargetHitTimer > 0.15f)
			{
				float fadeAlpha = Mathf.Clamp(1f - (m_TargetHitTimer - 0.15f) / 0.3f, 0f, 1f);
				HitLeftUp.Widget.m_FadeAlpha = fadeAlpha;
				HitLeftDown.Widget.m_FadeAlpha = fadeAlpha;
				HitRightUp.Widget.m_FadeAlpha = fadeAlpha;
				HitRightDown.Widget.m_FadeAlpha = fadeAlpha;
			}
			if (m_TargetHitTimer > 0.45000002f)
			{
				m_TargetHit = false;
				HideHitIndicator();
			}
		}
	}
}
