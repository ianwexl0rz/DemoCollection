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

	public static Coroutine WaitForEndOfFrameThen(this MonoBehaviour mono, Action action)
	{
		return mono.StartCoroutine(WaitForEndOfFrameThen(action));
	}

	private static IEnumerator WaitForEndOfFrameThen(Action action)
	{
		yield return new WaitForEndOfFrame();
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

	public static float LinearRemap(this float value, float oldMin, float oldMax, float newMin, float newMax)
	{
		return (value - oldMin) / (oldMax - oldMin) * (newMax - newMin) + newMin;
	}

	public static Vector2 LinearRemap(this Vector2 value, Vector2 oldMin, Vector2 oldMax, Vector2 newMin, Vector2 newMax)
	{
		return new Vector2()
		{
			x = (value.x - oldMin.x) / (oldMax.x - oldMin.x) * (newMax.x - newMin.x) + newMin.x,
			y = (value.y - oldMin.y) / (oldMax.y - oldMin.y) * (newMax.y - newMin.y) + newMin.y
		};
	}

	public static void SetPaused(this Animator animator, bool value)
	{
		animator.speed = value ? 0f : 1f;
	}

	public static void RestoreState(this Rigidbody rb, RigidbodyState rigidbodyState)
	{
		//Debug.Log("Restored " + rb.gameObject.name + " to velocity: "  + rigidbodyState.velocity.magnitude);

		rb.position = rigidbodyState.position;
		rb.rotation = rigidbodyState.rotation;
		rb.velocity = rigidbodyState.velocity;
		rb.angularVelocity = rigidbodyState.angularVelocity;
		rb.isKinematic = rigidbodyState.isKinematic;
	}

	public static Vector3 FindNearestPointOnLine(this Vector3 point, Vector3 origin, Vector3 direction, float maxDistance = Mathf.Infinity)
	{
		direction.Normalize();
		Vector3 lhs = point - origin;

		float dotP = Vector3.Dot(lhs, direction);
		return origin + direction * Mathf.Clamp(dotP, 0f, maxDistance);
	}
}
