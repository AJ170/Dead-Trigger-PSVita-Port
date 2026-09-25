using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Editor helper for adding new options to the MADFINGER GUI (MainOpt pivot etc).
//
// The GUI system finds widgets by GameObject NAME inside a GUIBase_Layout, and all of
// a widget's OEM look (atlas sprites, fonts, sounds, the on/off button pair on a
// switch) lives on the widget itself. So the reliable way to add an option that
// matches the game's style is to CLONE an existing widget - the clone inherits all of
// those references automatically.
//
// NAME COLLISIONS ARE THE BIG TRAP. GUIBase_Layout.GetWidget() returns the FIRST
// widget whose name matches, so a raw clone (which duplicates every child name) can
// steal the binding from the widget it was copied from - e.g. GuiOptionsMenu looks up
// "Sensitivity_Slider", finds the clone, and the real slider is then never given its
// value and renders blank. This tool therefore renames the whole cloned subtree and
// can scan a layout for leftover duplicates.
//
// Positioning note: GUIBase_Widget caches base.transform.position as m_OrigPos in
// AddMainSprite(), but GUIUpdate() re-reads transform.position every frame and offsets
// the sprites by (position - m_OrigPos). So moving the Transform moves the widget
// LIVE - you can nudge while in Play mode and watch it move.
//
// Usage:  Tools > GUI > Option Widget Cloner
public class GuiOptionCloner : EditorWindow
{
	private string m_NewName = "MyOption_Switch";

	private float m_VerticalSpacing = 30f;

	private bool m_DefaultValue;

	private float m_NudgeStep = 1f;

	private bool m_ClearTextID;

	private string m_PendingLabelText = string.Empty;

	private GUIBase_Label m_PendingLabel;

	private List<string> m_Duplicates = new List<string>();

	private bool m_DuplicateScanRun;

	private Vector2 m_Scroll;

	[MenuItem("Tools/GUI/Option Widget Cloner")]
	public static void ShowWindow()
	{
		GetWindow<GuiOptionCloner>("Option Cloner");
	}

	private void OnSelectionChange()
	{
		m_PendingLabel = null;
		m_PendingLabelText = string.Empty;
		m_DuplicateScanRun = false;
		m_Duplicates.Clear();
		Repaint();
	}

	private void OnGUI()
	{
		m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);

		GameObject sel = Selection.activeGameObject;
		if (sel == null)
		{
			EditorGUILayout.HelpBox("Select a widget in the Hierarchy (the object that has GUI Base_Widget on it).", MessageType.Info);
			EditorGUILayout.EndScrollView();
			return;
		}

		GUIBase_Widget selWidget = sel.GetComponent<GUIBase_Widget>();

		EditorGUILayout.LabelField("Selected", sel.name);
		EditorGUILayout.LabelField("Parent", (sel.transform.parent != null) ? sel.transform.parent.name : "(none)");
		if (selWidget == null)
		{
			EditorGUILayout.HelpBox("No GUIBase_Widget on this object - pick the widget object itself.", MessageType.Warning);
		}

		DrawCloneSection(sel, selWidget);
		EditorGUILayout.Space();
		DrawNudgeSection(sel);
		EditorGUILayout.Space();
		DrawLabelSection(sel);
		EditorGUILayout.Space();
		DrawDuplicateSection(sel);

		EditorGUILayout.EndScrollView();
	}

	// ---------------------------------------------------------------- clone

	private void DrawCloneSection(GameObject sel, GUIBase_Widget selWidget)
	{
		EditorGUILayout.LabelField("1. Clone a widget", EditorStyles.boldLabel);

		m_NewName = EditorGUILayout.TextField("New name", m_NewName);
		EditorGUILayout.HelpBox("This name is what you pass to GuiBaseUtils.RegisterSwitchDelegate(...). They must match exactly.\n\nEvery child of the clone is renamed too, so it cannot steal the original's name lookup.", MessageType.None);

		EditorGUILayout.BeginHorizontal();
		m_VerticalSpacing = EditorGUILayout.FloatField("Y offset", m_VerticalSpacing);
		if (GUILayout.Button("Suggest", GUILayout.Width(70f)))
		{
			m_VerticalSpacing = DetectSpacing(sel);
		}
		EditorGUILayout.EndHorizontal();

		if (sel.GetComponent<GUIBase_Switch>() != null)
		{
			m_DefaultValue = EditorGUILayout.Toggle("Default ON", m_DefaultValue);
		}

		GUI.enabled = (selWidget != null && !string.IsNullOrEmpty(m_NewName));
		if (GUILayout.Button("Clone Below", GUILayout.Height(24f)))
		{
			CloneWidget(sel);
		}
		GUI.enabled = true;
	}

	// Smallest non-zero vertical gap between sibling widgets - only a suggestion,
	// because rows are not always laid out as flat siblings.
	private float DetectSpacing(GameObject src)
	{
		Transform parent = src.transform.parent;
		if (parent == null)
		{
			return 30f;
		}
		List<float> ys = new List<float>();
		for (int i = 0; i < parent.childCount; i++)
		{
			Transform child = parent.GetChild(i);
			if (child.GetComponent<GUIBase_Widget>() != null)
			{
				ys.Add(child.localPosition.y);
			}
		}
		ys.Sort();
		float best = 0f;
		for (int i = 1; i < ys.Count; i++)
		{
			float d = Mathf.Abs(ys[i] - ys[i - 1]);
			if (d > 0.01f && (best <= 0.01f || d < best))
			{
				best = d;
			}
		}
		return (best > 0.01f) ? best : 30f;
	}

	private void CloneWidget(GameObject src)
	{
		// Instantiate under the same parent, then copy the source's LOCAL transform
		// verbatim so the clone starts exactly on top of it, and only then offset.
		// (Never write anything back to the source.)
		GameObject clone = Object.Instantiate(src, src.transform.parent) as GameObject;
		clone.name = m_NewName;
		clone.transform.localPosition = src.transform.localPosition;
		clone.transform.localRotation = src.transform.localRotation;
		clone.transform.localScale = src.transform.localScale;
		clone.transform.SetSiblingIndex(src.transform.GetSiblingIndex() + 1);

		// Offset downward. Y+ is up, so a positive "Y offset" moves the clone down.
		Vector3 p = clone.transform.localPosition;
		p.y -= m_VerticalSpacing;
		clone.transform.localPosition = p;

		// Rename the whole subtree. GUIBase_Layout.GetWidget() returns the first
		// name match, so leaving a child called e.g. "Sensitivity_Slider" on the
		// clone lets it hijack the original's binding - the original then never
		// receives SetValue() and draws empty.
		int renamed = RenameSubtree(clone.transform, src.name, m_NewName);

		GUIBase_Switch sw = clone.GetComponent<GUIBase_Switch>();
		if (sw != null)
		{
			sw.m_InitValue = m_DefaultValue;
		}

		Undo.RegisterCreatedObjectUndo(clone, "Clone GUI Option Widget");
		Selection.activeGameObject = clone;
		EditorSceneManager.MarkSceneDirty(clone.scene);

		Debug.Log("[GuiOptionCloner] Created '" + clone.name + "' at local " + clone.transform.localPosition
			+ " (world " + clone.transform.position + "), renamed " + renamed
			+ " child object(s). Use the Nudge buttons to position it - it moves live in Play mode.");
	}

	// Renames every descendant so no name survives that the original also uses.
	// A child named "<srcName>_Foo" becomes "<newName>_Foo"; anything else just gets
	// the new root name as a prefix.
	private int RenameSubtree(Transform root, string srcName, string newName)
	{
		int count = 0;
		Transform[] all = root.GetComponentsInChildren<Transform>(true);
		for (int i = 0; i < all.Length; i++)
		{
			Transform t = all[i];
			if (t == root)
			{
				continue;
			}
			string n = t.gameObject.name;
			if (!string.IsNullOrEmpty(srcName) && n.StartsWith(srcName))
			{
				t.gameObject.name = newName + n.Substring(srcName.Length);
			}
			else
			{
				t.gameObject.name = newName + "_" + n;
			}
			count++;
		}
		return count;
	}

	// ---------------------------------------------------------------- nudge

	private void DrawNudgeSection(GameObject sel)
	{
		EditorGUILayout.LabelField("2. Position", EditorStyles.boldLabel);
		EditorGUILayout.HelpBox("GUIUpdate() re-reads transform.position every frame, so nudging moves the widget LIVE - you can do it while the game is playing.", MessageType.None);

		EditorGUILayout.LabelField("World pos", sel.transform.position.ToString("F1"));

		Vector3 lp = sel.transform.localPosition;
		Vector3 newLp = EditorGUILayout.Vector3Field("Local position", lp);
		if (newLp != lp)
		{
			Undo.RecordObject(sel.transform, "Move GUI Widget");
			sel.transform.localPosition = newLp;
			EditorSceneManager.MarkSceneDirty(sel.scene);
		}

		m_NudgeStep = EditorGUILayout.FloatField("Nudge step", m_NudgeStep);

		EditorGUILayout.BeginHorizontal();
		GUILayout.Space(60f);
		if (GUILayout.Button("Up")) { Nudge(sel, 0f, m_NudgeStep); }
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Left")) { Nudge(sel, 0f - m_NudgeStep, 0f); }
		if (GUILayout.Button("Down")) { Nudge(sel, 0f, 0f - m_NudgeStep); }
		if (GUILayout.Button("Right")) { Nudge(sel, m_NudgeStep, 0f); }
		EditorGUILayout.EndHorizontal();

		DrawSiblingMap(sel);
	}

	// Lists the sibling widgets by Y so overlaps are obvious without entering Play.
	private void DrawSiblingMap(GameObject sel)
	{
		Transform parent = sel.transform.parent;
		if (parent == null)
		{
			return;
		}
		EditorGUILayout.LabelField("Sibling rows (world Y)", EditorStyles.miniBoldLabel);
		for (int i = 0; i < parent.childCount; i++)
		{
			Transform child = parent.GetChild(i);
			if (child.GetComponent<GUIBase_Widget>() == null)
			{
				continue;
			}
			string row = child.gameObject.name + "   y=" + child.position.y.ToString("F1");
			if (child == sel.transform)
			{
				EditorGUILayout.LabelField("> " + row, EditorStyles.boldLabel);
			}
			else
			{
				EditorGUILayout.LabelField("   " + row);
			}
		}
	}

	private void Nudge(GameObject go, float dx, float dy)
	{
		Undo.RecordObject(go.transform, "Nudge GUI Widget");
		Vector3 p = go.transform.localPosition;
		p.x += dx;
		p.y += dy;
		go.transform.localPosition = p;
		EditorSceneManager.MarkSceneDirty(go.scene);
	}

	// ---------------------------------------------------------------- labels

	private void DrawLabelSection(GameObject sel)
	{
		EditorGUILayout.LabelField("3. Caption", EditorStyles.boldLabel);

		GUIBase_Label[] labels = sel.GetComponentsInChildren<GUIBase_Label>(true);
		if (labels.Length == 0)
		{
			EditorGUILayout.HelpBox("No GUIBase_Label under this object.", MessageType.None);
			return;
		}

		EditorGUILayout.HelpBox("Editing is explicit - pick a label, type the text, press Apply. Nothing is written until you do.", MessageType.None);

		for (int i = 0; i < labels.Length; i++)
		{
			GUIBase_Label label = labels[i];
			SerializedObject so = new SerializedObject(label);
			SerializedProperty textProp = so.FindProperty("m_Text");
			SerializedProperty idProp = so.FindProperty("m_TextID");
			if (textProp == null || idProp == null)
			{
				continue;
			}

			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			string desc = label.gameObject.name;
			if (idProp.intValue != 0)
			{
				desc += "   [localized ID " + idProp.intValue + "]";
			}
			EditorGUILayout.LabelField(desc, EditorStyles.miniBoldLabel);
			EditorGUILayout.LabelField("Raw m_Text", string.IsNullOrEmpty(textProp.stringValue) ? "(empty)" : textProp.stringValue);

			if (GUILayout.Button("Edit this label"))
			{
				m_PendingLabel = label;
				m_PendingLabelText = textProp.stringValue;
				m_ClearTextID = (idProp.intValue != 0);
			}

			if (m_PendingLabel == label)
			{
				m_PendingLabelText = EditorGUILayout.TextField("New text", m_PendingLabelText);
				if (idProp.intValue != 0)
				{
					m_ClearTextID = EditorGUILayout.ToggleLeft("Clear localized ID (required for raw text to show)", m_ClearTextID);
					EditorGUILayout.HelpBox("This label currently pulls translated text from the TextDatabase. Clearing the ID means your text shows in every language.", MessageType.Warning);
				}
				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button("Apply"))
				{
					textProp.stringValue = m_PendingLabelText;
					if (m_ClearTextID)
					{
						idProp.intValue = 0;
					}
					// ApplyModifiedProperties registers its own undo - do NOT also
					// call Undo.RecordObject or the two fight and can revert edits.
					so.ApplyModifiedProperties();
					EditorSceneManager.MarkSceneDirty(label.gameObject.scene);
					m_PendingLabel = null;
				}
				if (GUILayout.Button("Cancel"))
				{
					m_PendingLabel = null;
				}
				EditorGUILayout.EndHorizontal();
			}
			EditorGUILayout.EndVertical();
		}
	}

	// ---------------------------------------------------------------- duplicates

	private void DrawDuplicateSection(GameObject sel)
	{
		EditorGUILayout.LabelField("4. Check for name clashes", EditorStyles.boldLabel);
		EditorGUILayout.HelpBox("GUIBase_Layout.GetWidget() returns the FIRST name match. Two widgets sharing a name means the game may bind to the wrong one - the other then never gets its value and renders blank.", MessageType.None);

		GUIBase_Layout layout = sel.GetComponentInParent<GUIBase_Layout>();
		if (layout == null)
		{
			EditorGUILayout.HelpBox("Selection is not inside a GUIBase_Layout.", MessageType.Warning);
			return;
		}

		EditorGUILayout.LabelField("Layout", layout.gameObject.name);
		if (GUILayout.Button("Scan layout"))
		{
			ScanDuplicates(layout);
		}

		if (!m_DuplicateScanRun)
		{
			return;
		}
		if (m_Duplicates.Count == 0)
		{
			EditorGUILayout.HelpBox("No duplicate widget names in this layout.", MessageType.Info);
			return;
		}
		EditorGUILayout.HelpBox("Duplicate widget names found - rename one of each pair:", MessageType.Error);
		for (int i = 0; i < m_Duplicates.Count; i++)
		{
			EditorGUILayout.LabelField("   " + m_Duplicates[i]);
		}
	}

	private void ScanDuplicates(GUIBase_Layout layout)
	{
		m_Duplicates.Clear();
		m_DuplicateScanRun = true;

		GUIBase_Widget[] widgets = layout.GetComponentsInChildren<GUIBase_Widget>(true);
		Dictionary<string, int> counts = new Dictionary<string, int>();
		for (int i = 0; i < widgets.Length; i++)
		{
			string n = widgets[i].gameObject.name;
			if (counts.ContainsKey(n))
			{
				counts[n] = counts[n] + 1;
			}
			else
			{
				counts[n] = 1;
			}
		}
		foreach (KeyValuePair<string, int> kv in counts)
		{
			if (kv.Value > 1)
			{
				m_Duplicates.Add(kv.Key + "   x" + kv.Value);
			}
		}
		m_Duplicates.Sort();
	}
}
