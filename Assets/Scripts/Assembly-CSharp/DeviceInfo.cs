using UnityEngine;

public static class DeviceInfo
{
	public enum Performance
	{
		Low = 0,
		Medium = 1,
		High = 2,
		UltraHigh = 3,
		Auto = 4
	}

	private static bool? m_IsTVDeviceWithAndroidOS;

	private static Performance m_Performance = Performance.Auto;

	private static bool m_Initialized = true;

	public static Performance PerformanceGrade
	{
		get
		{
			return m_Performance;
		}
	}

	public static void Initialize(Performance suggestion)
	{
		if (suggestion != Performance.Auto)
		{
			m_Performance = suggestion;
			SetPerformanceLevel(m_Performance);
		}
		else
		{
			Check();
		}
	}

	private static void Check()
	{
		if (m_Initialized)
		{
			m_Initialized = false;
			m_Performance = GetDetectedPerformanceLevel();
			UpdatePerformanceSettings();
		}
	}

	public static Performance GetDetectedPerformanceLevel()
	{
		return Performance.High;
	}

	private static void SetPerformanceLevel(Performance perfLevel)
	{
		Application.targetFrameRate = 10000;
		Screen.sleepTimeout = -1;
		string text;
		switch (perfLevel)
		{
		case Performance.UltraHigh:
			RenderSettings.fog = true;
			text = "MFUltraHigh";
			GraphicsDetailsUtl.SetShaderQuality(GraphicsDetailsUtl.Quality.VeryHigh);
			break;
		case Performance.High:
			RenderSettings.fog = true;
			text = "MFHigh";
			GraphicsDetailsUtl.SetShaderQuality(GraphicsDetailsUtl.Quality.High);
			break;
		case Performance.Medium:
			RenderSettings.fog = false;
			text = "MFMedium";
			GraphicsDetailsUtl.SetShaderQuality(GraphicsDetailsUtl.Quality.Medium);
			break;
		default:
			RenderSettings.fog = false;
			text = "MFLow";
			GraphicsDetailsUtl.SetShaderQuality(GraphicsDetailsUtl.Quality.Low);
			break;
		}
		string[] names = QualitySettings.names;
		for (int i = 0; i < names.Length; i++)
		{
			if (names[i] == text)
			{
				QualitySettings.SetQualityLevel(i, true);
				break;
			}
		}
		// SetQualityLevel loads the level's own vSyncCount, so the player's choice
		// has to be re-applied here or it is lost on every mission load.
		GuiOptions.ApplyVSync();
	}

	public static void UpdatePerformanceSettings()
	{
		SetPerformanceLevel(PerformanceGrade);
	}

	public static bool IsTVDeviceWithAndroidOS()
	{
		if (!m_IsTVDeviceWithAndroidOS.HasValue)
		{
			m_IsTVDeviceWithAndroidOS = false;
			AndroidJavaObject androidJavaObject = null;
			using (AndroidJavaClass androidJavaClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
			{
				androidJavaObject = androidJavaClass.GetStatic<AndroidJavaObject>("currentActivity");
			}
			AndroidJavaClass androidJavaClass2 = new AndroidJavaClass("android.content.Context");
			string @static = androidJavaClass2.GetStatic<string>("UI_MODE_SERVICE");
			AndroidJavaObject androidJavaObject2 = androidJavaObject.Call<AndroidJavaObject>("getSystemService", new object[1] { @static });
			int num = androidJavaObject2.Call<int>("getCurrentModeType", new object[0]);
			AndroidJavaClass androidJavaClass3 = new AndroidJavaClass("android.content.res.Configuration");
			int static2 = androidJavaClass3.GetStatic<int>("UI_MODE_TYPE_TELEVISION");
			m_IsTVDeviceWithAndroidOS = num == static2;
		}
		return m_IsTVDeviceWithAndroidOS.Value;
	}
}
