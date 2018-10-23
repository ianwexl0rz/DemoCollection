using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;


[PostProcess(typeof(BlurPostProcessingRenderer<BlurPostProcessing>), PostProcessEvent.BeforeStack, "Custom/Blur or Sharpen (After Stack)")]
public class BlurPostProcessing : PostProcessEffectSettings
{
	public TextureParameter test = new TextureParameter();
}

public class BlurPostProcessingRenderer<T> : PostProcessEffectRenderer<T> where T : BlurPostProcessing
{
	public override void Render(PostProcessRenderContext context)
	{
		if(settings == null)
			return;

		var sheet = context.propertySheets.Get(Shader.Find("Hidden/SeparableGlassBlur"));

		//Vector2 sensitivity = new Vector2(settings.sensitivityDepth, settings.sensitivityNormals);
		//sheet.properties.SetVector("_Sensitivity", new Vector4(sensitivity.x, sensitivity.y, 1.0f, sensitivity.y));
		//sheet.properties.SetFloat("_BgFade", settings.edgesOnly);
		//sheet.properties.SetFloat("_SampleDistance", settings.sampleDist);
		//sheet.properties.SetVector("_BgColor", settings.edgesOnlyBgColor.value);
		//sheet.properties.SetFloat("_Exponent", settings.edgeExp);
		//sheet.properties.SetFloat("_Threshold", settings.lumThreshold);

		//sheet.properties.SetTexture("_MainTex", settings.test);

		//// copy screen into temporary RT

		//if(!m_Material)
		//{
		//	m_Material = new Material(m_BlurShader);
		//	m_Material.hideFlags = HideFlags.HideAndDontSave;
		//}
		////buf.SetRenderTarget(BuiltinRenderTextureType.CurrentActive);

		//// copy screen into temporary RT
		//int screenCopyID = Shader.PropertyToID("_ScreenCopyTexture");
		//buf.GetTemporaryRT(screenCopyID, -1, -1, 0, FilterMode.Bilinear);
		//buf.Blit(BuiltinRenderTextureType.CurrentActive, screenCopyID);

		//// get two smaller RTs
		//int blurredID = Shader.PropertyToID("_Temp1");
		//int blurredID2 = Shader.PropertyToID("_Temp2");
		//buf.GetTemporaryRT(blurredID, -2, -2, 0, FilterMode.Bilinear);
		//buf.GetTemporaryRT(blurredID2, -2, -2, 0, FilterMode.Bilinear);

		//// downsample screen copy into smaller RT, release screen RT
		//buf.Blit(screenCopyID, blurredID);
		//buf.ReleaseTemporaryRT(screenCopyID);

		//buf.SetGlobalFloat("blur", settings.blur);

		//// horizontal blur
		//buf.SetGlobalVector("offsets", new Vector4(2.0f / Screen.width, 0, 0, 0));
		//buf.Blit(blurredID, blurredID2, m_Material);
		//// vertical blur
		//buf.SetGlobalVector("offsets", new Vector4(0, 2.0f / Screen.height, 0, 0));
		//buf.Blit(blurredID2, blurredID, m_Material);
		//// horizontal blur
		//buf.SetGlobalVector("offsets", new Vector4(2.0f / Screen.width, 0, 0, 0));
		//buf.Blit(blurredID, blurredID2, m_Material);
		//// vertical blur
		//buf.SetGlobalVector("offsets", new Vector4(0, 2.0f / Screen.height, 0, 0));
		//buf.Blit(blurredID2, blurredID, m_Material);

		//buf.SetGlobalTexture("_GrabBlurTexture", blurredID);

		//buf.SetRenderTarget(settings.blurTarget);
		//buf.Blit(blurredID, BuiltinRenderTextureType.CurrentActive);

		context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
	}
}