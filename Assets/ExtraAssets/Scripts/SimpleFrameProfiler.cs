using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Simple frame time profiler using camera disable trick.
/// Every 10 seconds, disables the main camera for 0.5 seconds to measure CPU vs rendering time.
/// 
/// Logic:
/// - Frame Time (normal) = CPU + GPU + Overhead
/// - Frame Time (camera off) = CPU + Overhead (no rendering)
/// - Rendering Time = Normal - Camera Off
/// - CPU Time = Camera Off
/// 
/// Persistent across level loads. Displays to a Text object.
/// </summary>
public class SimpleFrameProfiler : MonoBehaviour
{
	[SerializeField]
	private Text m_DisplayText;

	private const int MOVING_AVERAGE_SIZE = 30;  // Smooth over 30 frames
	private float[] m_FrameTimeHistory = new float[MOVING_AVERAGE_SIZE];
	private int m_FrameTimeHistoryIndex = 0;
	private float m_FrameTimeMovingAverage = 0f;

	private float m_TimeSinceLastMeasure = 0f;
	private const float MEASURE_INTERVAL = 3f;  // Measure every 5 seconds (more frequent samples)
	private const float CAMERA_OFF_DURATION = 1f;  // Quick 0.2s snapshot (~6 frames at 30fps)
	private const int FRAMES_TO_SKIP = 1;  // Skip first frame after camera disable (minimal overhead)

	private bool m_IsMeasuring = false;
	private float m_MeasureStartTime = 0f;
	private int m_MeasureFrameCount = 0;
	private System.Collections.Generic.List<float> m_MeasuredFrameTimes = new System.Collections.Generic.List<float>();

	private float m_LastNormalFrameTime = 0f;
	private float m_LastCpuFrameTime = 0f;
	private float m_RenderingTimeMs = 0f;

	private System.Text.StringBuilder m_StringBuilder = new System.Text.StringBuilder();
	private Camera m_MainCamera;

	private void Awake()
	{
		// Make this script persistent across level loads
		DontDestroyOnLoad(gameObject);
	}

	private void Start()
	{
		if (m_DisplayText == null)
		{
			Debug.LogError("SimpleFrameProfiler: Display Text field is not assigned!");
			enabled = false;
			return;
		}

		// Find main camera
		m_MainCamera = Camera.main;
		if (m_MainCamera == null)
		{
			Debug.LogError("SimpleFrameProfiler: Main Camera not found!");
			enabled = false;
			return;
		}

		m_TimeSinceLastMeasure = 0f;
	}

	private void Update()
	{
		float deltaTime = Time.deltaTime;

		// Add to moving average (circular buffer)
		m_FrameTimeHistory[m_FrameTimeHistoryIndex] = deltaTime * 1000f;  // Convert to milliseconds
		m_FrameTimeHistoryIndex = (m_FrameTimeHistoryIndex + 1) % MOVING_AVERAGE_SIZE;

		// Calculate moving average
		float sum = 0f;
		for (int i = 0; i < MOVING_AVERAGE_SIZE; i++)
		{
			sum += m_FrameTimeHistory[i];
		}
		m_FrameTimeMovingAverage = sum / MOVING_AVERAGE_SIZE;

		// Track time for measurement intervals
		m_TimeSinceLastMeasure += deltaTime;

		// Check if we should start/stop measuring
		if (!m_IsMeasuring && m_TimeSinceLastMeasure >= MEASURE_INTERVAL)
		{
			// Start measurement: disable camera
			StartMeasurement();
		}
		else if (m_IsMeasuring)
		{
			// Skip first few frames to let things stabilize
			if (m_MeasureFrameCount >= FRAMES_TO_SKIP)
			{
				// Record frame time during measurement
				m_MeasuredFrameTimes.Add(deltaTime * 1000f);
			}
			m_MeasureFrameCount++;

			// Check if measurement period is over
			if (Time.realtimeSinceStartup - m_MeasureStartTime >= CAMERA_OFF_DURATION)
			{
				// End measurement: re-enable camera
				EndMeasurement();
			}
		}

		// Update display
		UpdateDisplay();
	}

	private void StartMeasurement()
	{
		m_IsMeasuring = true;
		m_MeasureStartTime = Time.realtimeSinceStartup;
		m_MeasureFrameCount = 0;
		m_MeasuredFrameTimes.Clear();
		m_LastNormalFrameTime = m_FrameTimeMovingAverage;

		// Disable camera rendering only
		if (m_MainCamera != null) {
			m_MainCamera.enabled = false;
		} else {
			m_MainCamera = Camera.main;
			m_MainCamera.enabled = false;
		}

		#if UNITY_EDITOR
		Debug.Log("SimpleFrameProfiler: Starting measurement (camera disabled)");
		#endif
	}

	private void EndMeasurement()
	{
		m_IsMeasuring = false;
		m_TimeSinceLastMeasure = 0f;

		// Re-enable camera
		if (m_MainCamera != null)
		{
			m_MainCamera.enabled = true;
		}else {
			m_MainCamera = Camera.main;
			m_MainCamera.enabled = true;
		}

		// Calculate CPU time from measured frames
		if (m_MeasuredFrameTimes.Count > 0)
		{
			float sum = 0f;
			for (int i = 0; i < m_MeasuredFrameTimes.Count; i++)
			{
				sum += m_MeasuredFrameTimes[i];
			}
			m_LastCpuFrameTime = sum / m_MeasuredFrameTimes.Count;
		}

		// Calculate rendering time
		m_RenderingTimeMs = Mathf.Max(0, m_LastNormalFrameTime - m_LastCpuFrameTime);

		#if UNITY_EDITOR
		Debug.Log("SimpleFrameProfiler: Measurement complete");
		Debug.Log("  Normal Frame Time: " + m_LastNormalFrameTime.ToString("F2") + "ms");
		Debug.Log("  CPU Frame Time: " + m_LastCpuFrameTime.ToString("F2") + "ms");
		Debug.Log("  Rendering Time: " + m_RenderingTimeMs.ToString("F2") + "ms");
		#endif
	}

	private void UpdateDisplay()
	{
		m_StringBuilder.Length = 0;

		// Header
		m_StringBuilder.AppendLine("=== SIMPLE FRAME PROFILER ===");

		// Current frame time and FPS
		float fps = m_FrameTimeMovingAverage > 0 ? 1000f / m_FrameTimeMovingAverage : 0f;
		Color fpsColor = fps >= 30f ? Color.green : (fps >= 20f ? Color.yellow : Color.red);
		m_DisplayText.color = fpsColor;

		m_StringBuilder.AppendLine("Moving Avg FPS: " + fps.ToString("F1"));
		m_StringBuilder.AppendLine("Frame Time: " + m_FrameTimeMovingAverage.ToString("F2") + "ms");
		m_StringBuilder.AppendLine("Target: 33.33ms @ 30fps");

		// Measurement status
		m_StringBuilder.AppendLine();
		if (m_IsMeasuring)
		{
			float elapsed = Time.realtimeSinceStartup - m_MeasureStartTime;
			m_StringBuilder.AppendLine("[MEASURING] Camera OFF (" + m_MeasuredFrameTimes.Count + " frames)");
			m_StringBuilder.AppendLine("Elapsed: " + elapsed.ToString("F2") + "s / " + CAMERA_OFF_DURATION.ToString("F1") + "s");
		}
		else
		{
			float timeUntilNext = MEASURE_INTERVAL - m_TimeSinceLastMeasure;
			m_StringBuilder.AppendLine("Next measurement in: " + timeUntilNext.ToString("F1") + "s");
		}

		// Results from last measurement
		if (m_LastNormalFrameTime > 0)
		{
			m_StringBuilder.AppendLine();
			m_StringBuilder.AppendLine("=== LAST MEASUREMENT ===");
			m_StringBuilder.AppendLine("Normal Frame Time: " + m_LastNormalFrameTime.ToString("F2") + "ms");
			m_StringBuilder.AppendLine("CPU Frame Time: " + m_LastCpuFrameTime.ToString("F2") + "ms");
			m_StringBuilder.AppendLine("Rendering Time: " + m_RenderingTimeMs.ToString("F2") + "ms");

			// Calculate percentages
			if (m_LastNormalFrameTime > 0)
			{
				float cpuPercent = (m_LastCpuFrameTime / m_LastNormalFrameTime) * 100f;
				float renderPercent = (m_RenderingTimeMs / m_LastNormalFrameTime) * 100f;
				m_StringBuilder.AppendLine();
				m_StringBuilder.AppendLine("CPU: " + cpuPercent.ToString("F1") + "%");
				m_StringBuilder.AppendLine("Rendering: " + renderPercent.ToString("F1") + "%");
			}

			// Bottleneck analysis
			m_StringBuilder.AppendLine();
			if (m_RenderingTimeMs > m_LastCpuFrameTime)
			{
				m_StringBuilder.AppendLine("Bottleneck: GPU/RENDERING");
			}
			else if (m_LastCpuFrameTime > m_RenderingTimeMs)
			{
				m_StringBuilder.AppendLine("Bottleneck: CPU/SCRIPTS");
			}
			else
			{
				m_StringBuilder.AppendLine("Bottleneck: BALANCED");
			}
		}

		// Update display
		m_DisplayText.text = m_StringBuilder.ToString();
	}
}