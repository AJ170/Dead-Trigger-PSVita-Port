using System;
using UnityEngine;
using static Unity.Mathematics.math;

public class DebugDraw
{
	private const int MinDetails = 12;

	private const int MidDetails = 24;

	private const int MaxDetails = 36;

	private static readonly Vector3[] DiamondCorner;

	private static readonly Vector3[] BoxCorner;

	private static int m_Steps;

	private static float m_AngleStart;

	private static float m_AngleStep;

	private static float m_DisplayTime;

	private static bool m_DepthTest;

	private static float[] m_SinTable;

	private static float[] m_CosTable;

	private static Matrix4x4[] m_ToAxisRot;

	public static float DisplayTime
	{
		get
		{
			return m_DisplayTime;
		}
		set
		{
			m_DisplayTime = Mathf.Max(0f, value);
		}
	}

	public static bool DepthTest
	{
		get
		{
			return m_DepthTest;
		}
		set
		{
			m_DepthTest = value;
		}
	}

	static DebugDraw()
	{
		DiamondCorner = new Vector3[6]
		{
			new Vector3(-1f, 0f, 0f),
			new Vector3(1f, 0f, 0f),
			new Vector3(0f, -1f, 0f),
			new Vector3(0f, 1f, 0f),
			new Vector3(0f, 0f, -1f),
			new Vector3(0f, 0f, 1f)
		};
		BoxCorner = new Vector3[8]
		{
			new Vector3(-0.5f, -0.5f, -0.5f),
			new Vector3(0.5f, -0.5f, -0.5f),
			new Vector3(0.5f, -0.5f, 0.5f),
			new Vector3(-0.5f, -0.5f, 0.5f),
			new Vector3(-0.5f, 0.5f, -0.5f),
			new Vector3(0.5f, 0.5f, -0.5f),
			new Vector3(0.5f, 0.5f, 0.5f),
			new Vector3(-0.5f, 0.5f, 0.5f)
		};
		m_DisplayTime = 0f;
		m_DepthTest = true;
		m_SinTable = new float[36];
		m_CosTable = new float[36];
		m_ToAxisRot = new Matrix4x4[2]
		{
			Matrix4x4.identity,
			Matrix4x4.identity
		};
		Matrix.SetEulerAngles(ref m_ToAxisRot[0], 0f, 0f, (float)Math.PI / 2f);
		Matrix.SetEulerAngles(ref m_ToAxisRot[1], 0f, (float)Math.PI / 2f, 0f);
	}

	public static void Box(Color Col, BoxCollider BoxColl, float Scale = 1f)
	{
		Matrix4x4 Mat = BoxColl.transform.localToWorldMatrix;
		Vector3 vector = BoxColl.size * (2f * Scale);
		Matrix.SetOrigin(ref Mat, Mat.MultiplyPoint3x4(BoxColl.center));
		Box(Col, vector.x, vector.y, vector.z, Mat);
	}

	public static void Box(Color Col, Bounds AxisAlignedBox)
	{
		Box(Col, AxisAlignedBox.min, AxisAlignedBox.max);
	}

	public static void Box(Color Col, Vector3 Min, Vector3 Max)
	{
		Vector3[] array = new Vector3[8];
		Vector3 vector = (Max + Min) * 0.5f;
		Vector3 a = Max - Min;
		for (int i = 0; i < 8; i++)
		{
			array[i] = vector + Vector3.Scale(a, BoxCorner[i]);
		}
		Box(Col, array);
	}

	public static void Box(Color Col, float Width, float Height, float Depth, Vector3 Center)
	{
		Vector3[] array = new Vector3[8];
		Vector3 a = new Vector3(Width, Height, Depth);
		for (int i = 0; i < 8; i++)
		{
			array[i] = Center + Vector3.Scale(a, BoxCorner[i]);
		}
		Box(Col, array);
	}

	public static void Box(Color Col, Vector3 Extents, Matrix4x4 Local2World)
	{
		Vector3[] array = new Vector3[8];
		for (int i = 0; i < 8; i++)
		{
			array[i] = Local2World.MultiplyPoint3x4(Vector3.Scale(Extents, BoxCorner[i]));
		}
		Box(Col, array);
	}

	public static void Box(Color Col, float Width, float Height, float Depth, Matrix4x4 Local2World)
	{
		Vector3[] array = new Vector3[8];
		Vector3 a = new Vector3(Width, Height, Depth);
		for (int i = 0; i < 8; i++)
		{
			array[i] = Local2World.MultiplyPoint3x4(Vector3.Scale(a, BoxCorner[i]));
		}
		Box(Col, array);
	}

	private static void Box(Color Col, Vector3[] Corners)
	{
		Line(Col, Corners[0], Corners[1]);
		Line(Col, Corners[1], Corners[2]);
		Line(Col, Corners[2], Corners[3]);
		Line(Col, Corners[3], Corners[0]);
		Line(Col, Corners[4], Corners[5]);
		Line(Col, Corners[5], Corners[6]);
		Line(Col, Corners[6], Corners[7]);
		Line(Col, Corners[7], Corners[4]);
		Line(Col, Corners[0], Corners[4]);
		Line(Col, Corners[1], Corners[5]);
		Line(Col, Corners[2], Corners[6]);
		Line(Col, Corners[3], Corners[7]);
	}

	public static void Capsule(Color Col, CapsuleCollider CapColl, float Scale = 1f)
	{
		Matrix4x4 Mat = CapColl.transform.localToWorldMatrix;
		float num = Mathf.Max(0f, CapColl.height - 2f * CapColl.radius);
		Matrix.SetOrigin(ref Mat, Mat.MultiplyPoint3x4(CapColl.center));
		if (CapColl.direction > 0)
		{
			Mat *= m_ToAxisRot[CapColl.direction - 1];
		}
		Capsule(Col, CapColl.radius * Scale, num * Scale, Mat);
	}

	public static void Capsule(Color Col, float Radius, Vector3 From, Vector3 To)
	{
		Vector3 normal = To - From;
		Vector3 tangent = default(Vector3);
		Vector3 binormal = default(Vector3);
		float num = Vector3.Magnitude(normal);
		Vector3.OrthoNormalize(ref normal, ref tangent, ref binormal);
		Vector3 origin = From + normal * (num * 0.5f);
		Matrix4x4 local2World = Matrix.Create(origin, normal, tangent, binormal);
		Capsule(Col, Radius, num, local2World);
	}

	public static void Capsule(Color Col, float Radius, float Height, Matrix4x4 Local2World)
	{
		Vector3 Res = default(Vector3);
		Vector3 Res2 = default(Vector3);
		Vector3 vector = default(Vector3);
		Vector3 Res3 = default(Vector3);
		float num = Height * 0.5f;
		SetDetails(Radius);
		int i;
		for (i = 0; i < m_Steps; i++)
		{
			m_CosTable[i] = GetCos(i) * Radius;
			m_SinTable[i] = GetSin(i) * Radius;
		}
		TransformToWorld(num, Radius, 0f, Local2World, ref Res);
		TransformToWorld(0f - num, Radius, 0f, Local2World, ref Res2);
		Line(Col, Res, Res2);
		TransformToWorld(num, 0f - Radius, 0f, Local2World, ref Res);
		TransformToWorld(0f - num, 0f - Radius, 0f, Local2World, ref Res2);
		Line(Col, Res, Res2);
		TransformToWorld(num, 0f, Radius, Local2World, ref Res);
		TransformToWorld(0f - num, 0f, Radius, Local2World, ref Res2);
		Line(Col, Res, Res2);
		TransformToWorld(num, 0f, 0f - Radius, Local2World, ref Res);
		TransformToWorld(0f - num, 0f, 0f - Radius, Local2World, ref Res2);
		Line(Col, Res, Res2);
		i = m_Steps - 1;
		TransformToWorld(0f - num, m_CosTable[i], m_SinTable[i], Local2World, ref Res3);
		TransformToWorld(num, m_CosTable[i], m_SinTable[i], Local2World, ref Res2);
		float y;
		float z;
		for (i = 0; i < m_Steps; i++)
		{
			vector = Res3;
			y = m_CosTable[i];
			Res = Res2;
			z = m_SinTable[i];
			TransformToWorld(0f - num, y, z, Local2World, ref Res3);
			TransformToWorld(num, y, z, Local2World, ref Res2);
			Line(Col, vector, Res3);
			Line(Col, Res, Res2);
		}
		int num2 = m_Steps / 2;
		y = m_CosTable[0];
		z = m_SinTable[0] + num;
		TransformToWorld(z, y, 0f, Local2World, ref Res2);
		TransformToWorld(z, 0f, y, Local2World, ref Res3);
		for (i = 1; i <= num2; i++)
		{
			vector = Res3;
			y = m_CosTable[i];
			Res = Res2;
			z = m_SinTable[i] + num;
			TransformToWorld(z, 0f, y, Local2World, ref Res3);
			TransformToWorld(z, y, 0f, Local2World, ref Res2);
			Line(Col, vector, Res3);
			Line(Col, Res, Res2);
		}
		y = m_CosTable[0];
		z = 0f - m_SinTable[0] - num;
		TransformToWorld(z, 0f, y, Local2World, ref Res3);
		TransformToWorld(z, y, 0f, Local2World, ref Res2);
		for (i = 1; i <= num2; i++)
		{
			vector = Res3;
			y = m_CosTable[i];
			Res = Res2;
			z = 0f - m_SinTable[i] - num;
			TransformToWorld(z, 0f, y, Local2World, ref Res3);
			TransformToWorld(z, y, 0f, Local2World, ref Res2);
			Line(Col, vector, Res3);
			Line(Col, Res, Res2);
		}
	}

	public static void Collider(Color Col, Collider Coll, float Scale = 1f)
	{
		BoxCollider boxCollider = Coll as BoxCollider;
		if (boxCollider != null)
		{
			Box(Col, boxCollider, Scale);
			return;
		}
		CapsuleCollider capsuleCollider = Coll as CapsuleCollider;
		if (capsuleCollider != null)
		{
			Capsule(Col, capsuleCollider, Scale);
			return;
		}
		SphereCollider sphereCollider = Coll as SphereCollider;
		if (sphereCollider != null)
		{
			Sphere(Col, sphereCollider, Scale);
		}
	}

	public static void Cone(Color Col, float Height, float Radius, Matrix4x4 Local2World)
	{
		Cone(Col, Height, 0f, Radius, Local2World);
	}

	public static void Cone(Color Col, float Height, float RadiusA, float RadiusB, Matrix4x4 Local2World)
	{
		Vector3 Res = default(Vector3);
		Vector3 Res2 = default(Vector3);
		Vector3 vector = default(Vector3);
		Vector3 Res3 = default(Vector3);
		float num = Height * 0.5f;
		TransformToWorld(0f - num, RadiusB, 0f, Local2World, ref Res);
		TransformToWorld(num, RadiusA, 0f, Local2World, ref Res2);
		Line(Col, Res, Res2);
		TransformToWorld(0f - num, 0f - RadiusB, 0f, Local2World, ref Res);
		TransformToWorld(num, 0f - RadiusA, 0f, Local2World, ref Res2);
		Line(Col, Res, Res2);
		TransformToWorld(0f - num, 0f, RadiusB, Local2World, ref Res);
		TransformToWorld(num, 0f, RadiusA, Local2World, ref Res2);
		Line(Col, Res, Res2);
		TransformToWorld(0f - num, 0f, 0f - RadiusB, Local2World, ref Res);
		TransformToWorld(num, 0f, 0f - RadiusA, Local2World, ref Res2);
		Line(Col, Res, Res2);
		if (RadiusA > 0f)
		{
			int idx = SetDetails(RadiusA) - 1;
			TransformToWorld(num, GetCos(idx) * RadiusA, GetSin(idx) * RadiusA, Local2World, ref Res3);
			for (idx = 0; idx < m_Steps; idx++)
			{
				vector = Res3;
				TransformToWorld(num, GetCos(idx) * RadiusA, GetSin(idx) * RadiusA, Local2World, ref Res3);
				Line(Col, vector, Res3);
			}
		}
		if (RadiusB > 0f)
		{
			int idx = SetDetails(RadiusB) - 1;
			TransformToWorld(0f - num, GetCos(idx) * RadiusB, GetSin(idx) * RadiusB, Local2World, ref Res3);
			for (idx = 0; idx < m_Steps; idx++)
			{
				vector = Res3;
				TransformToWorld(0f - num, GetCos(idx) * RadiusB, GetSin(idx) * RadiusB, Local2World, ref Res3);
				Line(Col, vector, Res3);
			}
		}
	}

	public static void ConeSphere(Color Col, float Height, float RadiusA, float RadiusB, Matrix4x4 Local2World)
	{
		Vector3 Res = default(Vector3);
		Vector3 Res2 = default(Vector3);
		Vector3 vector = default(Vector3);
		Vector3 Res3 = default(Vector3);
		float num = Height * 0.5f;
		TransformToWorld(0f - num, RadiusB, 0f, Local2World, ref Res);
		TransformToWorld(num, RadiusA, 0f, Local2World, ref Res2);
		Line(Col, Res, Res2);
		TransformToWorld(0f - num, 0f - RadiusB, 0f, Local2World, ref Res);
		TransformToWorld(num, 0f - RadiusA, 0f, Local2World, ref Res2);
		Line(Col, Res, Res2);
		TransformToWorld(0f - num, 0f, RadiusB, Local2World, ref Res);
		TransformToWorld(num, 0f, RadiusA, Local2World, ref Res2);
		Line(Col, Res, Res2);
		TransformToWorld(0f - num, 0f, 0f - RadiusB, Local2World, ref Res);
		TransformToWorld(num, 0f, 0f - RadiusA, Local2World, ref Res2);
		Line(Col, Res, Res2);
		float num2 = RadiusB - RadiusA;
		float num3 = num2 / Height;
		float num4 = sqrt(Height * Height + num2 * num2) / Height;
		if (RadiusB > 0f)
		{
			int idx = SetDetails(RadiusB) - 1;
			TransformToWorld(0f - num, GetCos(idx) * RadiusB, GetSin(idx) * RadiusB, Local2World, ref Res3);
			for (idx = 0; idx < m_Steps; idx++)
			{
				vector = Res3;
				TransformToWorld(0f - num, GetCos(idx) * RadiusB, GetSin(idx) * RadiusB, Local2World, ref Res3);
				Line(Col, vector, Res3);
			}
			float num5 = RadiusB * num3;
			float num6 = RadiusB * num4;
			float num7 = Mathf.Acos(RadiusB / num6);
			if (RadiusA < RadiusB)
			{
				SetDetails(num6, 0f - num7, (float)Math.PI + 2f * num7);
			}
			else
			{
				SetDetails(num6, num7, (float)Math.PI - 2f * num7);
			}
			float num8 = GetCos(0) * num6;
			num2 = (0f - GetSin(0)) * num6 - num - num5;
			TransformToWorld(num2, 0f, num8, Local2World, ref Res3);
			TransformToWorld(num2, num8, 0f, Local2World, ref Res2);
			for (idx = 1; idx <= m_Steps; idx++)
			{
				vector = Res3;
				num8 = GetCos(idx) * num6;
				Res = Res2;
				num2 = (0f - GetSin(idx)) * num6 - num - num5;
				TransformToWorld(num2, 0f, num8, Local2World, ref Res3);
				TransformToWorld(num2, num8, 0f, Local2World, ref Res2);
				Line(Col, vector, Res3);
				Line(Col, Res, Res2);
			}
		}
		if (RadiusA > 0f)
		{
			int idx = SetDetails(RadiusA) - 1;
			TransformToWorld(num, GetCos(idx) * RadiusA, GetSin(idx) * RadiusA, Local2World, ref Res3);
			for (idx = 0; idx < m_Steps; idx++)
			{
				vector = Res3;
				TransformToWorld(num, GetCos(idx) * RadiusA, GetSin(idx) * RadiusA, Local2World, ref Res3);
				Line(Col, vector, Res3);
			}
			float num9 = RadiusA * num3;
			float num10 = RadiusA * num4;
			float num11 = Mathf.Acos(RadiusA / num10);
			if (RadiusA > RadiusB)
			{
				SetDetails(num10, 0f - num11, (float)Math.PI + 2f * num11);
			}
			else
			{
				SetDetails(num10, num11, (float)Math.PI - 2f * num11);
			}
			float num8 = GetCos(0) * num10;
			num2 = GetSin(0) * num10 + num - num9;
			TransformToWorld(num2, 0f, num8, Local2World, ref Res3);
			TransformToWorld(num2, num8, 0f, Local2World, ref Res2);
			for (idx = 1; idx <= m_Steps; idx++)
			{
				vector = Res3;
				num8 = GetCos(idx) * num10;
				Res = Res2;
				num2 = GetSin(idx) * num10 + num - num9;
				TransformToWorld(num2, 0f, num8, Local2World, ref Res3);
				TransformToWorld(num2, num8, 0f, Local2World, ref Res2);
				Line(Col, vector, Res3);
				Line(Col, Res, Res2);
			}
		}
	}

	public static void Cylinder(Color Col, float Radius, Vector3 From, Vector3 To)
	{
		Vector3 normal = To - From;
		Vector3 tangent = default(Vector3);
		Vector3 binormal = default(Vector3);
		float num = Vector3.Magnitude(normal);
		Vector3.OrthoNormalize(ref normal, ref tangent, ref binormal);
		Vector3 origin = From + normal * (num * 0.5f);
		Matrix4x4 local2World = Matrix.Create(origin, normal, tangent, binormal);
		Cylinder(Col, num, Radius, local2World);
	}

	public static void Cylinder(Color Col, float Height, float Radius, Matrix4x4 Local2World)
	{
		Vector3 Res = default(Vector3);
		Vector3 Res2 = default(Vector3);
		Vector3 vector = default(Vector3);
		Vector3 Res3 = default(Vector3);
		float num = Height * 0.5f;
		TransformToWorld(0f - num, Radius, 0f, Local2World, ref Res);
		TransformToWorld(num, Radius, 0f, Local2World, ref Res2);
		Line(Col, Res, Res2);
		TransformToWorld(0f - num, 0f - Radius, 0f, Local2World, ref Res);
		TransformToWorld(num, 0f - Radius, 0f, Local2World, ref Res2);
		Line(Col, Res, Res2);
		TransformToWorld(0f - num, 0f, Radius, Local2World, ref Res);
		TransformToWorld(num, 0f, Radius, Local2World, ref Res2);
		Line(Col, Res, Res2);
		TransformToWorld(0f - num, 0f, 0f - Radius, Local2World, ref Res);
		TransformToWorld(num, 0f, 0f - Radius, Local2World, ref Res2);
		Line(Col, Res, Res2);
		int idx = SetDetails(Radius) - 1;
		float y = GetCos(idx) * Radius;
		float z = GetSin(idx) * Radius;
		TransformToWorld(0f - num, y, z, Local2World, ref Res3);
		TransformToWorld(num, y, z, Local2World, ref Res2);
		for (idx = 0; idx < m_Steps; idx++)
		{
			vector = Res3;
			y = GetCos(idx) * Radius;
			Res = Res2;
			z = GetSin(idx) * Radius;
			TransformToWorld(0f - num, y, z, Local2World, ref Res3);
			TransformToWorld(num, y, z, Local2World, ref Res2);
			Line(Col, vector, Res3);
			Line(Col, Res, Res2);
		}
	}

	public static void CoordSystem(float Size, Matrix4x4 Local2World)
	{
		Vector3 axisX = Matrix.GetAxisX(Local2World);
		Vector3 axisY = Matrix.GetAxisY(Local2World);
		Vector3 axisZ = Matrix.GetAxisZ(Local2World);
		Vector3 origin = Matrix.GetOrigin(Local2World);
		axisX *= Size;
		axisY *= Size;
		axisZ *= Size;
		Line(Color.red, origin, origin + axisX);
		Line(Color.green, origin, origin + axisY);
		Line(Color.blue, origin, origin + axisZ);
	}

	public static void Diamond(Color Col, float Size, Vector3 Center)
	{
		Vector3[] array = new Vector3[6];
		for (int i = 0; i < 6; i++)
		{
			array[i] = Center + Size * DiamondCorner[i];
		}
		Line(Col, array[0], array[2]);
		Line(Col, array[1], array[3]);
		Line(Col, array[2], array[1]);
		Line(Col, array[3], array[0]);
		Line(Col, array[4], array[0]);
		Line(Col, array[4], array[1]);
		Line(Col, array[4], array[2]);
		Line(Col, array[4], array[3]);
		Line(Col, array[5], array[0]);
		Line(Col, array[5], array[1]);
		Line(Col, array[5], array[2]);
		Line(Col, array[5], array[3]);
	}

	public static void Line(Color Col, Vector3 From, Vector3 To)
	{
		Debug.DrawLine(From, To, Col, m_DisplayTime, m_DepthTest);
	}

	public static void Line(Color Col, Vector3 From, Vector3 To, Matrix4x4 Local2World)
	{
		Line(Col, Local2World.MultiplyPoint3x4(From), Local2World.MultiplyPoint3x4(To));
	}

	public static void LineOriented(Color Col, Vector3 From, Vector3 To, float ArrowSize = 0.1f)
	{
		Vector3 tangent = default(Vector3);
		Vector3 binormal = default(Vector3);
		Vector3 normal = To - From;
		Vector3.OrthoNormalize(ref normal, ref tangent, ref binormal);
		Vector3 vector = To - normal * ArrowSize;
		tangent *= 0.35f * ArrowSize;
		binormal *= 0.35f * ArrowSize;
		Line(Col, To, From);
		Line(Col, To, vector + tangent);
		Line(Col, To, vector - tangent);
		Line(Col, To, vector + binormal);
		Line(Col, To, vector - binormal);
	}

	public static void LineOriented(Color Col, Vector3 From, Vector3 To, Matrix4x4 Local2World)
	{
		LineOriented(Col, Local2World.MultiplyPoint3x4(From), Local2World.MultiplyPoint3x4(To));
	}

	public static void Sphere(Color Col, SphereCollider SphColl, float Scale = 1f)
	{
		Matrix4x4 Mat = SphColl.transform.localToWorldMatrix;
		Matrix.SetOrigin(ref Mat, Mat.MultiplyPoint3x4(SphColl.center));
		Sphere(Col, SphColl.radius * Scale, Mat);
	}

	public static void Sphere(Color Col, float Radius, Vector3 Center)
	{
		Sphere(Col, Radius, Matrix.CreateTranslation(Center));
	}

	public static void Sphere(Color Col, float Radius, Matrix4x4 Local2World)
	{
		Vector3 Res = default(Vector3);
		Vector3 vector = default(Vector3);
		Vector3 Res2 = default(Vector3);
		Vector3 vector2 = default(Vector3);
		Vector3 Res3 = default(Vector3);
		Vector3 vector3 = default(Vector3);
		Matrix4x4 mat = Local2World * Matrix.CreateScale(Radius);
		SetDetails(Radius);
		float cos = GetCos(0);
		float sin = GetSin(0);
		TransformToWorld(cos, sin, 0f, mat, ref Res);
		TransformToWorld(cos, 0f, sin, mat, ref Res2);
		TransformToWorld(0f, cos, sin, mat, ref Res3);
		for (int num = m_Steps; num >= 0; num--)
		{
			vector = Res;
			vector2 = Res2;
			vector3 = Res3;
			cos = GetCos(num);
			sin = GetSin(num);
			TransformToWorld(cos, sin, 0f, mat, ref Res);
			TransformToWorld(cos, 0f, sin, mat, ref Res2);
			TransformToWorld(0f, cos, sin, mat, ref Res3);
			Line(Col, Res, vector);
			Line(Col, Res2, vector2);
			Line(Col, Res3, vector3);
		}
	}

	private static int GetDetailSteps(float Radius)
	{
		return (Radius < 0.05f) ? 12 : ((!(Radius < 0.3f)) ? 36 : 24);
	}

	private static int SetDetails(float Radius)
	{
		m_Steps = GetDetailSteps(Radius);
		m_AngleStart = 0f;
		m_AngleStep = (float)Math.PI * 2f / (float)m_Steps;
		return m_Steps;
	}

	private static int SetDetails(float Radius, float Start, float Range)
	{
		m_Steps = GetDetailSteps(Radius);
		m_Steps = Mathf.CeilToInt((float)m_Steps * (Range / ((float)Math.PI * 2f)));
		m_AngleStart = Start;
		m_AngleStep = Range / (float)m_Steps;
		return m_Steps;
	}

	private static float GetSin(int Idx)
	{
		return Mathf.Sin(m_AngleStart + m_AngleStep * (float)Idx);
	}

	private static float GetCos(int Idx)
	{
		return Mathf.Cos(m_AngleStart + m_AngleStep * (float)Idx);
	}

	private static void TransformToWorld(float X, float Y, float Z, Matrix4x4 Mat, ref Vector3 Res)
	{
		Res.x = X * Mat.m00 + Y * Mat.m01 + Z * Mat.m02 + Mat.m03;
		Res.y = X * Mat.m10 + Y * Mat.m11 + Z * Mat.m12 + Mat.m13;
		Res.z = X * Mat.m20 + Y * Mat.m21 + Z * Mat.m22 + Mat.m23;
	}
}
