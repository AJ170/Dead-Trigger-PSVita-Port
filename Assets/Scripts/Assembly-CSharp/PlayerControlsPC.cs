using UnityEngine;

public class PlayerControlsPC
{
    public class MouseCameraControl
    {
        private float sensitivityX = 2f;

        private float sensitivityY = 1.85f;

        public bool Changed;

        public float OutYaw;

        public float OutPitch;

        private bool CursorLocked
        {
            get
            {
                return Screen.lockCursor;
            }
            set
            {
                Screen.lockCursor = value;
            }
        }

        public void SwitchCursor()
        {
            CursorLocked = !CursorLocked;
        }

        private float GetSensitivityX()
        {
            return sensitivityX * GuiOptions.sensitivity;
        }

        private float GetSensitivityY()
        {
            return sensitivityY * GuiOptions.sensitivity;
        }

        public void Update()
        {
            OutYaw = 0f;
            OutPitch = 0f;
            if (CursorLocked)
            {
                float num = Input.GetAxis("MouseX") * GetSensitivityX();
                float num2 = Input.GetAxis("MouseY") * GetSensitivityY();
                Changed = Mathf.Abs(num) > 0.001f || Mathf.Abs(num2) > 0.001f;
                if (Changed)
                {
                    float angle = num;
                    float angle2 = 0f - num2;
                    OutYaw = ClampAngle(angle, -160f, 160f);
                    OutPitch = ClampAngle(angle2, -160f, 160f);
                }
            }
        }
    }

    public class GamepadCameraControl
    {
        private float sensitivityX = 2f;

        private float sensitivityY = 1.5f;

        public float OutYaw;

        public float OutPitch;

        public bool Changed;

        private float GetSensitivityX()
        {
            return sensitivityX * GuiOptions.sensitivity;
        }

        private float GetSensitivityY()
        {
            return sensitivityY * GuiOptions.sensitivity;
        }

        public void Update()
        {
            float num = Input.GetAxis("HorizontalView") * GetSensitivityX();
            float num2 = Input.GetAxis("VerticalView") * GetSensitivityY();
            Changed = Mathf.Abs(num) > 0.001f || Mathf.Abs(num2) > 0.001f;
            float num3 = 360f;
            float num4 = num3 * Time.deltaTime;
            OutYaw = ClampAngle(num, 0f - num4, num4);
            OutPitch = ClampAngle(num2, 0f - num4, num4);
        }
    }

    private bool Enabled = true;

    private PlayerControlStates States;

    public MouseCameraControl MouseCameraCtrl = new MouseCameraControl();

    public GamepadCameraControl GpadCameraCtrl = new GamepadCameraControl();

    private bool lockCursorAfterStart = true;

    private Vector2 prevMousePos = new Vector2(0f, 0f);

    public PlayerControlsPC(PlayerControlStates inStates)
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

    private void UpdateMouseInteractionTouch()
    {
        if (Input.GetMouseButtonDown(0) && !Screen.lockCursor)
        {
            Vector2 position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            InteractionObject interactionObject = PlayerControlsTouch.TouchedInteractionIcon(position);
            if (interactionObject != null)
            {
                States.UseObjectDelegate(interactionObject);
            }
        }
        if (Input.GetMouseButton(0) || (Input.GetMouseButton(1) && !Screen.lockCursor))
        {
            Vector2 vector = Input.mousePosition;
            States.TouchUpdateDelegate(vector, vector - prevMousePos);
        }
    }

    public void Update()
    {
        if (!Enabled)
        {
            return;
        }
        if (Time.timeSinceLevelLoad > 0f && lockCursorAfterStart)
        {
            Screen.lockCursor = true;
            lockCursorAfterStart = false;
        }
        if (Input.GetKeyDown("return"))
        {
            MouseCameraCtrl.SwitchCursor();
        }
        UpdateMouseInteractionTouch();
        Vector2 vector = default(Vector2);
        vector.x = Input.GetAxis("HorizontalMovePC");
        vector.y = Input.GetAxis("VerticalMovePC");
        float num = Mathf.Clamp(vector.magnitude, 0f, 1f);
        if (num > 0.001f)
        {
            States.Move.Direction.x = vector.x;
            States.Move.Direction.z = vector.y;
            States.Move.Direction.Normalize();
            States._Temp.eulerAngles = new Vector3(0f, Player.Instance.Owner.Transform.rotation.eulerAngles.y, 0f);
            States.Move.Direction = States._Temp.TransformDirection(States.Move.Direction);
            States.Move.Force = num;
        }
        MouseCameraCtrl.Update();
        GpadCameraCtrl.Update();
        if (MouseCameraCtrl.Changed)
        {
            States.View.SetNewRotation(MouseCameraCtrl.OutYaw, MouseCameraCtrl.OutPitch);
        }
        else
        {
            States.View.ZeroInput(true);
        }
        if (GpadCameraCtrl.Changed)
        {
            States.View.SetNewRotation(GpadCameraCtrl.OutYaw, GpadCameraCtrl.OutPitch);
        }
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.RightControl))
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
        if ((Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.RightControl)) && !Player.Instance.InUseMode)
        {
            States.FireUpDelegate();
        }
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.RightAlt))
        {
            States.IronSightDelegate();
        }
        if (Input.GetKeyDown("r"))
        {
            States.ReloadDelegate();
        }
        if (Input.GetKeyDown("b") || Input.GetKeyDown("x"))
        {
            GuiHUD.Instance.SimulateIngameAction();
        }
        if (Input.GetKeyDown("escape") && Game.Instance.GameState != E_GameState.IngameMenu && !GuiHUD.Instance.IsHidden && MFGuiManager.Instance.FadeState == MFGuiManager.E_Fading.None)
        {
            GuiHUD.Instance.SwitchToIngameMenu();
        }
        if (Input.GetKeyDown("1"))
        {
            ChangeWeapon(GuiHUD.Instance.WeaponID(0));
        }
        else if (Input.GetKeyDown("2"))
        {
            ChangeWeapon(GuiHUD.Instance.WeaponID(1));
        }
        else if (Input.GetKeyDown("3"))
        {
            ChangeWeapon(GuiHUD.Instance.WeaponID(2));
        }
        else if (Input.GetKeyDown("4"))
        {
            ChangeWeapon(GuiHUD.Instance.WeaponID(3));
        }
        else if (Input.GetKeyDown("0"))
        {
            UseGadget(GuiHUD.Instance.ItemID(0));
        }
        else if (Input.GetKeyDown("9"))
        {
            UseGadget(GuiHUD.Instance.ItemID(1));
        }
        else if (Input.GetKeyDown("8"))
        {
            UseGadget(GuiHUD.Instance.ItemID(2));
        }
        else if (Input.GetKeyDown("7"))
        {
            UseGadget(GuiHUD.Instance.ItemID(3));
        }
        if (Input.GetKeyDown("f1") || Input.GetKeyDown("f2") || Input.GetKeyDown("f3") || Input.GetKeyDown("f4"))
        {
            E_WeaponID[] array = new E_WeaponID[4];
            if (Input.GetKeyDown("f1"))
            {
                array[0] = E_WeaponID.M4;
                array[1] = E_WeaponID.Colt1911;
                array[2] = E_WeaponID.Striker;
                array[3] = E_WeaponID.Minigun;
            }
            else if (Input.GetKeyDown("f2"))
            {
                array[0] = E_WeaponID.WaltherP99;
                array[1] = E_WeaponID.Scorpion;
                array[2] = E_WeaponID.P90;
                array[3] = E_WeaponID.Bren;
            }
            else if (Input.GetKeyDown("f3"))
            {
                array[0] = E_WeaponID.AK47;
                array[1] = E_WeaponID.KSG;
                array[2] = E_WeaponID.LeeEnfield303;
                array[3] = E_WeaponID.Uzi;
            }
            else if (Input.GetKeyDown("f4"))
            {
                array[0] = E_WeaponID.Lupara;
                array[1] = E_WeaponID.Remington870;
                array[2] = E_WeaponID.RemingtonTactics;
                array[3] = E_WeaponID.Scorpion;
            }
            WeaponBase currentWeapon = Player.Instance.Owner.WeaponComponent.GetCurrentWeapon();
            if ((bool)currentWeapon)
            {
                currentWeapon.WeaponHide();
            }
            ComponentWeaponsPlayer component = Player.Instance.GetComponent<ComponentWeaponsPlayer>();
            component.Weapons.Clear();
            Game.Instance.PlayerPersistentInfo.InventoryList.Weapons.Clear();
            Game.Instance.PlayerPersistentInfo.EquipList.Weapons.Clear();
            E_WeaponID[] array2 = array;
            foreach (E_WeaponID e_WeaponID in array2)
            {
                PPIWeaponData item = default(PPIWeaponData);
                item.ID = e_WeaponID;
                item.UpgradeLevel = E_UpgradeLevel.Mk1;
                Game.Instance.PlayerPersistentInfo.InventoryList.Weapons.Add(item);
                Game.Instance.PlayerPersistentInfo.EquipList.Weapons.Add(item);
                WeaponBase weapon = WeaponManager.Instance.GetWeapon(e_WeaponID, E_UpgradeLevel.Mk1, 1f);
                component.Weapons.Add(e_WeaponID, weapon);
            }
            component.SendMessage("DbgInitialize");
        }
        if ((bool)Mission.Instance && (bool)Mission.Instance.CurrentGameZone && Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.X))
        {
            Mission.Instance.CurrentGameZone.KillAllEnemies(Player.Instance.Owner);
        }
        if (Input.GetKeyDown("o"))
        {
            Debug.Log("Next gadget");
            GuiHUD.Instance.SelectNextGadget();
        }
        if (Input.GetKeyDown("p"))
        {
            Debug.Log("Use gadget");
            int selectedGadget = GuiHUD.Instance.GetSelectedGadget();
            UseGadget(GuiHUD.Instance.ItemID(selectedGadget));
        }
        prevMousePos = Input.mousePosition;
    }

    private void ChangeWeapon(E_WeaponID type)
    {
        if (Player.Instance.CanChangeWeapon() && (!Player.Instance.Owner.WeaponComponent || Player.Instance.Owner.WeaponComponent.GetCurrentWeapon().WeaponID != type))
        {
            States.ChangeWeaponDelegate(type);
            WeaponBase weapon = Player.Instance.Owner.WeaponComponent.GetWeapon(type);
            if ((bool)weapon)
            {
                GuiHUD.Instance.OnCurrentWeaponChanged(type);
            }
        }
    }

    private void UseGadget(E_ItemID item)
    {
        if (item != 0)
        {
            States.UseGadgetDelegate(item);
        }
    }
}
