using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public enum Platform
{   
    PSVita
}
public class InputTest : MonoBehaviour
{
    public Platform Platform;
    public Text Text;
    public string String;
    public void Start()
    {
        EditorEventSystemCheck();
    }
    public void Update()
    {
        InputListner();
    }
    public void InputListner()
    {       
        if (Platform == Platform.PSVita)
        {
            float xButtonAxis = Input.GetAxis("X Button");
            float circleButtonAxis = Input.GetAxis("Circle Button");
            float squareButtonAxis = Input.GetAxis("Square Button");
            float triangleButtonAxis = Input.GetAxis("Triangle Button");
            float lButtonAxis = Input.GetAxis("L Button");
            float rButtonAxis = Input.GetAxis("R Button");
            float selectButtonAxis = Input.GetAxis("Select Button");
            float startButtonAxis = Input.GetAxis("Start Button");
            float dpadUpAxis = Input.GetAxis("DPad Up");
            float dpadRightAxis = Input.GetAxis("DPad Right");
            float dpadDownAxis = Input.GetAxis("DPad Down");
            float dpadLeftAxis = Input.GetAxis("DPad Left");
            float leftStickHorizontalAxis = Input.GetAxis("Left Joystick Horizontal");
            float leftStickVerticalAxis = Input.GetAxis("Left Joystick Vertical");
            float rightStickHorizontalAxis = Input.GetAxis("Right Joystick Horizontal");
            float rightStickVerticalAxis = Input.GetAxis("Right Joystick Vertical");
            if (xButtonAxis > 0.1f)
            {
                String = "X Button";
                Text.text = String;
            }
            if (circleButtonAxis > 0.1f)
            {
                String = "Circle Button";
                Text.text = String;
            }
            if (squareButtonAxis > 0.1f)
            {
                String = "Square Button";
                Text.text = String;
            }
            if (triangleButtonAxis > 0.1f)
            {
                String = "Triangle Button";
                Text.text = String;
            }
            if (lButtonAxis > 0.1f)
            {
                String = "L Button";
                Text.text = String;
            }
            if (rButtonAxis > 0.1f)
            {
                String = "R Button";
                Text.text = String;
            }
            if (selectButtonAxis > 0.1f)
            {
                String = "Select Button";
                Text.text = String;
            }
            if (startButtonAxis > 0.1f)
            {
                String = "Start Button";
                Text.text = String;
            }
            if (dpadUpAxis > 0.1f)
            {
                String = "DPad Up";
                Text.text = String;
            }
            if (dpadRightAxis > 0.1f)
            {
                String = "DPad Right";
                Text.text = String;
            }
            if (dpadDownAxis > 0.1f)
            {
                String = "DPad Down";
                Text.text = String;
            }
            if (dpadLeftAxis > 0.1f)
            {
                String = "DPad Left";
                Text.text = String;
            }
            if (leftStickHorizontalAxis > 0.1f)
            {
                String = "Left Joystick Horizontal Right";
                Text.text = String;
            }
            if (leftStickHorizontalAxis < -0.1f)
            {
                String = "Left Joystick Horizontal Left";
                Text.text = String;
            }
            if (leftStickVerticalAxis > 0.1f)
            {
                String = "Left Joystick Vertical Down";
                Text.text = String;
            }
            if (leftStickVerticalAxis < -0.1f)
            {
                String = "Left Joystick Vertical Up";
                Text.text = String;
            }
            if (rightStickHorizontalAxis > 0.1f)
            {
                String = "Right Joystick Horizontal Right";
                Text.text = String;
            }
            if (rightStickHorizontalAxis < -0.1f)
            {
                String = "Right Joystick Horizontal Left";
                Text.text = String;
            }
            if (rightStickVerticalAxis > 0.1f)
            {
                String = "Right Joystick Vertical Down";
                Text.text = String;
            }
            if (rightStickVerticalAxis < -0.1f)
            {
                String = "Right Joystick Vertical Up";
                Text.text = String;
            }
        }
    }
    public void EditorEventSystemCheck()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            Debug.Log("InputTest: Automatically adding missing EventSystem to the scene.");
            GameObject EventSystemObject = new GameObject("EventSystem");
            EventSystemObject.AddComponent<EventSystem>();
            EventSystemObject.AddComponent<StandaloneInputModule>();
        }
    }
    [ExecuteInEditMode]
    public void OnValidate()
    {
        EditorEventSystemCheck();
    }
}