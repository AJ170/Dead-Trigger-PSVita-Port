using System.Collections.Generic;
using UnityEngine;
using static Unity.Mathematics.math;

public class AiRecon
{
	public class NearPositionData
	{
		public Vector3 Position;

		public float Distance;

		public bool SeeEnemy;

		public float DistanceToNearestEnemy;

		public void Reset()
		{
			SeeEnemy = false;
			Distance = 0f;
		}
	}

	public Agent Owner;

	public List<NearPositionData> Positions = new List<NearPositionData>();

	public int MaxPosition { get; private set; }

	public float Distance { get; private set; }

	public AiRecon(Agent owner, int maxPosition, float distance)
	{
		Distance = distance;
		MaxPosition = maxPosition;
		Owner = owner;
		for (int i = 0; i < MaxPosition; i++)
		{
			Positions.Add(new NearPositionData());
		}
	}

	public void Reset()
	{
		foreach (NearPositionData position in Positions)
		{
			position.Reset();
		}
	}

	public NearPositionData GetBestPositionInDirection(Vector3 dir, float minDistance, float minDistanceToEnemy)
	{
		NearPositionData result = null;
		float num = 0.1f;
		float num2 = minDistance;
		foreach (NearPositionData position in Positions)
		{
			if (!(position.DistanceToNearestEnemy < minDistanceToEnemy) && !(position.Distance < num2))
			{
				float num3 = Vector3.Dot((position.Position - Owner.Position).normalized, dir);
				if (!(num3 < num))
				{
					num = num3;
					num2 = position.Distance;
					result = position;
				}
			}
		}
		return result;
	}

	public NearPositionData GetBestPositionInDirection(Vector3 dir, float minDistance, float minDistanceToEnemy, bool seeEnemy)
	{
		NearPositionData result = null;
		float num = 0.1f;
		float num2 = minDistance;
		foreach (NearPositionData position in Positions)
		{
			if (position.SeeEnemy == seeEnemy && !(position.DistanceToNearestEnemy < minDistanceToEnemy) && !(position.Distance < num2))
			{
				float num3 = Vector3.Dot((position.Position - Owner.Position).normalized, dir);
				if (!(num3 < 0.1f) && !(num3 < num))
				{
					num = num3;
					num2 = position.Distance;
					result = position;
				}
			}
		}
		return result;
	}

	public NearPositionData GetNearestPositionToPos(Vector3 toPos, float minDistanceFromMe, float minDistanceToEnemy)
	{
		NearPositionData result = null;
		float num = 1000f;
		foreach (NearPositionData position in Positions)
		{
			if (!(position.Distance < minDistanceFromMe) && !(position.DistanceToNearestEnemy < minDistanceToEnemy))
			{
				float magnitude = length(position.Position - toPos);
				if (!(magnitude > num))
				{
					num = magnitude;
					result = position;
				}
			}
		}
		return result;
	}

	public NearPositionData GetFarestPositionToPos(Vector3 toPos, float minDistanceFromMe, float minDistanceToEnemy)
	{
		NearPositionData result = null;
		float num = 0f;
		foreach (NearPositionData position in Positions)
		{
			if (!(position.Distance < minDistanceFromMe) && !(position.DistanceToNearestEnemy < minDistanceToEnemy))
			{
				float magnitude = length(position.Position - toPos);
				if (!(magnitude < num))
				{
					num = magnitude;
					result = position;
				}
			}
		}
		return result;
	}

	public NearPositionData GetRandomPositionInDirection(Vector3 dir, float minDistance, float minDistanceToEnemy, float highestDot, float lowestDot, bool seeEnemy)
	{
		List<NearPositionData> list = new List<NearPositionData>();
		foreach (NearPositionData position in Positions)
		{
			if (position.SeeEnemy == seeEnemy && !(position.DistanceToNearestEnemy < minDistanceToEnemy) && !(position.Distance < minDistance))
			{
				float num = Vector3.Dot((position.Position - Owner.Position).normalized, dir);
				if (!(num < lowestDot) && !(num > highestDot))
				{
					list.Add(position);
				}
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		return list[Random.Range(0, list.Count)];
	}
}
