using UnityEngine;
using System.Collections;
using System;

public static class MonoBehaviourExtensions
{
	// Execute in some number of seconds (if zero, next frame)
	public static Coroutine ExecuteWithDelay(this MonoBehaviour mono, Action action, float delay)
	{
		return mono.StartCoroutine(DelayedExecute(action, delay));
	}

	private static IEnumerator DelayedExecute(Action action, float delay)
	{
		yield return new WaitForSecondsRealtime(delay);
		action();
	}

	// Start a coroutine and stop an existing coroutine if necessary
	public static Coroutine OverrideCoroutine(this MonoBehaviour mono, ref Coroutine coroutine, IEnumerator ienumerator)
	{
		if(coroutine != null)
		{
			mono.StopCoroutine(coroutine);
		}

		return coroutine = mono.StartCoroutine(ienumerator);
	}

	// Vector3 extensions
	public static Vector3 WithX(this Vector3 v, float x)
	{
		return new Vector3(x, v.y, v.z);
	}

	public static Vector3 WithY(this Vector3 v, float y)
	{
		return new Vector3(v.x, y, v.z);
	}

	public static Vector3 WithZ(this Vector3 v, float z)
	{
		return new Vector3(v.x, v.y, z);
	}

	public static float LinearRemap(this float value,
									 float valueRangeMin, float valueRangeMax,
									 float newRangeMin, float newRangeMax)
	{
		return (value - valueRangeMin) / (valueRangeMax - valueRangeMin) * (newRangeMax - newRangeMin) + newRangeMin;
	}

	/*
	static public float GetDistPointToLine(Vector3 origin, Vector3 direction, Vector3 point)
	{
		Vector3 point2origin = origin - point;
		Vector3 point2closestPointOnLine = point2origin - Vector3.Dot(point2origin, direction) * direction;
		return point2closestPointOnLine.magnitude;
	}
	*/

	static public void SetPaused(this Animator animator, bool value)
	{
		animator.speed = value ? 0f : 1f;
	}

	static public void RestoreState(this Rigidbody rb, RigidbodyState rigidbodyState)
	{
		//Debug.Log("Restored " + rb.gameObject.name + " to velocity: "  + rigidbodyState.velocity.magnitude);

		rb.position = rigidbodyState.position;
		rb.rotation = rigidbodyState.rotation;
		rb.velocity = rigidbodyState.velocity;
		rb.angularVelocity = rigidbodyState.angularVelocity;
		rb.isKinematic = rigidbodyState.isKinematic;
	}

	static public Vector3 FindNearestPointOnLine(this Vector3 point, Vector3 origin, Vector3 direction, float maxDistance = Mathf.Infinity)
	{
		direction.Normalize();
		Vector3 lhs = point - origin;

		float dotP = Vector3.Dot(lhs, direction);
		return origin + direction * Mathf.Clamp(dotP, 0f, maxDistance);
	}

	public static void RotateToAngleYaw(this Rigidbody rigidbody, PID angleController, PID angularVelocityController, float targetAngle)
	{
		float dt = Time.fixedDeltaTime;

		float angleError = Mathf.DeltaAngle(rigidbody.transform.eulerAngles.y, targetAngle);
		float torqueCorrectionForAngle = angleController.GetOutput(angleError, dt);

		float angularVelocityError = -rigidbody.angularVelocity.y;
		float torqueCorrectionForAngularVelocity = angularVelocityController.GetOutput(angularVelocityError, dt);

		Vector3 torque = rigidbody.transform.up * (torqueCorrectionForAngle + torqueCorrectionForAngularVelocity);
		rigidbody.AddTorque(torque, ForceMode.Acceleration);
	}
}
