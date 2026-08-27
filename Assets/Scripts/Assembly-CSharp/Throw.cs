using System;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Mathematics.math;

public static class Throw
{
	public static Vector3 m_Gravity = Physics.gravity;

	public static Vector3 ComputePosition(Vector3 SrcPos, Vector3 Velocity, float ElapsedTime)
	{
		return SrcPos + ElapsedTime * Velocity + 0.5f * (ElapsedTime * ElapsedTime) * m_Gravity;
	}

	public static float ComputeDuration(Vector3 SrcPos, Vector3 DstPos, Vector3 Velocity)
	{
		Vector3 vector = Vector3.Cross(m_Gravity, Velocity);
		float num;
		if ((double)Vector3.Dot(vector, vector) < 1E-05)
		{
			vector = -m_Gravity;
			float[] array = new float[2];
			float a = Vector3.Dot(vector, m_Gravity) * 0.5f;
			float b = Vector3.Dot(vector, Velocity);
			float c = Vector3.Dot(vector, SrcPos - DstPos);
			if (Roots.Quadratic(a, b, c, array) == 0)
			{
				return -1f;
			}
			num = array[0];
		}
		else
		{
			vector = Vector3.Cross(m_Gravity, vector);
			num = (0f - Vector3.Dot(vector, SrcPos - DstPos)) / Vector3.Dot(vector, Velocity);
		}
		return (!(num < 0f)) ? num : (-1f);
	}

	public static float ComputeMinSpeed(Vector3 SrcPos, Vector3 DstPos)
	{
		Vector3 vector = DstPos - SrcPos;
		float num = 0.25f * Vector3.Dot(m_Gravity, m_Gravity);
		float num2 = 0.5f * Vector3.Dot(m_Gravity, vector);
		float num3 = Vector3.Dot(vector, vector);
		float num4 = sqrt(2f * sqrt(num * num3) - num2);
		return num4 + 0.001f;
	}

	public static bool ComputeVelocity(Vector3 SrcPos, Vector3 DstPos, float Speed, ref Vector3 Velocity)
	{
		return ComputeVelocity(SrcPos, DstPos, Speed, true, ref Velocity);
	}

	public static bool ComputeVelocity(Vector3 SrcPos, Vector3 DstPos, float Speed, bool PreferShorterTrajectory, ref Vector3 Velocity)
	{
		Vector3 vector = DstPos - SrcPos;
		float a = 0.25f * Vector3.Dot(m_Gravity, m_Gravity);
		float num = 0.5f * Vector3.Dot(m_Gravity, vector) + Speed * Speed;
		float c = Vector3.Dot(vector, vector);
		float[] array = new float[2];
		int num2 = Roots.Quadratic(a, 0f - num, c, array);
		if (num2 == 0)
		{
			return false;
		}
		int num3 = ((num2 != 1 && !PreferShorterTrajectory) ? 1 : 0);
		float num4 = sqrt(Mathf.Max(1E-05f, array[num3]));
		Velocity = 1f / num4 * vector - 0.5f * num4 * m_Gravity;
		return true;
	}

	public static void ComputeTrajectory(Vector3 SrcPos, Vector3 Velocity, float Duration, float MaxError, List<Vector3> Trajectory)
	{
		Trajectory.Clear();
		if (Duration > 0f)
		{
			Vector3 vector = ComputePosition(SrcPos, Velocity, Duration);
			Trajectory.Add(SrcPos);
			GeneratePoints(SrcPos, Velocity, MaxError * MaxError, 0f, Duration, SrcPos, vector, ref Trajectory);
			Trajectory.Add(vector);
		}
	}

	private static void GeneratePoints(Vector3 SrcPos, Vector3 Velocity, float SqrMaxError, float T0, float T1, Vector3 P0, Vector3 P1, ref List<Vector3> Trajectory)
	{
		float num = (T0 + T1) * 0.5f;
		Vector3 vector = ComputePosition(SrcPos, Velocity, num);
		if (SquareDistance.PointRay(vector, P0, P1 - P0) > SqrMaxError)
		{
			GeneratePoints(SrcPos, Velocity, SqrMaxError, T0, num, P0, vector, ref Trajectory);
			Trajectory.Add(vector);
			GeneratePoints(SrcPos, Velocity, SqrMaxError, num, T1, vector, P1, ref Trajectory);
		}
	}

	public static bool ClipTrajectoryToFirstHit(List<Vector3> Trajectory, int ClipLayersMask = 1)
	{
		int count = Trajectory.Count;
		for (int i = 0; i < count - 1; i++)
		{
			Vector3 vector = Trajectory[i];
			Vector3 vector2 = Trajectory[i + 1] - vector;
			float num = Vector3.Magnitude(vector2);
			RaycastHit[] array = Physics.RaycastAll(vector, vector2 / num, num, ClipLayersMask);
			if (array.Length > 1)
			{
				Array.Sort(array, CollisionUtils.CompareHits);
			}
			RaycastHit[] array2 = array;
			for (int j = 0; j < array2.Length; j++)
			{
				RaycastHit raycastHit = array2[j];
				if (!raycastHit.collider.isTrigger)
				{
					Trajectory.RemoveRange(i, count - i);
					Trajectory.Add(raycastHit.point);
					return true;
				}
			}
		}
		return false;
	}

	public static float ComputeTrajectoryLength(List<Vector3> Trajectory)
	{
		float num = 0f;
		int num2 = Trajectory.Count - 1;
		for (int i = 0; i < num2; i++)
		{
			num += (Trajectory[i] - Trajectory[i + 1]).magnitude;
		}
		return num;
	}
}
