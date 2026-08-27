using System.Collections;
using UnityEngine;
using static Unity.Mathematics.math;

public class AnimStateDeath : AnimState
{
	private AgentActionDeath Action;

	private bool DontDisableNavMesh;

	public AnimStateDeath(Animation anims, AgentHuman owner)
		: base(anims, owner)
	{
	}

	public override void OnActivate(AgentAction action)
	{
		base.OnActivate(action);
		if (!Owner.BlackBoard.KeepMotion)
		{
			Owner.BlackBoard.MotionType = E_MotionType.None;
			Owner.BlackBoard.MoveDir = Vector3.zero;
			Owner.BlackBoard.Speed = 0f;
		}
		DontDisableNavMesh = true;
	}

	public override void OnDeactivate()
	{
		base.OnDeactivate();
	}

	public override void Reset()
	{
		Action.SetSuccess();
	}

	public override bool HandleNewAction(AgentAction action)
	{
		if (action is AgentActionDeath)
		{
			action.SetFailed();
			return true;
		}
		return false;
	}

	private void CompleteOffMeshLink()
	{
		if ((bool)Owner.NavMeshAgent && Owner.NavMeshAgent.isOnOffMeshLink)
		{
			float num = Mathf.Abs(Owner.NavMeshAgent.currentOffMeshLinkData.endPos.y - Owner.NavMeshAgent.currentOffMeshLinkData.startPos.y);
			if (num > 0.2f)
			{
				DontDisableNavMesh = true;
			}
			Owner.NavMeshAgent.CompleteOffMeshLink();
		}
	}

	protected override void Initialize(AgentAction action)
	{
		base.Initialize(action);
		Action = action as AgentActionDeath;
		bool flag = Action.WeaponType == E_WeaponType.Explosion || Action.WeaponType == E_WeaponType.Shotgun;
		CompleteOffMeshLink();
		Owner.BlackBoard.ContestBalance = -1f;
		if (Owner.AgentType == E_AgentType.Boss1)
		{
			TimeManager.Instance.SetTimeScale(0.14f, 0.2f, 0.5f, 10f);
		}
		else if (Owner.AgentType == E_AgentType.BossSanta)
		{
			TimeManager.Instance.SetTimeScale(0.25f, 0.2f, 0.5f, 3f);
		}
		if (Action.FromWeapon == E_WeaponID.AlienGun)
		{
			InitializeAlienDeath();
		}
		else if (!Owner.RagdollRoot || (!flag && Owner.BlackBoard.MotionType != E_MotionType.ActionPoint) || Action.BodyDisintegrated || Owner.WorldState.GetWSProperty(E_PropKey.BodyPose).GetBodyPose() != 0)
		{
			InitializeAnimation();
		}
		else
		{
			InitializeRagdoll();
		}
	}

	private void InitializeAnimation()
	{
		string deathAnim = Owner.AnimSet.GetDeathAnim(Action.BodyPart);
		if (Action.WeaponType == E_WeaponType.ReviveKit)
		{
			Owner.Dissolve(0f);
		}
		else
		{
			Owner.Dissolve(Animation[deathAnim].length);
		}
		CrossFade(deathAnim, 0.2f, PlayMode.StopAll);
		if (!Owner.BlackBoard.DontDeathAnimMove && (bool)Owner.NavMeshAgent && !Owner.IsActionPointOn)
		{
			Vector3 destination = Action.Impuls + Owner.Position;
			Owner.NavMeshAgent.speed = length(Action.Impuls) / 0.2f;
			Owner.NavMeshAgent.acceleration = 100f;
			Owner.NavMeshAgent.updatePosition = true;
			Owner.NavMeshAgent.autoRepath = false;
			Owner.NavMeshAgent.SetDestination(destination);
		}
		Owner.BlackBoard.MotionType = E_MotionType.Death;
		Owner.StartCoroutine(_DisableCollisions((!Action.BodyDisintegrated) ? Animation[deathAnim].length : 0f));
	}

	private void InitializeRagdoll()
	{
		Animation.Stop();
		if (Action.WeaponType == E_WeaponType.ReviveKit)
		{
			Owner.Dissolve(0f);
		}
		else
		{
			Owner.Dissolve(2.5f);
		}
		Owner.NavMeshAgent.Stop();
		Owner.NavMeshAgent.enabled = false;
		Owner.ToggleCollisions(false, true);
		Owner.EnableRagdoll(true);
		Vector3 force = ((Action.WeaponType != E_WeaponType.Explosion) ? (Action.Impuls + Vector3.up * length(Action.Impuls) * 0.5f) : ((Action.Impuls + Vector3.up) * 2000f));
		Owner.RigidBodyForce.AddForce(force);
		Owner.BlackBoard.MotionType = E_MotionType.Death;
	}

	private void InitializeAlienDeath()
	{
		Owner.NavMeshAgent.enabled = false;
		Owner.StartCoroutine(_SlowDownAnimation());
		Owner.Dissolve(0f);
		Owner.ToggleCollisions(false, false);
		Owner.BlackBoard.MotionType = E_MotionType.Death;
	}

	private IEnumerator _SlowDownAnimation()
	{
		float speed = 1f;
		while (speed > 0.1f)
		{
			speed -= 2.5f * Time.deltaTime;
			foreach (AnimationState state in Animation)
			{
				if (state.enabled)
				{
					state.speed = speed;
				}
			}
			yield return new WaitForEndOfFrame();
		}
	}

	private IEnumerator _DisableCollisions(float delay)
	{
		if (!DontDisableNavMesh && Owner.NavMeshAgent != null)
		{
			Owner.NavMeshAgent.enabled = false;
		}
		Owner.ToggleCollisions(false, true);
		yield return new WaitForSeconds(delay);
		Owner.ToggleCollisions(false, false);
	}
}
