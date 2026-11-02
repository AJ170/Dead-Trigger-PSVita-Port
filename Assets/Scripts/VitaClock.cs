using System;
#if UNITY_PSP2 && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

// DateTime.Now returns 1/1/1601 on the Vita, so read the system clock through
// the VitaClock native plugin instead. Source is in NativeSource/VitaClock.
public static class VitaClock
{
#if UNITY_PSP2 && !UNITY_EDITOR
	[DllImport("VitaClock")]
	private static extern int VitaClock_GetDate();

	[DllImport("VitaClock")]
	private static extern int VitaClock_GetTime();
#endif

	public static DateTime Now
	{
		get
		{
#if UNITY_PSP2 && !UNITY_EDITOR
			try
			{
				int date = VitaClock_GetDate();
				int time = VitaClock_GetTime();
				if (date > 0 && time >= 0)
				{
					return new DateTime(date / 10000, date / 100 % 100, date % 100, time / 100, time % 100, 0);
				}
			}
			catch (Exception e)
			{
				UnityEngine.Debug.LogError("VitaClock failed: " + e.Message);
			}
#endif
			return DateTime.Now;
		}
	}
}
