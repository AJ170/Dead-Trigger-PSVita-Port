using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AttachRecoveredLightProbes
{
    [MenuItem("Tools/Light Probe Recovery/Attach Selected Probes to Scene")]
    public static void AttachSelected()
    {
        try { Attach(Selection.activeObject as LightProbes); }
        catch (Exception e) { Debug.LogError("Light probe attachment failed: " + e.Message); }
    }

    public static string Attach(LightProbes probes)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.sceneCount != 1 || string.IsNullOrEmpty(scene.path))
            throw new InvalidOperationException("Open one saved scene outside Play mode first.");
        string probePath = AssetDatabase.GetAssetPath(probes);
        if (probes == null || probes.count == 0 || !probePath.StartsWith("Assets/RecoveredLightProbes/", StringComparison.Ordinal))
            throw new InvalidOperationException("Select this level's LightProbes asset under Assets/RecoveredLightProbes.");
        string level = new DirectoryInfo(Path.GetDirectoryName(probePath)).Name;
        string originalPath = "Assets/Levels/Test/" + level + "/LightingData.asset";
        LightingDataAsset previous = Lightmapping.lightingDataAsset;
        string previousPath = AssetDatabase.GetAssetPath(previous);
        string existingSceneGuid = AssetDatabase.AssetPathToGUID(scene.path);
        string snapshotPath = "Tools/LightProbeRecovery/LightingTemplates/" + level + ".yaml";
        string snapshot = File.ReadAllText(snapshotPath);
        Match expectedOwner = Regex.Match(snapshot, @"(?m)^  m_Scene:.*guid: ([0-9a-f]+)");
        if (!expectedOwner.Success || expectedOwner.Groups[1].Value != existingSceneGuid)
            throw new InvalidOperationException("The selected probes belong to a different level. Open the matching level scene.");
        // Prefer the scene's current lighting, preserving any later lighting edits.
        string templatePath = string.IsNullOrEmpty(previousPath) ? originalPath : previousPath;
        string text = File.Exists(templatePath) ? File.ReadAllText(templatePath) : "";
        if (!text.StartsWith("%YAML", StringComparison.Ordinal))
        {
            if (templatePath != originalPath)
                throw new InvalidOperationException("Current lighting data is binary. Serialize it as text before attaching probes so its lighting edits can be preserved.");
            text = snapshot;
        }
        if (!text.StartsWith("%YAML", StringComparison.Ordinal) ||
            Regex.Matches(text, @"(?m)^  m_LightProbes:.*$").Count != 1 ||
            Regex.Matches(text, @"(?m)^  m_Scene:.*$").Count != 1)
            throw new InvalidOperationException("Expected a text LightingDataAsset with one scene and probe reference.");
        // A populated lighting scene reference must match the scene being edited.
        Match owner = Regex.Match(text, @"(?m)^  m_Scene:.*guid: ([0-9a-f]+)");
        if (owner.Success && owner.Groups[1].Value != existingSceneGuid)
            throw new InvalidOperationException("The lighting template belongs to a different scene. Open the matching level scene.");
        text = Regex.Replace(text, @"(?m)^  m_LightProbes:.*$", "  m_LightProbes: {fileID: 25800000, guid: " + AssetDatabase.AssetPathToGUID(probePath) + ", type: 2}");
        text = Regex.Replace(text, @"(?m)^  m_Scene:.*$", "  m_Scene: {fileID: 102900000, guid: " + existingSceneGuid + ", type: 3}");
        string output = AssetDatabase.GenerateUniqueAssetPath(Path.GetDirectoryName(probePath).Replace('\\', '/') + "/LightingData_Recovered.asset");
        File.WriteAllText(output, text);
        AssetDatabase.ImportAsset(output, ImportAssetOptions.ForceUpdate);
        LightingDataAsset data = AssetDatabase.LoadAssetAtPath<LightingDataAsset>(output);
        if (data == null) throw new InvalidOperationException("Unity could not import the lighting data copy.");
        Lightmapping.lightingDataAsset = data;
        // Unity 2017 refreshes native probe state when the scene is reloaded.
        if (Lightmapping.lightingDataAsset != data)
        {
            Lightmapping.lightingDataAsset = previous;
            throw new InvalidOperationException("Unity did not assign the lighting data; previous lighting restored.");
        }
        int removed = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
                if (component != null && component.GetType().FullName == "RestoreLightProbes")
                {
                    Undo.DestroyObjectImmediate(component);
                    removed++;
                }
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("Attached " + probes.count + " recovered probes using " + output + ". Removed " + removed + " obsolete RestoreLightProbes components. Save and reopen the scene to load its native probes.", probes);
        return output;
    }
}
