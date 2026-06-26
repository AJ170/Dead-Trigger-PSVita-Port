using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BloomSettings
{
	[Range(0.0f, 1.5f)] public float threshold = 0.25f;
	[Range(0.00f, 4.0f)] public float intensity = 1.0f;
	[Range(0.25f, 5.5f)] public float blurSize = 1.0f;
	[Range(1, 4)] public int blurIterations = 2;
}

//A handler for Bloom settings
public class MobileBloomSettingsHandler : MonoBehaviour {

	public List<BloomSettings> BloomOptions = new List<BloomSettings>();
	public FasterMobileBloom ourBloom;

	// Use this for initialization
	/*
	IEnumerator Start()
	{
		
		while (UISettingsHandler.Instance == null)
		{
			yield return null;
		}
		//UISettingsHandler.Instance.OnSettingsChanged.AddListener(UpdateInternalSettings);
		UpdateInternalSettings();
	}*/

	void SetBloomLevel(int toThis)
    {
		if (ourBloom)
        {
			ourBloom.enabled = toThis != 0;	//Disable if we're not doing anything
			if (toThis > 0)
            {
				SetBloomValues(BloomOptions[toThis - 1]);
            }
        }
    }

	void SetBloomValues(BloomSettings newSettings)
    {
		ourBloom.threshold = newSettings.threshold;
		ourBloom.intensity = newSettings.intensity;
		ourBloom.blurSize = newSettings.blurSize;
		ourBloom.blurIterations = newSettings.blurIterations;
    }

	void UpdateInternalSettings()
	{
		/*
		if (PlayerPrefs.HasKey("bloom_level"))
		{
			SetBloomLevel(UISettingsHandler.Instance.getSettingInt("bloom_level"));
		}
		else
		{
			SetBloomLevel(0);
		}*/

	}

	void OnDestroy()
	{
		//UISettingsHandler.Instance.OnSettingsChanged.RemoveListener(UpdateInternalSettings);
	}
}
