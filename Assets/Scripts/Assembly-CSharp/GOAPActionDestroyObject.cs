using System;
using UnityEngine;
using static Unity.Mathematics.math;

internal class GOAPActionDestroyObject : GOAPAction
{
	private AgentActionGoToBase Action;

	private Vector3 Position;

	private float Delay;

	private float NextPathRecalcTime;

	private E_MotionType MotionType = E_MotionType.Walk;

	public GOAPActionDestroyObject(AgentHuman owner)
		: base(E_GOAPAction.DestroyObject, owner)
	{
	}

	public override void InitAction()
	{
		WorldEffects.SetWSProperty(E_PropKey.InWeaponRange, true);
		WorldEffects.SetWSProperty(E_PropKey.DestroyObject, false);
		WorldEffects.SetWSProperty(E_PropKey.AtTargetPos, true);
		Cost = 1f;
		Precedence = 15;
	}

	private void SetMotionType()
	{
		if (Owner.BlackBoard.MovementSkill == F_MovementSkill.Run || Owner.BlackBoard.MovementSkill == F_MovementSkill.Berserk)
		{
			MotionType = E_MotionType.Run;
		}
		else if (Owner.BlackBoard.MovementSkill == F_MovementSkill.Walk)
		{
			MotionType = E_MotionType.Walk;
		}
		else
		{
			MotionType = ((UnityEngine.Random.Range(0, 2) != 0) ? E_MotionType.Walk : E_MotionType.Run);
		}
	}

	public override void Activate()
	{
		base.Activate();
		Owner.WorldState.SetWSProperty(E_PropKey.AtTargetPos, false);
		Owner.WorldState.SetWSProperty(E_PropKey.Berserk, false);
		SetMotionType();
		Delay = 0f;
		NextPathRecalcTime = 0f;
	}

	public override void Update()
	{
		if (!Owner.IsAlive || Owner.BlackBoard.ImportantObject == null)
		{
			return;
		}
		if (Delay > 0f)
		{
			Delay -= Time.deltaTime;
		}
		else if (!Owner.BlackBoard.ActionPointOn)
		{
			DestructibleObject destrObj = Owner.BlackBoard.ImportantObject as DestructibleObject;
			if (Owner.WorldState.GetWSProperty(E_PropKey.BodyPose).GetBodyPose() == E_BodyPose.Stand)
			{
				CreateAgentActionGoTo(destrObj);
			}
			else
			{
				CreateAgentActionCrawlTo(destrObj);
			}
		}
	}

	public override void Deactivate()
	{
		Owner.WorldState.SetWSProperty(E_PropKey.AtTargetPos, true);
		if (Action != null && Action.IsActive())
		{
			AgentActionIdle action = AgentActionFactory.Create(AgentActionFactory.E_Type.Idle) as AgentActionIdle;
			Owner.BlackBoard.ActionAdd(action);
		}
		base.Deactivate();
	}

	private void CreateAgentActionGoTo(DestructibleObject destrObj)
	{
		if (destrObj == null || destrObj.GetGameObject() == null)
		{
			return;
		}
		Transform attackPoint = destrObj.GetAttackPoint(Owner);
		if (attackPoint != null)
		{
			Position = attackPoint.position;
		}
		else
		{
			Position = destrObj.GetGameObject().transform.position;
		}
		float num = float.PositiveInfinity;
		bool reselectMoveAnim = false;
		if (Action != null && Action.IsActive())
		{
			num = (Action.FinalPosition - Owner.Position).sqrMagnitude;
			if (MotionType != Action.Motion)
			{
				reselectMoveAnim = true;
			}
		}
		else
		{
			num = (Position - Owner.Position).sqrMagnitude;
			reselectMoveAnim = true;
		}
		if (num < 16f)
		{
			MotionType = E_MotionType.Walk;
		}
		if (!(NextPathRecalcTime > Time.timeSinceLevelLoad))
		{
			NextPathRecalcTime = Time.timeSinceLevelLoad + 0.5f;
			Action = AgentActionFactory.Create(AgentActionFactory.E_Type.Goto) as AgentActionGoTo;
			Action.FinalPosition = Position;
			Action.MoveType = E_MoveType.Forward;
			Action.Motion = MotionType;
			Action.MinDistance = Owner.BlackBoard.DestructibleObjectRange;
			(Action as AgentActionGoTo).ReselectMoveAnim = reselectMoveAnim;
			Action.DontChangeParameters = false;
			Owner.NavMeshAgent.stoppingDistance = 0f;
			if (Owner.BlackBoard.Desires.LookAtTarget)
			{
				Action.LookTarget = destrObj.GetGameObject().transform;
			}
			Owner.BlackBoard.ActionAdd(Action);
		}
	}

	private E_MotionSide ChooseMotionSide()
	{
		ComponentEnemy component = Owner.GetComponent<ComponentEnemy>();
		if (component == null)
		{
			throw new MemberAccessException("ComponentEnemy not found!");
		}
		bool flag = !component.IsLimbDecapitated(E_BodyPart.LeftArm);
		bool flag2 = !component.IsLimbDecapitated(E_BodyPart.RightArm);
		if (flag && flag2)
		{
			return E_MotionSide.Center;
		}
		if (flag2)
		{
			return E_MotionSide.Right;
		}
		return E_MotionSide.Left;
	}

	private void CreateAgentActionCrawlTo(DestructibleObject destrObj)
	{
		if (!(destrObj == null) && !(destrObj.GetGameObject() == null) && (Action == null || !Action.IsActive()))
		{
			Transform attackPoint = destrObj.GetAttackPoint(Owner);
			if (attackPoint != null)
			{
				Position = attackPoint.position;
			}
			else
			{
				Position = destrObj.GetGameObject().transform.position;
			}
			AgentActionCrawlTo agentActionCrawlTo = AgentActionFactory.Create(AgentActionFactory.E_Type.CrawlTo) as AgentActionCrawlTo;
			agentActionCrawlTo.FinalPosition = Position;
			agentActionCrawlTo.MoveType = E_MoveType.Forward;
			agentActionCrawlTo.Motion = E_MotionType.Crawl;
			agentActionCrawlTo.MotionSide = ChooseMotionSide();
			agentActionCrawlTo.DontChangeParameters = false;
			Action = agentActionCrawlTo;
			Owner.NavMeshAgent.stoppingDistance = 0f;
			if (Owner.BlackBoard.Desires.LookAtTarget)
			{
				Action.LookTarget = destrObj.GetGameObject().transform;
			}
			Owner.BlackBoard.ActionAdd(Action);
		}
	}

	public override bool IsActionComplete()
	{
		if (Owner.BlackBoard.ActionPointOn)
		{
			return false;
		}
		if (!Owner.WorldState.GetWSProperty(E_PropKey.DestroyObject).GetBool())
		{
			return true;
		}
		if (length(Owner.Transform.position - Position) < Owner.BlackBoard.DestructibleObjectRange)
		{
			return true;
		}
		if (Action != null && Action.IsSuccess())
		{
			return true;
		}
		return false;
	}

	public override bool ValidateAction()
	{
		if (Action != null && Action.IsFailed())
		{
			return false;
		}
		return true;
	}

	public override void SolvePlanWSVariable(WorldState currentState, WorldState goalState)
	{
		base.SolvePlanWSVariable(currentState, goalState);
	}

	public override void SetPlanWSPreconditions(WorldState goalState)
	{
		base.SetPlanWSPreconditions(goalState);
	}

	public override bool ValidateContextPreconditions(WorldState current, bool planning)
	{
		return Owner.WorldState.GetWSProperty(E_PropKey.DestroyObject).GetBool();
	}
}
