using UnityEngine;

public class MFNativeUtils
{
	private static AndroidJavaClass ms_AndroidMFNativeUtils;

	public static int AppIconBadgeNumber { get; set; }

	static MFNativeUtils()
	{
		//ms_AndroidMFNativeUtils = new AndroidJavaClass("com.madfingergames.android.utils.MFNativeUtils");
	}

	public static void OpenURLExternal(string url)
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			Application.OpenURL(url);
			return;
		}
		ms_AndroidMFNativeUtils.CallStatic("openURLExternal", url);
	}

	public static string GetPhoneNumber()
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			return string.Empty;
		}
		return ms_AndroidMFNativeUtils.CallStatic<string>("getPhoneNumber", new object[0]);
	}

	public static string GetDeviceId()
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			return SystemInfo.deviceUniqueIdentifier;
		}
		return ms_AndroidMFNativeUtils.CallStatic<string>("getDeviceId", new object[0]);
	}
}
