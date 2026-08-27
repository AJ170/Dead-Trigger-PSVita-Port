using UnityEngine;
using static Unity.Mathematics.math;

[AddComponentMenu("Weapons/GobEffectBlood")]
public class GobEffectBlood : MonoBehaviour
{
	//private WaterDroplets m_WaterDroplets;

	private float m_OriginalVelocityScale = 1f;

	private float m_OriginalEvaporationRate;

	private Color m_OriginalDropletsColor = new Color(0f, 0f, 0f, 0f);

	private bool m_DropletsActiveInPrevFrame;

	private bool m_BloodMode;

	public Color m_BloodColor = Color.red * 0.7f;

	private void Start()
	{
		if ((bool)Camera.main)
		{
			/*m_WaterDroplets = Camera.main.GetComponent<WaterDroplets>();
			if ((bool)m_WaterDroplets)
			{
				GetOriginalWaterDropletsParams();
			}*/
		}
	}

	private void Update()
	{
		/*if ((bool)m_WaterDroplets)
		{
			bool flag = m_WaterDroplets.NumActiveDroplets() > 0;
			if (!flag && m_DropletsActiveInPrevFrame)
			{
				ConfigureDropletsForDroplets();
			}
			m_DropletsActiveInPrevFrame = flag;
		}*/
	}

	public void SpawnBlood(Vector2 bloodNormPos)
	{
		/*if (!m_WaterDroplets)
		{
			return;
		}*/
		ConfigureDropletsForBlood();
		int num = 4;
		float num2 = 0.5f;
		float num3 = 1.5f;
		Vector2 pos = default(Vector2);
		for (int i = 0; i < num; i++)
		{
			pos.x = bloodNormPos.x + Random.Range((0f - num2) * 0.5f, num2 * 0.5f);
			pos.y = bloodNormPos.y + Random.Range((0f - num2) * 0.5f, num2 * 0.5f);
			if (!(pos.x < 0f) && !(pos.x > 1f) && !(pos.y < 0f) && !(pos.y > 1f))
			{
				//m_WaterDroplets.AddMass(pos, Random.Range(0.05f, 0.15f), Random.Range(0.25f, 1f) * num3);
			}
		}
	}

	public bool CleanBlood(Vector2 bloodNormPos, Vector2 normDelta)
	{
		/*if (!m_BloodMode || !m_WaterDroplets || m_WaterDroplets.NumActiveDroplets() == 0)
		{
			return false;
		}*/
		float num = 0.08f;
		int num2 = 1 + (int)(length(normDelta) / num);
		Vector2 vector = normDelta / num2;
		float num3 = -4f;
		for (int i = 0; i < num2; i++)
		{
			Vector2 pos = bloodNormPos - i * vector;
			if (!(pos.x < 0f) && !(pos.x > 1f) && !(pos.y < 0f) && !(pos.y > 1f))
			{
				//m_WaterDroplets.AddMass(pos, num, Random.Range(0.8f, 1f) * num3);
			}
		}
		return true;
	}

	private void ConfigureDropletsForBlood()
	{
		if (!m_BloodMode)
		{
			m_BloodMode = true;
			/*if ((bool)m_WaterDroplets)
			{
				m_WaterDroplets.SetVelocityScale(0.025f);
				m_WaterDroplets.SetEvaporationRate(0.65f);
			}*/
			if ((bool)MFRefractionEffects.Instance)
			{
				MFRefractionEffects.Instance.SetDropletsColor(m_BloodColor);
			}
		}
	}

	private void ConfigureDropletsForDroplets()
	{
		m_BloodMode = false;
		/*if ((bool)m_WaterDroplets)
		{
			m_WaterDroplets.SetVelocityScale(m_OriginalVelocityScale);
			m_WaterDroplets.SetEvaporationRate(m_OriginalEvaporationRate);
		}*/
		if ((bool)MFRefractionEffects.Instance)
		{
			MFRefractionEffects.Instance.SetDropletsColor(m_OriginalDropletsColor);
		}
	}

	private void GetOriginalWaterDropletsParams()
	{
		/*if ((bool)m_WaterDroplets)
		{
			m_OriginalVelocityScale = m_WaterDroplets.GetVelocityScale();
			m_OriginalEvaporationRate = m_WaterDroplets.GetEvaporationRate();
		}*/
	}

	private void OnGUI()
	{
	}
}
