using UnityEngine;

public class ComponentSensors : MonoBehaviour
{
	public SensorBase[] Sensors = new SensorBase[4];

	private AgentHuman Owner;

	private float UpdateTime;

	private int UpdateSensorIndex;

	private void Awake()
	{
		Owner = GetComponent<AgentHuman>();
	}

	private void Update()
	{
		if (UpdateTime < Time.timeSinceLevelLoad)
		{
			if (Sensors[UpdateSensorIndex] != null && Sensors[UpdateSensorIndex].Active)
			{
				Sensors[UpdateSensorIndex].Update();
			}
			UpdateSensorIndex++;
			if (UpdateSensorIndex == Sensors.Length)
			{
				UpdateSensorIndex = 0;
			}
			UpdateTime = Time.timeSinceLevelLoad + 0.05f;
		}
	}

	public void AddSensor(E_SensorType sensorType, bool activate)
	{
		SensorBase sensorBase = SensorFactory.Create(sensorType, Owner);
		sensorBase.Active = activate;
		Sensors[(int)sensorType] = sensorBase;
	}

	public void RemoveSensor(E_SensorType sensorType)
	{
		Sensors[(int)sensorType] = null;
	}

	public void RemoveAllSensors()
	{
		for (int i = 0; i < Sensors.Length; i++)
		{
			Sensors[i] = null;
		}
	}

	public void ActivateSensor(E_SensorType sensorType)
	{
		if (Sensors[(int)sensorType] != null)
		{
			Sensors[(int)sensorType].Active = true;
		}
		else
		{
			Debug.LogError(string.Concat("Sensor ", sensorType, " : is not added, cannot active it"));
		}
	}

	public void ActivateAllSensors()
	{
		for (int i = 0; i < Sensors.Length; i++)
		{
			if (Sensors[i] != null)
			{
				Sensors[i].Active = true;
			}
		}
	}

	public void DeactivateSensor(E_SensorType sensorType)
	{
		if (Sensors[(int)sensorType] != null)
		{
			Sensors[(int)sensorType].Reset();
			Sensors[(int)sensorType].Active = false;
		}
	}

	public void DeactivateAllSensors()
	{
		for (int i = 0; i < Sensors.Length; i++)
		{
			if (Sensors[i] != null)
			{
				Sensors[i].Reset();
				Sensors[i].Active = false;
			}
		}
	}
}
