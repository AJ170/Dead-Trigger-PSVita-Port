using UnityEngine;
using static Unity.Mathematics.math;
public class Quat
{
	public static Quaternion Create(Matrix4x4 Mat)
	{
		Quaternion result = default(Quaternion);
		result.x = sqrt(Mathf.Max(0f, 1f - Mat.m00 + Mat.m11 - Mat.m22)) * 0.5f;
		result.z = sqrt(Mathf.Max(0f, 1f - Mat.m00 - Mat.m11 + Mat.m22)) * 0.5f;
		result.w = sqrt(Mathf.Max(0f, 1f + Mat.m00 + Mat.m11 + Mat.m22)) * 0.5f;
		result.x *= Mathf.Sign(result.x * (Mat.m21 - Mat.m12));
		result.y *= Mathf.Sign(result.y * (Mat.m02 - Mat.m20));
		result.z *= Mathf.Sign(result.z * (Mat.m10 - Mat.m01));
		return result;
	}

	public static Vector3 GetAxis(Quaternion Q, int AxisIndex)
	{
		switch (AxisIndex)
		{
		case 0:
			return GetAxisX(Q);
		case 1:
			return GetAxisY(Q);
		default:
			return GetAxisZ(Q);
		}
	}

	public static Vector3 GetAxisX(Quaternion Q)
	{
		float num = Q.x + Q.x;
		float num2 = Q.y + Q.y;
		float num3 = Q.z + Q.z;
		float x = 1f - (Q.y * num2 + Q.z * num3);
		float y = Q.y * num + Q.w * num3;
		float z = Q.z * num - Q.w * num2;
		return new Vector3(x, y, z);
	}

	public static Vector3 GetAxisY(Quaternion Q)
	{
		float num = Q.x + Q.x;
		float num2 = Q.y + Q.y;
		float num3 = Q.z + Q.z;
		float x = Q.y * num - Q.w * num3;
		float y = 1f - (Q.x * num + Q.z * num3);
		float z = Q.z * num2 + Q.w * num;
		return new Vector3(x, y, z);
	}

	public static Vector3 GetAxisZ(Quaternion Q)
	{
		float num = Q.x + Q.x;
		float num2 = Q.y + Q.y;
		float x = Q.z * num + Q.w * num2;
		float y = Q.z * num2 - Q.w * num;
		float z = 1f - (Q.x * num + Q.y * num2);
		return new Vector3(x, y, z);
	}

	public static void GetAxis(Quaternion Q, ref Vector3 AxisX, ref Vector3 AxisY, ref Vector3 AxisZ)
	{
		float num = Q.x + Q.x;
		float num2 = Q.y + Q.y;
		float num3 = Q.z + Q.z;
		float num4 = num * Q.x;
		float num5 = num * Q.y;
		float num6 = num * Q.z;
		float num7 = num * Q.w;
		float num8 = num2 * Q.y;
		float num9 = num2 * Q.z;
		float num10 = num2 * Q.w;
		float num11 = num3 * Q.z;
		float num12 = num3 * Q.w;
		AxisX.x = 1f - (num8 + num11);
		AxisX.y = num5 + num12;
		AxisX.z = num6 - num10;
		AxisY.x = num5 - num12;
		AxisY.y = 1f - (num4 + num11);
		AxisY.z = num9 + num7;
		AxisZ.x = num6 + num10;
		AxisZ.y = num9 - num7;
		AxisZ.z = 1f - (num4 + num8);
	}
}
