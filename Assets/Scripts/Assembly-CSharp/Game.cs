using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game : MonoBehaviour
{
	[Serializable]
	private class LoadingScreens
	{
		public List<string> ForCity = new List<string>();

		public List<string> ForMission = new List<string>();
	}

	private const float GPadCheckInterval = 15f;

	private const string IOS_SYSTEM_NAME = "iPhone OS ";

	public DeviceInfo.Performance DevicePerformance = DeviceInfo.Performance.Auto;

	public PlayerPersistantInfo PlayerPersistentInfo = new PlayerPersistantInfo();

	public MissionResult MissionResultData = new MissionResult();

	public GameplayType GameplayType;

	public E_MissionType MissionType;

	public string MissionSubtype = string.Empty;

	public MissionFlowData.Difficulty Difficulty = MissionFlowData.Difficulty.Normal;

	public bool StandaloneMission = true;

	public int PlayerRankOverride;

	public int MoneyOverride;

	public int GoldOverride;

	public E_ItemID[] ItemsOverride;

	public E_WeaponID[] WeaponsOverride;

	public E_UpgradeLevel[] WeaponUpgrades;

	public E_UpgradeID[] Upgrades;

	public bool EnemyAutoSpawn = true;

	public bool PlayerInvincible;

	public bool SimulateFPSMissions;

	public bool SaveProgress = true;

	public bool DeleteProgress;

	public bool DeleteProgressConfirm;

	private static string MainLevel = "City";

	private string _CurrentLevel;

	private int _CurrentGameZone;

	private bool m_TapJoyAdvertActive;

	private bool m_IsHalloween;

	private bool m_IsChristmas;

	private bool m_UseChristmasMask;

	private int _Score;

	private E_GameState _GameState = E_GameState.Game;

	private E_GameType _GameType = E_GameType.ChapterOnly;

	private static Game _Instance;

	public int LeaderBoardID;

	private bool _CanHeal = true;

	public E_GameDifficulty GameDifficulty;

	private bool GameCenterPlayerIsLogged;

	private bool gpadConnectedCached;

	private float LastTimeGpadCheck;

	private MogaController m_MogaController;

	private bool BatteryLow;

	private float LastTimeBatteryCheck;

	private bool showMogaConnectionPopup;

	private AndroidJavaObject _CurrentConfig;

	private bool m_Focused = true;

	private ScreenOrientation m_OrientationLandscapeLeft = ScreenOrientation.LandscapeLeft;

	private ScreenOrientation m_OrientationLandscapeRight = ScreenOrientation.LandscapeRight;

	[SerializeField]
	private LoadingScreens loadingScreens = new LoadingScreens();

	public int Score
	{
		get
		{
			return _Score;
		}
		set
		{
			_Score = value;
		}
	}

	public bool CanHeal
	{
		get
		{
			return _CanHeal;
		}
		set
		{
			_CanHeal = value;
		}
	}

	public string CurrentLevel
	{
		get
		{
			return _CurrentLevel;
		}
		set
		{
			_CurrentLevel = value;
		}
	}

	public int CurrentGameZone
	{
		get
		{
			return _CurrentGameZone;
		}
		set
		{
			_CurrentGameZone = value;
		}
	}

	public E_GameState GameState
	{
		get
		{
			return _GameState;
		}
		set
		{
			_GameState = value;
		}
	}

	public E_GameType GameType
	{
		get
		{
			return _GameType;
		}
	}

	public float DontHealTime
	{
		get
		{
			return 10f;
		}
	}

	public float HealingModificator
	{
		get
		{
			return 3f;
		}
	}

	public bool TapJoyAdvertActive
	{
		get
		{
			return m_TapJoyAdvertActive;
		}
	}

	public bool IsHalloween
	{
		get
		{
			return m_IsHalloween;
		}
	}

	public bool IsChristmas
	{
		get
		{
			return m_IsChristmas;
		}
	}

	public bool UseChristmasMask
	{
		get
		{
			return m_UseChristmasMask;
		}
	}

	public bool NVidiaShiledCached { get; private set; }

	public static Game Instance
	{
		get
		{
			return _Instance;
		}
	}

	public bool IsXperiaPlay { get; private set; }

	public bool KeypadSlided { get; private set; }

	public bool IsMogaConnected { get; private set; }

	public bool IsMogaPro { get; private set; }

	public string LoadingScreen
	{
		get
		{
			List<string> list = ((!(Application.loadedLevelName == MainMenuLevelName)) ? loadingScreens.ForCity : loadingScreens.ForMission);
			return (list == null || list.Count <= 0) ? "empty" : list[UnityEngine.Random.Range(0, list.Count - 1)];
		}
	}

	public static string MainMenuLevelName
	{
		get
		{
			return MainLevel;
		}
	}

	public MogaController GetMogaGpad()
	{
		return m_MogaController;
	}

	private void Awake()
	{
		CanHeal = true;
		if ((bool)_Instance)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		_Instance = this;
		IsXperiaPlay = DetectXperiaPlayModel();
		try
		{
			InitMogaController();
		}
		catch (Exception)
		{
		}
		if (DetectKindleFireHDModel())
		{
			m_OrientationLandscapeLeft = ScreenOrientation.LandscapeRight;
			m_OrientationLandscapeRight = ScreenOrientation.LandscapeLeft;
		}
		UnityEngine.Object.DontDestroyOnLoad(this);
		base.transform.parent = null;
		DeviceInfo.Initialize(DeviceInfo.Performance.Auto);
		Screen.autorotateToPortrait = false;
		Screen.autorotateToPortraitUpsideDown = false;
		Screen.autorotateToLandscapeLeft = true;
		Screen.autorotateToLandscapeRight = true;
		if (StandaloneMission)
		{
			CurrentLevel = Application.loadedLevelName;
		}
		DateTime now = DateTime.Now;
		if (now.Month >= 10 && now.Month <= 11 && ((now.Day > 24 && now.Month == 10) || (now.Day < 18 && now.Month == 11)))
		{
			m_IsHalloween = true;
		}
		else
		{
			m_IsHalloween = false;
		}
		if (now.Month == 12 && now.Year == 2012 && now.Day > 20 && now.Day < 29)
		{
			m_TapJoyAdvertActive = true;
		}
		else
		{
			m_TapJoyAdvertActive = false;
		}
		if (now.Month == 12)
		{
			m_IsChristmas = true;
		}
		else
		{
			m_IsChristmas = false;
		}
		if (now.Month == 12 && now.Day > 19)
		{
			m_UseChristmasMask = true;
		}
		else
		{
			m_UseChristmasMask = false;
		}
		Etcetera.Init();
		//TwitterWrapper.Init("JkUv59AgxIL79gGaezqYQ", "eme6M4IcnyDkN8btgjHTqR4aHf8ltSlwDnOe4Wewk");
		Kontagent.StartSession();
		//JavaVM.AttachCurrentThread();
		//TapjoyPlugin.EnableLogging(false);
		//TapjoyPlugin.SetCallbackHandler("_Game");
		//TapjoyPlugin.RequestTapjoyConnect("4c40cc63-5475-45e3-80a0-945624ba9ead", "3wJtitNzEc73PoZ6d2P6");
		IDataFile dataFile = GameSaveLoadUtl.OpenReadGameData();
		int @int = dataFile.GetInt("VERSION", -1);
		int int2 = PlayerPrefs.GetInt("intro_video", -1);
		if (@int < 0 && int2 < 0)
		{
			PlayerPrefs.SetInt("intro_video", 1);
			PlayerPrefs.Save();
			//Handheld.PlayFullScreenMovie("Intro.mp4", Color.black, FullScreenMovieControlMode.Hidden, FullScreenMovieScalingMode.AspectFit);
		}
		StartCoroutine(initGameCenter());
	}

	private IEnumerator initGameCenter()
	{
		yield return null;
		RegisterGameCenterDelegates();
	}

	private void Start()
	{
		PlayerPersistentInfo.Initialize();
		PlayerPersistentInfo.Load(GameSaveLoadUtl.OpenReadPlayerData());
		LastTimeGpadCheck = -15f;
		if (IsXperiaPlay)
		{
			InitAndroidConfigLink();
			StartCoroutine(CheckForPhoneSlidedStatus());
		}
		if (custom_inputs.IsNVidiaShield())
		{
			NVidiaShiledCached = true;
		}
		else
		{
			NVidiaShiledCached = false;
		}
	}

	private void OnDestroy()
	{
		if (_Instance == this)
		{
			//TwitterWrapper.Done();
			Etcetera.Done();
			if (m_MogaController != null)
			{
				m_MogaController.exit();
				m_MogaController = null;
				IsMogaConnected = false;
			}
		}
	}

	private void RegisterGameCenterDelegates()
	{
	}

	public static bool IsHDResolution()
	{
		int num = Mathf.Max(Screen.width, Screen.height);
		int num2 = Mathf.Min(Screen.width, Screen.height);
		if (num > 960 || num2 > 640)
		{
			return true;
		}
		return false;
	}

	public void Save_Save()
	{
		PlayerPrefs.SetInt(string.Concat(GameType, "SaveExist"), 1);
		PlayerPrefs.SetString(string.Concat(GameType, "Level"), Application.loadedLevelName);
		PlayerPrefs.SetInt(string.Concat(GameType, "GameZone"), Mission.Instance ? Mission.Instance.GameZoneIndex : 0);
	}

	public void Save_Clear()
	{
		PlayerPrefs.DeleteKey(string.Concat(GameType, "SaveExist"));
		PlayerPrefs.DeleteKey(string.Concat(GameType, "Level"));
		PlayerPrefs.DeleteKey(string.Concat(GameType, "GameZone"));
		for (int i = 0; i < 100; i++)
		{
			if (PlayerPrefs.HasKey(string.Concat(Instance.GameType, "GB", i)))
			{
				PlayerPrefs.DeleteKey(string.Concat(Instance.GameType, "GB", i));
			}
		}
	}

	public bool UsePumpkins()
	{
		if (m_IsHalloween || (GameplayType == GameplayType.Arena && _CurrentLevel == "graveyard"))
		{
			return true;
		}
		return false;
	}

	public bool UseChristmasMasks()
	{
		if (m_UseChristmasMask || (GameplayType == GameplayType.Arena && _CurrentLevel == "northpole"))
		{
			return true;
		}
		return false;
	}

	public void Save_Load()
	{
		CurrentLevel = PlayerPrefs.GetString(string.Concat(GameType, "Level"), "level_01");
		CurrentGameZone = PlayerPrefs.GetInt(string.Concat(GameType, "GameZone"), 0);
	}

	public bool IsResumePossible(E_GameType gameType)
	{
		return PlayerPrefs.GetInt(string.Concat(gameType, "SaveExist"), 0) == 1;
	}

	public void LoadMainMenu(bool inForce = false)
	{
		if (inForce || GameState != 0)
		{
			if ((bool)TimeManager.Instance)
			{
				TimeManager.Instance.TimeScale = 1f;
			}
			Time.timeScale = 1f;
			StartCoroutine(LoadScene(MainMenuLevelName));
		}
	}

	public void StartNewGame(E_GameDifficulty difficulty)
	{
		_GameType = E_GameType.SinglePlayer;
		Save_Clear();
		Save_Load();
		GameDifficulty = difficulty;
		CurrentLevel = MainMenuLevelName;
		StartCoroutine(LoadScene(CurrentLevel));
		MissionResultData.Clear();
	}

	public void StartSaleScreens()
	{
	}

	public void LoadLevel(string level)
	{
		MissionResultData.Clear();
		CurrentLevel = level;
		StartCoroutine(LoadScene(CurrentLevel));
	}

	private IEnumerator LoadScene(string scene)
	{
		yield return new WaitForEndOfFrame();
		yield return new WaitForEndOfFrame();
		ClearInstances();
		Application.LoadLevel(LoadingScreen);
		yield return new WaitForSeconds(0.6f);
		ClearInstances();
		GC.Collect(100);
		yield return new WaitForEndOfFrame();
		yield return new WaitForEndOfFrame();
		yield return Resources.UnloadUnusedAssets();
		yield return new WaitForEndOfFrame();
		yield return new WaitForEndOfFrame();
		Application.LoadLevel(scene);
		GC.Collect(100);
		yield return new WaitForEndOfFrame();
		yield return new WaitForEndOfFrame();
		yield return Resources.UnloadUnusedAssets();
		GC.Collect(100);
	}

	public void TryToCleanSomeMemory()
	{
		GC.Collect(100);
		Resources.UnloadUnusedAssets();
	}

	private void ClearInstances()
	{
		AgentActionFactory.Clear();
		FactsFactory.Clear();
		if ((bool)Player.Instance)
		{
			Player.Instance.StopAllCoroutines();
			Player.Instance.CancelInvoke();
			Player.Instance = null;
		}
		if ((bool)GameCamera.Instance)
		{
			GameCamera.Instance.StopAllCoroutines();
			GameCamera.Instance.CancelInvoke();
			GameCamera.Instance = null;
		}
		if ((bool)CombatEffectsManager.Instance)
		{
			CombatEffectsManager.Instance.StopAllCoroutines();
			CombatEffectsManager.Instance.CancelInvoke();
			CombatEffectsManager.Instance = null;
		}
		if ((bool)ProjectileManager.Instance)
		{
			ProjectileManager.Instance.StopAllCoroutines();
			ProjectileManager.Instance.CancelInvoke();
			ProjectileManager.Instance = null;
		}
		if ((bool)Mission.Instance)
		{
			Mission.Instance.StopAllCoroutines();
			Mission.Instance.CancelInvoke();
			Mission.Instance = null;
		}
		if ((bool)MusicManager.Instance)
		{
			MusicManager.Instance.StopAllCoroutines();
			MusicManager.Instance.CancelInvoke();
			MusicManager.Instance = null;
		}
		if ((bool)BloodFXManager.Instance)
		{
			BloodFXManager.Instance.StopAllCoroutines();
			BloodFXManager.Instance.CancelInvoke();
			BloodFXManager.Instance = null;
		}
		if ((bool)CamExplosionFXMgr.Instance)
		{
			CamExplosionFXMgr.Instance.StopAllCoroutines();
			CamExplosionFXMgr.Instance.CancelInvoke();
			CamExplosionFXMgr.Instance = null;
		}
		if ((bool)GameBlackboard.Instance)
		{
			GameBlackboard.Instance.StopAllCoroutines();
			GameBlackboard.Instance.CancelInvoke();
			GameBlackboard.Instance = null;
		}
		if ((bool)WeaponManager.Instance)
		{
			WeaponManager.Instance.StopAllCoroutines();
			WeaponManager.Instance.CancelInvoke();
			WeaponManager.Instance = null;
		}
		if ((bool)GuiCustomizeControls.Instance)
		{
			GuiCustomizeControls.Instance.StopAllCoroutines();
			GuiCustomizeControls.Instance.CancelInvoke();
			GuiCustomizeControls.Instance = null;
		}
		if ((bool)GuiHUD.Instance)
		{
			GuiHUD.Instance.StopAllCoroutines();
			GuiHUD.Instance.CancelInvoke();
			GuiHUD.Instance = null;
		}
		if ((bool)GuiIngameMenu.Instance)
		{
			GuiIngameMenu.Instance.StopAllCoroutines();
			GuiIngameMenu.Instance.CancelInvoke();
			GuiIngameMenu.Instance = null;
		}
		if ((bool)GuiOptionsMenu.Instance)
		{
			GuiOptionsMenu.Instance.StopAllCoroutines();
			GuiOptionsMenu.Instance.CancelInvoke();
			GuiOptionsMenu.Instance = null;
		}
		if ((bool)GuiSubtitlesRenderer.Instance)
		{
			GuiSubtitlesRenderer.Instance.StopAllCoroutines();
			GuiSubtitlesRenderer.Instance.CancelInvoke();
			GuiSubtitlesRenderer.Instance = null;
		}
		if ((bool)MFGuiManager.Instance)
		{
			MFGuiManager.Instance.StopAllCoroutines();
			MFGuiManager.Instance.CancelInvoke();
			MFGuiManager.Instance = null;
		}
	}

	private void FixedUpdate()
	{
		if ((bool)Player.Instance && (bool)Player.Instance.Owner)
		{
			Player.Instance.Owner.BlackBoard.Invulnerable = PlayerInvincible;
		}
	}

	private void LateUpdate()
	{
		Shader.SetGlobalFloat("_GlobalTime", Time.time);
		HitDetection.Update();
	}

	private void Update()
	{
		if (m_MogaController == null)
		{
			return;
		}
		int state = m_MogaController.getState(1);
		bool flag = state == 1;
		if (flag != IsMogaConnected)
		{
			showMogaConnectionPopup = true;
			IsMogaConnected = flag;
			Debug.Log("Moga controller " + ((!flag) ? "disconected" : "connected"));
			IsMogaPro = m_MogaController.getState(4) == 1;
		}
		if (showMogaConnectionPopup && GuiMogaPopup.Instance != null && GuiMogaPopup.Instance.IsReady())
		{
			Debug.Log("Showing moga popup: " + ((!IsMogaConnected) ? "disconected" : "connected"));
			if (IsMogaConnected)
			{
				if (GuiOptions.showMogaHelp)
				{
					GuiMogaPopup.Instance.ShowHelp(IsMogaPro);
				}
				else
				{
					GuiMogaPopup.Instance.Show(2000110, 2f);
				}
			}
			else
			{
				GuiMogaPopup.Instance.HideHelp();
				GuiMogaPopup.Instance.Show(2000111, 2f);
			}
			showMogaConnectionPopup = false;
		}
		bool flag2 = m_MogaController.getState(2) == 1;
		if (flag2 != BatteryLow || (flag2 && Time.timeSinceLevelLoad > LastTimeBatteryCheck + 90f))
		{
			BatteryLow = flag2;
			LastTimeBatteryCheck = Time.timeSinceLevelLoad;
			Debug.Log("Moga gamepad battery is low");
			if (GuiMogaPopup.Instance != null)
			{
				if (flag2)
				{
					GuiMogaPopup.Instance.Show(2000112, 8f);
				}
				else
				{
					GuiMogaPopup.Instance.Hide();
				}
			}
		}
		if ((Application.loadedLevelName == MainMenuLevelName || GameState == E_GameState.IngameMenu) && IsMogaConnected && GuiMogaPopup.Instance != null)
		{
			if (GuiMogaPopup.Instance.IsHelpOn() && MenuMogaKeyPressed())
			{
				GuiMogaPopup.Instance.HideHelp();
			}
			else if (!GuiMogaPopup.Instance.IsShown() && (MenuMogaMovePressed() || MenuMogaKeyPressed()))
			{
				GuiMogaPopup.Instance.Show(2000130, 3.5f);
			}
		}
	}

	private void OnApplicationQuit()
	{
		Kontagent.StopSession();
		ClearInstances();
	}

	private void GameCenterPlayerLog()
	{
		GameCenterPlayerIsLogged = true;
	}

	private void GameCenterPlayerFailedToLog(string error)
	{
		GameCenterPlayerIsLogged = false;
	}

	private void GameCenterPlayerLogOut()
	{
		GameCenterPlayerIsLogged = false;
	}

	public bool IsGameCenterPlayerLogged()
	{
		return GameCenterPlayerIsLogged;
	}

	public static string CurrentJoystickName()
	{
		string[] joystickNames = Input.GetJoystickNames();
		if (joystickNames != null && joystickNames.Length > 0)
		{
			if (joystickNames.Length > 1)
			{
				Debug.LogWarning("More then 1 gamepad connected, getting name of first.");
			}
			return joystickNames[0];
		}
		return null;
	}

	public static bool IsGamepadConnected()
	{
		string[] joystickNames = Input.GetJoystickNames();
		return joystickNames != null && joystickNames.Length > 0;
	}

	public bool IsGamepadConnectedCached()
	{
		if (LastTimeGpadCheck + 15f < Time.timeSinceLevelLoad)
		{
			bool flag = IsGamepadConnected();
			if (flag != gpadConnectedCached)
			{
				gpadConnectedCached = flag;
			}
			LastTimeGpadCheck = Time.timeSinceLevelLoad;
		}
		return gpadConnectedCached;
	}

	public void TapjoyConnectSuccess(string message)
	{
		Debug.Log(Time.timeSinceLevelLoad + " TAP JOY connected " + message);
	}

	public void TapjoyConnectFail(string message)
	{
		Debug.Log(Time.timeSinceLevelLoad + " TAP JOY fail" + message);
	}

	public void TapPointsLoaded(string message)
	{
		Debug.Log(Time.timeSinceLevelLoad + " TAP JOY loaded" + message);
		int result = 0;
		int.TryParse(message, out result);
		if (result > 0)
		{
			StopCoroutine(CheckCurrency());
			AddGoldReward(result, true);
		}
	}

	public void TapPointsLoadedError(string message)
	{
		Debug.Log(Time.timeSinceLevelLoad + " TAP JOY loaded error" + message);
	}

	public void TapPointsSpent(string message)
	{
		Debug.Log(Time.timeSinceLevelLoad + " TAP JOY spent " + message);
	}

	public void TapPointsSpendError(string message)
	{
		Debug.Log(Time.timeSinceLevelLoad + " TAP JOY spend error" + message);
	}

	public void TapPointsAwarded(string message)
	{
		Debug.Log(Time.timeSinceLevelLoad + " TAP JOY award" + message);
	}

	public void TapPointsAwardError(string message)
	{
		Debug.Log("TAP JOY award error" + message);
	}

	public void FeaturedAppLoaded(string message)
	{
		Debug.Log(Time.timeSinceLevelLoad + " TAP JOY app loaded" + message);
	}

	public void FullScreenAdLoaded(string message)
	{
		Debug.Log(Time.timeSinceLevelLoad + " TAP JOY FullScreenAdLoaded" + message);
		//TapjoyWrapper.SetFullScreenAdLoaded();
	}

	public void FullScreenAdError(string message)
	{
		Debug.Log(Time.timeSinceLevelLoad + " TAP JOY FullScreenAdError" + message);
	}

	public void VideoAdStart(string message)
	{
		Debug.Log(Time.timeSinceLevelLoad + " TAP JOY VideoAdStart" + message);
	}

	public void VideoAdError(string message)
	{
		Debug.Log(Time.timeSinceLevelLoad + " TAP JOY VideoAdError" + message);
	}

	public void VideoAdComplete(string message)
	{
		Debug.Log(Time.timeSinceLevelLoad + " TAP JOY VideoAdComplete" + message);
		//TapjoyWrapper.GetFullScreenAd();
		StartCoroutine(CheckCurrency());
	}

	private IEnumerator CheckCurrency()
	{
		for (int i = 0; i < 4; i++)
		{
			yield return new WaitForSeconds(5f);
			//TapjoyPlugin.GetTapPoints();
		}
	}

	public void CurrencyEarned(string message)
	{
		Debug.Log(Time.timeSinceLevelLoad + " TAP JOY CurrencyEarned" + message);
	}

	public void ViewOpened(string message)
	{
	}

	public void ViewClosed(string message)
	{
	}

	public static void AddGoldReward(int reward, bool isTabjoy = false)
	{
		int i = 1011110;
		int i2 = 1011111;
		if (reward != 0)
		{
			if (isTabjoy)
			{
				//TapjoyPlugin.SpendTapPoints(TapjoyPlugin.QueryTapPoints());
			}
			/*if (Advertisement.PendingRewardType == Casino.PrizeType.Gold)
			{
				string inCaption = TextDatabase.instance[i];
				string text = TextDatabase.instance[i2];
				text = text.Replace("%d", reward.ToString());
				GuiMainMenu.Instance.ShowPopup("OkDialog", inCaption, text);
				Instance.PlayerPersistentInfo.AddGold(reward);
				Instance.PlayerPersistentInfo.Save();
			}
			else if (Advertisement.PendingRewardType == Casino.PrizeType.Chip)
			{
				Casino.AddChips(true);
			}*/
		}
	}

	private static bool DetectKindleFireHDModel()
	{
		string[] array = new string[3] { "KFTT", "KFJWI", "KFJWA" };
		string text = SystemInfo.deviceModel.ToLower();
		if (text.Contains("amazon"))
		{
			string[] array2 = array;
			foreach (string text2 in array2)
			{
				if (text.Contains(text2.ToLower()))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool DetectXperiaPlayModel()
	{
		return (SystemInfo.deviceModel.Contains("Sony Ericsson") && SystemInfo.deviceModel.Contains("R800")) || SystemInfo.deviceModel.Contains("Z1i");
	}

	private void InitAndroidConfigLink()
	{
		using (AndroidJavaClass androidJavaClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
		{
			if (androidJavaClass == null)
			{
				Debug.Log("player not found");
				return;
			}
			AndroidJavaObject @static = androidJavaClass.GetStatic<AndroidJavaObject>("currentActivity");
			if (@static == null)
			{
				Debug.Log("activity not found");
				return;
			}
			_CurrentConfig = @static.Call<AndroidJavaObject>("getResources", new object[0]).Call<AndroidJavaObject>("getConfiguration", new object[0]);
			if (_CurrentConfig == null)
			{
				Debug.LogError("config not found");
			}
		}
	}

	private IEnumerator CheckForPhoneSlidedStatus()
	{
		while (true)
		{
			if (_CurrentConfig != null)
			{
				int nav = 0;
				if (SystemInfo.deviceModel.Contains("R800") || SystemInfo.deviceModel.Contains("Z1i"))
				{
					nav = _CurrentConfig.Get<int>("navigationHidden");
				}
				if (nav == 2 || nav == 0)
				{
					if (KeypadSlided)
					{
						KeypadSlided = false;
					}
				}
				else if (!KeypadSlided)
				{
					KeypadSlided = true;
				}
			}
			else
			{
				KeypadSlided = false;
			}
			yield return new WaitForSeconds(0.5f);
		}
	}

	private void InitMogaController()
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			return;
		}
		AndroidJavaClass androidJavaClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
		if (androidJavaClass == null)
		{
			Debug.Log("unity player not found");
			return;
		}
		AndroidJavaObject @static = androidJavaClass.GetStatic<AndroidJavaObject>("currentActivity");
		if (@static == null)
		{
			Debug.Log("current activity not found");
			return;
		}
		AndroidJavaObject instance = MogaController.getInstance(@static);
		if (instance == null)
		{
			Debug.Log("moga controller not found");
			return;
		}
		m_MogaController = new MogaController(instance);
		m_MogaController.init();
		if (m_Focused)
		{
			m_MogaController.onResume();
		}
	}

	private bool MenuMogaKeyPressed()
	{
		return m_MogaController.getKeyCode(96) == 0 || m_MogaController.getKeyCode(97) == 0 || m_MogaController.getKeyCode(102) == 0 || m_MogaController.getKeyCode(103) == 0 || m_MogaController.getKeyCode(104) == 0 || m_MogaController.getKeyCode(105) == 0;
	}

	private bool MenuMogaMovePressed()
	{
		return Mathf.Abs(m_MogaController.getAxisValue(0)) > 0.4f || Mathf.Abs(m_MogaController.getAxisValue(1)) > 0.4f;
	}

	private void OnApplicationFocus(bool focus)
	{
		m_Focused = focus;
		if (m_MogaController != null)
		{
			if (m_Focused)
			{
				m_MogaController.onResume();
			}
			else
			{
				m_MogaController.onPause();
			}
		}
		if (focus)
		{
			Kontagent.StartSession();
		}
		else
		{
			Kontagent.StopSession();
		}
	}

	private void OnApplicationPause(bool pause)
	{
		if (!pause)
		{
			//TapjoyPlugin.GetTapPoints();
			Kontagent.StartSession();
		}
		else
		{
			Kontagent.StopSession();
		}
	}
}
