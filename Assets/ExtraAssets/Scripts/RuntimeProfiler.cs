using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Diagnostics;

/// <summary>
/// Runtime profiler using a single Canvas Text object.
/// Much more performant on Vita and mobile platforms.
/// 
/// Setup:
/// 1. Create a Text element in your Canvas (UI → Text)
/// 2. Assign it to the "Display Text" field in the inspector
/// 3. Call RuntimeProfiler.Sample("Name", timeMs) from your code
/// 4. The profiler will update the Text object every frame
/// </summary>
public class RuntimeProfiler : MonoBehaviour
{
	private struct ProfileSample
	{
		public string name;
		public float totalTime;
		public int sampleCount;
		public float avgTime { get { return sampleCount > 0 ? totalTime / sampleCount : 0f; } }
	}

	[SerializeField]
	private Text m_DisplayText;

	private Dictionary<string, ProfileSample> m_Samples = new Dictionary<string, ProfileSample>();
	private System.Text.StringBuilder m_StringBuilder = new System.Text.StringBuilder();

	private float m_FrameTime;
	private float m_FPS;
	private int m_FrameCount;
	private float m_FPSUpdateTimer;
	private Stopwatch m_UpdatePhaseStopwatch = new Stopwatch();
	private float m_UpdatePhaseTime;

	private void Start()
	{
		if (m_DisplayText == null)
		{
			UnityEngine.Debug.LogError("RuntimeProfiler: Display Text field is not assigned! Please assign a Text object in the inspector.");
			enabled = false;
			return;
		}

		m_FPSUpdateTimer = 0f;
	}

	private void Update()
	{
		// Start measuring Update phase
		m_UpdatePhaseStopwatch.Restart();

		// Measure frame time
		m_FrameTime = Time.deltaTime;
		m_FrameCount++;

		// Update FPS every 0.5 seconds
		m_FPSUpdateTimer += Time.deltaTime;
		if (m_FPSUpdateTimer >= 0.5f)
		{
			m_FPS = m_FrameCount / m_FPSUpdateTimer;
			m_FrameCount = 0;
			m_FPSUpdateTimer = 0f;
		}

		// Update display text
		UpdateDisplay();

		// Clear samples for next frame
		m_Samples.Clear();
	}

	private void LateUpdate()
	{
		// Stop measuring and convert to milliseconds
		m_UpdatePhaseStopwatch.Stop();
		m_UpdatePhaseTime = (float)m_UpdatePhaseStopwatch.Elapsed.TotalMilliseconds;
	}

	private void UpdateDisplay()
	{
		m_StringBuilder.Length = 0;  // Clear StringBuilder

		// Header
		m_StringBuilder.AppendLine("=== RUNTIME PROFILER ===");

		// FPS
		m_StringBuilder.AppendLine(string.Format("FPS: {0:F1}", m_FPS));

		// Frame time
		m_StringBuilder.AppendLine(string.Format("Frame Time: {0:F2}ms (Target: 33.33ms @ 30fps)", m_FrameTime * 1000f));

		// Update phase time
		m_StringBuilder.AppendLine(string.Format("Update Phase: {0:F2}ms", m_UpdatePhaseTime));

		// Separator
		m_StringBuilder.AppendLine();

		// Sample data
		foreach (KeyValuePair<string, ProfileSample> kvp in m_Samples)
		{
			ProfileSample sample = kvp.Value;
			m_StringBuilder.AppendLine(
				string.Format("{0}: {1:F2}ms (avg: {2:F3}ms, {3}x)",
					sample.name,
					sample.totalTime,
					sample.avgTime,
					sample.sampleCount));
		}

		// Update the Text object
		m_DisplayText.text = m_StringBuilder.ToString();
	}

	/// <summary>
	/// Record a timing sample. Call this from your code.
	/// Usage: RuntimeProfiler.Sample("MyMethod", 0.005f);
	/// </summary>
	public static void Sample(string name, float timeMs)
	{
		if (instance == null) return;
		instance.RecordSample(name, timeMs);
	}

	private void RecordSample(string name, float timeMs)
	{
		if (!m_Samples.ContainsKey(name))
		{
			m_Samples[name] = new ProfileSample { name = name, totalTime = 0f, sampleCount = 0 };
		}

		ProfileSample sample = m_Samples[name];
		sample.totalTime += timeMs;
		sample.sampleCount++;
		m_Samples[name] = sample;
	}

	private static RuntimeProfiler instance;

	private void OnEnable()
	{
		if (instance != null && instance != this)
		{
			UnityEngine.Debug.LogWarning("Multiple RuntimeProfiler instances detected. Disabling this one.");
			enabled = false;
			return;
		}
		instance = this;
	}

	private void OnDisable()
	{
		if (instance == this)
			instance = null;
	}
}