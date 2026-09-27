using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CityCamera))]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(CityInput))]
public class CityManager : MonoBehaviour
{
	private enum CameraType
	{
		CITY = 0,
		CASINO = 1
	}

	public enum Sounds
	{
		GUI_NumberLoop = 0,
		GUI_ShowItem = 1,
		GUI_ResultStar = 2,
		GUI_ZoomIn = 3,
		GUI_ZoomOut = 4,
		GUI_Reward = 5,
		GUI_Promoted = 6
	}

	public const int MAP_SCROLL_UNLOCK_CHAPTER = 3;

	public const int DAILY_BONUS_UNLOCK_MISSION = 3;

	public const int SHOP_UNLOCK_CHAPTER = 2;

	public const int SAFE_HAVEN_UNLOCK_CHAPTER = 3;

	public const int EQUIP_UNLOCK_CHAPTER = 2;

	public const int MISSION_TUTORIAL_UNLOCK_CHAPTER = 2;

	public const int BANK_UNLOCK_CHAPTER = 2;

	public const int ARENA_UNLOCK_MISSION = 4;

	public const int CASINO_UNLOCK_MISSION = 3;

	public const int DAILY_AVAILABLE_ARENA_GAMES = 5;

	public const int ONE_TIME_OFFER_UNLOCK_MISSION = 5;

	public const int CASINO_ADVERTISEMENT_VIDEO_REWORD = 1;

	public const int DAILY_CASINO_CHIP_BONUS = 1;

	public const int CASINO_BUY_CHIPS = 5;

	public const int CASINO_BUY_CHIPS_GOLD_COST = 5;

	public const int CITY_SAVE_VERSION_SPECIAL_REWARD = 5;

	public const int CITY_SAVE_VERSION_REMOVE_M4_ADD_SCORPION = 6;

	public const int CITY_SAVE_VERSION = 9;

	private const int MISSIONS_BETWEEN_CHOPPER_DROP = 8;

	private const int m_BuyerReward_Gold = 25;

	private const bool m_BuyerReward_AlienGun = true;

	private const int m_BuyerReward_Chips = 10;

	private const int m_Reward_Gold_180 = 150;

	private const int m_Reward_Chips_180 = 20;

	public AudioClip guiNumberLoop;

	public AudioClip guiShowItem;

	public AudioClip guiResultStar;

	public AudioClip guiZoomIn;

	public AudioClip guiZoomOut;

	public AudioClip guiReward;

	public AudioClip guiPromoted;

	public GameObject m_CityCameraObject;

	public GameObject m_CasinoCameraObject;

	private static CityManager _Instance;

	private CitySiteManager m_SiteManager;

	private MissionManager m_MissionManager;

	private CityGUIResources m_GuiResources;

	private CityGUIDialogs m_GuiDialogs;

	private CityGUIArena m_GuiArena;

	private CityInput m_Input;

	private CityCamera m_CityCamera;

	private Casino m_Casino;

	private OneTimeSaleOfferManager m_OneTimeSaleOfferManager;

	private int m_LateInitialize;

	private CitySiteIcon m_ActiveIcon;

	private CityHudUtility m_HudUtility;

	private CitySiteSlot m_ActiveHelicopterSlot;

	private GameObject m_ActiveHelicopter;

	private bool m_Promoted;

	private CityScreenTracker m_CityScreenTracker;

	private CityBaseScreen m_OtherScreens = new CityBaseScreen(CityScreen.Other);

	private AudioSource m_AudioMain;

	private AudioSource m_AudioSecond;

	private int m_DisableNextSuspend;

	private int m_DisableNextResume;

	private int m_ScheduledChopperDrop = 6;

	private bool m_SafeHavenIntroduced;

	private bool m_ShopIntroduced;

	private bool m_EquipIntroduced;

	private bool m_SafeHavenTutorialIntroduced;

	private bool m_ShopTutorialIntroduced;

	private bool m_EquipTutorialIntroduced;

	private bool m_BankIntroduced;

	private bool m_MissionIntroduced;

	private bool m_ArenaIntroduced;

	private bool m_HalloweenAdvertShown;

	private bool m_TapJoyAdvertShown;

	private bool m_ChristmasAdvertShown;

	private float m_TimeToShowInterstitial = 5f;

	private bool m_SpentRealMoney;

	private bool m_BuyerReward_Checked;

	private bool m_BuyerReward_CanReceive;

	private bool m_BuyerReward_ReceivedFirst;

	private bool m_BuyerReward_Received;

	private bool m_Reward_CanReceive_180;

	private bool m_Reward_Received_180;

	private bool m_WasUserBefore111;

	public static string PLAYER_PREFS_SAVEGAME_KEYID = "DeadTriggerSaveGame";

	private static int tmpCounter;

	public static CityManager Instance
	{
		get
		{
			return _Instance;
		}
	}

	public CityScreenTracker ScreenTracker
	{
		get
		{
			return m_CityScreenTracker;
		}
	}

	public bool hasSpentRealMoney
	{
		get
		{
			return m_SpentRealMoney;
		}
		set
		{
			m_SpentRealMoney = value;
		}
	}

	private bool guiInitialized
	{
		get
		{
			return m_LateInitialize == 3;
		}
	}

	public void PlaySound(Sounds soundType, bool loop)
	{
		switch (soundType)
		{
		case Sounds.GUI_NumberLoop:
			m_AudioMain.clip = guiNumberLoop;
			break;
		case Sounds.GUI_ShowItem:
			m_AudioMain.clip = guiShowItem;
			break;
		case Sounds.GUI_ResultStar:
			m_AudioMain.clip = guiResultStar;
			break;
		case Sounds.GUI_ZoomIn:
			m_AudioMain.clip = guiZoomIn;
			break;
		case Sounds.GUI_ZoomOut:
			m_AudioMain.clip = guiZoomOut;
			break;
		case Sounds.GUI_Promoted:
			m_AudioSecond.PlayOneShot(guiPromoted);
			return;
		case Sounds.GUI_Reward:
			m_AudioSecond.PlayOneShot(guiReward);
			return;
		default:
			m_AudioMain.clip = null;
			break;
		}
		m_AudioMain.loop = loop;
		m_AudioMain.Play();
	}

	public void StopSound(Sounds sound)
	{
		m_AudioMain.Stop();
	}

	private void Awake()
	{
		_Instance = this;
		GuiMainMenu.m_OnCityMapSuspend = OnCityMapSuspend;
		GuiMainMenu.m_OnCityMapResume = OnCityMapResume;
		SafeHaven.OnSafeHavenShow += OnCityMapSuspend;
		SafeHaven.OnSafeHavenHide += OnCityMapResume;
		AudioSource[] components = GetComponents<AudioSource>();
		m_AudioMain = components[0];
		m_AudioSecond = components[1];
	}

	private void OnDestroy()
	{
		ChartBoost.EnableInput = null;
		//Advertisement.AdjustRemainingTimeToShowAd = null;
		SafeHaven.OnSafeHavenShow -= OnCityMapSuspend;
		SafeHaven.OnSafeHavenHide -= OnCityMapResume;
		m_GuiResources.Destroy();
		m_GuiDialogs.Destroy();
		m_GuiArena.Destroy();
		_Instance = null;
		m_SiteManager = null;
		m_MissionManager = null;
		m_GuiResources = null;
		m_GuiDialogs = null;
		m_GuiArena = null;
		m_Input = null;
		m_CityCamera = null;
		m_Casino = null;
		m_LateInitialize = 0;
		m_ActiveIcon = null;
		m_HudUtility = null;
		m_CityScreenTracker = null;
	}

	private void Start()
	{
		m_Input = base.gameObject.GetComponent<CityInput>();
		if ((bool)m_Input)
		{
			m_Input.RegisterTouchEvent(TouchEvent);
		}
		else
		{
			Debug.LogError("CityManager - Can't find script 'CityInput' on GameObject  'City'");
		}
		m_CityCamera = base.gameObject.GetComponent<CityCamera>();
		if ((bool)m_CityCamera)
		{
			m_CityCamera.RegisterMoveEvent(CameraMoveEvent);
		}
		else
		{
			Debug.LogError("CityManager - Can't find script 'CityCamera' on GameObject  'City'");
		}
		m_Casino = base.gameObject.GetComponent<Casino>();
		if (!m_Casino)
		{
			Debug.LogError("CityManager - Can't find script 'Casino' on GameObject  'City'");
		}
		m_CityScreenTracker = new CityScreenTracker();
		m_MissionManager = new MissionManager();
		m_MissionManager.Init(base.gameObject);
		m_SiteManager = new CitySiteManager();
		m_SiteManager.Init(base.gameObject, m_MissionManager);
		m_HudUtility = new CityHudUtility();
		m_HudUtility.InitResolution();
		m_OneTimeSaleOfferManager = new OneTimeSaleOfferManager();
		ChartBoost.EnableInput = delegate(bool enableInput)
		{
			if (enableInput)
			{
				EnableInputAndIndicators();
			}
			else
			{
				DisableInputAndIndicators();
			}
		};
		/*Advertisement.AdjustRemainingTimeToShowAd = delegate(float delay)
		{
			m_TimeToShowInterstitial = Time.timeSinceLevelLoad + delay;
		};*/
		Plugins.Initialize(hasSpentRealMoney);
		if (!hasSpentRealMoney)
		{
			//Advertisement.CacheInterstitial("City");
			m_TimeToShowInterstitial = Time.timeSinceLevelLoad + Random.Range(5f, 10f);
		}
	}

	private void LateInit()
	{
		m_LateInitialize = 3;
		m_GuiResources = new CityGUIResources();
		m_GuiDialogs = new CityGUIDialogs();
		m_GuiArena = new CityGUIArena();
		m_Casino.Init();
		GUIBase_Button.TouchDelegate[] indicators = new GUIBase_Button.TouchDelegate[10] { Indicator1Pressed, Indicator2Pressed, Indicator3Pressed, Indicator4Pressed, Indicator5Pressed, Indicator6Pressed, Indicator7Pressed, Indicator8Pressed, Indicator9Pressed, Indicator10Pressed };
		m_GuiResources.ShowCityHud(indicators, IndicatorHideoutPressed, IndicatorShopPressed, IndicatorEquipPressed, IndicatorFreeGoldPressed, IndicatorCasinoPressed);
		m_GuiResources.ShowScreenFade(true);
		m_GuiResources.statusBar.Show(StatusBarBackPressed, StatusBarGoldPressed, StatusBarMoneyPressed, StatusBarXpPressed, StatusBarOptionsPressed);
		UpdateMissionHudIndicators(false);
		Load();
		if (Game.Instance.PlayerPersistentInfo.storyId == 0 || Game.Instance.PlayerPersistentInfo.storyId == 4)
		{
			NextStoryChapter();
		}
		//TapjoyPlugin.GetTapPoints();
	}

	private void LateUpdate()
	{
		if (m_LateInitialize == 2)
		{
			LateInit();
		}
		else if (m_LateInitialize < 2)
		{
			m_LateInitialize++;
		}
		else if (Input.GetKeyDown(KeyCode.Escape))
		{
			if (!m_CityScreenTracker.AnyScreenActive() && !m_Input.IsDisabled())
			{
				OnExitButton();
			}
			else
			{
				m_CityScreenTracker.ProcessBackButton();
			}
		}
		if (m_LateInitialize <= 2)
		{
			return;
		}
		if (Game.Instance.PlayerPersistentInfo.totalMissionsPlayed >= 3 && Game.Instance.PlayerPersistentInfo.IsBonusAvailable())
		{
			if (!m_CityScreenTracker.AnyScreenActive())
			{
				if (Game.Instance.TapJoyAdvertActive && m_TapJoyAdvertShown)
				{
					m_TapJoyAdvertShown = false;
				}
				ShowDailyReward();
			}
		}
		else if (!m_CityScreenTracker.AnyScreenActive() && !m_HalloweenAdvertShown && Game.Instance.IsHalloween)
		{
			ShowHalloweenAdvert();
		}
		else if (!m_CityScreenTracker.AnyScreenActive() && !m_TapJoyAdvertShown && Game.Instance.TapJoyAdvertActive)
		{
			ShowTapJoyAdvert();
		}
		else if (!m_CityScreenTracker.AnyScreenActive() && !m_ChristmasAdvertShown && Game.Instance.IsChristmas)
		{
			ShowChristmasAdvert();
		}
		else if (!m_CityScreenTracker.AnyScreenActive() && CanShowBuyerReward())
		{
			ShowBuyerReward();
		}
		else if (!m_CityScreenTracker.AnyScreenActive())
		{
			ShowCityInterstitial();
		}
		if (!hasSpentRealMoney && m_CityScreenTracker.AnyScreenActive() && m_TimeToShowInterstitial < Time.timeSinceLevelLoad + 1f)
		{
			m_TimeToShowInterstitial = Time.timeSinceLevelLoad + 1f;
		}
	}

	public DataFileJSON GetCityProgress()
	{
		DataFileJSON dataFileJSON = new DataFileJSON(string.Empty);
		dataFileJSON.SetInt("VERSION", 9);
		dataFileJSON.SetString("DEVICE_SIG", SysUtils.GetUniqueDeviceID());
		dataFileJSON.SetInt("SAFE_HAVEN_INTRODUCED", m_SafeHavenIntroduced ? 1 : 0);
		dataFileJSON.SetInt("SHOP_INTRODUCED", m_ShopIntroduced ? 1 : 0);
		dataFileJSON.SetInt("EQUIP_INTRODUCED", m_EquipIntroduced ? 1 : 0);
		dataFileJSON.SetInt("MISSION_INTRODUCED", m_MissionIntroduced ? 1 : 0);
		dataFileJSON.SetInt("SAFE_HAVEN_TUTORIAL_INTRODUCED", m_SafeHavenTutorialIntroduced ? 1 : 0);
		dataFileJSON.SetInt("SHOP_TUTORIAL_INTRODUCED", m_ShopTutorialIntroduced ? 1 : 0);
		dataFileJSON.SetInt("EQUIP_TUTORIAL_INTRODUCED", m_EquipTutorialIntroduced ? 1 : 0);
		dataFileJSON.SetInt("BANK_INTRODUCED", m_BankIntroduced ? 1 : 0);
		dataFileJSON.SetInt("SCHEDULED_CHOPPER_DROP", m_ScheduledChopperDrop);
		dataFileJSON.SetInt("ARENA_INTRODUCED", m_ArenaIntroduced ? 1 : 0);
		dataFileJSON.SetInt("HALLOWEEN_ADVERT", m_HalloweenAdvertShown ? 1 : 0);
		dataFileJSON.SetInt("CHRISTMAS_ADVERT", m_ChristmasAdvertShown ? 1 : 0);
		dataFileJSON.SetInt("TAPJOY_ADVERT", m_TapJoyAdvertShown ? 1 : 0);
		dataFileJSON.SetInt("WasUserBefore111", m_WasUserBefore111 ? 1 : 0);
		dataFileJSON.SetInt("BuyerReward_Checked", m_BuyerReward_Checked ? 1 : 0);
		dataFileJSON.SetInt("BuyerReward_CanReceive", m_BuyerReward_CanReceive ? 1 : 0);
		dataFileJSON.SetInt("BuyerReward_ReceivedFirst", m_BuyerReward_ReceivedFirst ? 1 : 0);
		dataFileJSON.SetInt("BuyerReward_Received", m_BuyerReward_Received ? 1 : 0);
		dataFileJSON.SetInt("Reward_CanReceive_180", m_Reward_CanReceive_180 ? 1 : 0);
		dataFileJSON.SetInt("Reward_Received_180", m_Reward_Received_180 ? 1 : 0);
		if (m_ActiveIcon != null)
		{
			dataFileJSON.SetInt("ActiveIconSlotUid", m_ActiveIcon.siteInfo.slot.m_UID);
		}
		dataFileJSON.SetInt("SpentRealMoney", m_SpentRealMoney ? 1 : 0);
		m_Casino.Save(dataFileJSON);
		m_SiteManager.Save(dataFileJSON);
		m_MissionManager.Save(dataFileJSON);
		m_OneTimeSaleOfferManager.Save(dataFileJSON);
		return dataFileJSON;
	}

	public string GetCityProgressAsJSON()
	{
		return GetCityProgress().ToString();
	}

	public void Save()
	{
		if (Game.Instance.SaveProgress)
		{
			IDataFile cityProgress = GetCityProgress();
			GameSaveLoadUtl.SaveGameData(cityProgress);
			Game.Instance.PlayerPersistentInfo.Save();
			if (CloudUser.instance.isUserAuthenticated)
			{
				GameCloudManager.SendPPIToCloud(Game.Instance.PlayerPersistentInfo.GetPlayerDataAsJsonStr());
			}
			Game.Instance.DeleteProgress = false;
		}
	}

	private void Load()
	{
		if (CloudUser.instance.CanAutoAuthenticate())
		{
			CloudUser.instance.AuthenticateLocalUser();
		}
		IDataFile dataFile = GameSaveLoadUtl.OpenReadGameData();
		int @int = dataFile.GetInt("VERSION", -1);
		m_WasUserBefore111 = 0 < @int && @int <= 5;
		m_WasUserBefore111 = dataFile.GetInt("WasUserBefore111", m_WasUserBefore111 ? 1 : 0) > 0;
		m_BuyerReward_Checked = dataFile.GetInt("BuyerReward_Checked") > 0;
		m_BuyerReward_CanReceive = dataFile.GetInt("BuyerReward_CanReceive") > 0;
		m_BuyerReward_ReceivedFirst = dataFile.GetInt("BuyerReward_ReceivedFirst") > 0;
		m_BuyerReward_Received = dataFile.GetInt("BuyerReward_Received") > 0;
		m_Reward_CanReceive_180 = dataFile.GetInt("Reward_CanReceive_180") > 0;
		m_Reward_Received_180 = dataFile.GetInt("Reward_Received_180") > 0;
		int int2 = dataFile.GetInt("ActiveIconSlotUid", -1);
		m_SafeHavenIntroduced = dataFile.GetInt("SAFE_HAVEN_INTRODUCED") > 0;
		m_ShopIntroduced = dataFile.GetInt("SHOP_INTRODUCED") > 0;
		m_EquipIntroduced = dataFile.GetInt("EQUIP_INTRODUCED") > 0;
		m_MissionIntroduced = dataFile.GetInt("MISSION_INTRODUCED") > 0;
		m_SafeHavenTutorialIntroduced = dataFile.GetInt("SAFE_HAVEN_TUTORIAL_INTRODUCED") > 0;
		m_ShopTutorialIntroduced = dataFile.GetInt("SHOP_TUTORIAL_INTRODUCED") > 0;
		m_EquipTutorialIntroduced = dataFile.GetInt("EQUIP_TUTORIAL_INTRODUCED") > 0;
		m_BankIntroduced = dataFile.GetInt("BANK_INTRODUCED") > 0;
		m_ScheduledChopperDrop = dataFile.GetInt("SCHEDULED_CHOPPER_DROP", 8);
		m_ArenaIntroduced = dataFile.GetInt("ARENA_INTRODUCED") > 0;
		m_HalloweenAdvertShown = dataFile.GetInt("HALLOWEEN_ADVERT") > 0;
		m_ChristmasAdvertShown = dataFile.GetInt("CHRISTMAS_ADVERT") > 0;
		m_TapJoyAdvertShown = dataFile.GetInt("TAPJOY_ADVERT") > 0;
		m_SpentRealMoney = dataFile.GetInt("SpentRealMoney") > 0;
		if (!m_BuyerReward_Checked)
		{
			m_BuyerReward_CanReceive = AndroidRewardSystem.CanReceiveReward();
			m_BuyerReward_Checked = true;
		}
		if (@int < 9 && !m_Reward_Received_180 && dataFile.GetInt("MISSION_INTRODUCED", -1) >= 0)
		{
			m_Reward_CanReceive_180 = true;
		}
		if (@int <= 6)
		{
			if (!Game.Instance.PlayerPersistentInfo.InventoryList.ContainsWeapon(E_WeaponID.Scorpion))
			{
				Game.Instance.PlayerPersistentInfo.InventoryAddWeapon(E_WeaponID.Scorpion, E_UpgradeLevel.Mk1);
			}
			if (Game.Instance.PlayerPersistentInfo.storyId <= 4)
			{
				bool flag = Game.Instance.PlayerPersistentInfo.EquipList.ContainsWeapon(E_WeaponID.M4);
				if (Game.Instance.PlayerPersistentInfo.InventoryList.ContainsWeapon(E_WeaponID.M4))
				{
					Game.Instance.PlayerPersistentInfo.InventoryRemoveWeapon(E_WeaponID.M4);
				}
				if (flag)
				{
					Game.Instance.PlayerPersistentInfo.EquipAddWeapon(E_WeaponID.Scorpion, E_UpgradeLevel.Mk1);
				}
			}
		}
		m_Casino.Load(dataFile);
		m_SiteManager.SpawnSpecialSites();
		m_SiteManager.Load(dataFile, Game.Instance.PlayerPersistentInfo.storyId, Game.Instance.PlayerPersistentInfo.experience, @int);
		m_MissionManager.Load(dataFile);
		m_OneTimeSaleOfferManager.Load(dataFile);
		UpdateObjective();
		UpdateMissionHudIndicators(false);
		m_GuiResources.statusBar.InitData();
		if (Game.Instance.PlayerPersistentInfo.storyId > 2 && !m_ShopIntroduced)
		{
			m_ShopIntroduced = true;
		}
		if (int2 > -1)
		{
			foreach (CitySiteIcon spawnedIcon in m_SiteManager.SpawnedIcons)
			{
				if (spawnedIcon.siteInfo.slot.m_UID == int2)
				{
					m_ActiveIcon = spawnedIcon;
				}
			}
			if (m_SiteManager.iconArena != null && m_SiteManager.iconArena.siteInfo.slot.m_UID == int2)
			{
				m_ActiveIcon = m_SiteManager.iconArena;
			}
		}
		if (Game.Instance.MissionResultData.Result != MissionResult.Type.NONE)
		{
			ShowMissionResult();
			if ((bool)m_ActiveIcon)
			{
				m_CityCamera.MoveCameraCloserTo(m_ActiveIcon.transform.position);
			}
			DisableInputAndIndicators();
		}
		else
		{
			m_ActiveIcon = null;
			SpawnNewMissions();
			ShowDialogForFirstMission();
			FocusOnMostImportantMission();
		}
		m_GuiResources.HideScreenFade();
		TutorialProgress(true);
	}

	private void ShowDialogForFirstMission()
	{
		if (Game.Instance.PlayerPersistentInfo.storyId != 1)
		{
			return;
		}
		foreach (CitySiteIcon spawnedIcon in m_SiteManager.SpawnedIcons)
		{
			CityMissionInfo missionInfo = spawnedIcon.missionInfo;
			if (missionInfo != null)
			{
				m_ActiveIcon = spawnedIcon;
				m_GuiDialogs.ShowMissionStart(MissionStartAccept, null, MissionStartEquip, MissionStartBuy, MissionStartEquippedOwned, missionInfo);
				DisableInputAndIndicators();
				break;
			}
		}
	}

	private void SpawnNewMissions()
	{
		m_SiteManager.SpawnSpecialSites();
		if (Game.Instance.PlayerPersistentInfo.storyId != 2 || (m_MissionIntroduced && m_EquipIntroduced))
		{
			m_SiteManager.SpawnAvailableMissions(Game.Instance.PlayerPersistentInfo.storyId, Game.Instance.PlayerPersistentInfo.experience);
		}
		if (Game.Instance.PlayerPersistentInfo.totalMissionsPlayed >= m_ScheduledChopperDrop)
		{
			SpawnChopperMission();
		}
		UpdateMissionHudIndicators(true);
		FocusOnMostImportantMission();
	}

	private void SpawnChopperMission()
	{
		m_ActiveHelicopterSlot = m_SiteManager.GetSlotForHelicopter();
		if (m_ActiveHelicopterSlot != null)
		{
			string text = "City/Helicopter";
			GameObject gameObject = Resources.Load(text) as GameObject;
			if (gameObject == null)
			{
				Debug.LogWarning("Cant find prefab: " + text);
				return;
			}
			m_ActiveHelicopter = Object.Instantiate(gameObject, m_ActiveHelicopterSlot.transform.position, m_ActiveHelicopterSlot.transform.rotation) as GameObject;
			string text2 = TextDatabase.instance[1010400];
			m_GuiResources.ShowNotification(text2);
			StartCoroutine("SpawnHelicopterCoroutine");
		}
	}

	private void FocusOnMostImportantMission()
	{
		CitySiteIcon citySiteIcon = null;
		foreach (CitySiteIcon spawnedIcon in m_SiteManager.SpawnedIcons)
		{
			if (spawnedIcon.missionInfo != null)
			{
				if (citySiteIcon == null)
				{
					citySiteIcon = spawnedIcon;
				}
				else if (spawnedIcon.missionInfo.story == MissionFlowData.StoryBound.DailyReward)
				{
					citySiteIcon = spawnedIcon;
				}
				else if (spawnedIcon.missionInfo.story == MissionFlowData.StoryBound.Story && citySiteIcon.missionInfo.story != MissionFlowData.StoryBound.DailyReward)
				{
					citySiteIcon = spawnedIcon;
				}
				else if (citySiteIcon.missionInfo.story == MissionFlowData.StoryBound.None && spawnedIcon.missionInfo.story == MissionFlowData.StoryBound.Dependent)
				{
					citySiteIcon = spawnedIcon;
				}
			}
		}
		if ((bool)citySiteIcon)
		{
			m_CityCamera.CenterCameraOn(citySiteIcon.transform.position);
		}
	}

	private void UpdateObjective()
	{
		StoryFlowData.Story storyChapterInfo = m_MissionManager.GetStoryChapterInfo(Game.Instance.PlayerPersistentInfo.storyId);
		if (storyChapterInfo == null)
		{
			return;
		}
		if (Game.Instance.PlayerPersistentInfo.chapterProgress >= storyChapterInfo.missionsRequired)
		{
			string text = TextDatabase.instance[1010065];
			m_GuiResources.ShowObjective(text);
			return;
		}
		string text2 = string.Empty;
		if (storyChapterInfo.objectiveText != 0)
		{
			text2 = TextDatabase.instance[1010060];
			text2 = text2 + " " + TextDatabase.instance[storyChapterInfo.objectiveText];
			text2 = text2.Replace("%f1", Game.Instance.PlayerPersistentInfo.chapterProgress.ToString());
			text2 = text2.Replace("%f2", storyChapterInfo.missionsRequired.ToString());
		}
		m_GuiResources.ShowObjective(text2);
	}

	private void ProgressDialogsClosed()
	{
		MusicManager.Instance.PlayMusic("default");
		Save();
		if (Game.Instance.PlayerPersistentInfo.storyId == 2 && !m_ShopIntroduced)
		{
			ShowAdditionalInfo();
			return;
		}
		if (Game.Instance.PlayerPersistentInfo.storyId == 3 && !m_SafeHavenIntroduced)
		{
			ShowAdditionalInfo();
			return;
		}
		if (Game.Instance.PlayerPersistentInfo.storyId == 2 && !m_EquipIntroduced)
		{
			ShowAdditionalInfo();
			return;
		}
		if (Game.Instance.PlayerPersistentInfo.storyId == 2 && !m_MissionIntroduced && m_EquipIntroduced)
		{
			ShowAdditionalInfo();
			return;
		}
		if (Game.Instance.PlayerPersistentInfo.storyId == 2 && !m_BankIntroduced)
		{
			ShowAdditionalInfo();
			return;
		}
		if (Game.Instance.PlayerPersistentInfo.totalMissionsPlayed >= 4 && !m_ArenaIntroduced)
		{
			ShowAdditionalInfo();
			return;
		}
		if (m_OneTimeSaleOfferManager.OneTimeSaleOfferAvailable())
		{
			ShowAdditionalInfo();
			return;
		}
		if (Game.Instance.PlayerPersistentInfo.storyId == 1)
		{
			ShowDialogForFirstMission();
		}
		EnableInputAndIndicators();
		Game.Instance.MissionResultData.Result = MissionResult.Type.NONE;
	}

	private void MissionFinished()
	{
		CityMissionInfo missionInfo = m_ActiveIcon.missionInfo;
		CityArenaInfo arenaInfo = m_ActiveIcon.arenaInfo;
		StoryFlowData.Story storyChapterInfo = m_MissionManager.GetStoryChapterInfo(Game.Instance.PlayerPersistentInfo.storyId);
		bool flag = missionInfo == null || missionInfo.missionResult.Result == MissionResult.Type.SUCCESS;
		if (missionInfo != null)
		{
			Game.Instance.PlayerPersistentInfo.MissionFinished(flag, missionInfo.missionResult.MissionTime);
		}
		else if (arenaInfo != null)
		{
			Game.Instance.PlayerPersistentInfo.ArenaFinished(arenaInfo.arenas[arenaInfo.arenaActiveIndex].arenaId, arenaInfo.missionResult.MissionTime, arenaInfo.arenaScore, arenaInfo.totalMoney, arenaInfo.totalExperience);
			arenaInfo.arenas[arenaInfo.arenaActiveIndex].availableGames--;
		}
		if (flag)
		{
			if (missionInfo != null)
			{
				if (missionInfo.story == MissionFlowData.StoryBound.DailyReward)
				{
					Game.Instance.PlayerPersistentInfo.BonusMissionPlayed();
				}
				if (missionInfo.story == MissionFlowData.StoryBound.Story || (Game.Instance.PlayerPersistentInfo.chapterProgress >= storyChapterInfo.missionsRequired && storyChapterInfo.rankToProgress > 0))
				{
					Game.Instance.PlayerPersistentInfo.ChapterMissionFinished();
				}
				else
				{
					Game.Instance.PlayerPersistentInfo.GeneratedMissionFinished();
				}
				UpdateObjective();
				m_SiteManager.DestroySpawnedSite(m_ActiveIcon);
			}
			m_ActiveIcon = null;
			if (storyChapterInfo.missionsRequired > 0 && arenaInfo == null)
			{
				if (Game.Instance.PlayerPersistentInfo.chapterProgress == storyChapterInfo.missionsRequired)
				{
					if (storyChapterInfo.debriefPages.Count > 0)
					{
						ShowChapterDebriefing();
					}
					else
					{
						AfterChapterObjectiveCompleted();
					}
					return;
				}
				if (Game.Instance.PlayerPersistentInfo.chapterProgress >= storyChapterInfo.missionsRequired && Game.Instance.PlayerPersistentInfo.rank >= storyChapterInfo.rankToProgress)
				{
					NextStoryChapter();
					return;
				}
			}
			SpawnNewMissions();
			Etcetera.AskForReview("DEAD TRIGGER", "If you like DEAD TRIGGER, support us with review, please", 5, 2);
		}
		else
		{
			m_ActiveIcon = null;
		}
		ProgressDialogsClosed();
	}

	private void NextStoryChapter()
	{
		DisableInputAndIndicators();
		List<CitySiteIcon> list = new List<CitySiteIcon>();
		foreach (CitySiteIcon spawnedIcon in m_SiteManager.SpawnedIcons)
		{
			CityMissionInfo missionInfo = spawnedIcon.missionInfo;
			if (missionInfo != null && missionInfo.storyID <= Game.Instance.PlayerPersistentInfo.storyId && missionInfo.story != MissionFlowData.StoryBound.ChopperMission && missionInfo.story != MissionFlowData.StoryBound.DailyReward)
			{
				list.Add(spawnedIcon);
			}
		}
		foreach (CitySiteIcon item in list)
		{
			m_SiteManager.DestroySpawnedSite(item);
		}
		StoryFlowData.Story storyChapterInfo = m_MissionManager.GetStoryChapterInfo(Game.Instance.PlayerPersistentInfo.NextStoryChapter());
		if (Game.Instance.PlayerPersistentInfo.storyId == 4)
		{
			storyChapterInfo = m_MissionManager.GetStoryChapterInfo(Game.Instance.PlayerPersistentInfo.NextStoryChapter());
		}
		if (storyChapterInfo != null)
		{
			if (storyChapterInfo.storyCaption == 0)
			{
				StoryChapterClose();
			}
			else
			{
				m_GuiDialogs.ShowStoryChapter(StoryChapterClose, storyChapterInfo, false);
			}
		}
	}

	private void StoryChapterClose()
	{
		m_GuiDialogs.HideStoryChapter();
		UpdateObjective();
		SpawnNewMissions();
		ProgressDialogsClosed();
	}

	private void ShowChapterDebriefing()
	{
		StoryFlowData.Story storyChapterInfo = m_MissionManager.GetStoryChapterInfo(Game.Instance.PlayerPersistentInfo.storyId);
		m_GuiDialogs.ShowStoryChapter(ChapterDebriefingClose, storyChapterInfo, true);
	}

	private void ChapterDebriefingClose()
	{
		m_GuiDialogs.HideStoryChapter();
		AfterChapterObjectiveCompleted();
	}

	private void AfterChapterObjectiveCompleted()
	{
		StoryFlowData.Story storyChapterInfo = m_MissionManager.GetStoryChapterInfo(Game.Instance.PlayerPersistentInfo.storyId);
		if (Game.Instance.PlayerPersistentInfo.chapterProgress >= storyChapterInfo.missionsRequired && Game.Instance.PlayerPersistentInfo.rank >= storyChapterInfo.rankToProgress)
		{
			NextStoryChapter();
			return;
		}
		UpdateObjective();
		SpawnNewMissions();
		ProgressDialogsClosed();
	}

	private void MissionStartAccept()
	{
		CityMissionInfo cityMissionInfo = m_ActiveIcon.siteInfo as CityMissionInfo;
		StopHelicopterCoroutine();
		if (Game.Instance.SimulateFPSMissions)
		{
			m_GuiDialogs.HideMissionStart();
			Game.Instance.Difficulty = cityMissionInfo.difficulty;
			Game.Instance.GameplayType = GameplayType.Missions;
			ShowMissionResult();
			return;
		}
		m_GuiDialogs.MissionStartDisableControls(true);
		m_GuiResources.statusBar.EnableControls(false);
		if (Game.Instance.PlayerPersistentInfo.storyId == 1 && Game.Instance.PlayerPersistentInfo.InventoryList.ContainsItem(E_ItemID.Bandage) == 0)
		{
			Game.Instance.PlayerPersistentInfo.InventoryAddItem(E_ItemID.Bandage, 1);
		}
		Save();
		m_GuiResources.ShowScreenFade(false);
		StartCoroutine(LoadMission(cityMissionInfo));
	}

	private void ArenaStartAccept(int arenaIndex)
	{
		m_ActiveIcon = m_SiteManager.iconArena;
		m_SiteManager.iconArena.arenaInfo.arenaActiveIndex = arenaIndex;
		StopHelicopterCoroutine();
		if (Game.Instance.SimulateFPSMissions)
		{
			m_GuiArena.HideArenaStart();
			Game.Instance.GameplayType = GameplayType.Arena;
			ShowArenaResult();
		}
		else
		{
			m_GuiArena.ArenaStartDisableControls();
			m_GuiResources.statusBar.EnableControls(false);
			Save();
			m_GuiResources.ShowScreenFade(false);
			StartCoroutine(LoadArena(m_SiteManager.iconArena.arenaInfo.arenas[arenaIndex].level));
		}
	}

	private void ArenaStartClose()
	{
		m_GuiArena.HideArenaStart();
		m_ActiveIcon = null;
		EnableInputAndIndicators();
	}

	private void MissionStartClose()
	{
		m_GuiDialogs.HideMissionStart();
		m_ActiveIcon = null;
		EnableInputAndIndicators();
	}

	private void MissionStartEquip()
	{
		m_GuiDialogs.HideMissionStart();
		DisableInputAndIndicators();
		GuiMainMenu.Instance.ShowScreen("Equip");
	}

	private void MissionEquipCancel()
	{
		m_ActiveIcon = null;
		EnableInputAndIndicators();
	}

	private void MissionStartBuy(bool inside)
	{
		if (inside)
		{
			m_GuiDialogs.HideMissionStart();
			DisableInputAndIndicators();
			GuiMainMenu.Instance.ShowScreen("Shop");
			E_WeaponID recommendedWeapon = (m_ActiveIcon.siteInfo as CityMissionInfo).recommendedWeapon;
			ShopItemId selId = ((recommendedWeapon == E_WeaponID.None) ? ShopItemId.EmptyId : new ShopItemId((int)recommendedWeapon, GuiShop.E_ItemType.Weapon));
			GuiShopMenu.Instance.SwitchToCategory(E_ShopCategory.Weapons, selId);
		}
	}

	private void MissionStartEquippedOwned()
	{
		m_GuiDialogs.HideMissionStart();
		m_ActiveIcon = null;
		EnableInputAndIndicators();
	}

	public void DisableNextSuspendAndResume()
	{
		m_DisableNextSuspend++;
	}

	private void TutorialProgress(bool gameLoad)
	{
		if (!gameLoad || !m_CityScreenTracker.AnyScreenActive())
		{
			EnableInput();
		}
		if ((Game.Instance.PlayerPersistentInfo.storyId == 2 && !m_EquipTutorialIntroduced && m_ShopIntroduced) || (Game.Instance.PlayerPersistentInfo.storyId == 2 && !m_MissionIntroduced && m_EquipIntroduced) || (Game.Instance.PlayerPersistentInfo.storyId == 2 && !m_ShopTutorialIntroduced) || (Game.Instance.PlayerPersistentInfo.storyId == 3 && !m_SafeHavenTutorialIntroduced))
		{
			Save();
			ShowAdditionalInfo();
		}
		if (!gameLoad && Game.Instance.PlayerPersistentInfo.storyId == 2 && m_MissionIntroduced && m_EquipIntroduced)
		{
			SpawnNewMissions();
			Save();
		}
		UpdateMissionHudIndicators(false);
	}

	private void OnCityMapSuspend()
	{
		if (m_DisableNextSuspend > 0)
		{
			m_DisableNextSuspend--;
			m_DisableNextResume++;
		}
		else
		{
			m_OtherScreens.Show();
			DisableInputAndIndicators();
			m_GuiDialogs.DisableScreens();
		}
	}

	private void OnCityMapResume()
	{
		if (m_DisableNextResume > 0)
		{
			m_DisableNextResume--;
			return;
		}
		m_OtherScreens.Hide();
		m_GuiDialogs.EnableScreens();
		if (m_ActiveIcon != null)
		{
			m_GuiDialogs.ShowMissionStart(MissionStartAccept, MissionStartClose, MissionStartEquip, MissionStartBuy, MissionStartEquippedOwned, m_ActiveIcon.siteInfo as CityMissionInfo);
		}
		else
		{
			TutorialProgress(false);
		}
	}

	private bool ShowAdditionalInfo()
	{
		DisableInputAndIndicators();
		if (Game.Instance.PlayerPersistentInfo.storyId == 2 && !m_ShopTutorialIntroduced)
		{
			m_ShopTutorialIntroduced = true;
			m_GuiDialogs.ShowSiteInfo(AdditionalInfoClose, 9800000);
			return true;
		}
		if (Game.Instance.PlayerPersistentInfo.storyId == 3 && !m_SafeHavenTutorialIntroduced)
		{
			m_SafeHavenTutorialIntroduced = true;
			m_GuiDialogs.ShowSiteInfo(AdditionalInfoClose, 9800003);
			return true;
		}
		if (Game.Instance.PlayerPersistentInfo.storyId == 2 && !m_EquipTutorialIntroduced && m_ShopIntroduced)
		{
			m_EquipTutorialIntroduced = true;
			m_GuiDialogs.ShowSiteInfo(AdditionalInfoClose, 9800001);
			return true;
		}
		if (Game.Instance.PlayerPersistentInfo.storyId == 2 && !m_MissionIntroduced && m_EquipIntroduced)
		{
			m_MissionIntroduced = true;
			m_GuiDialogs.ShowSiteInfo(AdditionalInfoClose, 9800002);
			return true;
		}
		if (Game.Instance.PlayerPersistentInfo.storyId == 2 && !m_BankIntroduced)
		{
			m_BankIntroduced = true;
			AdditionalInfoClose();
			return true;
		}
		if (Game.Instance.PlayerPersistentInfo.totalMissionsPlayed >= 4 && !m_ArenaIntroduced)
		{
			m_ArenaIntroduced = true;
			m_GuiDialogs.ShowSiteInfo(AdditionalInfoClose, 9800005);
			m_CityCamera.CenterCameraOn(m_SiteManager.iconArena.siteInfo.slot.transform.position);
			return true;
		}
		if (m_OneTimeSaleOfferManager.OneTimeSaleOfferAvailable())
		{
			m_GuiDialogs.ShowOneTimeSaleOffer(OneTimeSaleOfferClose, m_OneTimeSaleOfferManager);
		}
		return false;
	}

	private void AdditionalInfoClose()
	{
		m_GuiDialogs.HideSiteInfo();
		if (!ShowAdditionalInfo())
		{
			if (Game.Instance.PlayerPersistentInfo.storyId == 2 && m_ShopTutorialIntroduced && !m_EquipTutorialIntroduced)
			{
				Game.Instance.PlayerPersistentInfo.AddMoney(400);
			}
			Save();
			EnableInputAndIndicators();
		}
	}

	private void OneTimeSaleOfferClose()
	{
		m_GuiDialogs.HideOneTimeSaleOffer();
		Save();
		EnableInputAndIndicators();
	}

	private void ShowMissionResult()
	{
		if (m_ActiveIcon.arenaInfo != null)
		{
			ShowArenaResult();
			return;
		}
		CityMissionInfo cityMissionInfo = m_ActiveIcon.siteInfo as CityMissionInfo;
		cityMissionInfo.missionResult = Game.Instance.MissionResultData;
		if (Game.Instance.SimulateFPSMissions)
		{
			cityMissionInfo.missionResult.Result = ((tmpCounter++ % 8 >= 6) ? MissionResult.Type.FAIL : MissionResult.Type.SUCCESS);
			cityMissionInfo.missionResult.MissionTime = Random.Range(130, 260);
			float num = Random.Range(0.25f, 0.85f);
			for (int i = 0; i < 10; i++)
			{
				cityMissionInfo.missionResult.RegisterShot(Random.value <= num);
			}
			cityMissionInfo.missionResult.KilledZombies = Random.Range(30, 50);
			cityMissionInfo.missionResult.CollectedMoney = Mathf.RoundToInt((float)cityMissionInfo.missionResult.KilledZombies / Random.Range(6f, 8f) * (float)GameplayData.Instance.MoneyPerZombie());
			cityMissionInfo.missionResult.HeadShots = Random.Range(0, 20);
			cityMissionInfo.missionResult.HealthLost = Random.Range(0, 200);
			cityMissionInfo.missionResult.RemovedLimbs = Random.Range(0, 20);
			cityMissionInfo.missionResult.Disintegrations = Random.Range(0, 15);
			cityMissionInfo.missionResult.WastedMoneyBags = Random.Range(0, 5);
			cityMissionInfo.missionResult.WeaponUsed(E_WeaponID.Bren, 1f);
			cityMissionInfo.missionResult.ItemUsed(E_ItemID.Bandage);
			if (cityMissionInfo.missionResult.Result == MissionResult.Type.FAIL)
			{
				Game.Instance.PlayerPersistentInfo.AddDeath();
			}
			else if (Random.Range(0, 3) > 0)
			{
				Game.Instance.PlayerPersistentInfo.AddDeath();
			}
		}
		AudioListener.pause = false;
		if (cityMissionInfo.missionResult.Result == MissionResult.Type.SUCCESS)
		{
			MusicManager.Instance.PlayMusic("win_menu");
		}
		else
		{
			MusicManager.Instance.PlayMusic("lost_menu");
		}
		m_GuiDialogs.ShowMissionResult(MissionResultClose, cityMissionInfo);
	}

	private void ArenaMission_ResultClose(int totalMoney, int totalExperience, int bonusXp, SpecialReward.Type bonus)
	{
		int rank = Game.Instance.PlayerPersistentInfo.rank;
		Game.Instance.PlayerPersistentInfo.AddMoney(totalMoney);
		if (Game.Instance.SimulateFPSMissions)
		{
			Game.Instance.PlayerPersistentInfo.AddExperience(totalExperience);
		}
		else
		{
			Game.Instance.PlayerPersistentInfo.AddExperience(bonusXp);
		}
		m_Promoted = rank < Game.Instance.PlayerPersistentInfo.rank;
		if (bonus != 0 && Game.Instance.MissionResultData.Result == MissionResult.Type.SUCCESS)
		{
			CityGUIDialogs.SpecialRewardInfo reward = SpecialReward.GenerateSpecialReward(bonus);
			m_GuiDialogs.ShowSpecialReward(SpecialRewardClose, reward);
		}
		else if (m_Promoted)
		{
			m_GuiDialogs.ShowPromotion(PromotionClose, Game.Instance.PlayerPersistentInfo.storyId >= 2 && ShopDataBridge.Instance.NewItemsUnlocked(Game.Instance.PlayerPersistentInfo.rank));
		}
		else
		{
			MissionFinished();
		}
	}

	private void MissionResultClose()
	{
		CityMissionInfo cityMissionInfo = m_ActiveIcon.siteInfo as CityMissionInfo;
		m_GuiDialogs.HideMissionResult();
		ArenaMission_ResultClose(cityMissionInfo.totalMoney, cityMissionInfo.totalExperience, cityMissionInfo.bonusXp, cityMissionInfo.bonus);
	}

	private void ShowArenaResult()
	{
		CityArenaInfo cityArenaInfo = m_ActiveIcon.siteInfo as CityArenaInfo;
		cityArenaInfo.missionResult = Game.Instance.MissionResultData;
		if (Game.Instance.SimulateFPSMissions)
		{
			cityArenaInfo.missionResult.Result = ((tmpCounter++ % 3 >= 2) ? MissionResult.Type.FAIL : MissionResult.Type.SUCCESS);
			cityArenaInfo.missionResult.MissionTime = Random.Range(40, 300);
			float num = Random.Range(0.25f, 0.85f);
			for (int i = 0; i < 10; i++)
			{
				cityArenaInfo.missionResult.RegisterShot(Random.value <= num);
			}
			cityArenaInfo.missionResult.KilledZombies = Random.Range(30, 50);
			cityArenaInfo.missionResult.CollectedMoney = Mathf.RoundToInt((float)cityArenaInfo.missionResult.KilledZombies / Random.Range(6f, 8f) * (float)GameplayData.Instance.MoneyPerZombie());
			cityArenaInfo.missionResult.HeadShots = Random.Range(0, 20);
			cityArenaInfo.missionResult.HealthLost = Random.Range(0, 200);
			cityArenaInfo.missionResult.RemovedLimbs = Random.Range(0, 20);
			cityArenaInfo.missionResult.Disintegrations = Random.Range(0, 15);
			cityArenaInfo.missionResult.WastedMoneyBags = Random.Range(0, 5);
			cityArenaInfo.missionResult.WeaponUsed(E_WeaponID.Bren, 1f);
			cityArenaInfo.missionResult.ItemUsed(E_ItemID.Bandage);
		}
		if (cityArenaInfo.missionResult.Result == MissionResult.Type.SUCCESS)
		{
			MusicManager.Instance.PlayMusic("win_menu");
		}
		else
		{
			MusicManager.Instance.PlayMusic("lost_menu");
		}
		m_GuiArena.ShowArenaResult(ArenaResultClose, cityArenaInfo);
	}

	private void ArenaResultClose()
	{
		CityArenaInfo cityArenaInfo = m_ActiveIcon.siteInfo as CityArenaInfo;
		m_GuiArena.HideArenaResult();
		ArenaMission_ResultClose(cityArenaInfo.totalMoney, cityArenaInfo.totalExperience, cityArenaInfo.bonusXp, SpecialReward.Type.None);
	}

	private bool CanShowBuyerReward()
	{
		if (m_Reward_CanReceive_180 && !m_Reward_Received_180)
		{
			return true;
		}
		if (m_BuyerReward_CanReceive && !m_BuyerReward_Received && Game.Instance.PlayerPersistentInfo.totalMissionsPlayed >= 3)
		{
			return true;
		}
		return false;
	}

	private void ShowBuyerReward()
	{
		DisableInputAndIndicators();
		if (m_Reward_CanReceive_180 && !m_Reward_Received_180)
		{
			m_GuiDialogs.ShowBuyerReward(BuyerRewardClose, 150, false, 20, true);
		}
		else if (m_BuyerReward_ReceivedFirst)
		{
			m_GuiDialogs.ShowBuyerReward(BuyerRewardClose, 0, true, 0, false);
		}
		else
		{
			m_GuiDialogs.ShowBuyerReward(BuyerRewardClose, 25, true, 10, false);
		}
	}

	private void BuyerRewardClose()
	{
		m_GuiDialogs.HideBuyerReward();
		if (m_Reward_CanReceive_180 && !m_Reward_Received_180)
		{
			Game.Instance.PlayerPersistentInfo.AddGold(150);
			Game.Instance.PlayerPersistentInfo.AddTicket(20);
			m_Reward_Received_180 = true;
		}
		else
		{
			m_BuyerReward_Received = true;
			if (!m_BuyerReward_ReceivedFirst)
			{
				Game.Instance.PlayerPersistentInfo.AddGold(25);
				Game.Instance.PlayerPersistentInfo.AddTicket(10);
			}
			PPIInventoryList inventoryList = Game.Instance.PlayerPersistentInfo.InventoryList;
			if (!inventoryList.ContainsWeapon(E_WeaponID.AlienGun))
			{
				inventoryList.Weapons.Add(new PPIWeaponData
				{
					ID = E_WeaponID.AlienGun
				});
			}
		}
		Save();
		if (CanShowDailyReward())
		{
			ShowDailyReward();
			return;
		}
		EnableInputAndIndicators();
		FocusOnMostImportantMission();
	}

	private bool CanShowDailyReward()
	{
		if (Game.Instance.PlayerPersistentInfo.totalMissionsPlayed >= 3 && Game.Instance.PlayerPersistentInfo.IsBonusAvailable())
		{
			return true;
		}
		return false;
	}

	private void ShowDailyReward()
	{
		DisableInputAndIndicators();
		m_GuiDialogs.ShowDailyReward(DailyRewardClose, Game.Instance.PlayerPersistentInfo.totalMissionsPlayed >= 4);
	}

	private void DailyRewardClose()
	{
		m_GuiDialogs.HideDailyReward();
		if ((bool)m_SiteManager.iconArena)
		{
			m_SiteManager.iconArena.arenaInfo.DailyReward();
		}
		m_Casino.ActivateDailyReward();
		Game.Instance.PlayerPersistentInfo.SetBonusReceived();
		Game.Instance.PlayerPersistentInfo.AddTicket(PlayerPersistantInfo.TicketsPerDailyReward);
		m_SiteManager.SpawnDailyRewardMission(Game.Instance.PlayerPersistentInfo.storyId, Game.Instance.PlayerPersistentInfo.experience);
		Save();
		EnableInputAndIndicators();
		FocusOnMostImportantMission();
		CasinoActivated();
	}

	private void ShowHalloweenAdvert()
	{
		DisableInputAndIndicators();
		m_GuiDialogs.ShowHalloweenAdvert(HalloweenAdvertClose);
	}

	private void HalloweenAdvertClose()
	{
		m_GuiDialogs.HideHalloweenAdvert();
		m_HalloweenAdvertShown = true;
		Save();
		EnableInputAndIndicators();
		FocusOnMostImportantMission();
	}

	private void ShowTapJoyAdvert()
	{
		DisableInputAndIndicators();
		m_GuiDialogs.ShowTapJoyAdvert(TapJoyAdvertClose);
	}

	private void TapJoyAdvertClose()
	{
		m_GuiDialogs.HideTapJoyAdvert();
		m_TapJoyAdvertShown = true;
		Save();
		EnableInputAndIndicators();
		FocusOnMostImportantMission();
	}

	private void ShowChristmasAdvert()
	{
		DisableInputAndIndicators();
		m_GuiDialogs.ShowChristmasAdvert(ChristmasAdvertClose);
	}

	private void ChristmasAdvertClose()
	{
		m_GuiDialogs.HideChristmasAdvert();
		m_ChristmasAdvertShown = true;
		Save();
		EnableInputAndIndicators();
		FocusOnMostImportantMission();
	}

	private void ShowCityInterstitial()
	{
		if (!hasSpentRealMoney && Time.timeSinceLevelLoad >= m_TimeToShowInterstitial)
		{
			m_TimeToShowInterstitial = Time.timeSinceLevelLoad + 300f;
			//Advertisement.Instance.ShowInterstitialAd("City");
		}
	}

	private void ShowStoryEvent()
	{
		m_GuiDialogs.ShowStoryEvent(StoryEventClose, 1010500);
	}

	private void StoryEventClose()
	{
		m_GuiDialogs.HideStoryEvent();
		ShowMissionResult();
	}

	private void SpecialRewardClose()
	{
		m_GuiDialogs.HideSpecialReward();
		if (m_Promoted)
		{
			m_GuiDialogs.ShowPromotion(PromotionClose, Game.Instance.PlayerPersistentInfo.storyId >= 2 && ShopDataBridge.Instance.NewItemsUnlocked(Game.Instance.PlayerPersistentInfo.rank));
		}
		else
		{
			MissionFinished();
		}
	}

	private void PromotionClose()
	{
		m_Promoted = false;
		m_GuiDialogs.HidePromotion();
		Game.Instance.PlayerPersistentInfo.AddGold(PlayerPersistantInfo.GoldPerRankUp);
		Save();
		MissionFinished();
	}

	private void ShopActivated()
	{
		DisableInputAndIndicators();
		if (Game.Instance.PlayerPersistentInfo.storyId == 2 && !m_ShopIntroduced)
		{
			m_ShopIntroduced = true;
			GuiShopMenu.Instance.TutorialMode = true;
			Save();
		}
		GuiMainMenu.Instance.ShowScreen("Shop");
	}

	private void SafeHavenActivated()
	{
		DisableInputAndIndicators();
		if (Game.Instance.PlayerPersistentInfo.storyId == 3 && !m_SafeHavenIntroduced)
		{
			m_SafeHavenIntroduced = true;
			Save();
		}
		SafeHaven.Instance.Show();
	}

	private void MoneyIconActivated()
	{
		m_Input.Disable();
		CancelInvoke("EnableInput");
		Invoke("EnableInput", 0.5f);
	}

	private void ArenaActivated()
	{
		DisableInputAndIndicators();
		m_GuiArena.ShowArenaStart(ArenaStartAccept, ArenaStartClose, m_SiteManager.iconArena.siteInfo as CityArenaInfo);
	}

	private void EquipActivated()
	{
		m_ActiveIcon = null;
		DisableInputAndIndicators();
		if (Game.Instance.PlayerPersistentInfo.storyId == 2 && !m_EquipIntroduced)
		{
			GuiEquipMenu.Instance.TutorialMode = true;
			m_EquipIntroduced = true;
			Save();
		}
		GuiMainMenu.Instance.ShowScreen("Equip");
	}

	private void BankActivated()
	{
		ChartBoost.CacheMoreApps();
		if (!hasSpentRealMoney)
		{
			if (!ChartBoost.HasCachedInterstitial("Bank"))
			{
				ChartBoost.CacheInterstitial("Bank");
				Debug.Log("BankActivated() : caching an Interstitial");
			}
			//Advertisement.Instance.ShowInterstitialAd("Bank");
		}
		DisableInputAndIndicators();
		GuiMainMenu.Instance.ShowScreen("Bank");
	}

	private void FreeGoldActivated()
	{
		ChartBoost.CacheMoreApps();
		if (!hasSpentRealMoney && !ChartBoost.HasCachedInterstitial("FreeGold"))
		{
			ChartBoost.CacheInterstitial("FreeGold");
			Debug.Log("FreeGoldActivated() : caching an Interstitial");
		}
		DisableInputAndIndicators();
		GuiMainMenu.Instance.ShowScreen("FreeGold");
	}

	private void CasinoActivated()
	{
		if (!hasSpentRealMoney && !ChartBoost.HasCachedInterstitial("Casino"))
		{
			ChartBoost.CacheInterstitial("Casino");
			Debug.Log("CasinoActivated() : caching an Interstitial");
		}
		DisableInputAndIndicators();
		m_GuiResources.HideAllIndicators();
		m_GuiResources.HideObjective();
		SwitchCameras(CameraType.CASINO);
		m_CityCamera.enabled = false;
		m_Casino.Show(CasinoDectivated);
	}

	private void CasinoDectivated()
	{
		EnableInputAndIndicators();
		UpdateObjective();
		SwitchCameras(CameraType.CITY);
		m_CityCamera.enabled = true;
		m_Casino.Hide();
		UpdateMissionHudIndicators(false);
		if (!hasSpentRealMoney)
		{
			//Advertisement.Instance.ShowInterstitialAd("Casino");
		}
	}

	private void SwitchCameras(CameraType camera)
	{
		switch (camera)
		{
		case CameraType.CITY:
			m_CityCameraObject.GetComponent<Camera>().enabled = true;
			m_CasinoCameraObject.GetComponent<Camera>().enabled = false;
			break;
		case CameraType.CASINO:
			m_CityCameraObject.GetComponent<Camera>().enabled = false;
			m_CasinoCameraObject.GetComponent<Camera>().enabled = true;
			break;
		default:
			Debug.LogWarning("Unknown camera type!");
			break;
		}
	}

	private void UpdateMissionHudIndicators(bool onlyCameraMoved)
	{
		if (m_GuiResources == null)
		{
			return;
		}
		int num = 0;
		m_GuiResources.HideAllIndicators();
		foreach (CitySiteIcon spawnedIcon in m_SiteManager.SpawnedIcons)
		{
			Vector3 intersect;
			if (m_HudUtility.FindIntersectionWithScreen(m_CityCamera.WorldToScreenPoint(spawnedIcon.transform.position), out intersect))
			{
				spawnedIcon.indicatorIndex = num;
				m_GuiResources.MoveIndicator(CitySiteInfo.InfoType.Normal, num, intersect);
				m_GuiResources.ShowIndicator(CitySiteInfo.InfoType.Normal, num);
				num++;
			}
			else
			{
				spawnedIcon.indicatorIndex = -1;
			}
		}
		if (Game.Instance.PlayerPersistentInfo.storyId >= 3)
		{
			Vector3 indicatorPos = m_GuiResources.GetIndicatorPos(CitySiteInfo.InfoType.SafeHaven, 0);
			indicatorPos.y = m_HudUtility.GetScreenBottom() - 170f * ((float)Screen.height / 1200f);
			m_GuiResources.MoveIndicator(CitySiteInfo.InfoType.SafeHaven, 0, indicatorPos);
			m_GuiResources.ShowIndicator(CitySiteInfo.InfoType.SafeHaven, 0);
			if (Game.Instance.PlayerPersistentInfo.storyId == 3 && m_SafeHavenTutorialIntroduced && !m_SafeHavenIntroduced)
			{
				m_GuiResources.MoveIndicatorTutorial(CitySiteInfo.InfoType.SafeHaven, indicatorPos);
				m_GuiResources.ShowIndicatorTutorial(CitySiteInfo.InfoType.SafeHaven);
			}
		}
		if (Game.Instance.PlayerPersistentInfo.storyId >= 2)
		{
			Vector3 indicatorPos2 = m_GuiResources.GetIndicatorPos(CitySiteInfo.InfoType.Shop, 0);
			indicatorPos2.y = m_HudUtility.GetScreenBottom() - 170f * ((float)Screen.height / 1200f);
			m_GuiResources.MoveIndicator(CitySiteInfo.InfoType.Shop, 0, indicatorPos2);
			m_GuiResources.ShowIndicator(CitySiteInfo.InfoType.Shop, 0);
			if (Game.Instance.PlayerPersistentInfo.storyId == 2 && m_ShopTutorialIntroduced && !m_ShopIntroduced)
			{
				m_GuiResources.MoveIndicatorTutorial(CitySiteInfo.InfoType.Shop, indicatorPos2);
				m_GuiResources.ShowIndicatorTutorial(CitySiteInfo.InfoType.Shop);
			}
		}
		if (Game.Instance.PlayerPersistentInfo.storyId >= 2)
		{
			Vector3 indicatorPos3 = m_GuiResources.GetIndicatorPos(CitySiteInfo.InfoType.Equip, 0);
			indicatorPos3.y = m_HudUtility.GetScreenBottom() - 170f * ((float)Screen.height / 1200f);
			m_GuiResources.MoveIndicator(CitySiteInfo.InfoType.Equip, 0, indicatorPos3);
			m_GuiResources.ShowIndicator(CitySiteInfo.InfoType.Equip, 0);
			if (Game.Instance.PlayerPersistentInfo.storyId == 2 && m_EquipTutorialIntroduced && !m_EquipIntroduced && m_ShopIntroduced)
			{
				m_GuiResources.MoveIndicatorTutorial(CitySiteInfo.InfoType.Equip, indicatorPos3);
				m_GuiResources.ShowIndicatorTutorial(CitySiteInfo.InfoType.Equip);
			}
		}
		if (Game.Instance.PlayerPersistentInfo.storyId >= 2)
		{
			Vector3 indicatorPos4 = m_GuiResources.GetIndicatorPos(CitySiteInfo.InfoType.Bank, 0);
			indicatorPos4.y = m_HudUtility.GetScreenBottom() - 170f * ((float)Screen.height / 1200f);
			m_GuiResources.MoveIndicator(CitySiteInfo.InfoType.Bank, 0, indicatorPos4);
			m_GuiResources.ShowIndicator(CitySiteInfo.InfoType.Bank, 0);
		}
		if (Game.Instance.PlayerPersistentInfo.totalMissionsPlayed >= 3)
		{
			Vector3 indicatorPos5 = m_GuiResources.GetIndicatorPos(CitySiteInfo.InfoType.Casino, 0);
			indicatorPos5.y = m_HudUtility.GetScreenBottom() - 170f * ((float)Screen.height / 1200f);
			m_GuiResources.MoveIndicator(CitySiteInfo.InfoType.Casino, 0, indicatorPos5);
			m_GuiResources.ShowIndicator(CitySiteInfo.InfoType.Casino, 0);
		}
	}

	private void IndicatorPressed(int index)
	{
		foreach (CitySiteIcon spawnedIcon in m_SiteManager.SpawnedIcons)
		{
			if (spawnedIcon.indicatorIndex == index)
			{
				m_CityCamera.CenterCameraOn(spawnedIcon.transform.position);
				DisableInputForShortTime();
				break;
			}
		}
	}

	private void Indicator1Pressed()
	{
		IndicatorPressed(0);
	}

	private void Indicator2Pressed()
	{
		IndicatorPressed(1);
	}

	private void Indicator3Pressed()
	{
		IndicatorPressed(2);
	}

	private void Indicator4Pressed()
	{
		IndicatorPressed(3);
	}

	private void Indicator5Pressed()
	{
		IndicatorPressed(4);
	}

	private void Indicator6Pressed()
	{
		IndicatorPressed(5);
	}

	private void Indicator7Pressed()
	{
		IndicatorPressed(6);
	}

	private void Indicator8Pressed()
	{
		IndicatorPressed(7);
	}

	private void Indicator9Pressed()
	{
		IndicatorPressed(8);
	}

	private void Indicator10Pressed()
	{
		IndicatorPressed(9);
	}

	private void IndicatorHideoutPressed()
	{
		SafeHavenActivated();
	}

	private void IndicatorShopPressed()
	{
		ShopActivated();
	}

	private void IndicatorEquipPressed()
	{
		EquipActivated();
	}

	private void IndicatorFreeGoldPressed()
	{
		FreeGoldActivated();
	}

	private void IndicatorCasinoPressed()
	{
		CasinoActivated();
	}

	private void StatusBarBackPressed()
	{
	}

	private void StatusBarGoldPressed()
	{
		if (m_ShopIntroduced)
		{
			GuiMainMenu.Instance.ShowScreen("Shop");
			GuiShopMenu.Instance.SwitchToCategory(E_ShopCategory.Funds, new ShopItemId(1, GuiShop.E_ItemType.Fund));
		}
	}

	private void StatusBarMoneyPressed()
	{
		if (m_ShopIntroduced)
		{
			GuiMainMenu.Instance.ShowScreen("Shop");
			GuiShopMenu.Instance.SwitchToCategory(E_ShopCategory.Funds, new ShopItemId(6, GuiShop.E_ItemType.Fund));
		}
	}

	private void StatusBarXpPressed()
	{
		if (m_SafeHavenIntroduced)
		{
			SafeHavenActivated();
		}
	}

	private void StatusBarOptionsPressed()
	{
		OnCityMapSuspend();
		GuiOptionsMenu.Instance.m_OnHideDelegate = OnCityMapResume;
		GuiOptionsMenu.Instance.Show();
	}

	private void CameraMoveEvent(Vector3 camPos)
	{
		UpdateMissionHudIndicators(true);
	}

	private void TouchEvent(Collider collider)
	{
		CitySiteIcon component = collider.gameObject.GetComponent<CitySiteIcon>();
		if ((bool)component)
		{
			m_CityCamera.MoveCameraCloserTo(component.transform.position);
			if (component.siteInfo is CityMissionInfo)
			{
				DisableInputAndIndicators();
				m_ActiveIcon = component;
				m_GuiDialogs.ShowMissionStart(MissionStartAccept, MissionStartClose, MissionStartEquip, MissionStartBuy, MissionStartEquippedOwned, component.siteInfo as CityMissionInfo);
			}
			else if (component.siteInfo.infoType == CitySiteInfo.InfoType.Shop)
			{
				ShopActivated();
			}
			else if (component.siteInfo.infoType == CitySiteInfo.InfoType.SafeHaven)
			{
				SafeHavenActivated();
			}
			else if (component.siteInfo.infoType == CitySiteInfo.InfoType.Arena)
			{
				ArenaActivated();
			}
			else if (component.siteInfo.infoType == CitySiteInfo.InfoType.Money)
			{
				MoneyIconActivated();
			}
			else if (component.siteInfo.infoType == CitySiteInfo.InfoType.Equip)
			{
				EquipActivated();
			}
			else if (component.siteInfo.infoType == CitySiteInfo.InfoType.Bank)
			{
				BankActivated();
			}
			else if (component.siteInfo.infoType == CitySiteInfo.InfoType.Casino)
			{
				CasinoActivated();
			}
		}
	}

	private void DisableInputForShortTime()
	{
		m_Input.Disable();
		m_GuiResources.CityHudDisableInput();
		CancelInvoke("EnableInput");
		Invoke("EnableInput", 0.2f);
	}

	private void EnableInput()
	{
		CitySiteInfo.InfoType infoType = CitySiteInfo.InfoType.Normal;
		if (Game.Instance.PlayerPersistentInfo.storyId == 2 && !m_ShopIntroduced)
		{
			infoType = (m_ShopTutorialIntroduced ? CitySiteInfo.InfoType.Shop : CitySiteInfo.InfoType.None);
		}
		else if (Game.Instance.PlayerPersistentInfo.storyId == 2 && !m_EquipIntroduced)
		{
			infoType = ((!m_EquipTutorialIntroduced) ? CitySiteInfo.InfoType.None : CitySiteInfo.InfoType.Equip);
		}
		else if (Game.Instance.PlayerPersistentInfo.storyId == 3 && !m_SafeHavenIntroduced)
		{
			infoType = ((!m_SafeHavenTutorialIntroduced) ? CitySiteInfo.InfoType.None : CitySiteInfo.InfoType.SafeHaven);
		}
		m_GuiResources.CityHudEnableInput(infoType);
		m_GuiResources.statusBar.EnableControls(infoType == CitySiteInfo.InfoType.Normal && m_ShopIntroduced);
		if ((Game.Instance.PlayerPersistentInfo.storyId == 3 && !m_SafeHavenIntroduced) || (Game.Instance.PlayerPersistentInfo.storyId == 2 && !m_EquipIntroduced) || (Game.Instance.PlayerPersistentInfo.storyId == 2 && !m_ShopIntroduced))
		{
			m_Input.Disable();
		}
		else
		{
			m_Input.Enable(Game.Instance.PlayerPersistentInfo.storyId >= 3);
		}
	}

	private void DisableInput()
	{
		m_GuiResources.CityHudDisableInput();
		m_GuiResources.statusBar.EnableControls(false);
		m_Input.Disable();
	}

	private void DisableInputAndIndicators()
	{
		DisableInput();
	}

	private void EnableInputAndIndicators()
	{
		UpdateMissionHudIndicators(false);
		EnableInput();
	}

	private void StopHelicopterCoroutine()
	{
		StopCoroutine("SpawnHelicopterCoroutine");
		m_GuiResources.HideNotification();
		if ((bool)m_ActiveHelicopterSlot)
		{
			m_ScheduledChopperDrop = Game.Instance.PlayerPersistentInfo.totalMissionsPlayed + 8;
			m_SiteManager.SpawnHelicopterMissions(Game.Instance.PlayerPersistentInfo.storyId, Game.Instance.PlayerPersistentInfo.experience, m_ActiveHelicopterSlot);
			m_ActiveHelicopterSlot = null;
		}
		if ((bool)m_ActiveHelicopter)
		{
			Object.Destroy(m_ActiveHelicopter);
			m_ActiveHelicopter = null;
		}
	}

	private IEnumerator SpawnHelicopterCoroutine()
	{
		yield return new WaitForSeconds(17f);
		m_GuiResources.HideNotification();
		m_ScheduledChopperDrop = Game.Instance.PlayerPersistentInfo.totalMissionsPlayed + 8;
		m_SiteManager.SpawnHelicopterMissions(Game.Instance.PlayerPersistentInfo.storyId, Game.Instance.PlayerPersistentInfo.experience, m_ActiveHelicopterSlot);
		m_ActiveHelicopterSlot = null;
		yield return new WaitForSeconds(5f);
		Object.Destroy(m_ActiveHelicopter);
		m_ActiveHelicopter = null;
	}

	private IEnumerator LoadMission(CityMissionInfo info)
	{
		yield return new WaitForSeconds(0.3f);
		Game.Instance.StandaloneMission = false;
		Game.Instance.EnemyAutoSpawn = false;
		Game.Instance.GameplayType = GameplayType.Missions;
		Game.Instance.MissionType = info.missionType;
		Game.Instance.MissionSubtype = info.missionSubtype;
		Game.Instance.Difficulty = info.difficulty;
		Game.Instance.LoadLevel(info.level);
	}

	private IEnumerator LoadArena(string levelName)
	{
		yield return new WaitForSeconds(0.3f);
		Game.Instance.StandaloneMission = false;
		Game.Instance.EnemyAutoSpawn = false;
		Game.Instance.GameplayType = GameplayType.Arena;
		Kontagent.SendCustomEvent(levelName, "Game", "Equip", string.Empty, Game.Instance.PlayerPersistentInfo.rank);
		/*UnityAnalyticsWrapper.ReportCustomEvent("loadArena", new Dictionary<string, object>
		{
			{ "levelName", levelName },
			{
				"playerRank",
				Game.Instance.PlayerPersistentInfo.rank
			}
		});*/
		Game.Instance.LoadLevel(levelName);
	}

	private void OnExitButton()
	{
		string inCaption = TextDatabase.instance[2000002];
		string inText = TextDatabase.instance[2000027];
		GuiMainMenu.Instance.ShowPopup("ExitConfirm", inCaption, inText, ExitResultHandler);
	}

	private void ExitResultHandler(BasePopupScreen inPopup, E_PopupResultCode inResult)
	{
		if (inResult == E_PopupResultCode.Ok)
		{
			Debug.Log("Quitting application");
			Application.Quit();
		}
	}

	public void EnableStatusBarButtons(bool hidden)
	{
		m_GuiResources.EnableStatusBarButtons(hidden);
	}
}
