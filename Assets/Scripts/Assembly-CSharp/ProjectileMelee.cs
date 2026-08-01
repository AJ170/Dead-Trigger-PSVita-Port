#define DEBUG
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Weapons/ProjectileMelee")]
public class ProjectileMelee : Projectile
{
	protected bool Hit;

	protected bool Processed;

	protected float Timer;

	protected Vector3 StartPos;

	protected static float ShotCollisionDist = 3f;

	public override float Damage()
	{
		float num = (base.Transform.position - StartPos).magnitude - 0.55f;
		if (num <= Settings.EffectiveRange)
		{
			return Settings.Damage;
		}
		return 0f;
	}

	public override void ProjectileInit(Vector3 pos, Vector3 dir, ProjectileInitSettings inSettings)
	{
		base.ProjectileInit(pos, dir, inSettings);
		StartPos = pos;
		Timer = 0f;
		Processed = false;
		Hit = false;
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
		Vector3 position = base.Transform.position + base.Transform.forward * ShotCollisionDist;
		int layersMask = ~(ObjectLayerMask.IgnoreRaycast | ObjectLayerMask.IgnoreBullets);
		float radius = 0.25f;
		if (Settings.Weapon == E_WeaponID.Propeller)
		{
			radius = 0.55f;
		}
		List<HitInfo> list = HitDetection.SphereCast(base.Transform.position, radius, base.Transform.forward, ShotCollisionDist, layersMask);
		List<ComponentEnemy> list2 = new List<ComponentEnemy>();
		foreach (HitInfo item in list)
		{
			RaycastHit data = item.data;
			if (data.transform == Settings.IgnoreTransform)
			{
				continue;
			}
			base.Transform.position = data.point;
			if (Mathf.Approximately(Damage(), 0f))
			{
				continue;
			}
			HitZone hitZone = item.hitZone;
			if ((bool)hitZone)
			{
				ComponentEnemy componentEnemy = ((hitZone.HitZoneOwner == null) ? null : (hitZone.HitZoneOwner as ComponentEnemy));
				if ((bool)componentEnemy)
				{
					if (list2.Contains(componentEnemy))
					{
						continue;
					}
					list2.Add(componentEnemy);
				}
				hitZone.OnProjectileHit(this);
			}
			else
			{
				data.transform.SendMessage("OnProjectileHit", this, SendMessageOptions.DontRequireReceiver);
			}
			if (base.ignoreThisHit)
			{
				base.ignoreThisHit = false;
				continue;
			}
			// FluidSurface removed during Vita port; impact-effect spawn restored below.
			/*FluidSurface component = data.collider.GetComponent<FluidSurface>();
			if (component != null)
			{
				component.AddDropletAtWorldPos(data.point, 0.3f, 0.15f);
			}*/
			if (!data.collider.isTrigger)
			{
				SemanticMaterialManager.Instance.SpawnProjectileImpactEffect(base.ProjectileType, data);
				position = data.point;
				Hit = true;
				if (base.GetComponent<Renderer>() != null)
				{
					base.GetComponent<Renderer>().enabled = false;
				}
			}
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
