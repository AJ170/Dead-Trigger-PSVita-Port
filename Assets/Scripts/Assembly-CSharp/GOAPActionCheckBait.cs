using System;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Mathematics.math;

internal class GOAPActionCheckBait : GOAPAction
{
	private AgentActionGoToBase Action;

	private Vector3 Position;

	private float Delay;

	public GOAPActionCheckBait(AgentHuman owner)
		: base(E_GOAPAction.CheckBait, owner)
	{
	}

	public override void InitAction()
	{
		WorldEffects.SetWSProperty(E_PropKey.CheckBait, false);
		WorldEffects.SetWSProperty(E_PropKey.AtTargetPos, true);
		Cost = 1f;
		Precedence = 20;
	}

	public override void Activate()
	{
		base.Activate();
		Owner.WorldState.SetWSProperty(E_PropKey.AtTargetPos, false);
		Delay = 0f;
	}

	public override void Update()
	{
		if (!Owner.IsAlive)
		{
			return;
		}
		if (Delay > 0f)
		{
			Delay -= Time.deltaTime;
		}
		else
		{
			if (Owner.BlackBoard.ActionPointOn)
			{
				return;
			}
			switch (Owner.BlackBoard.BaitPhase)
			{
			case BlackBoard.E_BaitPhase.Surprise:
				PlaySurpriseAnim();
				break;
			case BlackBoard.E_BaitPhase.GoTo:
			{
				Vector3 lookRotation = Position - Owner.Transform.position;
				if (length(lookRotation) < Owner.BlackBoard.BaitRange)
				{
					Owner.BlackBoard.Desires.Rotation.SetLookRotation(lookRotation);
					Owner.BlackBoard.BaitPhase = BlackBoard.E_BaitPhase.ObjectReached;
				}
				else if (Owner.WorldState.GetWSProperty(E_PropKey.BodyPose).GetBodyPose() == E_BodyPose.Stand)
				{
					CreateAgentActionGoTo(FindTheBestBait());
				}
				else
				{
					CreateAgentActionCrawlTo(FindTheBestBait());
				}
				break;
			}
			case BlackBoard.E_BaitPhase.ObjectReached:
			{
				AgentActionIdle action = AgentActionFactory.Create(AgentActionFactory.E_Type.Idle) as AgentActionIdle;
				Owner.BlackBoard.ActionAdd(action);
				Owner.BlackBoard.BaitPhase = BlackBoard.E_BaitPhase.Stare;
				break;
			}
			case BlackBoard.E_BaitPhase.Stare:
				break;
			}
		}
	}

	private float PlaySurpriseAnim()
	{
		AnimSetZombie animSetZombie = Owner.AnimSet as AnimSetZombie;
		if (animSetZombie == null)
		{
			return 0f;
		}
		GameObject gameObject = FindTheBestBait();
		if (gameObject == null)
		{
			return 0f;
		}
		Position = gameObject.transform.position;
		Owner.BlackBoard.BaitPhase = BlackBoard.E_BaitPhase.GoTo;
		if (Owner.WorldState.GetWSProperty(E_PropKey.BodyPose).GetBodyPose() != 0)
		{
			return 0.1f;
		}
		float num = Vector3.Angle(Owner.Transform.forward, Position - Owner.Transform.position);
		AnimSetZombie.E_BaitAnim baitAnim;
		if (num < 45f)
		{
			baitAnim = AnimSetZombie.E_BaitAnim.LookForward;
		}
		else
		{
			float num2 = Vector3.Angle(Owner.Transform.right, Position - Owner.Transform.position);
			baitAnim = ((!(num2 < 90f)) ? AnimSetZombie.E_BaitAnim.LookLeft : AnimSetZombie.E_BaitAnim.LookRight);
		}
		string baitAnim2 = animSetZombie.GetBaitAnim(baitAnim);
		return Owner.PlayAnim(baitAnim2, E_TriState.Default);
	}

	public override void Deactivate()
	{
		Owner.WorldState.SetWSProperty(E_PropKey.AtTargetPos, true);
		Owner.WorldState.SetWSProperty(E_PropKey.CheckBait, false);
		if (Action != null && Action.IsActive())
		{
			AgentActionIdle action = AgentActionFactory.Create(AgentActionFactory.E_Type.Idle) as AgentActionIdle;
			Owner.BlackBoard.ActionAdd(action);
		}
		base.Deactivate();
	}

	private void CreateAgentActionGoTo(GameObject target)
	{
		if (target == null)
		{
			return;
		}
		float num = float.PositiveInfinity;
		if (Action != null && Action.IsActive())
		{
			num = (Action.FinalPosition - target.transform.position).sqrMagnitude;
		}
		if (!(num < 0.25f))
		{
			Position = target.transform.position;
			Action = AgentActionFactory.Create(AgentActionFactory.E_Type.Goto) as AgentActionGoTo;
			Action.FinalPosition = Position;
			Action.MoveType = E_MoveType.Forward;
			Action.Motion = E_MotionType.Run;
			Action.DontChangeParameters = false;
			if (Owner.BlackBoard.Desires.LookAtTarget)
			{
				Action.LookTarget = target.transform;
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

	private void CreateAgentActionCrawlTo(GameObject target)
	{
		if (target == null)
		{
			return;
		}
		float num = float.PositiveInfinity;
		if (Action != null && Action.IsActive())
		{
			num = (Action.FinalPosition - target.transform.position).sqrMagnitude;
		}
		if (!(num < 0.25f))
		{
			Position = target.transform.position;
			AgentActionCrawlTo agentActionCrawlTo = AgentActionFactory.Create(AgentActionFactory.E_Type.CrawlTo) as AgentActionCrawlTo;
			agentActionCrawlTo.FinalPosition = Position;
			agentActionCrawlTo.MoveType = E_MoveType.Forward;
			agentActionCrawlTo.Motion = E_MotionType.Crawl;
			agentActionCrawlTo.MotionSide = ChooseMotionSide();
			agentActionCrawlTo.DontChangeParameters = false;
			Action = agentActionCrawlTo;
			if (Owner.BlackBoard.Desires.LookAtTarget)
			{
				Action.LookTarget = target.transform;
			}
			Owner.BlackBoard.ActionAdd(Action);
		}
	}

	private GameObject CompareDistance(GameObject first, GameObject second)
	{
		float sqrMagnitude = (first.transform.position - Owner.transform.position).sqrMagnitude;
		float sqrMagnitude2 = (second.transform.position - Owner.transform.position).sqrMagnitude;
		return (!(sqrMagnitude < sqrMagnitude2)) ? second : first;
	}

	private GameObject FindTheBestBait()
	{
		List<IImportantObject> importantObjects = Mission.Instance.CurrentGameZone.ImportantObjects;
		GameObject gameObject = null;
		foreach (IImportantObject item in importantObjects)
		{
			if (item.GetImportantObjectType() == E_ImportantObjectType.Bait || item.GetImportantObjectType() == E_ImportantObjectType.GrenadeBait)
			{
				gameObject = ((!(gameObject == null)) ? CompareDistance(gameObject, item.GetGameObject()) : item.GetGameObject());
			}
		}
		return gameObject;
	}

	public override bool IsActionComplete()
	{
		if (Owner.BlackBoard.ActionPointOn)
		{
			return false;
		}
		if (!Owner.WorldState.GetWSProperty(E_PropKey.CheckBait).GetBool())
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
		return Owner.WorldState.GetWSProperty(E_PropKey.CheckBait).GetBool();
	}
}
