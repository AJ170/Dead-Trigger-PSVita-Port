using UnityEngine;

// On-screen gamepad probe. Add this component to any GameObject in the in-game scene.
// Press R (or any input) and watch which axis moves and which button lights up.
// C# 4 compatible (no interpolated strings / expression-bodied members) for Unity 2017.4.
public class PadProbe : MonoBehaviour
{
    // Axis names that already exist in this project's InputManager.
    // Joystick_3a = Unity "3rd axis" = joystick axis index 2 (the trigger axis on XInput).
    // Joystick_4a..8a = raw axis indices 3..7. HorizontalView/VerticalView are the gameplay look axes.
    private static readonly string[] AxisNames = new string[]
    {
        "HorizontalMove", "VerticalMove", "HorizontalView", "VerticalView",
        "Joystick_3a", "Joystick_4a", "Joystick_5a", "Joystick_6a", "Joystick_7a", "Joystick_8a"
    };

    private void OnGUI()
    {
        string s = "joystick(s): " + string.Join(", ", Input.GetJoystickNames()) + "\n\n";

        for (int a = 0; a < AxisNames.Length; a++)
        {
            float v = 0f;
            try { v = Input.GetAxis(AxisNames[a]); }
            catch { } // axis name not defined in InputManager -> leave at 0
            s += AxisNames[a] + ": " + v.ToString("F2") + "\n";
        }

        s += "\n";
        for (int b = 0; b < 20; b++)
        {
            if (Input.GetKey((KeyCode)((int)KeyCode.JoystickButton0 + b)))
            {
                s += "BUTTON " + b + " down\n";
            }
        }

        GUI.Label(new Rect(20, 20, 600, 700), s);
    }
}
