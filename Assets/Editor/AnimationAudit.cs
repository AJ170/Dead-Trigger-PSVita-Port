using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class AnimationAudit
{
    [MenuItem("Tools/Audit Scene Animations")]
    static void AuditSceneAnimations()
    {
        Animation[] animations = Object.FindObjectsOfType<Animation>();
        Animator[] animators = Object.FindObjectsOfType<Animator>();

        Debug.Log("AnimationAudit: Found " + animations.Length
            + " Animation components and " + animators.Length
            + " Animator components in scene.");

        foreach (Animation anim in animations)
        {
            Debug.Log("Animation on: " + anim.gameObject.name
                + "\n  Clip count: " + anim.GetClipCount()
                + "\n  Path: " + GetFullPath(anim.transform)
                + "\n  Active: " + anim.gameObject.activeInHierarchy,
                anim.gameObject);
        }

        foreach (Animator animator in animators)
        {
            Debug.Log("Animator on: " + animator.gameObject.name
                + "\n  Controller: " + (animator.runtimeAnimatorController != null
                    ? animator.runtimeAnimatorController.name
                    : "NULL")
                + "\n  Path: " + GetFullPath(animator.transform)
                + "\n  Active: " + animator.gameObject.activeInHierarchy,
                animator.gameObject);
        }
    }

    [MenuItem("Tools/Remove Empty Animation Components")]
    static void RemoveEmptyAnimationComponents()
    {
        Animation[] animations = Object.FindObjectsOfType<Animation>();
        int removedCount = 0;
        int skippedCount = 0;

        Undo.SetCurrentGroupName("Remove Empty Animation Components");
        int undoGroup = Undo.GetCurrentGroup();

        foreach (Animation anim in animations)
        {
            if (anim.GetClipCount() == 0)
            {
                Debug.Log("AnimationAudit: Removing empty Animation from "
                    + GetFullPath(anim.transform),
                    anim.gameObject);
                Undo.DestroyObjectImmediate(anim);
                removedCount++;
            }
            else
            {
                Debug.Log("AnimationAudit: Keeping Animation on "
                    + anim.gameObject.name
                    + " (" + anim.GetClipCount() + " clips assigned)",
                    anim.gameObject);
                skippedCount++;
            }
        }

        Undo.CollapseUndoOperations(undoGroup);

        EditorUtility.SetDirty(UnityEngine.SceneManagement
            .SceneManager.GetActiveScene().GetRootGameObjects()[0]);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("AnimationAudit: Cleanup complete."
            + "\n  Removed: " + removedCount
            + "\n  Kept: " + skippedCount);
    }

    static string GetFullPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}