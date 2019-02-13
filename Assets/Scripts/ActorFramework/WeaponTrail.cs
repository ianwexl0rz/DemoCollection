using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;

public class WeaponTrail : MonoBehaviour
{
	private new MeshRenderer renderer;
	private MeshFilter filter;
	private Mesh mesh;

	public void Init(Material material)
	{
		renderer = GetComponent<MeshRenderer>();
		filter = GetComponent<MeshFilter>();

		mesh = new Mesh();
		filter.mesh = mesh;
		renderer.shadowCastingMode = ShadowCastingMode.Off;
		renderer.sharedMaterial = material;
	}

	public void SetMaterial(Material material)
	{
		renderer.sharedMaterial = material;
	}

	public void HideMesh()
	{
		renderer.enabled = false;
	}

	public void UpdateAndShowMesh(List<Vector3> pointBuffer, List<Color> colors = null)
	{
		if(pointBuffer.Count < 4) return;

		renderer.enabled = true;

		Vector3[] vertices = new Vector3[pointBuffer.Count];
		Vector2[] uv = new Vector2[pointBuffer.Count];
		int[] triangles = new int[(pointBuffer.Count - 2) * 3];
		Color[] newColors = new Color[pointBuffer.Count];

		for(int n = 0; n < pointBuffer.Count; n += 2)
		{
			vertices[n] = pointBuffer[n];
			vertices[n + 1] = pointBuffer[n + 1];

			var alpha = n / (pointBuffer.Count - 2f);
			alpha *= alpha;

			var c = colors?[n] ?? Color.black;
			c = new Color(c.r, c.g, c.b, 0f);
			newColors[n] = c;

			c = colors?[n + 1] ?? Color.black;
			c = new Color(c.r, c.g, c.b, alpha);
			newColors[n + 1] = c;

			float uvRatio = (float)n / pointBuffer.Count;
			uv[n] = new Vector2(uvRatio, 0);
			uv[n + 1] = new Vector2(uvRatio, 1);

			if(n >= 2)
			{
				var ti = (n - 2) * 3;
				triangles[ti] = n - 2;
				triangles[ti + 1] = triangles[ti + 5] = n - 1;
				triangles[ti + 2] = triangles[ti + 4] = n;
				triangles[ti + 3] = n + 1;
			}
		}

		mesh.Clear();
		mesh.vertices = vertices;
		mesh.colors = newColors;
		mesh.uv = uv;
		mesh.triangles = triangles;
		mesh = mesh.MakeDoubleSided();
	}
}
