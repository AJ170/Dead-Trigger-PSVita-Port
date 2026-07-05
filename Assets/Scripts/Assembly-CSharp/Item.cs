using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item
{
	public class InitData
	{
		public AgentHuman Owner;

		public Vector3 Pos;

		public Vector3 Dir;
	}

	protected AgentHuman Owner;

	private InitData DataForSpawnedObject = new InitData();

	private GameObject SpawnedGameObject;

	private static Quaternion Temp;

	public ItemSettings Settings { get; private set; }

	public int Count { get; private set; }

	public float Timer { get; private set; }

	public bool Active { get; private set; }

	public Item(AgentHuman owner, E_ItemID itemID, int count)
	{
		Owner = owner;
		Settings = ItemSettingsManager.Instance.Get(itemID);
		Timer = Settings.Timer;
		Count = count;
		DataForSpawnedObject.Owner = Owner;
		switch (Settings.ItemBehaviour)
		{
		case E_ItemBehaviour.IncreaseDamage:
		{
			AgentHuman owner5 = Owner;
			owner5.ModifierDamage = (Agent.ModifierFloat)Delegate.Combine(owner5.ModifierDamage, new Agent.ModifierFloat(ModifierDamage));
			break;
		}
		case E_ItemBehaviour.IncreaseMoney:
		{
			AgentHuman owner4 = Owner;
			owner4.ModifierMoney = (Agent.ModifierInt)Delegate.Combine(owner4.ModifierMoney, new Agent.ModifierInt(ModifierMoney));
			break;
		}
		case E_ItemBehaviour.IncreaseXp:
		{
			AgentHuman owner3 = Owner;
			owner3.ModifierExperience = (Agent.ModifierInt)Delegate.Combine(owner3.ModifierExperience, new Agent.ModifierInt(ModifierExperience));
			break;
		}
		case E_ItemBehaviour.IncreaseSpeed:
		{
			AgentHuman owner2 = Owner;
			owner2.ModifierSpeed = (Agent.ModifierFloat)Delegate.Combine(owner2.ModifierSpeed, new Agent.ModifierFloat(ModifierSpeed));
			break;
		}
		}
	}

	public void Destroy()
	{
		switch (Settings.ItemBehaviour)
		{
		case E_ItemBehaviour.IncreaseDamage:
		{
			AgentHuman owner4 = Owner;
			owner4.ModifierDamage = (Agent.ModifierFloat)Delegate.Remove(owner4.ModifierDamage, new Agent.ModifierFloat(ModifierDamage));
			break;
		}
		case E_ItemBehaviour.IncreaseMoney:
		{
			AgentHuman owner3 = Owner;
			owner3.ModifierMoney = (Agent.ModifierInt)Delegate.Remove(owner3.ModifierMoney, new Agent.ModifierInt(ModifierMoney));
			break;
		}
		case E_ItemBehaviour.IncreaseXp:
		{
			AgentHuman owner2 = Owner;
			owner2.ModifierExperience = (Agent.ModifierInt)Delegate.Remove(owner2.ModifierExperience, new Agent.ModifierInt(ModifierExperience));
			break;
		}
		case E_ItemBehaviour.IncreaseSpeed:
		{
			AgentHuman owner = Owner;
			owner.ModifierSpeed = (Agent.ModifierFloat)Delegate.Remove(owner.ModifierSpeed, new Agent.ModifierFloat(ModifierSpeed));
			break;
		}
		}
		Owner = null;
		Settings = null;
		SpawnedGameObject = null;
	}

	public bool IsAvailableForUse()
	{
		switch (Settings.ItemBehaviour)
		{
		case E_ItemBehaviour.Throw:
		case E_ItemBehaviour.Place:
		case E_ItemBehaviour.ReviveAndKillAll:
			if (Count == 0)
			{
				return false;
			}
			break;
		case E_ItemBehaviour.Heal:
			if (Count == 0 || Owner.IsFullyHealed || !Game.Instance.CanHeal)
			{
				return false;
			}
			break;
		}
		return Timer == Settings.Timer;
	}

	public void Use(Vector3 pos, Vector3 dir)
	{
		if (Settings.ItemUse != 0)
		{
			Timer = 0f;
			switch (Settings.ItemBehaviour)
			{
			case E_ItemBehaviour.Throw:
				ThrowObject(pos, dir);
				return;
			case E_ItemBehaviour.Place:
				PlaceObject(pos, dir);
				return;
			case E_ItemBehaviour.Heal:
				Heal();
				return;
			case E_ItemBehaviour.SloMotion:
				SloMotion();
				return;
			}
			throw new Exception(string.Concat("Item (", Settings.ID, ") : Unknown behaviour", Settings.ItemBehaviour));
		}
	}

	public void Update()
	{
		if (!Active && Timer < Settings.Timer)
		{
			Timer = Mathf.Min(Timer + Settings.RechargeModificator * Time.deltaTime, Settings.Timer);
		}
	}

	private void PlaceObject(Vector3 pos, Vector3 dir)
	{
		if (Count == 0)
		{
			return;
		}
		Vector3 vector = pos + 0.2f * dir;
		Vector3 vector2 = pos + dir - 2f * Vector3.up;
		Vector3 direction = Vector3.Normalize(vector2 - vector);
		RaycastHit[] array = Physics.SphereCastAll(vector, 0.16f, direction, 2f);
		if (array == null || array.Length == 0)
		{
			return;
		}
		if (array.Length > 1)
		{
			Array.Sort(array, CollisionUtils.CompareHits);
		}
		GameObject gameObject = null;
		RaycastHit[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			RaycastHit hit = array2[i];
			if (!hit.collider.isTrigger)
			{
				if (IsValidForPlacement(hit))
				{
					DataForSpawnedObject.Pos = hit.point;
					DataForSpawnedObject.Dir = dir;
					gameObject = UnityEngine.Object.Instantiate(Settings.SpawnObject) as GameObject;
					gameObject._SetActiveRecursively(true);
					gameObject.BroadcastMessage("Init", DataForSpawnedObject, SendMessageOptions.RequireReceiver);
					Game.Instance.MissionResultData.ItemUsed(Settings.ID);
					Game.Instance.PlayerPersistentInfo.AddItemUse(Settings.ID);
					Count--;
					Timer = 0f;

					break;
				}
			}
		}
	}

	private bool IsValidForPlacement(RaycastHit hit)
	{

		if (hit.rigidbody != null)
		{
			return false;
		}
		if (Vector3.Dot(hit.normal, Vector3.up) < 0.86f)
		{
			return false;
		}
		Transform transform = hit.transform;
		Transform parent = hit.transform.parent;
		if (transform.GetComponent<ExplodableObject>() != null)
		{
			return false;
		}
		if (parent != null)
		{
			if (parent.GetComponent<DestructibleObject>() != null)
			{
				return false;
			}
		} else
        {
			return false; //We have no parent object
        }
		return true;
	}

	public void AddToHand(Transform transform)
	{
		SpawnedGameObject = UnityEngine.Object.Instantiate(Settings.SpawnObject) as GameObject;
		SpawnedGameObject._SetActiveRecursively(true);
		Transform transform2 = SpawnedGameObject.transform;
		transform2.parent = transform;
		transform2.localPosition = Vector3.zero;
		transform2.rotation = Quaternion.identity;
	}

	public void RemoveFromHand()
	{
		SpawnedGameObject.transform.parent = null;
	}

	private void ThrowObject(Vector3 pos, Vector3 dir)
	{
		if (Count != 0 && !(SpawnedGameObject == null))
		{
			Game.Instance.MissionResultData.ItemUsed(Settings.ID);
			Game.Instance.PlayerPersistentInfo.AddItemUse(Settings.ID);
			Temp.SetLookRotation(dir);
			Temp.eulerAngles = new Vector3(Temp.eulerAngles.x - 25f, Temp.eulerAngles.y, 0f);
			dir = Temp * Vector3.forward;
			DataForSpawnedObject.Pos = pos;
			DataForSpawnedObject.Dir = dir;
			SpawnedGameObject.transform.parent = null;
			SpawnedGameObject.SendMessage("Init", DataForSpawnedObject, SendMessageOptions.RequireReceiver);
			Count--;
			Timer = 0f;
			SpawnedGameObject = null;
		}
	}

	public void SloMotion()
	{
		if (Count != 0)
		{
			Game.Instance.MissionResultData.ItemUsed(Settings.ID);
			Game.Instance.PlayerPersistentInfo.AddItemUse(Settings.ID);
			TimeManager.Instance.SetTimeScale(0.14f, 0.2f, 0.5f, 10f, true);
			Count--;
			Timer = 0f;
		}
	}

	public void ReviveAndKill()
	{
		if (Count != 0)
		{
			Game.Instance.MissionResultData.ItemUsed(Settings.ID);
			Game.Instance.PlayerPersistentInfo.AddItemUse(Settings.ID);
			Owner.StartCoroutine(_ReviveAndKill());
			Count--;
			Timer = 0f;
		}
	}

	private IEnumerator _ReviveAndKill()
	{
		Owner.BlackBoard.Invulnerable = true;
		TimeManager.Instance.SetTimeScale(0.14f, 0.5f, 0.4f, 2f, true);
		yield return new WaitForSeconds(0.1f);
		Owner.Heal(Settings.HealHP);
		yield return new WaitForSeconds(0.1f);
		List<Agent> agents = new List<Agent>();
		agents.AddRange(Mission.Instance.CurrentGameZone.Enemies);
		foreach (AgentHuman agent in agents)
		{
			if (agent.IsAlive)
			{
				Vector3 dir = agent.Position - Owner.Position;
				if (!(dir.magnitude > 8f))
				{
					agent.TakeDamage(Owner, 5000f, null, dir.normalized * 10f, E_WeaponID.None, E_WeaponType.ReviveKit);
					BloodFXManager.Instance.SpawnBloodSplashes(5u);
					yield return new WaitForSeconds(UnityEngine.Random.Range(0.01f, 0.05f));
				}
			}
		}
		Owner.BlackBoard.Invulnerable = false;
	}

	private void Heal()
	{
		if (Count != 0)
		{
			Game.Instance.MissionResultData.ItemUsed(Settings.ID);
			Game.Instance.PlayerPersistentInfo.AddItemUse(Settings.ID);
			Owner.Heal(Settings.HealHP);
			Count--;
			Timer = 0f;
		}
	}

	public int ModifierMoney(int money)
	{
		return money;
	}

	public int ModifierExperience(int exp)
	{
		return Mathf.CeilToInt((float)exp * Settings.PowerUpModifier);
	}

	public float ModifierDamage(float damage)
	{
		return damage * Settings.PowerUpModifier;
	}

	public float ModifierSpeed(float speed)
	{
		return speed * Settings.PowerUpModifier;
	}
}
