using System;
using UnityEngine;
using static Unity.Mathematics.math;

[RequireComponent(typeof(AnimSetPlayer))]
[RequireComponent(typeof(ComponentWeaponsPlayer))]
[RequireComponent(typeof(ComponentSensors))]
[RequireComponent(typeof(CameraBehaviourHuman))]
[RequireComponent(typeof(AnimComponent))]
[RequireComponent(typeof(Animation))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AgentHuman))]
public class ComponentPlayer : MonoBehaviour
{
	private enum E_TouchCommandType
	{
		E_TC_UNKNOWN = 0,
		E_TC_TAP = 1,
		E_TC_DOUBLE_TAP = 2,
		E_TC_LEFT_TO_RIGHT = 3,
		E_TC_RIGHT_TO_LEFT = 4,
		E_TC_UP = 5,
		E_TC_DOWN = 6,
		E_TC_TOUCH_START = 7,
		E_TC_MOVING = 8
	}

	[Serializable]
	public class SoundInfo
	{
		public AudioClip AmmoPicked;

		public AudioClip MoneyPicked;

		public AudioClip SpitHit;

		public AudioClip[] Hit;

		public AudioClip TimeWarpIn;

		public AudioClip TimeWarpOut;

		public AudioClip LowHealthLoop;

		public AudioClip[] WipeGob;

		public AudioClip[] CommentPickup;
	}

	public SoundInfo SoundSetup = new SoundInfo();

	public float HeightStand = 1.72f;

	public float HeightCover = 1.3f;

	private int Experience;

	private float LastInjuryTime;

	private float NextUpdateTime;

	private float SendStepEventTime;

	private bool UseMode;

	private int PickedUpMoney;

	public PlayerControlStates Controls = new PlayerControlStates();

	private GobEffect Gob;

	private E_ItemID ActivePowerup;

	private int KillsToDeactivatePowerup;

	public StaticWeaponController StatWpnController;

	public AgentHuman Owner { get; private set; }

	public bool InUseMode
	{
		get
		{
			return UseMode;
		}
	}

	public AudioClip HitSound
	{
		get
		{
			if (SoundSetup.Hit.Length == 0)
			{
				return null;
			}
			return SoundSetup.Hit[UnityEngine.Random.Range(0, SoundSetup.Hit.Length)];
		}
	}

	public AudioClip WipeGobSound
	{
		get
		{
			if (SoundSetup.WipeGob.Length == 0)
			{
				return null;
			}
			return SoundSetup.WipeGob[UnityEngine.Random.Range(0, SoundSetup.WipeGob.Length)];
		}
	}

	public AudioClip CommentPickupSound
	{
		get
		{
			if (SoundSetup.CommentPickup.Length == 0)
			{
				return null;
			}
			return SoundSetup.CommentPickup[UnityEngine.Random.Range(0, SoundSetup.CommentPickup.Length)];
		}
	}

	private void Awake()
	{
		Owner = GetComponent<AgentHuman>();
		Gob = GetComponent<GobEffect>();
		Controls.Start();
		Owner.BlackBoard.AimAnimationsEnabled = true;
	}

	private void OnDestroy()
	{
		SoundSetup = null;
		UnityEngine.Object.Destroy(base.gameObject);
	}

	private void Start()
	{
		Owner.BlackBoard.IsPlayer = true;
		Owner.BlackBoard.Rage = 0f;
		Owner.BlackBoard.Dodge = 0f;
		Owner.BlackBoard.Fear = 0f;
		Owner.AddGOAPAction(E_GOAPAction.Goto);
		Owner.AddGOAPAction(E_GOAPAction.Move);
		Owner.AddGOAPAction(E_GOAPAction.Use);
		Owner.AddGOAPAction(E_GOAPAction.WeaponReload);
		Owner.AddGOAPAction(E_GOAPAction.WeaponChange);
		Owner.AddGOAPAction(E_GOAPAction.Contest);
		Owner.AddGOAPAction(E_GOAPAction.UseGadget);
		Owner.AddGOAPGoal(E_GOAPGoals.Move);
		Owner.AddGOAPGoal(E_GOAPGoals.UseWorldObject);
		Owner.AddGOAPGoal(E_GOAPGoals.WeaponReload);
		Owner.AddGOAPGoal(E_GOAPGoals.WeaponChange);
		Owner.AddGOAPGoal(E_GOAPGoals.Contest);
		Owner.AddGOAPGoal(E_GOAPGoals.UseGadget);
		Owner.InitializeGOAP();
		Controls.FireDownDelegate = ActionBeginFire;
		Controls.FireUpDelegate = ActionEndFire;
		Controls.UseDelegate = ActionUse;
		Controls.UseObjectDelegate = ActionUseObject;
		Controls.ReloadDelegate = ActionReload;
		Controls.ChangeWeaponDelegate = ActionChangeWeapon;
		Controls.UseGadgetDelegate = ActionUseGadget;
		Controls.TouchUpdateDelegate = ActionCleanGob;
		Controls.IronSightDelegate = ActionIronSight;
		BlackBoard blackBoard = Owner.BlackBoard;
		blackBoard.ActionHandler = (BlackBoard.AgentActionHandler)Delegate.Combine(blackBoard.ActionHandler, new BlackBoard.AgentActionHandler(HandleAction));
	}

	private void Activate(SpawnPoint spawn)
	{
		Player.Instance = this;
		// The port shipped a reset physics collision matrix (all layers collide),
		// letting the player stand on enemy hit-boxes and dead ragdolls (and walk
		// out of the map). Re-apply the intended player collision filtering.
		ObjectLayer.ApplyPlayerCollisionRules();
		BlackBoard blackBoard = Player.Instance.Owner.BlackBoard;
		blackBoard.ActionHandler = (BlackBoard.AgentActionHandler)Delegate.Combine(blackBoard.ActionHandler, new BlackBoard.AgentActionHandler(GuiHUD.Instance.HandleAction));
		Owner.CharacterController.center = new Vector3(0f, HeightStand * 0.5f + 0.08f, 0f);
		Owner.CharacterController.height = HeightStand;
		Controls.SwitchToCombatMode();
		Controls.EnableInput();
		GameCamera.Instance.SetAgent(Owner);
		NextUpdateTime = 0f;
		ComponentBody component = GetComponent<ComponentBody>();
		if ((bool)component)
		{
			component.EnableSmoothRotation(false);
		}
		Owner.BlackBoard.AutoHeal = Game.Instance.PlayerPersistentInfo.Upgrades.ContainsUpgrade(E_UpgradeID.AutohealKit);
		if (Game.Instance.PlayerPersistentInfo.Upgrades.Upgrades.Find((PPIUpgradeList.UpgradeData ps) => ps.ID == E_UpgradeID.Health1) != null)
		{
			Owner.BlackBoard.BaseSetup.MaxHealth += 20f;
		}
		if (Game.Instance.PlayerPersistentInfo.Upgrades.Upgrades.Find((PPIUpgradeList.UpgradeData ps) => ps.ID == E_UpgradeID.Health2) != null)
		{
			Owner.BlackBoard.BaseSetup.MaxHealth += 20f;
		}
		if (Game.Instance.PlayerPersistentInfo.Upgrades.Upgrades.Find((PPIUpgradeList.UpgradeData ps) => ps.ID == E_UpgradeID.HealthExclusive) != null)
		{
			Owner.BlackBoard.BaseSetup.MaxHealth += 20f;
		}
		if (Game.Instance.PlayerPersistentInfo.storyId == 1)
		{
			Owner.BlackBoard.Health = Owner.BlackBoard.BaseSetup.MaxHealth * 0.8f;
		}
		else
		{
			Owner.BlackBoard.Health = Owner.BlackBoard.BaseSetup.MaxHealth;
		}
		SpawnManager.Instance.PlayerActivated(Owner);
		ActivePowerup = E_ItemID.None;
	}

	private void Deactivate()
	{
		GameCamera.Instance.SetAgent(null);
		BlackBoard blackBoard = Player.Instance.Owner.BlackBoard;
		blackBoard.ActionHandler = (BlackBoard.AgentActionHandler)Delegate.Remove(blackBoard.ActionHandler, new BlackBoard.AgentActionHandler(GuiHUD.Instance.HandleAction));
		UseMode = false;
		ActivePowerup = E_ItemID.None;
		SpawnManager.Instance.PlayerDeactivated(Owner);
	}

	public void AddPowerup(E_ItemID powerup, int KillsToDeactivate)
	{
		Owner.GadgetsComponent.AddGadget(powerup);
		ActivePowerup = powerup;
		KillsToDeactivatePowerup = Game.Instance.MissionResultData.KilledZombies + KillsToDeactivate;
	}

	public E_ItemID GetActivePowerup()
	{
		return ActivePowerup;
	}

	public bool IsActivePowerupExpiring()
	{
		if (ActivePowerup == E_ItemID.None)
		{
			return false;
		}
		return Game.Instance.MissionResultData.KilledZombies + 3 >= KillsToDeactivatePowerup;
	}

	private void Update()
	{
		if (NextUpdateTime < Time.timeSinceLevelLoad)
		{
			SlowUpdate();
			NextUpdateTime = Time.timeSinceLevelLoad + 0.1f;
		}
		if (Owner.BlackBoard.Stop)
		{
			Controls.Update();
			return;
		}
		Controls.Update();
		if (!Owner.IsAlive || Camera.main == null || GameCamera.Instance == null)
		{
			return;
		}
		if (Controls.Use)
		{
			CreateOrderUse();
		}
		if (Controls.Move.Enabled && Controls.Move.Direction != Vector3.zero)
		{
			if (true)
			{
				Owner.BlackBoard.Desires.MoveDirection = Controls.Move.Direction;
				Owner.BlackBoard.Desires.MoveSpeedModifier = Controls.Move.Force;
				Owner.WorldState.SetWSProperty(E_PropKey.AtTargetPos, false);
			}
			else
			{
				Owner.BlackBoard.Desires.MoveDirection = Vector3.zero;
				Owner.BlackBoard.Desires.MoveSpeedModifier = 0f;
				Owner.WorldState.SetWSProperty(E_PropKey.AtTargetPos, true);
			}
		}
		else
		{
			Owner.BlackBoard.Desires.MoveDirection = Vector3.zero;
			Owner.BlackBoard.Desires.MoveSpeedModifier = 0f;
			Owner.WorldState.SetWSProperty(E_PropKey.AtTargetPos, true);
		}
		if (!(StatWpnController != null))
		{
			Vector3 vector = new Vector3(Controls.View.PitchAdd, Controls.View.YawAdd);
			if (Owner.BlackBoard.Desires.WeaponTriggerOn)
			{
				vector *= 0.7f;
			}
			if (Owner.BlackBoard.Desires.WeaponIronSight)
			{
				vector *= 0.5f;
			}
			Owner.BlackBoard.Desires.Rotation.eulerAngles += vector;
			ClipRotation();
			UpdateIdealFireDir();
			PickupManager.Instance.CollectPickups(this, 2.25f);
			if (ActivePowerup != 0 && KillsToDeactivatePowerup <= Game.Instance.MissionResultData.KilledZombies)
			{
				Owner.GadgetsComponent.RemoveGadget(ActivePowerup);
				ActivePowerup = E_ItemID.None;
			}
		}
	}

	private void SlowUpdate()
	{
		if (Mission.Instance == null)
		{
			return;
		}
		bool useMode = UseMode;
		if (UseMode != useMode)
		{
			if (UseMode)
			{
				Controls.SwitchToUseMode();
			}
			else
			{
				Controls.SwitchToCombatMode();
			}
		}
		if (SendStepEventTime < Time.timeSinceLevelLoad && (Owner.BlackBoard.MotionType == E_MotionType.Run || Owner.BlackBoard.MotionType == E_MotionType.Walk))
		{
			Mission.Instance.CurrentGameZone.SendFactToEnemies(Owner, Owner, E_EventTypes.EnemyStep, 12f, 1f, false);
			SendStepEventTime = Time.timeSinceLevelLoad + 1f;
		}
		if (Owner.BlackBoard.Health <= 30f)
		{
			if (SoundSetup.LowHealthLoop != null && !Owner.IsSoundPlaying(SoundSetup.LowHealthLoop) && Owner.IsSoundPlaying(null))
			{
				Owner.PlayLoopedSound(SoundSetup.LowHealthLoop, 0f, 10000f, 0.3f, 1f);
			}
		}
		else if (Owner.IsSoundPlaying(SoundSetup.LowHealthLoop))
		{
			Owner.StopLoopedSound();
		}
	}

	private void LateUpdate()
	{
		if (Owner.IsAlive && Owner.BlackBoard.AutoHeal && !(LastInjuryTime + Game.Instance.DontHealTime > Time.timeSinceLevelLoad) && Owner.BlackBoard.Health != Owner.BlackBoard.RealMaxHealth)
		{
			Owner.BlackBoard.Health = Mathf.Min(Owner.BlackBoard.Health + Game.Instance.HealingModificator * Time.deltaTime, Owner.BlackBoard.RealMaxHealth);
		}
	}

	public void LoadAllWeapon()
	{
	}

	public void UpdateUseModeHACK()
	{
		if (UseMode)
		{
			Controls.SwitchToUseMode();
		}
		else
		{
			Controls.SwitchToCombatMode();
		}
	}

	private void OnTriggerEnter(Collider other)
	{
	}

	public void HandleAction(AgentAction a)
	{
		if (a is AgentActionUseItem)
		{
			SetIronSight(false);
		}
		else if (a is AgentActionWeaponChange)
		{
			SetIronSight(false);
		}
		else if (a is AgentActionReload)
		{
			SetIronSight(false);
		}
		else if (a is AgentActionAttack && Owner.WeaponComponent.GetCurrentWeapon().ClipAmmo == 0 && Owner.WeaponComponent.GetCurrentWeapon().WeaponAmmo == 0)
		{
			SetIronSight(false);
		}
		else if (a is AgentActionInjury)
		{
			LastInjuryTime = Time.timeSinceLevelLoad;
			if ((a as AgentActionInjury).WeaponType == E_WeaponType.Vomit)
			{
				AudioClip spitHit = SoundSetup.SpitHit;
				Owner.SoundUniquePlay(spitHit, spitHit.length, "Vomit");
			}
			else
			{
				AudioClip hitSound = HitSound;
				Owner.SoundUniquePlay(hitSound, hitSound.length, "Hit");
			}
			Game.Instance.MissionResultData.HealthLost += (a as AgentActionInjury).Damage / Owner.BlackBoard.RealMaxHealth;
			SetIronSight(false);
		}
		else if (a is AgentActionDeath)
		{
			Game.Instance.MissionResultData.HealthLost += (a as AgentActionDeath).Damage / Owner.BlackBoard.RealMaxHealth;
			Mission.Instance.EndOfMission(Mission.E_MissionResult.PlayerKilled);
			Game.Instance.PlayerPersistentInfo.AddDeath();
			SetIronSight(false);
		}
	}

	public void CreateOrderUse()
	{
		if ((bool)Owner.BlackBoard.Desires.InteractionObject)
		{
			Owner.WorldState.SetWSProperty(E_PropKey.UseWorldObject, true);
		}
	}

	public void HandleBadUse()
	{
	}

	public void StopMove(bool stop)
	{
		Owner.Stop(stop);
		if (stop)
		{
			Controls.DisableInput();
		}
		else
		{
			Controls.EnableInput();
		}
		Owner.WorldState.SetWSProperty(E_PropKey.AtTargetPos, true);
		Owner.WorldState.SetWSProperty(E_PropKey.KillTarget, false);
	}

	public void StopView(bool stop)
	{
		Owner.Stop(stop);
		if (stop)
		{
			Controls.DisableView();
		}
		else
		{
			Controls.EnableView();
		}
	}

	private void ActionIronSight()
	{
		if (Owner.CanFire())
		{
			SetIronSight(!Owner.BlackBoard.Desires.WeaponIronSight);
		}
	}

	private void ActionBeginFire()
	{
		if (!Owner.BlackBoard.Stop)
		{
			if (Owner.CanFire())
			{
				Owner.BlackBoard.Desires.WeaponTriggerOn = true;
			}
			Controls.Fire = true;
		}
	}

	private void ActionEndFire()
	{
		Controls.Fire = false;
		Owner.BlackBoard.Desires.WeaponTriggerOn = false;
	}

	private void ActionUse()
	{
		if (!Owner.BlackBoard.Stop && !Owner.IsActionPointOn)
		{
			Controls.Use = true;
			CreateOrderUse();
		}
	}

	private void ActionUseObject(InteractionObject obj)
	{
		if (!Owner.BlackBoard.Stop && !Owner.IsActionPointOn)
		{
			Controls.Use = true;
			Owner.BlackBoard.Desires.InteractionObject = obj;
			if ((bool)Owner.BlackBoard.Desires.InteractionObject)
			{
				Owner.WorldState.SetWSProperty(E_PropKey.UseWorldObject, true);
			}
		}
	}

	private void ActionCleanGob(Vector2 pos, Vector2 delta)
	{
		if (!Gob)
		{
			return;
		}
		Vector2 gobNormPos = new Vector2(pos.x / (float)Screen.width, 1f - pos.y / (float)Screen.height);
		Vector2 normDelta = new Vector2(delta.x / (float)Screen.width, delta.y / (float)Screen.height);
		if (Gob.CleanGob(gobNormPos, normDelta) && delta.sqrMagnitude > 5f)
		{
			AudioClip wipeGobSound = WipeGobSound;
			if (wipeGobSound != null)
			{
				Owner.SoundUniquePlay(wipeGobSound, wipeGobSound.length + 0.2f, "WipeGob");
			}
		}
	}

	private void ActionReload()
	{
		if (!Owner.BlackBoard.Stop && !Owner.IsActionPointOn && !Owner.WeaponComponent.GetCurrentWeapon().IsFullyLoaded)
		{
			Owner.WorldState.SetWSProperty(E_PropKey.WeaponLoaded, false);
		}
	}

	private void ActionUseGadget(E_ItemID id)
	{
		if (Mission.Instance.MissionIsEnding)
		{
			return;
		}
		Item gadget = Owner.GadgetsComponent.GetGadget(id);
		if (Owner.CanUseGadget(gadget))
		{
			if (gadget.Settings.ItemBehaviour == E_ItemBehaviour.Throw)
			{
				Owner.WorldState.SetWSProperty(E_PropKey.UseGadget, true);
				Owner.BlackBoard.Desires.Gadget = id;
			}
			else
			{
				gadget.Use(Owner.ChestPosition, Owner.Forward);
				Owner.SoundPlay(gadget.Settings.UseSound);
			}
		}
	}

	public bool CanChangeWeapon()
	{
		if (Owner.BlackBoard.Stop || Owner.IsActionPointOn || Owner.BlackBoard.BusyAction || Owner.WeaponComponent.GetCurrentWeapon().IsBusy())
		{
			return false;
		}
		return true;
	}

	private void ActionChangeWeapon(E_WeaponID weapon)
	{
		if (CanChangeWeapon())
		{
			Owner.BlackBoard.Desires.Weapon = weapon;
			Owner.WorldState.SetWSProperty(E_PropKey.WeaponChange, true);
		}
	}

	private void ClipRotation()
	{
		Vector3 eulerAngles = Owner.BlackBoard.Desires.Rotation.eulerAngles;
		if (eulerAngles.x >= 180f)
		{
			eulerAngles.x -= 360f;
		}
		if (eulerAngles.x < -60f)
		{
			eulerAngles.x = -60f;
		}
		else if (eulerAngles.x > 50f)
		{
			eulerAngles.x = 50f;
		}
		eulerAngles.z = 0f;
		Owner.BlackBoard.Desires.Rotation.eulerAngles = eulerAngles;
	}

	private void UpdateIdealFireDir()
	{
		Owner.BlackBoard.Desires.FireDirection = GameCamera.Instance.CameraForward;
	}

	public void PickUp(Pickup P)
	{
		Pickup_Money pickup_Money = P as Pickup_Money;
		if (pickup_Money != null)
		{
			if (PickedUpMoney == 0)
			{
				Invoke("ProcessAccumulatedPickedUpMoney", 0.5f);
			}
			PickedUpMoney += pickup_Money.m_Amount;
			return;
		}
		Pickup_Ammo pickup_Ammo = P as Pickup_Ammo;
		if (pickup_Ammo != null && Owner.WeaponComponent.AddAmmoToWeapon(pickup_Ammo.m_WeaponID, pickup_Ammo.m_Amount))
		{
			AudioClip ammoPicked = SoundSetup.AmmoPicked;
			Owner.SoundUniquePlay(ammoPicked, ammoPicked.length * 0.1f, "Ammo");
			if (GuiHUD.Instance != null)
			{
				GuiHUD.Instance.OnAmmoCollected(pickup_Ammo.m_WeaponID, pickup_Ammo.m_Amount);
			}
		}
	}

	private void ProcessAccumulatedPickedUpMoney()
	{
		MoneyPickedUp(PickedUpMoney, 3001060);
		PickedUpMoney = 0;
	}

	public void MoneyPickedUp(int Units, int MsgTextID)
	{
		if (Units <= 0)
		{
			return;
		}
		Game.Instance.PlayerPersistentInfo.AddMoneyBags(Units);
		if (MsgTextID > 0)
		{
			AudioClip moneyPicked = SoundSetup.MoneyPicked;
			Owner.SoundUniquePlay(moneyPicked, moneyPicked.length * 0.1f, "Money");
			string text = TextDatabase.instance[MsgTextID];
			if (!string.IsNullOrEmpty(text))
			{
				int num = GameplayData.Instance.MoneyPerZombie();
				text = text.Replace("%d", (Units * num).ToString());
				GuiHUD.Instance.ShowMessage(GuiHUD.E_MessageType.Information, text, false, 2f);
			}
		}
	}

	public void CommentPickup()
	{
		AudioClip commentPickupSound = CommentPickupSound;
		if ((bool)commentPickupSound)
		{
			Owner.SoundUniquePlay(commentPickupSound, commentPickupSound.length + 1f, "PickupDrop");
		}
	}

	public void SetIronSight(bool on)
	{
		if (Owner.WeaponComponent.GetCurrentWeapon().CanAim())
		{
			Owner.BlackBoard.Desires.WeaponIronSight = on;
			if (on)
			{
				AimToBestEnemy();
				GameCamera.Instance.SetFov(Owner.WeaponComponent.GetCurrentWeapon().GetIronSightFov(), 150f, 0.02f);
			}
			else
			{
				GameCamera.Instance.SetDefaultFov(150f);
			}
		}
	}

	private void AimToBestEnemy()
	{
		float maxRange = Owner.WeaponComponent.GetCurrentWeapon().MaxRange;
		float num = 0.94f;
		Vector3 lookRotation = Vector3.zero;
		AgentHuman agentHuman = null;
		foreach (Agent enemy in Mission.Instance.CurrentGameZone.Enemies)
		{
			if (!(enemy is AgentHuman))
			{
				continue;
			}
			Vector3 vector = enemy.Position - Owner.Position;
			float magnitude = length(vector);
			if (magnitude > maxRange)
			{
				continue;
			}
			float num2 = Vector3.Dot(vector.normalized, Owner.Forward);
			if (!(num2 < num))
			{
				int layerMask = ~(ObjectLayerMask.IgnoreRaycast | ObjectLayerMask.Player | ObjectLayerMask.Enemy | ObjectLayerMask.EnemyBox);
				RaycastHit hitInfo;
				if ((!Physics.Raycast(Owner.EyePosition, vector, out hitInfo, magnitude, layerMask)) ? true : false)
				{
					lookRotation = vector;
					num = num2;
					agentHuman = enemy as AgentHuman;
				}
			}
		}
		if (agentHuman != null)
		{
			if (agentHuman.BlackBoard.MotionType == E_MotionType.Crawl)
			{
				Owner.BlackBoard.Desires.Rotation.SetLookRotation((agentHuman.TransformTarget.position - Owner.TransformEye.position).normalized);
			}
			else
			{
				Owner.BlackBoard.Desires.Rotation.SetLookRotation(lookRotation);
			}
		}
	}
}
