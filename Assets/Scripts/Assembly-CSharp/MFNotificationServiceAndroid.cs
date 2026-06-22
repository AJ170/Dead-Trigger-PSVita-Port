using System;
using System.Collections.Generic;
using LitJson;
using UnityEngine;

internal class MFNotificationServiceAndroid : MFNotificationService
{
	private MFPushNotificationService m_PushNotificationsService;

	private AndroidJavaClass m_Plugin;

	protected override void NotifyInternal(int id, MFNotification notification, DateTime when, TimeSpan period)
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			return;
		}
		string text = JsonMapper.ToJson(notification);
		m_Plugin.CallStatic("notify", id, (long)(when.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds, (long)period.Milliseconds, text);
	}

	protected override void CancelNotificationInternal(int id)
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			return;
		}
		m_Plugin.CallStatic("cancelNotification", id);
	}

	protected override void CancelAllInternal()
	{
		//m_Plugin.CallStatic("cancelAllNotifications");
	}

	protected override List<MFNotification> ReceivedNotificationsInternal()
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			return new List<MFNotification>();
		}
		AndroidJNI.AttachCurrentThread();
		List<MFNotification> list = new List<MFNotification>();
		//AndroidJavaObject[] array = m_Plugin.CallStatic<AndroidJavaObject[]>("receivedNotifications", new object[0]);
		/*for (int i = 0; i < array.Length; i++)
		{
			using (AndroidJavaObject androidJavaObject = array[i])
			{
				string json = androidJavaObject.Call<string>("toString", new object[0]);
				try
				{
					JsonData jsonData = JsonMapper.ToObject(json);
					MFNotification.ExtendedStyle extendedStyle = null;
					if (jsonData.HasValue("Style"))
					{
						JsonData jsonData2 = jsonData["Style"];
						if (jsonData2 != null)
						{
							if (jsonData2.HasValue("BigTitle"))
							{
								extendedStyle = JsonMapper.ToObject<MFNotification.BigTextStyle>(jsonData2.ToJson());
							}
							else if (jsonData2.HasValue("InboxTitle"))
							{
								extendedStyle = JsonMapper.ToObject<MFNotification.InboxStyle>(jsonData2.ToJson());
							}
						}
					}
					MFNotification mFNotification = JsonMapper.ToObject<MFNotification>(json);
					if (extendedStyle != null)
					{
						mFNotification.Style = extendedStyle;
					}
					list.Add(mFNotification);
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}
			}
		}*/
		return list;
	}

	protected override void ClearReceivedNotificationsInternal()
	{
		//m_Plugin.CallStatic("clearReceivedNotifications");
	}

	protected override void RegisterPushNotificationsInternal()
	{
		if (m_PushNotificationsService != null)
		{
			m_PushNotificationsService.Register();
		}
	}

	protected override void UnregisterPushNotificationsInternal()
	{
		if (m_PushNotificationsService != null)
		{
			m_PushNotificationsService.Unregister();
		}
	}

	private void Awake()
	{
		//m_Plugin = new AndroidJavaClass("com.madfingergames.android.notifications.UnityPlugin");
	}
}
