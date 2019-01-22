using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class InvertCameraProjection : MonoBehaviour
{
	public bool invertX, invertY;
	public Vector2 scale = Vector2.one;

	private Camera cam;
	private Vector3 _scale = Vector3.one;


	private void Awake()
    {
		cam = GetComponent<Camera>();
    }

	private void OnValidate()
	{
		_scale = new Vector3(scale.x * (invertX ? -1 : 1), scale.y * (invertY ? -1 : 1), 1);
	}

	private void OnPreCull()
	{
		cam.ResetProjectionMatrix();
		cam.projectionMatrix = cam.projectionMatrix * Matrix4x4.Scale(_scale);
	}

	private void OnPreRender()
	{
		if(invertX ^ invertY)
			GL.invertCulling = true;
	}

	private void OnPostRender()
	{
		if(invertX ^ invertY)
			GL.invertCulling = false;
	}
}
