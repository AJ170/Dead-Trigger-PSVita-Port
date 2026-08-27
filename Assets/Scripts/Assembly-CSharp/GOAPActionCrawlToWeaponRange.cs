using System;
using UnityEngine;
using static Unity.Mathematics.math;

internal class GOAPActionCrawlToWeaponRange : GOAPAction
{
	private AgentActionCrawlTo Action;

	private Vector3 Position;

	public GOAPActionCrawlToWeaponRange(AgentHuman owner)
		: base(E_GOAPAction.CrawlToWeaponRange, owner)
	{
	}

	public override void InitAction()
	{
		WorldPreconditions.SetWSProperty(E_PropKey.BodyPose, E_BodyPose.Crawl);
		WorldEffects.SetWSProperty(E_PropKey.InWeaponRange, true);
		WorldEffects.SetWSProperty(E_PropKey.AtTargetPos, true);
		Owner.WorldState.SetWSProperty(E_PropKey.AtTargetPos, false);
		Cost = 5f;
		Precedence = 20;
	}

	public override void Activate()
	{
		base.Activate();
		Vector3 targetPos = ((!Owner.BlackBoard.DangerousEnemy) ? Owner.Transform.position : Owner.BlackBoard.DangerousEnemy.Transform.position);
		CreateAgentActionCrawlTo(targetPos);
	}

	public override void Update()
	{
		if (!Owner.BlackBoard.ActionPointOn)
		{
			if (!Owner.BlackBoard.DangerousEnemy)
			{
				Debug.Log("GOAPActionCrawlToWeaponRange.Update() : Owner.BlackBoard.DangerousEnemy = NULL, name=" + Owner.name);
			}
			else
			{
				CreateAgentActionCrawlTo(Owner.BlackBoard.DangerousEnemy.Transform.position);
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

	private void CreateAgentActionCrawlTo(Vector3 targetPos)
	{
		float num = float.PositiveInfinity;
		if (Action != null && Action.IsActive())
		{
			num = (Action.FinalPosition - targetPos).sqrMagnitude;
		}
		if (num < 0.25f)
		{
			return;
		}
		Position = targetPos;
		Action = AgentActionFactory.Create(AgentActionFactory.E_Type.CrawlTo) as AgentActionCrawlTo;
		Action.FinalPosition = targetPos;
		Action.MoveType = E_MoveType.Forward;
		Action.Motion = E_MotionType.Crawl;
		Action.MotionSide = ChooseMotionSide();
		Action.DontChangeParameters = false;
		Owner.NavMeshAgent.stoppingDistance = 0f;
		if (Owner.BlackBoard.Desires.LookAtTarget && Owner.WorldState.GetWSProperty(E_PropKey.SeeEnemy).GetBool())
		{
			if ((bool)Owner.BlackBoard.VisibleTarget)
			{
				Action.LookTarget = Owner.BlackBoard.VisibleTarget.Transform;
			}
			else if ((bool)Owner.BlackBoard.DangerousEnemy)
			{
				Action.LookTarget = Owner.BlackBoard.DangerousEnemy.Transform;
			}
		}
		if (Action.LookTarget == null)
		{
			Action.UseNavMeshAgentRotation = true;
		}
		Owner.BlackBoard.ActionAdd(Action);
	}

	public override bool IsActionComplete()
	{
		if (Owner.BlackBoard.ActionPointOn)
		{
			return false;
		}
		if (length(Owner.Transform.position - Position) < Owner.BlackBoard.WeaponRange * 0.75f)
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
		return true;
	}
}
