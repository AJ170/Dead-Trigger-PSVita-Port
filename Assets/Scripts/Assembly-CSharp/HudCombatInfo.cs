using System;
using System.Collections.Generic;
using UnityEngine;

public class HudCombatInfo : HudComponent
{
	public enum E_MessageType
	{
		Leg = 0,
		Arm = 1,
		Head = 2,
		Body = 3,
		BloodBath = 4,
		BloodLust = 5,
		Massacre = 6
	}

	public class Message
	{
		private GUIBase_Widget m_Widget;

		private Vector3 m_BaseScale;

		private float m_CurrentScale;

		private float m_CurrentAlpha;

		private Transform m_Transform;

		private float m_Speed;

		private float m_Progress;

		public bool IsVisible { get; private set; }

		public float Progress
		{
			get
			{
				return m_Progress;
			}
		}

		public Message(GUIBase_Layout layout, string name)
		{
			m_Widget = layout.GetWidget(name);
			m_Transform = m_Widget.transform;
			m_BaseScale = m_Transform.localScale;
			Hide();
		}

		public void SetLayer(int order)
		{
			m_Widget.m_GuiWidgetLayer = order;
		}

		public void Enable(bool enable)
		{
			if (enable && IsVisible)
			{
				m_Widget.Show(true, true);
			}
			else if (!enable && IsVisible)
			{
				m_Widget.Show(false, true);
			}
		}

		public void Show(float speed)
		{
			m_Progress = 0f;
			m_Speed = speed;
			m_Widget.Show(true, true);
			IsVisible = true;
		}

		public void Update()
		{
			if (IsVisible)
			{
				m_Progress = Mathf.Min(m_Progress + TimeManager.Instance.GetRealDeltaTime() * m_Speed, 2f);
				m_CurrentScale = Mathfx.Sinerp(1f, 1.7f, m_Progress);
				float fadeAlpha = Mathfx.Sinerp(0f, 1.3f, m_Progress);
				m_Transform.localScale = m_BaseScale * m_CurrentScale;
				m_Widget.m_FadeAlpha = fadeAlpha;
				if (m_Progress >= 2f)
				{
					Hide();
				}
			}
		}

		private void Hide()
		{
			IsVisible = false;
			m_Widget.Show(false, true);
		}
	}

	private class Hits
	{
		private GUIBase_Widget m_HitsLabel;

		private Transform m_HitsLabelTransform;

		private GUIBase_Number m_HitsNumber;

		private Transform m_HitsNumberTransform;

		private Vector3 m_BaseScaleNumber;

		private Vector3 m_BaseScaleHit;

		private float m_CurrentScale;

		private float m_CurrentAlpha;

		private float m_Speed = 7f;

		private float m_Progress;

		private int Count;

		private float TimeToHide;

		public bool IsVisible { get; private set; }

		public float Progress
		{
			get
			{
				return m_Progress;
			}
		}

		public Hits(GUIBase_Layout layout)
		{
			m_HitsLabel = layout.GetWidget("Hits");
			m_HitsLabelTransform = m_HitsLabel.transform;
			m_HitsNumber = layout.GetWidget("HitsNumber").GetComponent<GUIBase_Number>();
			m_HitsNumberTransform = m_HitsNumber.transform;
			m_BaseScaleHit = m_HitsLabelTransform.localScale;
			m_BaseScaleNumber = m_HitsNumberTransform.localScale;
			Count = 0;
			Hide();
		}

		public void Enable(bool enable)
		{
			if (enable && IsVisible)
			{
				m_HitsLabel.Show(true, true);
				m_HitsNumber.Widget.Show(true, true);
			}
			else if (!enable && IsVisible)
			{
				m_HitsLabel.Show(false, true);
				m_HitsNumber.Widget.Show(false, true);
			}
		}

		public void AddHit()
		{
			if (!IsVisible)
			{
				m_HitsLabel.Show(true, true);
				m_HitsNumber.Widget.Show(true, true);
			}
			Count++;
			TimeToHide = Time.timeSinceLevelLoad + 5f;
			m_Progress = 0f;
			m_HitsNumber.SetNumber(Count, 999);
			IsVisible = true;
		}

		public void Update()
		{
			if (IsVisible)
			{
				m_Progress = Mathf.Min(m_Progress + TimeManager.Instance.GetRealDeltaTime() * m_Speed, 2f);
				m_CurrentScale = Mathfx.Sinerp(1f, 1.5f, m_Progress);
				m_HitsNumberTransform.localScale = m_BaseScaleNumber * m_CurrentScale;
				m_HitsLabelTransform.localScale = m_BaseScaleHit * m_CurrentScale;
				if (TimeToHide <= Time.timeSinceLevelLoad)
				{
					Hide();
				}
			}
		}

		private void Hide()
		{
			IsVisible = false;
			m_HitsLabel.Show(false, true);
			m_HitsNumber.Widget.Show(false, true);
			Count = 0;
		}
	}

	private class RankNotify
	{
		private GUIBase_Widget m_Parent;

		private GUIBase_Label m_Text;

		private GUIBase_Label m_NewGoodsInShop;

		private float m_CurrentAlpha;

		private float m_Speed = 1f;

		private float m_Progress;

		private float TimeToHide;

		public bool IsVisible { get; private set; }

		public float Progress
		{
			get
			{
				return m_Progress;
			}
		}

		public RankNotify(GUIBase_Layout layout)
		{
			m_Parent = layout.GetWidget("NotifyInfo");
			m_Text = layout.GetWidget("RankText").GetComponent<GUIBase_Label>();
			m_NewGoodsInShop = layout.GetWidget("NewGoodsInShop").GetComponent<GUIBase_Label>();
			Hide();
		}

		public void Enable(bool enable)
		{
			if (enable && IsVisible)
			{
				m_Parent.Show(true, true);
				m_NewGoodsInShop.Widget.Show(Game.Instance.PlayerPersistentInfo.storyId >= 2 && ShopDataBridge.Instance.NewItemsUnlocked(Game.Instance.PlayerPersistentInfo.rank), true);
			}
			else if (!enable && IsVisible)
			{
				m_Parent.Show(false, true);
			}
		}

		public void Show(int newRank)
		{
			m_Parent.Show(true, true);
			m_Text.SetNewText(TextDatabase.instance[m_Text.m_TextID] + " " + newRank + " " + TextDatabase.instance[1010251]);
			m_NewGoodsInShop.Widget.Show(Game.Instance.PlayerPersistentInfo.storyId >= 2 && ShopDataBridge.Instance.NewItemsUnlocked(newRank), true);
			m_Progress = 0f;
			IsVisible = true;
			m_Parent.GetComponent<AudioSource>().Play();
			Game.Instance.PlayerPersistentInfo.AddGold(PlayerPersistantInfo.GoldPerRankUp);
		}

		public void Update()
		{
			if (IsVisible)
			{
				m_Progress = Mathf.Min(m_Progress + TimeManager.Instance.GetRealDeltaTime() * m_Speed, 2f);
				float num = Mathfx.Sinerp(0f, 1f, m_Progress);
				m_Parent.m_FadeAlpha = num;
				if (num <= 0f)
				{
					Hide();
				}
			}
		}

		private void Hide()
		{
			IsVisible = false;
			m_Parent.Show(false, true);
		}
	}

	private class AchievementNotify
	{
		private GUIBase_Widget m_Parent;

		private GUIBase_Label m_Text;

		private float m_CurrentAlpha;

		private float m_Speed = 1f;

		private float m_Progress;

		private float TimeToHide;

		public bool IsVisible { get; private set; }

		public float Progress
		{
			get
			{
				return m_Progress;
			}
		}

		public AchievementNotify(GUIBase_Layout layout)
		{
			m_Parent = layout.GetWidget("Achievements");
			m_Text = layout.GetWidget("AchievementsText").GetComponent<GUIBase_Label>();
			Hide();
		}

		public void Enable(bool enable)
		{
			if (enable && IsVisible)
			{
				m_Parent.Show(true, true);
			}
			else if (!enable && IsVisible)
			{
				m_Parent.Show(false, true);
			}
		}

		public void Show(string text)
		{
			m_Parent.Show(true, true);
			m_Text.SetNewText(text);
			m_Progress = 0f;
			IsVisible = true;
			m_Parent.GetComponent<AudioSource>().Play();
		}

		public void Update()
		{
			if (IsVisible)
			{
				m_Progress = Mathf.Min(m_Progress + TimeManager.Instance.GetRealDeltaTime() * m_Speed, 2f);
				float num = Mathfx.Sinerp(0f, 3.5f, m_Progress);
				m_Parent.m_FadeAlpha = num;
				if (num <= 0f)
				{
					Hide();
				}
			}
		}

		private void Hide()
		{
			IsVisible = false;
			m_Parent.Show(false, true);
		}
	}

	private Dictionary<E_MessageType, Message> Messages = new Dictionary<E_MessageType, Message>();

	private Hits HitInfo;

	private RankNotify RankInfo;

	private AchievementNotify AchievementInfo;

	private string s_PivotMainName = "MainHUD";

	private string s_LayoutMainName = "HUD_Layout";

	private string s_Parent = "CombatInfo";

	public override bool VisibleOnStart()
	{
		return true;
	}

	public override void Init()
	{
		GUIBase_Pivot pivot = MFGuiManager.Instance.GetPivot(s_PivotMainName);
		GUIBase_Layout layout = pivot.GetLayout(s_LayoutMainName);
		layout.GetWidget(s_Parent).GetComponent<GUIBase_Widget>();
		Messages.Add(E_MessageType.Arm, new Message(layout, "Arm"));
		Messages.Add(E_MessageType.Leg, new Message(layout, "Leg"));
		Messages.Add(E_MessageType.Head, new Message(layout, "Head"));
		Messages.Add(E_MessageType.Body, new Message(layout, "Body"));
		HitInfo = new Hits(layout);
		RankInfo = new RankNotify(layout);
		AchievementInfo = new AchievementNotify(layout);
		PlayerPersistantInfo playerPersistentInfo = Game.Instance.PlayerPersistentInfo;
		playerPersistentInfo.OnRankChanged = (PlayerPersistantInfo.PersistenInfoChanged)Delegate.Combine(playerPersistentInfo.OnRankChanged, new PlayerPersistantInfo.PersistenInfoChanged(ShowNewRank));
	}

	public override void OnDestroy()
	{
		Messages.Clear();
		HitInfo = null;
		RankInfo = null;
		AchievementInfo = null;
		PlayerPersistantInfo playerPersistentInfo = Game.Instance.PlayerPersistentInfo;
		playerPersistentInfo.OnRankChanged = (PlayerPersistantInfo.PersistenInfoChanged)Delegate.Remove(playerPersistentInfo.OnRankChanged, new PlayerPersistantInfo.PersistenInfoChanged(ShowNewRank));
	}

	public override void Reset()
	{
	}

	public override void LateUpdate(float deltaTime)
	{
		foreach (KeyValuePair<E_MessageType, Message> message in Messages)
		{
			message.Value.Update();
		}
		HitInfo.Update();
		RankInfo.Update();
		AchievementInfo.Update();
	}

	protected override void ShowWidgets(bool on)
	{
		foreach (KeyValuePair<E_MessageType, Message> message in Messages)
		{
			message.Value.Enable(on);
		}
		HitInfo.Enable(on);
		RankInfo.Enable(on);
		AchievementInfo.Enable(on);
	}

	public void ShowInfo(E_MessageType message, float speed)
	{
		Messages[message].Show(speed);
		List<Message> list = new List<Message>();
		foreach (KeyValuePair<E_MessageType, Message> message2 in Messages)
		{
			if (message2.Value.IsVisible)
			{
				list.Add(message2.Value);
			}
		}
		list.Sort((Message p1, Message p2) => p1.Progress.CompareTo(p2.Progress));
		int num = 1;
		foreach (KeyValuePair<E_MessageType, Message> message3 in Messages)
		{
			message3.Value.SetLayer(num++);
		}
	}

	public void ShowHit()
	{
		HitInfo.AddHit();
	}

	public void ShowNewRank()
	{
		RankInfo.Show(Game.Instance.PlayerPersistentInfo.rank);
	}

	public void ShowAchievement(string text)
	{
		AchievementInfo.Show(text);
	}
}
