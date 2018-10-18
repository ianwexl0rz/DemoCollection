using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class InvertCameraProjection : MonoBehaviour
{
	public bool invertX, invertY;
	public Vector2 scale = Vector2.one;

	private Matrix4x4 lastProjectionMatrix;
	private new Camera camera;
	private Vector3 _scale;

	// Start is called before the first frame update
	private void Awake()
    {
	    camera = GetComponent<Camera>();
    }

	private void OnValidate()
	{
		_scale = new Vector3(scale.x * (invertX ? -1 : 1), scale.y * (invertY ? -1 : 1), 1);
	}

	private void OnPreCull()
	{
		//camera.ResetWorldToCameraMatrix();
		camera.ResetProjectionMatrix();
		camera.projectionMatrix = camera.projectionMatrix * Matrix4x4.Scale(_scale);
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
