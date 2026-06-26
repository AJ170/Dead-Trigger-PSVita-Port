using UnityEngine;
using UnityEditor;

public static class TidyUpTools
{
    [MenuItem("GameObject/Tidy Up All Children", false, 0)]
    static void TidyUpAllChildren()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogWarning("TidyUp: No GameObject selected.");
            return;
        }

        int removedAnimations = 0;

        // Gather self + all children
        Transform[] all = selected.GetComponentsInChildren<Transform>(true);

        Undo.SetCurrentGroupName("Tidy Up All Children");
        int undoGroup = Undo.GetCurrentGroup();

        foreach (Transform t in all)
        {
            GameObject go = t.gameObject;

            // --- Remove empty Animation components ---
            Animation anim = go.GetComponent<Animation>();
            if (anim != null && anim.GetClipCount() == 0)
            {
                Undo.DestroyObjectImmediate(anim);
                removedAnimations++;
            }

            // --- Future checks go here ---
            // e.g. remove empty AudioSource, missing scripts, etc.
        }

        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log("TidyUp complete on '" + selected.name + "': " +
                  "removed " + removedAnimations + " empty Animation component(s).");
    }

    [MenuItem("GameObject/Tidy Up All Children", true)]
    static bool TidyUpAllChildrenValidate()
    {
        return Selection.activeGameObject != null;
    }
}