using UnityEditor;
using UnityEngine;

public class PSVitaControlMappingTool : EditorWindow
{
    private enum ActionType
    {
        AddStandardInputSystem,     
        AddPSVitaInputSystem,  
        DeleteAllInputs
    }
    private ActionType actionType;
    [MenuItem("Code Explode Studios Tools/PS Vita Control Mapping Tool")]
    public static void ShowWindow()
    {
        GetWindow<PSVitaControlMappingTool>("PS Vita Control Mapping Tool");
    }
    private void OnGUI()
    {
        GUILayout.Label("PS Vita Control Mapping Tool Options", EditorStyles.boldLabel);  
        actionType = (ActionType)EditorGUILayout.EnumPopup("Action Type", actionType);
        if (GUILayout.Button("Add Control Mappings!"))
        {
            switch (actionType)
            {
                case ActionType.DeleteAllInputs:
                    DeleteAllInputs();
                    break;
                case ActionType.AddStandardInputSystem:
                    AddSinglePlayerStandardInputSystem();
                    break;             
                case ActionType.AddPSVitaInputSystem:
                    AddPSVitaInputSystem();
                    break;               
            }           
        }
    }
    //SINGLE PLAYER CONTROLLS
    private void AddSinglePlayerStandardInputSystem()
    {
        AddAxis("Horizontal", "left", "right", 3.0f, 0.001f, 3.0f, true, false, "a", "d", 0, 0, 0);
        AddAxis("Vertical", "down", "up", 3.0f, 0.001f, 3.0f, true, false, "s", "w", 0, 0, 0);
        AddAxis("Fire1", "", "left ctrl", 1000f, 0.001f, 1000f, false, false, "", "mouse 0", 0, 0, 0);
        AddAxis("Fire2", "", "left alt", 1000f, 0.001f, 1000f, false, false, "", "mouse 1", 0, 0, 0);
        AddAxis("Fire3", "", "left shift", 1000f, 0.001f, 1000f, false, false, "", "mouse 2", 0, 0, 0);
        AddAxis("Jump", "", "space", 1000f, 0.001f, 1000f, false, false, "", "", 0, 0, 0);
        AddAxis("Mouse X", "", "", 0f, 0f, 0.1f, false, false, "", "", 1, 0, 0);
        AddAxis("Mouse Y", "", "", 0f, 0f, 0.1f, false, false, "", "", 1, 1, 0);
        AddAxis("Mouse ScrollWheel", "", "", 0f, 0f, 0.1f, false, false, "", "", 1, 2, 0);
        AddAxis("Horizontal", "", "", 0f, 0.19f, 1.0f, false, false, "", "", 2, 0, 0);
        AddAxis("Vertical", "", "", 0f, 0.19f, 1.0f, false, true, "", "", 2, 1, 0);
        AddAxis("Fire1", "", "joystick button 0", 1000f, 0.001f, 1000f, false, false, "Mouse0", "", 0, 0, 0);
        AddAxis("Fire2", "", "joystick button 1", 1000f, 0.001f, 1000f, false, false, "Mouse1", "", 0, 0, 0);
        AddAxis("Fire3", "", "joystick button 2", 1000f, 0.001f, 1000f, false, false, "Mouse2", "", 0, 0, 0);
        AddAxis("Jump", "", "joystick button 3", 1000f, 0.001f, 1000f, false, false, "", "", 0, 0, 0);
        AddAxis("Submit", "", "return", 1000f, 0.001f, 1000f, false, false, "", "joystick button 0", 0, 0, 0);
        AddAxis("Submit", "", "enter", 1000f, 0.001f, 1000f, false, false, "", "space", 0, 0, 0);
        AddAxis("Cancel", "", "escape", 1000f, 0.001f, 1000f, false, false, "", "joystick button 1", 0, 0, 0);
    }        
    private void AddPSVitaInputSystem()
    {
        AddAxis("Right Joystick Horizontal", "left", "right", 3.0f, 0.3f, 3.0f, true, false, "", "", 2, 3, 1);
        AddAxis("Right Joystick Vertical", "down", "up", 3.0f, 0.3f, 3.0f, true, false, "", "", 2, 4, 1);
        AddAxis("Left Joystick Horizontal", "a", "d", 3.0f, 0.3f, 3.0f, false, false, "", "", 2, 0, 1);
        AddAxis("Left Joystick Vertical", "s", "w", 3.0f, 0.3f, 3.0f, false, false, "", "", 2, 1, 1);
        AddAxis("X Button", "", "joystick button 0", 3.0f, 0.3f, 3.0f, true, false, "", "", 0, 0, 1);
        AddAxis("Circle Button", "", "joystick button 1", 3.0f, 0.3f, 3.0f, true, false, "", "", 0, 0, 1);
        AddAxis("Square Button", "", "joystick button 2", 3.0f, 0.3f, 3.0f, true, false, "", "", 0, 0, 1);
        AddAxis("Triangle Button", "", "joystick button 3", 3.0f, 0.3f, 3.0f, true, false, "", "", 0, 0, 1);
        AddAxis("L Button", "", "joystick button 4", 3.0f, 0.3f, 3.0f, true, false, "", "", 0, 0, 1);
        AddAxis("R Button", "", "joystick button 5", 3.0f, 0.3f, 3.0f, true, false, "", "", 0, 0, 1);
        AddAxis("Select Button", "", "joystick button 6", 3.0f, 0.3f, 3.0f, true, false, "", "", 0, 0, 1);
        AddAxis("Start Button", "", "joystick button 7", 3.0f, 0.3f, 3.0f, true, false, "", "", 0, 0, 1);
        AddAxis("DPad Up", "", "joystick button 8", 3.0f, 0.3f, 3.0f, true, false, "", "", 0, 0, 1);
        AddAxis("DPad Right", "", "joystick button 9", 3.0f, 0.3f, 3.0f, true, false, "", "", 0, 0, 1);
        AddAxis("DPad Down", "", "joystick button 10", 3.0f, 0.3f, 3.0f, true, false, "", "", 0, 0, 1);
        AddAxis("DPad Left", "", "joystick button 11", 3.0f, 0.3f, 3.0f, true, false, "", "", 0, 0, 1);
    }    
    private void DeleteAllInputs()
    {
        SerializedObject inputManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0]);
        SerializedProperty axes = inputManager.FindProperty("m_Axes");
        axes.ClearArray();
        inputManager.ApplyModifiedProperties();
    }
    private void AddAxis(string name, string negativeKey, string positiveKey, float gravity, float dead, float sensitivity, bool snap, bool invert, string altNegativeKey, string altPositiveKey, int type, int Axis, int joyNum)
    {
        SerializedObject inputManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0]);
        SerializedProperty axes = inputManager.FindProperty("m_Axes");
        axes.arraySize++;
        inputManager.ApplyModifiedProperties();
        SerializedProperty axis = axes.GetArrayElementAtIndex(axes.arraySize - 1);
        axis.FindPropertyRelative("m_Name").stringValue = name;
        axis.FindPropertyRelative("negativeButton").stringValue = negativeKey;
        axis.FindPropertyRelative("positiveButton").stringValue = positiveKey;
        axis.FindPropertyRelative("altNegativeButton").stringValue = altNegativeKey;
        axis.FindPropertyRelative("altPositiveButton").stringValue = altPositiveKey;
        axis.FindPropertyRelative("gravity").floatValue = gravity;
        axis.FindPropertyRelative("dead").floatValue = dead;
        axis.FindPropertyRelative("sensitivity").floatValue = sensitivity;
        axis.FindPropertyRelative("snap").boolValue = snap;
        axis.FindPropertyRelative("invert").boolValue = invert;
        axis.FindPropertyRelative("type").intValue = type;
        axis.FindPropertyRelative("axis").intValue = Axis;
        axis.FindPropertyRelative("joyNum").intValue = joyNum;
        inputManager.ApplyModifiedProperties();
    }   
}
