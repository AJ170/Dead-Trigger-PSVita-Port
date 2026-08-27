using System;
using UnityEngine;
using static Unity.Mathematics.math;
public class Matrix
{
	public static Matrix4x4 CreateTranslation(Vector3 Origin)
	{
		Matrix4x4 result = default(Matrix4x4);
		result.m00 = 1f;
		result.m03 = Origin.x;
		result.m11 = 1f;
		result.m13 = Origin.y;
		result.m22 = 1f;
		result.m23 = Origin.z;
		result.m33 = 1f;
		return result;
	}

	public static Matrix4x4 CreateTranslation(float OriginX, float OriginY, float OriginZ)
	{
		Matrix4x4 result = default(Matrix4x4);
		result.m00 = 1f;
		result.m03 = OriginX;
		result.m11 = 1f;
		result.m13 = OriginY;
		result.m22 = 1f;
		result.m23 = OriginZ;
		result.m33 = 1f;
		return result;
	}

	public static Matrix4x4 CreateScale(Vector3 Scale)
	{
		Matrix4x4 result = default(Matrix4x4);
		result.m00 = Scale.x;
		result.m11 = Scale.y;
		result.m22 = Scale.z;
		result.m33 = 1f;
		return result;
	}

	public static Matrix4x4 CreateScale(float Scale)
	{
		Matrix4x4 result = default(Matrix4x4);
		result.m00 = Scale;
		result.m11 = Scale;
		result.m22 = Scale;
		result.m33 = 1f;
		return result;
	}

	public static Matrix4x4 CreateScale(float ScaleX, float ScaleY, float ScaleZ)
	{
		Matrix4x4 result = default(Matrix4x4);
		result.m00 = ScaleX;
		result.m11 = ScaleY;
		result.m22 = ScaleZ;
		result.m33 = 1f;
		return result;
	}

	public static Matrix4x4 Create(Quaternion Q)
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
		Matrix4x4 result = default(Matrix4x4);
		result.m00 = 1f - (num8 + num11);
		result.m10 = num5 - num12;
		result.m20 = num6 + num10;
		result.m30 = 0f;
		result.m01 = num5 + num12;
		result.m11 = 1f - (num4 + num11);
		result.m21 = num9 - num7;
		result.m31 = 0f;
		result.m02 = num6 - num10;
		result.m12 = num9 + num7;
		result.m22 = 1f - (num4 + num8);
		result.m32 = 0f;
		result.m03 = 0f;
		result.m13 = 0f;
		result.m23 = 0f;
		result.m33 = 1f;
		return result;
	}

	public static Matrix4x4 Create(Vector3 Origin, Vector3 AxisX, Vector3 AxisY, Vector3 AxisZ)
	{
		Matrix4x4 result = default(Matrix4x4);
		result.m00 = AxisX.x;
		result.m01 = AxisY.x;
		result.m02 = AxisZ.x;
		result.m03 = Origin.x;
		result.m10 = AxisX.y;
		result.m11 = AxisY.y;
		result.m12 = AxisZ.y;
		result.m13 = Origin.y;
		result.m20 = AxisX.z;
		result.m21 = AxisY.z;
		result.m22 = AxisZ.z;
		result.m23 = Origin.z;
		result.m33 = 1f;
		return result;
	}

	public static Vector3 GetAxis(Matrix4x4 Mat, int AxisIndex)
	{
		return Mat.GetColumn(AxisIndex);
	}

	public static Vector3 GetAxisX(Matrix4x4 Mat)
	{
		return new Vector3(Mat.m00, Mat.m10, Mat.m20);
	}

	public static Vector3 GetAxisY(Matrix4x4 Mat)
	{
		return new Vector3(Mat.m01, Mat.m11, Mat.m21);
	}

	public static Vector3 GetAxisZ(Matrix4x4 Mat)
	{
		return new Vector3(Mat.m02, Mat.m12, Mat.m22);
	}

	public static void GetAxis(Matrix4x4 Mat, ref Vector3 AxisX, ref Vector3 AxisY, ref Vector3 AxisZ)
	{
		AxisX.x = Mat.m00;
		AxisX.y = Mat.m10;
		AxisX.z = Mat.m20;
		AxisY.x = Mat.m01;
		AxisY.y = Mat.m11;
		AxisY.z = Mat.m21;
		AxisZ.x = Mat.m02;
		AxisZ.y = Mat.m12;
		AxisZ.z = Mat.m22;
	}

	public static void SetAxisX(ref Matrix4x4 Mat, Vector3 Axis)
	{
		Mat.m00 = Axis.x;
		Mat.m10 = Axis.y;
		Mat.m20 = Axis.z;
	}

	public static void SetAxisY(ref Matrix4x4 Mat, Vector3 Axis)
	{
		Mat.m01 = Axis.x;
		Mat.m11 = Axis.y;
		Mat.m21 = Axis.z;
	}

	public static void SetAxisZ(ref Matrix4x4 Mat, Vector3 Axis)
	{
		Mat.m02 = Axis.x;
		Mat.m12 = Axis.y;
		Mat.m22 = Axis.z;
	}

	public static void SetAxis(ref Matrix4x4 Mat, Vector3 Axis, int Index)
	{
		Mat[0, Index] = Axis.x;
		Mat[1, Index] = Axis.y;
		Mat[2, Index] = Axis.z;
	}

	public static Vector3 GetOrigin(Matrix4x4 Mat)
	{
		return new Vector3(Mat.m03, Mat.m13, Mat.m23);
	}

	public static void SetOrigin(ref Matrix4x4 Mat, Vector3 Origin)
	{
		Mat.m03 = Origin.x;
		Mat.m13 = Origin.y;
		Mat.m23 = Origin.z;
	}

	public static Vector3 GetEulerAngles(Matrix4x4 Mat)
	{
		Vector3 zero = Vector3.zero;
		if (Mat.m21 < 0.999f)
		{
			if (Mat.m21 > -0.999f)
			{
				zero.x = MathUtils.SanitizeRadians(Mathf.Asin(0f - Mat.m21));
				zero.y = MathUtils.SanitizeRadians(Mathf.Atan2(Mat.m20, Mat.m22));
				zero.z = MathUtils.SanitizeRadians(Mathf.Atan2(Mat.m01, Mat.m11));
			}
			else
			{
				zero.x = (float)Math.PI / 2f;
				zero.y = MathUtils.SanitizeRadians(Mathf.Atan2(Mat.m10, Mat.m00));
			}
		}
		else
		{
			zero.x = 4.712389f;
			zero.y = MathUtils.SanitizeRadians(Mathf.Atan2(0f - Mat.m10, Mat.m00));
		}
		return zero;
	}

	public static void SetEulerAngles(ref Matrix4x4 Mat, Vector3 Angles)
	{
		SetEulerAngles(ref Mat, Angles.x, Angles.y, Angles.z);
	}

	public static void SetEulerAngles(ref Matrix4x4 Mat, float X, float Y, float Z)
	{
		float num = Mathf.Cos(X);
		float num2 = Mathf.Sin(X);
		float num3 = Mathf.Cos(Y);
		float num4 = Mathf.Sin(Y);
		float num5 = Mathf.Cos(Z);
		float num6 = Mathf.Sin(Z);
		Mat.m00 = num2 * num4 * num6 + num3 * num5;
		Mat.m10 = num5 * num2 * num4 - num3 * num6;
		Mat.m20 = num * num4;
		Mat.m01 = num * num6;
		Mat.m11 = num * num5;
		Mat.m21 = 0f - num2;
		Mat.m02 = num3 * num2 * num6 - num5 * num4;
		Mat.m12 = num3 * num5 * num2 + num4 * num6;
		Mat.m22 = num * num3;
	}

	public static Vector3 GetScale(Matrix4x4 Mat)
	{
		return new Vector3(sqrt(Mat.m00 * Mat.m00 + Mat.m10 * Mat.m10 + Mat.m20 * Mat.m20), sqrt(Mat.m01 * Mat.m01 + Mat.m11 * Mat.m11 + Mat.m21 * Mat.m21), sqrt(Mat.m02 * Mat.m02 + Mat.m12 * Mat.m12 + Mat.m22 * Mat.m22));
	}

	public static float GetScaleX(Matrix4x4 Mat)
	{
		return sqrt(Mat.m00 * Mat.m00 + Mat.m10 * Mat.m10 + Mat.m20 * Mat.m20);
	}

	public static float GetScaleY(Matrix4x4 Mat)
	{
		return sqrt(Mat.m01 * Mat.m01 + Mat.m11 * Mat.m11 + Mat.m21 * Mat.m21);
	}

	public static float GetScaleZ(Matrix4x4 Mat)
	{
		return sqrt(Mat.m02 * Mat.m02 + Mat.m12 * Mat.m12 + Mat.m22 * Mat.m22);
	}

	public static Vector3 RemoveScale(ref Matrix4x4 Mat)
	{
		Vector3 scale = GetScale(Mat);
		if (scale.x != 0f)
		{
			float num = 1f / scale.x;
			Mat.m00 *= num;
			Mat.m10 *= num;
			Mat.m20 *= num;
		}
		if (scale.y != 0f)
		{
			float num2 = 1f / scale.y;
			Mat.m01 *= num2;
			Mat.m11 *= num2;
			Mat.m21 *= num2;
		}
		if (scale.z != 0f)
		{
			float num3 = 1f / scale.z;
			Mat.m02 *= num3;
			Mat.m12 *= num3;
			Mat.m22 *= num3;
		}
		return scale;
	}
}
