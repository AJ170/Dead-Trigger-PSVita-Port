using UnityEngine;
using static Unity.Mathematics.math;

public class ShapeCapsule : Shape
{
	private Vector3 m_Center;

	private Vector3[] m_Axis = new Vector3[3];

	private float m_Radius;

	private float m_Length;

	private float m_Scale = 1f;

	private float m_RadiusScaled;

	private float m_LengthHalfScaled;

	private static float[] m_Intersection = new float[4];

	public ShapeCapsule()
	{
	}

	public ShapeCapsule(float Length, float Radius)
	{
		SetRadius(Radius);
		SetLength(Length);
	}

	public void SetFrame(Matrix4x4 Frame, int Dir)
	{
		m_Center = Matrix.GetOrigin(Frame);
		m_Axis[0] = Matrix.GetAxis(Frame, Dir);
		Dir = (Dir + 1) % 3;
		m_Axis[1] = Matrix.GetAxis(Frame, Dir);
		Dir = (Dir + 1) % 3;
		m_Axis[2] = Matrix.GetAxis(Frame, Dir);
	}

	public void SetLength(float Length)
	{
		m_Length = Length;
		m_LengthHalfScaled = Length * m_Scale * 0.5f;
	}

	public void SetRadius(float Radius)
	{
		m_Radius = Radius;
		m_RadiusScaled = Radius * m_Scale;
	}

	public void SetScale(float Scale)
	{
		m_Scale = Scale;
		m_RadiusScaled = m_Radius * m_Scale;
		m_LengthHalfScaled = m_Length * m_Scale * 0.5f;
	}

	public override int RayCast(Vector3 Origin, Vector3 Direction, HitInfo[] Hits)
	{
		int num = 0;
		int num2 = ComputeIntersection(Origin, Direction);
		for (int i = 0; i < num2; i++)
		{
			float num3 = m_Intersection[2 * i];
			if (num3 >= 0f)
			{
				float num4 = m_Intersection[2 * i + 1];
				Vector3 vector = Origin + Direction * num3;
				Vector3 vector2 = m_Center + m_Axis[0] * num4;
				Hits[num].m_Point = vector;
				Hits[num].m_Time = num3;
				Hits[num++].m_Normal = (vector - vector2).normalized;
			}
		}
		return num;
	}

	private int ComputeIntersection(Vector3 Origin, Vector3 Direction)
	{
		Vector3 vector = default(Vector3);
		Vector3 vector2 = default(Vector3);
		Vector3 rhs = Origin - m_Center;
		vector.x = Vector3.Dot(m_Axis[0], rhs);
		vector.y = Vector3.Dot(m_Axis[1], rhs);
		vector.z = Vector3.Dot(m_Axis[2], rhs);
		vector2.x = Vector3.Dot(m_Axis[0], Direction);
		vector2.y = Vector3.Dot(m_Axis[1], Direction);
		vector2.z = Vector3.Dot(m_Axis[2], Direction);
		float num = m_RadiusScaled * m_RadiusScaled;
		float num5;
		float num8;
		float num10;
		if (1f - Mathf.Abs(vector2.x) > 1E-05f)
		{
			float num2 = vector2.y * vector2.y + vector2.z * vector2.z;
			float num3 = vector.y * vector2.y + vector.z * vector2.z;
			float num4 = vector.y * vector.y + vector.z * vector.z - num;
			num5 = num3 * num3 - num2 * num4;
			if (num5 < 0f)
			{
				return 0;
			}
			int num6 = 0;
			if (num5 > 0f)
			{
				float num7 = 1f / num2;
				num8 = sqrt(num5);
				float num9 = (0f - num3 - num8) * num7;
				num10 = vector.x + vector2.x * num9;
				if (Mathf.Abs(num10) <= m_LengthHalfScaled)
				{
					m_Intersection[0] = num9;
					m_Intersection[1] = num10;
					num6 = 2;
				}
				num9 = (0f - num3 + num8) * num7;
				num10 = vector.x + vector2.x * num9;
				if (Mathf.Abs(num10) <= m_LengthHalfScaled)
				{
					m_Intersection[num6++] = num9;
					m_Intersection[num6++] = num10;
					if (num6 == 4)
					{
						return 2;
					}
				}
			}
			else
			{
				float num9 = (0f - num3) / num2;
				num10 = vector.x + vector2.x * num9;
				if (Mathf.Abs(num10) <= m_LengthHalfScaled)
				{
					m_Intersection[0] = num9;
					m_Intersection[1] = num10;
					return 1;
				}
			}
			float num11 = vector.x + m_LengthHalfScaled;
			num3 += num11 * vector2.x;
			num4 += num11 * num11;
			num5 = num3 * num3 - num4;
			if (num5 > 0f)
			{
				num8 = sqrt(num5);
				float num9 = 0f - num3 - num8;
				num10 = vector.x + vector2.x * num9;
				if (num10 <= 0f - m_LengthHalfScaled)
				{
					m_Intersection[num6++] = num9;
					m_Intersection[num6++] = 0f - m_LengthHalfScaled;
					if (num6 == 4)
					{
						if (m_Intersection[0] > num9)
						{
							m_Intersection[2] = m_Intersection[0];
							m_Intersection[0] = num9;
							m_Intersection[3] = m_Intersection[1];
							m_Intersection[1] = 0f - m_LengthHalfScaled;
						}
						return 2;
					}
				}
				num9 = num8 - num3;
				num10 = vector.x + vector2.x * num9;
				if (num10 <= 0f - m_LengthHalfScaled)
				{
					m_Intersection[num6++] = num9;
					m_Intersection[num6++] = 0f - m_LengthHalfScaled;
					if (num6 == 4)
					{
						if (m_Intersection[0] > num9)
						{
							m_Intersection[2] = m_Intersection[0];
							m_Intersection[0] = num9;
							m_Intersection[3] = m_Intersection[1];
							m_Intersection[1] = 0f - m_LengthHalfScaled;
						}
						return 2;
					}
				}
			}
			else if (Mathf.Abs(num5) <= 1E-05f)
			{
				float num9 = 0f - num3;
				num10 = vector.x + vector2.x * num9;
				if (num10 <= 0f - m_LengthHalfScaled)
				{
					m_Intersection[num6++] = num9;
					m_Intersection[num6++] = 0f - m_LengthHalfScaled;
					if (num6 == 4)
					{
						if (m_Intersection[0] > num9)
						{
							m_Intersection[2] = m_Intersection[0];
							m_Intersection[0] = num9;
							m_Intersection[3] = m_Intersection[1];
							m_Intersection[1] = 0f - m_LengthHalfScaled;
						}
						return 2;
					}
				}
			}
			num3 -= 2f * m_LengthHalfScaled * vector2.x;
			num4 -= 4f * m_LengthHalfScaled * vector.x;
			num5 = num3 * num3 - num4;
			if (num5 > 0f)
			{
				num8 = sqrt(num5);
				float num9 = 0f - num3 - num8;
				num10 = vector.x + vector2.x * num9;
				if (num10 >= m_LengthHalfScaled)
				{
					m_Intersection[num6++] = num9;
					m_Intersection[num6++] = m_LengthHalfScaled;
					if (num6 == 4)
					{
						if (m_Intersection[0] > num9)
						{
							m_Intersection[2] = m_Intersection[0];
							m_Intersection[0] = num9;
							m_Intersection[3] = m_Intersection[1];
							m_Intersection[1] = m_LengthHalfScaled;
						}
						return 2;
					}
				}
				num9 = num8 - num3;
				num10 = vector.x + vector2.x * num9;
				if (num10 >= m_LengthHalfScaled)
				{
					m_Intersection[num6++] = num9;
					m_Intersection[num6++] = m_LengthHalfScaled;
					if (num6 == 4)
					{
						if (m_Intersection[0] > num9)
						{
							m_Intersection[2] = m_Intersection[0];
							m_Intersection[0] = num9;
							m_Intersection[3] = m_Intersection[1];
							m_Intersection[1] = m_LengthHalfScaled;
						}
						return 2;
					}
				}
			}
			else if (Mathf.Abs(num5) <= 1E-06f)
			{
				float num9 = 0f - num3;
				num10 = vector.x + vector2.x * num9;
				if (num10 >= m_LengthHalfScaled)
				{
					m_Intersection[num6++] = num9;
					m_Intersection[num6++] = m_LengthHalfScaled;
					if (num6 == 4)
					{
						if (m_Intersection[0] > num9)
						{
							m_Intersection[2] = m_Intersection[0];
							m_Intersection[0] = num9;
							m_Intersection[3] = m_Intersection[1];
							m_Intersection[1] = m_LengthHalfScaled;
						}
						return 2;
					}
				}
			}
			return num6;
		}
		num5 = num - (vector.y * vector.y - vector.z * vector.z);
		if (num5 < 0f)
		{
			return 0;
		}
		num8 = sqrt(num5);
		num10 = num8 + m_LengthHalfScaled;
		if (vector2.x < 0f)
		{
			m_Intersection[0] = vector.x - num10;
			m_Intersection[1] = m_LengthHalfScaled;
			m_Intersection[2] = vector.x + num10;
			m_Intersection[3] = 0f - m_LengthHalfScaled;
		}
		else
		{
			m_Intersection[0] = 0f - vector.x - num10;
			m_Intersection[1] = 0f - m_LengthHalfScaled;
			m_Intersection[2] = 0f - vector.x + num10;
			m_Intersection[3] = m_LengthHalfScaled;
		}
		return 2;
	}

	public override int SphereCast(Vector3 Origin, Vector3 Direction, float Radius, HitInfo[] Hits)
	{
		m_RadiusScaled += Radius;
		int num = RayCast(Origin, Direction, Hits);
		m_RadiusScaled -= Radius;
		for (int i = 0; i < num; i++)
		{
			Hits[i].m_Point -= Hits[i].m_Normal * Radius;
		}
		return num;
	}

	public override void UpdateBBox()
	{
		float num = Mathf.Abs(m_Axis[0].x * m_LengthHalfScaled) + m_RadiusScaled;
		float num2 = Mathf.Abs(m_Axis[0].y * m_LengthHalfScaled) + m_RadiusScaled;
		float num3 = Mathf.Abs(m_Axis[0].z * m_LengthHalfScaled) + m_RadiusScaled;
		m_BBox[0] = m_Center.x - num;
		m_BBox[1] = m_Center.y - num2;
		m_BBox[2] = m_Center.z - num3;
		m_BBox[3] = m_Center.x + num;
		m_BBox[4] = m_Center.y + num2;
		m_BBox[5] = m_Center.z + num3;
	}

	public void Draw(Color Col)
	{
		Matrix4x4 local2World = Matrix.Create(m_Center, m_Axis[0], m_Axis[1], m_Axis[2]);
		DebugDraw.Capsule(Col, m_RadiusScaled, 2f * m_LengthHalfScaled, local2World);
	}

	public void Draw(Color Col, Color BoundsCol)
	{
		DebugDraw.Box(BoundsCol, new Vector3(m_BBox[0], m_BBox[1], m_BBox[2]), new Vector3(m_BBox[3], m_BBox[4], m_BBox[5]));
		Draw(Col);
	}
}
