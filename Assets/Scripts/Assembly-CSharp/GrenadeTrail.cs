using UnityEngine;
using static Unity.Mathematics.math;

public class GrenadeTrail : MonoBehaviour
{
	private const float FADEOUT_TIME = 0.7f;

	public float m_BeamNoFadeDuration = 1f;

	public float m_BeamFadeDuration = 2f;

	public float m_MaxSegments = 10f;

	public float m_SegmentLength = 1f;

	private float m_InitTimer;

	private float m_FadeOutTimer;

	private LineRenderer m_LineRenderer;

	private Vector3 m_TrailInitPos;

	private int m_VertexCount;

	private Vector3 m_TrailAPos;

	private void Awake()
	{
		m_LineRenderer = GetComponent<LineRenderer>();
	}

	private void Update()
	{
		float num = 1f;
		if (Time.timeSinceLevelLoad - m_InitTimer > m_BeamNoFadeDuration && m_BeamFadeDuration > 0f)
		{
			float num2 = Time.timeSinceLevelLoad - m_InitTimer - m_BeamNoFadeDuration;
			num = 1f - num2 / m_BeamFadeDuration;
		}
		if (m_FadeOutTimer > 0f)
		{
			num *= 1f - Mathf.Clamp((Time.timeSinceLevelLoad - m_FadeOutTimer) / 0.7f, 0f, 1f);
		}
		if (!Mathf.Approximately(num, 1f))
		{
			Vector4 vector = m_LineRenderer.material.GetVector("_TintColor");
			vector.w = num;
			m_LineRenderer.material.SetVector("_TintColor", vector);
		}
	}

	public void InitTrail(Vector3 inPos)
	{
		m_InitTimer = Time.timeSinceLevelLoad;
		m_FadeOutTimer = -1f;
		m_TrailInitPos = inPos;
		m_TrailAPos = inPos;
		if (m_LineRenderer != null)
		{
			m_VertexCount = 2;
			m_LineRenderer.useWorldSpace = true;
			m_LineRenderer.SetVertexCount(m_VertexCount);
			m_LineRenderer.SetPosition(0, m_TrailInitPos);
			m_LineRenderer.SetPosition(1, m_TrailInitPos);
		}
		Vector4 vector = m_LineRenderer.material.GetVector("_TintColor");
		vector.w = 1f;
		m_LineRenderer.material.SetVector("_TintColor", vector);
	}

	protected void AddTrailPos(Vector3 inPos)
	{
		if (!(m_LineRenderer == null))
		{
			m_TrailAPos = inPos;
			m_LineRenderer.SetPosition(m_VertexCount - 1, inPos);
			m_VertexCount++;
			m_LineRenderer.SetVertexCount(m_VertexCount);
			m_LineRenderer.SetPosition(m_VertexCount - 1, inPos);
		}
	}

	public void UpdateTrailPos(Vector3 inPos)
	{
		if (!(m_LineRenderer == null))
		{
			m_LineRenderer.SetPosition(m_VertexCount - 1, inPos);
		}
	}

	public void UpdateAddTrailPos(Vector3 inPos)
	{
		if (m_LineRenderer == null)
		{
			return;
		}
		float num = length(inPos - m_TrailAPos);
		if (num > m_SegmentLength)
		{
			Vector3 normalized = (inPos - m_TrailAPos).normalized;
			while (num > m_SegmentLength)
			{
				AddTrailPos(m_TrailAPos + normalized * m_SegmentLength);
				num -= m_SegmentLength;
			}
		}
		UpdateTrailPos(inPos);
	}

	public void FadeOut()
	{
		m_FadeOutTimer = Time.timeSinceLevelLoad;
	}

	public bool IsVisible()
	{
		return Time.timeSinceLevelLoad - m_InitTimer < m_BeamNoFadeDuration + m_BeamFadeDuration;
	}
}
