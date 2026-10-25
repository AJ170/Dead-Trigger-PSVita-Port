using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Light probe recovery tools (Unity 2017, C# 4).
//
// 1. "Create Light Probe Group From Selection"
//    Select a LightProbes asset in the Project window. Creates (or replaces) a
//    LightProbeGroup in the open scene whose probe positions are copied from the asset.
//
// 2. "Attach Selected Probes to Scene"
//    Links the asset's baked data to the scene through a LightingData copy, as the
//    original tool did. Run this after step 1.
namespace LightProbeRecovery
{
	public static class RecoveredLightProbes
	{
		private const string GroupName = "Recovered Light Probes";
		private const string RecoveredFolder = "Assets/RecoveredLightProbes/";
		private const string LevelLightingFolder = "Assets/Levels/Test/";
		private const string TemplateFolder = "Tools/LightProbeRecovery/LightingTemplates/";

		// ---- Menu items -------------------------------------------------------

		[MenuItem("Tools/Light Probe Recovery/Create Light Probe Group From Selection")]
		public static void CreateGroupFromSelection()
		{
			LightProbes probes = Selection.activeObject as LightProbes;
			RunSafely(delegate { CreateGroup(probes); });
		}

		[MenuItem("Tools/Light Probe Recovery/Create Light Probe Group From Selection", true)]
		public static bool ValidateCreateGroupFromSelection()
		{
			return Selection.activeObject is LightProbes;
		}

		[MenuItem("Tools/Light Probe Recovery/Attach Selected Probes to Scene")]
		public static void AttachSelected()
		{
			LightProbes probes = Selection.activeObject as LightProbes;
			RunSafely(delegate { AttachLightingData(probes); });
		}

		[MenuItem("Tools/Light Probe Recovery/Attach Selected Probes to Scene", true)]
		public static bool ValidateAttachSelected()
		{
			return Selection.activeObject is LightProbes;
		}

		// ---- Step 1: group from asset ----------------------------------------

		public static LightProbeGroup CreateGroup(LightProbes probes)
		{
			ValidateProbes(probes);
			Scene scene = ValidateScene();

			// Replace any group created by an earlier run instead of stacking duplicates.
			GameObject existing = FindRootObject(scene, GroupName);
			if (existing != null)
			{
				Undo.DestroyObjectImmediate(existing);
			}

			GameObject go = new GameObject(GroupName);
			Undo.RegisterCreatedObjectUndo(go, "Create Light Probe Group");

			// Probe positions are world space. Keep the group at the identity transform
			// so its local positions match the baked positions exactly.
			go.transform.position = Vector3.zero;
			go.transform.rotation = Quaternion.identity;
			go.transform.localScale = Vector3.one;

			LightProbeGroup group = Undo.AddComponent<LightProbeGroup>(go);
			group.probePositions = probes.positions;

			EditorSceneManager.MarkSceneDirty(scene);
			// Do not change the selection here. The attach step needs the LightProbes
			// asset to stay selected, or its menu item greys out.
			// A LightProbeGroup stores positions only. The baked coefficients live in the
			// LightProbes asset and reach the scene through the attach step.
			Debug.Log("Created light probe group with " + probes.count + " probe positions from " +
				AssetDatabase.GetAssetPath(probes) + ". The group holds positions only; " +
				"run Attach Selected Probes to Scene to link the baked lighting data.", probes);
			return group;
		}

		// ---- Step 2: link baked data to the scene ----------------------------

		public static string AttachLightingData(LightProbes probes)
		{
			ValidateProbes(probes);
			Scene scene = ValidateScene();

			string probePath = AssetDatabase.GetAssetPath(probes);
			if (!probePath.StartsWith(RecoveredFolder, StringComparison.Ordinal))
			{
				throw new InvalidOperationException("Select a LightProbes asset under " + RecoveredFolder + ".");
			}

			string level = GetLevelName(probePath);
			string sceneGuid = AssetDatabase.AssetPathToGUID(scene.path);

			string text = ReadLightingText(level, sceneGuid);
			ValidateLightingText(text);

			// Only check the scene owner if the lighting data already names one.
			Match owner = Regex.Match(text, @"(?m)^  m_Scene:.*guid: ([0-9a-f]+)");
			if (owner.Success && owner.Groups[1].Value != sceneGuid)
			{
				throw new InvalidOperationException("The lighting template belongs to a different scene. Open the matching level scene.");
			}

			text = Regex.Replace(text, @"(?m)^  m_LightProbes:.*$",
				"  m_LightProbes: {fileID: 25800000, guid: " + AssetDatabase.AssetPathToGUID(probePath) + ", type: 2}");
			text = Regex.Replace(text, @"(?m)^  m_Scene:.*$",
				"  m_Scene: {fileID: 102900000, guid: " + sceneGuid + ", type: 3}");

			string folder = Path.GetDirectoryName(probePath).Replace('\\', '/');
			string output = AssetDatabase.GenerateUniqueAssetPath(folder + "/LightingData_Recovered.asset");
			File.WriteAllText(output, text);
			AssetDatabase.ImportAsset(output, ImportAssetOptions.ForceUpdate);

			LightingDataAsset data = AssetDatabase.LoadAssetAtPath<LightingDataAsset>(output);
			if (data == null)
			{
				throw new InvalidOperationException("Unity could not import the lighting data copy.");
			}

			LightingDataAsset previous = Lightmapping.lightingDataAsset;
			Lightmapping.lightingDataAsset = data;
			if (Lightmapping.lightingDataAsset != data)
			{
				Lightmapping.lightingDataAsset = previous;
				throw new InvalidOperationException("Unity did not assign the lighting data; previous lighting restored.");
			}

			int removed = RemoveObsoleteRestoreComponents(scene);
			EditorSceneManager.MarkSceneDirty(scene);

			Debug.Log("Attached " + probes.count + " recovered probes using " + output +
				". Removed " + removed + " obsolete RestoreLightProbes components. " +
				"Save and reopen the scene to load its native probes.", probes);
			return output;
		}

		// ---- Helpers ----------------------------------------------------------

		private static void RunSafely(Action action)
		{
			try
			{
				action();
			}
			catch (Exception e)
			{
				Debug.LogError("Light probe recovery failed: " + e.Message);
			}
		}

		private static void ValidateProbes(LightProbes probes)
		{
			if (probes == null || probes.count == 0)
			{
				throw new InvalidOperationException("Select a LightProbes asset that contains probes.");
			}
		}

		private static Scene ValidateScene()
		{
			Scene scene = SceneManager.GetActiveScene();
			if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.sceneCount != 1 || string.IsNullOrEmpty(scene.path))
			{
				throw new InvalidOperationException("Open one saved scene outside Play mode first.");
			}
			return scene;
		}

		private static string GetLevelName(string probePath)
		{
			return new DirectoryInfo(Path.GetDirectoryName(probePath)).Name;
		}

		private static GameObject FindRootObject(Scene scene, string name)
		{
			foreach (GameObject root in scene.GetRootGameObjects())
			{
				if (root.name == name)
				{
					return root;
				}
			}
			return null;
		}

		private static void CheckSnapshotOwner(string snapshot, string sceneGuid)
		{
			Match expectedOwner = Regex.Match(snapshot, @"(?m)^  m_Scene:.*guid: ([0-9a-f]+)");
			if (!expectedOwner.Success || expectedOwner.Groups[1].Value != sceneGuid)
			{
				throw new InvalidOperationException("The selected probes belong to a different level. Open the matching level scene.");
			}
		}

		// Source order, first match wins:
		//  1. The scene's current lighting data (keeps later lighting edits).
		//  2. The level's LightingData.asset under Assets/Levels/Test.
		//  3. The template YAML from the original machine, if it has been copied over.
		private static string ReadLightingText(string level, string sceneGuid)
		{
			LightingDataAsset previous = Lightmapping.lightingDataAsset;
			string previousPath = previous == null ? null : AssetDatabase.GetAssetPath(previous);
			if (!string.IsNullOrEmpty(previousPath))
			{
				string current = ReadTextIfExists(previousPath);
				if (current == null || !IsTextYaml(current))
				{
					throw new InvalidOperationException("The scene's current lighting data is binary or unreadable. Serialize it as text before attaching probes so its lighting edits can be preserved.");
				}
				return current;
			}

			string original = ReadTextIfExists(LevelLightingFolder + level + "/LightingData.asset");
			if (original != null && IsTextYaml(original))
			{
				return original;
			}

			string templatePath = GetTemplatePath(level);
			string template = ReadTextIfExists(templatePath);
			if (template != null)
			{
				CheckSnapshotOwner(template, sceneGuid);
				return template;
			}

			throw new InvalidOperationException(
				"No text lighting data found for this level. Assign a text LightingData asset to the scene, " +
				"or copy the level's lighting YAML to: " + templatePath);
		}

		private static string ReadTextIfExists(string path)
		{
			return File.Exists(path) ? File.ReadAllText(path) : null;
		}

		// The template folder sits in the project root, next to Assets, not inside Assets.
		private static string GetTemplatePath(string level)
		{
			string projectRoot = Directory.GetParent(Application.dataPath).FullName;
			return Path.Combine(Path.Combine(projectRoot, TemplateFolder), level + ".yaml");
		}

		private static bool IsTextYaml(string text)
		{
			return text.StartsWith("%YAML", StringComparison.Ordinal);
		}

		private static void ValidateLightingText(string text)
		{
			if (!IsTextYaml(text) ||
				Regex.Matches(text, @"(?m)^  m_LightProbes:.*$").Count != 1 ||
				Regex.Matches(text, @"(?m)^  m_Scene:.*$").Count != 1)
			{
				throw new InvalidOperationException("Expected a text LightingDataAsset with one scene and probe reference.");
			}
		}

		private static int RemoveObsoleteRestoreComponents(Scene scene)
		{
			int removed = 0;
			foreach (GameObject root in scene.GetRootGameObjects())
			{
				foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
				{
					if (component != null && component.GetType().FullName == "RestoreLightProbes")
					{
						Undo.DestroyObjectImmediate(component);
						removed++;
					}
				}
			}
			return removed;
		}
	}
}