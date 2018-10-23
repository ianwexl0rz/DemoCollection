using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class BlurCommandBuffer : MonoBehaviour
{
	public Shader m_BlurShader;
	private Material m_Material;
	private Camera cam;

	private CommandBuffer buf;
	public bool enableCommandBuffer;
	private bool isCommandBufferEnabled;

	public Settings settings;
	private Settings oldSettings;

	[System.Serializable]
	public struct Settings
	{
		public float blur;
		public CameraEvent camEvent;
		public BuiltinRenderTextureType blurTarget;

		public Settings(float blur, CameraEvent camEvent, BuiltinRenderTextureType blurTarget)
		{
			this.blur = blur;
			this.camEvent = camEvent;
			this.blurTarget = blurTarget;
		}

		public override bool Equals(object obj)
		{
			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public static bool operator ==(Settings c1, Settings c2)
		{
			return c1.Equals(c2);
		}

		public static bool operator !=(Settings c1, Settings c2)
		{
			return !c1.Equals(c2);
		}
	}

	private void Awake()
	{
		cam = GetComponent<Camera>();
	}

	// TODO: Get translucency, edge light, hardness from material properties and encode them into the GBuffer AFTER it's been written normally
	Color RGBToYCoCg(Color c)
	{
		return new Color()
		{
			r = 0.25f * c.r + 0.5f * c.g + 0.25f * c.b,
			g = 0.5f * c.r - 0.5f * c.b + 0.5f,
			b = -0.25f * c.r + 0.5f * c.g - 0.25f * c.b + 0.5f
		};
	}

	Color YCoCgToRGB(Color c)
	{
		c.g -= 0.5f;
		c.b -= 0.5f;
		return new Color()
		{
			r = c.r + c.g - c.b,
			g = c.r + c.b,
			b = c.r - c.g - c.b
		};
	}

	void SetCommandBufferEnabled(bool value)
	{
		if(!value && isCommandBufferEnabled)
		{
			if(buf != null)
				cam.RemoveCommandBuffer(settings.camEvent, buf);
			isCommandBufferEnabled = false;
		}

		if(!value || (isCommandBufferEnabled && settings == oldSettings))
			return;

		if(isCommandBufferEnabled)
		{
			if(buf != null)
				cam.RemoveCommandBuffer(oldSettings.camEvent, buf);
			oldSettings = settings;
		}
		isCommandBufferEnabled = true;

		if(!m_Material)
		{
			m_Material = new Material(m_BlurShader);
			m_Material.hideFlags = HideFlags.HideAndDontSave;
		}

		buf = new CommandBuffer();
		buf.name = "Grab screen and blur";
		//buf.SetRenderTarget(BuiltinRenderTextureType.CurrentActive);

		// copy screen into temporary RT
		int screenCopyID = Shader.PropertyToID("_ScreenCopyTexture");
		buf.GetTemporaryRT(screenCopyID, -1, -1, 0, FilterMode.Bilinear);
		buf.Blit(BuiltinRenderTextureType.CurrentActive, screenCopyID);

		// get two smaller RTs
		int blurredID = Shader.PropertyToID("_Temp1");
		int blurredID2 = Shader.PropertyToID("_Temp2");
		buf.GetTemporaryRT(blurredID, -2, -2, 0, FilterMode.Bilinear);
		buf.GetTemporaryRT(blurredID2, -2, -2, 0, FilterMode.Bilinear);

		// downsample screen copy into smaller RT, release screen RT
		buf.Blit(screenCopyID, blurredID);
		buf.ReleaseTemporaryRT(screenCopyID);

		buf.SetGlobalFloat("blur", settings.blur);

		// horizontal blur
		buf.SetGlobalVector("offsets", new Vector4(2.0f / Screen.width, 0, 0, 0));
		buf.Blit(blurredID, blurredID2, m_Material);
		// vertical blur
		buf.SetGlobalVector("offsets", new Vector4(0, 2.0f / Screen.height, 0, 0));
		buf.Blit(blurredID2, blurredID, m_Material);
		// horizontal blur
		buf.SetGlobalVector("offsets", new Vector4(2.0f / Screen.width, 0, 0, 0));
		buf.Blit(blurredID, blurredID2, m_Material);
		// vertical blur
		buf.SetGlobalVector("offsets", new Vector4(0, 2.0f / Screen.height, 0, 0));
		buf.Blit(blurredID2, blurredID, m_Material);

		buf.SetGlobalTexture("_GrabBlurTexture", blurredID);

		buf.SetRenderTarget(settings.blurTarget);
		buf.Blit(blurredID, BuiltinRenderTextureType.CurrentActive);

		cam.AddCommandBuffer(settings.camEvent, buf);
	}

	private void OnValidate()
	{
		SetCommandBufferEnabled(enableCommandBuffer);
	}
}
