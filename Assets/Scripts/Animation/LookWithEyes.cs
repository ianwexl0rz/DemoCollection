/***********************************
 * LookWithEyes.cs
 * 
 * Ian Wexler
 * 
 * Makes eyes look at a position.
 **********************************/

using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class LookWithEyes : MonoBehaviour {

	#region SERIALIZED_VARIABLES
	[SerializeField]
	private Transform face = null;

	[SerializeField]
	private List<Transform> eyes = null;

	[SerializeField]
	private float hFov = 90f;

	[SerializeField]
	private float vFov = 50f;

	[SerializeField]
	private float lookSpeed = 360f;

	[Header("Debug")]
	[SerializeField]
	private Transform debugTarget = null;
	#endregion

	#region PRIVATE_VARIABLES
	private Transform focus = null;
	#endregion

	#region UNITY_METHODS
	private void Update ()
	{
		if(face == null || eyes.Count == 0) { return; }

		// For testing purposes only!
		focus = debugTarget;

		// If we have a target, make sure it's still valid.
		if(focus != null && !ValidateFocus(focus))
		{
			// Set the focus to null if it's invalid.
			focus = null;
		}

		foreach(Transform eye in eyes)
		{
			// Rotate the eyes as needed.
			Quaternion targetRotation = focus != null ? Quaternion.LookRotation(focus.position - eye.position) : face.rotation;
			eye.rotation = Quaternion.RotateTowards(eye.rotation, targetRotation, lookSpeed * Time.deltaTime);
		}
	}
	#endregion 

	#region PRIVATE_METHODS
	private bool ValidateFocus(Transform target)
	{
		// Get the vector to the object
		var toFocus = (target.position - face.position).normalized;

		// The object must be in front of us.
		if(toFocus.z > 0)
		{
			// Check if the object is in our horizontal FOV.
			float hAngle = 90f - Mathf.Abs(Mathf.Atan(toFocus.z / toFocus.x) * Mathf.Rad2Deg);
			if(hAngle < hFov * 0.5f)
			{
				// Check if the object is also in our vertical FOV.
				float vAngle = 90f - Mathf.Abs(Mathf.Atan(toFocus.z / toFocus.y) * Mathf.Rad2Deg);
				if(vAngle < vFov * 0.5f)
				{
					// The target is valid.
					return true ;
				}
			}
		}
		return false;
	}
	#endregion
}
