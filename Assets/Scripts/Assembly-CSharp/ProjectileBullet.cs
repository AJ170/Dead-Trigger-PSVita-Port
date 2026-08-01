#define DEBUG
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Weapons/ProjectileBullet")]
public class ProjectileBullet : Projectile
{
	[SerializeField]
	private ProjectileTrail m_ProjectileTrail;

	private bool m_ScaleProjectile = true;

	protected bool Hit;

	protected bool Processed;

	protected float Timer;

	protected Vector3 StartPos;

	protected static float ShotCollisionDist = 60f;

	public override float Damage()
	{
		float num = (base.Transform.position - StartPos).magnitude - 0.55f;
		if (num <= Settings.EffectiveRange)
		{
			return Settings.Damage;
		}
		if (num >= Settings.MaxRange)
		{
			return Settings.MaxRangeDamage;
		}
		float num2 = Mathf.Clamp((num - Settings.EffectiveRange) / (Settings.MaxRange - Settings.EffectiveRange), 0f, 1f);
		return num2 * Settings.MaxRangeDamage + (1f - num2) * Settings.Damage;
	}

	public override void ProjectileInit(Vector3 pos, Vector3 dir, ProjectileInitSettings inSettings)
	{
		base.ProjectileInit(pos, dir, inSettings);
		if (m_ScaleProjectile)
		{
			base.Transform.localScale = new Vector3(1f, 1f, 0f);
		}
		StartPos = pos;
		Timer = 0f;
		Processed = false;
		Hit = false;
		if (m_ProjectileTrail != null)
		{
			m_ProjectileTrail.gameObject.SetActive(false);
		}
		if (base.GetComponent<Renderer>() != null)
		{
			base.GetComponent<Renderer>().enabled = true;
		}
	}

	public override void ProjectileUpdate(float deltaTime)
	{
		Timer += deltaTime;
		if (Processed)
		{
			return;
		}
		DebugUtils.Assert(!IsFinished());
		DebugUtils.Assert(!Hit);
		if (base.Transform.localScale.z != 1f)
		{
			base.Transform.localScale = new Vector3(1f, 1f, Mathf.Min(1f, base.Transform.localScale.z + Time.deltaTime * 8f));
		}
		Vector3 position = base.Transform.position + base.Transform.forward * ShotCollisionDist;
		int layersMask = ~(ObjectLayerMask.IgnoreRaycast | ObjectLayerMask.IgnoreBullets);
		List<HitInfo> list = HitDetection.RayCast(base.Transform.position, base.Transform.forward, ShotCollisionDist, layersMask);
		foreach (HitInfo item in list)
		{
			RaycastHit data = item.data;
			if (data.transform == Settings.IgnoreTransform)
			{
				continue;
			}
			base.Transform.position = data.point;
			HitZone hitZone = item.hitZone;
			if ((bool)hitZone)
			{
				hitZone.OnProjectileHit(this);
			}
			else
			{
				data.transform.SendMessage("OnProjectileHit", this, SendMessageOptions.DontRequireReceiver);
			}
			Damage();
			if (base.ignoreThisHit)
			{
				base.ignoreThisHit = false;
				continue;
			}
			/* "FluidSurface" doesnt exist in the project files
			FluidSurface component = data.collider.GetComponent<FluidSurface>();
			if (component != null)
			{
				component.AddDropletAtWorldPos(data.point, 0.3f, 0.15f);
			}*/
			if (!data.collider.isTrigger)
			{
				SemanticMaterialManager.Instance.SpawnProjectileImpactEffect(base.ProjectileType, data);
			}
			if (data.collider.isTrigger)
			{
				continue;
			}
			position = data.point;
			Hit = true;
			if (base.GetComponent<Renderer>() != null)
			{
				base.GetComponent<Renderer>().enabled = false;
			}
			break;
		}
		if (base.Agent != null && base.Agent.IsPlayer)
		{
			Game.Instance.MissionResultData.RegisterShot(base.hitProcessed);
		}
		Processed = true;
		base.Transform.position = position;
	}

	public override void ProjectileDeinit()
	{
	}

	public override bool IsFinished()
	{
		return Processed;
	}
}
