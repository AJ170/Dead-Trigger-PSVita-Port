using UnityEngine;
using static Unity.Mathematics.math;

internal class GOAPGoalLookAtTarget : GOAPGoal
{
	public GOAPGoalLookAtTarget(AgentHuman owner)
		: base(E_GOAPGoals.LookAtTarget, owner)
	{
	}

	public override void InitGoal()
	{
	}

	public override float GetMaxRelevancy()
	{
		return base.Owner.BlackBoard.GoapSetup.LookAtTargetRelevancy;
	}

	public override void CalculateGoalRelevancy()
	{
		base.GoalRelevancy = 0f;
		if (base.Owner.WorldState.GetWSProperty(E_PropKey.LookingAtTarget).GetBool() || !base.Owner.WorldState.GetWSProperty(E_PropKey.AtTargetPos).GetBool() || base.Owner.WorldState.GetWSProperty(E_PropKey.DestroyObject).GetBool() || base.Owner.BlackBoard.DangerousEnemy == null)
		{
			return;
		}
		Vector3 vector = base.Owner.BlackBoard.DangerousEnemy.Position - base.Owner.Position;
		RaycastHit hitInfo;
		if (!Physics.Raycast(base.Owner.EyePosition, vector, out hitInfo, length(vector)) || !(hitInfo.distance < base.Owner.BlackBoard.DistanceToTarget * 0.7f))
		{
			base.GoalRelevancy = Mathf.Min(base.Owner.BlackBoard.GoapSetup.LookAtTargetRelevancy, Vector3.Angle(base.Owner.Forward, vector) * 0.1f);
			if (base.GoalRelevancy < 0.2f)
			{
				base.GoalRelevancy = 0f;
			}
		}
	}

	public override void SetDisableTime()
	{
		base.NextEvaluationTime = base.Owner.BlackBoard.GoapSetup.LookAtTargetDelay + Time.timeSinceLevelLoad;
	}

	public override void SetWSSatisfactionForPlanning(WorldState worldState)
	{
		worldState.SetWSProperty(E_PropKey.LookingAtTarget, true);
	}

	public override bool IsWSSatisfiedForPlanning(WorldState worldState)
	{
		WorldStateProp wSProperty = worldState.GetWSProperty(E_PropKey.LookingAtTarget);
		if (wSProperty != null && wSProperty.GetBool())
		{
			return true;
		}
		return false;
	}

	public override bool IsSatisfied()
	{
		WorldStateProp wSProperty = base.Owner.WorldState.GetWSProperty(E_PropKey.LookingAtTarget);
		if (wSProperty.GetBool())
		{
			return true;
		}
		return IsPlanFinished();
	}

	public override bool ReplanRequired()
	{
		return false;
	}

	public override bool Activate(GOAPPlan plan)
	{
		return base.Activate(plan);
	}
}
