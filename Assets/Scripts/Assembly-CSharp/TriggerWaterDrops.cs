using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[AddComponentMenu("Triggers/Spawn water drops")]
public class TriggerWaterDrops : MonoBehaviour
{
	public float m_NumDropsPerSecond = 10f;

	private float m_NumDropsToGenerate;

	private float m_LastGenerateTime = -1f;

	private float m_OrigVelocityScale = 1f;

	private float m_OrigEvaporation;

	//private WaterDroplets m_WaterDroplets;

	private void Start()
	{
		if ((bool)Camera.main)
		{
			/*m_WaterDroplets = Camera.main.GetComponent<WaterDroplets>();
			if ((bool)m_WaterDroplets)
			{
				m_OrigVelocityScale = m_WaterDroplets.GetVelocityScale();
				m_OrigEvaporation = m_WaterDroplets.GetEvaporationRate();
			}*/
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if ((bool)Player.Instance && other == Player.Instance.Owner.CharacterController)
		{
			GenerateDrops();
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if ((bool)Player.Instance && other == Player.Instance.Owner.CharacterController)
		{
			/*if ((bool)m_WaterDroplets)
			{
				m_WaterDroplets.SetVelocityScale(m_OrigVelocityScale);
				m_WaterDroplets.SetEvaporationRate(m_OrigEvaporation);
			}*/
			if ((bool)MFRefractionEffects.Instance)
			{
				MFRefractionEffects.Instance.SetDropletsColor(new Color(0f, 0f, 0f, 0f));
			}
		}
	}

	private void OnTriggerExit()
	{
		m_LastGenerateTime = -1f;
	}

	private void GenerateDrops()
	{
		if (m_LastGenerateTime < 0f)
		{
			m_LastGenerateTime = Time.time;
		}
		float num = Time.time - m_LastGenerateTime;
		m_LastGenerateTime = Time.time;
		m_NumDropsToGenerate += num * m_NumDropsPerSecond;
		int num2 = Mathf.FloorToInt(m_NumDropsToGenerate);
		m_NumDropsToGenerate -= num2;
		// The original refraction system (MFRefractionEffects + the deleted
		// WaterDroplets sim) is gone; drive the ScreenDrops sprite overlay instead.
		if (num2 > 0)
		{
			ScreenDrops.EnsureInstance().SpawnDrops((uint)num2);
		}
	}
}
