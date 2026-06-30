using UnityEngine;

public class HudActions : HudComponent
{
	private GUIBase_Button m_AttackButton;

	private GUIBase_Button m_UseButton;

	private GUIBase_Button m_ReloadButton;

	private GUIBase_Button m_AimButton;

	private string s_PivotMainName = "MainHUD";

	private string s_LayoutMainName = "HUD_Layout";

	private string s_AttackButtonName = "FireButton";

	private string s_ReloadButtonName = "ReloadButton";

	private string s_UseButtonName = "UseButton";

	private string s_AimButtonName = "AimButton";

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
		m_AttackButton = GuiBaseUtils.RegisterButtonDelegate(layout, s_AttackButtonName, AttackButtonBeginDelegate, AttackButtonEndDelegate);
		m_ReloadButton = GuiBaseUtils.RegisterButtonDelegate(layout, s_ReloadButtonName, OnReloadButton, null);
		m_UseButton = GuiBaseUtils.RegisterButtonDelegate(layout, s_UseButtonName, UseButtonDelegate, null);
		m_AimButton = GuiBaseUtils.RegisterButtonDelegate(layout, s_AimButtonName, OnAimButton, null);
	}

	public override void Reset()
	{
	}

	public override void LateUpdate(float deltaTime)
	{
		base.LateUpdate(deltaTime);
#if !UNITY_PSP2
		if (!(Player.Instance == null) && !(Player.Instance.Owner == null) && !(Player.Instance.Owner.WeaponComponent == null) && Player.Instance.Owner.WeaponComponent.CurrentWeapon != 0)
		{
			WeaponBase weapon = Player.Instance.Owner.WeaponComponent.GetWeapon(Player.Instance.Owner.WeaponComponent.CurrentWeapon);
			if (weapon != null && weapon.WeaponAmmo > 0 && !weapon.IsFullyLoaded)
			{
				m_ReloadButton.SetDisabled(false);
			}
			else
			{
				m_ReloadButton.SetDisabled(true);
			}
		}
#else
		m_ReloadButton.SetDisabled(true);
#endif
	}

	protected override void ShowWidgets(bool on)
	{
		if (on)
		{
			m_AttackButton.Widget.Show(true, true);
		}
		else
		{
			m_AttackButton.Widget.Show(false, true);
			m_UseButton.Widget.Show(false, true);
		}
		m_ReloadButton.Widget.Show(on, true);
		m_AimButton.Widget.Show(on, true);
	}

	public override void StoreControlsOrigPositions()
	{
		base.StoreControlsOrigPositions();
		GuiOptions.FireUseButton.OrigPos = new Vector2(m_AttackButton.transform.position.x, m_AttackButton.transform.position.y);
		GuiOptions.ReloadButton.OrigPos = new Vector2(m_ReloadButton.transform.position.x, m_ReloadButton.transform.position.y);
		GuiOptions.AimButton.OrigPos = new Vector2(m_AimButton.transform.position.x, m_AimButton.transform.position.y);
	}

	public override void UpdateControlsPosition()
	{
		base.UpdateControlsPosition();
		m_AttackButton.transform.position = GuiOptions.FireUseButton.Positon;
		m_ReloadButton.transform.position = GuiOptions.ReloadButton.Positon;
		m_UseButton.transform.position = GuiOptions.FireUseButton.Positon;
		m_AimButton.transform.position = GuiOptions.AimButton.Positon;
	}

	private void AttackButtonBeginDelegate()
	{
		Player.Instance.Controls.FireDownDelegate();
	}

	private void AttackButtonEndDelegate(bool inside)
	{
		if (Player.Instance != null && Player.Instance.Controls != null)
		{
			Player.Instance.Controls.FireUpDelegate();
		}
	}

	private void UseButtonDelegate()
	{
		if (Player.Instance != null && Player.Instance.Controls != null)
		{
			Player.Instance.Controls.UseDelegate();
		}
	}

	private void OnReloadButton()
	{
		if (Player.Instance != null && Player.Instance.Controls != null)
		{
			Player.Instance.Controls.ReloadDelegate();
		}
	}

	private void OnAimButton()
	{
		Player.Instance.Controls.IronSightDelegate();
	}
}
