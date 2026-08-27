using UnityEngine;


public class SquareDistance
{
	public static float PointRay(Vector3 P, Vector3 RayO, Vector3 RayD)
	{
		return Vector3.SqrMagnitude(P - ClosestPoint.PointRay(P, RayO, RayD));
	}

	public static float PointSegment(Vector3 P, Vector3 S0, Vector3 S1)
	{
		return Vector3.SqrMagnitude(P - ClosestPoint.PointSegment(P, S0, S1));
	}
}
