using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Mission : MonoBehaviour
{
	public enum E_GuiState
	{
		E_GS_NONE = 0,
		E_GS_HUD = 1,
		E_GS_INGAME_MENU = 2,
		E_GS_FIRST_HELP = 3
	}

	public enum E_MissionResult
	{
		Success = 0,
		PlayerKilled = 1,
		DoorDestroyed = 2
	}

	public static Mission Instance;

	public List<GameZoneBase> GameZones = new List<GameZoneBase>();

	public List<GameObject> ManagedGameObject = new List<GameObject>();

	public AgentsCache2 AgentsCache = new AgentsCache2();

	public ExplosionCache ExplosionCache = new ExplosionCache();

	public int AchievementID;

	public int LeaderBoardID;

	private E_GuiState m_CurrentGuiState;

	public AudioClip m_MissionSuccessSound;

	public AudioClip m_MissionFailedSound;

	public bool DisablePause;

	[NonSerialized]
	public GameZoneBase CurrentGameZone;

	[NonSerialized]
	private NESController NESController;

	[NonSerialized]
	private bool m_EndOfMissionActive;

	[NonSerialized]
	private GuiMissionResult m_GuiMissionResult = new GuiMissionResult();

	public bool MissionIsEnding
	{
		get
		{
			return m_EndOfMissionActive;
		}
	}

	public int ProgressNum { get; private set; }

	public int GameZoneIndex
	{
		get
		{
			for (int i = 0; i < GameZones.Count; i++)
			{
				if (GameZones[i] == CurrentGameZone)
				{
					return i;
				}
			}
			Debug.LogError("Unknow game zone");
			return 0;
		}
	}

	public NESController GlobalNESController
	{
		get
		{
			return NESController;
		}
	}

	private void Awake()
	{
		if (!m_EndOfMissionActive)
		{
			m_EndOfMissionActive = false;
		}
		Instance = this;
		ProgressNum = 0;
		CamExplosionFXMgr.PreloadResources();
		UnityEngine.Random.seed = DateTime.Now.Second;
		Transform transform = base.gameObject.transform.parent.Find("_GameZone");
		if (transform != null)
		{
			NESController = transform.gameObject.GetComponent<NESController>();
		}
		if (NESController == null)
		{
		}
		AgentsCache.InitPlayer();
		for (int i = 0; i < GameZones.Count; i++)
		{
			if (i == Game.Instance.CurrentGameZone)
			{
				CurrentGameZone = GameZones[i];
			}
			else
			{
				GameZones[i].Disable();
			}
		}
	}

	private void Start()
	{
		if (Game.Instance.GameType == E_GameType.SinglePlayer && Game.Instance.CurrentGameZone != 0)
		{
			Save_Load();
		}
		DeviceInfo.UpdatePerformanceSettings();
		MFGuiManager.Instance.SetFadeOut(1f);
		AudioListener.pause = true;
		AudioListener.volume = 0f;
		Game.Instance.GameState = E_GameState.Game;
		Game.Instance.CurrentLevel = Application.loadedLevelName;
		ExplosionCache.Init();
		Game.Instance.LeaderBoardID = LeaderBoardID;
		LoadGui();
		Shader.WarmupAllShaders();
		Invoke("WaitForLogGui", 0.1f);
		Game.Instance.PlayerPersistentInfo.MissionStart();
		GameplayData.Instance.RecomputeItemModifiers();
		Kontagent.SendEquipToKontagent(Game.Instance.PlayerPersistentInfo);
	}

	private void Update()
	{
		Game.Instance.MissionResultData.MissionTime += Time.deltaTime;
	}

	private void OnDestroy()
	{
		ExplosionCache.Clear();
		ExplosionCache = null;
		Game.Instance.PlayerPersistentInfo.MissionEnd();
		if (Game.Instance.GameplayType == GameplayType.Arena)
		{
			Kontagent.SendCustomEvent("ArenaTime", "Game", string.Empty, string.Empty, Game.Instance.PlayerPersistentInfo.rank, (int)(Game.Instance.MissionResultData.MissionTime / 60f));
			/*UnityAnalyticsWrapper.ReportCustomEvent("missionEnd", new Dictionary<string, object>
			{
				{
					"levelName",
					Application.loadedLevel
				},
				{ "type", "arena" },
				{
					"missionTime",
					(int)(Game.Instance.MissionResultData.MissionTime / 60f)
				},
				{
					"playerRank",
					Game.Instance.PlayerPersistentInfo.rank
				}
			});*/
		}
		else
		{
			Kontagent.SendCustomEvent("MissionTime", "Game", string.Empty, string.Empty, Game.Instance.PlayerPersistentInfo.rank, (int)(Game.Instance.MissionResultData.MissionTime / 60f));
			/*UnityAnalyticsWrapper.ReportCustomEvent("missionEnd", new Dictionary<string, object>
			{
				{
					"levelName",
					Application.loadedLevel
				},
				{ "type", "mission" },
				{
					"missionTime",
					(int)(Game.Instance.MissionResultData.MissionTime / 60f)
				},
				{
					"playerRank",
					Game.Instance.PlayerPersistentInfo.rank
				}
			});*/
		}
	}

	private void WaitForLogGui()
	{
		GUIBase_Platform gUIBase_Platform = MFGuiManager.Instance.FindPlatform("Gui_16_9");
		if ((bool)gUIBase_Platform && gUIBase_Platform.IsInitialized())
		{
			StartCoroutine(PrepareForStart());
		}
		else
		{
			Invoke("WaitForLogGui", 0.1f);
		}
	}

	private IEnumerator PrepareForStart()
	{
		yield return new WaitForSeconds(0.2f);
		StartGui();
		CurrentGameZone.Enable();
		GameBlackboard.Instance.GameEvents.Update("RESET", GameEvents.E_State.True);
		GameBlackboard.Instance.GameEvents.Update("RESET", GameEvents.E_State.False);
		StartCoroutine(FadeInAudio_Corout(1f, GuiOptions.ListenerVolume));
		yield return new WaitForSeconds(1f);
		MFGuiManager.Instance.FadeIn(2f);
		if (Game.Instance.PlayerPersistentInfo.totalMissionsPlayed == 0 && !Game.Instance.StandaloneMission && custom_inputs.IsNVidiaShield())
		{
			GuiNvidiaShieldHelp.Instance.Show(null, true);
			while (GuiNvidiaShieldHelp.Instance.IsShown)
			{
				yield return new WaitForEndOfFrame();
			}
		}
		SpawnPlayer();
	}

	private void ShowHelpFirstTime()
	{
		GuiHUD.Instance.SwitchToFirstHelp();
	}

	private void SpawnPlayer()
	{
		GameObject agentFromCache = GetAgentFromCache(E_AgentType.Player);
		Player.Instance = agentFromCache.GetComponent<ComponentPlayer>();
		Player.Instance.Owner.GameObject._SetActiveRecursively(true);
		Player.Instance.Owner.SendMessage("Initialize");
		Player.Instance.Owner.SendMessage("Activate", CurrentGameZone.PlayerSpawn);
		ChangeGuiState(E_GuiState.E_GS_HUD);
	}

	private void LogTextures()
	{
		Texture[] array = Resources.FindObjectsOfTypeAll(typeof(Texture)) as Texture[];
		Texture[] array2 = array;
		foreach (Texture texture in array2)
		{
			if (texture.width * texture.height * 4 / 1024 > 1024)
			{
				Debug.Log("Resource loaded " + texture.name + " " + texture.width * texture.height * 4 / 1024 + "Kb");
			}
		}
	}

	private void LogAnims()
	{
		AnimationClip[] array = Resources.FindObjectsOfTypeAll(typeof(AnimationClip)) as AnimationClip[];
		AnimationClip[] array2 = array;
		foreach (AnimationClip animationClip in array2)
		{
			Debug.Log("Resource loaded " + animationClip.name + " " + animationClip.length);
		}
	}

	private void LogResources(Type type)
	{
		UnityEngine.Object[] array = Resources.FindObjectsOfTypeAll(type);
		UnityEngine.Object[] array2 = array;
		foreach (UnityEngine.Object @object in array2)
		{
			Debug.Log("Resource loaded " + @object.ToString());
		}
	}

	private IEnumerator FadeInAudio_Corout(float fadeTime, float targetVolume)
	{
		float beginFadeTime = Time.realtimeSinceStartup;
		AudioListener.volume = 0f;
		AudioListener.pause = false;
		while (Time.realtimeSinceStartup - beginFadeTime <= fadeTime)
		{
			float volume = targetVolume * Mathf.Max((Time.realtimeSinceStartup - beginFadeTime) / fadeTime, 1f);
			AudioListener.volume = volume;
			yield return new WaitForEndOfFrame();
		}
		AudioListener.volume = targetVolume;
	}

	public void Save_Save()
	{
		GameBlackboard.Instance.Save_Save();
	}

	public void Save_Load()
	{
		GameBlackboard.Instance.Save_Load();
	}

	private void LoadGui()
	{
		string text = "Gui_16_9";
		if (Game.IsHDResolution())
		{
			text = "Gui_16_9";
			if ((float)Screen.width / (float)Screen.height <= 1.4f)
			{
				text = "Gui_4_3";
			}
		}
		Application.LoadLevelAdditive(text);
	}

	private void StartGui()
	{
		if (!GuiHUD.Instance)
		{
			Debug.LogError("Can't find GuiHUD script.");
			return;
		}
		if (!GuiIngameMenu.Instance)
		{
			Debug.LogError("Can't find GuiIngameMenu script.");
			return;
		}
		GuiHUD.Instance.Init(this);
		GuiIngameMenu.Instance.Init(this);
		m_GuiMissionResult.Init();
		GuiHelpMenu.Instance.Init();
	}

	public void ChangeGuiState(E_GuiState newState)
	{
		switch (m_CurrentGuiState)
		{
		case E_GuiState.E_GS_HUD:
			GuiHUD.Instance.Hide();
			break;
		case E_GuiState.E_GS_INGAME_MENU:
			GuiIngameMenu.Instance.Hide();
			break;
		case E_GuiState.E_GS_FIRST_HELP:
			GuiHelpMenu.Instance.Hide();
			break;
		default:
			Debug.LogError("Unsupported case");
			break;
		case E_GuiState.E_GS_NONE:
			break;
		}
		m_CurrentGuiState = newState;
		switch (m_CurrentGuiState)
		{
		case E_GuiState.E_GS_NONE:
			break;
		case E_GuiState.E_GS_HUD:
			GuiHUD.Instance.Show();
			break;
		case E_GuiState.E_GS_INGAME_MENU:
			GuiIngameMenu.Instance.Show();
			break;
		case E_GuiState.E_GS_FIRST_HELP:
			GuiHelpMenu.Instance.Show();
			break;
		default:
			Debug.LogError("Unsupported case");
			break;
		}
	}

	public void EndOfMission(E_MissionResult result, GUIBase_Button.TouchDelegate touchDelegate = null)
	{
		m_EndOfMissionActive = true;
		StopAllCoroutines();
		GuiSubtitles.DeactivateAllRuning();
		GuiHUD.Instance.Hide();
		if ((bool)base.GetComponent<AudioSource>())
		{
			if (base.GetComponent<AudioSource>().isPlaying)
			{
				base.GetComponent<AudioSource>().Stop();
			}
			base.GetComponent<AudioSource>().loop = false;
		}
		if (result == E_MissionResult.Success)
		{
			Game.Instance.MissionResultData.Result = MissionResult.Type.SUCCESS;
			if ((bool)base.GetComponent<AudioSource>())
			{
				base.GetComponent<AudioSource>().clip = m_MissionSuccessSound;
				base.GetComponent<AudioSource>().Play();
			}
		}
		else
		{
			Game.Instance.MissionResultData.Result = MissionResult.Type.FAIL;
			if ((bool)base.GetComponent<AudioSource>())
			{
				base.GetComponent<AudioSource>().clip = m_MissionFailedSound;
				base.GetComponent<AudioSource>().Play();
			}
		}
		PickupManager.Instance.OnEndOfMission();
		StartCoroutine(_EndOfMission(result, touchDelegate));
	}

	private IEnumerator _EndOfMission(E_MissionResult result, GUIBase_Button.TouchDelegate touchDelegate = null)
	{
		TimeManager.Instance.SetTimeScale(0.15f, 0.15f, 0f, 0f);
		Player.Instance.StopMove(true);
		Player.Instance.Owner.BlackBoard.Stop = true;
		Player.Instance.Owner.BlackBoard.Invulnerable = true;
		yield return new WaitForSeconds(0.2f);
		if (ArenaDirector.Instance != null)
		{
			m_GuiMissionResult.SetArenaScore(ArenaDirector.Instance.GetWave(), ArenaDirector.Instance.GetScore());
			Game.Instance.MissionResultData.ArenaScore = ArenaDirector.Instance.GetScore();
			Game.Instance.MissionResultData.ArenaWaves = ArenaDirector.Instance.GetWave();
		}
		m_GuiMissionResult.ShowMissionResult(result, touchDelegate);
		MusicManager.Instance.FadeOutMusic(0.4f);
		yield return new WaitForSeconds(0.6f);
		MFGuiManager.Instance.FadeOut();
		Game.Instance.Save_Clear();
		yield return new WaitForSeconds(0.4f);
		TimeManager.Instance.SetTimeScale(1f, 0f, 0f, 0f);
		if (Game.Instance.StandaloneMission)
		{
			if ((bool)CurrentGameZone)
			{
				CurrentGameZone.KillAllEnemies(Player.Instance.Owner);
			}
			m_GuiMissionResult.HideMissionResult();
			m_EndOfMissionActive = false;
			StartCoroutine(LoadLastSave(3f));
		}
		else
		{
			Game.Instance.LoadMainMenu();
		}
	}

	public void RestartCheckpoint()
	{
		GuiSubtitles.DeactivateAllRuning();
		StopAllCoroutines();
		StartCoroutine(LoadLastSave(0f));
	}

	private IEnumerator LoadLastSave(float delay)
	{
		MusicManager.Instance.FadeOutMusic(2f);
		yield return new WaitForSeconds(delay);
		GuiHUD.Instance.Hide();
		yield return new WaitForEndOfFrame();
		Game.Instance.MissionResultData.MissionTime = 0f;
		GuiHUD.Instance.OnMissionReset();
		MFGuiManager.Instance.FadeOut();
		yield return new WaitForSeconds(MFGuiManager.Instance.FadeRemainingTime);
		GuiHUD.Instance.HideAllMessages();
		Player.Instance.Owner.SendMessage("Deactivate");
		yield return new WaitForSeconds(0.1f);
		ExplosionCache.Reset();
		CurrentGameZone.Disable();
		ProjectileManager.Instance.Reset();
		if (CamExplosionFXMgr.Instance != null)
		{
			CamExplosionFXMgr.Instance.Reset();
		}
		for (int i = 0; i < ManagedGameObject.Count; i++)
		{
			ManagedGameObject[i]._SetActiveRecursively(true);
		}
		Game.Instance.Save_Load();
		Save_Load();
		yield return new WaitForEndOfFrame();
		CurrentGameZone = GameZones[Game.Instance.CurrentGameZone];
		CurrentGameZone.Enable();
		GameBlackboard.Instance.GameEvents.Update("RESET", GameEvents.E_State.True);
		GuiHUD.Instance.Reset();
		yield return new WaitForSeconds(0.1f);
		GameBlackboard.Instance.GameEvents.Update("RESET", GameEvents.E_State.False);
		yield return new WaitForSeconds(1f);
		MFGuiManager.Instance.FadeIn(2f);
		MusicManager.Instance.PlayDefaultMusic();
		yield return new WaitForSeconds(0.1f);
		ReturnAgentToCache(Player.Instance.Owner.GameObject);
		Player.Instance = null;
		yield return new WaitForSeconds(0.1f);
		SpawnPlayer();
		GuiHUD.Instance.Show();
	}

	public GameObject GetAgentFromCache(E_AgentType Type)
	{
		GameObject gameObject = AgentsCache.SpitOut(Type);
		if (gameObject != null)
		{
			gameObject._SetActiveRecursively(true);
		}
		return gameObject;
	}

	public void ReturnAgentToCache(GameObject obj)
	{
		obj.SendMessage("Deactivate");
		obj._SetActiveRecursively(false);
		AgentsCache.Swallow(obj);
	}

	public void UnlockNextGameZone()
	{
		int gameZoneIndex = GameZoneIndex;
		if (gameZoneIndex + 1 < GameZones.Count)
		{
			GameZones[gameZoneIndex + 1].Enable();
		}
	}

	public void LockPrevGameZone()
	{
		int gameZoneIndex = GameZoneIndex;
		if (gameZoneIndex > 0)
		{
			GameZones[gameZoneIndex - 1].Disable();
		}
	}

	private void OnApplicationFocus(bool focus)
	{
		if (!m_EndOfMissionActive && !DisablePause && Game.Instance.GameState != E_GameState.IngameMenu)
        {
            if (GuiIngameMenu.Instance != null)
            {
                GuiIngameMenu.Instance.DisableRestartButton(true);
                GuiHUD.Instance.SwitchToIngameMenu();
            }
		}
		AudioListener.pause = true;
	}

	private void OnApplicationPause(bool focus)
	{
		if (!m_EndOfMissionActive && !DisablePause && (bool)Game.Instance && Game.Instance.GameState != E_GameState.IngameMenu)
		{
			// dont throw the error every fucking time i test the level
			if (GuiIngameMenu.Instance != null)
			{
                GuiIngameMenu.Instance.DisableRestartButton(true);
                GuiHUD.Instance.SwitchToIngameMenu();
            }
		}
		AudioListener.pause = true;
	}

	public void SendGameEvent(GameEvent inGameEvent)
	{
		StartCoroutine(SendGameEvent_Corutine(inGameEvent.Name, inGameEvent.State, inGameEvent.Delay));
	}

	public void SendGameEvent(string inEventName, GameEvents.E_State inState, float inDelay)
	{
		StartCoroutine(SendGameEvent_Corutine(inEventName, inState, inDelay));
	}

	private IEnumerator SendGameEvent_Corutine(string inEventName, GameEvents.E_State inState, float inDelay)
	{
		yield return new WaitForSeconds(inDelay);
		GameBlackboard.Instance.GameEvents.Update(inEventName, inState);
	}
}
