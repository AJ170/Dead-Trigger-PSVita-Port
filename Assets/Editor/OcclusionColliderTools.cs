using UnityEditor;
using UnityEngine;

public static class OcclusionColliderTools
{
    private const string OcclusionLayerName = "OcclusionColliders";

    [MenuItem("GameObject/Occlusion Bake/Add Occlusion Colliders to Children")]
    static void AddOcclusionColliders()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogWarning("OcclusionColliderTools: No GameObject selected.");
            return;
        }

        // Ensure the layer exists
        int layer = LayerMask.NameToLayer(OcclusionLayerName);
        if (layer == -1)
        {
            Debug.LogError("OcclusionColliderTools: Layer '" + OcclusionLayerName + "' does not exist. Please create it in Edit > Project Settings > Tags and Layers, then set it to collide with nothing in the Physics matrix.");
            return;
        }

        int count = 0;
        MeshRenderer[] renderers = selected.GetComponentsInChildren<MeshRenderer>(true);

        Undo.SetCurrentGroupName("Add Occlusion Colliders");
        int undoGroup = Undo.GetCurrentGroup();

        foreach (MeshRenderer renderer in renderers)
            {
                GameObject go = renderer.gameObject;

                if (go.GetComponent<Collider>() != null)
                    continue;

                MeshFilter mf = go.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null)
                    continue;

                BoxCollider col = Undo.AddComponent<BoxCollider>(go);

                // Use the world space bounds from the renderer which accounts
                // for the transform's rotation, scale and position offset
                // then convert back into the object's local space
                Bounds worldBounds = renderer.bounds;

                // Convert world bounds centre to local space
                col.center = go.transform.InverseTransformPoint(worldBounds.center);

                // Convert world bounds size to local space
                // We use InverseTransformVector for size to account for scale
                // but we need absolute values since size can't be negative
                Vector3 localSize = go.transform.InverseTransformVector(worldBounds.size);
                col.size = new Vector3(
                    Mathf.Abs(localSize.x),
                    Mathf.Abs(localSize.y),
                    Mathf.Abs(localSize.z)
                );

                col.isTrigger = false;
                go.layer = layer;
                count++;
            }

        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log("OcclusionColliderTools: Added " + count + " box colliders on layer '" + OcclusionLayerName + "'.");
    }

    [MenuItem("GameObject/Occlusion Bake/Add Occlusion Colliders to Children", true)]
    static bool AddOcclusionCollidersValidate()
    {
        return Selection.activeGameObject != null;
    }

    [MenuItem("GameObject/Occlusion Bake/Remove Occlusion Colliders from Children")]
    static void RemoveOcclusionColliders()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogWarning("OcclusionColliderTools: No GameObject selected.");
            return;
        }

        int layer = LayerMask.NameToLayer(OcclusionLayerName);
        if (layer == -1)
        {
            Debug.LogWarning("OcclusionColliderTools: Layer '" + OcclusionLayerName + "' not found.");
            return;
        }

        int count = 0;

        Undo.SetCurrentGroupName("Remove Occlusion Colliders");
        int undoGroup = Undo.GetCurrentGroup();

        // Only remove colliders on our specific layer
        // so we don't touch anything that had a collider before
        BoxCollider[] colliders = selected.GetComponentsInChildren<BoxCollider>(true);
        foreach (BoxCollider col in colliders)
        {
            if (col.gameObject.layer != layer)
                continue;

            // Restore layer to default
            col.gameObject.layer = 0;
            Undo.DestroyObjectImmediate(col);
            count++;
        }

        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log("OcclusionColliderTools: Removed " + count + " occlusion colliders.");
    }

    [MenuItem("GameObject/Occlusion Bake/Remove Occlusion Colliders from Children", true)]
    static bool RemoveOcclusionCollidersValidate()
    {
        return Selection.activeGameObject != null;
    }
}