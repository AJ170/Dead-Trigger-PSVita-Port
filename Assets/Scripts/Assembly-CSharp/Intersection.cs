using UnityEngine;
using static Unity.Mathematics.math;
public static class Intersection
{
	public static bool RaySphere(Vector3 RayO, Vector3 RayD, float RayL, Vector3 SphereC, float SphereR)
	{
		Vector3 vector = RayO - SphereC;
		float num = Vector3.Dot(vector, vector) - SphereR * SphereR;
		if (num <= 0f)
		{
			return true;
		}
		float num2 = Vector3.Dot(vector, RayD);
		float num3 = num2 * num2 - num;
		if (num3 < -1E-06f)
		{
			return false;
		}
		float num4 = ((!(num3 > 1E-06f)) ? num2 : (num2 - sqrt(num3)));
		return num4 >= 0f && num4 <= RayL;
	}

	public static bool RayBox(Vector3 RayO, Vector3 RayD, float RayL, Vector3 BoxC, Vector3 BoxE)
	{
		float num = 0f;
		float num2 = RayL;
		Vector3 vector = RayO - BoxC;
		float num3 = 1f / RayD.x;
		float num4;
		float num5;
		if (num3 >= 0f)
		{
			num4 = (0f - BoxE.x - vector.x) * num3;
			num5 = (BoxE.x - vector.x) * num3;
		}
		else
		{
			num4 = (BoxE.x - vector.x) * num3;
			num5 = (0f - BoxE.x - vector.x) * num3;
		}
		if (num2 < num4 || num5 < num)
		{
			return false;
		}
		num = Mathf.Max(num, num4);
		num2 = Mathf.Min(num2, num5);
		num3 = 1f / RayD.y;
		if (num3 >= 0f)
		{
			num4 = (0f - BoxE.y - vector.y) * num3;
			num5 = (BoxE.y - vector.y) * num3;
		}
		else
		{
			num4 = (BoxE.y - vector.y) * num3;
			num5 = (0f - BoxE.y - vector.y) * num3;
		}
		if (num2 < num4 || num5 < num)
		{
			return false;
		}
		num = Mathf.Max(num, num4);
		num2 = Mathf.Min(num2, num5);
		num3 = 1f / RayD.z;
		if (num3 >= 0f)
		{
			num4 = (0f - BoxE.z - vector.z) * num3;
			num5 = (BoxE.z - vector.z) * num3;
		}
		else
		{
			num4 = (BoxE.z - vector.z) * num3;
			num5 = (0f - BoxE.z - vector.z) * num3;
		}
		return !(num2 < num4) && !(num5 < num);
	}

	public static int LineSphere(Vector3 LineP, Vector3 LineD, Vector3 SphereC, float SphereR, ref float T0, ref float T1)
	{
		Vector3 vector = LineP - SphereC;
		float num = Vector3.Dot(vector, vector) - SphereR * SphereR;
		float num2 = 0f - Vector3.Dot(vector, LineD);
		float num3 = num2 * num2 - num;
		if (num3 < -1E-06f)
		{
			return 0;
		}
		if (num3 > 1E-06f)
		{
			float num4 = sqrt(num3);
			T0 = num2 - num4;
			T1 = num2 + num4;
			return 2;
		}
		T0 = (T1 = num2);
		return 1;
	}

	public static int LineBox(Vector3 LineP, Vector3 LineD, Vector3 BoxC, Vector3 BoxE, ref float T0, ref float T1)
	{
		Vector3 vector = LineP - BoxC;
		float num = 1f / LineD.x;
		float num2;
		float num3;
		if (num >= 0f)
		{
			num2 = (0f - BoxE.x - vector.x) * num;
			num3 = (BoxE.x - vector.x) * num;
		}
		else
		{
			num2 = (BoxE.x - vector.x) * num;
			num3 = (0f - BoxE.x - vector.x) * num;
		}
		num = 1f / LineD.z;
		float num4;
		float num5;
		if (num >= 0f)
		{
			num4 = (0f - BoxE.z - vector.z) * num;
			num5 = (BoxE.z - vector.z) * num;
		}
		else
		{
			num4 = (BoxE.z - vector.z) * num;
			num5 = (0f - BoxE.z - vector.z) * num;
		}
		if (num3 < num4 || num2 > num5)
		{
			return 0;
		}
		num2 = Mathf.Max(num2, num4);
		num3 = Mathf.Min(num3, num5);
		num = 1f / LineD.y;
		if (num >= 0f)
		{
			num4 = (0f - BoxE.y - vector.y) * num;
			num5 = (BoxE.y - vector.y) * num;
		}
		else
		{
			num4 = (BoxE.y - vector.y) * num;
			num5 = (0f - BoxE.y - vector.y) * num;
		}
		if (num3 < num4 || num2 > num5)
		{
			return 0;
		}
		T0 = Mathf.Max(num2, num4);
		T1 = Mathf.Min(num3, num5);
		return 2;
	}

	public static bool Planes(Plane P0, Plane P1, Plane P2, ref Vector3 Point)
	{
		Vector3 vector = Vector3.Cross(P1.normal, P2.normal);
		float num = Vector3.Dot(P0.normal, vector);
		if (Mathf.Abs(num) > 1E-06f)
		{
			Vector3 vector2 = Vector3.Cross(P2.normal, P0.normal);
			Vector3 vector3 = Vector3.Cross(P0.normal, P1.normal);
			Point = (vector * P0.distance + vector2 * P1.distance + vector3 * P2.distance) / (0f - num);
			return true;
		}
		return false;
	}
}
