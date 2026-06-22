using System;
using System.Collections.Generic;
using UnityEngine;

namespace OnePF
{
	public class OpenIAB_Android : IOpenIAB
	{
		public static readonly string STORE_GOOGLE;

		public static readonly string STORE_AMAZON;

		public static readonly string STORE_SAMSUNG;

		public static readonly string STORE_NOKIA;

		public static readonly string STORE_SKUBIT;

		public static readonly string STORE_SKUBIT_TEST;

		public static readonly string STORE_YANDEX;

		public static readonly string STORE_APPLAND;

		public static readonly string STORE_SLIDEME;

		public static readonly string STORE_APTOIDE;

		private static AndroidJavaObject _plugin;

		static OpenIAB_Android()
		{
			if (Application.platform != RuntimePlatform.Android)
			{
				STORE_GOOGLE = "STORE_GOOGLE";
				STORE_AMAZON = "STORE_AMAZON";
				STORE_SAMSUNG = "STORE_SAMSUNG";
				STORE_NOKIA = "STORE_NOKIA";
				STORE_SKUBIT = "STORE_SKUBIT";
				STORE_SKUBIT_TEST = "STORE_SKUBIT_TEST";
				STORE_YANDEX = "STORE_YANDEX";
				STORE_APPLAND = "STORE_APPLAND";
				STORE_SLIDEME = "STORE_SLIDEME";
				STORE_APTOIDE = "STORE_APTOIDE";
				return;
			}
			AndroidJNI.AttachCurrentThread();
			using (AndroidJavaClass androidJavaClass = new AndroidJavaClass("org.onepf.openiab.UnityPlugin"))
			{
				_plugin = androidJavaClass.CallStatic<AndroidJavaObject>("instance", new object[0]);
			}
			using (AndroidJavaClass androidJavaClass2 = new AndroidJavaClass("org.onepf.oms.OpenIabHelper"))
			{
				STORE_GOOGLE = androidJavaClass2.GetStatic<string>("NAME_GOOGLE");
				STORE_AMAZON = androidJavaClass2.GetStatic<string>("NAME_AMAZON");
				STORE_SAMSUNG = androidJavaClass2.GetStatic<string>("NAME_SAMSUNG");
				STORE_NOKIA = androidJavaClass2.GetStatic<string>("NAME_NOKIA");
				STORE_SKUBIT = androidJavaClass2.GetStatic<string>("NAME_SKUBIT");
				STORE_SKUBIT_TEST = androidJavaClass2.GetStatic<string>("NAME_SKUBIT_TEST");
				STORE_YANDEX = androidJavaClass2.GetStatic<string>("NAME_YANDEX");
				STORE_APPLAND = androidJavaClass2.GetStatic<string>("NAME_APPLAND");
				STORE_SLIDEME = androidJavaClass2.GetStatic<string>("NAME_SLIDEME");
				STORE_APTOIDE = androidJavaClass2.GetStatic<string>("NAME_APTOIDE");
			}
		}

		private IntPtr ConvertToStringJNIArray(string[] array)
		{
			IntPtr clazz = AndroidJNI.FindClass("java/lang/String");
			IntPtr obj = AndroidJNI.NewStringUTF(string.Empty);
			IntPtr intPtr = AndroidJNI.NewObjectArray(array.Length, clazz, obj);
			for (int i = 0; i < array.Length; i++)
			{
				AndroidJNI.SetObjectArrayElement(intPtr, i, AndroidJNI.NewStringUTF(array[i]));
			}
			return intPtr;
		}

		private bool IsDevice()
		{
			if (Application.platform != RuntimePlatform.Android)
			{
				return false;
			}
			return true;
		}

		private AndroidJavaObject CreateJavaHashMap(Dictionary<string, string> storeKeys)
		{
			AndroidJavaObject androidJavaObject = new AndroidJavaObject("java.util.HashMap");
			IntPtr methodID = AndroidJNIHelper.GetMethodID(androidJavaObject.GetRawClass(), "put", "(Ljava/lang/Object;Ljava/lang/Object;)Ljava/lang/Object;");
			if (storeKeys != null)
			{
				object[] array = new object[2];
				foreach (KeyValuePair<string, string> storeKey in storeKeys)
				{
					using (AndroidJavaObject androidJavaObject2 = new AndroidJavaObject("java.lang.String", storeKey.Key))
					{
						using (AndroidJavaObject androidJavaObject3 = new AndroidJavaObject("java.lang.String", storeKey.Value))
						{
							array[0] = androidJavaObject2;
							array[1] = androidJavaObject3;
							AndroidJNI.CallObjectMethod(androidJavaObject.GetRawObject(), methodID, AndroidJNIHelper.CreateJNIArgArray(array));
						}
					}
				}
			}
			return androidJavaObject;
		}

		public void init(Options options)
		{
			if (!IsDevice())
			{
				OpenIAB.EventManager.SendMessage("OnBillingSupported", string.Empty);
				return;
			}
			using (AndroidJavaObject androidJavaObject = new AndroidJavaObject("org.onepf.oms.OpenIabHelper$Options$Builder"))
			{
				IntPtr rawClass = androidJavaObject.GetRawClass();
				IntPtr rawObject = androidJavaObject.GetRawObject();
				androidJavaObject.Call<AndroidJavaObject>("setDiscoveryTimeout", new object[1] { options.discoveryTimeoutMs }).Call<AndroidJavaObject>("setCheckInventory", new object[1] { options.checkInventory }).Call<AndroidJavaObject>("setCheckInventoryTimeout", new object[1] { options.checkInventoryTimeoutMs })
					.Call<AndroidJavaObject>("setVerifyMode", new object[1] { (int)options.verifyMode })
					.Call<AndroidJavaObject>("setStoreSearchStrategy", new object[1] { (int)options.storeSearchStrategy });
				if (options.samsungCertificationRequestCode > 0)
				{
					androidJavaObject.Call<AndroidJavaObject>("setSamsungCertificationRequestCode", new object[1] { options.samsungCertificationRequestCode });
				}
				foreach (KeyValuePair<string, string> storeKey in options.storeKeys)
				{
					androidJavaObject.Call<AndroidJavaObject>("addStoreKey", new object[2] { storeKey.Key, storeKey.Value });
				}
				IntPtr methodID = AndroidJNI.GetMethodID(rawClass, "addPreferredStoreName", "([Ljava/lang/String;)Lorg/onepf/oms/OpenIabHelper$Options$Builder;");
				jvalue[] array = new jvalue[1];
				array[0].l = ConvertToStringJNIArray(options.prefferedStoreNames);
				AndroidJNI.CallObjectMethod(rawObject, methodID, array);
				IntPtr methodID2 = AndroidJNI.GetMethodID(rawClass, "addAvailableStoreNames", "([Ljava/lang/String;)Lorg/onepf/oms/OpenIabHelper$Options$Builder;");
				array = new jvalue[1];
				array[0].l = ConvertToStringJNIArray(options.availableStoreNames);
				AndroidJNI.CallObjectMethod(rawObject, methodID2, array);
				IntPtr methodID3 = AndroidJNI.GetMethodID(rawClass, "build", "()Lorg/onepf/oms/OpenIabHelper$Options;");
				IntPtr l = AndroidJNI.CallObjectMethod(rawObject, methodID3, new jvalue[0]);
				IntPtr methodID4 = AndroidJNI.GetMethodID(_plugin.GetRawClass(), "initWithOptions", "(Lorg/onepf/oms/OpenIabHelper$Options;)V");
				array = new jvalue[1];
				array[0].l = l;
				AndroidJNI.CallVoidMethod(_plugin.GetRawObject(), methodID4, array);
			}
		}

		public void init(Dictionary<string, string> storeKeys = null)
		{
			if (IsDevice() && storeKeys != null)
			{
				AndroidJavaObject androidJavaObject = CreateJavaHashMap(storeKeys);
				_plugin.Call("init", androidJavaObject);
				androidJavaObject.Dispose();
			}
		}

		public void mapSku(string sku, string storeName, string storeSku)
		{
			if (IsDevice())
			{
				_plugin.Call("mapSku", sku, storeName, storeSku);
			}
		}

		public void unbindService()
		{
			if (IsDevice())
			{
				_plugin.Call("unbindService");
			}
		}

		public bool areSubscriptionsSupported()
		{
			if (!IsDevice())
			{
				return true;
			}
			return _plugin.Call<bool>("areSubscriptionsSupported", new object[0]);
		}

		public void queryInventory()
		{
			if (IsDevice())
			{
				IntPtr methodID = AndroidJNI.GetMethodID(_plugin.GetRawClass(), "queryInventory", "()V");
				AndroidJNI.CallVoidMethod(_plugin.GetRawObject(), methodID, new jvalue[0]);
			}
		}

		public void queryInventory(string[] skus)
		{
			queryInventory(skus, skus);
		}

		private void queryInventory(string[] inAppSkus, string[] subsSkus)
		{
			if (IsDevice())
			{
				jvalue[] array = new jvalue[2];
				array[0].l = ConvertToStringJNIArray(inAppSkus);
				array[1].l = ConvertToStringJNIArray(subsSkus);
				IntPtr methodID = AndroidJNI.GetMethodID(_plugin.GetRawClass(), "queryInventory", "([Ljava/lang/String;[Ljava/lang/String;)V");
				AndroidJNI.CallVoidMethod(_plugin.GetRawObject(), methodID, array);
			}
		}

		public void purchaseProduct(string sku, string developerPayload = "")
		{
			if (!IsDevice())
			{
				OpenIAB.EventManager.SendMessage("OnPurchaseSucceeded", Purchase.CreateFromSku(sku, developerPayload).Serialize());
				return;
			}
			_plugin.Call("purchaseProduct", sku, developerPayload);
		}

		public void purchaseSubscription(string sku, string developerPayload = "")
		{
			if (!IsDevice())
			{
				OpenIAB.EventManager.SendMessage("OnPurchaseSucceeded", Purchase.CreateFromSku(sku, developerPayload).Serialize());
				return;
			}
			_plugin.Call("purchaseSubscription", sku, developerPayload);
		}

		public void consumeProduct(Purchase purchase)
		{
			if (!IsDevice())
			{
				OpenIAB.EventManager.SendMessage("OnConsumePurchaseSucceeded", purchase.Serialize());
				return;
			}
			_plugin.Call("consumeProduct", purchase.Serialize());
		}

		public void restoreTransactions()
		{
		}

		public bool isDebugLog()
		{
			if (!IsDevice())
			{
				return false;
			}
			return _plugin.Call<bool>("isDebugLog", new object[0]);
		}

		public void enableDebugLogging(bool enabled)
		{
			if (!IsDevice())
			{
				return;
			}
			_plugin.Call("enableDebugLogging", enabled);
		}

		public void enableDebugLogging(bool enabled, string tag)
		{
			if (!IsDevice())
			{
				return;
			}
			_plugin.Call("enableDebugLogging", enabled, tag);
		}
	}
}
