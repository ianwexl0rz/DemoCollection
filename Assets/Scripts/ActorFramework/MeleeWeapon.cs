using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;

public class MeleeWeapon : MonoBehaviour
{
	[SerializeField] private float weaponLength = 1f;
	[SerializeField] private Material trailMaterial = null;
	[SerializeField] private bool showTrail = true;

	private Vector3 origin, end, lastOrigin, lastEnd = Vector3.zero;
	private readonly List<Vector3> pointBuffer = new List<Vector3>();
	private readonly List<Color> colors = new List<Color>();

	private MeshRenderer renderer;
	private MeshFilter filter;
	private Mesh mesh;

	private void Awake()
	{
		if(showTrail)
		{
			mesh = new Mesh();
			filter = gameObject.AddComponent<MeshFilter>();
			renderer = gameObject.AddComponent<MeshRenderer>();
			filter.mesh = mesh;
			renderer.sharedMaterial = trailMaterial;
			renderer.shadowCastingMode = ShadowCastingMode.Off;
		}
	}

	public void CheckHits(CombatActor attacker, float distThreshold)
	{
        float debugTime = Time.fixedDeltaTime * 8;

		origin = transform.position;
		end = origin + transform.forward * weaponLength;

		attacker.CheckHit(origin, end);
        attacker.CheckHit(end, origin);
		attacker.CheckHit(lastOrigin, origin);

        Vector3 currentVector = end - origin;
        Vector3 lastVector = lastEnd - lastOrigin;

        int steps = 1 + (int)((currentVector - lastVector).magnitude / distThreshold);

        float colorRange = ((float)steps).LinearRemap(1f, 5f, 0.5f, 0f);
        Color color = Color.HSVToRGB(Mathf.Clamp01(colorRange), 1, 1);

		Vector3[] addPoints = new Vector3[steps * 2];
		Color[] addColors = new Color[steps * 2];

		for(int i = 0; i < steps; i++)
        {
			float t = (i + 1f) / steps;

            Vector3 blendedOrigin = Vector3.Lerp(lastOrigin, origin, t);
            Vector3 blendedEnd = blendedOrigin + Vector3.Slerp(lastVector, currentVector, t);
			attacker.CheckHit(blendedOrigin, blendedEnd);

	        addPoints[i * 2] = blendedOrigin;
	        addPoints[i * 2 + 1] = blendedEnd;

	        addColors[i * 2] = addColors[i * 2 + 1] = Color.white; //color;

			Debug.DrawLine(blendedOrigin, blendedEnd, color, debugTime);
	        Debug.DrawLine(i == 0 ? lastEnd : addPoints[i * 2 - 1], blendedEnd, color, debugTime);
	        Debug.DrawLine(i == 0 ? lastOrigin : addPoints[i * 2 - 2], blendedOrigin, color, debugTime);
		}

		if(pointBuffer.Count == 0)
		{
			Vector3[] lastPoints =
			{
				lastOrigin,
				lastEnd
				//attacker.transform.InverseTransformPoint(lastOrigin),
				//attacker.transform.InverseTransformPoint(lastEnd),
			};

			pointBuffer.AddRange(lastPoints);
			Debug.DrawLine(lastOrigin, lastEnd, color, debugTime);

			Color[] lastColors = { color, color };
			colors.AddRange(lastColors);
		}

		pointBuffer.AddRange(addPoints);
		colors.AddRange(addColors);

		var localPoints = new List<Vector3>(pointBuffer);
		for(var i = localPoints.Count; i-- > 0;)
		{
			localPoints[i] = transform.InverseTransformPoint(localPoints[i]);
		}

		//SetInitialPosition(origin, end);
		lastOrigin = origin;
		lastEnd = end;

		if(showTrail)
			UpdateAndShowMesh(localPoints, colors);
	}

	public void ClearWeaponTrail(CombatActor attacker)
	{
		lastOrigin = transform.position;
		lastEnd = lastOrigin + transform.forward * weaponLength;

		pointBuffer.Clear();
		colors.Clear();
		if(showTrail)
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
