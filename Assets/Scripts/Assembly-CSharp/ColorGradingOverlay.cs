using UnityEngine;
using UnityEngine.UI;

public class ColorGradingOverlay : MonoBehaviour
{
	public enum GradingMode
	{
		Simple = 0,
		Warm = 1,
		Cool = 2,
		Sepia = 3
	}

	private Image _overlayImage;
	private Canvas _overlayCanvas;
	private Material _gradingMaterial;
	private GradingMode _currentMode = GradingMode.Simple;
	private Color _currentColor = new Color(0.03f, -0.05f, -0.1f, 0.0f);

	public static ColorGradingOverlay Instance { get; private set; }

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
		Initialize ();
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public void Initialize()
	{
		// Create canvas for overlay
		GameObject canvasObj = new GameObject("ColorGradingCanvas");
		canvasObj.transform.SetParent(transform);

		_overlayCanvas = canvasObj.AddComponent<Canvas>();
		_overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

		// Set to render BEFORE HUD (low sort order)
		_overlayCanvas.sortingOrder = -100;

		// Add raycaster but disable it (overlay shouldn't block input)
		GraphicRaycaster raycaster = canvasObj.AddComponent<GraphicRaycaster>();
		raycaster.enabled = false;

		// Add scaler for consistency
		CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(960, 544);  // Vita resolution

		// Create full-screen image
		GameObject imageObj = new GameObject("ColorGrade");
		imageObj.transform.SetParent(canvasObj.transform, false);

		RectTransform rt = imageObj.AddComponent<RectTransform>();
		rt.anchorMin = Vector2.zero;
		rt.anchorMax = Vector2.one;
		rt.offsetMin = Vector2.zero;
		rt.offsetMax = Vector2.zero;

		_overlayImage = imageObj.AddComponent<Image>();
		_overlayImage.raycastTarget = false;

		// Create material with custom shader
		Shader gradingShader = Shader.Find("UI/ColorGradingOverlay");
		if (gradingShader != null)
		{
			_gradingMaterial = new Material(gradingShader);
			_overlayImage.material = _gradingMaterial;
		}
		else
		{
			Debug.LogWarning("ColorGradingOverlay shader not found! Using default UI shader.");
			_overlayImage.material = new Material(Shader.Find("UI/Default"));
		}

		//get the information we need to do our colorgrade
		if (Camera.main != null) {
			MFColorCorrectionEffectSimple MFCol = Camera.main.GetComponent<MFColorCorrectionEffectSimple> ();
			float minVal = 100f;
			minVal = Mathf.Min (minVal, MFCol.R_offs);
			minVal = Mathf.Min (minVal, MFCol.G_offs);
			minVal = Mathf.Min (minVal, MFCol.B_offs);

			Color tintColor = new Color (MFCol.R_offs, MFCol.G_offs, MFCol.B_offs, 0);

			//In theory this should have a major negative value, but if it's not we'll just have to do something about it I guess
			if (minVal < 0) {
				tintColor = new Color (MFCol.R_offs - minVal, MFCol.B_offs - minVal, MFCol.G_offs - minVal, 0f);
			}
			SetColorGrade(tintColor, _currentMode);
		} else {
			

			// Set initial color
			SetColorGrade (_currentColor, _currentMode);
		}
	}

	public void SetColorGrade(Color color, GradingMode mode = GradingMode.Simple)
	{
		if (_overlayImage == null)
		{
			return;
		}

		_currentColor = color;
		_currentMode = mode;

		_overlayImage.color = color;
		/*
		if (_gradingMaterial != null)
		{
			_gradingMaterial.SetInt("_Mode", (int)mode);
		}*/
	}

	public void SetColorGradeAlpha(float alpha)
	{
		if (_overlayImage == null)
		{
			return;
		}

		Color newColor = _currentColor;
		newColor.a = alpha;
		_overlayImage.color = newColor;
	}

	public void FadeToGrading(Color targetColor, GradingMode mode, float duration)
	{
		StartCoroutine(FadeGradingCoroutine(targetColor, mode, duration));
	}

	private System.Collections.IEnumerator FadeGradingCoroutine(Color targetColor, GradingMode mode, float duration)
	{
		Color startColor = _overlayImage.color;
		float elapsed = 0f;

		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			float t = Mathf.Clamp01(elapsed / duration);

			Color lerpedColor = Color.Lerp(startColor, targetColor, t);
			SetColorGrade(lerpedColor, mode);

			yield return null;
		}

		SetColorGrade(targetColor, mode);
	}

	public void SetEnabled(bool enabled)
	{
		if (_overlayImage != null)
		{
			_overlayImage.enabled = enabled;
		}
	}

	public Color GetCurrentColor()
	{
		return _currentColor;
	}

	public GradingMode GetCurrentMode()
	{
		return _currentMode;
	}
}
