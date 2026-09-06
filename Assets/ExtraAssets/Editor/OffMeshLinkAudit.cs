using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using System.Collections.Generic;

public class OffMeshLinkAudit : MonoBehaviour
{
	private class OffMeshLinkIssue
	{
		public OffMeshLink link;
		public string parentName;
		public string childName;
		public List<string> issues = new List<string>();
	}

	[MenuItem("Tools/Audit Off-Mesh Links")]
	public static void AuditOffMeshLinks()
	{
		OffMeshLink[] allLinks = FindObjectsOfType<OffMeshLink>();

		if (allLinks.Length == 0)
		{
			Debug.Log("No Off-Mesh Links found in the scene.");
			return;
		}

		List<OffMeshLinkIssue> problematicLinks = new List<OffMeshLinkIssue>();

		Debug.Log("=== OFF-MESH LINK AUDIT ===");
		Debug.Log("Scanning " + allLinks.Length + " off-mesh links...");
		Debug.Log("");

		for (int i = 0; i < allLinks.Length; i++)
		{
			OffMeshLink link = allLinks[i];
			OffMeshLinkIssue issue = new OffMeshLinkIssue();
			issue.link = link;
			issue.childName = link.gameObject.name;
			issue.parentName = link.gameObject.transform.parent != null ? 
				link.gameObject.transform.parent.name : "[ROOT]";

			// Check for issues
			CheckOffMeshLinkIssues(link, issue);

			if (issue.issues.Count > 0)
			{
				problematicLinks.Add(issue);
			}
		}

		// Report results
		if (problematicLinks.Count == 0)
		{
			Debug.Log("✓ All off-mesh links appear to be correctly configured!");
		}
		else
		{
			Debug.LogWarning("Found " + problematicLinks.Count + " problematic off-mesh links:");
			Debug.Log("");

			for (int i = 0; i < problematicLinks.Count; i++)
			{
				OffMeshLinkIssue issue = problematicLinks[i];
				Debug.LogWarning((i + 1) + ". " + issue.parentName + " → " + issue.childName);

				for (int j = 0; j < issue.issues.Count; j++)
				{
					Debug.LogWarning("   - " + issue.issues[j]);
				}
				Debug.Log("");
			}
		}

		Debug.Log("=== AUDIT COMPLETE ===");
	}

	private static void CheckOffMeshLinkIssues(OffMeshLink link, OffMeshLinkIssue issue)
	{
		// Check 1: Start and end points are assigned
		if (link.startTransform == null)
		{
			issue.issues.Add("Start Transform is not assigned");
		}
		if (link.endTransform == null)
		{
			issue.issues.Add("End Transform is not assigned");
		}

		// If we don't have both points, can't check further
		if (link.startTransform == null || link.endTransform == null)
		{
			return;
		}

		Vector3 startPos = link.startTransform.position;
		Vector3 endPos = link.endTransform.position;

		// Check 2: Start and end points aren't at the same location
		if (Vector3.Distance(startPos, endPos) < 0.01f)
		{
			issue.issues.Add("Start and end points are at the same location");
		}

		// Check 3: Distance between points is reasonable (not too far)
		float distance = Vector3.Distance(startPos, endPos);
		if (distance > 100f)
		{
			issue.issues.Add("Start and end points are very far apart (" + distance.ToString("F1") + "m) - may indicate incorrect setup");
		}

		// Check 4: Points are on the NavMesh
		NavMeshHit hit;
		if (!NavMesh.SamplePosition(startPos, out hit, 2f, NavMesh.AllAreas))
		{
			issue.issues.Add("Start point is not on NavMesh");
		}
		if (!NavMesh.SamplePosition(endPos, out hit, 2f, NavMesh.AllAreas))
		{
			issue.issues.Add("End point is not on NavMesh");
		}

		// Check 5: Check if link is enabled
		if (!link.enabled)
		{
			issue.issues.Add("Off-mesh link is disabled");
		}

		// Check 6: Vertical drop/climb isn't too extreme
		float verticalDiff = Mathf.Abs(startPos.y - endPos.y);
		if (verticalDiff > 10f)
		{
			issue.issues.Add("Large vertical difference (" + verticalDiff.ToString("F1") + "m) between start and end - may be intentional but verify");
		}

		// Check 7: Both points should have some Y separation (not floating way above ground)
		if (startPos.y > 50f || endPos.y > 50f)
		{
			issue.issues.Add("One or both points are very high in the world - may indicate floating geometry");
		}

		// Check 8: Check if both points are already connected via NavMesh (redundant link)
		NavMeshPath path = new NavMeshPath();
		if (NavMesh.CalculatePath(startPos, endPos, NavMesh.AllAreas, path))
		{
			// Path exists - check if it's a direct path (doesn't require off-mesh link)
			if (path.status == NavMeshPathStatus.PathComplete)
			{
				// Check if path corners suggest a direct connection (only 2 corners = direct line)
				if (path.corners.Length <= 2)
				{
					issue.issues.Add("Start and end are already connected via NavMesh - off-mesh link may be redundant or spanning incorrect gap");
				}
			}
		}
	}
}