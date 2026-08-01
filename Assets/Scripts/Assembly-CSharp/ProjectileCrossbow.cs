#define DEBUG
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Weapons/ProjectileCrossbow")]
public class ProjectileCrossbow : Projectile
{
	[SerializeField]
	private ProjectileTrail m_ProjectileTrail;

	protected bool Hit;

	protected bool Processed;

	protected float Timer;

	protected float LastTrailNewPosTimer;

	protected Vector3 StartPos;

	private Renderer Render;

	private Vector3 GetTrailPos()
	{
		Vector3 position = base.transform.position;
		return position + (1f * base.transform.forward - 0.05f * base.transform.up + 0.11f * base.transform.right);
	}

	public override void ProjectileInit(Vector3 pos, Vector3 dir, ProjectileInitSettings inSettings)
	{
		base.ProjectileInit(pos, dir, inSettings);
		StartPos = pos;
		Timer = 0f;
		LastTrailNewPosTimer = 0f;
		Processed = false;
		Hit = false;
		if (m_ProjectileTrail != null)
		{
			m_ProjectileTrail.gameObject.SetActive(false);
		}
		if (m_ProjectileTrail != null)
		{
			m_ProjectileTrail.InitTrail(GetTrailPos());
			m_ProjectileTrail.gameObject.SetActive(true);
		}
		Render = GetComponentInChildren<Renderer>();
		if ((bool)Render)
		{
			Render.enabled = true;
		}
	}

	public override void ProjectileUpdate(float deltaTime)
	{
		deltaTime = TimeManager.Instance.GetRealDeltaTime();
		Timer += deltaTime;
		if (Processed)
		{
			return;
		}
		DebugUtils.Assert(!IsFinished());
		Vector3 vector = base.Transform.forward * Settings.Speed * deltaTime - Vector3.up * 1f * 9.81f * Timer * Timer * deltaTime;
		Vector3 position = base.Transform.position + vector;
		int layersMask = ~(ObjectLayerMask.IgnoreRaycast | ObjectLayerMask.IgnoreBullets);
		List<HitInfo> list = HitDetection.SphereCast(base.Transform.position - base.Transform.forward * 1f, 0.1f, base.Transform.forward, vector.magnitude + 1.6f, layersMask);
		float sqrMagnitude;
		foreach (HitInfo item in list)
		{
			RaycastHit data = item.data;
			sqrMagnitude = (data.point - StartPos).sqrMagnitude;
			if (data.transform == Settings.IgnoreTransform || sqrMagnitude < 1f)
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
			}
		}
		sqrMagnitude = (base.Transform.position - StartPos).magnitude;
		if (Hit || sqrMagnitude > 60f)
		{
			m_ProjectileTrail.FadeOut();
			Processed = true;
			Timer = 0f;
			if (base.Agent != null && base.Agent.IsPlayer)
			{
				Game.Instance.MissionResultData.RegisterShot(base.hitProcessed);
			}
		}
		else
		{
			base.Transform.position = position;
			if (m_ProjectileTrail != null)
			{
				m_ProjectileTrail.UpdateAddTrailPos(GetTrailPos());
			}
		}
	}

	public override void ProjectileDeinit()
	{
	}

	public override bool IsFinished()
	{
		if (Processed && Timer > 0.2f && (bool)Render)
		{
			Render.enabled = false;
		}
		if (Processed && Timer > 0.7f)
		{
			return true;
		}
		return false;
	}
}
