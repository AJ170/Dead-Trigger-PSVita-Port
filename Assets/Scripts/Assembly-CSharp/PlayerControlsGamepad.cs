using UnityEngine;

public class PlayerControlsGamepad
{
    public enum E_Input
    {
        Fire = 0,
        Reload = 1,
        Pause = 2,
        PrevWeapon = 3,
        NextWeapon = 4,
        Aim = 5,
        Item1 = 6,
        Item2 = 7,
        Item3 = 8,
        Item4 = 9,
        Action = 10,
        Axis_MoveRight = 11,
        Axis_MoveUp = 12,
        Axis_ViewRight = 13,
        Axis_ViewUp = 14,
        COUNT = 15
    }

    public bool Enabled = true;

    private PlayerControlStates States;

    private bool m_IronSightDown;

    private bool m_Gadget0Down;

    private bool m_Gadget1Down;

    private bool m_IngameActionDown;

    private bool m_StartDown;

    private bool m_DpadUp;

    private bool m_DpadDown;

    private bool m_DpadLeft;

    private bool m_DpadRight;

    private float ViewSensitivityX
    {
        get
        {
            return 1f * GuiOptions.sensitivity;
        }
    }

    private float ViewSensitivityY
    {
        get
        {
            return 1f * GuiOptions.sensitivity;
        }
    }

    public PlayerControlsGamepad(PlayerControlStates inStates)
    {
        States = inStates;
    }

    public static float ClampAngle(float angle, float min, float max)
    {
        angle %= 360f;
        if (angle >= -360f && angle <= 360f)
        {
            if (angle < -360f)
            {
                angle += 360f;
            }
            if (angle > 360f)
            {
                angle -= 360f;
            }
        }
        return Mathf.Clamp(angle, min, max);
    }

    private void UpdateMove()
    {
        Vector2 zero = Vector2.zero;
        if (Game.Instance.IsMogaConnected)
        {
            zero.x = Game.Instance.GetMogaGpad().getAxisValue(0);
            zero.y = 0f - Game.Instance.GetMogaGpad().getAxisValue(1);
        }
        else
        {
            zero.x = GetGpadAxis(E_Input.Axis_MoveRight);
            zero.y = GetGpadAxis(E_Input.Axis_MoveUp);
        }
        float num = Mathf.Clamp(zero.magnitude, 0f, 1f);
        if (num > 0.001f)
        {
            States.Move.Direction.x = zero.x;
            States.Move.Direction.z = zero.y;
            States.Move.Direction.Normalize();
            States._Temp.eulerAngles = new Vector3(0f, Player.Instance.Owner.Transform.rotation.eulerAngles.y, 0f);
            States.Move.Direction = States._Temp.TransformDirection(States.Move.Direction);
            States.Move.Force = num;
        }
    }

    private void UpdateView()
    {
        Vector2 zero = Vector2.zero;
        if (Game.Instance.IsMogaConnected)
        {
            zero.x = Game.Instance.GetMogaGpad().getAxisValue(11) * ViewSensitivityX * 0.5f;
            zero.y = Game.Instance.GetMogaGpad().getAxisValue(14) * ViewSensitivityY * 0.5f;
        }
        else
        {
            zero.x = GetGpadAxis(E_Input.Axis_ViewRight) * ViewSensitivityX;
            zero.y = GetGpadAxis(E_Input.Axis_ViewUp) * ViewSensitivityY;
            if (Game.Instance.NVidiaShiledCached)
            {
                zero.x *= 0.5f;
                zero.y *= 0.5f;
            }
        }
        float x = zero.x;
        float y = zero.y;
        if (Mathf.Abs(x) > 0.001f || Mathf.Abs(y) > 0.001f)
        {
            float num = 360f;
            float num2 = num * Time.deltaTime;
            float yaw = ClampAngle(x, 0f - num2, num2);
            float pitch = ClampAngle(y, 0f - num2, num2);
            States.View.SetNewRotation(yaw, pitch);
        }
    }

    private float GetGpadAxis(E_Input inp)
    {
        string axisName = custom_inputs.Instance.GetAxisName((int)inp);
        if (axisName != string.Empty)
        {
            if (inp == E_Input.Axis_ViewUp)
            {
                return 0f - Input.GetAxis(axisName);
            }
            return Input.GetAxis(axisName);
        }
        switch (inp)
        {
            case E_Input.Axis_MoveRight:
                return Input.GetAxis("HorizontalMove");
            case E_Input.Axis_MoveUp:
                return Input.GetAxis("VerticalMove");
            case E_Input.Axis_ViewRight:
                return Input.GetAxis("HorizontalView");
            case E_Input.Axis_ViewUp:
                return Input.GetAxis("VerticalView");
            default:
                return 0f;
        }
    }

    public static bool ButtonDown(E_Input inp)
    {
        return custom_inputs.Instance.isInputDown[(int)inp];
    }

    public static bool ButtonUp(E_Input inp)
    {
        return custom_inputs.Instance.isInputUp[(int)inp];
    }

    private void UpdateActions()
    {
        if (ButtonDown(E_Input.Fire))
        {
            if (Player.Instance.InUseMode)
            {
                States.UseDelegate();
            }
            else
            {
                States.FireDownDelegate();
            }
        }
        if (ButtonUp(E_Input.Fire) && !Player.Instance.InUseMode)
        {
            States.FireUpDelegate();
        }
        if (Input.GetKeyDown("joystick button 0") && Player.Instance.InUseMode)
        {
            States.UseDelegate();
        }
        if (ButtonDown(E_Input.Reload))
        {
            States.ReloadDelegate();
        }
        if (ButtonDown(E_Input.Pause) && Game.Instance.GameState != E_GameState.IngameMenu && !GuiHUD.Instance.IsHidden && MFGuiManager.Instance.FadeState == MFGuiManager.E_Fading.None && (bool)GuiIngameMenu.Instance)
        {
            GuiHUD.Instance.SwitchToIngameMenu();
        }
        if (ButtonDown(E_Input.PrevWeapon))
        {
            ChangeWeaponPrev();
        }
        else if (ButtonDown(E_Input.NextWeapon))
        {
            ChangeWeaponNext();
        }
        if (ButtonDown(E_Input.Aim))
        {
            States.IronSightDelegate();
        }
        if (ButtonDown(E_Input.Item1))
        {
            UseGadget(0);
        }
        else if (ButtonDown(E_Input.Item2))
        {
            UseGadget(1);
        }
        else if (ButtonDown(E_Input.Item3))
        {
            UseGadget(2);
        }
        else if (ButtonDown(E_Input.Item4))
        {
            UseGadget(3);
        }
    }

    private void UpdateActionsAlways()
    {
        if (ButtonDown(E_Input.Action))
        {
            GuiHUD.Instance.SimulateIngameAction();
        }
    }

    private void UseGadget(int index)
    {
        E_ItemID e_ItemID = GuiHUD.Instance.ItemID(index);
        if (e_ItemID != 0)
        {
            States.UseGadgetDelegate(e_ItemID);
        }
    }

    public void Update()
    {
        if (!Enabled || GuiHUD.Instance == null || (GuiHUD.Instance.IsHidden && !GuiHUD.Instance.IsHudGadgetsVisible()))
        {
            return;
        }
        if (States.Move.Enabled)
        {
            UpdateMove();
        }
        if (States.View.Enabled)
        {
            UpdateView();
        }
        if (States.ActionsEnabled || GuiHUD.Instance.IsHudTutorialVisible())
        {
            UpdateActions();
            if (Game.Instance.IsMogaConnected)
            {
                UpdateActionsMoga();
            }
            else
            {
                UpdateActionsBlueTooth();
            }
        }
        UpdateActionsAlways();
        if (Game.Instance.IsMogaConnected)
        {
            UpdateActionsAlwaysMoga();
        }
        else
        {
            UpdateActionsAlwaysBlueTooth();
        }
    }

    private void UpdateActionsBlueTooth()
    {
        if (Input.GetKeyDown("8"))
        {
            if (Player.Instance.InUseMode)
            {
                States.UseDelegate();
            }
            else
            {
                States.FireDownDelegate();
            }
        }
        if (Input.GetKeyUp("8") && !Player.Instance.InUseMode)
        {
            States.FireUpDelegate();
        }
        if (Input.GetKeyDown("7"))
        {
            States.ReloadDelegate();
        }
        if (Input.GetKeyDown("a"))
        {
            ChangeWeaponPrev();
        }
        else if (Input.GetKeyDown("d"))
        {
            ChangeWeaponNext();
        }
        if (Input.GetKeyDown("6"))
        {
            States.IronSightDelegate();
        }
        if (Input.GetKeyDown("3"))
        {
            UseGadget(0);
        }
        else if (Input.GetKeyDown("1"))
        {
            UseGadget(1);
        }
        else if (Input.GetKeyDown("2"))
        {
            UseGadget(2);
        }
        else if (Input.GetKeyDown("4"))
        {
            UseGadget(3);
        }
    }

    private void UpdateActionsAlwaysBlueTooth()
    {
        if (Input.GetKeyDown("s"))
        {
            GuiHUD.Instance.SimulateIngameAction();
        }
    }

    private void ChangeWeapon(E_WeaponID type)
    {
        if (CanChangeToWeapon(type) && (bool)GuiHUD.Instance && (bool)Player.Instance && (bool)Player.Instance.Owner && (bool)Player.Instance.Owner.WeaponComponent && Player.Instance.CanChangeWeapon())
        {
            States.ChangeWeaponDelegate(type);
            if ((bool)Player.Instance.Owner.WeaponComponent.GetWeapon(type))
            {
                GuiHUD.Instance.OnCurrentWeaponChanged(type);
            }
        }
    }

    private bool CanChangeToWeapon(E_WeaponID type)
    {
        ComponentWeapons weaponComponent = Player.Instance.Owner.WeaponComponent;
        WeaponBase weapon = weaponComponent.GetWeapon(type);
        if (weapon == null || (weapon.ClipAmmo <= 0 && weapon.WeaponAmmo <= 0) || weaponComponent.CurrentWeapon == type)
        {
            return false;
        }
        return true;
    }

    private void ChangeWeaponPrev()
    {
        if (!Player.Instance.CanChangeWeapon() || !Player.Instance.Owner.WeaponComponent || !Player.Instance.Owner.WeaponComponent.GetCurrentWeapon())
        {
            return;
        }
        E_WeaponID weaponID = Player.Instance.Owner.WeaponComponent.GetCurrentWeapon().WeaponID;
        int num = (int)weaponID;
        do
        {
            num = ((num > 0) ? (num - 1) : 25);
            if (CanChangeToWeapon((E_WeaponID)num))
            {
                ChangeWeapon((E_WeaponID)num);
                break;
            }
        }
        while (num != (int)weaponID);
    }

    private void ChangeWeaponNext()
    {
        if (!Player.Instance.CanChangeWeapon() || !Player.Instance.Owner.WeaponComponent || !Player.Instance.Owner.WeaponComponent.GetCurrentWeapon())
        {
            return;
        }
        E_WeaponID weaponID = Player.Instance.Owner.WeaponComponent.GetCurrentWeapon().WeaponID;
        int num = (int)weaponID;
        do
        {
            num = (num + 1) % 26;
            if (CanChangeToWeapon((E_WeaponID)num))
            {
                ChangeWeapon((E_WeaponID)num);
                break;
            }
        }
        while (num != (int)weaponID);
    }

    private void UpdateActionsMoga()
    {
        if (!Game.Instance.IsMogaConnected || Game.Instance.GetMogaGpad() == null)
        {
            return;
        }
        int keyCode = ((!Game.Instance.IsMogaPro) ? 103 : 105);
        if (Game.Instance.GetMogaGpad().getKeyCode(keyCode) == 0)
        {
            if (Player.Instance.InUseMode)
            {
                States.UseDelegate();
            }
            else
            {
                States.FireDownDelegate();
            }
        }
        if (Game.Instance.GetMogaGpad().getKeyCode(keyCode) == 1 && !Player.Instance.InUseMode)
        {
            States.FireUpDelegate();
        }
        if (Game.Instance.GetMogaGpad().getKeyCode(100) == 0 || (Game.Instance.IsMogaPro && Game.Instance.GetMogaGpad().getKeyCode(103) == 0))
        {
            States.ReloadDelegate();
        }
        if (Game.Instance.GetMogaGpad().getKeyCode(99) == 0)
        {
            ChangeWeaponNext();
        }
        else if (Game.Instance.GetMogaGpad().getKeyCode(99) != 1)
        {
        }
        if (Game.Instance.IsMogaPro)
        {
            if (Game.Instance.GetMogaGpad().getKeyCode(21) == 0 && !m_DpadLeft)
            {
                m_DpadLeft = true;
                ChangeWeaponPrev();
            }
            else if (Game.Instance.GetMogaGpad().getKeyCode(21) == 1)
            {
                m_DpadLeft = false;
            }
            if (Game.Instance.GetMogaGpad().getKeyCode(22) == 0 && !m_DpadRight)
            {
                m_DpadRight = true;
                ChangeWeaponNext();
            }
            else if (Game.Instance.GetMogaGpad().getKeyCode(22) == 1)
            {
                m_DpadRight = false;
            }
            if (Game.Instance.GetMogaGpad().getKeyCode(19) == 0 && !m_DpadUp)
            {
                m_DpadUp = true;
                GuiHUD.Instance.SelectPrevGadget();
            }
            else if (Game.Instance.GetMogaGpad().getKeyCode(19) == 1)
            {
                m_DpadUp = false;
            }
            if (Game.Instance.GetMogaGpad().getKeyCode(20) == 0 && !m_DpadDown)
            {
                m_DpadDown = true;
                GuiHUD.Instance.SelectNextGadget();
            }
            else if (Game.Instance.GetMogaGpad().getKeyCode(20) == 1)
            {
                m_DpadDown = false;
            }
        }
        int keyCode2 = ((!Game.Instance.IsMogaPro) ? 102 : 104);
        if (Game.Instance.GetMogaGpad().getKeyCode(keyCode2) == 0 && !m_IronSightDown)
        {
            m_IronSightDown = true;
            States.IronSightDelegate();
        }
        else if (Game.Instance.GetMogaGpad().getKeyCode(keyCode2) == 1)
        {
            m_IronSightDown = false;
        }
        if (Game.Instance.GetMogaGpad().getKeyCode(96) == 0 && !m_Gadget0Down)
        {
            int selectedGadget = GuiHUD.Instance.GetSelectedGadget();
            UseGadget(selectedGadget);
            m_Gadget0Down = true;
        }
        else if (Game.Instance.GetMogaGpad().getKeyCode(96) == 1)
        {
            m_Gadget0Down = false;
        }
        if (Game.Instance.GetMogaGpad().getKeyCode(97) == 0 && !m_Gadget1Down)
        {
            GuiHUD.Instance.SelectNextGadget();
            m_Gadget1Down = true;
        }
        else if (Game.Instance.GetMogaGpad().getKeyCode(97) == 1)
        {
            m_Gadget1Down = false;
        }
        if (Game.Instance.GetMogaGpad().getKeyCode(108) == 0 && !m_StartDown)
        {
            m_StartDown = true;
            if (Game.Instance.GameState != E_GameState.IngameMenu && !GuiHUD.Instance.IsHidden && MFGuiManager.Instance.FadeState == MFGuiManager.E_Fading.None)
            {
                GuiHUD.Instance.SwitchToIngameMenu();
            }
        }
        else if (Game.Instance.GetMogaGpad().getKeyCode(108) == 1)
        {
            m_StartDown = false;
        }
    }

    private void UpdateActionsAlwaysMoga()
    {
        if (Game.Instance.IsMogaConnected && Game.Instance.GetMogaGpad() != null)
        {
            if (Game.Instance.GetMogaGpad().getKeyCode(102) == 0 && !m_IngameActionDown)
            {
                m_IngameActionDown = true;
                GuiHUD.Instance.SimulateIngameAction();
            }
            else if (Game.Instance.GetMogaGpad().getKeyCode(102) == 1)
            {
                m_IngameActionDown = false;
            }
        }
    }
}
