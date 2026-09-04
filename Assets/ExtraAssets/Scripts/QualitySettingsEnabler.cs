using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class QualitySettingsEnabler : MonoBehaviour {
	public int qualityLevel = 2;
	public TextMeshProUGUI indicatorText;

	void Awake() {
		DontDestroyOnLoad (gameObject);
	}

	// Update is called once per frame
	void Update () {
		if (Input.GetButtonDown ("DPad Up") || Input.GetKeyDown (KeyCode.Q)) {
			qualityLevel++;
			qualityLevel = Mathf.Clamp (qualityLevel, 0, 3);
			SetQualityLevel ();
		}

		if (Input.GetButtonDown ("DPad Down") || Input.GetKeyDown (KeyCode.T)) {
			qualityLevel--;
			qualityLevel = Mathf.Clamp (qualityLevel, 0, 3);
			SetQualityLevel ();
		}
	}

	void SetQualityLevel() {
		indicatorText.text = "Q: " + qualityLevel.ToString ();
		QualitySettings.SetQualityLevel (qualityLevel);
		/*
		switch (qualityLevel) {
		case 3:
			RenderSettings.fog = true;
			//text = "MFUltraHigh";
			GraphicsDetailsUtl.SetShaderQuality (GraphicsDetailsUtl.Quality.VeryHigh);
			break;
		case 2:
			RenderSettings.fog = true;
			//text = "MFHigh";
			GraphicsDetailsUtl.SetShaderQuality (GraphicsDetailsUtl.Quality.High);
			break;
		case 1:
			RenderSettings.fog = false;
			//text = "MFMedium";
			GraphicsDetailsUtl.SetShaderQuality (GraphicsDetailsUtl.Quality.Medium);
			break;
		case 0:
			RenderSettings.fog = false;
			//text = "MFLow";
			GraphicsDetailsUtl.SetShaderQuality (GraphicsDetailsUtl.Quality.Low);
			break;
		default:
			RenderSettings.fog = false;
			//text = "MFLow";
			GraphicsDetailsUtl.SetShaderQuality (GraphicsDetailsUtl.Quality.Low);
			break;
		}
	}*/
	}
}
