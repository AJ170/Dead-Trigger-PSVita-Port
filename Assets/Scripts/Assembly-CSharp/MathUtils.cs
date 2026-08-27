using System;
using UnityEngine;
using static Unity.Mathematics.math;
public class MathUtils
{
	public static float SanitizeDegrees(float Angle)
	{
		float num = -0.0001f;
		float num2 = 359.9999f;
		if (Angle < num)
		{
			Angle += 360f;
		}
		else if (Angle > num2)
		{
			Angle -= 360f;
		}
		return Angle;
	}

	public static float SanitizeRadians(float Angle)
	{
		float num = -0.0001f;
		float num2 = 6.2830853f;
		if (Angle < num)
		{
			Angle += (float)Math.PI * 2f;
		}
		else if (Angle > num2)
		{
			Angle -= (float)Math.PI * 2f;
		}
		return Angle;
	}

	public static Vector3 AnglesToVector(Vector3 RefForward, Vector3 RefUp, float AngleH, float AngleV)
	{
		float num = Mathf.Cos(AngleV);
		float num2 = Mathf.Sin(AngleV);
		float num3 = Mathf.Cos(AngleH);
		float num4 = Mathf.Sin(AngleH);
		float num5 = num * num3;
		float num6 = num * num4;
		float num7 = num2;
		Vector3 vector = Vector3.Cross(RefUp, RefForward);
		Vector3 value = num5 * RefForward + num6 * vector + num7 * RefUp;
		return Vector3.Normalize(value);
	}

	public static void VectorToAngles(Vector3 RefForward, Vector3 RefUp, Vector3 Vec, ref float AngleH, ref float AngleV)
	{
		float num = Vector3.Dot(Vec, RefUp);
		Vector3 vector = Vec - num * RefUp;
		num = Mathf.Clamp(num, -1f, 1f);
		AngleV = Mathf.Asin(num);
		num = Vector3.SqrMagnitude(vector);
		if (num < 0.0001f)
		{
			AngleH = 0f;
			return;
		}
		vector /= sqrt(num);
		num = Vector3.Dot(vector, RefForward);
		num = Mathf.Clamp(num, -1f, 1f);
		Vector3 lhs = Vector3.Cross(vector, RefForward);
		AngleH = ((!(Vector3.Dot(lhs, RefUp) > 0f)) ? Mathf.Acos(num) : (0f - Mathf.Acos(num)));
	}

	public static Vector3 RandomVectorInsideCone(Vector3 ConeAxis, float ConeAngle)
	{
		float f = UnityEngine.Random.Range(0f, (float)Math.PI * 2f);
		float f2 = UnityEngine.Random.Range(0f, ConeAngle * ((float)Math.PI / 180f));
		float num = Mathf.Sin(f2);
		Vector3 vector = new Vector3(Mathf.Sin(f) * num, Mathf.Cos(f) * num, Mathf.Cos(f2));
		return Quaternion.LookRotation(ConeAxis) * vector;
	}

	public static bool InRange(float Val, float Min, float Max)
	{
		return Min <= Val && Val <= Max;
	}
}
