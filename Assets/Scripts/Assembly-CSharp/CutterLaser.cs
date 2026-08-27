using System;
using UnityEngine;
using static Unity.Mathematics.math;

[RequireComponent(typeof(AudioSource))]
[AddComponentMenu("Items/Cutter - Laser")]
public class CutterLaser : MonoBehaviour
{
	private enum State
	{
		Activating = 0,
		Active = 1,
		Deactivating = 2,
		Inactive = 3
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

	private AgentHuman m_Owner;

	private Transform m_Transform;

	public Transform m_SpinningPart;

	private AudioSource m_Audio;

	private State m_State;

	private MappingChange m_LifeBar;

	public float m_SpinDuration = 1.5f;

	public float m_Radius = 3f;

	public float m_AverageKilledSec = 2f;

	public float m_AverageKilledNum = 10f;

	private float m_DamageTotal;

	private float m_DamageRemaining;

	private float m_DamageSensor;

	public AudioClip m_SndActivating;

	public AudioClip m_SndActive;

	public AudioClip m_SndDeactivating;

	private DirectionalMeshSensor[] m_Lasers;

	private float m_LayersHeightMin;

	private float m_LayersHeightMax;

	public DeactivationSettings m_Deactivation = new DeactivationSettings();

	private void Awake()
	{
		m_Transform = base.transform;
	}

	private void Init(Item.InitData Data)
	{
		m_Owner = Data.Owner;
		m_Transform.position = Data.Pos;
		m_Transform.rotation.SetLookRotation(Data.Dir);
		SemanticMaterialManager.Instance.SpawnPlacementEffect(Data.Pos);
	}

	private void Start()
	{
		GameObject gameObject = base.gameObject;
		MappingChange[] componentsInChildren = gameObject.GetComponentsInChildren<MappingChange>();
		if (componentsInChildren != null && componentsInChildren.Length > 0)
		{
			m_LifeBar = componentsInChildren[0];
			m_LifeBar.Value = 1f;
			m_LifeBar.MappingChangeType = MappingChange.Type.BasedOnValue;
		}
		m_State = State.Activating;
		m_Audio = gameObject.GetComponent<AudioSource>();
		m_Audio.clip = m_SndActivating;
		m_Audio.loop = false;
		m_Audio.Play();
		m_Lasers = gameObject.GetComponentsInChildren<DirectionalMeshSensor>();
		m_LayersHeightMin = float.MaxValue;
		m_LayersHeightMax = float.MinValue;
		DirectionalMeshSensor[] lasers = m_Lasers;
		foreach (DirectionalMeshSensor directionalMeshSensor in lasers)
		{
			directionalMeshSensor.m_MaxLength = m_Radius;
			directionalMeshSensor.gameObject.SetActive(false);
			float y = directionalMeshSensor.gameObject.transform.position.y;
			m_LayersHeightMin = Mathf.Min(y, m_LayersHeightMin);
			m_LayersHeightMax = Mathf.Max(y, m_LayersHeightMax);
		}
		m_LayersHeightMin -= 0.3f;
		m_LayersHeightMax += 0.3f;
		float num = GameplayData.Instance.EnemyHealth();
		m_DamageTotal = num * m_AverageKilledNum;
		m_DamageRemaining = m_DamageTotal;
		m_DamageSensor = num / m_AverageKilledSec;
	}

	private void Update()
	{
		if (m_State == State.Inactive)
		{
			return;
		}
		float num = (float)Math.PI * 2f / m_SpinDuration;
		switch (m_State)
		{
		case State.Activating:
			if (m_Audio.isPlaying)
			{
				num *= m_Audio.time / m_Audio.clip.length;
				break;
			}
			m_State = State.Active;
			m_Audio.clip = m_SndActive;
			m_Audio.loop = true;
			m_Audio.Play();
			ToggleLasers(true);
			break;
		case State.Active:
			if (m_DamageRemaining > 0f)
			{
				ApplyDamage(Time.deltaTime);
				break;
			}
			m_State = State.Deactivating;
			m_Audio.clip = m_SndDeactivating;
			m_Audio.loop = false;
			m_Audio.Play();
			ToggleLasers(false);
			break;
		case State.Deactivating:
			if (m_Audio.isPlaying)
			{
				num *= 1f - m_Audio.time / m_Audio.clip.length;
				break;
			}
			m_State = State.Inactive;
			Invoke("OnDeactivation", m_Deactivation.m_Delay);
			break;
		}
		m_SpinningPart.Rotate(m_SpinningPart.up, num * Time.deltaTime);
	}

	private void ToggleLasers(bool Enable)
	{
		DirectionalMeshSensor[] lasers = m_Lasers;
		foreach (DirectionalMeshSensor directionalMeshSensor in lasers)
		{
			directionalMeshSensor.gameObject.SetActive(Enable);
		}
	}

	private void ApplyDamage(float DeltaTime)
	{
		float num = 0f;
		float radius = GetRadius();
		DirectionalMeshSensor[] lasers = m_Lasers;
		foreach (DirectionalMeshSensor directionalMeshSensor in lasers)
		{
			float Damage = m_DamageSensor * DeltaTime;
			if (directionalMeshSensor.HitFound && ApplyDamage(directionalMeshSensor.HitInfo, ref Damage))
			{
				num += Damage;
			}
			directionalMeshSensor.m_MaxLength = radius;
		}
		m_DamageRemaining -= num;
		if (m_LifeBar != null)
		{
			m_LifeBar.Value = m_DamageRemaining / m_DamageTotal;
		}
	}

	private bool ApplyDamage(HitInfo Hit, ref float Damage)
	{
		if (Hit.hitZone != null)
		{
			AgentHuman firstComponentUpward = Hit.hitZone.GameObj.GetFirstComponentUpward<AgentHuman>();
			if (firstComponentUpward != null)
			{
				Damage = Mathf.Min(Damage, firstComponentUpward.BlackBoard.Health);
				SemanticMaterialManager.Instance.SpawnProjectileImpactEffect(E_ProjectileType.None, Hit.data);
			}
			Hit.hitZone.OnReceiveRangeDamage(m_Owner, Damage, Vector3.zero, E_WeaponID.None, E_WeaponType.Laser);
			return true;
		}
		return false;
	}

	private float GetRadius()
	{
		float result = m_Radius;
		if (m_Owner != null && m_Owner.IsAlive)
		{
			Vector3 position = m_Owner.Position;
			Vector3 vector = position - m_Transform.position;
			float num = vector.x * vector.x + vector.z * vector.z;
			if (num <= m_Radius * m_Radius)
			{
				float num2 = 2f;
				if (!(position.y + num2 < m_LayersHeightMin) && !(position.y > m_LayersHeightMax))
				{
					result = sqrt(num) - 0.3f;
					result = ((!(result < 0.2f)) ? result : 0f);
				}
			}
		}
		return result;
	}

	private void OnDeactivation()
	{
		if (m_Deactivation.m_Explosion != null)
		{
			Vector3 inPosition = ((!(m_Deactivation.m_ExplosionOrigin != null)) ? m_Transform.position : m_Deactivation.m_ExplosionOrigin.position);
			Quaternion inRotation = ((!(m_Deactivation.m_ExplosionOrigin != null)) ? m_Transform.rotation : m_Deactivation.m_ExplosionOrigin.rotation);
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
