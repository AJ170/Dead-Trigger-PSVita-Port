using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("GUI/Menu/GuiHUD")]
public class GuiHUD : MonoBehaviour
{
    public enum E_ActionButton
    {
        None = 0,
        Fire = 1,
        Use = 2
    }

    public enum E_MessageType
    {
        Objective = 0,
        Information = 1,
        Console = 2,
        SecondObjective = 3
    }

    public enum E_MessageEffect
    {
        Kill = 0,
        Headshot = 1,
        Money = 2,
        Ammo = 3
    }

    public enum E_RadarObjectType
    {
        CarryObjectSource = 0,
        CarryObjectTarget = 1,
        ProtectObject = 2,
        Target = 3
    }

    public enum ContestResult
    {
        Success = 0,
        Fail = 1,
        PartialSucess = 2
    }

    public delegate void ContestResultCallback(GameObject contestObject, ContestResult result);

    public delegate void IngameBuyCallback(bool buyPressed);

    public static GuiHUD Instance;

    public bool Initialised;

    public float Last100msUpdate;

    public float Last200msUpdate;

    private Mission m_Mission;

    private bool m_PlayingCutscene;

    private static GUIBase_Pivot m_PivotMain;

    private static GUIBase_Layout m_LayoutMain;

    private static string s_PivotMainName = "MainHUD";

    private static string s_LayoutMainName = "HUD_Layout";

    private static bool Disabled;

    private static bool s_VirtualJoystickEnabled = true;

    private static HudCrosshair hudCrosshair;

    private static HudRadar hudRadar;

    private static HudWeaponSelector hudWeaponSelector;

    private static HudMoveControl hudMoveControl;

    private static HudActions hudActions;

    private static HudPause hudPause;

    private static HudMessages hudMessages;

    private static HudIndicators hudIndicators;

    private static HudContest hudContest;

    private static HudGadgets hudGadgets;

    private static HudCombatInfo hudCombatInfo;

    private static HudArena hudArena;

    private static HudIngameBuy hudIngameBuy;

    private static HudTutorial hudTutorial;

    private HudComponent[] m_HudComponents;

    private static int deadCount;

    public bool IsHidden { get; private set; }

    public E_WeaponID WeaponID(int index)
    {
        return hudWeaponSelector.WeaponID(index);
    }

    public E_ItemID ItemID(int index)
    {
        return hudGadgets.GetGadgetID(index);
    }

    private void Awake()
    {
        Instance = this;
        hudCrosshair = new HudCrosshair();
        hudRadar = new HudRadar();
        hudWeaponSelector = new HudWeaponSelector();
        hudMoveControl = new HudMoveControl();
        hudActions = new HudActions();
        hudPause = new HudPause();
        hudMessages = new HudMessages();
        hudIndicators = new HudIndicators();
        hudContest = new HudContest();
        hudGadgets = new HudGadgets();
        hudCombatInfo = new HudCombatInfo();
        hudArena = new HudArena();
        hudIngameBuy = new HudIngameBuy();
        hudTutorial = new HudTutorial();
        m_HudComponents = new HudComponent[14]
        {
            hudCrosshair, hudRadar, hudWeaponSelector, hudMoveControl, hudActions, hudPause, hudMessages, hudIndicators, hudContest, hudGadgets,
            hudCombatInfo, hudArena, hudIngameBuy, hudTutorial
        };
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        CancelInvoke();
        HudComponent[] hudComponents = m_HudComponents;
        foreach (HudComponent hudComponent in hudComponents)
        {
            hudComponent.OnDestroy();
        }
        hudCrosshair = null;
        hudRadar = null;
        hudWeaponSelector = null;
        hudMoveControl = null;
        hudActions = null;
        hudPause = null;
        hudMessages = null;
        hudIndicators = null;
        hudContest = null;
        hudGadgets = null;
        hudCombatInfo = null;
        hudArena = null;
        hudIngameBuy = null;
        hudTutorial = null;
        m_HudComponents = null;
        Instance = null;
    }

    public void Init(Mission mission)
    {
        if (Disabled)
        {
            return;
        }
        m_Mission = mission;
        IsHidden = true;
        Initialised = true;
        m_PivotMain = MFGuiManager.Instance.GetPivot(s_PivotMainName);
        if (!m_PivotMain)
        {
            Debug.LogError("'" + s_PivotMainName + "' not found!!! Assert should come now");
            return;
        }
        m_LayoutMain = m_PivotMain.GetLayout(s_LayoutMainName);
        if (!m_LayoutMain)
        {
            Debug.LogError("'" + s_LayoutMainName + "' not found!!! Assert should come now");
            return;
        }
        MFGuiManager.Instance.ShowPivot(m_PivotMain, true);
        HudComponent[] hudComponents = m_HudComponents;
        foreach (HudComponent hudComponent in hudComponents)
        {
            hudComponent.Init();
        }

#if UNITY_STANDALONE
        // Creating new list, so, there is no interaction with disabled ones
        m_HudComponents = new HudComponent[11]
        {
            hudCrosshair, hudRadar, hudWeaponSelector, hudMessages, hudIndicators, hudContest, hudGadgets,
            hudCombatInfo, hudArena, hudIngameBuy, hudTutorial
        };
        hudPause.Show(false);
        hudActions.Show(false);
        hudMoveControl.Show(false);
#endif
        SetDefaultVisibility();
        StoreControlsOrigPositions();
        UpdateControlsPosition();
    }

    private void SetDefaultVisibility()
    {
        HudComponent[] hudComponents = m_HudComponents;
        foreach (HudComponent hudComponent in hudComponents)
        {
            if (hudComponent.VisibleOnStart())
            {
                hudComponent.Show(true);
            }
        }
    }

    public bool FingerIdInUse(int fingerId)
    {
        if (Disabled)
        {
            return false;
        }
        if (m_LayoutMain.FingerIdInUse(fingerId))
        {
            return true;
        }
        return hudWeaponSelector.FingerIdInUse(fingerId);
    }

    public void ReleaseFinger(Touch touch)
    {
        m_LayoutMain.ReleaseFinger(touch);
    }

    public void SetWeapons(List<E_WeaponID> weapons)
    {
        if (!Disabled)
        {
            hudWeaponSelector.SetWeapons(weapons);
        }
    }

    public void SetGadgets(List<E_ItemID> gadgets)
    {
        if (!Disabled)
        {
            hudGadgets.SetGadgets(gadgets);
        }
    }

    public void SelectNextGadget()
    {
        if (!Disabled)
        {
            hudGadgets.SelectNext();
        }
    }

    public void SelectPrevGadget()
    {
        if (!Disabled)
        {
            hudGadgets.SelectPrev();
        }
    }

    public int GetSelectedGadget()
    {
        if (Disabled)
        {
            return -1;
        }
        return hudGadgets.GetSelected();
    }

    private void StoreControlsOrigPositions()
    {
        if (!Disabled)
        {
            HudComponent[] hudComponents = m_HudComponents;
            foreach (HudComponent hudComponent in hudComponents)
            {
                hudComponent.StoreControlsOrigPositions();
            }
            if (GuiOptions.leftHandControlsNeedUpdate)
            {
                GuiOptions.SwitchLeftHandAimingControls();
            }
        }
    }

    public void Hide()
    {
        if (!Disabled)
        {
            IsHidden = true;
            HudComponent[] hudComponents = m_HudComponents;
            foreach (HudComponent hudComponent in hudComponents)
            {
                hudComponent.Enable(HudComponent.EnableLayer.IngameMenu, false);
            }
        }
    }

    public void Show()
    {
        if (!Disabled && !m_PlayingCutscene)
        {
            Player.Instance.Controls.TouchControls.OnControlSchemeChange();
            Player.Instance.UpdateUseModeHACK();
            HudComponent[] hudComponents = m_HudComponents;
            foreach (HudComponent hudComponent in hudComponents)
            {
                hudComponent.Enable(HudComponent.EnableLayer.IngameMenu, true);
            }
            IsHidden = false;
        }
    }

    private void LateUpdate()
    {
        if (Disabled || IsHidden || !Initialised)
        {
            return;
        }
        bool flag = Last100msUpdate < 1E-05f || TimeManager.Instance.timeSinceLevelLoad - Last100msUpdate > 0.1f;
        bool flag2 = Last100msUpdate < 1E-05f || TimeManager.Instance.timeSinceLevelLoad - Last200msUpdate > 0.2f;
        HudComponent[] hudComponents = m_HudComponents;
        foreach (HudComponent hudComponent in hudComponents)
        {
            if (!hudComponent.IsEnabled())
            {
                continue;
            }
            hudComponent.LateUpdate(TimeManager.Instance.GetRealDeltaTime());
            if (flag)
            {
                hudComponent.LateUpdate100ms();
                float num = TimeManager.Instance.timeSinceLevelLoad - Last100msUpdate - 0.1f;
                if (num > 0.1f || num < 0f)
                {
                    Last100msUpdate = TimeManager.Instance.timeSinceLevelLoad;
                }
                else
                {
                    Last100msUpdate = TimeManager.Instance.timeSinceLevelLoad - num;
                }
            }
            if (flag2)
            {
                hudComponent.LateUpdate200ms();
                float num2 = TimeManager.Instance.timeSinceLevelLoad - Last200msUpdate - 0.2f;
                if (num2 > 0.2f || num2 < 0f)
                {
                    Last200msUpdate = TimeManager.Instance.timeSinceLevelLoad;
                }
                else
                {
                    Last200msUpdate = TimeManager.Instance.timeSinceLevelLoad - num2;
                }
            }
        }
        if (Game.Instance.KeypadSlided || (NoTouchForSec(10f) && Game.Instance.IsGamepadConnectedCached()) || Game.Instance.IsMogaConnected)
        {
            EnableActionControls(false);
        }
        else
        {
            EnableActionControls(true);
        }
    }

    private bool NoTouchForSec(float inactivityTime)
    {
        if ((bool)Player.Instance)
        {
            return Player.Instance.Controls.TouchControls.LastTouchControlTime + inactivityTime < Time.timeSinceLevelLoad;
        }
        return false;
    }

    private void EnableActionControls(bool on)
    {
        hudMoveControl.Enable(HudComponent.EnableLayer.Controls, on);
        hudActions.Enable(HudComponent.EnableLayer.Controls, on);
        hudPause.Enable(HudComponent.EnableLayer.Controls, on);
    }

    public void EnableControlsTutorial(bool on)
    {
        hudActions.Enable(HudComponent.EnableLayer.Tutorial, on);
        hudPause.Enable(HudComponent.EnableLayer.Tutorial, on);
        hudRadar.Enable(HudComponent.EnableLayer.Tutorial, on);
        hudCrosshair.Enable(HudComponent.EnableLayer.Tutorial, on);
    }

    public void OnCurrentWeaponChanged(E_WeaponID Weapon)
    {
        if (!Disabled)
        {
            hudWeaponSelector.OnCurrentWeaponChanged(Weapon);
        }
    }

    public void JoystickBaseShow(Vector2 center)
    {
        if (!Disabled && s_VirtualJoystickEnabled)
        {
            hudMoveControl.JoystickBaseShow(center);
        }
    }

    public void JoystickBaseHide()
    {
        if (!Disabled && s_VirtualJoystickEnabled)
        {
            hudMoveControl.JoystickBaseHide();
        }
    }
    private void Update()
    {
        if (Disabled || IsHidden || !Initialised || hudContest == null)
            return;

        // Contest must be running to listen for keys
        if (!hudContest.IsContestActive())
            return;

        foreach (var element in hudContest.GetActiveContestElements())
        {
            if (!element.Active || element.ContestState != HudContest.ContestElement.State.InProgress || !element.ShouldBePressed)
                continue;

            if (Input.GetKeyDown(element.AssignedKey))
            {
                Debug.Log("Key pressed: " + element.AssignedKey);
                hudContest.ContestButtonDelegate(element.TrueIndex);
            }
        }
    }

    public void JoystickDown(Vector2 center)
    {
        if (!Disabled && s_VirtualJoystickEnabled)
        {
            hudMoveControl.JoystickDown(center);
        }
    }

    public void JoystickUpdate(Vector2 center)
    {
        if (!Disabled && s_VirtualJoystickEnabled)
        {
            hudMoveControl.JoystickUpdate(center);
        }
    }

    public void JoystickUp()
    {
        if (!Disabled && s_VirtualJoystickEnabled)
        {
            hudMoveControl.JoystickUp();
        }
    }

    public void SwitchToIngameMenu()
    {
        if (!(m_Mission == null) && Game.Instance.GameState != E_GameState.IngameMenu && MFGuiManager.Instance.FadeState == MFGuiManager.E_Fading.None)
        {
            m_Mission.ChangeGuiState(Mission.E_GuiState.E_GS_INGAME_MENU);
            GuiSubtitles.ShowAllRunning(false);
            GuiSubtitlesRenderer.Suspend(true);
            Game.Instance.GameState = E_GameState.IngameMenu;
            AudioListener.pause = true;
            TimeManager.Instance.PauseTime();
        }
    }

    public void SwitchBackFromIngameMenu()
    {
        Screen.lockCursor = true;
        GuiSubtitles.ShowAllRunning(true);
        m_Mission.ChangeGuiState(Mission.E_GuiState.E_GS_HUD);
        GuiSubtitlesRenderer.Suspend(false);
        Game.Instance.GameState = E_GameState.Game;
        TimeManager.Instance.UnpauseTime();
        AudioListener.pause = false;
    }

    public void SwitchToFirstHelp()
    {
        if (!(m_Mission == null) && Game.Instance.GameState != E_GameState.IngameMenu)
        {
            m_Mission.ChangeGuiState(Mission.E_GuiState.E_GS_FIRST_HELP);
            GuiSubtitles.ShowAllRunning(false);
            GuiSubtitlesRenderer.Suspend(true);
            Game.Instance.GameState = E_GameState.IngameMenu;
            AudioListener.pause = true;
            TimeManager.Instance.PauseTime();
        }
    }

    public void SwitchBackFromFirstHelp()
    {
        Screen.lockCursor = true;
        GuiSubtitles.ShowAllRunning(true);
        m_Mission.ChangeGuiState(Mission.E_GuiState.E_GS_HUD);
        GuiSubtitlesRenderer.Suspend(false);
        Game.Instance.GameState = E_GameState.Game;
        TimeManager.Instance.UnpauseTime();
        AudioListener.pause = false;
    }

    public void UpdateControlsPosition()
    {
        if (!Disabled)
        {
            HudComponent[] hudComponents = m_HudComponents;
            foreach (HudComponent hudComponent in hudComponents)
            {
                hudComponent.UpdateControlsPosition();
            }
        }
    }

    public void HideAllMessages()
    {
        if (!Disabled)
        {
            hudMessages.HideAllMessages();
        }
    }

    public void HideWeaponControls()
    {
        if (!Disabled)
        {
            hudActions.Show(false);
            hudWeaponSelector.Show(false);
            hudCrosshair.Show(false);
        }
    }

    public void ShowWeaponControls()
    {
        if (!Disabled)
        {
            hudActions.Show(true);
            hudCrosshair.Show(true);
            hudWeaponSelector.Show(true);
        }
    }

    public void PlayCutsceneBegin()
    {
        m_PlayingCutscene = true;
    }

    public void PlayCutsceneEnd()
    {
        m_PlayingCutscene = false;
    }

    public void Reset()
    {
        if (!Disabled)
        {
            m_PlayingCutscene = false;
            SetDefaultVisibility();
            HudComponent[] hudComponents = m_HudComponents;
            foreach (HudComponent hudComponent in hudComponents)
            {
                hudComponent.Reset();
            }
        }
    }

    public void HandleAction(AgentAction a)
    {
        if (!Disabled)
        {
            HudComponent[] hudComponents = m_HudComponents;
            foreach (HudComponent hudComponent in hudComponents)
            {
                hudComponent.HandleAction(a);
            }
        }
    }

    public void ShowAchievementNotify(string text)
    {
        hudCombatInfo.ShowAchievement(text);
    }

    public void OnAgentHit(AgentHuman agent, E_BodyPart bodyPart, bool bodyPartRemoved)
    {
        hudCrosshair.TargetHit();
        hudCombatInfo.ShowHit();
        if (bodyPartRemoved)
        {
            switch (bodyPart)
            {
                case E_BodyPart.Body:
                    hudCombatInfo.ShowInfo(HudCombatInfo.E_MessageType.Body, 2f);
                    break;
                case E_BodyPart.Head:
                    hudCombatInfo.ShowInfo(HudCombatInfo.E_MessageType.Head, 2f);
                    break;
                case E_BodyPart.LeftArm:
                case E_BodyPart.RightArm:
                    hudCombatInfo.ShowInfo(HudCombatInfo.E_MessageType.Arm, 4f);
                    break;
                case E_BodyPart.LeftLeg:
                case E_BodyPart.RightLeg:
                    hudCombatInfo.ShowInfo(HudCombatInfo.E_MessageType.Leg, 4f);
                    break;
            }
        }
    }

    public void OnAgentDeath(AgentHuman agent, E_BodyPart bodyPart, bool bodyPartRemoved)
    {
        if (Disabled)
        {
            return;
        }
        hudCrosshair.TargetHit();
        hudCombatInfo.ShowHit();
        if (bodyPartRemoved)
        {
            switch (bodyPart)
            {
                case E_BodyPart.Body:
                    hudCombatInfo.ShowInfo(HudCombatInfo.E_MessageType.Body, 1f + Random.Range(0.5f, 1.5f));
                    break;
                case E_BodyPart.Head:
                    hudCombatInfo.ShowInfo(HudCombatInfo.E_MessageType.Head, 1f + Random.Range(0.5f, 1.5f));
                    break;
                case E_BodyPart.LeftArm:
                case E_BodyPart.RightArm:
                    hudCombatInfo.ShowInfo(HudCombatInfo.E_MessageType.Arm, 4f);
                    break;
                case E_BodyPart.LeftLeg:
                case E_BodyPart.RightLeg:
                    hudCombatInfo.ShowInfo(HudCombatInfo.E_MessageType.Leg, 4f);
                    break;
            }
        }
        deadCount++;
    }

    public void OnAmmoCollected(E_WeaponID weapon, int ammo)
    {
        if (!Disabled)
        {
        }
    }

    public void ShowMessage(E_MessageType type, int messageID, bool dontDisappear = false, float time = 0f)
    {
        if (!Disabled)
        {
            hudMessages.ShowMessage(type, messageID, dontDisappear, time);
        }
    }

    public void ShowMessage(E_MessageType type, string message, bool dontDisappear = false, float time = 0f)
    {
        if (!Disabled)
        {
            hudMessages.ShowMessage(type, message, dontDisappear, time);
        }
    }

    public void ShowMessageEffect(E_MessageEffect type, string text)
    {
        if (!Disabled)
        {
        }
    }

    public void RegisterRadarObject(GameObject obj, E_RadarObjectType objType)
    {
        hudRadar.RegisterObject(obj, objType);
    }

    public void HighlightRadarObject(GameObject obj, bool highlight)
    {
        hudRadar.HighlightObject(obj, highlight);
    }

    public void UnregisterRadarObject(GameObject obj, E_RadarObjectType objType)
    {
        hudRadar.UnregisterObject(obj, objType);
    }

    public void SetMissionType(E_MissionType missionType)
    {
        if (!Disabled)
        {
            hudIndicators.SetMissionType(missionType);
        }
    }

    public void SetArenaScore(int score)
    {
        if (!Disabled)
        {
            hudArena.SetScore(score);
        }
    }

    public void ShowArenaScore(bool show)
    {
        if (!Disabled)
        {
            hudArena.ShowArena(show);
        }
    }

    public void ShowIngameBuy(IngameBuy ingameBuy, IngameBuyCallback closeCallback)
    {
        hudIngameBuy.ShowIngameBuy(ingameBuy, closeCallback);
    }

    public void SimulateIngameAction()
    {
        hudIngameBuy.SimulateIngameBuyAccept();
        hudContest.SimulateIngameAction();
    }

    public void HideIngameBuy()
    {
        hudIngameBuy.HideIngameBuy();
    }

    public void SetCounter(int counter)
    {
        if (!Disabled)
        {
            hudIndicators.SetCounter(counter);
        }
    }

    public void ShowCounter(bool show)
    {
        if (!Disabled)
        {
            hudIndicators.ShowCounter(show);
        }
    }

    public void SetCounterTime(float seconds)
    {
        if (!Disabled)
        {
            hudIndicators.SetCounterTime(seconds);
        }
    }

    public void ShowCarryObjectIcon(bool show)
    {
        if (!Disabled)
        {
            hudIndicators.ShowCarryObjectIcon(show);
        }
    }

    public void RegisterObjectHP(GameObject obj)
    {
        hudIndicators.RegisterObjectHP(obj);
    }

    public void UnregisterObjectHP(GameObject obj)
    {
        hudIndicators.UnregisterObjectHP(obj);
    }

    public void SetObjectHP(GameObject obj, float HP, bool highlight)
    {
        hudIndicators.SetObjectHP(obj, HP, highlight);
    }

    public void ShowRepairIndicator(GameObject obj, bool show)
    {
        hudIndicators.ShowRepairIndicator(obj, show);
    }

    public void ShowTutorial(int index, bool show)
    {
        hudTutorial.ShowTutorial(index, show);
        switch (index)
        {
            case 4:
                hudGadgets.StartHighlight(0);
                break;
            case 3:
                hudPause.StartHighlight();
                break;
        }
    }

    public bool IsHudGadgetsVisible()
    {
        return hudGadgets.IsVisible();
    }

    public bool IsHudTutorialVisible()
    {
        return hudTutorial.IsVisible();
    }

    public void HideTutorials()
    {
        hudTutorial.HideTutorials();
        hudGadgets.StopHighlight();
        hudPause.StopHighlight();
    }

    public void StartContest(GameObject[] objects, KeyCode[] keys, ContestResultCallback callback)
    {
        hudContest.StartContest(objects, keys, callback);
        foreach (HudComponent hudComponent in m_HudComponents)
        {
            if (hudComponent != hudContest)
                hudComponent.Enable(HudComponent.EnableLayer.Contest, false);
        }
    }


    public void StopContest()
    {
        hudContest.StopContest();
        HudComponent[] hudComponents = m_HudComponents;
        foreach (HudComponent hudComponent in hudComponents)
        {
            if (hudComponent != hudContest)
            {
                hudComponent.Enable(HudComponent.EnableLayer.Contest, true);
            }
        }
    }

    public static IEnumerator HighlightObject(GUIBase_Widget sprite)
    {
        while (true)
        {
            sprite.m_FadeAlpha = 0.4f;
            yield return new WaitForSeconds(0.3f);
            sprite.m_FadeAlpha = 1f;
            yield return new WaitForSeconds(0.3f);
        }
    }

    public void OnMissionReset()
    {
        if (!Disabled)
        {
        }
    }
}
