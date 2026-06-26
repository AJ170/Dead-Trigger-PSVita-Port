using UnityEngine;
[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Image Effects/FastMobileBloom")]

//An optimised (as much as I could) version of the Fast Mobile Bloom that shaves off a couple FPS
public class FasterMobileBloom : MonoBehaviour
{
	[Range(0.0f, 1.5f)] public float threshold = 0.25f;
	[Range(0.00f, 4.0f)] public float intensity = 1.0f;
	[Range(0.25f, 5.5f)] public float blurSize = 1.0f;
	[Range(1, 4)] public int blurIterations = 2;
	public Material fastBloomMaterial = null;

	private static int _SpreadID = -1;
	private static int _ThresholdParamsID = -1;
	private static int _BloomIntensityID = -1;
	private static int _BloomTexID = -1;

	[Header("Performance Settings")]
	public int bloomWidth = 256;
	public int bloomHeight = 128;

	// Ping-pong pair
	private RenderTexture tempRT1;
	private RenderTexture tempRT2;
	private bool texturesCreated = false;

	void Awake()
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
		if (tempRT1 != null)
		{
			tempRT1.Release();
			DestroyImmediate(tempRT1);
			tempRT1 = null;
		}
		if (tempRT2 != null)
		{
			tempRT2.Release();
			DestroyImmediate(tempRT2);
			tempRT2 = null;
		}
		texturesCreated = false;
	}

	void CreateTextures(RenderTextureFormat format)
	{
		if (texturesCreated) return;

		tempRT1 = new RenderTexture(bloomWidth, bloomHeight, 0, format);
		tempRT1.name = "BloomTemp1";
		tempRT1.Create();

		tempRT2 = new RenderTexture(bloomWidth, bloomHeight, 0, format);
		tempRT2.name = "BloomTemp2";
		tempRT2.Create();

		texturesCreated = true;
	}

	void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		// Ensure shader properties are initialized (for ExecuteInEditMode)
		InitializeShaderProperties();

		// Ensure textures exist
		CreateTextures(source.format);

		// Set properties once
		fastBloomMaterial.SetFloat(_SpreadID, blurSize);
		fastBloomMaterial.SetVector(_ThresholdParamsID, new Vector2(1.0f, -threshold));

		// Initial downsample with threshold into tempRT1
		tempRT1.DiscardContents();
		Graphics.Blit(source, tempRT1, fastBloomMaterial, 0);

		// Ping-pong blur iterations
		RenderTexture rtRead = tempRT1;
		RenderTexture rtWrite = tempRT2;

		for (int i = 0; i < blurIterations; i++)
		{
			rtWrite.DiscardContents();
			// Alternate between downscale and upscale passes
			int pass = (i % 2 == 0) ? 1 : 2;
			Graphics.Blit(rtRead, rtWrite, fastBloomMaterial, pass);

			// Swap
			RenderTexture temp = rtRead;
			rtRead = rtWrite;
			rtWrite = temp;
		}

		// Final composite (rtRead now contains the final blurred result)
		fastBloomMaterial.SetFloat(_BloomIntensityID, intensity);
		fastBloomMaterial.SetTexture(_BloomTexID, rtRead);
		Graphics.Blit(source, destination, fastBloomMaterial, 3);
	}
}