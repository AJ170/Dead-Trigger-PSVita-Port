using UnityEngine;

public class custom_inputs : MonoBehaviour
{
	private class JAxis
	{
		public string axis;

		public string name;

		public JAxis(string _axis, string _name)
		{
			axis = _axis;
			name = _name;
		}
	}

	private enum E_JoystickAxis
	{
		NONE = -1,
		JoystickUp = 0,
		JoystickDown = 1,
		JoystickLeft = 2,
		JoystickRight = 3,
		Joystick_3a = 4,
		Joystick_3b = 5,
		Joystick_4a = 6,
		Joystick_4b = 7,
		Joystick_5a = 8,
		Joystick_5b = 9,
		Joystick_6a = 10,
		Joystick_6b = 11,
		Joystick_7a = 12,
		Joystick_7b = 13,
		Joystick_8a = 14,
		Joystick_8b = 15,
		COUNT = 16
	}

	private class JoyInput
	{
		public KeyCode key;

		public E_JoystickAxis joyAxis;

		public JoyInput(KeyCode _key, E_JoystickAxis _joyAxis)
		{
			key = _key;
			joyAxis = _joyAxis;
		}
	}

	private struct GpadConfig
	{
		public string idName;

		public JoyInput[] defaults;

		public GpadConfig(string _idName, JoyInput[] _defaults)
		{
			idName = _idName;
			defaults = _defaults;
		}
	}

	public delegate void HideDelegate();

	private const int MaxSaveSlots = 10;

	private static custom_inputs s_Instance;

	public HideDelegate m_OnHideDelegate;

	private bool menuOn;

	public Texture2D inputManagerLogo;

	public int[] DescriptionString;

	private JoyInput[] inputK;

	private float Boxes_Y = 100f;

	private float BoxesMargin_Y = 32f;

	private float BoxesHeight = 27f;

	private float BoxesMargin_X = 20f;

	private int DescriptionSize = 140;

	private int buttonSize = 140;

	private float DescriptionBox_X;

	private float InputBox1_X;

	private float DescriptionBox_X2;

	private float InputBox1_X2;

	private float resetbuttonLocY = 550f;

	private float resetbuttonSizeX = 220f;

	private float resetbuttonHeight = 40f;

	private float resetbuttonX;

	private float closebuttonX;

	private float Title_Y = 50f;

	private float screenMarginX = 100f;

	private float screenMarginY = 50f;

	private bool allowDuplicates;

	private int inputButtonIndex = -1;

	private bool[] tempjoy1;

	private bool[] tempjoy2;

	[HideInInspector]
	public bool[] isInput;

	[HideInInspector]
	public bool[] isInputDown;

	[HideInInspector]
	public bool[] isInputUp;

	public GUISkin OurSkin;

	public GUISkin OurSkin_Korean;

	private string sTitle;

	private string sAction;

	private string sButton;

	private string sDefaults;

	private string sDone;

	private string[] sDescription;

	private static string sKeyGpadName = "GamepadName";

	private static string sKeyGpadKeys = "GamepadKeys";

	private static string sKeyGpadAxis = "GamepadAxis";

	private static string sKeyGpadKeyLength = "GamepadLength";

	private static string sKeyLastSaveSlot = "ConfigLastSaveSlot";

	private static JAxis[] JoyAxis = new JAxis[16]
	{
		new JAxis("JoystickUp", "Up"),
		new JAxis("JoystickDown", "Down"),
		new JAxis("JoystickLeft", "Left"),
		new JAxis("JoystickRight", "Right"),
		new JAxis("Joystick_3a", "Axis 3 +"),
		new JAxis("Joystick_3b", "Axis 3 -"),
		new JAxis("Joystick_4a", "Axis 4 +"),
		new JAxis("Joystick_4b", "Axis 4 -"),
		new JAxis("Joystick_5a", "Axis 5 +"),
		new JAxis("Joystick_5b", "Axis 5 -"),
		new JAxis("Joystick_6a", "Axis 6 +"),
		new JAxis("Joystick_6b", "Axis 6 -"),
		new JAxis("Joystick_7a", "Axis 7 +"),
		new JAxis("Joystick_7b", "Axis 7 -"),
		new JAxis("Joystick_8a", "Axis 8 +"),
		new JAxis("Joystick_8b", "Axis 8 -")
	};

	private static JoyInput[] EmptyDefaults = new JoyInput[15]
	{
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE)
	};

	private static JoyInput[] X360Default = new JoyInput[15]
	{
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_8b),
		new JoyInput(KeyCode.Joystick1Button7, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button11, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_5a),
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_5b),
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_7b),
		new JoyInput(KeyCode.Joystick1Button0, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button1, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button3, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button4, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_6b),
		new JoyInput(KeyCode.None, E_JoystickAxis.JoystickRight),
		new JoyInput(KeyCode.None, E_JoystickAxis.JoystickUp),
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_3b),
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_4a)
	};

	private static JoyInput[] PS3Default = new JoyInput[15]
	{
		new JoyInput(KeyCode.Joystick1Button9, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button11, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button3, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button7, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button5, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button10, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button12, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button13, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button14, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button15, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button6, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.None, E_JoystickAxis.JoystickRight),
		new JoyInput(KeyCode.None, E_JoystickAxis.JoystickUp),
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_3b),
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_4a)
	};

	private static JoyInput[] LogitechDualDefault = new JoyInput[15]
	{
		new JoyInput(KeyCode.Joystick1Button7, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button5, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button9, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_5a),
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_5b),
		new JoyInput(KeyCode.Joystick1Button6, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button1, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button2, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button3, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button0, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_6b),
		new JoyInput(KeyCode.None, E_JoystickAxis.JoystickRight),
		new JoyInput(KeyCode.None, E_JoystickAxis.JoystickUp),
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_3b),
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_4a)
	};

	private static JoyInput[] LogitechF710Default = new JoyInput[15]
	{
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_8b),
		new JoyInput(KeyCode.Joystick1Button7, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button11, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_5a),
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_5b),
		new JoyInput(KeyCode.Joystick1Button6, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button0, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button1, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button2, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button3, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_6b),
		new JoyInput(KeyCode.None, E_JoystickAxis.JoystickRight),
		new JoyInput(KeyCode.None, E_JoystickAxis.JoystickUp),
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_3b),
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_4a)
	};

	private static JoyInput[] WikiPadControllerDefault = new JoyInput[15]
	{
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_8b),
		new JoyInput(KeyCode.Joystick1Button7, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button11, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_5a),
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_5b),
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_7b),
		new JoyInput(KeyCode.Joystick1Button0, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button1, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button3, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button4, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_6b),
		new JoyInput(KeyCode.None, E_JoystickAxis.JoystickRight),
		new JoyInput(KeyCode.None, E_JoystickAxis.JoystickUp),
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_3b),
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_4a)
	};

	private static JoyInput[] MogaProHIDDefault = new JoyInput[15]
	{
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_7b),
		new JoyInput(KeyCode.Joystick1Button7, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button11, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_5a),
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_5b),
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button3, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button4, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button1, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button0, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button6, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.None, E_JoystickAxis.JoystickRight),
		new JoyInput(KeyCode.None, E_JoystickAxis.JoystickUp),
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_3b),
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_4a)
	};

	private static JoyInput[] NVidiaShieldDefault = new JoyInput[15]
	{
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_8b),
		new JoyInput(KeyCode.Joystick1Button7, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button11, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_5a),
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_5b),
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_7b),
		new JoyInput(KeyCode.Joystick1Button4, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button1, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button0, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button3, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.Joystick1Button6, E_JoystickAxis.NONE),
		new JoyInput(KeyCode.None, E_JoystickAxis.JoystickRight),
		new JoyInput(KeyCode.None, E_JoystickAxis.JoystickUp),
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_3b),
		new JoyInput(KeyCode.None, E_JoystickAxis.Joystick_4a)
	};

	// PSVita default gamepad config. Axes are left as NONE so the analog sticks
	// fall through to the named InputManager axes (HorizontalMove/VerticalMove for
	// the left stick, HorizontalView/VerticalView for the right stick). Buttons use
	// the generic "joystick button N" codes the Vita reports. Fire = R (button 5) is
	// confirmed; the rest follow the standard XInput/Vita layout and can be retuned.
	private static JoyInput[] PSVitaDefault = new JoyInput[15]
	{
		new JoyInput(KeyCode.JoystickButton5, E_JoystickAxis.NONE),  // Fire        -> R
		new JoyInput(KeyCode.JoystickButton2, E_JoystickAxis.NONE),  // Reload      -> Square/X
		new JoyInput(KeyCode.JoystickButton7, E_JoystickAxis.NONE),  // Pause       -> Start
		new JoyInput(KeyCode.JoystickButton3, E_JoystickAxis.NONE),  // PrevWeapon  -> Triangle/Y
		new JoyInput(KeyCode.JoystickButton1, E_JoystickAxis.NONE),  // NextWeapon  -> Circle/B
		new JoyInput(KeyCode.JoystickButton4, E_JoystickAxis.NONE),  // Aim         -> L
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE),             // Item1
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE),             // Item2
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE),             // Item3
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE),             // Item4
		new JoyInput(KeyCode.JoystickButton0, E_JoystickAxis.NONE),  // Action      -> Cross/A
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE),             // Axis_MoveRight (fallback)
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE),             // Axis_MoveUp   (fallback)
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE),             // Axis_ViewRight (fallback)
		new JoyInput(KeyCode.None, E_JoystickAxis.NONE)              // Axis_ViewUp   (fallback)
	};

	private static string s_NvidiaShield1 = "nvidia_Corporation nvidia_joypad";

	private static string s_NvidiaShield2 = "NVIDIA Corporation NVIDIA Controller v01";

	private GpadConfig[] supportedGpads = new GpadConfig[8]
	{
		new GpadConfig("Logitech Logitech Dual Action", LogitechDualDefault),
		new GpadConfig("Sony PLAYSTATION(R)3 Controller", PS3Default),
		new GpadConfig("Microsoft X-Box 360 pad", X360Default),
		new GpadConfig("Generic X-Box pad", LogitechF710Default),
		new GpadConfig("WikiPad Controller", WikiPadControllerDefault),
		new GpadConfig("Moga Pro HID", MogaProHIDDefault),
		new GpadConfig(s_NvidiaShield1, NVidiaShieldDefault),
		new GpadConfig(s_NvidiaShield2, NVidiaShieldDefault)
	};

	public static custom_inputs Instance
	{
		get
		{
			if (s_Instance == null)
			{
				GameObject original = Resources.Load("InputManagerController") as GameObject;
				GameObject gameObject = Object.Instantiate(original) as GameObject;
				s_Instance = gameObject.GetComponent<custom_inputs>();
				s_Instance.Init();
				Object.DontDestroyOnLoad(s_Instance);
			}
			return s_Instance;
		}
	}

	private void InitTexts()
	{
		sTitle = TextDatabase.instance[2020000];
		sAction = TextDatabase.instance[2020001];
		sButton = TextDatabase.instance[2020002];
		sDefaults = TextDatabase.instance[2020003];
		sDone = TextDatabase.instance[2020004];
		sDescription = new string[DescriptionString.Length];
		for (int i = 0; i < DescriptionString.Length; i++)
		{
			sDescription[i] = TextDatabase.instance[DescriptionString[i]];
		}
	}

	public static bool IsNVidiaShield()
	{
		string text = Game.CurrentJoystickName();
		if (text != null)
		{
			return text.Contains(s_NvidiaShield1) || text.Contains(s_NvidiaShield2);
		}
		return false;
	}

	private void Start()
	{
		base.useGUILayout = false;
	}

	public bool IsMenuOn()
	{
		return menuOn;
	}

	private void Init()
	{
		inputK = new JoyInput[DescriptionString.Length];
		isInput = new bool[DescriptionString.Length];
		isInputDown = new bool[DescriptionString.Length];
		isInputUp = new bool[DescriptionString.Length];
		tempjoy1 = new bool[DescriptionString.Length];
		tempjoy2 = new bool[DescriptionString.Length];
		InitTexts();
		LoadConfig();
		for (int i = 0; i < DescriptionString.Length; i++)
		{
			isInput[i] = false;
			isInputDown[i] = false;
			isInputUp[i] = false;
			tempjoy1[i] = true;
			tempjoy2[i] = false;
		}
	}

	public void LoadConfig()
	{
		if (Application.platform == RuntimePlatform.PSP2)
		{
			SetDefaults(PSVitaDefault);
			return;
		}
		SetDefaults(EmptyDefaults);
		string text = Game.CurrentJoystickName();
		if (text == null)
		{
			return;
		}
		if (HasStoredConfig(text))
		{
			int num = StoredConfigIndex(text);
			Debug.Log("Loading config from slot: " + num + "gpad: " + text);
			if (!loadConfig(num))
			{
				Debug.Log("Load from slot " + num + " failed, deleting saved data");
				DeleteConfigKeys(num);
				SetDefaults(EmptyDefaults);
			}
		}
		else if (HasDefaultConfig(text))
		{
			Debug.Log("Setting default config: " + text);
			SetDefaultConfig(text);
		}
	}

	private void Update()
	{
		float num = (float)Screen.width - screenMarginX;
		float num2 = (float)Screen.height - screenMarginY - Boxes_Y - resetbuttonHeight;
		DescriptionSize = (int)num / 6;
		buttonSize = (int)num / 6;
		int num3 = DescriptionString.Length / 2 + 4;
		BoxesMargin_Y = num2 / (float)num3;
		BoxesHeight = BoxesMargin_Y * 0.75f;
		DescriptionBox_X = screenMarginX / 2f + num / 4f - ((float)DescriptionSize + BoxesMargin_X / 2f);
		InputBox1_X = screenMarginX / 2f + num / 4f + BoxesMargin_X / 2f;
		DescriptionBox_X2 = screenMarginX / 2f + num / 4f * 3f - ((float)DescriptionSize + BoxesMargin_X / 2f);
		InputBox1_X2 = screenMarginX / 2f + num / 4f * 3f + BoxesMargin_X / 2f;
		resetbuttonX = (float)(Screen.width / 2) - resetbuttonSizeX - BoxesMargin_X * 2f;
		closebuttonX = (float)(Screen.width / 2) + BoxesMargin_X * 2f;
		resetbuttonLocY = (float)Screen.height - screenMarginY / 2f - resetbuttonHeight - BoxesMargin_Y;
		if (!menuOn)
		{
			inputSetBools();
		}
	}

	public void ShowConfig()
	{
		InitTexts();
		menuOn = true;
		inputButtonIndex = -1;
	}

	public void CloseAndSaveConfig()
	{
		inputButtonIndex = -1;
		menuOn = false;
		int slot = FindSaveSlot(Game.CurrentJoystickName());
		saveInputs(slot);
		if (m_OnHideDelegate != null)
		{
			m_OnHideDelegate();
		}
	}

	private void OnGUI()
	{
		if (menuOn)
		{
			drawButtons1();
		}
	}

	private void inputSetBools()
	{
		for (int i = 0; i < DescriptionString.Length; i++)
		{
			KeyCode key = inputK[i].key;
			bool flag = inputK[i].joyAxis != E_JoystickAxis.NONE;
			float num = 0f;
			if (flag)
			{
				num = Input.GetAxis(JoyAxis[(int)inputK[i].joyAxis].axis);
			}
			if (Input.GetKey(key) || (flag && num > 0.65f))
			{
				isInput[i] = true;
			}
			else
			{
				isInput[i] = false;
			}
			if (Input.GetKeyDown(key))
			{
				isInputDown[i] = true;
			}
			else
			{
				isInputDown[i] = false;
			}
			if (flag && num > 0.65f)
			{
				if (!tempjoy1[i])
				{
					isInputDown[i] = false;
				}
				if (tempjoy1[i])
				{
					isInputDown[i] = true;
					tempjoy1[i] = false;
				}
			}
			if (!tempjoy1[i] && flag && num < 0.25f)
			{
				isInputDown[i] = false;
				tempjoy1[i] = true;
			}
			if (Input.GetKeyUp(key))
			{
				isInputUp[i] = true;
			}
			else
			{
				isInputUp[i] = false;
			}
			if (flag && num > 0.65f)
			{
				if (tempjoy2[i])
				{
					isInputUp[i] = false;
				}
				if (!tempjoy2[i])
				{
					isInputUp[i] = false;
					tempjoy2[i] = true;
				}
			}
			if (tempjoy2[i] && flag && num < 0.25f)
			{
				isInputUp[i] = true;
				tempjoy2[i] = false;
			}
		}
	}

	private void saveInputs(int Slot)
	{
		string text = string.Empty;
		string text2 = string.Empty;
		for (int num = DescriptionString.Length - 1; num > -1; num--)
		{
			text = (int)inputK[num].key + "*" + text;
			text2 = (int)inputK[num].joyAxis + "*" + text2;
		}
		PlayerPrefs.SetInt(sKeyLastSaveSlot, Slot);
		Save(Slot, Game.CurrentJoystickName(), text, text2, DescriptionString.Length);
	}

	private int FindSaveSlot(string gpadName)
	{
		int num = StoredConfigIndex(gpadName);
		if (num < 0)
		{
			num = PlayerPrefs.GetInt(sKeyLastSaveSlot, -1);
			num++;
			if (num > 10)
			{
				num = 0;
			}
		}
		return num;
	}

	private void Save(int Slot, string gpadName, string KeyCodes, string JoysticInput, int KeyLength)
	{
		PlayerPrefs.SetString(sKeyGpadName + Slot, gpadName);
		PlayerPrefs.SetString(sKeyGpadKeys + Slot, KeyCodes);
		PlayerPrefs.SetString(sKeyGpadAxis + Slot, JoysticInput);
		PlayerPrefs.SetInt(sKeyGpadKeyLength + Slot, KeyLength);
	}

	private bool Load(string KeyCodes_loadstring, string Joystick_loadstring)
	{
		if (KeyCodes_loadstring == string.Empty || Joystick_loadstring == string.Empty)
		{
			Debug.LogError("Load failed - empty strings");
			return false;
		}
		string[] array = KeyCodes_loadstring.Split('*');
		string[] array2 = Joystick_loadstring.Split('*');
		int num = DescriptionString.Length;
		if (array.Length < num || array2.Length < num)
		{
			Debug.LogError("Load failed - lengt of parset strings do not match: " + num + ", " + array.Length + ", " + array2.Length);
			return false;
		}
		for (int i = 0; i < num; i++)
		{
			int result;
			int.TryParse(array[i], out result);
			int result2;
			int.TryParse(array2[i], out result2);
			inputK[i] = new JoyInput((KeyCode)result, (E_JoystickAxis)result2);
		}
		return true;
	}

	private void DeleteConfigKeys(int Slot)
	{
		PlayerPrefs.DeleteKey(sKeyGpadName + Slot);
		PlayerPrefs.DeleteKey(sKeyGpadKeys + Slot);
		PlayerPrefs.DeleteKey(sKeyGpadAxis + Slot);
		PlayerPrefs.DeleteKey(sKeyGpadKeyLength + Slot);
	}

	public bool HasStoredConfig(string gpadName)
	{
		return StoredConfigIndex(gpadName) >= 0;
	}

	private int StoredConfigIndex(string gpadName)
	{
		for (int i = 0; i < 10; i++)
		{
			if (PlayerPrefs.HasKey(sKeyGpadName + i))
			{
				string @string = PlayerPrefs.GetString(sKeyGpadName + i, string.Empty);
				if (@string == gpadName)
				{
					return i;
				}
			}
		}
		return -1;
	}

	public bool HasDefaultConfig(string gpadName)
	{
		for (int i = 0; i < supportedGpads.Length; i++)
		{
			if (gpadName.Contains(supportedGpads[i].idName))
			{
				return true;
			}
		}
		return false;
	}

	private void SetDefaultConfig(string gpadName)
	{
		for (int i = 0; i < supportedGpads.Length; i++)
		{
			if (gpadName.Contains(supportedGpads[i].idName))
			{
				SetDefaults(supportedGpads[i].defaults);
				return;
			}
		}
		SetDefaults(EmptyDefaults);
	}

	private void SetDefaults(JoyInput[] defaults)
	{
		string text = string.Empty;
		string text2 = string.Empty;
		if (defaults.Length != DescriptionString.Length)
		{
			Debug.LogError(string.Concat("Defaults length (", defaults, ") do not match DescriptionString (", DescriptionString.Length, ")"));
		}
		for (int num = DescriptionString.Length - 1; num > -1; num--)
		{
			text = (int)defaults[num].key + "*" + text;
			text2 = (int)defaults[num].joyAxis + "*" + text2;
		}
		Load(text, text2);
	}

	public bool loadConfig(int Slot)
	{
		int @int = PlayerPrefs.GetInt(sKeyGpadKeyLength + Slot);
		if (@int != DescriptionString.Length)
		{
			Debug.LogError("Load failed - savedLength (" + @int + ") do not match DescriptionString.Length (" + DescriptionString.Length + ")");
			return false;
		}
		if (!PlayerPrefs.HasKey(sKeyGpadKeys + Slot) || !PlayerPrefs.HasKey(sKeyGpadAxis + Slot))
		{
			Debug.LogError("Load failed - keys not saved");
			return false;
		}
		string @string = PlayerPrefs.GetString(sKeyGpadKeys + Slot);
		string string2 = PlayerPrefs.GetString(sKeyGpadAxis + Slot);
		return Load(@string, string2);
	}

	private void drawButtons1()
	{
		float x = Input.mousePosition.x;
		float y = Input.mousePosition.y;
		Vector3 point = GUI.matrix.inverse.MultiplyPoint3x4(new Vector3(x, (float)Screen.height - y, 1f));
		GUI.skin = OurSkin;
		if (inputManagerLogo != null)
		{
			GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), inputManagerLogo);
		}
		float num = 100f;
		float num2 = 50f;
		GUI.Box(new Rect(num / 2f, num2 / 2f, (float)Screen.width - num, (float)Screen.height - num2), string.Empty, "window");
		GUI.Label(new Rect(DescriptionBox_X, Title_Y, (float)Screen.width - DescriptionBox_X * 2f, 30f), sTitle, "textfield");
		GUI.Label(new Rect(DescriptionBox_X, Boxes_Y - 5f, DescriptionSize, BoxesHeight), sAction, "textfield");
		GUI.Label(new Rect(InputBox1_X, Boxes_Y - 5f, buttonSize, BoxesHeight), sButton, "textfield");
		GUI.Label(new Rect(DescriptionBox_X2, Boxes_Y - 5f, DescriptionSize, BoxesHeight), sAction, "textfield");
		GUI.Label(new Rect(InputBox1_X2, Boxes_Y - 5f, buttonSize, BoxesHeight), sButton, "textfield");
		if (GUI.Button(new Rect(resetbuttonX, resetbuttonLocY, resetbuttonSizeX, resetbuttonHeight), sDefaults) && Input.GetMouseButtonUp(0))
		{
			ResetToDefaults();
		}
		if (GUI.Button(new Rect(closebuttonX, resetbuttonLocY, resetbuttonSizeX, resetbuttonHeight), sDone) && Input.GetMouseButtonUp(0))
		{
			CloseAndSaveConfig();
			return;
		}
		int num3 = DescriptionString.Length / 2 + 1;
		for (int i = 0; i < DescriptionString.Length; i++)
		{
			bool flag = i >= num3;
			float num4 = ((!flag) ? i : (i - num3));
			float top = Boxes_Y + (num4 + 1f) * BoxesMargin_Y;
			GUI.Label(new Rect((!flag) ? DescriptionBox_X : DescriptionBox_X2, top, DescriptionSize, BoxesHeight), sDescription[i], "box");
			Rect position = new Rect((!flag) ? InputBox1_X : InputBox1_X2, top, buttonSize, BoxesHeight);
			GUI.Button(position, GetButtonLabel(i));
			if (inputButtonIndex == i)
			{
				GUI.Toggle(position, true, string.Empty, GUI.skin.button);
			}
			if (position.Contains(point) && Input.GetMouseButtonUp(0) && inputButtonIndex == -1)
			{
				inputButtonIndex = i;
			}
		}
		if (inputButtonIndex != -1)
		{
			DetectInputSetup();
		}
	}

	private string GetButtonLabel(int btnIndex)
	{
		E_JoystickAxis joyAxis = inputK[btnIndex].joyAxis;
		if (joyAxis != E_JoystickAxis.NONE)
		{
			return JoyAxis[(int)joyAxis].name;
		}
		string text = inputK[btnIndex].key.ToString();
		if (text.StartsWith("Joystick0") || text.StartsWith("Joystick1") || text.StartsWith("Joystick2") || text.StartsWith("Joystick3") || text.StartsWith("Joystick4"))
		{
			text = text.Substring(9);
		}
		else if (text.StartsWith("Joystick"))
		{
			text = text.Substring(8);
		}
		return text;
	}

	public string GetAxisName(int btnIndex)
	{
		E_JoystickAxis joyAxis = inputK[btnIndex].joyAxis;
		if (joyAxis != E_JoystickAxis.NONE)
		{
			return JoyAxis[(int)joyAxis].axis;
		}
		return string.Empty;
	}

	private void DetectInputSetup()
	{
		JoyInput pressedInput = GetPressedInput();
		if (pressedInput != null)
		{
			inputK[inputButtonIndex] = pressedInput;
			if (pressedInput.joyAxis == E_JoystickAxis.NONE)
			{
				checDoubles(pressedInput.key, inputButtonIndex, 1);
			}
			else
			{
				checDoubleAxis(pressedInput.joyAxis, inputButtonIndex, 1);
			}
			inputButtonIndex = -1;
		}
	}

	private JoyInput GetPressedInput()
	{
		if (Event.current.type == EventType.KeyDown && Event.current.keyCode != KeyCode.Escape)
		{
			KeyCode keyCode = Event.current.keyCode;
			return new JoyInput(keyCode, E_JoystickAxis.NONE);
		}
		for (int i = 350; i < 409; i++)
		{
			if (Input.GetKey((KeyCode)i) && Event.current.keyCode != KeyCode.Escape)
			{
				KeyCode key = (KeyCode)i;
				return new JoyInput(key, E_JoystickAxis.NONE);
			}
		}
		for (int j = 0; j < JoyAxis.Length; j++)
		{
			JAxis jAxis = JoyAxis[j];
			float num = ((j >= 4) ? 0.8f : 0.5f);
			if (Input.GetAxis(jAxis.axis) > num && Event.current.keyCode != KeyCode.Escape)
			{
				return new JoyInput(KeyCode.None, (E_JoystickAxis)j);
			}
		}
		return null;
	}

	private void checDoubles(KeyCode testkey, int o, int p)
	{
		if (allowDuplicates)
		{
			return;
		}
		for (int i = 0; i < DescriptionString.Length; i++)
		{
			if (testkey == inputK[i].key && (i != o || p == 2))
			{
				inputK[i].key = KeyCode.None;
				inputK[i].joyAxis = E_JoystickAxis.NONE;
			}
		}
	}

	private void checDoubleAxis(E_JoystickAxis testAxis, int o, int p)
	{
		if (allowDuplicates)
		{
			return;
		}
		for (int i = 0; i < DescriptionString.Length; i++)
		{
			if (testAxis == inputK[i].joyAxis && (i != o || p == 2))
			{
				inputK[i].key = KeyCode.None;
				inputK[i].joyAxis = E_JoystickAxis.NONE;
			}
		}
	}

	private void ResetToDefaults()
	{
		inputButtonIndex = -1;
		PlayerPrefs.DeleteKey(sKeyLastSaveSlot);
		for (int i = 0; i < 10; i++)
		{
			DeleteConfigKeys(i);
		}
		SetDefaultConfig(Game.CurrentJoystickName());
	}
}
