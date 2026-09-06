using UnityEngine;
using UnityEditor;
using UnityEngine.AI;

public class FindNavMesh : MonoBehaviour
{
	[MenuItem("Tools/Find NavMesh Surface")]
	public static void FindNavMeshSurface()
	{
		// Find all NavMeshSurface components in the scene
		NavMeshSurface[] navMeshSurfaces = FindObjectsOfType<NavMeshSurface>();

		if (navMeshSurfaces.Length == 0)
		{
			Debug.LogWarning("No NavMeshSurface components found in the scene.");
			return;
		}

		Debug.Log("=== NAVMESH SURFACE FOUND ===");
		Debug.Log("Total: " + navMeshSurfaces.Length);
		Debug.Log("");

		// Print each one
		for (int i = 0; i < navMeshSurfaces.Length; i++)
		{
			NavMeshSurface surface = navMeshSurfaces[i];
			string objectPath = GetGameObjectPath(surface.gameObject);

			Debug.Log((i + 1) + ". " + surface.gameObject.name + 
				" (Path: " + objectPath + ")" +
				" [Agent Type ID: " + surface.agentTypeID + "]");
		}

		Debug.Log("");
		Debug.Log("=== END ===");
	}

	/// <summary>
	/// Gets the full hierarchy path to a GameObject (e.g., "Parent/Child/Object")
	/// </summary>
	private static string GetGameObjectPath(GameObject obj)
	{
		string path = obj.name;
		Transform current = obj.transform.parent;

		while (current != null)
		{
			path = current.name + "/" + path;
			current = current.parent;
		}

		return path;
	}
}