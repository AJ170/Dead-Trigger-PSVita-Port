using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugMenu : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void CurrencyHack()
	{
		if (Game.Instance.PlayerPersistentInfo == null)
			return;

		PlayerPersistantInfo ppi = Game.Instance.PlayerPersistentInfo;
		ppi.AddMoney(1000000);
		ppi.AddGold(100000);
		ppi.AddTicket(100);
	}

	 public void GiveWeaponLoadout(int loadout)
    {
        if (Game.Instance == null || Player.Instance == null || Player.Instance.Owner == null)
        {
            Debug.LogWarning("GiveWeaponLoadout: not in gameplay.");
            return;
        }

        E_WeaponID[] array = new E_WeaponID[4];
        switch (loadout)
        {
            case 1:
                array[0] = E_WeaponID.M4;
                array[1] = E_WeaponID.Colt1911;
                array[2] = E_WeaponID.Striker;
                array[3] = E_WeaponID.Minigun;
                break;
            case 2:
                array[0] = E_WeaponID.WaltherP99;
                array[1] = E_WeaponID.Scorpion;
                array[2] = E_WeaponID.P90;
                array[3] = E_WeaponID.Bren;
                break;
            case 3:
                array[0] = E_WeaponID.AK47;
                array[1] = E_WeaponID.KSG;
                array[2] = E_WeaponID.LeeEnfield303;
                array[3] = E_WeaponID.Uzi;
                break;
            case 4:
                array[0] = E_WeaponID.Lupara;
                array[1] = E_WeaponID.Remington870;
                array[2] = E_WeaponID.RemingtonTactics;
                array[3] = E_WeaponID.Scorpion;
                break;
            default:
                Debug.LogWarning("Unknown weapon loadout: " + loadout);
                return;
        }

        WeaponBase currentWeapon = Player.Instance.Owner.WeaponComponent.GetCurrentWeapon();
        if ((bool)currentWeapon)
        {
            currentWeapon.WeaponHide();
        }

        ComponentWeaponsPlayer component = Player.Instance.GetComponent<ComponentWeaponsPlayer>();
        component.Weapons.Clear();
        Game.Instance.PlayerPersistentInfo.InventoryList.Weapons.Clear();
        Game.Instance.PlayerPersistentInfo.EquipList.Weapons.Clear();

        foreach (E_WeaponID e_WeaponID in array)
        {
            PPIWeaponData item = default(PPIWeaponData);
            item.ID = e_WeaponID;
            item.UpgradeLevel = E_UpgradeLevel.Mk1;
            Game.Instance.PlayerPersistentInfo.InventoryList.Weapons.Add(item);
            Game.Instance.PlayerPersistentInfo.EquipList.Weapons.Add(item);
            WeaponBase weapon = WeaponManager.Instance.GetWeapon(e_WeaponID, E_UpgradeLevel.Mk1, 1f);
            component.Weapons.Add(e_WeaponID, weapon);
        }
        component.SendMessage("DbgInitialize");
    }
    
    public void KillAllEnemies()
    {
        if ((bool)Mission.Instance && (bool)Mission.Instance.CurrentGameZone && Player.Instance != null)
        {
            Mission.Instance.CurrentGameZone.KillAllEnemies(Player.Instance.Owner);
        }
    }

    public void ForceSave()
    {
        PlayerPersistantInfo ppi = Game.Instance.PlayerPersistentInfo;
        ppi.Save();
        
    }
    
    public void ForceMainMenu()
    {
        Game.Instance.LoadMainMenu(true);
    }
    
    public void RankUp()
    {
        PlayerPersistantInfo ppi = Game.Instance.PlayerPersistentInfo;

        int nextRank = ppi.rank + 1;
        if (nextRank > GameplayData.Instance.playerLevelData.GetRankCount())
        {
            Debug.Log("Already at max rank.");
            return;
        }
        int needed = PlayerPersistantInfo.GetPlayerMinExperienceForRank(nextRank) - ppi.experience;
        if (needed > 0)
        {
            ppi.AddExperience(needed);
        }
    }
}
