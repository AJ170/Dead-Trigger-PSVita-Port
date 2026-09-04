#define DEBUG
using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("Image Effects/MADFINGER color correction - simple")]
public class MFColorCorrectionEffectSimple : ImageEffectBase
{
	public float R_offs;

	public float G_offs;

	public float B_offs;

	protected void Update()
	{
		base.enabled = false;
		return;
		if (Application.isPlaying && DeviceInfo.PerformanceGrade == DeviceInfo.Performance.Low)
		{
			base.enabled = false;
		}
	}

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		DebugUtils.Assert(shader);
		if ((bool)base.material)
		{
			base.material.shader = shader;
			base.material.SetVector("_ColorBias", new Vector4(R_offs, G_offs, B_offs, 0f));
		}
		Graphics.Blit(source, destination, base.material);
	}
}
