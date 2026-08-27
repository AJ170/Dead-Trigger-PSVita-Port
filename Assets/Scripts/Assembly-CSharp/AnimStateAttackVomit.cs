using System;
using UnityEngine;
using static Unity.Mathematics.math;

public class AnimStateAttackVomit : AnimState
{
	private enum E_State
	{
		E_PREPARING_FOR_ATTACK = 0,
		E_ATTACKING = 1
	}

	private const float MoveTime = 0.2f;

	private AgentActionAttackVomit Action;

	private ComponentEnemy EnemyComponent;

	private Vector3 StartPosition;

	private Vector3 FinalPosition;

	private float CurrentMoveTime;

	private bool PositionOK;

	private E_State State;

	private float timeOfAttack;

	private bool damageCaused;

	private float EndOfStateTime;

	private float PlayAnimTime;

	public AnimStateAttackVomit(Animation anims, AgentHuman owner)
		: base(anims, owner)
	{
	}

	public override void OnActivate(AgentAction action)
	{
		base.OnActivate(action);
		Owner.BlackBoard.ReactOnHits = false;
		Owner.BlackBoard.BusyAction = true;
		Owner.BlackBoard.Desires.WeaponTriggerOn = false;
		Owner.BlackBoard.MotionType = E_MotionType.Vomit;
		Owner.BlackBoard.MoveDir = Vector3.zero;
		Owner.BlackBoard.Speed = 0f;
		timeOfAttack = 0f;
		damageCaused = false;
	}

	public override void OnDeactivate()
	{
		Owner.BlackBoard.ReactOnHits = true;
		Owner.BlackBoard.BusyAction = false;
		Owner.BlackBoard.PrevMotionType = Owner.BlackBoard.MotionType;
		Owner.BlackBoard.MotionType = E_MotionType.None;
		Action.SetSuccess();
		Action = null;
		base.OnDeactivate();
	}

	public override void Reset()
	{
		Action.SetSuccess();
		Action = null;
		CurrentMoveTime = 0f;
		base.Reset();
	}

	public override void Update()
	{
		if (Owner.BlackBoard.DangerousEnemy == null || !Owner.BlackBoard.DangerousEnemy.IsAlive)
		{
			Release();
			return;
		}
		Owner.BlackBoard.Desires.Rotation.SetLookRotation(Owner.BlackBoard.DangerousEnemy.GameObject.transform.position - Transform.position);
		if (State == E_State.E_PREPARING_FOR_ATTACK)
		{
			if (!PositionOK)
			{
				CurrentMoveTime += Time.deltaTime;
				if (CurrentMoveTime >= 0.2f)
				{
					CurrentMoveTime = 0.2f;
					PositionOK = true;
				}
				float num = Mathf.Min(1f, CurrentMoveTime / 0.2f);
				Vector3 vector = Mathfx.Sinerp(StartPosition, FinalPosition, num);
				if (num > 0.9f && !Move(vector - Transform.position))
				{
					PositionOK = true;
				}
			}
			else
			{
				State = E_State.E_ATTACKING;
				PlayAnim();
				timeOfAttack = Time.realtimeSinceStartup;
			}
		}
		if (State == E_State.E_ATTACKING && Time.realtimeSinceStartup - timeOfAttack > 0.4f && !damageCaused && (bool)Owner.BlackBoard.DangerousEnemy && Owner.BlackBoard.DistanceToTarget <= Owner.BlackBoard.VomitRangeMax && Owner.BlackBoard.DistanceToTarget >= Owner.BlackBoard.VomitRangeMin && Owner.WorldState.GetWSProperty(E_PropKey.EnemyAheadOfMe).GetBool())
		{
			damageCaused = true;
			if (Owner.AgentType == E_AgentType.Boss1_small || Owner.AgentType == E_AgentType.Boss1)
			{
				ThrowVomit(E_ProjectileType.VomitGreen);
			}
			else if (Owner.AgentType == E_AgentType.BossSanta)
			{
				ThrowVomit(E_ProjectileType.SantaPresent);
			}
			else
			{
				ThrowVomit(E_ProjectileType.VomitRed);
			}
		}
		if (State == E_State.E_ATTACKING && Time.realtimeSinceStartup - timeOfAttack > PlayAnimTime)
		{
			Release();
		}
	}

	public void ThrowVomit(E_ProjectileType ProjectileType)
	{
		EnemyComponent.PlayAttackSound(E_WeaponAction.Vomit);
		ProjectileInitSettings projectileInitSettings = new ProjectileInitSettings();
		projectileInitSettings.Agent = Owner;
		projectileInitSettings.IgnoreTransform = Owner.GameObject.transform;
		projectileInitSettings.Speed = 15f;
		projectileInitSettings.Damage = Owner.BlackBoard.AttackSetup.VomitAttackDamage;
		projectileInitSettings.EffectiveRange = 10f;
		projectileInitSettings.MaxRange = 0.6f;
		projectileInitSettings.Destination = Owner.BlackBoard.DangerousEnemy.TransformEye.position;
		Vector3 position = Owner.TransformEye.position;
		Vector3 inDir = projectileInitSettings.Destination - position;
		projectileInitSettings.EffectiveRange = length(inDir);
		inDir.y += projectileInitSettings.EffectiveRange;
		ProjectileManager.Instance.SpawnProjectile(ProjectileType, position, inDir, projectileInitSettings);
	}

	public override void Release()
	{
		Transform.parent = null;
		base.Release();
	}

	public override bool HandleNewAction(AgentAction action)
	{
		if (action is AgentActionUse)
		{
			if (Action != null)
			{
				action.SetFailed();
			}
		}
		else if (action is AgentActionInjury)
		{
			if (!PlayingInjury())
			{
				Animation.Stop();
			}
			PlayInjuryAnimation(action as AgentActionInjury);
			return true;
		}
		return false;
	}

	private void PlayAnim()
	{
		if (Owner.debugAnims)
		{
			Debug.Log("PlayAnim() " + Time.timeSinceLevelLoad);
		}
		string weaponAnim = Owner.AnimSet.GetWeaponAnim(Action.WeaponAction);
		PlayAnimTime = Animation[weaponAnim].length + 0.1f;
		CrossFade(weaponAnim, 0.25f, PlayMode.StopSameLayer);
	}

	protected override void Initialize(AgentAction action)
	{
		base.Initialize(action);
		Action = action as AgentActionAttackVomit;
		Owner.BlackBoard.MotionType = E_MotionType.Vomit;
		EnemyComponent = Owner.GetComponent<ComponentEnemy>();
		if (EnemyComponent == null)
		{
			throw new MemberAccessException("ComponentEnemy not found!");
		}
		StartPosition = Transform.position;
		if ((bool)Owner.BlackBoard.VisibleTarget)
		{
			Vector3 vector = Owner.BlackBoard.VisibleTarget.Position - Owner.Position;
			vector.Normalize();
			FinalPosition = Transform.position + vector;
		}
		else
		{
			FinalPosition = Transform.position;
		}
		CurrentMoveTime = 0f;
		PositionOK = false;
		State = E_State.E_PREPARING_FOR_ATTACK;
	}
}
