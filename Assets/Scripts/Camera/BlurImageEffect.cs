using System.Linq.Expressions;
using UnityEngine;

[ExecuteInEditMode]
public class BlurImageEffect : MonoBehaviour
{
	public Material material;
	public float blur;
	private RenderTexture temp;

	[ImageEffectOpaque]
	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		//RenderTexture temp = new RenderTexture(source);

		Shader.SetGlobalFloat("blur", blur);

		Shader.SetGlobalVector("offsets", new Vector4(2.0f / Screen.width, 0, 0, 0));
		Graphics.Blit(source, destination, material);
		// vertical blur
		Shader.SetGlobalVector("offsets", new Vector4(0, 2.0f / Screen.height, 0, 0));
		Graphics.Blit(destination, source, material);
		// horizontal blur
		Shader.SetGlobalVector("offsets", new Vector4(2.0f / Screen.width, 0, 0, 0));
		Graphics.Blit(source, destination, material);
		// vertical blur
		Shader.SetGlobalVector("offsets", new Vector4(0, 2.0f / Screen.height, 0, 0));
		Graphics.Blit(destination, source, material);

		Graphics.Blit(source, destination, material);
	}
}
