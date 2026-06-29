#define DEBUG
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;

[Serializable]
public class AgentHuman : Agent
{
	[Serializable]
	public class ModData
	{
		public E_AgentType m_Type;

		public float m_Health;

		public float m_AttackDamage;

		public float m_MoveSpeedModif;

		public float m_AttackFreqModif;
	}

	[Serializable]
	public class SoundInfo
	{
		public AudioClip[] Steps = new AudioClip[0];

		public AudioClip[] StepsMetal = new AudioClip[0];

		public AudioClip[] Injuries = new AudioClip[0];

		public AudioClip[] InjuryCritical = new AudioClip[0];

		public AudioClip[] Deaths = new AudioClip[0];

		public AudioClip[] DeathsAlien = new AudioClip[0];

		public AudioClip[] Throw = new AudioClip[0];

		public AudioClip[] Place = new AudioClip[0];
	}

	private struct FadeoutOverrideDef
	{
		public bool enabled;

		public Color color;
	}

	public delegate void HealthChangedDelegate(AgentHuman Human, float HealthChange);

	static public float AgentUpdateTime = 0.1f;

	private FadeoutOverrideDef FadeoutOverride = default(FadeoutOverrideDef);

	public BlackBoard BlackBoard = new BlackBoard();

	public SoundInfo SoundSetup = new SoundInfo();

	private HashSet<string> SoundLocks = new HashSet<string>();

	public Material FadeoutMaterial;

	public Material DiffuseMaterial;

	public Transform TransformEye;

	public Transform TransformTarget;

	public GameObject Mask;

	public Transform TransformEyeOriginal;

	public Transform RagdollRoot;

	public Rigidbody RigidBodyForce;

	public float RigidBodyPushingForce = 2f;

	private Rigidbody[] RigidBodies;

	private float PrevHealth;

	private GameObject CollBox;

	private GameObject CollCapsule;

	private GameObject CollSphere;

	private Collider[] Colliders;

	private float TeleportTimer;

	[NonSerialized]
	public AgentHuman ContestEnemy;

	public bool debugGOAP;

	public bool debugAnims;

	public bool debugMemory;

	public bool debugAI;

	private AnimSet AnimationSet;

	private GameObject Shadow;

	private GOAPManager GoalManager;

	private Hashtable Actions = new Hashtable();

	private float FadeoutFXDuration = 2f;

	private float TimeToUpdateAgent;

	private float StepTime;

	private int KillExperience;

	public override bool IsPlayer
	{
		get
		{
			return base.AgentType == E_AgentType.Player;
		}
	}

	public bool IsBoss
	{
		get
		{
			return base.AgentType == E_AgentType.Boss1 || base.AgentType == E_AgentType.BossSanta;
		}
	}

	public override bool IsAlive
	{
		get
		{
			return BlackBoard.Health > 0f && base.GameObject.activeSelf;
		}
	}

	public override bool IsVisible
	{
		get
		{
			return Renderer.isVisible;
		}
	}

	public override bool IsInvulnerable
	{
		get
		{
			return BlackBoard.DamageSetup.Invulnerable || BlackBoard.Invulnerable;
		}
	}

	public bool IsActionPointOn
	{
		get
		{
			return BlackBoard.ActionPointOn;
		}
	}

	public bool IsFullyHealed
	{
		get
		{
			return BlackBoard.Health == BlackBoard.RealMaxHealth;
		}
	}

	public override Vector3 ChestPosition
	{
		get
		{
			return base.Transform.position + base.transform.up * 1.5f;
		}
	}

	public AnimSet AnimSet
	{
		get
		{
			return (!(AnimationSet != null)) ? GetComponent<AnimSet>() : AnimationSet;
		}
		private set
		{
			AnimationSet = value;
		}
	}

	public WorldState WorldState { get; private set; }

	public Memory Memory { get; private set; }

	public CharacterController CharacterController { get; private set; }

	public ComponentWeapons WeaponComponent { get; private set; }

	public ComponentGadgets GadgetsComponent { get; private set; }

	public ComponentSensors SensorsComponent { get; private set; }

	public ComponentEnemy EnemyComponent { get; private set; }

	public AnimComponent AnimComponent { get; private set; }

	public UnityEngine.AI.NavMeshAgent NavMeshAgent { get; private set; }

	public SkinnedMeshRenderer Renderer { get; private set; }

	public SkinnedMeshRenderer[] LodRenderers { get; private set; }

	public Vector3 EyePosition
	{
		get
		{
			return TransformEye.position;
		}
	}

	public AudioClip StepSound
	{
		get
		{
			if (SoundSetup.Steps.Length == 0)
			{
				return null;
			}
			return SoundSetup.Steps[UnityEngine.Random.Range(0, SoundSetup.Steps.Length)];
		}
	}

	public AudioClip StepMetalSound
	{
		get
		{
			if (SoundSetup.StepsMetal.Length == 0)
			{
				return null;
			}
			return SoundSetup.StepsMetal[UnityEngine.Random.Range(0, SoundSetup.StepsMetal.Length)];
		}
	}

	public AudioClip InjurySound
	{
		get
		{
			if (SoundSetup.Injuries.Length == 0)
			{
				return null;
			}
			return SoundSetup.Injuries[UnityEngine.Random.Range(0, SoundSetup.Injuries.Length)];
		}
	}

	public AudioClip InjuryCriticalSound
	{
		get
		{
			if (SoundSetup.InjuryCritical.Length == 0)
			{
				return null;
			}
			return SoundSetup.InjuryCritical[UnityEngine.Random.Range(0, SoundSetup.InjuryCritical.Length)];
		}
	}

	public AudioClip DeathSound
	{
		get
		{
			if (SoundSetup.Deaths.Length == 0)
			{
				return null;
			}
			return SoundSetup.Deaths[UnityEngine.Random.Range(0, SoundSetup.Deaths.Length)];
		}
	}

	public AudioClip DeathAlienSound
	{
		get
		{
			if (SoundSetup.DeathsAlien.Length == 0)
			{
				return null;
			}
			return SoundSetup.DeathsAlien[UnityEngine.Random.Range(0, SoundSetup.DeathsAlien.Length)];
		}
	}

	public AudioClip ThrowSound
	{
		get
		{
			if (SoundSetup.Throw.Length == 0)
			{
				return null;
			}
			return SoundSetup.Throw[UnityEngine.Random.Range(0, SoundSetup.Throw.Length)];
		}
	}

	public AudioClip PlaceSound
	{
		get
		{
			if (SoundSetup.Place.Length == 0)
			{
				return null;
			}
			return SoundSetup.Place[UnityEngine.Random.Range(0, SoundSetup.Place.Length)];
		}
	}

	public float MaxRunSpeed
	{
		get
		{
			return (ModifierSpeed == null) ? BlackBoard.BaseSetup.MaxRunSpeed : ModifierSpeed(BlackBoard.BaseSetup.MaxRunSpeed);
		}
	}

	public float MaxWalkSpeed
	{
		get
		{
			return (ModifierSpeed == null) ? BlackBoard.BaseSetup.MaxWalkSpeed : ModifierSpeed(BlackBoard.BaseSetup.MaxWalkSpeed);
		}
	}

	[method: MethodImpl(32)]
	public event HealthChangedDelegate OnHealthChanged;

	public GOAPAction GetAction(E_GOAPAction type)
	{
		return (GOAPAction)Actions[type];
	}

	public int GetNumberOfActions()
	{
		return Actions.Count;
	}

	public void ClearActions()
	{
		Actions.Clear();
	}

	private void Awake()
	{
		Initialize();
		Renderer = base.GameObject.GetComponentInChildren(typeof(SkinnedMeshRenderer)) as SkinnedMeshRenderer;
		LodRenderers = base.GameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
		if (TransformTarget != null)
		{
			if ((bool)Renderer)
			{
				Renderer.probeAnchor = TransformTarget;
			}
			int num = ((LodRenderers != null) ? LodRenderers.Length : 0);
			while (num-- > 0)
			{
				LodRenderers[num].probeAnchor = TransformTarget;
			}
		}
		CharacterController = base.Transform.GetComponent<CharacterController>();
		SensorsComponent = GetComponent<ComponentSensors>();
		WeaponComponent = GetComponent<ComponentWeapons>();
		GadgetsComponent = GetComponent<ComponentGadgets>();
		AnimComponent = GetComponent<AnimComponent>();
		EnemyComponent = GetComponent<ComponentEnemy>();
		NavMeshAgent = GetComponent<NavMeshAgent>();
		if (NavMeshAgent != null)
		{
			NavMeshAgent.updateRotation = false;
			NavMeshAgent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.HighQualityObstacleAvoidance;
		}
		BlackBoard.Owner = this;
		BlackBoard.GameObject = base.GameObject;
		AnimSet = GetComponent<AnimSet>();
		WorldState = new WorldState();
		Memory = new Memory(this);
		GoalManager = new GOAPManager(this);
		ResetAgent();
		BlackBoard.DontUpdate = true;
		TimeToUpdateAgent = 0f;
		Transform transform = base.Transform.FindChildByName("_Box");
		Transform transform2 = base.Transform.FindChildByName("_Capsule");
		Transform transform3 = base.Transform.FindChildByName("_Sphere");
		if ((bool)transform)
		{
			CollBox = transform.gameObject;
		}
		if ((bool)transform2)
		{
			CollCapsule = transform2.gameObject;
		}
		if ((bool)transform3)
		{
			CollSphere = transform3.gameObject;
		}
		Collider[] componentsInChildren = base.GameObject.GetComponentsInChildren<Collider>();
		List<Collider> list = new List<Collider>();
		Collider[] array = componentsInChildren;
		foreach (Collider collider in array)
		{
			if (collider.gameObject.layer == ObjectLayer.Enemy)
			{
				list.Add(collider);
			}
		}
		Colliders = list.ToArray();
		if ((bool)RagdollRoot)
		{
			RigidBodies = RagdollRoot.GetComponentsInChildren<Rigidbody>();
		}
	}

	private void OnDestroy()
	{
		SoundSetup = null;
		BlackBoard.Reset();
		BlackBoard = null;
	}

	private void Start()
	{
		BlackBoard blackBoard = BlackBoard;
		blackBoard.ActionHandler = (BlackBoard.AgentActionHandler)Delegate.Combine(blackBoard.ActionHandler, new BlackBoard.AgentActionHandler(HandleAction));
		CharacterShadow component = GetComponent<CharacterShadow>();
		if ((bool)component)
		{
			Shadow = component.ShadowPlaneGameObject;
		}
		//AgentUpdateTime = UnityEngine.Random.Range(0.1f, 0.2f);
	}

	private bool DestroyEnemyConfig()
	{
		AnimSet component = GetComponent<AnimSet>();
		EnemyConfig component2 = GetComponent<EnemyConfig>();
		if ((bool)component)
		{
			UnityEngine.Object.Destroy(component);
			AnimSet = null;
		}
		if ((bool)component2)
		{
			UnityEngine.Object.Destroy(component2);
		}
		ClearActions();
		GoalManager.Reset(true);
		BlackBoard.Desires.Reset();
		return true;
	}

	private bool CreateEnemyConfig(E_AgentType agentType)
	{
		DestroyEnemyConfig();
		base.AgentType = agentType;
		TeleportTimer = Time.timeSinceLevelLoad + 10f;
		EnemyConfig enemyConfig = null;
		switch (base.AgentType)
		{
		case E_AgentType.ZombieSlow1:
			enemyConfig = base.GameObject.AddComponent<EnemyConfigZombieSlow1>();
			break;
		case E_AgentType.ZombieFast1:
			enemyConfig = base.GameObject.AddComponent<EnemyConfigZombieFast1>();
			break;
		case E_AgentType.Vomitter1:
			enemyConfig = base.GameObject.AddComponent<EnemyConfigZombieVomit1>();
			break;
		case E_AgentType.Berserker1:
			enemyConfig = base.GameObject.AddComponent<EnemyConfigZombieBerserk1>();
			break;
		case E_AgentType.Walker1:
			enemyConfig = base.GameObject.AddComponent<EnemyConfigZombieWalker1>();
			break;
		case E_AgentType.Boss1:
			enemyConfig = base.GameObject.AddComponent<EnemyConfigZombieBoss1>();
			break;
		case E_AgentType.Boss1_small:
			enemyConfig = base.GameObject.AddComponent<EnemyConfigZombieBoss1small>();
			break;
		case E_AgentType.Swat:
			enemyConfig = base.GameObject.AddComponent<EnemyConfigZombieSwat>();
			break;
		case E_AgentType.Athlete:
			enemyConfig = base.GameObject.AddComponent<EnemyConfigZombieAthlete>();
			break;
		case E_AgentType.BossSanta:
			enemyConfig = base.GameObject.AddComponent<EnemyConfigZombieBossSanta>();
			break;
		default:
			Debug.Log(string.Concat("AgentHuman.CreateEnemyConfig() : UNSUPPORTED Agent Type=", base.AgentType, ", agent=", base.name));
			return false;
		}
		AnimSet = GetComponent<AnimSet>();
		return (enemyConfig != null) ? true : false;
	}

	private void ApplyModifications(ModData Data)
	{
		CreateEnemyConfig(Data.m_Type);
		BlackBoard.Health = Data.m_Health;
		BlackBoard.BaseSetup.MaxHealth = Data.m_Health;
		BlackBoard.AttackSetup.MeleeAttackDamage = Data.m_AttackDamage;
		BlackBoard.AttackSetup.VomitAttackDamage = Data.m_AttackDamage / 2f;
		float num = UnityEngine.Random.Range(0.9f, 1.1f);
		BlackBoard.BaseSetup.MaxWalkSpeed *= num;
		BlackBoard.BaseSetup.MaxRunSpeed *= num;
		Mission.Instance.CurrentGameZone.AddEnemy(this);
		EnemyMaskManager.Instance.Add(this);
		SetEyes(true);
	}

	public void SetEyes(bool show)
	{
		if (TransformEye == null)
		{
			return;
		}
		Animation component = TransformEye.gameObject.GetComponent<Animation>();
		if (!(component == null))
		{
			string text;
			switch (base.AgentType)
			{
			case E_AgentType.ZombieSlow1:
				text = "eye_green";
				break;
			case E_AgentType.ZombieFast1:
				text = "eye_orange";
				break;
			case E_AgentType.Berserker1:
				text = "eye_red";
				break;
			case E_AgentType.Vomitter1:
				text = "eye_orange";
				break;
			case E_AgentType.Boss1:
				text = "eye_red";
				break;
			case E_AgentType.Boss1_small:
				text = "eye_red";
				break;
			case E_AgentType.Walker1:
				text = "eye_green";
				break;
			case E_AgentType.Swat:
				text = "eye_red";
				break;
			case E_AgentType.Athlete:
				text = "eye_red";
				break;
			case E_AgentType.BossSanta:
				text = "eye_red";
				break;
			default:
				text = "eye_green";
				break;
			}
			if (!show)
			{
				text += "_off";
			}
			component.Play(text);
		}
	}

	private void Activate(SpawnPoint spawn)
	{
		EnableRagdoll(false);
		if (!IsPlayer)
		{
			SetFPVLayer(false);
			ResetAnimations();
		}
		BlackBoard.DontUpdate = false;
		RaycastHit hitInfo;
		if (!Physics.Raycast(spawn.Transform.position + Vector3.up, -Vector3.up, out hitInfo, 5f, 16384))
		{
			base.Transform.position = spawn.Transform.position;
		}
		else
		{
			base.Transform.position = hitInfo.point;
		}
		base.Transform.rotation = spawn.Transform.rotation;
		BlackBoard.Desires.Rotation = spawn.Transform.rotation;
		BlackBoard.Desires.FireDirection = base.Transform.forward;
		if (Vector3.Dot(Vector3.up, base.Transform.up) <= 0f)
		{
			Debug.LogWarning("Player is probably upside down! " + base.Transform.up);
		}
		StepTime = 0f;
		base.Audio.pitch = 1f;
		base.GameObject.SetActive(true);
		if ((bool)NavMeshAgent)
		{
			NavMeshAgent.enabled = true;
			NavMeshAgent.updatePosition = true;
			NavMeshAgent.updateRotation = false;
			NavMeshAgent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.HighQualityObstacleAvoidance;
		}
		if ((bool)Shadow)
		{
			Shadow._SetActiveRecursively(true);
		}
		ContestEnemy = null;
		PrevHealth = BlackBoard.Health;
		KillExperience = GameplayData.Instance.XpPerZombie();
		spawn.OnSpawn();
	}

	private void Deactivate()
	{
		EnableRagdoll(false);
		Reset();
	}

	private static readonly Vector3 s_RaycastOffset = new Vector3(0f, 0.1f, 0f);
	private static readonly Vector3 s_RaycastDown = Vector3.down;

	private void LateUpdate()
	{
		float health = BlackBoard.Health;
		float num = health - PrevHealth;
		if (num != 0f && this.OnHealthChanged != null)
			this.OnHealthChanged(this, num);
		PrevHealth = health;

		if (!IsAlive)
			return;

		// Cache time values used multiple times
		float timeSinceLevelLoad = Time.timeSinceLevelLoad;
		float deltaTime = Time.deltaTime;

		RaycastHit hitInfo;
		if ((BlackBoard.MotionType == E_MotionType.Run
			|| BlackBoard.MotionType == E_MotionType.Walk
			|| BlackBoard.MotionType == E_MotionType.AnimationDrive)
			&& StepTime < timeSinceLevelLoad
			&& Physics.Raycast(base.Position + s_RaycastOffset, s_RaycastDown, out hitInfo))
		{
			AudioClip clip;
			switch (hitInfo.transform.gameObject.layer)
			{
				case 28:
					clip = StepSound;
					break;
				case 29:
					clip = StepMetalSound;
					break;
				default:
					clip = StepSound;
					break;
			}

			if (BlackBoard.MotionType == E_MotionType.Run)
			{
				SoundPlay(clip);
				StepTime = timeSinceLevelLoad + UnityEngine.Random.Range(
					BlackBoard.StepsSetup.RunMinDelay,
					BlackBoard.StepsSetup.RunMaxDelay);
			}
			else if (BlackBoard.MotionType == E_MotionType.Walk)
			{
				SoundPlay(clip);
				StepTime = timeSinceLevelLoad + UnityEngine.Random.Range(
					BlackBoard.StepsSetup.WalkMinDelay,
					BlackBoard.StepsSetup.WalkMaxDelay);
			}
			else if (BlackBoard.MotionType == E_MotionType.AnimationDrive)
			{
				SoundPlay(clip);
				StepTime = timeSinceLevelLoad + UnityEngine.Random.Range(
					BlackBoard.StepsSetup.AnimMinDelay,
					BlackBoard.StepsSetup.AnimMaxDelay);
			}
		}

		BlackBoard.IdleTimer += deltaTime;

		if (TimeToUpdateAgent < timeSinceLevelLoad)
		{
			UpdateAgent();
			if (IsPlayer)
				return;
			TimeToUpdateAgent = timeSinceLevelLoad + AgentUpdateTime;
		}

		if (!IsPlayer)
		{
			float num2 = ((!(Time.timeScale >= 1f))
				? Mathf.Max(Time.timeScale, 0.5f)
				: 1f);
			if (deltaTime > float.Epsilon && base.Audio.pitch != num2)
				base.Audio.pitch = num2;
		}

		if (IsPlayer
			|| !(TeleportTimer <= timeSinceLevelLoad)
			|| !(BlackBoard.VisibleTarget != null)
			|| !(BlackBoard.DistanceToTarget > 15f)
			|| IsActionPointOn)
			return;

		float num3 = Vector3.Angle(
			BlackBoard.VisibleTarget.Forward,
			-BlackBoard.DirToTarget);

		if (num3 > 90f)
		{
			UnityEngine.AI.NavMeshHit hit;
			NavMeshAgent.SamplePathPosition(
				-1,
				BlackBoard.DistanceToTarget - 9f,
				out hit);

			Vector3 to = hit.position - BlackBoard.VisibleTarget.Position;
			float num4 = Vector3.Angle(BlackBoard.VisibleTarget.Forward, to);

			if (num4 > 90f)
			{
				Teleport(hit.position, base.Transform.rotation);
				TeleportTimer = timeSinceLevelLoad + 5f;
			}
			else
			{
				TeleportTimer = timeSinceLevelLoad + 1f;
			}
		}
		else
		{
			TeleportTimer = timeSinceLevelLoad + 2f;
		}
	}
	private void UpdateAgent()
	{
		if (!BlackBoard.DontUpdate)
		{
			BlackBoard.Update();
			if (!BlackBoard.BusyAction)
			{
				GoalManager.UpdateCurrentGoal();
				GoalManager.ManageGoals();
			}
			Memory.Update();
			BlackBoard.PostUpdate();
			WorldState.SetWSProperty(E_PropKey.Start, GoalManager.CurrentGoal == null);
		}
	}

	public void HandleAction(AgentAction a)
	{
	}

	public void PrepareForStart()
	{
	}

	public void Reset()
	{
		ResetAgent();
		ToggleCollisions(true, true);
		if (!IsPlayer)
		{
			SetFPVLayer(false);
		}
	}

	public void Stop(bool stop)
	{
		BlackBoard.Stop = stop;
	}

	private void ResetAgent()
	{
		if (NavMeshAgent != null)
		{
			NavMeshAgent.enabled = false;
		}
		StopAllCoroutines();
		GoalManager.Reset();
		BlackBoard.Reset();
		WorldState.Reset();
		WorldState.SetWSProperty(E_PropKey.Start, true);
		WorldState.SetWSProperty(E_PropKey.AtTargetPos, true);
		WorldState.SetWSProperty(E_PropKey.TargetNode, base.Position);
		WorldState.SetWSProperty(E_PropKey.KillTarget, false);
		WorldState.SetWSProperty(E_PropKey.LookingAtTarget, false);
		WorldState.SetWSProperty(E_PropKey.EnemyLookingAtMe, false);
		WorldState.SetWSProperty(E_PropKey.UseWorldObject, false);
		WorldState.SetWSProperty(E_PropKey.PlayAnim, false);
		WorldState.SetWSProperty(E_PropKey.InWeaponRange, false);
		WorldState.SetWSProperty(E_PropKey.InContestRange, false);
		WorldState.SetWSProperty(E_PropKey.InVomitRange, false);
		WorldState.SetWSProperty(E_PropKey.InDodge, false);
		WorldState.SetWSProperty(E_PropKey.WeaponChange, false);
		WorldState.SetWSProperty(E_PropKey.WeaponLoaded, false);
		WorldState.SetWSProperty(E_PropKey.SeeEnemy, false);
		WorldState.SetWSProperty(E_PropKey.AheadOfEnemy, false);
		WorldState.SetWSProperty(E_PropKey.EnemyAheadOfMe, false);
		WorldState.SetWSProperty(E_PropKey.Teleport, false);
		WorldState.SetWSProperty(E_PropKey.CoverState, E_CoverState.None);
		WorldState.SetWSProperty(E_PropKey.BodyPose, E_BodyPose.Stand);
		WorldState.SetWSProperty(E_PropKey.Event, E_EventTypes.None);
		WorldState.SetWSProperty(E_PropKey.DoSpawnAction, false);
		WorldState.SetWSProperty(E_PropKey.Patrol, false);
		WorldState.SetWSProperty(E_PropKey.CriticalInjury, false);
		WorldState.SetWSProperty(E_PropKey.CheckBait, false);
		WorldState.SetWSProperty(E_PropKey.DestroyObject, false);
		WorldState.SetWSProperty(E_PropKey.Contest, false);
		WorldState.SetWSProperty(E_PropKey.Berserk, false);
		ResetMaterial();
		ResetHitZones();
		Memory.Reset();
		BlackBoard.DontUpdate = true;
		ContestEnemy = null;
	}

	private void ResetHitZones()
	{
		HitZone[] array = UnityEngine.Object.FindObjectsOfType(typeof(HitZone)) as HitZone[];
		HitZone[] array2 = array;
		foreach (HitZone hitZone in array2)
		{
			hitZone.Reset();
		}
	}

	public GOAPPlan GetGOAPPlan()
	{
		return GoalManager.GetPlan();
	}

	public void AddGOAPAction(E_GOAPAction action)
	{
		Actions.Add(action, GOAPActionFactory.Create(action, this));
	}

	public void AddGOAPGoal(E_GOAPGoals goal)
	{
		GoalManager.AddGoal(goal);
	}

	public GOAPGoal GetGOAPGoal(E_GOAPGoals goal)
	{
		if (GoalManager == null)
		{
			return null;
		}
		return GoalManager.GetGoal(goal);
	}

	public void InitializeGOAP()
	{
		GoalManager.Initialize();
	}

	public bool IsFriend(AgentHuman friend)
	{
		if (friend == null)
		{
			return false;
		}
		return friend.IsPlayer == IsPlayer || !friend.IsPlayer == !IsPlayer;
	}

	public void OnProjectileHit(Projectile projectile)
	{
		OnProjectileHit(projectile, null, E_BodyPart.Body, false);
	}

	public void OnProjectileHit(Projectile projectile, HitZone hitZone = null, E_BodyPart bodyPart = E_BodyPart.Body, bool bodyPartDestroyed = false)
	{
		if (!IsAlive && projectile.Weapon != 0)
		{
			if (!IsPlayer)
			{
				GuiHUD.Instance.OnAgentHit(this, bodyPart, false);
			}
			return;
		}
		if (IsFriend(projectile.Agent))
		{
			projectile.ignoreThisHit = true;
			return;
		}
		if (projectile is ProjectileAlienGun)
		{
			FadeoutOverride.enabled = true;
			FadeoutOverride.color = (projectile as ProjectileAlienGun).FadeoutColor;
		}
		else
		{
			FadeoutOverride.enabled = false;
		}
		float num = ((projectile.ProjectileType == E_ProjectileType.Melee) ? 1f : ((!hitZone) ? 1f : hitZone.DamageModifier));
		float inDamage = projectile.Damage() * num;
		TakeDamage(projectile.Agent, inDamage, projectile.transform, projectile.Transform.forward * projectile.Impuls, projectile.Weapon, projectile.WeaponType, bodyPart, bodyPartDestroyed);
		if ((bool)BloodFXManager.Instance && IsPlayer)
		{
			BloodFXManager.Instance.SpawnBloodDrops((uint)UnityEngine.Random.Range(1, 4));
		}
		projectile.hitProcessed = true;
	}

	public void OnMeleeHit(HitZone zone, MeleeDamageData Data, E_BodyPart bodyPart = E_BodyPart.Body, bool bodyPartDestroyed = false)
	{
		if (IsAlive && !IsFriend(Data.Attacker as AgentHuman))
		{
			TakeDamage(Data.Attacker, Data.Damage, null, Data.Impulse, E_WeaponID.None, Data.WeaponType, bodyPart, bodyPartDestroyed);
			if ((bool)BloodFXManager.Instance && IsPlayer)
			{
				BloodFXManager.Instance.SpawnBloodDrops((uint)UnityEngine.Random.Range(1, 4));
			}
		}
	}

	public override void OnReceiveRangeDamage(Agent attacker, float damage, Vector3 impuls, E_WeaponID weaponID, E_WeaponType weaponType)
	{
		if (!IsAlive)
		{
			return;
		}
		AgentHuman friend = attacker as AgentHuman;
		if (IsFriend(friend))
		{
			return;
		}
		if (weaponID != E_WeaponID.VomitGreen)
		{
			TakeDamage(attacker, damage, null, impuls, weaponID, weaponType);
			if ((bool)BloodFXManager.Instance && IsPlayer)
			{
				BloodFXManager.Instance.SpawnBloodDrops((uint)UnityEngine.Random.Range(4, 6));
			}
		}
		else if (IsPlayer)
		{
			GobEffect component = GetComponent<GobEffect>();
			if ((bool)component)
			{
				component.SpawnGob(new Vector2(UnityEngine.Random.Range(0.2f, 0.8f), UnityEngine.Random.Range(0.2f, 0.8f)));
			}
		}
	}

	public void OnReceiveEnviromentDamage(float damage, Vector3 impuls)
	{
		if (IsAlive)
		{
			TakeDamage(null, damage, null, impuls, E_WeaponID.None, E_WeaponType.Unknown);
			if ((bool)BloodFXManager.Instance && IsPlayer)
			{
				BloodFXManager.Instance.SpawnBloodDrops((uint)UnityEngine.Random.Range(1, 4));
			}
		}
	}

	private E_Direction GetAttackDir(Transform transform)
	{
		Vector3 from = transform.position - base.Transform.position;
		float num = Vector3.Angle(from, base.Transform.forward);
		float num2 = Vector3.Angle(from, base.Transform.right);
		if (num < 45f)
		{
			if (num2 < 90f)
			{
				return E_Direction.ForwardRight;
			}
			return E_Direction.ForwardLeft;
		}
		if (num > 135f)
		{
			if (num2 < 90f)
			{
				return E_Direction.BackwardRight;
			}
			return E_Direction.BackwardLeft;
		}
		if (num2 < 90f)
		{
			return E_Direction.Right;
		}
		return E_Direction.Left;
	}

	public float GetNewHealth(float damage)
	{
		return (!IsInvulnerable) ? Mathf.Max(0f, BlackBoard.Health - damage) : BlackBoard.Health;
	}

	public void Heal(float hp)
	{
		BlackBoard.Health = Mathf.Min(BlackBoard.Health + hp, BlackBoard.BaseSetup.MaxHealth);
	}

	internal void TakeDamage(Agent inAttacker, float inDamage, Transform inHitTransform, Vector3 inImpuls, E_WeaponID inWeaponID, E_WeaponType type, E_BodyPart bodyPart = E_BodyPart.Body, bool bodyPartDestroyed = false)
	{
		if (Game.Instance.MissionResultData.Result == MissionResult.Type.SUCCESS || BlackBoard.Invulnerable)
		{
			return;
		}
		AgentHuman agentHuman = inAttacker as AgentHuman;
		if (ModifierDamage != null)
		{
			inDamage = ModifierDamage(inDamage);
		}
		if (inAttacker != null && inAttacker.ModifierDamage != null)
		{
			inDamage = inAttacker.ModifierDamage(inDamage);
		}
		BlackBoard.Health = GetNewHealth(inDamage);
		if (!IsPlayer)
		{
			SpawnManager.Instance.EnemyInjured(this, inDamage);
		}
		if (base.AgentType == E_AgentType.Boss1 || base.AgentType == E_AgentType.BossSanta)
		{
			DropPickups(E_DropEvent.OnHit, true, false);
		}
		if (IsAlive)
		{
			WorldState.SetWSProperty(E_PropKey.Event, E_EventTypes.EnemyInjuredMe);
			if (agentHuman != null)
			{
				AddFactToMemory(E_EventTypes.EnemyInjuredMe, agentHuman, 1f, UnityEngine.Random.Range(2.1f, 2.3f));
			}
			if (BlackBoard.ReactOnHits)
			{
				if (bodyPartDestroyed && (bodyPart == E_BodyPart.Head || bodyPart == E_BodyPart.Body))
				{
					AudioClip injuryCriticalSound = InjuryCriticalSound;
					if (injuryCriticalSound != null)
					{
						SoundUniquePlay(injuryCriticalSound, injuryCriticalSound.length, "InjuryCrit");
					}
				}
				else
				{
					AudioClip injurySound = InjurySound;
					if (injurySound != null)
					{
						SoundUniquePlay(injurySound, injurySound.length, "Injury");
					}
				}
				AgentActionInjury agentActionInjury = AgentActionFactory.Create(AgentActionFactory.E_Type.Injury) as AgentActionInjury;
				agentActionInjury.Attacker = agentHuman;
				agentActionInjury.Damage = inDamage;
				agentActionInjury.FromWeapon = inWeaponID;
				agentActionInjury.WeaponType = type;
				agentActionInjury.Impuls = inImpuls;
				agentActionInjury.BodyPart = bodyPart;
				agentActionInjury.PlayAnim = !bodyPartDestroyed || (bodyPart != E_BodyPart.LeftLeg && bodyPart != E_BodyPart.RightLeg);
				agentActionInjury.Destroy = bodyPartDestroyed;
				agentActionInjury.Direction = ((inAttacker != null) ? GetAttackDir(inAttacker.Transform) : E_Direction.Forward);
				BlackBoard.ActionAdd(agentActionInjury);
			}
			if (!IsPlayer && agentHuman != null && inWeaponID != 0)
			{
				if (bodyPartDestroyed)
				{
					agentHuman.CriticalHit(bodyPart, false);
				}
				if (inWeaponID != 0)
				{
					GuiHUD.Instance.OnAgentHit(this, bodyPart, bodyPartDestroyed);
				}
			}
			return;
		}
		BlackBoard.Desires.WeaponTriggerOn = false;
		if (IsPlayer)
		{
			Item gadgetAvailableWithBehaviour = GadgetsComponent.GetGadgetAvailableWithBehaviour(E_ItemBehaviour.ReviveAndKillAll);
			if (gadgetAvailableWithBehaviour != null)
			{
				gadgetAvailableWithBehaviour.ReviveAndKill();
				return;
			}
		}
		WorldState.SetWSProperty(E_PropKey.Event, E_EventTypes.Died);
		BlackBoard.DontUpdate = true;
		AgentActionDeath agentActionDeath = AgentActionFactory.Create(AgentActionFactory.E_Type.Death) as AgentActionDeath;
		agentActionDeath.Attacker = agentHuman;
		agentActionDeath.FromWeapon = inWeaponID;
		agentActionDeath.WeaponType = type;
		agentActionDeath.Impuls = inImpuls;
		agentActionDeath.BodyPart = bodyPart;
		agentActionDeath.Damage = inDamage;
		agentActionDeath.BodyDisintegrated = ((bodyPart == E_BodyPart.Body && bodyPartDestroyed) ? true : false);
		BlackBoard.ActionAdd(agentActionDeath);
		if (!IsPlayer && agentHuman != null)
		{
			agentHuman.KillEnemy();
			if (inWeaponID != 0)
			{
				if (bodyPartDestroyed)
				{
					agentHuman.CriticalHit(bodyPart, true);
				}
				else if (bodyPart == E_BodyPart.Head)
				{
					agentHuman.CriticalHit(bodyPart, true);
				}
				int num = 0;
				if (base.AgentType == E_AgentType.Boss1 || base.AgentType == E_AgentType.Boss1_small || base.AgentType == E_AgentType.Swat || base.AgentType == E_AgentType.BossSanta)
				{
					Game.Instance.MissionResultData.SpecialEnemies++;
					num += GameplayData.Instance.MoneyPerSpecialEnemy();
				}
				if (ModifierMoney != null)
				{
					num = ModifierMoney(num);
				}
				Game.Instance.PlayerPersistentInfo.AddRoundMoney(num);
				GuiHUD.Instance.OnAgentHit(this, bodyPart, bodyPartDestroyed);
			}
		}
		else if (IsPlayer && agentHuman != null)
		{
			Kontagent.SendCustomEvent(agentHuman.AgentType.ToString(), "Game", "Death", string.Empty, Game.Instance.PlayerPersistentInfo.rank);
			/*UnityAnalyticsWrapper.ReportCustomEvent("playerDeath", new Dictionary<string, object>
			{
				{
					"humanAttacker",
					agentHuman.AgentType.ToString()
				},
				{
					"playerRank",
					Game.Instance.PlayerPersistentInfo.rank
				}
			});*/
		}
		StopLoopedSound();
		if (bodyPartDestroyed)
		{
			AudioClip injuryCriticalSound2 = InjuryCriticalSound;
			if (injuryCriticalSound2 != null)
			{
				SoundUniquePlay(injuryCriticalSound2, injuryCriticalSound2.length, "Injury");
			}
		}
		if (inWeaponID != E_WeaponID.AlienGun)
		{
			SoundPlay(DeathSound);
		}
		else
		{
			SoundPlay(DeathAlienSound);
		}
	}

	private void KillEnemy()
	{
		int num = KillExperience;
		if (ModifierExperience != null)
		{
			num = ModifierExperience(num);
		}
		Game.Instance.MissionResultData.KilledZombies++;
		Game.Instance.PlayerPersistentInfo.AddExperience(num);
		Game.Instance.PlayerPersistentInfo.AddKill();
	}

	private void CriticalHit(E_BodyPart bodyPart, bool death)
	{
		int num = 0;
		switch (bodyPart)
		{
		case E_BodyPart.Body:
			num += GameplayData.Instance.MoneyPerCarnage();
			Game.Instance.MissionResultData.Disintegrations++;
			Game.Instance.PlayerPersistentInfo.AddCarnage();
			break;
		case E_BodyPart.Head:
			num += GameplayData.Instance.MoneyPerHead();
			Game.Instance.MissionResultData.HeadShots++;
			Game.Instance.PlayerPersistentInfo.AddHeadShot();
			break;
		case E_BodyPart.LeftArm:
		case E_BodyPart.RightArm:
		case E_BodyPart.LeftLeg:
		case E_BodyPart.RightLeg:
			num += GameplayData.Instance.MoneyPerLimb();
			Game.Instance.MissionResultData.RemovedLimbs++;
			Game.Instance.PlayerPersistentInfo.AddLimbOut();
			break;
		}
		if (ModifierMoney != null)
		{
			num = ModifierMoney(num);
		}
		Game.Instance.PlayerPersistentInfo.AddRoundMoney(num);
		if (death)
		{
			GuiHUD.Instance.OnAgentHit(this, bodyPart, true);
			if ((bool)ArenaDirector.Instance)
			{
				ArenaDirector.Instance.OnAgentHit(this, bodyPart, true);
			}
		}
		else
		{
			GuiHUD.Instance.OnAgentHit(this, bodyPart, false);
			if ((bool)ArenaDirector.Instance)
			{
				ArenaDirector.Instance.OnAgentHit(this, bodyPart, true);
			}
		}
	}

	public bool StartContest(AgentHuman starter)
	{
		ContestEnemy = starter;
		WorldState.SetWSProperty(E_PropKey.Contest, true);
		return true;
	}

	public bool StopContest(AgentHuman starter)
	{
		ContestEnemy = null;
		WorldState.SetWSProperty(E_PropKey.Contest, false);
		return true;
	}

	public bool CanDoContest(AgentHuman enemy, bool checkMyself)
	{
		if (EnemyComponent != null && !EnemyComponent.CanDoContest)
		{
			return false;
		}
		if (Mask != null)
		{
			return false;
		}
		if (WorldState.GetWSProperty(E_PropKey.BodyPose).GetBodyPose() != 0)
		{
			return false;
		}
		if (Game.Instance.PlayerPersistentInfo.EquipList.ContainsItem(E_ItemID.BigHeads) || ((bool)Player.Instance && Player.Instance.GetActivePowerup() == E_ItemID.BigHeads))
		{
			return false;
		}
		if (enemy == null || enemy.BlackBoard.BusyAction || (enemy.WeaponComponent.GetCurrentWeapon() != null && enemy.WeaponComponent.GetCurrentWeapon().IsBusy()))
		{
			return false;
		}
		return enemy.BlackBoard.ContestAllowNextTime <= Time.timeSinceLevelLoad && WorldState.GetWSProperty(E_PropKey.InContestRange).GetBool() && BlackBoard.ImportantObject == null && (!checkMyself || !IsInContest()) && !enemy.IsInContest();
	}

	public bool IsInContest()
	{
		return WorldState.GetWSProperty(E_PropKey.Contest).GetBool() && ContestEnemy != null;
	}

	public float PlayAnim(string animName, E_TriState invulnerable)
	{
		if (animName == null)
		{
			return 0f;
		}
		BlackBoard.Desires.Animation = animName;
		BlackBoard.Desires.Invulnerable = invulnerable;
		WorldState.SetWSProperty(E_PropKey.PlayAnim, true);
		return base.Animation[animName].length;
	}

	public void Teleport(Vector3 position, Quaternion rotation)
	{
		if ((bool)NavMeshAgent)
		{
			NavMeshAgent.Stop();
			NavMeshAgent.ResetPath();
			NavMeshAgent.enabled = false;
		}
		base.Transform.position = position;
		base.Transform.rotation = rotation;
		GoalManager.Reset();
		BlackBoard.MoveDir = Vector3.zero;
		BlackBoard.FireDir = base.Transform.forward;
		BlackBoard.Desires.Reset();
		BlackBoard.Desires.Rotation = rotation;
		BlackBoard.Desires.FireDirection = rotation * Vector3.forward;
		AnimComponent.OnTeleport();
		if ((bool)NavMeshAgent)
		{
			NavMeshAgent.enabled = true;
		}
	}

	public void Teleport(Transform destination)
	{
		if ((bool)NavMeshAgent)
		{
			NavMeshAgent.enabled = false;
		}
		GoalManager.Reset();
		BlackBoard.Desires.Rotation = destination.rotation;
		BlackBoard.Desires.FireDirection = destination.rotation * Vector3.forward;
		BlackBoard.Desires.WeaponTriggerOn = false;
		BlackBoard.MoveDir = Vector3.zero;
		BlackBoard.FireDir = base.Transform.forward;
		BlackBoard.BusyAction = false;
		BlackBoard.InteractionObject = null;
		BlackBoard.DangerousEnemy = null;
		BlackBoard.Invulnerable = false;
		BlackBoard.ReactOnHits = true;
		if ((bool)NavMeshAgent)
		{
			NavMeshAgent.enabled = true;
		}
		AnimComponent.OnTeleport();
		base.Transform.position = destination.position;
		base.Transform.rotation = destination.rotation;
	}

	public void Dissolve(float delay)
	{
		if (!IsPlayer)
		{
			StartCoroutine(Fadeout(delay));
		}
	}

	protected IEnumerator Fadeout(float delay)
	{
		SetEyes(false);
		if (EnemyMaskManager.Instance.Drop(this, MathUtils.RandomVectorInsideCone(Vector3.up, 140f)))
		{
			EnemyComponent.RemoveHead(true);
		}
		SpawnManager.Instance.EnemyDied(this);
		if (base.AgentType != E_AgentType.Boss1 || base.AgentType != E_AgentType.BossSanta)
		{
			DropPickups(E_DropEvent.OnDeath, true, true);
		}
		yield return new WaitForSeconds(delay);
		if (NavMeshAgent != null)
		{
			NavMeshAgent.enabled = false;
		}
		if ((bool)Shadow)
		{
			Shadow._SetActiveRecursively(false);
		}
		if (FadeoutMaterial != null)
		{
			SetFPVLayer(false);
			SetFadeoutMaterial(0f - Time.time, 0f, FadeoutFXDuration);
			yield return new WaitForSeconds(FadeoutFXDuration);
		}
		DebugUtils.Assert(!IsPlayer);
		SpawnManager.Instance.EnemyDespawned(this);
		Mission.Instance.ReturnAgentToCache(base.GameObject);
	}

	public void TeleportFadeOut()
	{
		SetFadeoutMaterial(0f - Time.time, 0f, 1f);
	}

	public void TeleportFadeIn()
	{
		StartCoroutine(FadeIn());
	}

	public void ShowModel(bool show)
	{
		if (Renderer != null)
		{
			Renderer.enabled = show;
		}
		if (LodRenderers != null)
		{
			SkinnedMeshRenderer[] lodRenderers = LodRenderers;
			foreach (Renderer renderer in lodRenderers)
			{
				renderer.enabled = show;
			}
		}
	}

	protected void SetMaterial(Material mtl)
	{
		if (LodRenderers != null)
		{
			SkinnedMeshRenderer[] lodRenderers = LodRenderers;
			foreach (Renderer renderer in lodRenderers)
			{
				renderer.material = mtl;
			}
		}
		if (Renderer != null)
		{
			Renderer.material = mtl;
		}
	}

	protected void SetFadeoutMaterial(float timeOfs, float invert, float duration)
	{
		if (LodRenderers != null)
		{
			SkinnedMeshRenderer[] lodRenderers = LodRenderers;
			foreach (Renderer renderer in lodRenderers)
			{
				renderer.material = FadeoutMaterial;
				renderer.material.SetFloat("_TimeOffs", timeOfs);
				renderer.material.SetFloat("_Invert", invert);
				renderer.material.SetFloat("_Duration", duration);
				if (FadeoutOverride.enabled)
				{
					renderer.material.SetColor("_FXColor", FadeoutOverride.color);
				}
			}
		}
		if (Renderer != null)
		{
			Renderer.material = FadeoutMaterial;
			Renderer.material.SetFloat("_TimeOffs", timeOfs);
			Renderer.material.SetFloat("_Invert", invert);
			Renderer.material.SetFloat("_Duration", duration);
			if (FadeoutOverride.enabled)
			{
				Renderer.material.SetColor("_FXColor", FadeoutOverride.color);
			}
		}
	}

	protected IEnumerator FadeIn()
	{
		SetFadeoutMaterial(0f - Time.time, 1f, 1f);
		yield return new WaitForSeconds(1f);
		SetMaterial(DiffuseMaterial);
	}

	public void ResetMaterial()
	{
		SetMaterial(DiffuseMaterial);
	}

	public bool SoundIsPlaying()
	{
		return base.Audio.isPlaying;
	}

	public void SoundStop()
	{
		if (base.Audio.isPlaying)
		{
			base.Audio.Stop();
		}
	}

	public float SoundPlay(AudioClip clip)
	{
		if ((bool)clip)
		{
			base.Audio.PlayOneShot(clip);
			return clip.length;
		}
		return 0f;
	}

	public float SoundUniquePlay(AudioClip Clip)
	{
		return SoundUniquePlay(Clip, Clip.length, Clip.name);
	}

	public float SoundUniquePlay(AudioClip Clip, float LockDuration)
	{
		return SoundUniquePlay(Clip, LockDuration, Clip.name);
	}

	public float SoundUniquePlay(AudioClip Clip, float LockDuration, string LockName)
	{
		if (Clip != null && !SoundLocks.Contains(LockName))
		{
			LockDuration = Mathf.Max(0f, LockDuration);
			StartCoroutine(_SoundUniquePlay(Clip, LockDuration, LockName));
			return Clip.length;
		}
		return 0f;
	}

	private IEnumerator _SoundUniquePlay(AudioClip Clip, float LockDuration, string LockName)
	{
		base.Audio.PlayOneShot(Clip);
		if (LockDuration > 0f)
		{
			SoundLocks.Add(LockName);
			yield return new WaitForSeconds(LockDuration);
			SoundLocks.Remove(LockName);
		}
	}

	public void PlayLoopedSound(AudioClip clip, float delay, float time, float fadeInTime, float fadeOutTime)
	{
		StartCoroutine(_PlayLoopedSound(clip, delay, time, fadeInTime, fadeOutTime));
	}

	private IEnumerator _PlayLoopedSound(AudioClip clip, float delay, float time, float fadeInTime, float fadeOutTime)
	{
		base.Audio.volume = 0f;
		base.Audio.loop = true;
		base.Audio.clip = clip;
		yield return new WaitForEndOfFrame();
		yield return new WaitForSeconds(delay);
		base.Audio.Play();
		float step2 = 1f / fadeInTime;
		while (base.Audio.volume < 1f)
		{
			base.Audio.volume = Mathf.Min(1f, base.Audio.volume + step2 * Time.deltaTime);
			yield return new WaitForEndOfFrame();
		}
		yield return new WaitForSeconds(time - fadeInTime - fadeOutTime);
		step2 = 1f / fadeOutTime;
		while (base.Audio.volume > 0f)
		{
			base.Audio.volume = Mathf.Max(0f, base.Audio.volume - step2 * Time.deltaTime);
			yield return new WaitForEndOfFrame();
		}
		base.Audio.Stop();
		yield return new WaitForEndOfFrame();
		base.Audio.volume = 1f;
	}

	public void StopLoopedSound()
	{
		if (base.Audio.isPlaying && base.Audio.loop)
		{
			base.Audio.Stop();
		}
	}

	public bool IsSoundPlaying(AudioClip clip)
	{
		return base.Audio.clip == clip;
	}

	public void ToggleCollisions(bool BlockPlayer, bool BlockRaycasts)
	{
		if ((bool)CollBox)
		{
			CollBox.SetActive(BlockRaycasts);
		}
		if ((bool)CollCapsule)
		{
			CollCapsule.SetActive(BlockPlayer);
		}
		if ((bool)CollSphere)
		{
			CollSphere.SetActive(BlockPlayer);
		}
		int layer = ObjectLayer.Enemy;
		bool flag = true;
		if (!BlockPlayer)
		{
			layer = ObjectLayer.IgnorePlayer;
			flag = BlockRaycasts;
		}
		if (Colliders != null)
		{
			Collider[] colliders = Colliders;
			foreach (Collider collider in colliders)
			{
				collider.enabled = flag;
				collider.gameObject.layer = layer;
			}
		}
		if ((bool)CharacterController)
		{
			CharacterController.enabled = BlockPlayer;
		}
	}

	public void SetFPVLayer(bool fpvOn)
	{
		if (!Renderer.material.HasProperty("_Params"))
		{
			return;
		}
		Vector4 vector = Renderer.material.GetVector("_Params");
		vector.y = (fpvOn ? 1 : 0);
		Renderer.material.SetVector("_Params", vector);
		if (LodRenderers != null)
		{
			SkinnedMeshRenderer[] lodRenderers = LodRenderers;
			foreach (Renderer renderer in lodRenderers)
			{
				renderer.material.SetVector("_Params", vector);
			}
		}
		if (Mask != null)
		{
			Mask.GetComponent<Renderer>().material.SetVector("_Params", vector);
		}
	}

	private void ResetAnimations()
	{
		foreach (AnimationState item in base.Animation)
		{
			item.speed = 1f;
		}
		BlackBoard.KeepMotion = false;
	}

	private void EnablePhysics(Rigidbody rb, bool enable)
	{
		rb.isKinematic = !enable;
		rb.useGravity = enable;
		CharacterJoint component = rb.gameObject.GetComponent<CharacterJoint>();
		if (!enable)
		{
			rb.Sleep();
			if ((bool)component)
			{
				component.connectedBody = null;
			}
		}
		else if ((bool)component && component.connectedBody == null)
		{
			Transform parent = rb.gameObject.transform.parent;
			Rigidbody component2 = parent.GetComponent<Rigidbody>();
			while (component2 == null && parent != null)
			{
				parent = parent.gameObject.transform.parent;
				component2 = parent.GetComponent<Rigidbody>();
			}
			component.connectedBody = component2;
		}
	}

	public void EnableRagdoll(bool enable)
	{
		if (RigidBodies == null)
		{
			return;
		}
		ArrayList arrayList = new ArrayList();
		Rigidbody[] rigidBodies = RigidBodies;
		foreach (Rigidbody rigidbody in rigidBodies)
		{
			if (!enable || (enable && rigidbody.transform.localScale.x > 0.9f))
			{
				EnablePhysics(rigidbody, enable);
			}
			else
			{
				arrayList.Add(rigidbody);
			}
		}
		foreach (Rigidbody item in arrayList)
		{
			Rigidbody[] componentsInChildren = item.GetComponentsInChildren<Rigidbody>();
			Rigidbody[] array = componentsInChildren;
			foreach (Rigidbody rb in array)
			{
				EnablePhysics(rb, false);
			}
		}
	}

	public void AddFactToMemory(E_EventTypes eventType, AgentHuman agent, float lifetime, float delay)
	{
		Fact fact = FactsFactory.Create(eventType);
		fact.Agent = agent;
		fact.Delay = delay;
		fact.LiveTime = lifetime;
		fact.Position = agent.Position;
		Memory.AddFact(fact);
		BlackBoard.UpdateCombatSetting(fact);
		WorldState.SetWSProperty(E_PropKey.Event, eventType);
	}

	public void AddFactToMemory(Fact fact)
	{
		Memory.AddFact(fact);
		BlackBoard.UpdateCombatSetting(fact);
		WorldState.SetWSProperty(E_PropKey.Event, fact.Type);
	}

	public bool CanFire()
	{
		if (BlackBoard.BusyAction)
		{
			return false;
		}
		if (IsActionPointOn)
		{
			return false;
		}
		if ((bool)WeaponComponent && (bool)WeaponComponent.GetCurrentWeapon())
		{
			if (WeaponComponent.GetCurrentWeapon().IsBusy())
			{
				return false;
			}
			if (WeaponComponent.GetCurrentWeapon().ClipAmmo == 0)
			{
				return false;
			}
		}
		return true;
	}

	public bool CanUseGadget(Item gadget)
	{
		if (gadget == null)
		{
			return false;
		}
		if (!IsAlive)
		{
			return false;
		}
		if (gadget.Settings.ItemUse != E_ItemUse.Activate)
		{
			return false;
		}
		if (BlackBoard.BusyAction)
		{
			return false;
		}
		if (!gadget.IsAvailableForUse())
		{
			return false;
		}
		if ((gadget.Settings.ItemBehaviour == E_ItemBehaviour.Throw || gadget.Settings.ItemBehaviour == E_ItemBehaviour.Place) && WeaponComponent.GetCurrentWeapon().IsBusy())
		{
			return false;
		}
		return true;
	}

	private void OnControllerColliderHit(ControllerColliderHit hit)
	{
		Rigidbody attachedRigidbody = hit.collider.attachedRigidbody;
		if (!(attachedRigidbody == null) && !attachedRigidbody.isKinematic)
		{
			Vector3 forward = default(Vector3);
			if (hit.moveDirection.y < -0.75f)
			{
				forward = hit.controller.transform.forward;
			}
			else
			{
				forward.x = hit.moveDirection.x;
				forward.y = 0f;
				forward.z = hit.moveDirection.z;
			}
			forward.Normalize();
			attachedRigidbody.velocity = forward * RigidBodyPushingForce;
		}
	}

	private void DropPickups(E_DropEvent DropEvent, bool Ammo, bool Money)
	{
		int num = 0;
		Vector3 vector = base.Forward * 0.25f;
		if (Ammo && PickupManager.Instance.DropPickup(DropEvent, E_PickupID.Ammo, base.Position + vector))
		{
			num++;
		}
		if (Money && PickupManager.Instance.DropPickup(DropEvent, E_PickupID.Money, base.Position - vector))
		{
			num++;
		}
		if (num > 0)
		{
			Player.Instance.CommentPickup();
		}
	}
}
