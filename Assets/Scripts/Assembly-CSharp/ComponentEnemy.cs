using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(AnimComponent))]
[RequireComponent(typeof(ComponentSensors))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(ComponentBody))]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Animation))]
[RequireComponent(typeof(AgentHuman))]
public class ComponentEnemy : MonoBehaviour, IHitZoneOwner
{
	[Serializable]
	public class SoundInfo
	{
		public AudioClip[] SeeEnemy = new AudioClip[0];

		public AudioClip[] Idles = new AudioClip[0];

		public AudioClip[] Attacks = new AudioClip[0];

		public AudioClip[] Spits = new AudioClip[0];

		public AudioClip ContestStart = new AudioClip();

		public AudioClip ContestWon = new AudioClip();

		public AudioClip ContestLost = new AudioClip();
	}

	[Serializable]
	public class RhythmDef
	{
		public AudioClip Pattern;

		public AudioClip SpeechLoop;

		public BeatDef[] Beats = new BeatDef[0];
	}

	[Serializable]
	public class BeatDef
	{
		public float BeatStart;

		public float BeatLength;
	}

	[Serializable]
	public class ParticleHitInfo
	{
		public string boneName;

		public ParticleSystem particle;

		[HideInInspector]
		public int boneIndex;
	}

	public class LimbManager
	{
		private struct BoneInfo
		{
			private int index;

			public float modifier;

			public void Init(string boneName, Transform[] bones, float mod = 1f)
			{
				index = FindBone(boneName, bones);
				modifier = mod;
			}

			public void ApplyModifier(Transform[] bones)
			{
				bones[index].localScale = new Vector3(modifier, modifier, modifier);
			}

			private static int FindBone(string boneName, Transform[] bones)
			{
				if (bones == null || bones.Length < 1)
				{
					return -1;
				}
				for (int i = 0; i < bones.Length; i++)
				{
					if (bones[i].name == boneName)
					{
						return i;
					}
				}
				return -1;
			}
		}

		private BoneInfo head;

		private BoneInfo lArm;

		private BoneInfo rArm;

		private BoneInfo lLeg;

		private BoneInfo rLeg;

		public int LimbsDecapitated { get; private set; }

		public void Init(AgentHuman owner)
		{
			if (!(owner == null) && !(owner.Renderer == null))
			{
				if (!owner.Renderer.enabled)
				{
					owner.ShowModel(true);
				}
				LimbsDecapitated = 0;
				float mod = 1f;
				PlayerPersistantInfo playerPersistentInfo = Game.Instance.PlayerPersistentInfo;
				if (playerPersistentInfo.EquipList.ContainsItem(E_ItemID.BigHeads) || ((bool)Player.Instance && Player.Instance.GetActivePowerup() == E_ItemID.BigHeads))
				{
					mod = 2f;
				}
				head.Init("Head", owner.Renderer.bones, mod);
				lArm.Init("Larm", owner.Renderer.bones, 1f);
				rArm.Init("Rarm", owner.Renderer.bones, 1f);
				lLeg.Init("Lcalf", owner.Renderer.bones, 1f);
				rLeg.Init("Rcalf", owner.Renderer.bones, 1f);
			}
		}

		private void StartCrawl(AgentHuman owner, E_MotionSide side)
		{
			if (owner.WorldState.GetWSProperty(E_PropKey.BodyPose).GetBodyPose() == E_BodyPose.Stand)
			{
				owner.NavMeshAgent.autoTraverseOffMeshLink = true;
				owner.StartCoroutine(_DoCrawlTransition(owner, owner.AnimSet.GetStandToCrawlAnim(side)));
			}
		}

		private IEnumerator _DoCrawlTransition(AgentHuman owner, string animName)
		{
			yield return new WaitForEndOfFrame();
			owner.WorldState.SetWSProperty(E_PropKey.BodyPose, E_BodyPose.StandToCrawl);
			yield return new WaitForSeconds(owner.PlayAnim(animName, E_TriState.False) - 0.2f);
			owner.BlackBoard.CrawlTime = 0f;
			owner.WorldState.SetWSProperty(E_PropKey.BodyPose, E_BodyPose.Crawl);
		}

		public bool DecapitateLimbs(AgentHuman owner, E_BodyPart bodyPart, bool willDie)
		{
			switch (bodyPart)
			{
			case E_BodyPart.Head:
				head.modifier = 0.01f;
				return true;
			case E_BodyPart.Body:
				owner.ShowModel(false);
				return true;
			case E_BodyPart.LeftArm:
				lArm.modifier = 0.01f;
				break;
			case E_BodyPart.RightArm:
				rArm.modifier = 0.01f;
				break;
			case E_BodyPart.LeftLeg:
				lLeg.modifier = 0.01f;
				if (!willDie)
				{
					StartCrawl(owner, E_MotionSide.Left);
				}
				break;
			case E_BodyPart.RightLeg:
				rLeg.modifier = 0.01f;
				if (!willDie)
				{
					StartCrawl(owner, E_MotionSide.Right);
				}
				break;
			default:
				return false;
			}
			LimbsDecapitated++;
			return true;
		}

		public void UpdateDecapitatedLimbs(AgentHuman owner)
		{
			if (IsScaled(E_BodyPart.Head))
			{
				head.ApplyModifier(owner.Renderer.bones);
			}
			if (IsScaled(E_BodyPart.LeftArm))
			{
				lArm.ApplyModifier(owner.Renderer.bones);
			}
			if (IsScaled(E_BodyPart.RightArm))
			{
				rArm.ApplyModifier(owner.Renderer.bones);
			}
			if (IsScaled(E_BodyPart.LeftLeg))
			{
				lLeg.ApplyModifier(owner.Renderer.bones);
			}
			if (IsScaled(E_BodyPart.RightLeg))
			{
				rLeg.ApplyModifier(owner.Renderer.bones);
			}
		}

		public bool IsScaled(E_BodyPart limbID, float scale = 1f, float threshold = 0.5f)
		{
			float num = scale - threshold;
			float num2 = scale + threshold;
			switch (limbID)
			{
			case E_BodyPart.Head:
				return head.modifier < num || head.modifier > num2;
			case E_BodyPart.LeftArm:
				return lArm.modifier < num || lArm.modifier > num2;
			case E_BodyPart.RightArm:
				return rArm.modifier < num || rArm.modifier > num2;
			case E_BodyPart.LeftLeg:
				return lLeg.modifier < num || lLeg.modifier > num2;
			case E_BodyPart.RightLeg:
				return rLeg.modifier < num || rLeg.modifier > num2;
			default:
				return false;
			}
		}
	}

	public bool CanDoContest;

	public SoundInfo SoundSetup = new SoundInfo();

	public RhythmDef[] ContestRhythm = new RhythmDef[0];

	private static float DontSpeakTimer;

	private bool AttackSoundPlaying;

	[NonSerialized]
	public ArrayList ContestHit = new ArrayList();

	private ParticleSystem ParticleHit;

	private LimbManager LimbMgr = new LimbManager();

	public AgentHuman Owner { get; private set; }

	public AudioClip SeeEnemy
	{
		get
		{
			if (SoundSetup.SeeEnemy.Length == 0)
			{
				return null;
			}
			return SoundSetup.SeeEnemy[UnityEngine.Random.Range(0, SoundSetup.SeeEnemy.Length)];
		}
	}

	public AudioClip IdleSound
	{
		get
		{
			if (SoundSetup.Idles.Length == 0)
			{
				return null;
			}
			return SoundSetup.Idles[UnityEngine.Random.Range(0, SoundSetup.Idles.Length)];
		}
	}

	public AudioClip AttackSound
	{
		get
		{
			if (SoundSetup.Attacks.Length == 0)
			{
				return null;
			}
			return SoundSetup.Attacks[UnityEngine.Random.Range(0, SoundSetup.Attacks.Length)];
		}
	}

	public AudioClip SpitSound
	{
		get
		{
			if (SoundSetup.Spits.Length == 0)
			{
				return null;
			}
			return SoundSetup.Spits[UnityEngine.Random.Range(0, SoundSetup.Spits.Length)];
		}
	}

	public AudioClip ContestStartSound
	{
		get
		{
			return SoundSetup.ContestStart;
		}
	}

	public AudioClip ContestWonSound
	{
		get
		{
			return SoundSetup.ContestWon;
		}
	}

	public AudioClip ContestLostSound
	{
		get
		{
			return SoundSetup.ContestLost;
		}
	}

	public void RemoveHead(bool willDie)
	{
		LimbMgr.DecapitateLimbs(Owner, E_BodyPart.Head, willDie);
	}

	protected void Awake()
	{
		Owner = GetComponent<AgentHuman>();
	}

	private void InitContestButtons()
	{
		int index = 0;
		Transform t;
		while ((t = Owner.Transform.FindChildByName("ContestHit" + index)) != null)
		{
			ContestHit.Add(t.gameObject);
			index++;
		}
	}

	protected void Start()
	{
		Owner.BlackBoard.IsPlayer = false;
		BlackBoard blackBoard = Owner.BlackBoard;
		blackBoard.ActionHandler = (BlackBoard.AgentActionHandler)Delegate.Combine(blackBoard.ActionHandler, new BlackBoard.AgentActionHandler(HandleAction));
		InitContestButtons();
		LimbMgr.Init(Owner);
	}

	protected void Activate(SpawnPoint spawn)
	{
		LimbMgr.Init(Owner);
		DontSpeakTimer = 0f;
		Owner.BlackBoard.SpawnPointEnemy = spawn as SpawnPointEnemy;
		Owner.BlackBoard.SightRange = Owner.BlackBoard.BaseSetup.SightRangeInCombat;
		Owner.BlackBoard.SightFov = Owner.BlackBoard.BaseSetup.SightFovInCombat;
		Owner.NavMeshAgent.areaMask &= ~(1 << NavMesh.GetAreaFromName("WalkOnlyWhenSpawn"));
		Owner.NavMeshAgent.autoTraverseOffMeshLink = false;
	}

	protected void Deactivate()
	{
		Owner.NavMeshAgent.areaMask = 286331153;
		DestroyHitParticle();
	}

	protected void Update()
	{
		Owner.BlackBoard.Desires.FireDirection = Owner.Forward;
		if (!Owner.BlackBoard.Stop)
		{
		}
	}

	private void LateUpdate()
	{
		UpdateCombatSetting();
		UpdateSpeech();
		LimbMgr.UpdateDecapitatedLimbs(Owner);
	}

	public void HandleAction(AgentAction a)
	{
		if (a is AgentActionInjury)
		{
			if (Mission.Instance.CurrentGameZone != null)
			{
				Mission.Instance.CurrentGameZone.SendFactToEnemies(Owner, (a as AgentActionInjury).Attacker, E_EventTypes.FriendInjured, 30f, 3f, true);
			}
		}
		else if (a is AgentActionDeath && Mission.Instance.CurrentGameZone != null)
		{
			Mission.Instance.CurrentGameZone.SendFactToEnemies(Owner, (a as AgentActionDeath).Attacker, E_EventTypes.FriendDeath, 30f, 3f, true);
		}
		Owner.BlackBoard.UpdateCombatSetting(a);
	}

	private void DestroyHitParticle()
	{
		if ((bool)ParticleHit)
		{
			UnityEngine.Object.Destroy(ParticleHit.gameObject);
			ParticleHit = null;
		}
	}

	private void PlayHitParticles(ParticleSystem particle, Vector3 position, bool forceStop)
	{
		if ((bool)ParticleHit && (ParticleHit.isPlaying || ParticleHit.particleCount > 0))
		{
			if (!forceStop)
			{
				return;
			}
			ParticleHit.Stop();
			ParticleHit.Clear();
		}
		if ((bool)ParticleHit && ParticleHit.name != particle.name)
		{
			DestroyHitParticle();
		}
		if (!ParticleHit)
		{
			ParticleHit = UnityEngine.Object.Instantiate(particle) as ParticleSystem;
			ParticleHit.name = particle.name;
		}
		if ((bool)ParticleHit)
		{
			ParticleHit.transform.position = position;
			ParticleHit.Play();
		}
	}

	public bool IsLimbDecapitated(E_BodyPart limbID)
	{
		return LimbMgr.IsScaled(limbID, 1.2f, 0.9f);
	}

	public int GetNumDecapitatedLimbs()
	{
		return LimbMgr.LimbsDecapitated;
	}

	public E_BodyPart GetBodyPart(HitZone zone)
	{
		switch (zone.name)
		{
		case "Head":
			return E_BodyPart.Head;
		case "Spine01":
			return E_BodyPart.Body;
		case "Lforearm":
		case "Larm":
			return E_BodyPart.LeftArm;
		case "Rforearm":
		case "Rarm":
			return E_BodyPart.RightArm;
		case "Lthigh":
		case "Lcalf":
			return E_BodyPart.LeftLeg;
		case "Rthigh":
		case "Rcalf":
			return E_BodyPart.RightLeg;
		default:
			return E_BodyPart.Body;
		}
	}

	private bool BodyPartDestroyed(HitZoneEffects zoneEffect, float damage, bool willDie, bool blades)
	{
		if (!willDie && (Owner.IsInvulnerable || !Owner.BlackBoard.ReactOnHits))
		{
			return false;
		}
		if (blades && (!zoneEffect.MustDieToDestroy || (zoneEffect.MustDieToDestroy && willDie)))
		{
			return true;
		}
		switch (GetBodyPart(zoneEffect))
		{
		case E_BodyPart.LeftArm:
		case E_BodyPart.RightArm:
		case E_BodyPart.LeftLeg:
		case E_BodyPart.RightLeg:
			if (LimbMgr.LimbsDecapitated > 2 && !willDie)
			{
				return false;
			}
			break;
		}
		return (zoneEffect.CumulativeDamage >= zoneEffect.DestroyCumulativePercentage * Owner.BlackBoard.RealMaxHealth || damage >= zoneEffect.DestroyBashPercentage * Owner.BlackBoard.RealMaxHealth) && ((zoneEffect.MustDieToDestroy && willDie) || !zoneEffect.MustDieToDestroy);
	}

	public void OnHitZoneProjectileHit(HitZone zone, Projectile projectile)
	{
		if (!Owner.IsAlive)
		{
			return;
		}
		HitZoneEffects hitZoneEffects = zone as HitZoneEffects;
		if (hitZoneEffects == null)
		{
			Owner.OnProjectileHit(projectile, zone);
			return;
		}
		E_BodyPart bodyPart = GetBodyPart(zone);
		float num = ((projectile.ProjectileType == E_ProjectileType.Melee) ? 1f : hitZoneEffects.DamageModifier);
		bool flag = 0f >= Owner.GetNewHealth(projectile.Damage() * num);
		bool flag2 = BodyPartDestroyed(hitZoneEffects, projectile.Damage() * num * projectile.BodyPartDamageModif, flag, false);
		if (flag || Owner.BlackBoard.ReactOnHits)
		{
			Vector3 position = zone.transform.position;
			float distanceToTarget = Owner.BlackBoard.DistanceToTarget;
			if (flag2)
			{
				LimbMgr.DecapitateLimbs(Owner, bodyPart, flag);
				PlayHitParticles(hitZoneEffects.DestroyParticle, position, bodyPart == E_BodyPart.Body);
				switch (bodyPart)
				{
				case E_BodyPart.Body:
					if (distanceToTarget < 4.5f)
					{
						BloodFXManager.Instance.SpawnBloodSplashes(8u);
					}
					break;
				case E_BodyPart.Head:
					if (distanceToTarget < 3f)
					{
						BloodFXManager.Instance.SpawnBloodSplashes(4u);
					}
					break;
				default:
					if (distanceToTarget < 2.5f)
					{
						BloodFXManager.Instance.SpawnBloodSplashes(2u);
					}
					break;
				}
			}
			else if (flag && distanceToTarget < 2.5f && UnityEngine.Random.value < 0.33f)
			{
				BloodFXManager.Instance.SpawnBloodSplashes(2u);
			}
		}
		Owner.OnProjectileHit(projectile, zone, bodyPart, flag2);
	}

	public void OnHitZoneRangeDamage(HitZone Zone, Agent Attacker, float Damage, Vector3 Impulse, E_WeaponID weaponID, E_WeaponType WeaponType)
	{
		if (Owner.IsAlive)
		{
			Owner.OnReceiveRangeDamage(Attacker, Damage, Impulse, weaponID, WeaponType);
		}
	}

	public void OnHitZoneMeleeDamage(HitZone zone, MeleeDamageData Data)
	{
		if (!Owner.IsAlive)
		{
			return;
		}
		BladesChopperDamage bladesChopperDamage = Data as BladesChopperDamage;
		E_BodyPart e_BodyPart = ((bladesChopperDamage == null) ? GetBodyPart(zone) : bladesChopperDamage.ChoppedOffPart);
		float num = ((bladesChopperDamage == null) ? 0f : (bladesChopperDamage.ReducedHealthCoef * Owner.BlackBoard.RealMaxHealth));
		float damage = ((bladesChopperDamage == null || Data.Damage != 0f) ? Data.Damage : ((!(Owner.BlackBoard.Health > num)) ? 0f : (Owner.BlackBoard.Health - num)));
		Data.Damage = damage;
		HitZoneEffects hitZoneEffects = zone as HitZoneEffects;
		if (hitZoneEffects == null)
		{
			Owner.OnMeleeHit(zone, Data, e_BodyPart);
			return;
		}
		bool flag = BodyPartDestroyed(hitZoneEffects, Data.Damage, 0f >= Owner.GetNewHealth(Data.Damage), bladesChopperDamage != null);
		if (Owner.BlackBoard.ReactOnHits && flag)
		{
			Vector3 position = zone.transform.position;
			bool willDie = 0f >= Owner.GetNewHealth(Data.Damage * hitZoneEffects.DamageModifier);
			LimbMgr.DecapitateLimbs(Owner, e_BodyPart, willDie);
			PlayHitParticles(hitZoneEffects.DestroyParticle, position, e_BodyPart == E_BodyPart.Body);
		}
		Owner.OnMeleeHit(zone, Data, e_BodyPart, flag);
	}

	public float PlayAttackSound(E_WeaponAction type)
	{
		AudioClip audioClip = null;
		audioClip = ((type != E_WeaponAction.Vomit) ? AttackSound : SpitSound);
		if (audioClip != null)
		{
			AttackSoundPlaying = true;
			Owner.SoundUniquePlay(audioClip, audioClip.length, "Attack");
			DontSpeakTimer = Time.timeSinceLevelLoad + audioClip.length;
			return audioClip.length;
		}
		return 0f;
	}

	private void UpdateSpeech()
	{
		if (!Owner.IsVisible || Owner.BlackBoard.Stop || !Owner.IsAlive)
		{
			return;
		}
		if (!AttackSoundPlaying && Owner.BlackBoard.MotionType == E_MotionType.Attack)
		{
			PlayAttackSound(E_WeaponAction.MeleeLeft);
		}
		if (Owner.IsInContest())
		{
			if (Owner.Memory.GetValidFact(E_EventTypes.ContestStart) != null)
			{
				Owner.Memory.RemoveFact(E_EventTypes.ContestStart);
				Owner.SoundStop();
				DontSpeakTimer = Owner.SoundUniquePlay(ContestStartSound) + Time.timeSinceLevelLoad + 0.1f;
			}
			if (Owner.Memory.GetValidFact(E_EventTypes.ContestWon) != null)
			{
				Owner.SoundStop();
				DontSpeakTimer = Owner.SoundUniquePlay(ContestWonSound) + Time.timeSinceLevelLoad + 0.2f;
			}
			else if (Owner.Memory.GetValidFact(E_EventTypes.ContestLost) != null)
			{
				Owner.SoundStop();
				DontSpeakTimer = Owner.SoundUniquePlay(ContestLostSound) + Time.timeSinceLevelLoad + 0.2f;
			}
		}
		else if (!Owner.BlackBoard.BusyAction && !(DontSpeakTimer > Time.timeSinceLevelLoad))
		{
			AttackSoundPlaying = false;
			if (Owner.Memory.GetValidFact(E_EventTypes.EnemySee) != null && UnityEngine.Random.Range(0, 100) < 25)
			{
				Owner.SoundPlay(SeeEnemy);
				DontSpeakTimer = Time.timeSinceLevelLoad + UnityEngine.Random.Range(10f, 15f);
			}
			else if (!Owner.WorldState.GetWSProperty(E_PropKey.InWeaponRange).GetBool() && !Owner.Audio.isPlaying && Owner.IsAlive)
			{
				Owner.PlayLoopedSound(IdleSound, 0f, 1000f, 0.1f, 0.1f);
			}
		}
	}

	protected virtual void UpdateCombatSetting()
	{
		bool flag = Owner.WorldState.GetWSProperty(E_PropKey.SeeEnemy).GetBool();
		BlackBoard blackBoard = Owner.BlackBoard;
		if (!flag && Owner.Memory.GetValidFact(E_EventTypes.EnemyLost) != null)
		{
			flag = true;
		}
		if (flag)
		{
			blackBoard.SetRage(blackBoard.Rage + blackBoard.CombatSetup.RageModificator * Time.deltaTime);
		}
		else
		{
			blackBoard.SetRage(blackBoard.Rage - blackBoard.CombatSetup.RageModificator * Time.deltaTime * 0.5f);
		}
		if (!flag)
		{
			blackBoard.SetFear(blackBoard.Fear - blackBoard.CombatSetup.FearModificator * Time.deltaTime);
		}
		else if (Owner.WorldState.GetWSProperty(E_PropKey.CoverState).GetCoverState() != 0)
		{
			blackBoard.SetFear(blackBoard.Fear - blackBoard.CombatSetup.FearModificator * Time.deltaTime * 0.2f);
		}
		else if (Owner.WorldState.GetWSProperty(E_PropKey.AheadOfEnemy).GetBool())
		{
			blackBoard.SetFear(blackBoard.Fear + blackBoard.CombatSetup.FearModificator * Time.deltaTime);
		}
		else
		{
			blackBoard.SetFear(blackBoard.Fear + blackBoard.CombatSetup.FearModificator * Time.deltaTime * 0.5f);
		}
	}
}
