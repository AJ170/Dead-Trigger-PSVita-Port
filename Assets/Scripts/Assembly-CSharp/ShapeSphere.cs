using UnityEngine;
using static Unity.Mathematics.math;
public class ShapeSphere : Shape
{
	private Vector3 m_Center;

	private float m_Radius;

	private float m_RadiusScaled;

	private float m_Scale = 1f;

	public ShapeSphere()
	{
	}

	public ShapeSphere(float Radius)
	{
		m_Radius = Radius;
	}

	public void SetPos(Vector3 Pos)
	{
		m_Center = Pos;
	}

	public void SetRot(Quaternion Rot)
	{
	}

	public void SetFrame(Matrix4x4 Frame)
	{
		m_Center = Matrix.GetOrigin(Frame);
	}

	public void SetRadius(float Radius)
	{
		m_Radius = Radius;
		m_RadiusScaled = m_Radius * m_Scale;
	}

	public void SetScale(float Scale)
	{
		m_Scale = Scale;
		m_RadiusScaled = m_Radius * m_Scale;
	}

	public override int RayCast(Vector3 Origin, Vector3 Direction, HitInfo[] Hits)
	{
		Vector3 vector = Origin - m_Center;
		float num = Vector3.Dot(vector, vector) - m_RadiusScaled * m_RadiusScaled;
		float num2 = 0f - Vector3.Dot(vector, Direction);
		float num3 = num2 * num2 - num;
		int result = 0;
		if (num3 < 0f)
		{
			return 0;
		}
		if (num3 >= 1E-05f)
		{
			float num4 = sqrt(num3);
			float num5 = num2 - num4;
			float num6 = num2 + num4;
			if (num5 > 0f)
			{
				SetupRayHit(ref Hits[result++], Origin, Direction, num5);
			}
			if (num6 > 0f)
			{
				SetupRayHit(ref Hits[result++], Origin, Direction, num6);
			}
		}
		else if (num2 > 0f)
		{
			SetupRayHit(ref Hits[result++], Origin, Direction, num2);
		}
		return result;
	}

	public override int SphereCast(Vector3 Origin, Vector3 Direction, float Radius, HitInfo[] Hits)
	{
		Vector3 vector = Origin - m_Center;
		float num = m_RadiusScaled + Radius;
		float num2 = Vector3.Dot(vector, vector) - num * num;
		float num3 = 0f - Vector3.Dot(vector, Direction);
		float num4 = num3 * num3 - num2;
		int result = 0;
		if (num4 < 0f)
		{
			return 0;
		}
		if (num4 >= 1E-05f)
		{
			float num5 = sqrt(num4);
			float num6 = num3 - num5;
			float num7 = num3 + num5;
			if (num6 > 0f)
			{
				SetupSphereHit(ref Hits[result++], Origin, Direction, num6);
			}
			if (num7 > 0f)
			{
				SetupSphereHit(ref Hits[result++], Origin, Direction, num7);
			}
		}
		else if (num3 > 0f)
		{
			SetupSphereHit(ref Hits[result++], Origin, Direction, num3);
		}
		return result;
	}

	private void SetupRayHit(ref HitInfo Info, Vector3 Origin, Vector3 Direction, float HitTime)
	{
		Info.m_Point = Origin + Direction * HitTime;
		Info.m_Normal = Vector3.Normalize(Info.m_Point - m_Center);
		Info.m_Time = HitTime;
	}

	private void SetupSphereHit(ref HitInfo Info, Vector3 Origin, Vector3 Direction, float HitTime)
	{
		Vector3 vector = Origin + Direction * HitTime;
		Info.m_Normal = Vector3.Normalize(vector - m_Center);
		Info.m_Point = m_Center + Info.m_Normal * m_RadiusScaled;
		Info.m_Time = HitTime;
	}

	public override void UpdateBBox()
	{
		m_BBox[0] = m_Center.x - m_RadiusScaled;
		m_BBox[1] = m_Center.y - m_RadiusScaled;
		m_BBox[2] = m_Center.z - m_RadiusScaled;
		m_BBox[3] = m_Center.x + m_RadiusScaled;
		m_BBox[4] = m_Center.y + m_RadiusScaled;
		m_BBox[5] = m_Center.z + m_RadiusScaled;
	}

	public void Draw(Color Col)
	{
		DebugDraw.Sphere(Col, m_RadiusScaled, m_Center);
	}

	public void Draw(Color Col, Color BoundsCol)
	{
		DebugDraw.Box(BoundsCol, new Vector3(m_BBox[0], m_BBox[1], m_BBox[2]), new Vector3(m_BBox[3], m_BBox[4], m_BBox[5]));
		Draw(Col);
	}
}
