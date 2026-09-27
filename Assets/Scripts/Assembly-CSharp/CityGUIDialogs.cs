using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CityGUIDialogs
{
	public class SpecialRewardInfo
	{
		public string Amount;

		public string Name;

		public GUIBase_Widget Picture;
	}

	private class StoryChapterDlg : CityBaseScreen
	{
		private StoryFlowData.Story m_Story;

		private int m_ActualPage;

		private bool m_Debriefing;

		private GUIBase_Button m_PrevButton;

		private GUIBase_Button m_NextButton;

		private GUIBase_Layout m_Dialog;

		private GUIBase_Label m_Caption;

		private GUIBase_Label m_Page;

		private GUIBase_TextArea m_Description;

		private Vector3 m_OrigPos;

		public StoryChapterDlg()
			: base(CityScreen.StoryChapter)
		{
		}

		public void Init(GUIBase_Pivot pivot)
		{
			m_Dialog = pivot.GetLayout("StoryChapter");
			m_Caption = m_Dialog.GetWidget("Caption").GetComponent<GUIBase_Label>();
			m_Description = m_Dialog.GetWidget("Description").GetComponent<GUIBase_TextArea>();
			m_Page = m_Dialog.GetWidget("PageText").GetComponent<GUIBase_Label>();
			m_PrevButton = m_Dialog.GetWidget("ButtonPrev").GetComponent<GUIBase_Button>();
			m_NextButton = m_Dialog.GetWidget("ButtonNext").GetComponent<GUIBase_Button>();
			m_OrigPos = m_Dialog.transform.localPosition;
		}

		public void Show(GUIBase_Button.TouchDelegate close, StoryFlowData.Story story, bool isDebriefing)
		{
			base.Show(close);
			GUIBase_Sprite component = m_Dialog.GetWidget("Portrait_Sprite").GetComponent<GUIBase_Sprite>();
			if ((bool)component)
			{
				string text = ((!isDebriefing) ? story.storyPicture : story.debriefPicture);
				if (text != string.Empty)
				{
					string text2 = "BriefingPortraits/" + text;
					GameObject gameObject = Resources.Load(text2) as GameObject;
					if (gameObject == null)
					{
						Debug.LogWarning("Cant find prefab: " + text2);
					}
					else
					{
						GUIBase_Widget component2 = gameObject.GetComponent<GUIBase_Widget>();
						if ((bool)component2)
						{
							component.Widget.CopyMaterialSettings(component2);
						}
					}
				}
			}
			m_Story = story;
			m_ActualPage = 1;
			m_Debriefing = isDebriefing;
			GuiBaseUtils.RegisterButtonDelegate(m_Dialog, "ButtonPrev", Prev, null);
			GuiBaseUtils.RegisterButtonDelegate(m_Dialog, "ButtonNext", Next, null);
			GuiBaseUtils.RegisterButtonDelegate(m_Dialog, "ButtonClose", close, null);
			m_Caption.SetNewText(story.storyCaption);
			UpdateText();
			m_Dialog.StopAllCoroutines();
			m_Dialog.StartCoroutine(CityGUIResources.Reset(m_Dialog, m_OrigPos));
			m_Dialog.StartCoroutine(CityGUIResources.ShowDialog(m_Dialog, m_OrigPos));
		}

		public override void Hide()
		{
			base.Hide();
			m_Dialog.StopAllCoroutines();
			m_Dialog.StartCoroutine(CityGUIResources.Reset(m_Dialog, m_OrigPos));
			m_Dialog.StartCoroutine(CityGUIResources.HideDialog(m_Dialog, m_OrigPos));
		}

		private void UpdateText()
		{
			List<int> list = ((!m_Debriefing) ? m_Story.storyPages : m_Story.debriefPages);
			if (m_ActualPage > list.Count)
			{
				Debug.LogWarning("Invalid text count, requested: " + m_ActualPage + ", available: " + list.Count);
				return;
			}
			m_Description.SetNewText(list[m_ActualPage - 1]);
			if (m_ActualPage == list.Count)
			{
				m_NextButton.SetDisabled(true);
			}
			else if (list.Count > 1)
			{
				m_NextButton.SetDisabled(false);
			}
			m_Page.Widget.Show(true, true);
			if (m_ActualPage == 1)
			{
				m_PrevButton.SetDisabled(true);
			}
			else if (m_ActualPage > 1)
			{
				m_PrevButton.SetDisabled(false);
			}
			string text = TextDatabase.instance[1010270];
			text = text.Replace("%d1", m_ActualPage.ToString());
			text = text.Replace("%d2", list.Count.ToString());
			m_Page.SetNewText(text);
		}

		private void Prev()
		{
			m_ActualPage--;
			UpdateText();
		}

		private void Next()
		{
			m_ActualPage++;
			UpdateText();
		}
	}

	private class StoryEventDlg : CityBaseScreen
	{
		private GUIBase_Layout m_Dialog;

		private GUIBase_TextArea m_Description;

		private Vector3 m_OrigPos;

		public StoryEventDlg()
			: base(CityScreen.StoryEvent)
		{
		}

		public void Init(GUIBase_Pivot pivot)
		{
			m_Dialog = pivot.GetLayout("StoryEvent");
			m_Description = m_Dialog.GetWidget("Description").GetComponent<GUIBase_TextArea>();
			m_OrigPos = m_Dialog.transform.localPosition;
		}

		public void Show(GUIBase_Button.TouchDelegate close, int text)
		{
			base.Show();
			GuiBaseUtils.RegisterButtonDelegate(m_Dialog, "ButtonClose", close, null);
			m_Description.SetNewText(text);
			m_Dialog.StopAllCoroutines();
			m_Dialog.StartCoroutine(CityGUIResources.Reset(m_Dialog, m_OrigPos));
			m_Dialog.StartCoroutine(CityGUIResources.ShowDialog(m_Dialog, m_OrigPos));
		}

		public override void Hide()
		{
			base.Hide();
			GuiBaseUtils.RegisterButtonDelegate(m_Dialog, "ButtonClose", null, null);
			m_Dialog.StopAllCoroutines();
			m_Dialog.StartCoroutine(CityGUIResources.Reset(m_Dialog, m_OrigPos));
			m_Dialog.StartCoroutine(CityGUIResources.HideDialog(m_Dialog, m_OrigPos));
		}
	}

	private class BuyerRewardDlg : CityBaseScreen
	{
		private GUIBase_Layout m_Dialog;

		private Vector3 m_OrigPos;

		private float[,] m_RewardXPos = new float[3, 3]
		{
			{ 885f, 0f, 0f },
			{ 515f, 1265f, 0f },
			{ 330f, 890f, 1440f }
		};

		public BuyerRewardDlg()
			: base(CityScreen.BuyerReward)
		{
		}

		public void Init(GUIBase_Pivot pivot)
		{
			m_Dialog = pivot.GetLayout("BuyerReward");
			m_OrigPos = m_Dialog.transform.localPosition;
		}

		public void Show(GUIBase_Button.TouchDelegate close, int numGold, bool alienGun, int numCasinoChips, bool reward180)
		{
			int num = 0;
			if (numGold > 0)
			{
				num++;
			}
			if (alienGun)
			{
				num++;
			}
			if (numCasinoChips > 0)
			{
				num++;
			}
			if (--num >= 0)
			{
				base.Show();
				GuiBaseUtils.RegisterButtonDelegate(m_Dialog, "ButtonClose", close, null);
				GUIBase_Widget component = m_Dialog.GetWidget("Reward1").GetComponent<GUIBase_Widget>();
				GUIBase_Label component2 = m_Dialog.GetWidget("Reward1_Count").GetComponent<GUIBase_Label>();
				GUIBase_Widget component3 = m_Dialog.GetWidget("Reward2").GetComponent<GUIBase_Widget>();
				GUIBase_Widget component4 = m_Dialog.GetWidget("Reward3").GetComponent<GUIBase_Widget>();
				GUIBase_Label component5 = m_Dialog.GetWidget("Reward3_Count").GetComponent<GUIBase_Label>();
				if (reward180)
				{
					GUIBase_Label component6 = m_Dialog.GetWidget("Caption").GetComponent<GUIBase_Label>();
					GUIBase_Label component7 = m_Dialog.GetWidget("Text").GetComponent<GUIBase_Label>();
					component6.transform.localScale = Vector3.Scale(component6.transform.localScale, new Vector3(0.95f, 0.95f, 0.95f));
					component6.SetNewText(1012061);
					component7.SetNewText(1012062);
				}
				int num2 = 0;
				if (numGold > 0)
				{
					component.Show(true, true);
					component.transform.localPosition = new Vector3(m_RewardXPos[num, num2++], component.transform.localPosition.y, 0f);
					component2.SetNewText("+" + numGold);
				}
				else
				{
					component.Show(false, true);
				}
				if (alienGun)
				{
					component3.Show(true, true);
					component3.transform.localPosition = new Vector3(m_RewardXPos[num, num2++], component3.transform.localPosition.y, 0f);
				}
				else
				{
					component3.Show(false, true);
				}
				if (numCasinoChips > 0)
				{
					component4.Show(true, true);
					component4.transform.localPosition = new Vector3(m_RewardXPos[num, num2++], component4.transform.localPosition.y, 0f);
					component5.SetNewText("+" + numCasinoChips);
				}
				else
				{
					component4.Show(false, true);
				}
				if ((bool)CityManager.Instance)
				{
					CityManager.Instance.PlaySound(CityManager.Sounds.GUI_Reward, false);
				}
				m_Dialog.StopAllCoroutines();
				m_Dialog.StartCoroutine(CityGUIResources.Reset(m_Dialog, m_OrigPos));
				m_Dialog.StartCoroutine(CityGUIResources.ShowDialog(m_Dialog, m_OrigPos));
			}
		}

		public override void Hide()
		{
			base.Hide();
			GuiBaseUtils.RegisterButtonDelegate(m_Dialog, "ButtonClose", null, null);
			m_Dialog.StopAllCoroutines();
			m_Dialog.StartCoroutine(CityGUIResources.Reset(m_Dialog, m_OrigPos));
			m_Dialog.StartCoroutine(CityGUIResources.HideDialog(m_Dialog, m_OrigPos));
		}
	}

	private class OneTimeSaleOfferDlg : CityBaseScreen
	{
		private GUIBase_Layout m_Dialog;

		private Vector3 m_OrigPos;

		private GUIBase_Label m_ObjectName;

		private GUIBase_Widget m_ObjectPicture;

		private GUIBase_Label m_SaleValue;

		private GUIBase_Label m_Cost_Label;

		private GUIBase_Label m_Cost_Label2;

		private GUIBase_Widget m_CostSpriteGold;

		private GUIBase_Widget m_CostSpriteMoney;

		private OneTimeSaleOfferManager m_OneTimeSaleOfferManager;

		public OneTimeSaleOfferDlg()
			: base(CityScreen.OneTimeSaleOffer)
		{
		}

		public void Init(GUIBase_Pivot pivot)
		{
			m_Dialog = pivot.GetLayout("OneTimeSaleOffer");
			m_OrigPos = m_Dialog.transform.localPosition;
			m_ObjectName = m_Dialog.GetWidget("WeaponName").GetComponent<GUIBase_Label>();
			m_ObjectPicture = m_Dialog.GetWidget("WeaponPicture").GetComponent<GUIBase_Widget>();
			m_SaleValue = m_Dialog.GetWidget("SaleOffLabel").GetComponent<GUIBase_Label>();
			m_Cost_Label = m_Dialog.GetWidget("Cost_Label").GetComponent<GUIBase_Label>();
			m_Cost_Label2 = m_Dialog.GetWidget("Cost_Label2").GetComponent<GUIBase_Label>();
			m_CostSpriteGold = m_Dialog.GetWidget("Gold_Sprite").GetComponent<GUIBase_Widget>();
			m_CostSpriteMoney = m_Dialog.GetWidget("Money_Sprite").GetComponent<GUIBase_Widget>();
		}

		public void Show(GUIBase_Button.TouchDelegate close, OneTimeSaleOfferManager manager)
		{
			base.Show(close);
			m_OneTimeSaleOfferManager = manager;
			GuiBaseUtils.RegisterButtonDelegate(m_Dialog, "ButtonClose", close, null);
			ShopItemInfo itemInfo = ShopDataBridge.Instance.GetItemInfo(manager.ActualOneTimeSaleOffer());
			m_ObjectName.SetNewText(itemInfo.NameTextId);
			m_ObjectPicture.CopyMaterialSettings(itemInfo.SpriteWidget);
			string text = TextDatabase.instance[1013010];
			bool goldCurrency = itemInfo.GoldCurrency;
			float num = manager.ActualOfferDiscount();
			int num2 = manager.ActualOfferOrigPrice();
			text = text.Replace("%d", Mathf.RoundToInt(100f * num).ToString());
			m_SaleValue.SetNewText(text);
			CheckMoney();
			m_Cost_Label.SetNewText(Mathf.RoundToInt((float)num2 - (float)num2 * num).ToString());
			m_Cost_Label2.SetNewText(num2.ToString());
			m_CostSpriteGold.Show(goldCurrency, false);
			m_CostSpriteMoney.Show(!goldCurrency, false);
			m_Dialog.StopAllCoroutines();
			m_Dialog.StartCoroutine(CityGUIResources.Reset(m_Dialog, m_OrigPos));
			m_Dialog.StartCoroutine(CityGUIResources.ShowDialog(m_Dialog, m_OrigPos));
		}

		public override void Hide()
		{
			base.Hide();
			GuiBaseUtils.RegisterButtonDelegate(m_Dialog, "ButtonClose", null, null);
			m_OneTimeSaleOfferManager = null;
			m_Dialog.StopAllCoroutines();
			m_Dialog.StartCoroutine(CityGUIResources.Reset(m_Dialog, m_OrigPos));
			m_Dialog.StartCoroutine(CityGUIResources.HideDialog(m_Dialog, m_OrigPos));
		}

		internal void CheckMoney()
		{
			if (m_OneTimeSaleOfferManager != null)
			{
				if (ShopDataBridge.Instance.HaveEnoughMoney(m_OneTimeSaleOfferManager.ActualOneTimeSaleOffer(), m_OneTimeSaleOfferManager.ActualOfferDiscount()))
				{
					GuiBaseUtils.RegisterButtonDelegate(m_Dialog, "Buy_Button", BuyButton, null);
				}
				else
				{
					GuiBaseUtils.RegisterButtonDelegate(m_Dialog, "Buy_Button", MoreGoldButton, null);
				}
			}
		}

		internal void BuyButton()
		{
			if (m_OneTimeSaleOfferManager != null)
			{
				ShopDataBridge.Instance.SynchroniseBoughtItem(m_OneTimeSaleOfferManager.ActualOneTimeSaleOffer(), m_OneTimeSaleOfferManager.ActualOfferDiscount());
				GuiEquipMenu.Instance.TryEquipBoughtItem(m_OneTimeSaleOfferManager.ActualOneTimeSaleOffer());
				base.ProcessBackButton();
			}
		}

		internal void MoreGoldButton()
		{
			if (m_OneTimeSaleOfferManager == null)
			{
				return;
			}
			ShopItemInfo itemInfo = ShopDataBridge.Instance.GetItemInfo(m_OneTimeSaleOfferManager.ActualOneTimeSaleOffer());
			ShopItemId item = ShopDataBridge.Instance.FindFundsItem(itemInfo.Cost, itemInfo.GoldCurrency);
			CityManager.Instance.DisableNextSuspendAndResume();
			CityManager.Instance.DisableNextSuspendAndResume();
			if (ShopDataBridge.Instance.IAPServiceAvailable())
			{
				GuiBaseUtils.RegisterButtonDelegate(m_Dialog, "ButtonClose", null, null);
				GuiMainMenu.Instance.ShowPopup("ShopStatusIAP", TextDatabase.instance[2030044], TextDatabase.instance[2030045]);
				ShopDataBridge.Instance.IAPRequestPurchase(item, delegate(IAP.E_Buy result)
				{
					int i = 2030049;
					switch (result)
					{
					case IAP.E_Buy.Success:
						i = 2030048;
						break;
					case IAP.E_Buy.UserCancelled:
						i = 2030050;
						break;
					case IAP.E_Buy.Failure:
						i = 2030049;
						break;
					case IAP.E_Buy.Fatal:
						i = 2030049;
						break;
					}
					GuiMainMenu.Instance.Back();
					GuiMainMenu.Instance.ShowPopup("ShopMessageBox", TextDatabase.instance[2030046], TextDatabase.instance[i], InappDlgClosed);
				});
			}
			else
			{
				GuiBaseUtils.RegisterButtonDelegate(m_Dialog, "ButtonClose", null, null);
				GuiMainMenu.Instance.ShowPopup("ShopMessageBox", TextDatabase.instance[2030046], TextDatabase.instance[2030047], InappDlgClosed);
			}
		}

		internal void InappDlgClosed(BasePopupScreen inPopup, E_PopupResultCode inResult)
		{
			GuiBaseUtils.RegisterButtonDelegate(m_Dialog, "ButtonClose", GetCloseDelegate(), null);
			CheckMoney();
		}

		internal void FreeGoldButton()
		{
			List<ShopItemId> funds = ShopDataBridge.Instance.GetFunds();
			ShopItemId shopItemId = null;
			foreach (ShopItemId item in funds)
			{
				if (ShopDataBridge.Instance.IsFreeGold(item) && item.Id == 13)
				{
					shopItemId = item;
					break;
				}
			}
			switch ((E_FundID)shopItemId.Id)
			{
			case E_FundID.TapJoyWeb:
				Etcetera.ShowWeb("https://www.tapjoy.com/earn?eid=ab8c8296e98e6ef62a40221a7a17a6dd394f7d0dbdc5948f4828ae02c65220f03a1be4186eb3e2531a52c2a0c033b9c0&referral=madfinger_deadtrigger");
				break;
			case E_FundID.TapJoyInApp:
				//TapjoyPlugin.ShowOffers();
				break;
			}
			CheckMoney();
		}
	}

	private class TapJoyAdvertDlg : CityBaseScreen
	{
		private GUIBase_Layout m_Dialog;

		private Vector3 m_OrigPos;

		public TapJoyAdvertDlg()
			: base(CityScreen.HalloweenAdvert)
		{
		}

		public void Init(GUIBase_Pivot pivot)
		{
			m_Dialog = pivot.GetLayout("TapJoyAdvert");
			m_OrigPos = m_Dialog.transform.localPosition;
		}

		public override void Show(GUIBase_Button.TouchDelegate close)
		{
			base.Show(close);
			GuiBaseUtils.RegisterButtonDelegate(m_Dialog, "ButtonClose", close, null);
			m_Dialog.StopAllCoroutines();
			m_Dialog.StartCoroutine(CityGUIResources.Reset(m_Dialog, m_OrigPos));
			m_Dialog.StartCoroutine(CityGUIResources.ShowDialog(m_Dialog, m_OrigPos));
		}

		public override void Hide()
		{
			base.Hide();
			GuiBaseUtils.RegisterButtonDelegate(m_Dialog, "ButtonClose", null, null);
			m_Dialog.StopAllCoroutines();
			m_Dialog.StartCoroutine(CityGUIResources.Reset(m_Dialog, m_OrigPos));
			m_Dialog.StartCoroutine(CityGUIResources.HideDialog(m_Dialog, m_OrigPos));
		}
	}

	private class HalloweenAdvertDlg : CityBaseScreen
	{
		private GUIBase_Layout m_Dialog;

		private Vector3 m_OrigPos;

		public HalloweenAdvertDlg()
			: base(CityScreen.HalloweenAdvert)
		{
		}

		public void Init(GUIBase_Pivot pivot)
		{
			m_Dialog = pivot.GetLayout("HalloweenAdvert");
			m_OrigPos = m_Dialog.transform.localPosition;
		}

		public override void Show(GUIBase_Button.TouchDelegate close)
		{
			base.Show(close);
			GuiBaseUtils.RegisterButtonDelegate(m_Dialog, "ButtonClose", close, null);
			m_Dialog.StopAllCoroutines();
			m_Dialog.StartCoroutine(CityGUIResources.Reset(m_Dialog, m_OrigPos));
			m_Dialog.StartCoroutine(CityGUIResources.ShowDialog(m_Dialog, m_OrigPos));
		}

		public override void Hide()
		{
			base.Hide();
			GuiBaseUtils.RegisterButtonDelegate(m_Dialog, "ButtonClose", null, null);
			m_Dialog.StopAllCoroutines();
			m_Dialog.StartCoroutine(CityGUIResources.Reset(m_Dialog, m_OrigPos));
			m_Dialog.StartCoroutine(CityGUIResources.HideDialog(m_Dialog, m_OrigPos));
		}
	}

	private class ChristmasAdvertDlg : CityBaseScreen
	{
		private GUIBase_Layout m_Dialog;

		private Vector3 m_OrigPos;

		public ChristmasAdvertDlg()
			: base(CityScreen.ChristmasAdvert)
		{
		}

		public void Init(GUIBase_Pivot pivot)
		{
			m_Dialog = pivot.GetLayout("ChristmasAdvert");
			m_OrigPos = m_Dialog.transform.localPosition;
		}

		public override void Show(GUIBase_Button.TouchDelegate close)
		{
			base.Show(close);
			GuiBaseUtils.RegisterButtonDelegate(m_Dialog, "ButtonClose", close, null);
			m_Dialog.StopAllCoroutines();
			m_Dialog.StartCoroutine(CityGUIResources.Reset(m_Dialog, m_OrigPos));
			m_Dialog.StartCoroutine(CityGUIResources.ShowDialog(m_Dialog, m_OrigPos));
		}

		public override void Hide()
		{
			base.Hide();
			GuiBaseUtils.RegisterButtonDelegate(m_Dialog, "ButtonClose", null, null);
			m_Dialog.StopAllCoroutines();
			m_Dialog.StartCoroutine(CityGUIResources.Reset(m_Dialog, m_OrigPos));
			m_Dialog.StartCoroutine(CityGUIResources.HideDialog(m_Dialog, m_OrigPos));
		}
	}

	private class DailyRewardDlg : CityBaseScreen
	{
		private GUIBase_Layout m_Dialog;

		private Vector3 m_OrigPos;

		public DailyRewardDlg()
			: base(CityScreen.DailyReward)
		{
		}

		public void Init(GUIBase_Pivot pivot)
		{
			m_Dialog = pivot.GetLayout("DailyReward");
			m_OrigPos = m_Dialog.transform.localPosition;
		}

		public void Show(GUIBase_Button.TouchDelegate close, bool arenaUnlocked)
		{
			base.Show();
			GuiBaseUtils.RegisterButtonDelegate(m_Dialog, "ButtonClose", close, null);
			GUIBase_Widget component = m_Dialog.GetWidget("Reward1").GetComponent<GUIBase_Widget>();
			GUIBase_Widget component2 = m_Dialog.GetWidget("Reward3").GetComponent<GUIBase_Widget>();
			component.Show(true, true);
			component2.Show(true, true);
			GUIBase_Label component3 = m_Dialog.GetWidget("Reward1_Count").GetComponent<GUIBase_Label>();
			component3.SetNewText("+" + Game.Instance.PlayerPersistentInfo.numGoldForGoldMission);
			GUIBase_Label component4 = m_Dialog.GetWidget("Reward3_Count").GetComponent<GUIBase_Label>();
			component4.SetNewText("+" + PlayerPersistantInfo.TicketsPerDailyReward);
			if ((bool)CityManager.Instance)
			{
				CityManager.Instance.PlaySound(CityManager.Sounds.GUI_Reward, false);
			}
			m_Dialog.StopAllCoroutines();
			m_Dialog.StartCoroutine(CityGUIResources.Reset(m_Dialog, m_OrigPos));
			m_Dialog.StartCoroutine(CityGUIResources.ShowDialog(m_Dialog, m_OrigPos));
		}

		public override void Hide()
		{
			base.Hide();
			GuiBaseUtils.RegisterButtonDelegate(m_Dialog, "ButtonClose", null, null);
			m_Dialog.StopAllCoroutines();
			m_Dialog.StartCoroutine(CityGUIResources.Reset(m_Dialog, m_OrigPos));
			m_Dialog.StartCoroutine(CityGUIResources.HideDialog(m_Dialog, m_OrigPos));
		}
	}

	private class SiteInfoDlg : CityBaseScreen
	{
		private GUIBase_Layout m_Dialog;

		private GUIBase_TextArea m_Description;

		private Vector3 m_OrigPos;

		public SiteInfoDlg()
			: base(CityScreen.SiteInfo)
		{
		}

		public void Init(GUIBase_Pivot pivot)
		{
			m_Dialog = pivot.GetLayout("SiteInfo");
			m_Description = m_Dialog.GetWidget("Description").GetComponent<GUIBase_TextArea>();
			m_OrigPos = m_Dialog.transform.localPosition;
		}

		public void Show(GUIBase_Button.TouchDelegate close, int text)
		{
			base.Show(close);
			GuiBaseUtils.RegisterButtonDelegate(m_Dialog, "ButtonClose", close, null);
			m_Description.SetNewText(text);
			m_Dialog.StopAllCoroutines();
			m_Dialog.StartCoroutine(CityGUIResources.Reset(m_Dialog, m_OrigPos));
			m_Dialog.StartCoroutine(CityGUIResources.ShowDialog(m_Dialog, m_OrigPos));
		}

		public override void Hide()
		{
			base.Hide();
			GuiBaseUtils.RegisterButtonDelegate(m_Dialog, "ButtonClose", null, null);
			m_Dialog.StopAllCoroutines();
			m_Dialog.StartCoroutine(CityGUIResources.Reset(m_Dialog, m_OrigPos));
			m_Dialog.StartCoroutine(CityGUIResources.HideDialog(m_Dialog, m_OrigPos));
		}
	}

	private class MissionStartDlg : CityBaseScreen
	{
		private GUIBase_Layout m_Dialog;

		private GUIBase_Label m_Caption;

		private GUIBase_TextArea m_Description;

		private GUIBase_Label m_Objective;

		private Vector3 m_OrigPos;

		public MissionStartDlg()
			: base(CityScreen.MissionStart)
		{
		}

		public void Init(GUIBase_Pivot pivot)
		{
			m_Dialog = pivot.GetLayout("MissionStart");
			m_Caption = m_Dialog.GetWidget("Caption").GetComponent<GUIBase_Label>();
			m_Description = m_Dialog.GetWidget("Description").GetComponent<GUIBase_TextArea>();
			m_Objective = m_Dialog.GetWidget("Objective").GetComponent<GUIBase_Label>();
			m_OrigPos = m_Dialog.transform.localPosition;
		}

		public void Show(GUIBase_Button.TouchDelegate accept, GUIBase_Button.TouchDelegate close, GUIBase_Button.TouchDelegate equip, GUIBase_Button.ReleaseDelegate buy, GUIBase_Button.TouchDelegate ownedEquipped, CityMissionInfo missionInfo)
		{
			base.Show(close);
			m_Dialog.StopAllCoroutines();
			GuiBaseUtils.RegisterButtonDelegate(m_Dialog, "ButtonAccept", accept, null);
			GuiBaseUtils.RegisterButtonDelegate(m_Dialog, "ButtonClose", close, null);
			GuiBaseUtils.RegisterButtonDelegate(m_Dialog, "ButtonEquip", equip, null);
			GUIBase_Widget component = m_Dialog.GetWidget("ButtonClose").GetComponent<GUIBase_Widget>();
			if (Game.Instance.PlayerPersistentInfo.storyId == 1)
			{
				component.Show(false, true);
			}
			else
			{
				component.Show(true, true);
			}
			GUIBase_Sprite component2 = m_Dialog.GetWidget("MissionPreview").GetComponent<GUIBase_Sprite>();
			if ((bool)component2 && missionInfo.levelPreview != string.Empty)
			{
				string text = "MissionPreviews/" + missionInfo.levelPreview;
				GameObject gameObject = Resources.Load(text) as GameObject;
				if (gameObject == null)
				{
					Debug.LogWarning("Cant find prefab: " + text);
				}
				else
				{
					GUIBase_Widget component3 = gameObject.GetComponent<GUIBase_Widget>();
					if ((bool)component3)
					{
						component2.Widget.CopyMaterialSettings(component3);
					}
				}
			}
			GUIBase_Sprite component4 = m_Dialog.GetWidget("WeaponPreview").GetComponent<GUIBase_Sprite>();
			GUIBase_Sprite component5 = m_Dialog.GetWidget("WeaponPreviewEmpty").GetComponent<GUIBase_Sprite>();
			GUIBase_Label component6 = m_Dialog.GetWidget("WeaponName").GetComponent<GUIBase_Label>();
			GUIBase_Label component7 = m_Dialog.GetWidget("WeaponCaption").GetComponent<GUIBase_Label>();
			if ((bool)component4 && missionInfo.recommendedWeapon != 0)
			{
				component4.Widget.CopyMaterialSettings(WeaponSettingsManager.Instance.Get(missionInfo.recommendedWeapon).ShopWidget);
				component6.SetNewText(WeaponSettingsManager.Instance.Get(missionInfo.recommendedWeapon).Name);
				component4.Widget.Show(true, true);
				component6.Widget.Show(true, true);
				component7.Widget.Show(true, true);
			}
			else
			{
				component4.Widget.Show(false, true);
				component6.Widget.Show(false, true);
				component7.Widget.Show(false, true);
			}
			GUIBase_Button component8 = m_Dialog.GetWidget("ButtonAccept").GetComponent<GUIBase_Button>();
			GUIBase_Button component9 = m_Dialog.GetWidget("ButtonEquip").GetComponent<GUIBase_Button>();
			GUIBase_Button component10 = m_Dialog.GetWidget("ButtonBuy").GetComponent<GUIBase_Button>();
			GUIBase_Button component11 = m_Dialog.GetWidget("ButtonOwnedEquipped").GetComponent<GUIBase_Button>();
			if (!Game.Instance.PlayerPersistentInfo.InventoryList.ContainsWeapon(missionInfo.recommendedWeapon))
			{
				component10.Widget.Show(true, true);
				component11.Widget.Show(false, true);
				GuiBaseUtils.RegisterButtonDelegate(m_Dialog, "ButtonBuy", null, buy);
			}
			else
			{
				component10.Widget.Show(false, true);
				component11.Widget.Show(true, true);
				GUIBase_Label component12 = m_Dialog.GetWidget("Button_OwnedEquippedCaption").GetComponent<GUIBase_Label>();
				if (!Game.Instance.PlayerPersistentInfo.EquipList.ContainsWeapon(missionInfo.recommendedWeapon))
				{
					component12.SetNewText(1010070);
					component11.SetDisabled(false);
				}
				else
				{
					component12.SetNewText(1010080);
					component11.SetDisabled(true);
				}
			}
			if (Game.Instance.PlayerPersistentInfo.storyId < 2 || missionInfo.recommendedWeapon == E_WeaponID.None)
			{
				component10.SetDisabled(true);
				component10.Widget.Show(false, true);
				component11.Widget.Show(false, true);
				component4.Widget.CopyMaterialSettings(component5.Widget);
				component6.SetNewText(1010055);
			}
			else
			{
				component10.SetDisabled(false);
				component11.SetDisabled(false);
			}
			if (Game.Instance.PlayerPersistentInfo.storyId < 2)
			{
				component9.Widget.Show(false, true);
				component9.SetDisabled(true);
			}
			else
			{
				component9.Widget.Show(true, true);
				if (Game.Instance.PlayerPersistentInfo.InventoryList.Weapons.Count == 0)
				{
					component9.SetDisabled(true);
				}
				else
				{
					component9.SetDisabled(false);
				}
				if (Game.Instance.PlayerPersistentInfo.EquipList.Weapons.Count == 0 || Game.Instance.PlayerPersistentInfo.EquipList.ContainsItemWithZeroCount())
				{
					m_Dialog.StartCoroutine(CityGUIResources.HighlightObject(component9.Widget));
				}
				else
				{
					component9.Widget.m_FadeAlpha = 1f;
				}
			}
			component8.SetDisabled(Game.Instance.PlayerPersistentInfo.EquipList.Weapons.Count == 0);
			if (missionInfo.bonus != 0)
			{
				m_Dialog.GetWidget("Bonus").GetComponent<GUIBase_Widget>().Show(true, true);
			}
			else
			{
				m_Dialog.GetWidget("Bonus").GetComponent<GUIBase_Widget>().Show(false, true);
			}
			GUIBase_Label component13 = m_Dialog.GetWidget("DifficultyText").GetComponent<GUIBase_Label>();
			switch (missionInfo.difficulty)
			{
			case MissionFlowData.Difficulty.Easy:
				component13.Widget.m_Color = Color.green;
				component13.SetNewText(1010012);
				break;
			case MissionFlowData.Difficulty.Normal:
				component13.Widget.m_Color = Color.white;
				component13.SetNewText(1010014);
				break;
			case MissionFlowData.Difficulty.Hard:
				component13.Widget.m_Color = Color.red * 0.75f;
				component13.SetNewText(1010016);
				break;
			}
			string text2 = TextDatabase.instance[1010290];
			text2 += " ";
			text2 += Game.Instance.PlayerPersistentInfo.totalMissionsPlayed + 1;
			text2 += ": ";
			text2 = ((missionInfo.story != MissionFlowData.StoryBound.DailyReward) ? (text2 + TextDatabase.instance[missionInfo.caption]) : (text2 + TextDatabase.instance[1010095]));
			m_Caption.SetNewText(text2);
			m_Objective.SetNewText(missionInfo.objective);
			string text3 = TextDatabase.instance[missionInfo.description];
			text3 = text3.Replace("%f", Random.Range(10, 400).ToString());
			m_Description.SetNewText(text3);
			m_Dialog.StartCoroutine(CityGUIResources.Reset(m_Dialog, m_OrigPos));
			m_Dialog.StartCoroutine(CityGUIResources.ShowDialog(m_Dialog, m_OrigPos));
		}

		public void DisableControls(bool disable)
		{
			m_Dialog.EnableControls(!disable);
		}

		public override void Hide()
		{
			base.Hide();
			GuiBaseUtils.RegisterButtonDelegate(m_Dialog, "ButtonClose", null, null);
			m_Dialog.StopAllCoroutines();
			m_Dialog.StartCoroutine(CityGUIResources.Reset(m_Dialog, m_OrigPos));
			m_Dialog.StartCoroutine(CityGUIResources.HideDialog(m_Dialog, m_OrigPos));
		}
	}

	private class MissionResultDlg : CityBaseScreen
	{
		private GUIBase_Layout m_Dialog;

		private Color m_FailColor;

		private Vector3 m_OrigPos;

		private AnimatedNumber MissionBonusMoney;

		private AnimatedNumber HeadShotsMoney;

		private AnimatedNumber RemovedLimbsMoney;

		private AnimatedNumber DesintegrationsMoney;

		private AnimatedNumber MoneyFoundMoney;

		private AnimatedNumber MissionEarnedValue;

		private AnimatedNumber ProfitTotalMoney;

		public MissionResultDlg()
			: base(CityScreen.MissionResult)
		{
		}

		public void Init(GUIBase_Pivot pivot)
		{
			m_Dialog = pivot.GetLayout("MissionResult");
			m_FailColor = new Color(1f, 0.1f, 0.1f);
			m_OrigPos = m_Dialog.transform.localPosition;
			MissionBonusMoney = new AnimatedNumber(m_Dialog.GetWidget("MissionBonusMoney").GetComponent<GUIBase_Label>());
			HeadShotsMoney = new AnimatedNumber(m_Dialog.GetWidget("HeadShotsMoney").GetComponent<GUIBase_Label>());
			RemovedLimbsMoney = new AnimatedNumber(m_Dialog.GetWidget("RemovedLimbsMoney").GetComponent<GUIBase_Label>());
			DesintegrationsMoney = new AnimatedNumber(m_Dialog.GetWidget("DesintegrationsMoney").GetComponent<GUIBase_Label>());
			MoneyFoundMoney = new AnimatedNumber(m_Dialog.GetWidget("MoneyFoundMoney").GetComponent<GUIBase_Label>());
			MissionEarnedValue = new AnimatedNumber(m_Dialog.GetWidget("MissionEarnedValue").GetComponent<GUIBase_Label>());
			ProfitTotalMoney = new AnimatedNumber(m_Dialog.GetWidget("ProfitTotalMoney").GetComponent<GUIBase_Label>());
		}

		public void Show(GUIBase_Button.TouchDelegate close, CityMissionInfo missionInfo)
		{
			base.Show(close);
			bool flag = missionInfo.missionResult.Result == MissionResult.Type.SUCCESS;
			Color color = ((!flag) ? m_FailColor : m_FailColor);
			GuiBaseUtils.RegisterButtonDelegate(m_Dialog, "ButtonClose", close, null);
			GUIBase_TextArea component = m_Dialog.GetWidget("Description").GetComponent<GUIBase_TextArea>();
			if (flag)
			{
				component.SetNewText(missionInfo.successText);
			}
			else
			{
				component.SetNewText(missionInfo.failText);
			}
			GUIBase_Label component2 = m_Dialog.GetWidget("CaptionResult").GetComponent<GUIBase_Label>();
			component2.GetComponent<GUIBase_Widget>().m_Color = color;
			if (flag)
			{
				component2.SetNewText(1010110);
			}
			else
			{
				component2.SetNewText(1010112);
			}
			GUIBase_Sprite component3 = m_Dialog.GetWidget("BestGadgetSprite").GetComponent<GUIBase_Sprite>();
			E_ItemID favouriteItem = missionInfo.missionResult.GetFavouriteItem();
			if (favouriteItem == E_ItemID.None)
			{
				component3.Widget.Show(false, true);
			}
			else
			{
				component3.Widget.CopyMaterialSettings(ItemSettingsManager.Instance.Get(favouriteItem).ShopWidget);
			}
			component3 = m_Dialog.GetWidget("BestWeaponSprite").GetComponent<GUIBase_Sprite>();
			E_WeaponID favouriteWeapon = missionInfo.missionResult.GetFavouriteWeapon();
			if (favouriteWeapon == E_WeaponID.None)
			{
				component3.Widget.Show(false, true);
			}
			else
			{
				component3.Widget.CopyMaterialSettings(WeaponSettingsManager.Instance.Get(favouriteWeapon).ShopWidget);
			}
			MFGuiManager.Instance.ShowLayout(m_Dialog, true);
			m_Dialog.GetWidget("MissionEarnedXP").Show(false, false);
			m_Dialog.StopAllCoroutines();
			m_Dialog.StartCoroutine(CityGUIResources.Reset(m_Dialog, m_OrigPos));
			m_Dialog.StartCoroutine(Show(missionInfo));
		}

		public override void Hide()
		{
			base.Hide();
			GuiBaseUtils.RegisterButtonDelegate(m_Dialog, "ButtonClose", null, null);
			m_Dialog.StopAllCoroutines();
			m_Dialog.StartCoroutine(CityGUIResources.Reset(m_Dialog, m_OrigPos));
			CityManager.Instance.StopSound(CityManager.Sounds.GUI_NumberLoop);
			m_Dialog.StartCoroutine(CityGUIResources.HideDialog(m_Dialog, m_OrigPos));
		}

		private IEnumerator Show(CityMissionInfo missionInfo)
		{
			yield return m_Dialog.StartCoroutine(CityGUIResources.ShowDialog(m_Dialog, m_OrigPos));
			float delay = 0.01f;
			GUIBase_Label label4 = m_Dialog.GetWidget("MissionTimeValue").GetComponent<GUIBase_Label>();
			label4.GetComponent<GUIBase_Widget>().Show(true, true);
			label4.SetNewText(missionInfo.missionResult.GetMissionTime());
			yield return new WaitForSeconds(delay);
			label4 = m_Dialog.GetWidget("KilledZombiesValue").GetComponent<GUIBase_Label>();
			label4.GetComponent<GUIBase_Widget>().Show(true, true);
			label4.SetNewText(missionInfo.missionResult.KilledZombies.ToString());
			yield return new WaitForSeconds(delay);
			label4 = m_Dialog.GetWidget("FireAccuracyValue").GetComponent<GUIBase_Label>();
			label4.GetComponent<GUIBase_Widget>().Show(true, true);
			label4.SetNewText(missionInfo.missionResult.FireAccuracy + "%");
			yield return new WaitForSeconds(delay);
			label4 = m_Dialog.GetWidget("InjuredValue").GetComponent<GUIBase_Label>();
			label4.GetComponent<GUIBase_Widget>().Show(true, true);
			label4.SetNewText(Mathf.RoundToInt(missionInfo.missionResult.HealthLost * 100f) + " %");
			yield return new WaitForSeconds(delay);
			label4 = m_Dialog.GetWidget("WastedMoneyValue").GetComponent<GUIBase_Label>();
			label4.GetComponent<GUIBase_Widget>().Show(true, true);
			label4.SetNewText(missionInfo.wastedMoney.ToString());
			yield return new WaitForSeconds(delay);
			MissionBonusMoney.Label.Widget.Show(true, true);
			m_Dialog.StartCoroutine(CityGUIResources.AnimateNumber(0, missionInfo.bonusMoney, MissionBonusMoney, 1f, "$"));
			yield return new WaitForSeconds(delay);
			label4 = m_Dialog.GetWidget("HeadshotsValue").GetComponent<GUIBase_Label>();
			label4.GetComponent<GUIBase_Widget>().Show(true, true);
			label4.SetNewText(missionInfo.missionResult.HeadShots.ToString());
			yield return new WaitForSeconds(delay);
			HeadShotsMoney.Label.Widget.Show(true, true);
			m_Dialog.StartCoroutine(CityGUIResources.AnimateNumber(0, missionInfo.headShotMoney, HeadShotsMoney, 1f, "$"));
			yield return new WaitForSeconds(delay);
			label4 = m_Dialog.GetWidget("RemovedLimbsValue").GetComponent<GUIBase_Label>();
			label4.GetComponent<GUIBase_Widget>().Show(true, true);
			label4.SetNewText(missionInfo.missionResult.RemovedLimbs.ToString());
			yield return new WaitForSeconds(delay);
			RemovedLimbsMoney.Label.Widget.Show(true, true);
			m_Dialog.StartCoroutine(CityGUIResources.AnimateNumber(0, missionInfo.limbMoney, RemovedLimbsMoney, 1f, "$"));
			yield return new WaitForSeconds(delay);
			label4 = m_Dialog.GetWidget("DisintegrationsValue").GetComponent<GUIBase_Label>();
			label4.GetComponent<GUIBase_Widget>().Show(true, true);
			label4.SetNewText(missionInfo.missionResult.Disintegrations.ToString());
			yield return new WaitForSeconds(delay);
			DesintegrationsMoney.Label.Widget.Show(true, true);
			m_Dialog.StartCoroutine(CityGUIResources.AnimateNumber(0, missionInfo.desintegrationMoney, DesintegrationsMoney, 1f, "$"));
			yield return new WaitForSeconds(delay);
			yield return new WaitForSeconds(delay);
			MoneyFoundMoney.Label.Widget.GetComponent<GUIBase_Widget>().Show(true, true);
			m_Dialog.StartCoroutine(CityGUIResources.AnimateNumber(0, missionInfo.foundMoney, MoneyFoundMoney, 1f, "$"));
			yield return new WaitForSeconds(0.5f);
			MissionEarnedValue.Label.Widget.Show(true, true);
			yield return m_Dialog.StartCoroutine(CityGUIResources.AnimateNumber(0, missionInfo.totalExperience, MissionEarnedValue, 1f, "xp"));
			ProfitTotalMoney.Label.Widget.Show(true, true);
			yield return m_Dialog.StartCoroutine(CityGUIResources.AnimateNumber(0, missionInfo.totalMoney, ProfitTotalMoney, 1f, "$"));
		}
	}

	private class PromotionDlg : CityBaseScreen
	{
		private GUIBase_Layout m_Dialog;

		private Vector3 m_OrigPos;

		public PromotionDlg()
			: base(CityScreen.Promoted)
		{
		}

		public void Init(GUIBase_Pivot pivot)
		{
			m_Dialog = pivot.GetLayout("Promoted");
			m_OrigPos = m_Dialog.transform.localPosition;
		}

		public void Show(GUIBase_Button.TouchDelegate close, bool newGoodsInShop)
		{
			base.Show();
			GuiBaseUtils.RegisterButtonDelegate(m_Dialog, "ButtonClose", close, null);
			GUIBase_Label component = m_Dialog.GetWidget("Message").GetComponent<GUIBase_Label>();
			string text = TextDatabase.instance[1010250];
			string text2 = text;
			text = text2 + " " + Game.Instance.PlayerPersistentInfo.rank + " " + TextDatabase.instance[1010251];
			component.SetNewText(text);
			GUIBase_Label component2 = m_Dialog.GetWidget("Shop").GetComponent<GUIBase_Label>();
			component2.Widget.Show(newGoodsInShop, true);
			// The reward count is a static "+1" baked into the Promoted layout, so it
			// would silently disagree with GoldPerRankUp. Drive it from the constant
			// instead. GetWidget is scoped to this layout, so the identically named
			// label in the other reward dialogs is not affected.
			GUIBase_Widget rewardWidget = m_Dialog.GetWidget("Reward3_Count", false);
			if (rewardWidget != null)
			{
				GUIBase_Label rewardLabel = rewardWidget.GetComponent<GUIBase_Label>();
				if (rewardLabel != null)
				{
					rewardLabel.SetNewText("+" + PlayerPersistantInfo.GoldPerRankUp);
				}
			}
			if ((bool)CityManager.Instance)
			{
				CityManager.Instance.PlaySound(CityManager.Sounds.GUI_Promoted, false);
			}
			m_Dialog.StartCoroutine(CityGUIResources.ShowDialog(m_Dialog, m_OrigPos));
		}

		public override void Hide()
		{
			base.Hide();
			GuiBaseUtils.RegisterButtonDelegate(m_Dialog, "ButtonClose", null, null);
			m_Dialog.StartCoroutine(CityGUIResources.HideDialog(m_Dialog, m_OrigPos));
		}
	}

	private class SpecialRewardDlg : CityBaseScreen
	{
		private GUIBase_Layout m_Dialog;

		private Vector3 m_OrigPos;

		public SpecialRewardDlg()
			: base(CityScreen.SpecialReward)
		{
		}

		public void Init(GUIBase_Pivot pivot)
		{
			m_Dialog = pivot.GetLayout("SpecialReward");
			m_OrigPos = m_Dialog.transform.localPosition;
		}

		public void Show(GUIBase_Button.TouchDelegate close, SpecialRewardInfo rewardInfo)
		{
			base.Show();
			GuiBaseUtils.RegisterButtonDelegate(m_Dialog, "ButtonClose", close, null);
			GUIBase_Label component = m_Dialog.GetWidget("Reward_Count").GetComponent<GUIBase_Label>();
			GUIBase_Label component2 = m_Dialog.GetWidget("Reward_Label").GetComponent<GUIBase_Label>();
			GUIBase_Widget component3 = m_Dialog.GetWidget("Reward1").GetComponent<GUIBase_Widget>();
			component.SetNewText(rewardInfo.Amount);
			component2.SetNewText(rewardInfo.Name);
			component3.CopyMaterialSettings(rewardInfo.Picture);
			if ((bool)CityManager.Instance)
			{
				CityManager.Instance.PlaySound(CityManager.Sounds.GUI_Reward, false);
			}
			m_Dialog.StartCoroutine(CityGUIResources.ShowDialog(m_Dialog, m_OrigPos));
		}

		public override void Hide()
		{
			base.Hide();
			GuiBaseUtils.RegisterButtonDelegate(m_Dialog, "ButtonClose", null, null);
			m_Dialog.StartCoroutine(CityGUIResources.HideDialog(m_Dialog, m_OrigPos));
		}
	}

	private StoryChapterDlg m_StoryChapter = new StoryChapterDlg();

	private StoryEventDlg m_StoryEvent = new StoryEventDlg();

	private BuyerRewardDlg m_BuyerReward = new BuyerRewardDlg();

	private OneTimeSaleOfferDlg m_OneTimeSaleOffer = new OneTimeSaleOfferDlg();

	private TapJoyAdvertDlg m_TapJoyAdvert = new TapJoyAdvertDlg();

	private HalloweenAdvertDlg m_HalloweenAdvert = new HalloweenAdvertDlg();

	private ChristmasAdvertDlg m_ChristmasAdvert = new ChristmasAdvertDlg();

	private DailyRewardDlg m_DailyReward = new DailyRewardDlg();

	private SiteInfoDlg m_SiteInfo = new SiteInfoDlg();

	private MissionStartDlg m_MissionStart = new MissionStartDlg();

	private MissionResultDlg m_MissionResult = new MissionResultDlg();

	private PromotionDlg m_Promotion = new PromotionDlg();

	private SpecialRewardDlg m_SpecialReward = new SpecialRewardDlg();

	private GUIBase_Pivot m_Pivot;

	public CityGUIDialogs()
	{
		Init();
	}

	public void Destroy()
	{
		DestroyInternal();
	}

	public void ShowStoryChapter(GUIBase_Button.TouchDelegate close, StoryFlowData.Story story, bool isDebriefing)
	{
		m_StoryChapter.Show(close, story, isDebriefing);
	}

	public void HideStoryChapter()
	{
		m_StoryChapter.Hide();
	}

	public void ShowStoryEvent(GUIBase_Button.TouchDelegate close, int text)
	{
		m_StoryEvent.Show(close, text);
	}

	public void HideStoryEvent()
	{
		m_StoryEvent.Hide();
	}

	public void ShowBuyerReward(GUIBase_Button.TouchDelegate close, int numGold, bool alienGun, int numCasinoChips, bool reward180)
	{
		m_BuyerReward.Show(close, numGold, alienGun, numCasinoChips, reward180);
	}

	public void HideBuyerReward()
	{
		m_BuyerReward.Hide();
	}

	public void ShowOneTimeSaleOffer(GUIBase_Button.TouchDelegate close, OneTimeSaleOfferManager manager)
	{
		m_OneTimeSaleOffer.Show(close, manager);
	}

	public void HideOneTimeSaleOffer()
	{
		m_OneTimeSaleOffer.Hide();
	}

	public void ShowTapJoyAdvert(GUIBase_Button.TouchDelegate close)
	{
		m_TapJoyAdvert.Show(close);
	}

	public void HideTapJoyAdvert()
	{
		m_TapJoyAdvert.Hide();
	}

	public void ShowHalloweenAdvert(GUIBase_Button.TouchDelegate close)
	{
		m_HalloweenAdvert.Show(close);
	}

	public void HideHalloweenAdvert()
	{
		m_HalloweenAdvert.Hide();
	}

	public void ShowChristmasAdvert(GUIBase_Button.TouchDelegate close)
	{
		m_ChristmasAdvert.Show(close);
	}

	public void HideChristmasAdvert()
	{
		m_ChristmasAdvert.Hide();
	}

	public void ShowDailyReward(GUIBase_Button.TouchDelegate close, bool arenaUnlocked)
	{
		m_DailyReward.Show(close, arenaUnlocked);
	}

	public void HideDailyReward()
	{
		m_DailyReward.Hide();
	}

	public void ShowSiteInfo(GUIBase_Button.TouchDelegate close, int text)
	{
		m_SiteInfo.Show(close, text);
	}

	public void HideSiteInfo()
	{
		m_SiteInfo.Hide();
	}

	public void ShowMissionStart(GUIBase_Button.TouchDelegate accept, GUIBase_Button.TouchDelegate close, GUIBase_Button.TouchDelegate equip, GUIBase_Button.ReleaseDelegate buy, GUIBase_Button.TouchDelegate ownedEquipped, CityMissionInfo missionInfo)
	{
		m_MissionStart.Show(accept, close, equip, buy, ownedEquipped, missionInfo);
	}

	public void MissionStartDisableControls(bool disable)
	{
		m_MissionStart.DisableControls(disable);
	}

	public void HideMissionStart()
	{
		m_MissionStart.Hide();
	}

	public void ShowMissionResult(GUIBase_Button.TouchDelegate close, CityMissionInfo missionInfo)
	{
		m_MissionResult.Show(close, missionInfo);
	}

	public void HideMissionResult()
	{
		m_MissionResult.Hide();
	}

	public void ShowPromotion(GUIBase_Button.TouchDelegate close, bool newGoodsInShop)
	{
		m_Promotion.Show(close, newGoodsInShop);
	}

	public void HidePromotion()
	{
		m_Promotion.Hide();
	}

	public void ShowSpecialReward(GUIBase_Button.TouchDelegate close, SpecialRewardInfo reward)
	{
		m_SpecialReward.Show(close, reward);
	}

	public void HideSpecialReward()
	{
		m_SpecialReward.Hide();
	}

	public void EnableScreens()
	{
	}

	public void DisableScreens()
	{
	}

	private void Init()
	{
		m_Pivot = MFGuiManager.Instance.GetPivot("CityScreens");
		m_StoryChapter.Init(m_Pivot);
		m_StoryEvent.Init(m_Pivot);
		m_BuyerReward.Init(m_Pivot);
		m_OneTimeSaleOffer.Init(m_Pivot);
		m_TapJoyAdvert.Init(m_Pivot);
		m_HalloweenAdvert.Init(m_Pivot);
		m_ChristmasAdvert.Init(m_Pivot);
		m_DailyReward.Init(m_Pivot);
		m_SiteInfo.Init(m_Pivot);
		m_MissionStart.Init(m_Pivot);
		m_MissionResult.Init(m_Pivot);
		m_Promotion.Init(m_Pivot);
		m_SpecialReward.Init(m_Pivot);
	}

	private void DestroyInternal()
	{
	}
}
