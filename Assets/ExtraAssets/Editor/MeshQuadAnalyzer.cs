using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Analyzes a mesh and identifies planar quads (4-vertex faces).
/// Outputs debug information about quad count and distribution.
/// Also creates box colliders for each quad found.
/// 
/// Usage:
/// 1. Window → Mesh Quad Analyzer
/// 2. Assign mesh and click Analyze
/// 3. Or select object and click Create Colliders
/// </summary>
public class MeshQuadAnalyzer : EditorWindow
{
	private float m_PlanarityThreshold = 0.01f;
	private float m_BoxColliderThickness = 0.05f;
	private const float m_MinThickness = 0.1f;  // Threshold for detecting planar quads

	[MenuItem("Window/Mesh Quad Analyzer")]
	public static void ShowWindow()
	{
		GetWindow<MeshQuadAnalyzer>("Mesh Quad Analyzer");
	}

	private Mesh GetSelectedMesh()
	{
		if (Selection.activeGameObject == null)
			return null;

		// Try to get mesh from MeshFilter
		MeshFilter meshFilter = Selection.activeGameObject.GetComponent<MeshFilter>();
		if (meshFilter != null && meshFilter.sharedMesh != null)
			return meshFilter.sharedMesh;

		// Try to get mesh from MeshCollider
		MeshCollider meshCollider = Selection.activeGameObject.GetComponent<MeshCollider>();
		if (meshCollider != null && meshCollider.sharedMesh != null)
			return meshCollider.sharedMesh;

		return null;
	}

	private void OnGUI()
	{
		GUILayout.Label("Mesh Quad Analyzer", EditorStyles.boldLabel);
		GUILayout.Space(10);

		GameObject selectedObject = Selection.activeGameObject;
		if (selectedObject == null)
		{
			EditorGUILayout.HelpBox("Please select a GameObject with a Mesh or MeshCollider", MessageType.Warning);
			return;
		}

		Mesh selectedMesh = GetSelectedMesh();
		if (selectedMesh == null)
		{
			EditorGUILayout.HelpBox("Selected object has no Mesh or MeshCollider", MessageType.Warning);
			return;
		}

		EditorGUILayout.LabelField("Selected: " + selectedObject.name);
		EditorGUILayout.LabelField("Mesh: " + selectedMesh.name);
		GUILayout.Space(10);

		m_PlanarityThreshold = EditorGUILayout.FloatField("Planarity Threshold", m_PlanarityThreshold);
		m_BoxColliderThickness = EditorGUILayout.FloatField("Box Collider Thickness", m_BoxColliderThickness);

		GUILayout.Space(10);

		if (GUILayout.Button("Analyze Mesh", GUILayout.Height(40)))
		{
			AnalyzeMesh(selectedMesh);
		}

		GUILayout.Space(5);

		if (GUILayout.Button("Create Colliders From Quads", GUILayout.Height(40)))
		{
			CreateCollidersFromQuads(selectedMesh, selectedObject);
		}

		GUILayout.Space(10);
		EditorGUILayout.HelpBox("Analyze: Identifies quads in mesh.\nCreate Colliders: Generates box colliders for each quad.", MessageType.Info);
	}

	private void AnalyzeMesh(Mesh analyzeMesh)
	{
		if (analyzeMesh == null)
			return;

		GameObject selectedObject = Selection.activeGameObject;
		if (selectedObject == null)
			return;

		Vector3[] vertices = analyzeMesh.vertices;
		int[] triangles = analyzeMesh.triangles;

		// Convert vertices to world space
		Vector3[] worldVertices = new Vector3[vertices.Length];
		for (int i = 0; i < vertices.Length; i++)
		{
			worldVertices[i] = selectedObject.transform.TransformPoint(vertices[i]);
		}

		Debug.Log("=== MESH QUAD ANALYSIS ===");
		Debug.Log("Mesh: " + analyzeMesh.name);
		Debug.Log("Total Vertices: " + worldVertices.Length);
		Debug.Log("Total Triangles: " + (triangles.Length / 3));
		Debug.Log("");

		// Find all quads
		List<int[]> quads = FindAllQuads(worldVertices, triangles);

		Debug.Log("RESULTS:");
		Debug.Log("Total Quads Found: " + quads.Count);
		Debug.Log("Coverage: " + (quads.Count * 4f / (triangles.Length / 3) * 100f).ToString("F1") + "% of mesh");
		Debug.Log("");

		// Analyze quad sizes
		if (quads.Count > 0)
		{
			float smallestQuad = float.MaxValue;
			float largestQuad = 0;
			float totalArea = 0;

			for (int i = 0; i < quads.Count; i++)
			{
				float area = CalculateQuadArea(worldVertices, quads[i]);
				totalArea += area;
				smallestQuad = Mathf.Min(smallestQuad, area);
				largestQuad = Mathf.Max(largestQuad, area);
			}

			float avgArea = totalArea / quads.Count;

			Debug.Log("QUAD STATISTICS:");
			Debug.Log("Smallest Quad Area: " + smallestQuad.ToString("F3") + " m²");
			Debug.Log("Largest Quad Area: " + largestQuad.ToString("F3") + " m²");
			Debug.Log("Average Quad Area: " + avgArea.ToString("F3") + " m²");
			Debug.Log("Total Coverage Area: " + totalArea.ToString("F1") + " m²");
			Debug.Log("");

			// Show first few quads as examples
			Debug.Log("FIRST 10 QUADS (vertex indices):");
			for (int i = 0; i < Mathf.Min(10, quads.Count); i++)
			{
				int[] quad = quads[i];
				float area = CalculateQuadArea(worldVertices, quad);
				Debug.Log("  Quad " + i + ": [" + quad[0] + ", " + quad[1] + ", " + quad[2] + ", " + quad[3] + "] Area: " + area.ToString("F3"));
			}
		}
		else
		{
			Debug.Log("No quads found in mesh!");
		}

		Debug.Log("=== END ANALYSIS ===");
	}

	private void CreateCollidersFromQuads(Mesh analyzeMesh, GameObject targetObject)
	{
		if (analyzeMesh == null || targetObject == null)
			return;

		Vector3[] vertices = analyzeMesh.vertices;
		int[] triangles = analyzeMesh.triangles;

		// Convert vertices to world space
		Vector3[] worldVertices = new Vector3[vertices.Length];
		for (int i = 0; i < vertices.Length; i++)
		{
			worldVertices[i] = targetObject.transform.TransformPoint(vertices[i]);
		}

		// STEP 1: Find ALL connected components of triangles first
		List<List<int>> allComponents = FindAllTriangleComponents(triangles);

		// STEP 2: Separate complex submeshes from simple triangles
		List<List<int>> complexSubmeshes = new List<List<int>>();
		List<bool> usedTriangles = new List<bool>();
		for (int i = 0; i < triangles.Length / 3; i++)
		{
			usedTriangles.Add(false);
		}

		foreach (List<int> component in allComponents)
		{
			// Count unique vertices in this component
			HashSet<int> uniqueVertices = new HashSet<int>();
			for (int i = 0; i < component.Count; i++)
			{
				int triIdx = component[i];
				uniqueVertices.Add(triangles[triIdx * 3]);
				uniqueVertices.Add(triangles[triIdx * 3 + 1]);
				uniqueVertices.Add(triangles[triIdx * 3 + 2]);
			}

			// If more than 4 unique vertices, it's complex - mark as used
			if (uniqueVertices.Count > 4)
			{
				complexSubmeshes.Add(component);
				for (int i = 0; i < component.Count; i++)
				{
					usedTriangles[component[i]] = true;
				}
			}
		}

		// STEP 3: Find quads only from remaining simple triangles
		List<int[]> quads = FindAllQuadsWithTracking(worldVertices, triangles, usedTriangles);

		// Get parent
		Transform parent = targetObject.transform.parent;

		// STEP 4: Create box colliders for quads
		int createdCount = 0;
		List<int[]> nonCartesianQuads = new List<int[]>();
		for (int i = 0; i < quads.Count; i++)
		{
			if (!CreateBoxColliderForQuad(worldVertices, quads[i], parent, targetObject.name))
			{
				// This quad is non-cartesian, save it for mesh collider
				nonCartesianQuads.Add(quads[i]);
			}
			else
			{
				createdCount++;
			}
		}

		// STEP 5: Create mesh colliders for non-cartesian quads
		int nonCartesianMeshCollidersCreated = 0;
		if (nonCartesianQuads.Count > 0)
		{
			nonCartesianMeshCollidersCreated = CreateMeshColliderForQuads(analyzeMesh, nonCartesianQuads, triangles, targetObject, parent);
		}

		// STEP 6: Create mesh colliders for complex submeshes
		int complexMeshCollidersCreated = 0;
		for (int i = 0; i < complexSubmeshes.Count; i++)
		{
			if (CreateMeshColliderForSubmesh(analyzeMesh, complexSubmeshes[i], triangles, targetObject, parent, i))
			{
				complexMeshCollidersCreated++;
			}
		}

		int totalMeshColliders = nonCartesianMeshCollidersCreated + complexMeshCollidersCreated;
		if (totalMeshColliders > 0)
		{
			Debug.Log("Created " + totalMeshColliders + " mesh colliders for complex geometry");
		}

		Debug.Log("Created " + createdCount + " box colliders");
		EditorUtility.DisplayDialog("Success", "Created " + createdCount + " box colliders" + 
			(totalMeshColliders > 0 ? " and " + totalMeshColliders + " mesh colliders" : ""), "OK");
	}

	private List<List<int>> FindAllTriangleComponents(int[] triangles)
	{
		List<List<int>> components = new List<List<int>>();
		List<bool> visited = new List<bool>();
		for (int i = 0; i < triangles.Length / 3; i++)
		{
			visited.Add(false);
		}

		// For each unvisited triangle, find its connected component
		for (int i = 0; i < triangles.Length / 3; i++)
		{
			if (!visited[i])
			{
				List<int> component = new List<int>();
				FloodFillTrianglesForComponent(triangles, visited, i, component);
				components.Add(component);
			}
		}

		return components;
	}

	private void FloodFillTrianglesForComponent(int[] triangles, List<bool> visited, int startTriIdx, List<int> component)
	{
		Queue<int> queue = new Queue<int>();
		queue.Enqueue(startTriIdx);
		visited[startTriIdx] = true;

		while (queue.Count > 0)
		{
			int triIdx = queue.Dequeue();
			component.Add(triIdx);

			// Get vertices of this triangle
			int v0 = triangles[triIdx * 3];
			int v1 = triangles[triIdx * 3 + 1];
			int v2 = triangles[triIdx * 3 + 2];

			// Find all adjacent triangles (that share a vertex)
			for (int j = 0; j < triangles.Length / 3; j++)
			{
				if (!visited[j])
				{
					int ov0 = triangles[j * 3];
					int ov1 = triangles[j * 3 + 1];
					int ov2 = triangles[j * 3 + 2];

					// Check if they share any vertices
					if ((v0 == ov0 || v0 == ov1 || v0 == ov2) ||
						(v1 == ov0 || v1 == ov1 || v1 == ov2) ||
						(v2 == ov0 || v2 == ov1 || v2 == ov2))
					{
						visited[j] = true;
						queue.Enqueue(j);
					}
				}
			}
		}
	}

	private List<int[]> FindAllQuadsWithTracking(Vector3[] vertices, int[] triangles, List<bool> usedTriangles)
	{
		List<int[]> quads = new List<int[]>();

		// For each triangle, try to find an adjacent coplanar triangle
		for (int i = 0; i < triangles.Length; i += 3)
		{
			int triIndex = i / 3;
			if (usedTriangles[triIndex])
				continue;

			int i0 = triangles[i];
			int i1 = triangles[i + 1];
			int i2 = triangles[i + 2];

			Vector3 normalI = GetTriangleNormal(vertices[i0], vertices[i1], vertices[i2]);

			// Look for adjacent coplanar triangle
			for (int j = i + 3; j < triangles.Length; j += 3)
			{
				int triIndexJ = j / 3;
				if (usedTriangles[triIndexJ])
					continue;

				int j0 = triangles[j];
				int j1 = triangles[j + 1];
				int j2 = triangles[j + 2];

				// Check if they share an edge
				int[] sharedVerts = GetSharedVertices(i0, i1, i2, j0, j1, j2);
				if (sharedVerts != null && sharedVerts.Length == 2)
				{
					// Check if normals match (coplanar)
					Vector3 normalJ = GetTriangleNormal(vertices[j0], vertices[j1], vertices[j2]);
					float dotProduct = Vector3.Dot(normalI, normalJ);

					if (dotProduct > (1.0f - m_PlanarityThreshold))
					{
						// Found a coplanar pair! Form quad
						int[] quad = new int[4];
						FormQuad(i0, i1, i2, j0, j1, j2, quad);
						quads.Add(quad);
						usedTriangles[triIndex] = true;
						usedTriangles[triIndexJ] = true;
						break;
					}
				}
			}
		}

		return quads;
	}

	private List<List<int>> FindConnectedTriangleComponents(int[] triangles, List<int> triangleIndices)
	{
		List<List<int>> components = new List<List<int>>();
		List<bool> visited = new List<bool>();
		for (int i = 0; i < triangleIndices.Count; i++)
		{
			visited.Add(false);
		}

		// For each unvisited triangle, do a flood fill to find connected component
		for (int i = 0; i < triangleIndices.Count; i++)
		{
			if (!visited[i])
			{
				List<int> component = new List<int>();
				FloodFillTriangles(triangles, triangleIndices, visited, i, component);
				components.Add(component);
			}
		}

		return components;
	}

	private void FloodFillTriangles(int[] triangles, List<int> triangleIndices, List<bool> visited, int startIdx, List<int> component)
	{
		Queue<int> queue = new Queue<int>();
		queue.Enqueue(startIdx);
		visited[startIdx] = true;

		while (queue.Count > 0)
		{
			int idx = queue.Dequeue();
			int triIdx = triangleIndices[idx];
			component.Add(triIdx);

			// Get vertices of this triangle
			int v0 = triangles[triIdx * 3];
			int v1 = triangles[triIdx * 3 + 1];
			int v2 = triangles[triIdx * 3 + 2];

			// Find all adjacent triangles (that share a vertex)
			for (int j = 0; j < triangleIndices.Count; j++)
			{
				if (!visited[j])
				{
					int otherTriIdx = triangleIndices[j];
					int ov0 = triangles[otherTriIdx * 3];
					int ov1 = triangles[otherTriIdx * 3 + 1];
					int ov2 = triangles[otherTriIdx * 3 + 2];

					// Check if they share any vertices
					if ((v0 == ov0 || v0 == ov1 || v0 == ov2) ||
						(v1 == ov0 || v1 == ov1 || v1 == ov2) ||
						(v2 == ov0 || v2 == ov1 || v2 == ov2))
					{
						visited[j] = true;
						queue.Enqueue(j);
					}
				}
			}
		}
	}

	private int CreateMeshColliderForQuads(Mesh sourceMesh, List<int[]> quads, int[] allTriangles, GameObject sourceObject, Transform parent)
	{
		if (quads.Count == 0)
			return 0;

		// Collect all unique vertices from the quads
		HashSet<int> uniqueVertexIndices = new HashSet<int>();
		for (int i = 0; i < quads.Count; i++)
		{
			int[] quad = quads[i];
			uniqueVertexIndices.Add(quad[0]);
			uniqueVertexIndices.Add(quad[1]);
			uniqueVertexIndices.Add(quad[2]);
			uniqueVertexIndices.Add(quad[3]);
		}

		// Create new mesh
		Mesh submesh = new Mesh();
		submesh.name = sourceMesh.name + "_NonCartesian";

		Vector3[] allVertices = sourceMesh.vertices;
		Dictionary<int, int> vertexMap = new Dictionary<int, int>();
		List<Vector3> newVertices = new List<Vector3>();

		// Build new vertex list and create triangles from quads
		List<int> newTriangles = new List<int>();
		for (int i = 0; i < quads.Count; i++)
		{
			int[] quad = quads[i];

			// Map quad vertices to new vertex list
			int[] mappedQuad = new int[4];
			for (int j = 0; j < 4; j++)
			{
				if (!vertexMap.ContainsKey(quad[j]))
				{
					vertexMap[quad[j]] = newVertices.Count;
					newVertices.Add(allVertices[quad[j]]);
				}
				mappedQuad[j] = vertexMap[quad[j]];
			}

			// Create two triangles from the quad
			newTriangles.Add(mappedQuad[0]);
			newTriangles.Add(mappedQuad[1]);
			newTriangles.Add(mappedQuad[2]);

			newTriangles.Add(mappedQuad[2]);
			newTriangles.Add(mappedQuad[3]);
			newTriangles.Add(mappedQuad[0]);
		}

		submesh.vertices = newVertices.ToArray();
		submesh.triangles = newTriangles.ToArray();
		submesh.RecalculateNormals();
		submesh.RecalculateBounds();

		// Save mesh asset
		string levelName = sourceObject.name;
		string saveFolder = "Assets/LevelColliders/" + levelName;
		SaveMeshAsset(submesh, saveFolder);

		// Create collider GameObject
		GameObject meshColliderObj = new GameObject("NonCartesianCollider");
		meshColliderObj.transform.SetParent(parent, false);

		MeshCollider meshCollider = meshColliderObj.AddComponent<MeshCollider>();
		meshCollider.convex = false;
		meshCollider.sharedMesh = submesh;

		Debug.Log("Created mesh collider for " + quads.Count + " non-cartesian quads (" + uniqueVertexIndices.Count + " unique vertices)");
		return 1;
	}

	private bool CreateMeshColliderForSubmesh(Mesh sourceMesh, List<int> submeshTriangleIndices, int[] allTriangles, GameObject sourceObject, Transform parent, int submeshIndex)
	{
		if (submeshTriangleIndices.Count == 0)
			return false;

		// Count unique vertices in this submesh
		HashSet<int> uniqueVertices = new HashSet<int>();
		for (int i = 0; i < submeshTriangleIndices.Count; i++)
		{
			int triIdx = submeshTriangleIndices[i];
			uniqueVertices.Add(allTriangles[triIdx * 3]);
			uniqueVertices.Add(allTriangles[triIdx * 3 + 1]);
			uniqueVertices.Add(allTriangles[triIdx * 3 + 2]);
		}

		// Only create mesh collider for submeshes with > 4 vertices
		if (uniqueVertices.Count <= 4)
		{
			return false;
		}

		// Create new mesh from submesh triangles
		Mesh submesh = new Mesh();
		submesh.name = sourceMesh.name + "_Submesh_" + submeshIndex;

		Vector3[] allVertices = sourceMesh.vertices;
		Dictionary<int, int> vertexMap = new Dictionary<int, int>();
		List<Vector3> newVertices = new List<Vector3>();

		// Remap triangle indices
		int[] remappedTriangles = new int[submeshTriangleIndices.Count * 3];
		for (int i = 0; i < submeshTriangleIndices.Count; i++)
		{
			int triIdx = submeshTriangleIndices[i];
			for (int j = 0; j < 3; j++)
			{
				int originalVertIdx = allTriangles[triIdx * 3 + j];
				if (!vertexMap.ContainsKey(originalVertIdx))
				{
					vertexMap[originalVertIdx] = newVertices.Count;
					newVertices.Add(allVertices[originalVertIdx]);
				}
				remappedTriangles[i * 3 + j] = vertexMap[originalVertIdx];
			}
		}

		submesh.vertices = newVertices.ToArray();
		submesh.triangles = remappedTriangles;
		submesh.RecalculateNormals();
		submesh.RecalculateBounds();

		// Save mesh asset
		string levelName = sourceObject.name;
		string saveFolder = "Assets/LevelColliders/" + levelName;
		SaveMeshAsset(submesh, saveFolder);

		// Create collider GameObject
		GameObject meshColliderObj = new GameObject("SubmeshCollider_" + submeshIndex);
		meshColliderObj.transform.SetParent(parent, false);

		MeshCollider meshCollider = meshColliderObj.AddComponent<MeshCollider>();
		meshCollider.convex = false;
		meshCollider.sharedMesh = submesh;

		Debug.Log("Created mesh collider for submesh " + submeshIndex + " (" + uniqueVertices.Count + " vertices)");
		return true;
	}

	private void SaveMeshAsset(Mesh mesh, string folderPath)
	{
#if UNITY_EDITOR
		// Create folder if it doesn't exist
		if (!System.IO.Directory.Exists(folderPath))
		{
			System.IO.Directory.CreateDirectory(folderPath);
		}

		// Generate unique filename
		string fileName = mesh.name + ".asset";
		string fullPath = folderPath + "/" + fileName;
		int counter = 1;
		while (System.IO.File.Exists(fullPath))
		{
			fullPath = folderPath + "/" + mesh.name + "_" + counter + ".asset";
			counter++;
		}

		// Save mesh as asset
		AssetDatabase.CreateAsset(mesh, fullPath);
		AssetDatabase.SaveAssets();
		Debug.Log("Saved mesh asset: " + fullPath);
#endif
	}

	private List<int[]> FindAllQuads(Vector3[] vertices, int[] triangles)
	{
		List<int[]> quads = new List<int[]>();
		List<bool> usedTriangles = new List<bool>();

		for (int i = 0; i < triangles.Length / 3; i++)
		{
			usedTriangles.Add(false);
		}

		// For each triangle, try to find an adjacent coplanar triangle
		for (int i = 0; i < triangles.Length; i += 3)
		{
			int triIndex = i / 3;
			if (usedTriangles[triIndex])
				continue;

			int i0 = triangles[i];
			int i1 = triangles[i + 1];
			int i2 = triangles[i + 2];

			Vector3 normalI = GetTriangleNormal(vertices[i0], vertices[i1], vertices[i2]);

			// Look for adjacent coplanar triangle
			for (int j = i + 3; j < triangles.Length; j += 3)
			{
				int triIndexJ = j / 3;
				if (usedTriangles[triIndexJ])
					continue;

				int j0 = triangles[j];
				int j1 = triangles[j + 1];
				int j2 = triangles[j + 2];

				// Check if they share an edge
				int[] sharedVerts = GetSharedVertices(i0, i1, i2, j0, j1, j2);
				if (sharedVerts != null && sharedVerts.Length == 2)
				{
					// Check if normals match (coplanar)
					Vector3 normalJ = GetTriangleNormal(vertices[j0], vertices[j1], vertices[j2]);
					float dotProduct = Vector3.Dot(normalI, normalJ);

					if (dotProduct > (1.0f - m_PlanarityThreshold))
					{
						// Found a coplanar pair! Form quad
						int[] quad = new int[4];
						FormQuad(i0, i1, i2, j0, j1, j2, quad);
						quads.Add(quad);
						usedTriangles[triIndex] = true;
						usedTriangles[triIndexJ] = true;
						break;
					}
				}
			}
		}

		return quads;
	}

	private Vector3 GetTriangleNormal(Vector3 v0, Vector3 v1, Vector3 v2)
	{
		Vector3 edge1 = v1 - v0;
		Vector3 edge2 = v2 - v0;
		return Vector3.Cross(edge1, edge2).normalized;
	}

	private int[] GetSharedVertices(int i0, int i1, int i2, int j0, int j1, int j2)
	{
		// Find vertices shared by both triangles
		List<int> shared = new List<int>();

		if (i0 == j0 || i0 == j1 || i0 == j2) shared.Add(i0);
		if (i1 == j0 || i1 == j1 || i1 == j2) shared.Add(i1);
		if (i2 == j0 || i2 == j1 || i2 == j2) shared.Add(i2);

		if (shared.Count == 2)
		{
			return shared.ToArray();
		}
		return null;
	}

	private void FormQuad(int i0, int i1, int i2, int j0, int j1, int j2, int[] quad)
	{
		// Get the two shared vertices
		int shared1 = -1, shared2 = -1;
		int unique1 = -1, unique2 = -1;

		// Find shared vertices from triangle i
		if (i0 == j0 || i0 == j1 || i0 == j2)
		{
			if (shared1 == -1) shared1 = i0;
			else if (shared2 == -1) shared2 = i0;
		}
		if (i1 == j0 || i1 == j1 || i1 == j2)
		{
			if (shared1 == -1) shared1 = i1;
			else if (shared2 == -1) shared2 = i1;
		}
		if (i2 == j0 || i2 == j1 || i2 == j2)
		{
			if (shared1 == -1) shared1 = i2;
			else if (shared2 == -1) shared2 = i2;
		}

		// Get unique vertices
		if (i0 != shared1 && i0 != shared2) unique1 = i0;
		else if (i1 != shared1 && i1 != shared2) unique1 = i1;
		else unique1 = i2;

		if (j0 != shared1 && j0 != shared2) unique2 = j0;
		else if (j1 != shared1 && j1 != shared2) unique2 = j1;
		else unique2 = j2;

		// Store quad
		quad[0] = shared1;
		quad[1] = unique1;
		quad[2] = shared2;
		quad[3] = unique2;
	}

	private float CalculateQuadArea(Vector3[] vertices, int[] quad)
	{
		Vector3 v0 = vertices[quad[0]];
		Vector3 v1 = vertices[quad[1]];
		Vector3 v2 = vertices[quad[2]];
		Vector3 v3 = vertices[quad[3]];

		// Split quad into two triangles and sum their areas
		Vector3 tri1Edge1 = v1 - v0;
		Vector3 tri1Edge2 = v2 - v0;
		float tri1Area = Vector3.Cross(tri1Edge1, tri1Edge2).magnitude * 0.5f;

		Vector3 tri2Edge1 = v2 - v0;
		Vector3 tri2Edge2 = v3 - v0;
		float tri2Area = Vector3.Cross(tri2Edge1, tri2Edge2).magnitude * 0.5f;

		return tri1Area + tri2Area;
	}

	private bool CreateBoxColliderForQuad(Vector3[] vertices, int[] quad, Transform parent, string sourceName)
	{
		Vector3 v0 = vertices[quad[0]];
		Vector3 v1 = vertices[quad[1]];
		Vector3 v2 = vertices[quad[2]];
		Vector3 v3 = vertices[quad[3]];

		// Create bounds from quad vertices
		Bounds quadBounds = new Bounds(v0, Vector3.zero);
		quadBounds.Encapsulate(v1);
		quadBounds.Encapsulate(v2);
		quadBounds.Encapsulate(v3);

		// Get center and size from bounds
		Vector3 worldCenter = quadBounds.center;
		Vector3 boundsSize = quadBounds.size;

		// Check if quad lies on a cartesian plane (one dimension is below threshold)
		if (boundsSize.x < m_MinThickness || boundsSize.y < m_MinThickness || boundsSize.z < m_MinThickness)
		{
			// We've got a coplanar quad on a cartesian plane, which is what we want!
			boundsSize.x = Mathf.Max(boundsSize.x, m_MinThickness);
			boundsSize.y = Mathf.Max(boundsSize.y, m_MinThickness);
			boundsSize.z = Mathf.Max(boundsSize.z, m_MinThickness);
		}
		else
		{
			// Not a planar quad - skip it, it will be handled as non-planar geometry
			return false;
		}

		// Create GameObject
		GameObject colliderObj = new GameObject("QuadCollider_" + quad[0] + "_" + quad[1] + "_" + quad[2] + "_" + quad[3]);

		// Set position in world space
		colliderObj.transform.position = worldCenter;

		// Set rotation to identity (no rotation)
		colliderObj.transform.rotation = Quaternion.identity;

		// Set parent, maintaining world position
		colliderObj.transform.SetParent(parent, true);

		// Add box collider sized to bounds
		BoxCollider collider = colliderObj.AddComponent<BoxCollider>();
		collider.size = boundsSize;
		collider.center = Vector3.zero;

		return true;
	}
}

#endif