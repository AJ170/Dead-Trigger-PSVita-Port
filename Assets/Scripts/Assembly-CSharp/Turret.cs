using System;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Mathematics.math;

[RequireComponent(typeof(AudioSource))]
[AddComponentMenu("Items/Turret")]
public class Turret : MonoBehaviour
{
	private enum E_AimPhase
	{
		Idle = 0,
		Starting = 1,
		InProgress = 2,
		Stopping = 3
	}

	[Serializable]
	public class PartsSettings
	{
		public Transform m_Base;

		public Transform m_Neck;

		public Transform m_Muzzle;
	}

	[Serializable]
	public class AimingSettings
	{
		public float m_HRange = 140f;

		public float m_HSpeed = 40f;

		public float m_VRange = 80f;

		public float m_VSpeed = 30f;

		public float m_FOV = 60f;

		public float m_InSightsTolerance = 4f;

		public float m_TargetLostTimeout = 2f;
	}

	[Serializable]
	public class WpnSettings
	{
		public int m_AmmoClip = 20;

		public float m_AverageKilledSec = 2f;

		public float m_AverageKilledNum = 10f;

		public float m_FireRate = 0.1f;

		public float m_FireEffectTime = 0.08f;

		public float m_ReloadDelay = 2f;

		public float m_Dispersion = 0.05f;

		public float m_RangeEffective = 20f;

		public float m_RangeMaximal = 30f;

		public float m_ProjSpeed = 100f;

		public float m_ProjImpuls;

		public E_ProjectileType m_ProjType = E_ProjectileType.MG;
	}

	[Serializable]
	public class SoundsSettings
	{
		public AudioSource m_SpecFireAudioSrc;

		public AudioClip m_Fire;

		public AudioClip m_AimingStart;

		public AudioClip m_AimingLoop;

		public AudioClip m_AimingStop;
	}

	private class TargetData
	{
		public AgentHuman m_Agent;

		public Vector3 m_LastVisiblePos;

		public float m_LastVisibleTime;

		public bool m_InFOV;

		public bool m_InSights;

		public bool m_InRange;

		public bool m_IsVisible;
	}

	[Serializable]
	public class DeactivationSettings
	{
		public float m_Delay;

		public GameObject m_Destroy;

		public Explosion m_Explosion;

		public Transform m_ExplosionOrigin;

		public float m_ExplosionDamageCoef;

		public float m_ExplosionRadius;

		public float m_ExplosionRadiusMax;
	}

	private const float MuzzleRotLimit = 8f;

	private const float MuzzleScaleLimit = 0.05f;

	private AgentHuman m_Owner;

	public PartsSettings m_PartsSettings = new PartsSettings();

	public AimingSettings m_AimSettings = new AimingSettings();

	public WpnSettings m_WpnSettings = new WpnSettings();

	public SoundsSettings m_SndSettings = new SoundsSettings();

	private Vector3 m_AimDir;

	private E_AimPhase m_AimPhase;

	private TurretRotator m_HMotor;

	private TurretRotator m_VMotor;

	private float m_InitHAngle;

	private float m_InitVAngle;

	private AudioSource m_Audio;

	private AudioSource m_AudioFire;

	private TargetData m_Target = new TargetData();

	private int m_AmmoClip;

	private int m_AmmoRemaining;

	private int m_AmmoTotal;

	private float m_NextShotTime;

	private float m_ShotEffectTime;

	private float m_ReloadTime;

	private float m_SelectTargetTime;

	private float m_ThresholdInFOV;

	private float m_ThresholdInSight;

	private ProjectileInitSettings m_ProjInitSettings;

	private Vector3 m_MuzzleOriginalRot;

	private Vector3 m_MuzzleOriginalScale;

	private bool m_Deactivated;

	public DeactivationSettings m_Deactivation = new DeactivationSettings();

	private MappingChange m_LifeBar;

	private UnityEngine.AI.NavMeshAgent m_NavMeshAgent;

	private float tm;

	public bool OutOfAmmo
	{
		get
		{
			return m_AmmoRemaining == 0 && m_AmmoClip == 0;
		}
	}

	private void Init(Item.InitData Data)
	{
		m_Owner = Data.Owner;
		Transform transform = base.transform;
		transform.position = Data.Pos;
		transform.rotation = Data.Owner.Transform.rotation;
		SemanticMaterialManager.Instance.SpawnPlacementEffect(Data.Pos);
	}

	private void Start()
	{
		m_Audio = base.GetComponent<AudioSource>();
		m_Audio.playOnAwake = false;
		m_Audio.loop = true;
		m_Audio.clip = m_SndSettings.m_AimingLoop;
		m_AudioFire = ((!(m_SndSettings.m_SpecFireAudioSrc != null)) ? m_Audio : m_SndSettings.m_SpecFireAudioSrc);
		m_InitHAngle = 0f;
		m_InitVAngle = 0f;
		m_HMotor = new TurretRotator(m_InitHAngle, m_AimSettings.m_HRange * ((float)Math.PI / 180f), m_AimSettings.m_HSpeed * ((float)Math.PI / 180f));
		m_VMotor = new TurretRotator(m_InitVAngle, m_AimSettings.m_VRange * ((float)Math.PI / 180f), m_AimSettings.m_VSpeed * ((float)Math.PI / 180f));
		m_MuzzleOriginalRot = m_PartsSettings.m_Muzzle.localEulerAngles;
		m_MuzzleOriginalScale = m_PartsSettings.m_Muzzle.localScale;
		m_PartsSettings.m_Muzzle.gameObject.SetActive(false);
		m_ThresholdInFOV = Mathf.Cos((float)Math.PI / 180f * m_AimSettings.m_FOV * 0.5f);
		m_ThresholdInSight = Mathf.Cos((float)Math.PI / 180f * m_AimSettings.m_InSightsTolerance);
		float num = GameplayData.Instance.EnemyHealth();
		float num2 = num / m_WpnSettings.m_AverageKilledSec * m_WpnSettings.m_FireRate;
		float num3 = num * m_WpnSettings.m_AverageKilledNum;
		m_ProjInitSettings = new ProjectileInitSettings();
		m_ProjInitSettings.Agent = m_Owner;
		m_ProjInitSettings.IgnoreTransform = ((!(m_Owner != null)) ? null : m_Owner.Transform);
		m_ProjInitSettings.Speed = m_WpnSettings.m_ProjSpeed;
		m_ProjInitSettings.Damage = num2;
		m_ProjInitSettings.Impuls = m_WpnSettings.m_ProjImpuls;
		m_ProjInitSettings.EffectiveRange = m_WpnSettings.m_RangeEffective;
		m_ProjInitSettings.MaxRange = m_WpnSettings.m_RangeMaximal;
		m_ProjInitSettings.Weapon = E_WeaponID.None;
		m_AmmoTotal = Mathf.CeilToInt(num3 / num2);
		m_AmmoRemaining = m_AmmoTotal;
		m_AmmoClip = Mathf.Min(m_WpnSettings.m_AmmoClip, m_AmmoRemaining);
		m_AmmoRemaining -= m_AmmoClip;
		MappingChange[] componentsInChildren = base.gameObject.GetComponentsInChildren<MappingChange>();
		if (componentsInChildren != null && componentsInChildren.Length > 0)
		{
			m_LifeBar = componentsInChildren[0];
			m_LifeBar.Value = 1f;
			m_LifeBar.MappingChangeType = MappingChange.Type.BasedOnValue;
		}
		m_NavMeshAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
		if (m_NavMeshAgent != null)
		{
			m_NavMeshAgent.updatePosition = false;
			m_NavMeshAgent.updateRotation = false;
			m_NavMeshAgent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.NoObstacleAvoidance;
			m_NavMeshAgent.enabled = true;
		}
	}

	private void OnDestroy()
	{
		m_ProjInitSettings = null;
		m_NavMeshAgent = null;
		m_Owner = null;
		m_LifeBar = null;
		m_HMotor = null;
		m_VMotor = null;
	}

	private void Update()
	{
		UpdateAiming(Time.deltaTime);
		if (!OutOfAmmo)
		{
			UpdateTarget(Time.timeSinceLevelLoad);
			UpdateReload(Time.deltaTime);
		}
		UpdateShooting(Time.deltaTime);
	}

	private void UpdateAiming(float DeltaTime)
	{
		bool flag = false;
		bool flag2 = Mathf.Max(m_HMotor.AbsDiff, m_VMotor.AbsDiff) <= (float)Math.PI / 180f;
		if (flag2)
		{
			tm = 0f;
		}
		else
		{
			tm += DeltaTime;
			flag = tm > 0.1f;
		}
		m_HMotor.Update(DeltaTime);
		m_VMotor.Update(DeltaTime);
		m_AimDir = MathUtils.AnglesToVector(m_PartsSettings.m_Base.forward, m_PartsSettings.m_Base.up, m_HMotor.Angle, m_VMotor.Angle);
		m_PartsSettings.m_Neck.rotation = Quaternion.LookRotation(m_AimDir, m_PartsSettings.m_Base.up);
		E_AimPhase aimPhase = m_AimPhase;
		if (flag)
		{
			if (m_AimPhase == E_AimPhase.Idle || m_AimPhase == E_AimPhase.Stopping)
			{
				ChangeAimPhase(E_AimPhase.Starting);
			}
		}
		else if (m_AimPhase == E_AimPhase.Starting || m_AimPhase == E_AimPhase.InProgress)
		{
			ChangeAimPhase(E_AimPhase.Stopping);
		}
		if (aimPhase == m_AimPhase && (m_Audio == null || !m_Audio.isPlaying))
		{
			if (m_AimPhase == E_AimPhase.Starting)
			{
				ChangeAimPhase(E_AimPhase.InProgress);
			}
			else if (m_AimPhase == E_AimPhase.Stopping)
			{
				ChangeAimPhase(E_AimPhase.Idle);
			}
		}
		if (m_Target.m_Agent != null)
		{
			float AngleH = 0f;
			float AngleV = 0f;
			Vector3 vec = Vector3.Normalize(m_Target.m_LastVisiblePos - m_PartsSettings.m_Neck.position);
			MathUtils.VectorToAngles(m_PartsSettings.m_Base.forward, m_PartsSettings.m_Base.up, vec, ref AngleH, ref AngleV);
			m_HMotor.TargetAngle = AngleH;
			m_VMotor.TargetAngle = AngleV;
		}
		else if (!OutOfAmmo)
		{
			m_HMotor.TargetAngle = m_InitHAngle;
			m_VMotor.TargetAngle = m_InitVAngle;
		}
		else if (m_Deactivated && flag2)
		{
			Invoke("OnDeactivation", m_Deactivation.m_Delay);
		}
	}

	private void ChangeAimPhase(E_AimPhase NewPhase)
	{
		if (m_Audio != null)
		{
			float num = 0f;
			if (m_Audio.isPlaying && m_AimPhase != E_AimPhase.InProgress
				&& m_Audio.clip != null && m_Audio.clip.length > 0f)
			{
				num = 1f - m_Audio.time / m_Audio.clip.length;
			}

			m_Audio.Stop();
			m_Audio.clip = null;
			m_Audio.loop = false;

			switch (NewPhase)
			{
				case E_AimPhase.Starting:
					if (m_SndSettings.m_AimingStart != null)
					{
						m_Audio.clip = m_SndSettings.m_AimingStart;
						// Clamp seek to valid range for the new clip
						float startSeek = num * m_Audio.clip.length;
						m_Audio.time = Mathf.Clamp(
							startSeek, 0f,
							m_Audio.clip.length - 0.01f);
					}
					break;

				case E_AimPhase.InProgress:
					m_Audio.clip = m_SndSettings.m_AimingLoop;
					m_Audio.loop = true;
					// Loop always starts from beginning
					m_Audio.time = 0f;
					break;

				case E_AimPhase.Stopping:
					if (m_SndSettings.m_AimingStop != null)
					{
						m_Audio.clip = m_SndSettings.m_AimingStop;
						// Clamp seek to valid range for the new clip
						float stopSeek = num * m_Audio.clip.length;
						m_Audio.time = Mathf.Clamp(
							stopSeek, 0f,
							m_Audio.clip.length - 0.01f);
					}
					break;
			}

			// Only play if a valid loaded clip was assigned
			if (m_Audio.clip != null
				&& m_Audio.clip.loadState == AudioDataLoadState.Loaded)
			{
				m_Audio.Play();
			}
		}

		m_AimPhase = NewPhase;
	}

	private void UpdateTarget(float CurrTime)
	{
		bool flag = false;
		flag |= m_Target.m_Agent == null || !m_Target.m_Agent.IsAlive;
		if (flag | (CurrTime - m_Target.m_LastVisibleTime >= m_AimSettings.m_TargetLostTimeout))
		{
			SelectTarget(CurrTime);
		}
		else if (m_Target.m_Agent != null)
		{
			Vector3 vector = m_Target.m_Agent.TransformTarget.position - m_PartsSettings.m_Neck.position;
			float num = Vector3.Magnitude(vector);
			float num2 = Vector3.Dot(m_AimDir, vector / num);
			m_Target.m_InFOV = num2 > m_ThresholdInFOV;
			m_Target.m_InSights = num2 > m_ThresholdInSight;
			m_Target.m_InRange = num < m_WpnSettings.m_RangeMaximal;
			m_Target.m_IsVisible = IsTargetVisible(m_Target.m_Agent);
			if (m_Target.m_InRange && m_Target.m_InFOV && m_Target.m_IsVisible)
			{
				m_Target.m_LastVisiblePos = m_Target.m_Agent.TransformTarget.position;
				m_Target.m_LastVisibleTime = CurrTime;
			}
		}
	}

	private void SelectTarget(float CurrTime)
	{
		m_Target.m_Agent = null;
		AgentHuman agentHuman = null;
		List<Agent> enemies = Mission.Instance.CurrentGameZone.Enemies;
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < enemies.Count; i++)
		{
			AgentHuman agentHuman2 = enemies[i] as AgentHuman;
			if (agentHuman2 == null || !IsTargetValid(agentHuman2))
			{
				continue;
			}
			Vector3 vector = agentHuman2.TransformTarget.position - m_PartsSettings.m_Neck.position;
			float num3 = length(vector);
			if (num3 > m_WpnSettings.m_RangeMaximal)
			{
				continue;
			}
			float num4 = Vector3.Dot(m_AimDir, vector / num3);
			if (!(num4 < m_ThresholdInFOV) && IsTargetVisible(agentHuman2))
			{
				num3 = 1f - num3 / m_WpnSettings.m_RangeMaximal;
				float num5 = (num4 + 1f) / 2f;
				float num6 = 3f * num3 + num5;
				if (num6 > num2)
				{
					agentHuman = agentHuman2;
					num2 = num6;
					num = num4;
				}
			}
		}
		if (agentHuman != null)
		{
			m_Target.m_Agent = agentHuman;
			m_Target.m_LastVisiblePos = agentHuman.TransformTarget.position;
			m_Target.m_LastVisibleTime = CurrTime;
			m_Target.m_InFOV = true;
			m_Target.m_InSights = num > m_ThresholdInSight;
			m_Target.m_InRange = true;
			m_Target.m_IsVisible = true;
		}
	}

	protected virtual bool IsTargetValid(AgentHuman T)
	{
		return !T.IsPlayer;
	}

	protected virtual bool IsTargetVisible(AgentHuman T)
	{
		Vector3 position = T.TransformTarget.position;
		Vector3 position2 = m_PartsSettings.m_Neck.position;
		Vector3 vector = position - position2;
		float num = length(vector);
		List<HitInfo> list = HitDetection.RayCast(position2, vector / num, num);
		foreach (HitInfo item in list)
		{
			if (!item.data.collider.isTrigger && (item.hitZone == null || !(item.hitZone is HitZoneEffects)))
			{
				return false;
			}
		}
		return true;
	}

	private void UpdateReload(float DeltaTime)
	{
		if (m_ReloadTime > 0f && (m_ReloadTime -= DeltaTime) <= 0f)
		{
			m_AmmoClip = Mathf.Min(m_AmmoRemaining, m_WpnSettings.m_AmmoClip);
			m_AmmoRemaining -= m_AmmoClip;
		}
	}

	private void UpdateShooting(float DeltaTime)
	{
		if (m_ShotEffectTime > 0f && (m_ShotEffectTime -= DeltaTime) <= 0f)
		{
			m_PartsSettings.m_Muzzle.gameObject.SetActive(false);
		}
		m_NextShotTime -= Time.deltaTime;
		if (m_Target.m_Agent != null && m_NextShotTime <= 0f && m_ReloadTime <= 0f && m_Target.m_InSights && m_AmmoClip > 0)
		{
			ShootAtTarget();
		}
	}

	private void ShootAtTarget()
	{
		Vector3 muzzleOriginalRot = m_MuzzleOriginalRot;
		Vector3 muzzleOriginalScale = m_MuzzleOriginalScale;
		muzzleOriginalRot.z += UnityEngine.Random.Range(-8f, 8f);
		muzzleOriginalScale += Vector3.one * UnityEngine.Random.Range(-0.05f, 0.05f);
		m_PartsSettings.m_Muzzle.gameObject.SetActive(true);
		m_PartsSettings.m_Muzzle.localRotation = Quaternion.Euler(muzzleOriginalRot);
		m_PartsSettings.m_Muzzle.localScale = muzzleOriginalScale;
		m_AudioFire.PlayOneShot(m_SndSettings.m_Fire);
		Vector3 inDir = MathUtils.RandomVectorInsideCone(m_AimDir, m_WpnSettings.m_Dispersion);
		ProjectileManager.Instance.SpawnProjectile(m_WpnSettings.m_ProjType, m_PartsSettings.m_Muzzle.position, inDir, m_ProjInitSettings);
		m_NextShotTime = m_WpnSettings.m_FireRate;
		m_ShotEffectTime = m_WpnSettings.m_FireEffectTime;
		if (--m_AmmoClip == 0)
		{
			if (m_AmmoRemaining > 0)
			{
				m_ReloadTime = m_WpnSettings.m_ReloadDelay;
			}
			else
			{
				float num = ((!MathUtils.InRange(m_AimSettings.m_VRange, 0f, 360f)) ? 0f : (m_AimSettings.m_VRange / 2f));
				m_HMotor.TargetAngle = m_InitHAngle;
				m_VMotor.TargetAngle = m_InitVAngle - (float)Math.PI / 180f * num;
				m_Deactivated = true;
				m_Target.m_Agent = null;
			}
		}
		if (m_LifeBar != null)
		{
			m_LifeBar.Value = (float)(m_AmmoClip + m_AmmoRemaining) / (float)m_AmmoTotal;
		}
	}

	private void OnDeactivation()
	{
		if (m_Deactivation.m_Explosion != null)
		{
			Vector3 inPosition = ((!(m_Deactivation.m_ExplosionOrigin != null)) ? m_PartsSettings.m_Base.position : m_Deactivation.m_ExplosionOrigin.position);
			Quaternion inRotation = ((!(m_Deactivation.m_ExplosionOrigin != null)) ? m_PartsSettings.m_Base.rotation : m_Deactivation.m_ExplosionOrigin.rotation);
			Explosion explosion = Mission.Instance.ExplosionCache.Get(m_Deactivation.m_Explosion, inPosition, inRotation);
			if (explosion != null)
			{
				explosion.causer = m_Owner;
				if (m_Deactivation.m_ExplosionDamageCoef > 0f)
				{
					explosion.damage = m_Deactivation.m_ExplosionDamageCoef * (float)GameplayData.Instance.EnemyHealth();
				}
				if (m_Deactivation.m_ExplosionRadius > 0f)
				{
					explosion.damageRadius = m_Deactivation.m_ExplosionRadiusMax;
				}
				if (m_Deactivation.m_ExplosionRadiusMax > 0f)
				{
					explosion.damageMaxRadius = m_Deactivation.m_ExplosionRadius;
				}
			}
		}
		if (m_Deactivation.m_Destroy != null)
		{
			UnityEngine.Object.Destroy(m_Deactivation.m_Destroy);
		}
	}
}
