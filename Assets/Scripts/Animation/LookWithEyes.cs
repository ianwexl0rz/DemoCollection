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

	#region PUBLIC_VARIABLES
	[SerializeField]
	private Transform face = null;

	[SerializeField]
	private List<Transform> eyes = null;

	[SerializeField]
	private Transform focus = null;

	[SerializeField]
	private float transitionTime = 0.25f;
	#endregion

	#region PRIVATE VARIABLES
	private float ease = 0;
	#endregion

	#region UNITY_METHODS
	private void Update ()
	{
		if(face == null || focus == null || eyes.Count == 0) { return; }

		foreach(Transform eye in eyes)
		{
			// Rotation from the eye to the focus.
			var rotation = Quaternion.LookRotation(focus.position - eye.position);

			// How much I am facing the focal point.
			var t = Quaternion.Dot(face.rotation, rotation);

			// Squaring the dot product makes it 0-1 regardless of direction.
			t *= t;

			// If we are facing the focus...
			if(t > 0.5f)
			{
				// Ramp up our attention.
				ease = Mathf.Min(ease + Time.deltaTime, transitionTime);
			}
			else
			{
				// Diminish our attention.
				ease = Mathf.Max(ease - Time.deltaTime, 0f);
			}

			// Diminish the intensity of the look based on facing angle.
			eye.rotation = Quaternion.Slerp(face.rotation, Quaternion.LookRotation(focus.position - eye.position), t * t * ease / transitionTime);
		}
	}
	#endregion
}
