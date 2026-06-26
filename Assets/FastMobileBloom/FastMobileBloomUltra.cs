using UnityEngine;
[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Image Effects/FastMobileBloomUltra")]
public class FastMobileBloomUltra : MonoBehaviour
{
	[Range(0.0f, 1.5f)] public float threshold = 0.25f;
	[Range(0.00f, 4.0f)] public float intensity = 1.0f;
	[Range(0.25f, 5.5f)] public float blurSize = 1.0f;
	[Range(1, 2)] public int blurIterations = 1;  // Max 2 for ultra mode
	public Material fastBloomMaterial = null;

	private static int _SpreadID = -1;
	private static int _ThresholdParamsID = -1;
	private static int _BloomIntensityID = -1;
	private static int _BloomTexID = -1;

	[Header("Performance Settings")]
	public int bloomWidth = 128;   // Reduced from 256
	public int bloomHeight = 64;   // Reduced from 128

	// Single texture for ultra-simple path
	private RenderTexture bloomRT;
	private bool textureCreated = false;

	void Start()
	{
		InitializeShaderProperties();
	}

	void InitializeShaderProperties()
	{
		if (_SpreadID == -1)
		{
			_SpreadID = Shader.PropertyToID("_Spread");
			_ThresholdParamsID = Shader.PropertyToID("_ThresholdParams");
			_BloomIntensityID = Shader.PropertyToID("_BloomIntensity");
			_BloomTexID = Shader.PropertyToID("_BloomTex");
		}
	}

	void OnDisable()
	{
		ReleaseTextures();
	}

	void OnDestroy()
	{
		ReleaseTextures();
	}

	void ReleaseTextures()
	{
		if (bloomRT != null)
		{
			bloomRT.Release();
			DestroyImmediate(bloomRT);
			bloomRT = null;
		}
		textureCreated = false;
	}

	void CreateTexture()
	{
		if (textureCreated) return;

		// Use RGB565 for even better performance (16-bit color)
		bloomRT = new RenderTexture(bloomWidth, bloomHeight, 0, RenderTextureFormat.RGB565);
		bloomRT.filterMode = FilterMode.Bilinear;
		bloomRT.name = "BloomRT_Ultra";
		bloomRT.Create();

		textureCreated = true;
	}

	void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		InitializeShaderProperties();
		CreateTexture();

		// Set all properties once
		fastBloomMaterial.SetFloat(_SpreadID, blurSize);
		fastBloomMaterial.SetVector(_ThresholdParamsID, new Vector2(1.0f, -threshold));
		fastBloomMaterial.SetFloat(_BloomIntensityID, intensity);

		// Ultra-simple path: threshold -> blur(s) -> composite
		bloomRT.DiscardContents();

		if (blurIterations == 1)
		{
			// Single pass: threshold + minimal blur
			Graphics.Blit(source, bloomRT, fastBloomMaterial, 0);
			Graphics.Blit(bloomRT, bloomRT, fastBloomMaterial, 1);
		}
		else
		{
			// Two iterations max
			Graphics.Blit(source, bloomRT, fastBloomMaterial, 0);
			Graphics.Blit(bloomRT, bloomRT, fastBloomMaterial, 1);
			Graphics.Blit(bloomRT, bloomRT, fastBloomMaterial, 1);
		}

		// Composite
		fastBloomMaterial.SetTexture(_BloomTexID, bloomRT);
		Graphics.Blit(source, destination, fastBloomMaterial, 3);
	}
}