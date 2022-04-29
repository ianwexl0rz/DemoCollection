using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;

public class WeaponTrail : MonoBehaviour
{
	public Color Color = Color.white;
	public Material Material = null;

	private List<Vector3> _worldPositions;
	private MeshRenderer _renderer;
	private MeshFilter _filter;
	private Mesh _mesh;

	private void Awake()
	{
		var weapon = GetComponent<MeleeWeapon>();

		weapon.OnNewHit += ClearVertices;
		weapon.OnProcessHit += UpdateAndShowMesh;
		weapon.OnEndHit += HideMesh;

		_renderer = gameObject.AddComponent<MeshRenderer>();
		_filter = gameObject.AddComponent<MeshFilter>();

		_mesh = new Mesh();
		_filter.mesh = _mesh;
		_renderer.shadowCastingMode = ShadowCastingMode.Off;
		_renderer.sharedMaterial = Material;
	}

	public void ClearVertices()
	{
		_worldPositions = new List<Vector3>();
	}
	
	public void UpdateAndShowMesh(Transform localSpace, Vector3[] addPoints)
	{
		_worldPositions.AddRange(addPoints);
		
		if(_worldPositions.Count < 4) return;

		_renderer.enabled = true;

		var length = _worldPositions.Count;
		var vertices = new Vector3[length];
		var colors = new Color[length];
		var uv = new Vector2[length];
		var triangles = new int[(length - 2) * 3];

		for(var v = 0; v < length; v += 2)
		{
			vertices[v] = localSpace.InverseTransformPoint(_worldPositions[v]);
			vertices[v + 1] = localSpace.InverseTransformPoint(_worldPositions[v + 1]);
			
			var alpha = v / (length - 2f);
			alpha *= alpha;
			
			colors[v] = new Color(Color.r, Color.g, Color.b, 0);
			colors[v + 1] = new Color(Color.r, Color.g, Color.b, alpha);

			var uvRatio = (float)v / length;
			uv[v] = new Vector2(uvRatio, 0);
			uv[v + 1] = new Vector2(uvRatio, 1);

			if(v >= 2)
			{
				var t = (v - 2) * 3;
				triangles[t] = v - 2;
				triangles[t + 1] = triangles[t + 5] = v - 1;
				triangles[t + 2] = triangles[t + 4] = v;
				triangles[t + 3] = v + 1;
			}
		}

		_mesh.Clear();
		_mesh.vertices = vertices;
		_mesh.colors = colors;
		_mesh.uv = uv;
		_mesh.triangles = triangles;
		_mesh = _mesh.MakeDoubleSided();
	}
	
	public void HideMesh()
	{
		_renderer.enabled = false;
	}
}
