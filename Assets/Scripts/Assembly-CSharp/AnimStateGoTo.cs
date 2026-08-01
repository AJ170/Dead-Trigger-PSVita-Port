using UnityEngine;

public class AnimStateGoTo : AnimState
{
	private class ActionPointData
	{
		public enum State
		{
			MoveToStart = 0,
			MoveToEnd = 1
		}

		public ActionPoint actionPoint;

		public ActionPoint.AnimData currentData;

		public UnityEngine.AI.OffMeshLinkData offMeshLinkData;

		public float timeActionPointEnd;

		public float prevCostOverride = -1f;

		public bool prevInvulnerable;

		public State state;

		public AnimationClip GetAnim()
		{
			return currentData.m_Anim;
		}

		public Vector3 GetLookDir()
		{
			return (offMeshLinkData.endPos - offMeshLinkData.startPos).normalized;
		}

		public float GetLength()
		{
			return (offMeshLinkData.endPos - offMeshLinkData.startPos).magnitude;
		}

		public bool IsStillActive(AgentHuman owner)
		{
			return actionPoint != null && owner.NavMeshAgent != null && owner.NavMeshAgent.isOnOffMeshLink;
		}

		public Vector3 GetStartPos()
		{
			return (!(actionPoint != null)) ? Vector3.zero : offMeshLinkData.startPos;
		}

		public Vector3 GetEndPos()
		{
			return (!(actionPoint != null)) ? Vector3.zero : offMeshLinkData.endPos;
		}

		public float GetMoveSpeed()
		{
			if (currentData.m_MoveSpeed > 0f)
			{
				return currentData.m_MoveSpeed;
			}
			float speed = 0f;
			GetVelocity(out speed);
			return speed;
		}

		public Vector3 GetVelocity(out float speed)
		{
			if (actionPoint == null)
			{
				speed = 0f;
				return Vector3.zero;
			}
			Vector3 result = offMeshLinkData.endPos - offMeshLinkData.startPos;
			speed = ((!actionPoint.enabled) ? GetMoveSpeed() : (result.magnitude / GetAnim().length));
			return result;
		}

		public float GetTime(AgentHuman owner, float moveSpeed)
		{
			if (actionPoint.enabled)
			{
				return GetAnim().length - 0.2f;
			}
			float magnitude = (GetStartPos() - owner.Transform.position).magnitude;
			magnitude += GetLength();
			float num = magnitude / moveSpeed;
			return num - 0.1f;
		}
	}

	private AgentActionGoTo Action;

	private float MaxSpeed;

	private string AnimNameMove;

	private string AnimNameAP;

	private float TimeToFinishRotateAction;

	private AgentActionRotate RotateAction;

	private ActionPointData actionPointData = new ActionPointData();

	public AnimStateGoTo(Animation anims, AgentHuman owner)
		: base(anims, owner)
	{
	}

	protected override void Initialize(AgentAction action)
	{
		base.Initialize(action);
		Action = action as AgentActionGoTo;
		if (!SetTargetLocation(Action.FinalPosition))
		{
			Debug.LogWarning(ToString() + " SetTargetLocation Failed - " + Action.FinalPosition);
			Release();
			return;
		}
		Owner.NavMeshAgent.updateRotation = Action.UseNavMeshAgentRotation;
		Owner.BlackBoard.MotionType = Action.Motion;
		Owner.BlackBoard.MoveType = Action.MoveType;
		Owner.BlackBoard.Speed = GetMoveSpeed(Action.Motion);
		Owner.NavMeshAgent.speed = Owner.BlackBoard.Speed;
		if (Action.ReselectMoveAnim)
		{
			AnimNameMove = Owner.AnimSet.GetMoveAnim();
		}
	}

	public override void OnActivate(AgentAction action)
	{
		Owner.BlackBoard.Velocity = Vector3.zero;
		base.OnActivate(action);
		AnimNameMove = Owner.AnimSet.GetMoveAnim();
		AnimNameAP = null;
	}

	public override void OnDeactivate()
	{
		StopActionPoint(true);
		Owner.NavMeshAgent.updateRotation = false;
		if (RotateAction != null)
		{
			RotateAction.SetSuccess();
			RotateAction = null;
		}
		if (Owner.IsAlive)
		{
			Owner.BlackBoard.PrevMotionType = Owner.BlackBoard.MotionType;
			Owner.BlackBoard.MotionType = E_MotionType.None;
		}
		if (!Owner.BlackBoard.KeepMotion)
		{
			if (Owner.NavMeshAgent.enabled)
			{
				Owner.NavMeshAgent.Stop();
			}
			Owner.BlackBoard.MoveDir = Vector3.zero;
			Owner.BlackBoard.Speed = 0f;
			Owner.BlackBoard.VelocityPrevious = Owner.BlackBoard.Velocity;
			Owner.BlackBoard.Velocity = Vector3.zero;
		}
		Action.SetSuccess();
		Action = null;
		AnimNameMove = null;
		AnimNameAP = null;
		base.OnDeactivate();
	}

	public override void Reset()
	{
		StopActionPoint(true);
		Action.SetSuccess();
		Action = null;
		if (RotateAction != null)
		{
			RotateAction.SetSuccess();
			RotateAction = null;
		}
		base.Reset();
	}

	private bool GetActionPoint()
	{
		UnityEngine.AI.NavMeshAgent navMeshAgent = Owner.NavMeshAgent;
		if (navMeshAgent == null) // || !navMeshAgent.isOnOffMeshLink)
		{
			navMeshAgent.updatePosition = true;   // <-- see note below, this line still needs the null-guard
			if (actionPointData.actionPoint != null)
			{
				actionPointData.actionPoint = null;
			}
			return false;
		}
		if (navMeshAgent.currentOffMeshLinkData.offMeshLink == null)
		{
			actionPointData.actionPoint = null;
			return false;
		}
		actionPointData.actionPoint = navMeshAgent.currentOffMeshLinkData.offMeshLink.gameObject.GetComponent<ActionPoint>();
		if (actionPointData.actionPoint == null)
		{
			return false;
		}
		actionPointData.offMeshLinkData = navMeshAgent.currentOffMeshLinkData;
		if (!actionPointData.actionPoint.enabled)
		{
			actionPointData.currentData = new ActionPoint.AnimData();
			actionPointData.currentData.m_Anim = Owner.Animation.GetClip(Owner.AnimSet.GetMoveAnim());
			actionPointData.currentData.m_MoveSpeed = GetMoveSpeed(GetMotionType());
			Owner.NavMeshAgent.updatePosition = false;
			return true;
		}
		E_MotionType motionType = GetMotionType();
		if (motionType == E_MotionType.Walk || motionType != E_MotionType.Run)
		{
			actionPointData.currentData = actionPointData.actionPoint.m_AnimMove;
		}
		else
		{
			actionPointData.currentData = actionPointData.actionPoint.m_AnimRun;
		}
		return true;
	}

	private void StartActionPoint()
	{
		Owner.BlackBoard.ActionPointOn = true;
		Owner.BlackBoard.MotionType = GetMotionType();
		Owner.NavMeshAgent.speed = actionPointData.GetMoveSpeed();
		actionPointData.state = ActionPointData.State.MoveToStart;
		actionPointData.prevInvulnerable = Owner.IsInvulnerable;
		Owner.BlackBoard.Invulnerable = actionPointData.actionPoint.AgentInvulnerable;
		Owner.BlackBoard.ReactOnHits = false;
		actionPointData.prevCostOverride = actionPointData.offMeshLinkData.offMeshLink.costOverride;
		actionPointData.offMeshLinkData.offMeshLink.costOverride = 10f;
		Owner.NavMeshAgent.updateRotation = false;
		if (Owner.debugAnims)
		{
			Debug.Log("StartActionPoint() : actionPoint.name=" + actionPointData.actionPoint.name + ", enabled=" + actionPointData.actionPoint.enabled + ", Owner.name=" + Owner.name + ", time=" + Time.timeSinceLevelLoad);
		}
		actionPointData.timeActionPointEnd = Time.timeSinceLevelLoad + actionPointData.GetTime(Owner, GetMoveSpeed(GetMotionType()));
	}

	private void UpdateActionPoint()
	{
		if (!Owner.IsActionPointOn)
		{
			if (!actionPointData.IsStillActive(Owner) && GetActionPoint())
			{
				StartActionPoint();
			}
			return;
		}
		if (Time.timeSinceLevelLoad >= actionPointData.timeActionPointEnd || (actionPointData.GetEndPos() - Owner.Transform.position).sqrMagnitude < 0.0025000002f)
		{
			StopActionPoint(false);
		}
		if (Owner.debugAnims)
		{
			float magnitude = Owner.NavMeshAgent.velocity.magnitude;
			Debug.Log("Update AP: time=" + Time.timeSinceLevelLoad + ", NavMesh Speed=" + Owner.NavMeshAgent.speed + ", realSpeed=" + magnitude);
		}
	}

	private void StopActionPoint(bool force)
	{
		if (Owner.debugAnims)
		{
			Debug.Log("StopActionPoint() : Owner.name=" + Owner.name + ", time=" + Time.timeSinceLevelLoad);
		}
		if (force)
		{
			actionPointData.actionPoint = null;
		}
		if ((bool)actionPointData.offMeshLinkData.offMeshLink)
		{
			actionPointData.offMeshLinkData.offMeshLink.costOverride = actionPointData.prevCostOverride;
		}
		actionPointData.prevCostOverride = -1f;
		Owner.BlackBoard.Invulnerable = actionPointData.prevInvulnerable;
		actionPointData.prevInvulnerable = false;
		Owner.BlackBoard.ReactOnHits = true;
		Owner.NavMeshAgent.updateRotation = Action.UseNavMeshAgentRotation;
		Owner.NavMeshAgent.updatePosition = true;
		Owner.BlackBoard.ActionPointOn = false;
		actionPointData.timeActionPointEnd = 0f;
		if (Owner.IsAlive)
		{
			Owner.BlackBoard.MotionType = Action.Motion;
			Owner.BlackBoard.Speed = GetMoveSpeed(Action.Motion);
			if (Owner.NavMeshAgent.isOnOffMeshLink)
			{
				Owner.NavMeshAgent.CompleteOffMeshLink();
			}
		}
	}

	public override void Update()
	{
		if (Time.deltaTime < float.Epsilon)
		{
			return;
		}
		UpdateRotation();
		if (RotateAction != null && TimeToFinishRotateAction < Time.timeSinceLevelLoad)
		{
			RotateAction.SetSuccess();
			RotateAction = null;
		}
		Owner.BlackBoard.MotionType = GetMotionType();
		Owner.BlackBoard.MoveType = GetMoveType();
		Owner.BlackBoard.Speed = GetMoveSpeed(Owner.BlackBoard.MotionType);
		Owner.NavMeshAgent.speed = Owner.BlackBoard.Speed;
		if (Owner.IsActionPointOn)
		{
			Vector3 vector = default(Vector3);
			if (actionPointData.state == ActionPointData.State.MoveToStart)
			{
				vector = actionPointData.GetStartPos() - Owner.Transform.position;
				if (vector.sqrMagnitude < 0.010000001f)
				{
					actionPointData.state = ActionPointData.State.MoveToEnd;
					actionPointData.timeActionPointEnd = Time.timeSinceLevelLoad + actionPointData.GetTime(Owner, GetMoveSpeed(GetMotionType()));
				}
				vector.Normalize();
			}
			else
			{
				vector = (actionPointData.GetEndPos() - Owner.Transform.position).normalized;
			}
			vector *= Owner.BlackBoard.Speed;
			Owner.Transform.position += vector * Time.deltaTime;
			Owner.NavMeshAgent.velocity = vector;
			Owner.BlackBoard.Velocity = vector;
		}
		else if (PlayingInjury())
		{
			Owner.BlackBoard.MotionType = GetMotionType();
			Owner.BlackBoard.MoveType = GetMoveType();
			Owner.BlackBoard.Speed = Owner.NavMeshAgent.speed * 0.8f;
			Owner.BlackBoard.Velocity = Owner.NavMeshAgent.velocity * 0.8f;
			Owner.NavMeshAgent.speed = Owner.BlackBoard.Speed;
			Owner.NavMeshAgent.velocity = Owner.BlackBoard.Velocity;
		}
		UpdateActionPoint();
		if (!PlayingInjury())
		{
			PlayAnim(1f);
		}
		if (!Owner.NavMeshAgent.pathPending && Owner.NavMeshAgent.remainingDistance < Action.MinDistance && !Owner.BlackBoard.ActionPointOn)
		{
			if (Owner.debugAI)
			{
				Debug.Log("Goto terminated " + Owner.NavMeshAgent.remainingDistance + " < " + Action.MinDistance);
			}
			Release();
		}
	}

	public override bool HandleNewAction(AgentAction action)
	{
		if (action is AgentActionGoTo)
		{
			if (Action != null)
			{
				Action.SetSuccess();
			}
			SetFinished(false);
			Initialize(action);
			return true;
		}
		if (action is AgentActionInjury)
		{
			if (!PlayingInjury() && !Owner.IsActionPointOn)
			{
				Animation.Stop();
			}
			PlayInjuryAnimation(action as AgentActionInjury);
			return true;
		}
		if (action is AgentActionDeath)
		{
			if ((action as AgentActionDeath).FromWeapon == E_WeaponID.AlienGun)
			{
				Owner.BlackBoard.KeepMotion = true;
			}
		}
		else
		{
			if (action is AgentActionAttack)
			{
				string weaponAnim = Owner.AnimSet.GetWeaponAnim(E_WeaponAction.Fire);
				Animation[weaponAnim].blendMode = AnimationBlendMode.Additive;
				if (Animation.IsPlaying(weaponAnim))
				{
					Animation.Stop(weaponAnim);
					Animation.Play(weaponAnim);
				}
				else
				{
					Animation.Blend(weaponAnim, 1f, 0.1f);
				}
				action.SetSuccess();
				return true;
			}
			if (action is AgentActionReload)
			{
				string weaponAnim2 = Owner.AnimSet.GetWeaponAnim(E_WeaponAction.Reload);
				Animation[weaponAnim2].layer = 3;
				Animation[weaponAnim2].blendMode = AnimationBlendMode.Additive;
				Blend(weaponAnim2, 0.1f);
				action.SetSuccess();
				return true;
			}
			if (action is AgentActionRotate)
			{
				RotateAction = action as AgentActionRotate;
				string rotateAnim = Owner.AnimSet.GetRotateAnim(RotateAction.Rotation, RotateAction.Angle);
				if (rotateAnim != null && !Animation.IsPlaying(rotateAnim))
				{
					Animation[rotateAnim].blendMode = AnimationBlendMode.Additive;
					Animation[rotateAnim].layer = 1;
					TimeToFinishRotateAction = Time.timeSinceLevelLoad + Animation[rotateAnim].length;
					Blend(rotateAnim, 0.1f);
				}
				return true;
			}
		}
		return false;
	}

	private void PlayAnim(float speedMult = 1f)
	{
		if (!Owner.IsActionPointOn || !actionPointData.actionPoint.enabled || actionPointData.state == ActionPointData.State.MoveToStart)
		{
			if (!Animation.IsPlaying(AnimNameMove))
			{
				CrossFade(AnimNameMove, 0.2f, PlayMode.StopAll);
			}
			Animation[AnimNameMove].speed = speedMult;
			return;
		}
		AnimNameAP = actionPointData.GetAnim().name;
		if (!Animation.IsPlaying(AnimNameAP))
		{
			if (Animation.GetClip(AnimNameAP) == null)
			{
				Animation.AddClip(actionPointData.GetAnim(), AnimNameAP);
			}
			Animation[AnimNameAP].layer = 5;
			CrossFade(AnimNameAP, 0.2f, PlayMode.StopAll);
			if (Owner.debugAnims)
			{
				Debug.Log("Play AP: name=" + Owner.name + ", Owner.NavMeshAgent.speed=" + Owner.NavMeshAgent.speed + ", time=" + Time.timeSinceLevelLoad);
			}
		}
	}

	private E_MotionType GetMotionType()
	{
		if (Action.DontChangeParameters)
		{
			return Action.Motion;
		}
		if (Owner.IsActionPointOn && actionPointData.actionPoint.enabled && actionPointData.state == ActionPointData.State.MoveToEnd)
		{
			return E_MotionType.ActionPoint;
		}
		if (Owner.BlackBoard.Speed > Owner.MaxWalkSpeed * 1.5f)
		{
			return E_MotionType.Run;
		}
		return E_MotionType.Walk;
	}

	private E_MoveType GetMoveType()
	{
		if (Action.DontChangeParameters)
		{
			return Action.MoveType;
		}
		Vector2 from = new Vector2(Transform.forward.x, Transform.forward.z);
		Vector2 from2 = new Vector2(Transform.right.x, Transform.right.z);
		Vector3 vector = Owner.BlackBoard.Velocity.normalized;
		if (vector == Vector3.zero)
		{
			vector = Owner.Forward;
		}
		Vector2 to = new Vector2(vector.x, vector.z);
		float num = Vector2.Angle(from, to);
		float num2 = Vector2.Angle(from2, to);
		if (num < 45f)
		{
			return E_MoveType.Forward;
		}
		if (num > 135f)
		{
			return E_MoveType.Backward;
		}
		if (num2 < 45f)
		{
			return E_MoveType.StrafeRight;
		}
		return E_MoveType.StrafeLeft;
	}

	private float GetMoveSpeed(E_MotionType motion)
	{
		switch (motion)
		{
		case E_MotionType.Walk:
			return Owner.MaxWalkSpeed;
		case E_MotionType.Run:
			return Owner.MaxRunSpeed;
		case E_MotionType.ActionPoint:
			return actionPointData.GetMoveSpeed();
		default:
			return Owner.MaxWalkSpeed;
		}
	}

	private void UpdateRotation()
	{
		if (Owner.IsActionPointOn)
		{
			Owner.BlackBoard.Desires.Rotation.SetLookRotation(actionPointData.GetLookDir());
		}
		else if (Action.UseNavMeshAgentRotation)
		{
			Owner.BlackBoard.Desires.Rotation = Owner.NavMeshAgent.transform.rotation;
		}
		else if ((bool)Action.LookTarget)
		{
			Owner.BlackBoard.Desires.Rotation.SetLookRotation((Action.LookTarget.position - Owner.Position).normalized);
		}
		else if (Owner.NavMeshAgent.velocity != Vector3.zero)
		{
			if (Owner.BlackBoard.MoveType == E_MoveType.Forward)
			{
				Owner.BlackBoard.Desires.Rotation.SetLookRotation(Owner.NavMeshAgent.velocity.normalized);
			}
			else if (Owner.BlackBoard.MoveType != E_MoveType.Backward)
			{
			}
		}
	}
}
