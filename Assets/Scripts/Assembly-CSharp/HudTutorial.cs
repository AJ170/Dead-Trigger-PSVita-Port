using UnityEngine;

public class HudTutorial : HudComponent
{
	private class Tutorial
	{
		public GUIBase_Widget m_Widget;

		public bool m_Activated;
	}

	private const int TUTORIALS = 7;

	private Tutorial[] Tutorials;

	private string s_PivotMainName = "MainHUD";

	private string s_LayoutMainName = "Tutorial";

	private string s_TutorialName = "Tutorial";

	public override bool VisibleOnStart()
	{
		return true;
	}

	public override void Init()
	{
		GUIBase_Pivot pivot = MFGuiManager.Instance.GetPivot(s_PivotMainName);
		if (!pivot)
		{
			Debug.LogError("'" + s_PivotMainName + "' not found!!! Assert should come now");
			return;
		}
		GUIBase_Layout layout = pivot.GetLayout(s_LayoutMainName);
		if (!layout)
		{
			Debug.LogError("'" + s_LayoutMainName + "' not found!!! Assert should come now");
			return;
		}
		Tutorials = new Tutorial[7];
		for (int i = 0; i < Tutorials.Length; i++)
		{
			Tutorials[i] = new Tutorial();
			Tutorials[i].m_Widget = layout.GetWidget(s_TutorialName + (i + 1)).GetComponent<GUIBase_Widget>();
			Tutorials[i].m_Activated = false;
		}
	}

	public override void Reset()
	{
	}

	public override void LateUpdate(float deltaTime)
	{
		base.LateUpdate(deltaTime);
	}

	public void ShowTutorial(int index, bool show)
	{
#if UNITY_STANDALONE
		switch (index)
		{
			case 0: return;
			case 1: return;
			case 2: return;
			case 3: return;
		}
#endif

		if (!Game.Instance.IsXperiaPlay || !Game.Instance.KeypadSlided)
		{
			Tutorials[index].m_Activated = show;
			Tutorials[index].m_Widget.Show(show, true);
		}
	}

	public void HideTutorials()
	{
		Tutorial[] tutorials = Tutorials;
		foreach (Tutorial tutorial in tutorials)
		{
			tutorial.m_Activated = false;
			tutorial.m_Widget.Show(false, true);
		}
	}

	protected override void ShowWidgets(bool on)
	{
		Tutorial[] tutorials = Tutorials;
		foreach (Tutorial tutorial in tutorials)
		{
			tutorial.m_Widget.Show(on && tutorial.m_Activated, true);
		}
	}
}
