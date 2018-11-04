using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class WeaponTrail : MonoBehaviour
{

	private new MeshRenderer renderer;
	private MeshFilter filter;
	private Mesh mesh;

    void Awake()
    {
        mesh = new Mesh();
	    renderer = GetComponent<MeshRenderer>();
	    filter = GetComponent<MeshFilter>();
	    filter.mesh = mesh;
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
		    //alpha *= alpha;

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

	    // Source: https://forum.unity.com/threads/double-sided-rendering-without-special-shaders.197923/
		
	    // Invert normals on duplicated geometry
		var oldVertexCount = mesh.vertexCount;
		var newVertices = DoubleArray(mesh.vertices);
		var newNormals = DoubleArray(mesh.normals);
		for(var i = newNormals.Length / 2; i < newNormals.Length; ++i)
		{
			newNormals[i] = -newNormals[i];
		}

		// Invert tangent W components to account for mirrored UVs
		var newTangents = DoubleArray(mesh.tangents);
		for(var i = newTangents.Length / 2; i < newTangents.Length; ++i)
		{
			newTangents[i].w = -newTangents[i].w;
		}

		// All other attributes remain the same
		var newColors2 = DoubleArray(mesh.colors);
		var newUVs = DoubleArray(mesh.uv);
		var newUV2s = DoubleArray(mesh.uv2);
		var newBoneWeights = DoubleArray(mesh.boneWeights);

		// Reverse winding on doubled triangles so front face matches normal
		// Also point doubled triangles at doubled vertex indices
		var triangleLists = new List<int[]>();
		for(var submeshIndex = 0; submeshIndex < mesh.subMeshCount; ++submeshIndex)
		{
			var oldTriangles = mesh.GetTriangles(submeshIndex);
			var newTriangles = DoubleArray(oldTriangles);
			for(var i = oldTriangles.Length / 3; i < oldTriangles.Length / 3 * 2; ++i)
			{

				newTriangles[i * 3] += oldVertexCount;

				var temp = newTriangles[i * 3 + 1] + oldVertexCount;
				newTriangles[i * 3 + 1] = newTriangles[i * 3 + 2] + oldVertexCount;
				newTriangles[i * 3 + 2] = temp;
			}
			triangleLists.Add(newTriangles);
		}

		// Assign all vertex attributes
		mesh.vertices = newVertices;
		mesh.normals = newNormals;
		mesh.tangents = newTangents;
		mesh.colors = newColors2;
		mesh.uv = newUVs;
		mesh.uv2 = newUV2s;
		mesh.boneWeights = newBoneWeights;

		// Assign triangles last, so they match vertices
		for(var submeshIndex = 0; submeshIndex < mesh.subMeshCount; ++submeshIndex)
		{
			mesh.SetTriangles(triangleLists[submeshIndex], submeshIndex);
		}
	}

	// Returns the input array concatenated with itself
	protected static T[] DoubleArray<T>(T[] input)
	{
		var newArray = new T[input.Length * 2];
		Array.Copy(
			input,
			0,
			newArray,
			0,
			input.Length
		);
		Array.Copy(
			input,
			0,
			newArray,
			input.Length,
			input.Length
		);
		return newArray;
	}
}
