using UnityEngine;
using static Unity.Mathematics.math;
public class Roots
{
	public const float ZeroEps = 1E-06f;

	public static float Linear(float A, float B)
	{
		if (Mathf.Abs(A) > 1E-06f)
		{
			return (0f - B) / A;
		}
		return 0f;
	}

	public static int Quadratic(float A, float B, float C, float[] X)
	{
		if (Mathf.Abs(A) > 1E-06f)
		{
			float num = (0f - B) / A;
			float num2 = C / A;
			float num3 = num * num - 4f * num2;
			if (num3 > 0f)
			{
				float num4 = sqrt(num3);
				X[0] = (num - num4) * 0.5f;
				X[1] = (num + num4) * 0.5f;
				return 2;
			}
			if (num3 < 0f)
			{
				return 0;
			}
			X[0] = num * 0.5f;
		}
		else
		{
			X[0] = Linear(B, C);
		}
		return 1;
	}
}
