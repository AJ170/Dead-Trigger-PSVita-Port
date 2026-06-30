using System;
using UnityEngine;

public class HudMessages : HudComponent
{
	private class MessageEffect
	{
		private const float TextScaleIn = 0.15f;

		private const float TextScaleHold = 0f;

		private const float TextScaleBack = 0.25f;

		private const float TextFadeIn = 0.1f;

		private const float TextFadeOut = 0.8f;

		private const float TextBaseScale = 1f;

		private const float TextFadeOutStart = 0.4f;

		private const float TextMoveUpStart = 0.1f;

		private const float TextMoveUpDuration = 1.2f;

		private const float TextMoveUpScreenRatio = 0.16f;

		private const float BckFadeIn = 0.1f;

		private const float BckFadeOut = 0.8f;

		private const float BckFadeOutStart = 0.2f;

		private const float BckMoveUpStart = 0.1f;

		private const float BckMoveUpDuration = 1f;

		private const float BckMoveUpScreenRatio = 0.16f;

		private GUIBase_Platform m_Platform;

		private GUIBase_Sprite m_Background;

		private GUIBase_Label m_Text;

		private GUIBase_Widget m_TextW;

		private Vector3 m_BckOrigPos;

		private Vector3 m_TextOrigPos;

		private float m_Timer = -1f;

		private float m_BckTimer = -1f;

		public MessageEffect(GUIBase_Sprite background, GUIBase_Label text, GUIBase_Pivot pivotMessages)
		{
			m_Background = background;
			m_Text = text;
			m_TextW = text.GetComponent<GUIBase_Widget>();
			m_TextOrigPos = m_Text.transform.localPosition;
			m_BckOrigPos = m_Background.transform.localPosition;
			m_Platform = MFGuiManager.Instance.GetPlatform(pivotMessages);
		}

		public void Show(string text)
		{
			m_Text.transform.localScale = Vector3.one;
			m_Background.Widget.transform.localScale = Vector3.one;
			m_Text.transform.localPosition = m_TextOrigPos;
			m_Background.transform.localPosition = m_BckOrigPos;
			m_Background.Widget.Show(true, true);
			m_Text.SetNewText(text);
			m_Timer = 0f;
			m_BckTimer = 0f;
		}

		public void Update(float time)
		{
			UpdateBackground(time);
			UpdateText(time);
		}

		private void UpdateBackground(float time)
		{
			if (!(m_BckTimer < 0f))
			{
				m_BckTimer += time;
				float num = 1f;
				if (m_BckTimer > 1f)
				{
					m_Background.Widget.Show(false, true);
					m_BckTimer = -1f;
				}
				else if (m_BckTimer > 0.2f)
				{
					num = 1f - (m_BckTimer - 0.2f) / 0.8f;
				}
				else if (m_BckTimer < 0.1f)
				{
					num = m_BckTimer / 0.1f;
				}
				if (m_BckTimer > 0.1f && m_BckTimer < 1.1f)
				{
					float num2 = (m_BckTimer - 0.1f) / 1f;
					Vector3 bckOrigPos = m_BckOrigPos;
					bckOrigPos.y = m_BckOrigPos.y - (float)m_Platform.m_Height * 0.16f * num2;
					m_Background.transform.localPosition = bckOrigPos;
					float x = 0.5f + (1f - num2) / 2f;
					float num3 = num2 / 2f;
					m_Background.Widget.transform.localScale = new Vector3(x, 1f + num3, 1f);
				}
				m_Background.Widget.m_FadeAlpha = num * 0.75f;
			}
		}

		public void UpdateText(float time)
		{
			if (!(m_Timer < 0f))
			{
				m_Timer += time;
				if (!m_TextW.IsVisible())
				{
					m_TextW.Show(true, true);
				}
				float num = 1f;
				float y = 1f;
				float num2 = 1f;
				if (m_Timer > 1.2f)
				{
					m_TextW.Show(false, true);
					m_Timer = -1f;
				}
				else if (m_Timer > 0.4f)
				{
					num2 = 1f - (m_Timer - 0.4f) / 0.8f;
				}
				else if (m_Timer < 0.1f)
				{
					num2 = m_Timer / 0.1f;
				}
				if (m_Timer > 0.15f && m_Timer < 0.4f)
				{
					num = 1f + Mathf.Sin((1f - (m_Timer - 0.15f) / 0.25f) * (float)Math.PI / 2f) * 2f;
					y = num;
				}
				else if (!(m_Timer > 0.15f) && m_Timer <= 0.15f)
				{
					num = 1f + Mathf.Sin(m_Timer / 0.15f * (float)Math.PI / 2f) * 2f;
					y = num;
				}
				if (m_Timer > 0.1f && m_Timer < 1.3000001f)
				{
					Vector3 textOrigPos = m_TextOrigPos;
					textOrigPos.y = m_TextOrigPos.y - (float)m_Platform.m_Height * 0.16f * Mathf.Sin((float)Math.PI / 2f * (m_Timer - 0.1f) / 1.2f);
					m_TextW.transform.localPosition = textOrigPos;
				}
				m_TextW.transform.localScale = new Vector3(num, y, num);
				m_TextW.m_FadeAlpha = num2 * 0.95f;
			}
		}
	}

	private class FadeText
	{
		private GUIBase_Label m_Label;

		private GUIBase_Sprite m_Background;

		private string m_Text;

		private float m_DelayTimer;

		private float m_Timer = -1f;

		private float m_Alpha;

		private float m_AlphaWeight = 1f;

		private float m_Hold = 5f;

		private float FadeIn = 0.5f;

		private float FadeOut = 0.5f;

		// Add as a field
		private GUIBase_Widget m_LabelWidget = null;

		public FadeText(GUIBase_Label label, GUIBase_Sprite background, float fadeIn, float fadeOut, float alphaWeight, Color textColor)
		{
			m_Label = label;
			m_Label.Widget.m_Color = textColor;
			m_Background = background;
			FadeIn = fadeIn;
			FadeOut = fadeOut;
			m_AlphaWeight = alphaWeight;
		}

		public bool IsActive()
		{
			return m_Timer >= 0f;
		}

		public void CopyFrom(FadeText otherText)
		{
			if (!otherText.IsActive())
			{
				Reset();
				return;
			}
			m_Text = otherText.m_Text;
			m_Timer = otherText.m_Timer;
			m_Alpha = otherText.m_Alpha;
			m_Label.SetNewText(m_Text);
			m_Hold = otherText.m_Hold;
			GUIBase_Widget component = m_Label.GetComponent<GUIBase_Widget>();
			component.Show(true, true);
			if ((bool)m_Background)
			{
				m_Background.Widget.Show(true, true);
			}
			SetAlpha();
		}

		public void ShowMessage(string msg, float time, float showDelay = 0f)
		{
			if (m_Timer > FadeIn && m_Timer < FadeIn + m_Hold && showDelay == 0f)
			{
				m_Timer = FadeIn;
				m_Alpha = 1f;
			}
			else
			{
				m_Timer = 0f;
				m_Alpha = 0f;
			}
			m_Alpha = 0f;
			m_DelayTimer = showDelay;
			m_Text = msg;
			m_Hold = time;
			m_Label.SetNewText(msg);
			GUIBase_Widget component = m_Label.GetComponent<GUIBase_Widget>();
			component.Show(true, true);
			if ((bool)m_Background)
			{
				m_Background.Widget.Show(true, true);
			}
			SetAlpha();
		}

		public void Update(float time)
		{
			if (m_LabelWidget == null)
            {
				m_LabelWidget = m_Label.GetComponent<GUIBase_Widget>();
			}

			if (m_DelayTimer > 0f)
				m_DelayTimer -= time;

			if (!(m_DelayTimer <= 0f) || !(m_Timer >= 0f))
				return;

			m_Timer += time;

			float num = ((m_Timer > FadeIn + m_Hold && m_Hold > 0f)
				? Mathf.Clamp(1f - (m_Timer - FadeIn - m_Hold) / FadeOut, 0f, 1f)
				: ((Mathf.Approximately(m_Alpha, 1f) || (!(m_Hold > 0f)
					&& !(m_Timer <= FadeIn)))
					? (-1f)
					: Mathf.Clamp(m_Timer / FadeIn, 0f, 1f)));

			if (num >= 0f)
			{
				m_Alpha = num;
				SetAlpha();
			}

			if (m_Timer > FadeIn + m_Hold + FadeOut && m_Hold > 0f)
			{
				Reset();
				return;
			}

			if (!m_LabelWidget.IsVisible())
				m_LabelWidget.Show(true, true);

			if ((bool)m_Background && !m_Background.Widget.IsVisible())
				m_Background.Widget.Show(true, true);
		}

		private void Reset()
		{
			m_Timer = -1f;
			m_Label.GetComponent<GUIBase_Widget>().Show(false, true);
			if ((bool)m_Background)
			{
				m_Background.Widget.Show(false, true);
			}
		}

		private void SetAlpha()
		{
			m_Label.GetComponent<GUIBase_Widget>().m_FadeAlpha = m_Alpha * m_AlphaWeight;
			if ((bool)m_Background)
			{
				m_Background.Widget.m_FadeAlpha = m_Alpha * m_AlphaWeight * 0.2f;
			}
		}
	}

	private const float ObjectiveHoldDefault = 5f;

	private GUIBase_Pivot m_PivotMessages;

	private GUIBase_Layout m_ConsoleLayout;

	private GUIBase_Layout m_EffectLayout;

	private FadeText[] m_ConsoleRows;

	private MessageEffect[] m_MessageEffects;

	private FadeText m_Objective;

	private FadeText m_Objective2;

	private FadeText m_ObjectiveSmall;

	private GUIBase_Layout m_ObjectiveLayout;

	private string s_PivotMessages = "Messages";

	private string s_Console = "Console";

	private string s_Objective = "Objectives";

	private string s_Effects = "Effects";

	public override bool VisibleOnStart()
	{
		return true;
	}

	public override void Init()
	{
		m_PivotMessages = MFGuiManager.Instance.GetPivot(s_PivotMessages);
		if (!m_PivotMessages)
		{
			Debug.LogError("Pivot 'Messages' not found! ");
			return;
		}
		m_ConsoleLayout = GuiBaseUtils.GetLayout(s_Console, m_PivotMessages);
		m_ConsoleRows = new FadeText[4];
		float fadeIn = 0.1f;
		float fadeOut = 2f;
		m_ConsoleRows[0] = new FadeText(m_ConsoleLayout.GetWidget("Row1").GetComponent<GUIBase_Label>(), null, fadeIn, fadeOut, 1f, Color.red);
		m_ConsoleRows[1] = new FadeText(m_ConsoleLayout.GetWidget("Row2").GetComponent<GUIBase_Label>(), null, fadeIn, fadeOut, 0.85f, Color.red);
		m_ConsoleRows[2] = new FadeText(m_ConsoleLayout.GetWidget("Row3").GetComponent<GUIBase_Label>(), null, fadeIn, fadeOut, 0.7f, Color.red);
		m_ConsoleRows[3] = new FadeText(m_ConsoleLayout.GetWidget("Row4").GetComponent<GUIBase_Label>(), null, fadeIn, fadeOut, 0.55f, Color.red);
		MFGuiManager.Instance.ShowLayout(m_ConsoleLayout, true);
		m_EffectLayout = GuiBaseUtils.GetLayout(s_Effects, m_PivotMessages);
		m_MessageEffects = new MessageEffect[4];
		m_MessageEffects[0] = new MessageEffect(m_EffectLayout.GetWidget("KillBck").GetComponent<GUIBase_Sprite>(), m_EffectLayout.GetWidget("KillText").GetComponent<GUIBase_Label>(), m_PivotMessages);
		m_MessageEffects[1] = new MessageEffect(m_EffectLayout.GetWidget("HeadshotBck").GetComponent<GUIBase_Sprite>(), m_EffectLayout.GetWidget("HeadshotText").GetComponent<GUIBase_Label>(), m_PivotMessages);
		m_MessageEffects[2] = new MessageEffect(m_EffectLayout.GetWidget("MoneyBck").GetComponent<GUIBase_Sprite>(), m_EffectLayout.GetWidget("MoneyText").GetComponent<GUIBase_Label>(), m_PivotMessages);
		m_MessageEffects[3] = new MessageEffect(m_EffectLayout.GetWidget("AmmoBck").GetComponent<GUIBase_Sprite>(), m_EffectLayout.GetWidget("AmmoText").GetComponent<GUIBase_Label>(), m_PivotMessages);
		MFGuiManager.Instance.ShowLayout(m_EffectLayout, true);
		m_ObjectiveLayout = GuiBaseUtils.GetLayout(s_Objective, m_PivotMessages);
		GUIBase_Sprite componentInChildren = m_ObjectiveLayout.GetComponentInChildren<GUIBase_Sprite>();
		GUIBase_Label childByName = GetChildByName<GUIBase_Label>(m_ObjectiveLayout.gameObject, "Text");
		GUIBase_Label childByName2 = GetChildByName<GUIBase_Label>(m_ObjectiveLayout.gameObject, "Text2");
		GUIBase_Label childByName3 = GetChildByName<GUIBase_Label>(m_ObjectiveLayout.gameObject, "SmallText");
		m_Objective = new FadeText(childByName, componentInChildren, 0.5f, 1f, 1f, Color.white);
		m_Objective2 = new FadeText(childByName2, null, 0.5f, 1f, 1f, Color.green);
		m_ObjectiveSmall = new FadeText(childByName3, null, 1f, 0f, 1f, Color.white);
		MFGuiManager.Instance.ShowLayout(m_ObjectiveLayout, true);
	}

	public override void Reset()
	{
	}

	public override void LateUpdate(float deltaTime)
	{
		base.LateUpdate(deltaTime);
		if (IsVisible())
		{
			m_Objective.Update(deltaTime);
			m_Objective2.Update(deltaTime);
			m_ObjectiveSmall.Update(deltaTime);

			for (int i = 0; i < m_ConsoleRows.Length; i++)
				m_ConsoleRows[i].Update(deltaTime);

			for (int i = 0; i < m_MessageEffects.Length; i++)
				m_MessageEffects[i].Update(deltaTime);
		}
	}

	protected override void ShowWidgets(bool on)
	{
		MFGuiManager.Instance.ShowLayout(m_ConsoleLayout, on);
		MFGuiManager.Instance.ShowLayout(m_ObjectiveLayout, on);
	}

	public void ShowMessage(GuiHUD.E_MessageType type, int messageID, bool dontDisappear, float time)
	{
		ShowMessage(type, TextDatabase.instance[messageID], dontDisappear, time);
	}

	public void ShowMessage(GuiHUD.E_MessageType type, string message, bool dontDisappear, float time)
	{
		switch (type)
		{
		case GuiHUD.E_MessageType.Objective:
			if (time <= 0f)
			{
				m_Objective.ShowMessage(message, 5f, 0f);
				if (dontDisappear)
				{
					m_ObjectiveSmall.ShowMessage(message, 0f, 7f);
				}
			}
			else
			{
				m_Objective.ShowMessage(message, time, 0f);
				if (dontDisappear)
				{
					m_ObjectiveSmall.ShowMessage(message, 0f, time + 2f);
				}
			}
			break;
		case GuiHUD.E_MessageType.SecondObjective:
			if (time <= 0f)
			{
				m_Objective2.ShowMessage(message, 5f, 0f);
			}
			else
			{
				m_Objective2.ShowMessage(message, time, 0f);
			}
			break;
		case GuiHUD.E_MessageType.Information:
			if (time <= 0f)
			{
				m_Objective.ShowMessage(message, 5f, 0f);
			}
			else
			{
				m_Objective.ShowMessage(message, time, 0f);
			}
			break;
		case GuiHUD.E_MessageType.Console:
		{
			float time2 = ((!(time <= 0f)) ? time : 4f);
			for (int num = m_ConsoleRows.Length - 1; num > 0; num--)
			{
				m_ConsoleRows[num].CopyFrom(m_ConsoleRows[num - 1]);
			}
			m_ConsoleRows[0].ShowMessage(message, time2, 0f);
			break;
		}
		}
	}

	public void ShowMessageEffect(GuiHUD.E_MessageEffect type, string text)
	{
		m_MessageEffects[(int)type].Show(text);
	}

	public void HideAllMessages()
	{
		if ((bool)m_ConsoleLayout)
		{
			MFGuiManager.Instance.ShowLayout(m_ConsoleLayout, false);
		}
		if ((bool)m_PivotMessages)
		{
			MFGuiManager.Instance.ShowPivot(m_PivotMessages, false);
		}
	}

	private T GetChildByName<T>(GameObject obj, string name) where T : Component
	{
		Transform transform = obj.transform.Find(name);
		return (!(transform != null)) ? ((T)null) : transform.GetComponent<T>();
	}
}
