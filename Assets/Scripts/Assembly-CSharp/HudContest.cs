using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HudContest : HudComponent
{
    public class ContestElement
    {
        public enum State
        {
            None = 0,
            InProgress = 1,
            Success = 2,
            Fail = 3
        }

        public int TrueIndex;

        public GUIBase_Button Button;

        public GUIBase_Sprite Indicator;

        public GUIBase_Sprite SuccessIndicator;

        public GUIBase_Sprite FailIndicator;

        public GameObject Parent;

        public State ContestState;

        public float TouchTimeLimit;

        public float TimeFromStart;

        private bool[] visibility = new bool[4];

        public bool ShouldBePressed { get; set; }

        public float TimeLimit
        {
            get
            {
                return TouchTimeLimit;
            }
        }

        public bool Active { get; set; }
        public KeyCode AssignedKey;
        public void SaveVisibilityState()
        {
            visibility[0] = Button.Widget.IsVisible();
            visibility[1] = Indicator.Widget.IsVisible();
            visibility[2] = SuccessIndicator.Widget.IsVisible();
            visibility[3] = FailIndicator.Widget.IsVisible();
        }

        public void RestoreVisibilityState()
        {
            Button.Widget.Show(visibility[0], false);
            Indicator.Widget.Show(visibility[1], false);
            SuccessIndicator.Widget.Show(visibility[2], false);
            FailIndicator.Widget.Show(visibility[3], false);
        }
    }

    public enum ContestState
    {
        Inactive = 0,
        InProgress = 1,
        Success = 2,
        Fail = 3,
        Terminating = 4
    }

    private const float MaxScaleAdd = 1f;

    private const float IntervalBetweenButtons = 0.7f;

    private const float ShowTime = 1.3f;

    private const float ButtonFadeOut = 0.2f;

    private static Color SuccessColor = new Color(0.2f, 0.8f, 0.2f);

    private static Color FailColor = new Color(0.8f, 0.1f, 0.1f);

    private static Color DefaultColor = new Color(1f, 1f, 1f);

    private Camera m_ContestCamera;

    private float HudTimeFromStart;

    private GUIBase_Layout m_Main;

    private List<ContestElement> m_ContestResources = new List<ContestElement>();

    private List<ContestElement> m_Contest = new List<ContestElement>();

    private GuiHUD.ContestResultCallback m_Callback;

    private float TimeFromStart;

    private ContestState m_ContestState;

    private string s_PivotMainName = "MainHUD";

    private string s_LayoutMainName = "Contest";

    private string s_ContestButton1 = "ContestButton1";

    private string s_ContestButton2 = "ContestButton2";

    private string s_ContestButton3 = "ContestButton3";

    private string s_ContestButton4 = "ContestButton4";

    private string s_ContestButton5 = "ContestButton5";

    public override bool VisibleOnStart()
    {
        return true;
    }
    public bool IsContestActive()
    {
        return m_ContestState == ContestState.InProgress;
    }
    public List<ContestElement> GetActiveContestElements()
    {
        return m_Contest;
    }

    public override void Init()
    {
        HudTimeFromStart = 0f;
        GUIBase_Pivot pivot = MFGuiManager.Instance.GetPivot(s_PivotMainName);
        if (!pivot)
        {
            Debug.LogError("'" + s_PivotMainName + "' not found!!! Assert should come now");
            return;
        }
        m_Main = pivot.GetLayout(s_LayoutMainName);
        if (!m_Main)
        {
            Debug.LogError("'" + s_LayoutMainName + "' not found!!! Assert should come now");
            return;
        }
        for (int i = 0; i < 5; i++)
        {
            m_ContestResources.Add(new ContestElement());
        }
        m_ContestResources[0].Button = m_Main.GetWidget(s_ContestButton1).GetComponent<GUIBase_Button>();
        m_ContestResources[0].Indicator = m_Main.GetWidget("Indicator1").GetComponent<GUIBase_Sprite>();
        m_ContestResources[0].SuccessIndicator = m_Main.GetWidget("Indicator1Success").GetComponent<GUIBase_Sprite>();
        m_ContestResources[0].FailIndicator = m_Main.GetWidget("Indicator1Fail").GetComponent<GUIBase_Sprite>();
        m_ContestResources[0].Button.RegisterTouchDelegate(ContestButton1Delegate);
        m_ContestResources[0].TrueIndex = 1;
        m_ContestResources[1].Button = m_Main.GetWidget(s_ContestButton2).GetComponent<GUIBase_Button>();
        m_ContestResources[1].Indicator = m_Main.GetWidget("Indicator2").GetComponent<GUIBase_Sprite>();
        m_ContestResources[1].SuccessIndicator = m_Main.GetWidget("Indicator2Success").GetComponent<GUIBase_Sprite>();
        m_ContestResources[1].FailIndicator = m_Main.GetWidget("Indicator2Fail").GetComponent<GUIBase_Sprite>();
        m_ContestResources[1].Button.RegisterTouchDelegate(ContestButton2Delegate);
        m_ContestResources[1].TrueIndex = 2;
        m_ContestResources[2].Button = m_Main.GetWidget(s_ContestButton3).GetComponent<GUIBase_Button>();
        m_ContestResources[2].Indicator = m_Main.GetWidget("Indicator3").GetComponent<GUIBase_Sprite>();
        m_ContestResources[2].SuccessIndicator = m_Main.GetWidget("Indicator3Success").GetComponent<GUIBase_Sprite>();
        m_ContestResources[2].FailIndicator = m_Main.GetWidget("Indicator3Fail").GetComponent<GUIBase_Sprite>();
        m_ContestResources[2].Button.RegisterTouchDelegate(ContestButton3Delegate);
        m_ContestResources[2].TrueIndex = 3;
        m_ContestResources[3].Button = m_Main.GetWidget(s_ContestButton4).GetComponent<GUIBase_Button>();
        m_ContestResources[3].Indicator = m_Main.GetWidget("Indicator4").GetComponent<GUIBase_Sprite>();
        m_ContestResources[3].SuccessIndicator = m_Main.GetWidget("Indicator4Success").GetComponent<GUIBase_Sprite>();
        m_ContestResources[3].FailIndicator = m_Main.GetWidget("Indicator4Fail").GetComponent<GUIBase_Sprite>();
        m_ContestResources[3].Button.RegisterTouchDelegate(ContestButton4Delegate);
        m_ContestResources[3].TrueIndex = 4;
        m_ContestResources[4].Button = m_Main.GetWidget(s_ContestButton5).GetComponent<GUIBase_Button>();
        m_ContestResources[4].Indicator = m_Main.GetWidget("Indicator5").GetComponent<GUIBase_Sprite>();
        m_ContestResources[4].SuccessIndicator = m_Main.GetWidget("Indicator5Success").GetComponent<GUIBase_Sprite>();
        m_ContestResources[4].FailIndicator = m_Main.GetWidget("Indicator5Fail").GetComponent<GUIBase_Sprite>();
        m_ContestResources[4].Button.RegisterTouchDelegate(ContestButton5Delegate);
        m_ContestResources[4].TrueIndex = 5;
        GameCamera component = Camera.main.GetComponent<GameCamera>();
        if (component == null && Camera.main.transform.parent != null)
        {
            component = Camera.main.transform.parent.GetComponent<GameCamera>();
        }
        m_ContestCamera = component.CameraFPV;
    }

    public override void Reset()
    {
    }
    private static KeyCode[] ContestKeys = { KeyCode.E, KeyCode.R, KeyCode.F, KeyCode.Space, KeyCode.Q }; // Or pass this dynamically

    void Update()
    {
        if (m_ContestState != ContestState.InProgress) return;

        for (int i = 0; i < m_Contest.Count; i++)
        {
            var element = m_Contest[i];
            if (!element.Active || element.ContestState != ContestElement.State.InProgress || !element.ShouldBePressed)
                continue;

            if (Input.GetKeyDown(ContestKeys[i])) // or element.AssignedKey if you stored it
            {
                ContestButtonDelegate(element.TrueIndex);
            }
        }
    }
    public override void LateUpdate(float deltaTime)
    {
        base.LateUpdate(deltaTime);
        HudTimeFromStart += deltaTime;

        if (m_Contest.Count > 0)
            CheckContestButtonActivation();

        bool flag = false;

        for (int i = 0; i < m_Contest.Count; i++)
        {
            ContestElement item = m_Contest[i];

            if ((bool)item.Parent)
            {
                Vector3 position = m_ContestCamera
                    .WorldToScreenPoint(item.Parent.transform.position);
                position.z = 0f;
                position.y = (float)Screen.height - position.y;
                item.Button.Widget.transform.position = position;
            }

            if (item.Active)
            {
                HandleContest(item);
                flag = true;
            }
        }

        if (m_ContestState == ContestState.Terminating && !flag)
        {
            StopContestInternal();
        }
        else if (m_ContestState == ContestState.Success)
        {
            m_Callback(null, GuiHUD.ContestResult.Success);
            StopContest();
        }
        else if (m_ContestState == ContestState.Fail)
        {
            m_Callback(null, GuiHUD.ContestResult.Fail);
            StopContest();
        }
    }
    public void StartContest(GameObject[] objects, KeyCode[] keys, GuiHUD.ContestResultCallback callback)
    {
        m_Contest.Clear();
        m_Callback = callback;

        int num = 0;
        foreach (GameObject parent in objects)
        {
            if (num >= m_ContestResources.Count) break;

            ContestElement element = m_ContestResources[num];
            m_Contest.Add(element);
            element.Parent = parent;

            //  Assign key for input matching (recommended)
            if (num < keys.Length)
                element.AssignedKey = keys[num];
            var labelComponent = element.Button.keyLabel.GetComponent<GUIBase_Label>();
            if (labelComponent != null)
            {
                labelComponent.SetNewText(element.AssignedKey.ToString());
                element.Button.keyLabel.ShowImmediate(false, true);
            }

            //  Inject key label text into "Key" child
            Transform keyTransform = element.Button.Widget.transform.Find("Key");
            if (keyTransform != null)
            {
                Text keyLabel = keyTransform.GetComponent<Text>();
                if (keyLabel != null)
                {
                    Debug.Log("Setting keybind label: " + keys[num]);
                    keyLabel.text = keys[num].ToString(); // e.g. "E", "R", "F"
                    keyLabel.enabled = true;
                }
            }

            num++;
        }

        foreach (ContestElement item in m_Contest)
        {
            item.Active = false;
            item.TouchTimeLimit = 1.3f;
            item.ContestState = ContestElement.State.InProgress;
            item.TimeFromStart = 0f;
            item.ShouldBePressed = false;

            if (item.Button.Widget.IsVisible())
                item.Button.Widget.Show(false, true);

            item.Button.Widget.m_FadeAlpha = 1f;
            item.Indicator.Widget.m_FadeAlpha = 1f;
            item.FailIndicator.Widget.m_FadeAlpha = 1f;
            item.SuccessIndicator.Widget.m_FadeAlpha = 1f;
            item.Indicator.Widget.m_Color = DefaultColor;
        }

        m_Contest.Shuffle(); // randomize button order

        // ⏱️ Initialize the first prompt
        m_Contest[0].ShouldBePressed = true;
        m_Contest[0].TouchTimeLimit *= 1.6f;

        TimeFromStart = HudTimeFromStart;
        m_ContestState = ContestState.InProgress;
        CheckContestButtonActivation();
    }

    public void StopContest()
    {
        m_ContestState = ContestState.Terminating;
    }

    public void SimulateIngameAction()
    {
        if (m_ContestState != ContestState.InProgress)
        {
            return;
        }
        foreach (ContestElement item in m_Contest)
        {
            if (item.ShouldBePressed && item.Active && item.ContestState == ContestElement.State.InProgress)
            {
                ContestButtonDelegate(item.TrueIndex);
                break;
            }
        }
    }

    private void CheckContestButtonActivation()
    {
        if (m_ContestState != ContestState.InProgress)
            return;

        int num = Mathf.Clamp(
            Mathf.FloorToInt((HudTimeFromStart - TimeFromStart) / 0.7f),
            0,
            m_Contest.Count - 1
        );

        for (int i = 0; i <= num; i++)
        {
            var element = m_Contest[i];
            if (!element.Active && element.ContestState == ContestElement.State.InProgress)
            {
                element.Active = true;
                element.TimeFromStart = HudTimeFromStart;

                // Activate button
                if (!element.Button.Widget.IsVisible())
                    element.Button.Widget.Show(true, false);

                // Activate indicator
                if (!element.Indicator.Widget.IsVisible())
                    element.Indicator.Widget.Show(true, false);

                // ✅ Show key label only when button becomes active
                GUIBase_Label label = null;
                if (element.Button != null && element.Button.keyLabel != null)
                {
                    label = element.Button.keyLabel.GetComponent<GUIBase_Label>();
                }
                if (element.Button.keyLabel != null)
                {
                    element.Button.keyLabel.ShowImmediate(true, true);
                }
            }
        }
    }


    protected override void ShowWidgets(bool on)
    {
        if (!on && m_Main.IsVisible())
        {
            foreach (ContestElement item in m_Contest)
            {
                item.SaveVisibilityState();
            }
        }
        MFGuiManager.Instance.ShowLayout(m_Main, on);
        if (!on)
        {
            return;
        }
        foreach (ContestElement item2 in m_Contest)
        {
            item2.RestoreVisibilityState();
        }
    }

    private void ContestButton1Delegate()
    {
        ContestButtonDelegate(1);
    }

    private void ContestButton2Delegate()
    {
        ContestButtonDelegate(2);
    }

    private void ContestButton3Delegate()
    {
        ContestButtonDelegate(3);
    }

    private void ContestButton4Delegate()
    {
        ContestButtonDelegate(4);
    }

    private void ContestButton5Delegate()
    {
        ContestButtonDelegate(5);
    }

    public void ContestButtonDelegate(int buttonID)
    {
        ContestElement contestElement = null;
        int num = 0;
        for (int i = 0; i < m_Contest.Count; i++)
        {
            if (m_Contest[i].TrueIndex == buttonID)
            {
                contestElement = m_Contest[i];
                num = i;
                break;
            }
        }
        if (m_ContestState != ContestState.InProgress || !contestElement.Active || contestElement.ContestState != ContestElement.State.InProgress)
        {
            return;
        }
        if (!contestElement.ShouldBePressed)
        {
            m_ContestState = ContestState.Fail;
            contestElement.FailIndicator.Widget.Show(true, false);
            contestElement.Indicator.Widget.m_Color = FailColor;
            return;
        }
        contestElement.ContestState = ContestElement.State.Success;
        contestElement.SuccessIndicator.Widget.Show(true, false);
        contestElement.Indicator.Widget.m_Color = SuccessColor;
        m_Callback(contestElement.Parent, GuiHUD.ContestResult.PartialSucess);
        if (num + 1 == m_Contest.Count)
        {
            m_ContestState = ContestState.Success;
        }
        else
        {
            m_Contest[num + 1].ShouldBePressed = true;
        }
    }

    private void HandleContest(ContestElement element)
    {
        float num = HudTimeFromStart - element.TimeFromStart;
        float num2 = 1f;
        bool flag = false;
        if (m_ContestState == ContestState.Terminating)
        {
            if (num < element.TimeLimit)
            {
                element.TimeFromStart = HudTimeFromStart - element.TimeLimit;
            }
            num2 = 1f - Mathf.Clamp((HudTimeFromStart - element.TimeFromStart - element.TouchTimeLimit) / 0.2f, 0f, 1f);
            flag = true;
        }
        else if (num > element.TouchTimeLimit)
        {
            if (element.ContestState == ContestElement.State.InProgress)
            {
                element.ContestState = ContestElement.State.Fail;
                element.FailIndicator.Widget.Show(true, false);
                element.Indicator.Widget.m_Color = FailColor;
                m_ContestState = ContestState.Fail;
            }
            num2 = 1f - Mathf.Clamp((HudTimeFromStart - element.TimeFromStart - element.TouchTimeLimit) / 0.2f, 0f, 1f);
            flag = true;
        }
        else if (HudTimeFromStart - element.TimeFromStart <= element.TimeLimit)
        {
            float num3 = 1f;
            num3 = ((!(num > element.TimeLimit / 2f)) ? (num3 + 1f * Mathf.Sin((float)Math.PI / 2f * Mathf.Clamp(num * 2f / element.TimeLimit, 0f, 1f))) : (num3 + 1f * Mathf.Sin((float)Math.PI / 2f * (1f - Mathf.Clamp(num * 2f / element.TimeLimit - 1f, 0f, 1f)))));
            Vector3 localScale = new Vector3(num3, num3, num3);
            element.Indicator.Widget.transform.localScale = localScale;
        }
        else
        {
            element.Indicator.Widget.transform.localScale = Vector3.one;
        }
        element.Button.Widget.m_FadeAlpha = num2;
        element.Indicator.Widget.m_FadeAlpha = num2;
        element.FailIndicator.Widget.m_FadeAlpha = num2;
        element.SuccessIndicator.Widget.m_FadeAlpha = num2;
        if (Mathf.Approximately(num2, 0f) && flag)
        {
            element.Active = false;
        }
    }

    private void StopContestInternal()
    {
        m_ContestState = ContestState.Inactive;
        foreach (ContestElement item in m_Contest)
        {
            item.Button.Widget.Show(false, true);
        }
        m_Contest.Clear();
    }
}
