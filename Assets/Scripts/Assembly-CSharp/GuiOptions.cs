using System.Collections.Generic;
using UnityEngine;

public class GuiOptions
{
	public enum E_ControlScheme
	{
		Scheme1 = 0,
		Scheme2 = 1
	}

	public enum E_ControlSide
	{
		Neutral = 0,
		LeftHand = 1,
		RightHand = 2
	}

	public class ControlPos
	{
		public Vector2 OrigPos;

		public Vector2 Offset;

		public E_ControlSide Side;

		public Vector2 Positon
		{
			get
			{
				return OrigPos + Offset;
			}
		}

		public ControlPos(E_ControlSide _side)
		{
			Side = _side;
		}
	}

	public static float sensitivity = 1f;

	// Hip-fire camera FOV, driven by FOV_Slider on the CONTROLS options page.
	// GameCamera uses this as its DefaultFOV, so iron-sight zoom still overrides
	// it and SetDefaultFov() returns to the player's chosen value.
	public static float fov = 55f;

	// Aim-down-sights FOV, driven by AimFOV_Slider. Each weapon carries its own
	// WeaponSettings.AimFov (a sniper zooms further than a pistol), so this is
	// applied as a DELTA from DefaultAimFov rather than as an absolute override -
	// that keeps the per-weapon differences intact. Leaving the slider at 50 gives
	// exactly the OEM behaviour.
	public static float aimFov = 50f;

	public static float musicVolume = 1f;

	public static bool subtitles = true;

	public static bool invertYAxis = false;

	public static bool leftHandAiming = false;

	private static E_ControlScheme DefaultScheme = E_ControlScheme.Scheme2;

	public static E_ControlScheme m_ControlScheme = DefaultScheme;

	public static float ListenerVolume = 1f;

	public static int graphicDetail = 0;

	public static bool musicOn = true;

	public static bool showMogaHelp = true;

	public static float DefaultFov = 55f;

	public static float MinFov = 45f;

	public static float MaxFov = 90f;

	public static float DefaultAimFov = 50f;

	public static float MinAimFov = 35f;

	public static float MaxAimFov = 75f;

	private static float DefaultMusicVolume = 0.7f;

	private static bool DefaultSubtitles = true;

	private static bool DefaultInvertYAxis = false;

	private static bool DefaultLeftHandAiming = false;

	private static bool DefaultMusicOn = true;

	public static bool leftHandControlsNeedUpdate = false;

	public static ControlPos FireUseButton = new ControlPos(E_ControlSide.RightHand);

	public static ControlPos PauseButton = new ControlPos(E_ControlSide.Neutral);

	public static ControlPos WeaponButton = new ControlPos(E_ControlSide.Neutral);

	public static ControlPos[] GadgetButtons = new ControlPos[5]
	{
		new ControlPos(E_ControlSide.RightHand),
		new ControlPos(E_ControlSide.RightHand),
		new ControlPos(E_ControlSide.RightHand),
		new ControlPos(E_ControlSide.RightHand),
		new ControlPos(E_ControlSide.RightHand)
	};

	public static ControlPos MoveStick = new ControlPos(E_ControlSide.LeftHand);

	public static ControlPos ReloadButton = new ControlPos(E_ControlSide.RightHand);

	public static ControlPos AimButton = new ControlPos(E_ControlSide.RightHand);

	private static float DefaultSensitivity
	{
		get
		{
			if (Game.IsHDResolution() && Application.platform != RuntimePlatform.WindowsEditor)
			{
				return 4f;
			}
			return 1.8f;
		}
	}

	public static void SetNewLeftHandAiming(bool newVal)
	{
		if (leftHandAiming != newVal)
		{
			leftHandAiming = newVal;
			if ((bool)GuiHUD.Instance && GuiHUD.Instance.Initialised)
			{
				SwitchLeftHandAimingControls();
			}
			else
			{
				leftHandControlsNeedUpdate = true;
			}
		}
	}

	// Pushes the chosen FOV to the live camera. Safe to call from the menu at any
	// time - there is no GameCamera in the main menu, so it just no-ops there.
	public static void ApplyFov()
	{
		fov = Mathf.Clamp(fov, MinFov, MaxFov);
		if (GameCamera.Instance != null)
		{
			GameCamera.Instance.ApplyFovOption();
		}
	}

	// Shifts a weapon's own aim FOV by the player's chosen delta. Clamped so an
	// extreme slider value can never invert or flatten a scope.
	public static float GetAimFov(float weaponAimFov)
	{
		float delta = Mathf.Clamp(aimFov, MinAimFov, MaxAimFov) - DefaultAimFov;
		return Mathf.Clamp(weaponAimFov + delta, 10f, 100f);
	}

	public static int GetDefaultGraphics()
	{
		return (int)DeviceInfo.GetDetectedPerformanceLevel();
	}

	public static void ResetToDefaults()
	{
		sensitivity = DefaultSensitivity;
		fov = DefaultFov;
		aimFov = DefaultAimFov;
		ApplyFov();
		musicVolume = DefaultMusicVolume;
		subtitles = DefaultSubtitles;
		invertYAxis = DefaultInvertYAxis;
		m_ControlScheme = DefaultScheme;
		SetNewLeftHandAiming(DefaultLeftHandAiming);
		musicOn = DefaultMusicOn;
		graphicDetail = GetDefaultGraphics();
		showMogaHelp = true;
	}

	public static void SwitchLeftHandAimingControls()
	{
		List<ControlPos> list = new List<ControlPos>();
		list.Add(FireUseButton);
		list.Add(PauseButton);
		list.Add(WeaponButton);
		list.Add(MoveStick);
		list.Add(ReloadButton);
		list.Add(AimButton);
		for (int i = 0; i < GadgetButtons.Length; i++)
		{
			list.Add(GadgetButtons[i]);
		}
		foreach (ControlPos item in list)
		{
			if (item.Side != 0)
			{
				bool flag = false;
				if (item.Side == E_ControlSide.LeftHand)
				{
					flag = (!leftHandAiming && item.Positon.x > (float)(Screen.width / 2)) || (leftHandAiming && item.Positon.x < (float)(Screen.width / 2));
				}
				else if (item.Side == E_ControlSide.RightHand)
				{
					flag = (!leftHandAiming && item.Positon.x < (float)(Screen.width / 2)) || (leftHandAiming && item.Positon.x > (float)(Screen.width / 2));
				}
				if (flag)
				{
					float num = (float)Screen.width - item.Positon.x;
					float x = num - item.OrigPos.x;
					item.Offset.x = x;
				}
			}
		}
		GuiHUD.Instance.UpdateControlsPosition();
		if (Player.Instance != null && Player.Instance.Controls.TouchControls != null)
		{
			Player.Instance.Controls.TouchControls.OnControlSchemeChange();
		}
		leftHandControlsNeedUpdate = false;
	}

	public static void Save()
	{
		PlayerPrefs.SetFloat("OptionsSensitivity", sensitivity);
		PlayerPrefs.SetFloat("OptionsFov", fov);
		PlayerPrefs.SetFloat("OptionsAimFov", aimFov);
		PlayerPrefs.SetFloat("OptionsMusicVolume", musicVolume);
		PlayerPrefs.SetInt("OptionsSubtitles", subtitles ? 1 : 0);
		PlayerPrefs.SetInt("OptionsInvertYAxis", invertYAxis ? 1 : 0);
		PlayerPrefs.SetInt("OptionsLeftHandAiming", leftHandAiming ? 1 : 0);
		PlayerPrefs.SetInt("OptionsControlScheme", (int)m_ControlScheme);
		PlayerPrefs.SetFloat("OptionsFireButtonX", FireUseButton.Offset.x);
		PlayerPrefs.SetFloat("OptionsFireButtonY", FireUseButton.Offset.y);
		PlayerPrefs.SetFloat("OptionsPauseButtonX", PauseButton.Offset.x);
		PlayerPrefs.SetFloat("OptionsPauseButtonY", PauseButton.Offset.y);
		PlayerPrefs.SetFloat("OptionsWeaponButtonX", WeaponButton.Offset.x);
		PlayerPrefs.SetFloat("OptionsWeaponButtonY", WeaponButton.Offset.y);
		for (int i = 0; i < GadgetButtons.Length; i++)
		{
			PlayerPrefs.SetFloat("OptionsGadgetButtonX" + i, GadgetButtons[i].Offset.x);
			PlayerPrefs.SetFloat("OptionsGadgetButtonY" + i, GadgetButtons[i].Offset.y);
		}
		PlayerPrefs.SetFloat("OptionsMoveStickX", MoveStick.Offset.x);
		PlayerPrefs.SetFloat("OptionsMoveStickY", MoveStick.Offset.y);
		PlayerPrefs.SetFloat("OptionsReloadButtonX", ReloadButton.Offset.x);
		PlayerPrefs.SetFloat("OptionsReloadButtonY", ReloadButton.Offset.y);
		PlayerPrefs.SetFloat("OptionsAimButtonX", AimButton.Offset.x);
		PlayerPrefs.SetFloat("OptionsAimButtonY", AimButton.Offset.y);
		PlayerPrefs.SetInt("OptionsGraphicDetail", graphicDetail);
		PlayerPrefs.SetInt("OptionsMusicOn", musicOn ? 1 : 0);
		PlayerPrefs.SetInt("OptionsShowMogaHelp", showMogaHelp ? 1 : 0);
		PlayerPrefs.SetInt("OptionsLeftHandControlsNeedUpdate", leftHandControlsNeedUpdate ? 1 : 0);
	}

	public static void Load()
	{
		sensitivity = PlayerPrefs.GetFloat("OptionsSensitivity", DefaultSensitivity);
		fov = Mathf.Clamp(PlayerPrefs.GetFloat("OptionsFov", DefaultFov), MinFov, MaxFov);
		aimFov = Mathf.Clamp(PlayerPrefs.GetFloat("OptionsAimFov", DefaultAimFov), MinAimFov, MaxAimFov);
		ApplyFov();
		musicVolume = PlayerPrefs.GetFloat("OptionsMusicVolume", DefaultMusicVolume);
		subtitles = PlayerPrefs.GetInt("OptionsSubtitles", DefaultSubtitles ? 1 : 0) != 0;
		invertYAxis = PlayerPrefs.GetInt("OptionsInvertYAxis", DefaultInvertYAxis ? 1 : 0) != 0;
		leftHandAiming = PlayerPrefs.GetInt("OptionsLeftHandAiming", DefaultLeftHandAiming ? 1 : 0) != 0;
		m_ControlScheme = (E_ControlScheme)PlayerPrefs.GetInt("OptionsControlScheme", (int)DefaultScheme);
		FireUseButton.Offset.x = PlayerPrefs.GetFloat("OptionsFireButtonX", 0f);
		FireUseButton.Offset.y = PlayerPrefs.GetFloat("OptionsFireButtonY", 0f);
		PauseButton.Offset.x = PlayerPrefs.GetFloat("OptionsPauseButtonX", 0f);
		PauseButton.Offset.y = PlayerPrefs.GetFloat("OptionsPauseButtonY", 0f);
		WeaponButton.Offset.x = PlayerPrefs.GetFloat("OptionsWeaponButtonX", 0f);
		WeaponButton.Offset.y = PlayerPrefs.GetFloat("OptionsWeaponButtonY", 0f);
		for (int i = 0; i < GadgetButtons.Length; i++)
		{
			GadgetButtons[i].Offset.x = PlayerPrefs.GetFloat("OptionsGadgetButtonX" + i, 0f);
			GadgetButtons[i].Offset.y = PlayerPrefs.GetFloat("OptionsGadgetButtonY" + i, 0f);
		}
		MoveStick.Offset.x = PlayerPrefs.GetFloat("OptionsMoveStickX", 0f);
		MoveStick.Offset.y = PlayerPrefs.GetFloat("OptionsMoveStickY", 0f);
		ReloadButton.Offset.x = PlayerPrefs.GetFloat("OptionsReloadButtonX", 0f);
		ReloadButton.Offset.y = PlayerPrefs.GetFloat("OptionsReloadButtonY", 0f);
		AimButton.Offset.x = PlayerPrefs.GetFloat("OptionsAimButtonX", 0f);
		AimButton.Offset.y = PlayerPrefs.GetFloat("OptionsAimButtonY", 0f);
		graphicDetail = PlayerPrefs.GetInt("OptionsGraphicDetail", GetDefaultGraphics());
		musicOn = PlayerPrefs.GetInt("OptionsMusicOn", DefaultMusicOn ? 1 : 0) != 0;
		if ((bool)MusicManager.Instance)
		{
			MusicManager.Instance.ApplyOptionsChange();
		}
		showMogaHelp = PlayerPrefs.GetInt("OptionsShowMogaHelp", 1) != 0;
		leftHandControlsNeedUpdate = PlayerPrefs.GetInt("OptionsLeftHandControlsNeedUpdate", 0) != 0;
	}
}
