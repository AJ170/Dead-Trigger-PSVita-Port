using System;
using UnityEngine;
using static Unity.Mathematics.math;

public class SensorPosition : SensorBase
{
	private UnityEngine.AI.NavMeshHit NavMeshHit = default(UnityEngine.AI.NavMeshHit);

	private int PosIndex;

	public SensorPosition(AgentHuman owner)
		: base(owner)
	{
	}

	public override void Update()
	{
		if (base.Owner.BlackBoard.Stop || base.Owner.BlackBoard.DangerousEnemy == null)
		{
			return;
		}
		float f = (float)Math.PI * 2f / (float)base.Owner.BlackBoard.AiRecon.MaxPosition * (float)PosIndex;
		Vector3 vector = new Vector3(Mathf.Cos(f), 0f, Mathf.Sin(f));
		UnityEngine.AI.NavMesh.Raycast(base.Owner.Position, base.Owner.Position + base.Owner.BlackBoard.AiRecon.Distance * vector, out NavMeshHit, base.Owner.NavMeshAgent.areaMask);
		if (length(base.Owner.BlackBoard.AiRecon.Positions[PosIndex].Position - NavMeshHit.position) > 1f)
		{
			base.Owner.BlackBoard.AiRecon.Positions[PosIndex].Position = NavMeshHit.position;
			base.Owner.BlackBoard.AiRecon.Positions[PosIndex].Distance = NavMeshHit.distance;
			if ((bool)base.Owner.BlackBoard.DangerousEnemy && base.Owner.BlackBoard.AiRecon.Positions[PosIndex].Distance > 0f)
			{
				bool seeEnemy = true;
				Vector3 vector2 = NavMeshHit.position + Vector3.up * 1.5f - base.Owner.BlackBoard.DangerousEnemy.TransformTarget.position;
				RaycastHit[] array = Physics.RaycastAll(NavMeshHit.position + Vector3.up * 1.5f, vector2.normalized, base.Owner.BlackBoard.AiRecon.Positions[PosIndex].Distance);
				RaycastHit[] array2 = array;
				for (int i = 0; i < array2.Length; i++)
				{
					RaycastHit raycastHit = array2[i];
					if (!raycastHit.collider.isTrigger && !(raycastHit.collider == base.Owner.CharacterController))
					{
						seeEnemy = false;
						break;
					}
				}
				base.Owner.BlackBoard.AiRecon.Positions[PosIndex].SeeEnemy = seeEnemy;
			}
			else
			{
				base.Owner.BlackBoard.AiRecon.Positions[PosIndex].SeeEnemy = false;
			}
			base.Owner.BlackBoard.AiRecon.Positions[PosIndex].DistanceToNearestEnemy = Mission.Instance.CurrentGameZone.GetDistanceToNearestEnemy(base.Owner.BlackBoard.AiRecon.Positions[PosIndex].Position, base.Owner);
		}
		PosIndex++;
		if (PosIndex == base.Owner.BlackBoard.AiRecon.MaxPosition)
		{
			PosIndex = 0;
		}
	}

	public override void Reset()
	{
		PosIndex = 0;
	}

	public override void DebugDraw()
	{
		foreach (AiRecon.NearPositionData position in base.Owner.BlackBoard.AiRecon.Positions)
		{
			if (position.SeeEnemy)
			{
				Debug.DrawLine(base.Owner.Position, position.Position, Color.green);
			}
			else
			{
				Debug.DrawLine(base.Owner.Position, position.Position, Color.white);
			}
		}
	}
}
