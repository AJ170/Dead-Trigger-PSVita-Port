using System.Collections.Generic;
using UnityEngine;

public class SpecialReward
{
	public enum Type
	{
		None = 0,
		Auto = 1,
		RockStock = 2,
		Chopper_Mission = 3,
		Scientist = 4,
		Bunker = 5,
		Bunker2 = 6,
		DailyRewardMission = 7,
		SecondMission = 8
	}

	public static CityGUIDialogs.SpecialRewardInfo GenerateSpecialReward(Type type)
	{
		if (type == Type.None)
		{
			return null;
		}
		int count = 1;
		List<E_ItemID> list = new List<E_ItemID>();
		CityGUIDialogs.SpecialRewardInfo specialRewardInfo = new CityGUIDialogs.SpecialRewardInfo();
		switch (type)
		{
		case Type.DailyRewardMission:
		{
			count = Game.Instance.PlayerPersistentInfo.numGoldForGoldMission;
			E_FundID key2 = E_FundID.Gold099;
			Settings<E_FundID> settings3 = FundSettingsManager.Instance.Get(key2);
			specialRewardInfo.Amount = "+ " + count;
			specialRewardInfo.Name = TextDatabase.instance[1010145];
			specialRewardInfo.Picture = settings3.ShopWidget;
			Game.Instance.PlayerPersistentInfo.AddGold(count);
			Game.Instance.PlayerPersistentInfo.Save();
			return specialRewardInfo;
		}
		case Type.Bunker:
		case Type.Bunker2:
		{
			count = ((type != Type.Bunker) ? PlayerPersistantInfo.GoldPerBunker2 : PlayerPersistantInfo.GoldPerBunker);
			E_FundID key = E_FundID.Gold099;
			Settings<E_FundID> settings2 = FundSettingsManager.Instance.Get(key);
			specialRewardInfo.Amount = "+ " + count;
			specialRewardInfo.Name = TextDatabase.instance[1010145];
			specialRewardInfo.Picture = settings2.ShopWidget;
			Game.Instance.PlayerPersistentInfo.AddGold(count);
			Game.Instance.PlayerPersistentInfo.Save();
			return specialRewardInfo;
		}
		case Type.Scientist:
			list.Add(E_ItemID.ReviveKit);
			list.Add(E_ItemID.SloMotion);
			list.Add(E_ItemID.GrenadeBait);
			count = 1;
			break;
		case Type.RockStock:
			list.Add(E_ItemID.LaserTurret);
			list.Add(E_ItemID.MiniMortar);
			list.Add(E_ItemID.CutterLaser);
			count = 1;
			break;
		case Type.SecondMission:
			list.Add(E_ItemID.Turret);
			count = 1;
			break;
		default:
		{
			ItemSettings[] all = ItemSettingsManager.Instance.GetAll();
			bool flag = Game.Instance.PlayerPersistentInfo.Upgrades.ContainsUpgrade(E_UpgradeID.AutohealKit);
			ItemSettings[] array = all;
			foreach (Settings<E_ItemID> settings in array)
			{
				if (!settings.DISABLED && Game.Instance.PlayerPersistentInfo.rank + 5 >= settings.Rank && settings.ID != E_ItemID.BoosterDamage && settings.ID != E_ItemID.BoosterMoney && settings.ID != E_ItemID.BoosterSpeed && settings.ID != E_ItemID.BoosterXP && settings.ID != E_ItemID.BigHeads && settings.ID != E_ItemID.LaserTurret && settings.ID != E_ItemID.MiniMortar && (!flag || settings.ID != E_ItemID.Bandage))
				{
					int num = Game.Instance.PlayerPersistentInfo.InventoryList.ContainsItem(settings.ID);
					list.Add(settings.ID);
					if (num < 2)
					{
						list.Add(settings.ID);
					}
				}
			}
			if (list.Count == 0)
			{
				Debug.LogWarning("Can't find any item for special reward!");
				return null;
			}
			break;
		}
		}
		if (list.Count == 0)
		{
			Debug.LogWarning("Can't find any item for special reward!");
			return null;
		}
		E_ItemID e_ItemID = list[Random.Range(0, list.Count)];
		if (type != Type.Scientist && type != Type.RockStock)
		{
			switch (e_ItemID)
			{
			case E_ItemID.Bait:
				count = 1;
				break;
			case E_ItemID.Bandage:
				count = 3;
				break;
			case E_ItemID.CutterLaser:
				count = 1;
				break;
			case E_ItemID.CutterMechanical:
				count = 1;
				break;
			case E_ItemID.GrenadeBait:
				count = 1;
				break;
			case E_ItemID.GrenadeFrag:
				count = 2;
				break;
			case E_ItemID.GrenadeSticky:
				count = 2;
				break;
			case E_ItemID.MedKit:
				count = 1;
				break;
			case E_ItemID.Mine:
				count = 2;
				break;
			case E_ItemID.SloMotion:
				count = 1;
				break;
			case E_ItemID.ReviveKit:
				count = 1;
				break;
			case E_ItemID.Turret:
				count = 1;
				break;
			}
		}
		Settings<E_ItemID> settings4 = ItemSettingsManager.Instance.Get(e_ItemID);
		specialRewardInfo.Amount = count + " x";
		specialRewardInfo.Name = TextDatabase.instance[settings4.Name];
		specialRewardInfo.Picture = settings4.ShopWidget;
		Game.Instance.PlayerPersistentInfo.InventoryAddItem(e_ItemID, count);
		if ((bool)GuiEquipMenu.Instance)
		{
			GuiEquipMenu.Instance.TryEquipBoughtItem(new ShopItemId((int)e_ItemID, GuiShop.E_ItemType.Item));
		}
		Game.Instance.PlayerPersistentInfo.Save();
		return specialRewardInfo;
	}
}
