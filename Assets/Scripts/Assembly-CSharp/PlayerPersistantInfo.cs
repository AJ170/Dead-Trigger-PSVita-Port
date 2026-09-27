using System;
using System.Collections;
using LitJson;
using UnityEngine;

public class PlayerPersistantInfo
{
	public class MissionProgress
	{
		public int Rank;

		public PlayerLevelData.PlayerLevelInfo LevelInfo;
	}

	public delegate void PPIUpdateDelegate();

	public delegate void PersistenInfoChanged();

	private const string ERR_STR_UNSYNCED_PPI_UPDATE = "Ignoring PPI update command on unsynced PPI";

	public PPIUpdateDelegate m_OnPPIUpdate;

	private bool GeneratePPIChangeNotifications;

	private string RefPPIForNotificationsJSON;

	public string Name;

	public string UserName;

	public bool IsValid = true;

	public bool IsCheater;

	public PersistenInfoChanged OnExperienceChanged;

	public PersistenInfoChanged OnRankChanged;

	public PersistenInfoChanged OnMoneyChanged;

	public PersistenInfoChanged OnGoldChanged;

	public PersistenInfoChanged OnTicketChanged;

	public PersistenInfoChanged OnRoundMoneyChanged;

	private PlayerPersistentInfoData PlayerData = new PlayerPersistentInfoData();

	private MissionProgress CurrentMission = new MissionProgress();

	public int experience
	{
		get
		{
			return PlayerData.Params.Experience;
		}
	}

	public int rank
	{
		get
		{
			return GetPlayerRankFromExperience(experience);
		}
	}

	public int money
	{
		get
		{
			return PlayerData.Params.Money;
		}
	}

	public int gold
	{
		get
		{
			return PlayerData.Params.Gold;
		}
	}

	public int numKills
	{
		get
		{
			return PlayerData.Params.Kills;
		}
	}

	public int numHeadshots
	{
		get
		{
			return PlayerData.Params.HeadShots;
		}
	}

	public int numTickets
	{
		get
		{
			return PlayerData.Params.Tickets;
		}
	}

	// The stored GoldForGoldMission is the ladder POSITION (1..5, climbing one step
	// per consecutive day). Scaling here rather than at the award site keeps the
	// preview label in CityGUIDialogs and the actual payout in SpecialReward in
	// agreement, and leaves the raw counter alone so the increment/reset rules are
	// untouched. At x5 the ladder pays 5/10/15/20/25.
	private const int GoldMissionMultiplier = 5;

	// Gold granted on rank up. There are two award sites - the HUD banner when the
	// rank is crossed mid-mission (HudCombatInfo) and the promotion dialog back in
	// the city when the rank is crossed by the end-of-mission XP (CityManager).
	// Only one of them fires per rank, so they must agree; hence one constant.
	public const int GoldPerRankUp = 50;

	// Bunker mission payouts. SpecialReward builds both the award and the dialog
	// caption from these, so the banner cannot disagree with what is granted.
	public const int GoldPerBunker = 50;

	public const int GoldPerBunker2 = 100;

	// Daily reward casino tickets. Granted in CityManager.DailyRewardClose and
	// captioned in the DailyReward dialog - two files, hence one constant.
	public const int TicketsPerDailyReward = 1;

	public int numGoldForGoldMission
	{
		get
		{
			return PlayerData.Params.GoldForGoldMission * GoldMissionMultiplier;
		}
	}

	public int roundMoney { get; private set; }

	public int storyId
	{
		get
		{
			return PlayerData.Params.m_StoryId;
		}
	}

	public int chapterProgress
	{
		get
		{
			return PlayerData.Params.m_ChapterProgress;
		}
	}

	public int totalMissionsPlayed
	{
		get
		{
			return PlayerData.Params.m_TotalMissionsPlayed;
		}
	}

	public int storyMissionsPlayed
	{
		get
		{
			return PlayerData.Params.m_StoryMissionsPlayed;
		}
	}

	public int deaths
	{
		get
		{
			return PlayerData.Params.Deaths;
		}
	}

	public float GameTime
	{
		get
		{
			return PlayerData.Params.GameTime;
		}
	}

	public bool ProtectObjectsTutorial
	{
		get
		{
			return PlayerData.Params.ProtectObjectsTutorial;
		}
		set
		{
			PlayerData.Params.ProtectObjectsTutorial = value;
		}
	}

	public PPIInventoryList InventoryList
	{
		get
		{
			return PlayerData.InventoryList;
		}
	}

	public PPIEquipList EquipList
	{
		get
		{
			return PlayerData.EquipList;
		}
		private set
		{
			PlayerData.EquipList = value;
		}
	}

	public PPIUpgradeList Upgrades
	{
		get
		{
			return PlayerData.Upgrades;
		}
	}

	public PPIBankData BankData
	{
		get
		{
			return PlayerData.BankData;
		}
	}

	public bool StatsDirty { get; private set; }

	public int ArenaPlayed(ArenaId arena)
	{
		switch (arena)
		{
		case ArenaId.Arena1:
			return PlayerData.Params.Arena1Played;
		case ArenaId.Arena2:
			return PlayerData.Params.Arena2Played;
		case ArenaId.Arena3:
			return PlayerData.Params.Arena3Played;
		case ArenaId.Arena4:
			return PlayerData.Params.Arena4Played;
		default:
			return 0;
		}
	}

	public float ArenaTotalTime(ArenaId arena)
	{
		switch (arena)
		{
		case ArenaId.Arena1:
			return PlayerData.Params.Arena1Time;
		case ArenaId.Arena2:
			return PlayerData.Params.Arena2Time;
		case ArenaId.Arena3:
			return PlayerData.Params.Arena3Time;
		case ArenaId.Arena4:
			return PlayerData.Params.Arena4Time;
		default:
			return 0f;
		}
	}

	public int ArenaHiMoney(ArenaId arena)
	{
		switch (arena)
		{
		case ArenaId.Arena1:
			return PlayerData.Params.Arena1HiMoney;
		case ArenaId.Arena2:
			return PlayerData.Params.Arena2HiMoney;
		case ArenaId.Arena3:
			return PlayerData.Params.Arena3HiMoney;
		case ArenaId.Arena4:
			return PlayerData.Params.Arena4HiMoney;
		default:
			return 0;
		}
	}

	public int ArenaHiScore(ArenaId arena)
	{
		switch (arena)
		{
		case ArenaId.Arena1:
			return PlayerData.Params.Arena1HiScore;
		case ArenaId.Arena2:
			return PlayerData.Params.Arena2HiScore;
		case ArenaId.Arena3:
			return PlayerData.Params.Arena3HiScore;
		case ArenaId.Arena4:
			return PlayerData.Params.Arena4HiScore;
		default:
			return 0;
		}
	}

	public void ArenaFinished(ArenaId arena, float arenaTime, int score, int money, int xp)
	{
		switch (arena)
		{
		case ArenaId.Arena1:
			PlayerData.Params.Arena1Played++;
			PlayerData.Params.Arena1Time += arenaTime;
			if (score > PlayerData.Params.Arena1HiScore)
			{
				PlayerData.Params.Arena1HiScore = score;
			}
			if (money > PlayerData.Params.Arena1HiMoney)
			{
				PlayerData.Params.Arena1HiMoney = money;
			}
			break;
		case ArenaId.Arena2:
			PlayerData.Params.Arena2Played++;
			PlayerData.Params.Arena2Time += arenaTime;
			if (score > PlayerData.Params.Arena2HiScore)
			{
				PlayerData.Params.Arena2HiScore = score;
			}
			if (money > PlayerData.Params.Arena2HiMoney)
			{
				PlayerData.Params.Arena2HiMoney = money;
			}
			break;
		case ArenaId.Arena3:
			PlayerData.Params.Arena3Played++;
			PlayerData.Params.Arena3Time += arenaTime;
			if (score > PlayerData.Params.Arena3HiScore)
			{
				PlayerData.Params.Arena3HiScore = score;
			}
			if (money > PlayerData.Params.Arena3HiMoney)
			{
				PlayerData.Params.Arena3HiMoney = money;
			}
			break;
		case ArenaId.Arena4:
			PlayerData.Params.Arena4Played++;
			PlayerData.Params.Arena4Time += arenaTime;
			if (score > PlayerData.Params.Arena4HiScore)
			{
				PlayerData.Params.Arena4HiScore = score;
			}
			if (money > PlayerData.Params.Arena4HiMoney)
			{
				PlayerData.Params.Arena4HiMoney = money;
			}
			break;
		}
	}

	public void GeneratedMissionFinished()
	{
		PlayerData.Params.m_TotalMissionsPlayed++;
	}

	public int ChapterMissionFinished()
	{
		PlayerData.Params.m_ChapterProgress++;
		PlayerData.Params.m_StoryMissionsPlayed++;
		PlayerData.Params.m_TotalMissionsPlayed++;
		return PlayerData.Params.m_ChapterProgress;
	}

	public int NextStoryChapter()
	{
		PlayerData.Params.m_ChapterProgress = 0;
		PlayerData.Params.m_StoryId++;
		return PlayerData.Params.m_StoryId;
	}

	public void MissionFinished(bool success, float missionTime)
	{
		PlayerData.Params.MissionCount++;
		if (success)
		{
			PlayerData.Params.MissionSuccess++;
		}
		PlayerData.Params.GameTime += missionTime;
	}

	public PlayerPersistentInfoData GetPlayerData_ForStatistics()
	{
		return PlayerData;
	}

	public void Initialize()
	{
		SetDefaultPPI();
	}

	public void ResetPlayerData()
	{
		PlayerData = new PlayerPersistentInfoData();
		IsCheater = false;
		IsValid = false;
	}

	public void MissionStart()
	{
		CurrentMission.Rank = GetPlayerRankFromExperience(PlayerData.Params.Experience);
		CurrentMission.LevelInfo = GameplayData.Instance.playerLevelData.GetInfo(CurrentMission.Rank);
		roundMoney = 0;
	}

	public void MissionEnd()
	{
		CurrentMission.Rank = -1;
		CurrentMission.LevelInfo = null;
	}

	public string GetPlayerDataAsJsonStr()
	{
		return JsonMapper.ToJson(PlayerData);
	}

	public bool InitPlayerDataFromStr(string jsonStr)
	{
		PlayerPersistentInfoData playerData;
		try
		{
			playerData = JsonMapper.ToObject<PlayerPersistentInfoData>(jsonStr);
		}
		catch (JsonException ex)
		{
			Debug.LogError("JSON exception caught: " + ex.Message);
			return false;
		}
		PlayerData = playerData;
		return true;
	}

	public string GetInventoryAsJSON()
	{
		return JsonMapper.ToJson(PlayerData.InventoryList);
	}

	public string GetEquipListAsJSON()
	{
		return JsonMapper.ToJson(PlayerData.EquipList);
	}

	public void SetArenaHiMoney(ArenaId arenaId, int money)
	{
	}

	public void SetArenaHiXp(ArenaId arenaId, int xp)
	{
	}

	public void SetArenaHiScore(ArenaId arenaId, int score)
	{
	}

	public void AddRoundMoney(int money)
	{
		roundMoney += money;
		if (OnRoundMoneyChanged != null)
		{
			OnRoundMoneyChanged();
		}
	}

	public void AddMoneyBags(int bags)
	{
		roundMoney += GameplayData.Instance.MoneyPerZombie() * bags;
		if (OnRoundMoneyChanged != null)
		{
			OnRoundMoneyChanged();
		}
		Game.Instance.MissionResultData.CollectedMoney += GameplayData.Instance.MoneyPerZombie() * bags;
	}

	public void AddExperience(int experience)
	{
		int playerRankFromExperience = GetPlayerRankFromExperience(PlayerData.Params.Experience);
		PlayerData.Params.Experience += experience;
		if (OnExperienceChanged != null)
		{
			OnExperienceChanged();
		}
		int playerRankFromExperience2 = GetPlayerRankFromExperience(PlayerData.Params.Experience);
		if (playerRankFromExperience2 > playerRankFromExperience)
		{
		}
		if (playerRankFromExperience2 > CurrentMission.Rank && OnRankChanged != null)
		{
			OnRankChanged();
			CurrentMission.Rank = playerRankFromExperience2;
		}
	}

	public void AddMoney(int money)
	{
		if (money != 0)
		{
			PlayerData.Params.Money += money;
			PlayerData.Params.TotalMoney += money;
			if (OnMoneyChanged != null)
			{
				OnMoneyChanged();
			}
		}
	}

	public void RemoveMoney(int money)
	{
		if (money != 0)
		{
			if (PlayerData.Params.Money < money)
			{
				Debug.LogError("Invalid money operation: removing more then have");
				PlayerData.Params.Money = 0;
			}
			else
			{
				PlayerData.Params.Money -= money;
			}
			if (OnMoneyChanged != null)
			{
				OnMoneyChanged();
			}
		}
	}

	public void EquipAddWeapon(E_WeaponID id, E_UpgradeLevel upgrade)
	{
		int num = PlayerData.EquipList.Weapons.FindIndex((PPIWeaponData p) => p.ID == id);
		if (num < 0)
		{
			EquipList.Weapons.Add(new PPIWeaponData
			{
				ID = id,
				UpgradeLevel = upgrade
			});
		}
	}

	public void InventoryAddWeapon(E_WeaponID id, E_UpgradeLevel upgrade)
	{
		int num = PlayerData.InventoryList.Weapons.FindIndex((PPIWeaponData p) => p.ID == id);
		if (num < 0)
		{
			InventoryList.Weapons.Add(new PPIWeaponData
			{
				ID = id,
				UpgradeLevel = upgrade
			});
		}
	}

	public void InventoryRemoveWeapon(E_WeaponID id)
	{
		InventoryList.Weapons.RemoveAll((PPIWeaponData ps) => ps.ID == id);
		EquipList.Weapons.RemoveAll((PPIWeaponData ps) => ps.ID == id);
	}

	public void InventoryAddItem(E_ItemID id, int count)
	{
		int num = PlayerData.InventoryList.Items.FindIndex((PPIItemData p) => p.ID == id);
		if (num >= 0)
		{
			PPIItemData value = PlayerData.InventoryList.Items[num];
			value.Count += count;
			PlayerData.InventoryList.Items[num] = value;
		}
		else
		{
			InventoryList.Items.Add(new PPIItemData
			{
				ID = id,
				Count = count
			});
		}
		int num2 = PlayerData.EquipList.Items.FindIndex((PPIItemData p) => p.ID == id);
		if (num2 >= 0)
		{
			PPIItemData value2 = PlayerData.EquipList.Items[num2];
			value2.Count += count;
			PlayerData.EquipList.Items[num2] = value2;
		}
		GameplayData.Instance.RecomputeItemModifiers();
	}

	public void InventoryRemoveItem(E_ItemID id)
	{
		InventoryList.Items.RemoveAll((PPIItemData ps) => ps.ID == id);
		EquipList.Items.RemoveAll((PPIItemData ps) => ps.ID == id);
		GameplayData.Instance.RecomputeItemModifiers();
	}

	public void AddTicket(int ticket)
	{
		PlayerData.Params.Tickets += ticket;
		if (OnTicketChanged != null)
		{
			OnTicketChanged();
		}
	}

	public void RemoveTicket(int ticket)
	{
		if (PlayerData.Params.Tickets < ticket)
		{
			Debug.LogError("Invalid gold operation: removing more then have");
			PlayerData.Params.Tickets = 0;
		}
		else
		{
			PlayerData.Params.Tickets -= ticket;
		}
		if (OnTicketChanged != null)
		{
			OnTicketChanged();
		}
	}

	public void AddGold(int gold)
	{
		if (gold != 0)
		{
			PlayerData.Params.Gold += gold;
			if (OnGoldChanged != null)
			{
				OnGoldChanged();
			}
			PlayerData.Params.TotalGold += gold;
		}
	}

	public void RemoveGold(int gold)
	{
		if (gold != 0)
		{
			if (PlayerData.Params.Gold < gold)
			{
				Debug.LogError("Invalid gold operation: removing more then have");
				PlayerData.Params.Gold = 0;
			}
			else
			{
				PlayerData.Params.Gold -= gold;
			}
			if (OnGoldChanged != null)
			{
				OnGoldChanged();
			}
		}
	}

	public void AddKill()
	{
		PlayerData.Params.Kills++;
		StatsDirty = true;
	}

	public void AddDeath()
	{
		PlayerData.Params.Deaths++;
		StatsDirty = true;
	}

	public void AddHeadShot()
	{
		PlayerData.Params.HeadShots++;
	}

	public void AddCarnage()
	{
		PlayerData.Params.Carnage++;
	}

	public void AddLimbOut()
	{
		PlayerData.Params.Limbs++;
	}

	public void AddItemUse(E_ItemID id)
	{
		int num = PlayerData.InventoryList.Items.FindIndex((PPIItemData p) => p.ID == id);
		if (num >= 0)
		{
			PPIItemData value = PlayerData.InventoryList.Items[num];
			value.StatsUseCount++;
			value.Count--;
			PlayerData.InventoryList.Items[num] = value;
			num = PlayerData.EquipList.Items.FindIndex((PPIItemData p) => p.ID == id);
			PPIItemData value2 = PlayerData.EquipList.Items[num];
			value2.Count--;
			PlayerData.EquipList.Items[num] = value2;
			StatsDirty = true;
		}
	}

	public void AddWeaponUse(E_WeaponID id)
	{
		int num = PlayerData.InventoryList.Weapons.FindIndex((PPIWeaponData p) => p.ID == id);
		if (num >= 0)
		{
			PPIWeaponData value = PlayerData.InventoryList.Weapons[num];
			value.StatsFire++;
			PlayerData.InventoryList.Weapons[num] = value;
			StatsDirty = true;
		}
	}

	public void AddWeaponHit(E_WeaponID id)
	{
		int num = PlayerData.InventoryList.Weapons.FindIndex((PPIWeaponData p) => p.ID == id);
		if (num >= 0)
		{
			PPIWeaponData value = PlayerData.InventoryList.Weapons[num];
			value.StatsHits++;
			PlayerData.InventoryList.Weapons[num] = value;
			StatsDirty = true;
		}
	}

	public void SynchronizePendingPPIChanges()
	{
	}

	public void SynchronizeToCloud()
	{
	}

	private void AsyncOpFinished(CloudServices.AsyncOpResult res)
	{
	}

	public void CopyPlayerData(PlayerPersistantInfo otherPPI)
	{
		PlayerData = otherPPI.PlayerData;
	}

	public void FetchPPIDataFromCloudAsyncOpFinished(CloudServices.AsyncOpResult res)
	{
		if (res.m_Res)
		{
			PlayerPersistantInfo playerPersistantInfo = new PlayerPersistantInfo();
			if (playerPersistantInfo.InitPlayerDataFromStr(res.m_ResultDesc))
			{
				PlayerData.Params.Experience = playerPersistantInfo.experience;
				PlayerData.Params.Money = playerPersistantInfo.money;
				PlayerData.Params.Gold = playerPersistantInfo.gold;
				PlayerData.EquipList = playerPersistantInfo.EquipList;
				IsCheater = false;
				IsValid = true;
				return;
			}
		}
		PlayerData.Params.Experience = 0;
		PlayerData.Params.Money = 0;
		PlayerData.Params.Gold = 0;
		Debug.LogWarning("FetchPPIDataFromCloudAsyncOpFinished(): error getting PPI from cloud for user : " + UserName);
	}

	private bool ValidatePPI(PlayerPersistantInfo refPPI)
	{
		string equipListAsJSON = refPPI.GetEquipListAsJSON();
		string equipListAsJSON2 = GetEquipListAsJSON();
		return equipListAsJSON == equipListAsJSON2;
	}

	public static int GetPlayerMaxExperienceForRank(int rank)
	{
		PlayerLevelData.PlayerLevelInfo playerLevelInfo = ((rank < GameplayData.Instance.playerLevelData.GetRankCount()) ? GameplayData.Instance.playerLevelData.GetInfo(rank + 1) : GameplayData.Instance.playerLevelData.GetInfo(rank));
		return playerLevelInfo.playerXp - 1;
	}

	public static int GetPlayerMinExperienceForRank(int rank)
	{
		PlayerLevelData.PlayerLevelInfo info = GameplayData.Instance.playerLevelData.GetInfo(rank);
		return info.playerXp;
	}

	public static int GetPlayerRankFromExperience(int experience)
	{
		PlayerLevelData.PlayerLevelInfo infoByPlayerXp = GameplayData.Instance.playerLevelData.GetInfoByPlayerXp(experience);
		return infoByPlayerXp.playerLevel;
	}

	private void SetDefaultPPI()
	{
		Name = "PlayerName";
		PlayerData.Params.GoldForGoldMission = 1;
		PlayerData.Params.Tickets = 1;
		Upgrades.Upgrades.Clear();
		Upgrades.Upgrades.Add(new PPIUpgradeList.UpgradeData
		{
			ID = E_UpgradeID.ItemSlot1
		});
		Upgrades.Upgrades.Add(new PPIUpgradeList.UpgradeData());
		InventoryList.Weapons.Clear();
		InventoryList.Weapons.Add(new PPIWeaponData
		{
			ID = E_WeaponID.Scorpion
		});
		InventoryList.Items.Clear();
		InventoryList.Items.Add(new PPIItemData
		{
			ID = E_ItemID.Bandage,
			Count = 1
		});
		EquipList.Weapons.Clear();
		EquipList.Weapons.Add(InventoryList.Weapons[0]);
		EquipList.Items.Clear();
		EquipList.Items.Add(InventoryList.Items[0]);
	}

	public static PlayerPersistantInfo GetDefaultPPI()
	{
		PlayerPersistantInfo playerPersistantInfo = new PlayerPersistantInfo();
		playerPersistantInfo.SetDefaultPPI();
		return playerPersistantInfo;
	}

	public void Save()
	{
		IDataFile dataFile = new DataFileJSON(string.Empty);
		dataFile.SetString("PPI", GetPlayerDataAsJsonStr());
		dataFile.SetString("DEVID", SysUtils.GetUniqueDeviceID());
		GameSaveLoadUtl.SavePlayerData(dataFile);
	}

	public void EditorDataOverride()
	{
	}

	public void Load(IDataFile data)
	{
		string @string = data.GetString("PPI", string.Empty);
		if (@string.Length == 0)
		{
			EditorDataOverride();
			return;
		}
		InitPlayerDataFromStr(@string);
		EditorDataOverride();
		if (PlayerData.Params.GoldForGoldMission == 0)
		{
			PlayerData.Params.GoldForGoldMission = 1;
		}
		GameplayData.Instance.RecomputeItemModifiers();
	}

	public void FetchPPIFromCloud()
	{
	}

	private void GetCloudPPI()
	{
	}

	private IEnumerator GetCloudPPI_Coroutine()
	{
		yield break;
	}

	public void SetPPIFromCloud(PlayerPersistantInfo inCloudPPI)
	{
	}

	private void OnPPIFromCloudUpdated(PlayerPersistantInfo ppi)
	{
		if (GeneratePPIChangeNotifications)
		{
			GeneratePPIChangeNotifications = false;
		}
	}

	public bool IsBonusAvailable()
	{
		DateTime dateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
		DateTime dateTime2 = new DateTime(PlayerData.Params.LastBonusTime.Year, PlayerData.Params.LastBonusTime.Month, PlayerData.Params.LastBonusTime.Day);
		TimeSpan timeSpan = dateTime - dateTime2;
		if (timeSpan.Days >= 2)
		{
			PlayerData.Params.GoldForGoldMission = 1;
		}
		if (timeSpan.Days >= 1)
		{
			return true;
		}
		return false;
	}

	public void SetBonusReceived()
	{
		PlayerData.Params.LastBonusTime = DateTime.Today;
	}

	public void Fixator_ResetBonusTime()
	{
		PlayerData.Params.LastBonusTime = DateTime.Today.AddDays(-1.0);
	}

	public void BonusMissionPlayed()
	{
		if (PlayerData.Params.GoldForGoldMission < 5)
		{
			PlayerData.Params.GoldForGoldMission++;
		}
	}

	public void SetAccountNameifNotExist(string inAccountName)
	{
		if (string.IsNullOrEmpty(PlayerData.AccountName))
		{
			PlayerData.AccountName = inAccountName;
		}
	}
}
