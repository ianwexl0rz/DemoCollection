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

	public static Vector3 LowestPoint(this CapsuleCollider capsuleCollider)
	{
		return capsuleCollider.ClosestPoint(capsuleCollider.transform.TransformPoint(capsuleCollider.center) + Vector3.down * capsuleCollider.height * 0.5f);
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

	public static void DecomposeSwingTwist(Quaternion q, Vector3 twistAxis, out Quaternion swing, out Quaternion twist)
	{
		Vector3 r = q.GetXYZ();
		twist = Quaternion.identity;
		swing = Quaternion.identity;
		
		// singularity: rotation by 180 degree
		if (r.sqrMagnitude < Mathf.Epsilon)
		{
			Vector3 rotatedTwistAxis = q * twistAxis;
			Vector3 swingAxis = 
			Vector3.Cross(twistAxis, rotatedTwistAxis);
		
			if (swingAxis.sqrMagnitude > Mathf.Epsilon)
			{
				float swingAngle = Vector3.Angle(twistAxis, rotatedTwistAxis);
				swing = Quaternion.AngleAxis(swingAngle, swingAxis);
			}
			else
			{
				// more singularity: 
				// rotation axis parallel to twist axis
				swing = Quaternion.identity; // no swing
			}
		
			// always twist 180 degree on singularity
			twist = Quaternion.AngleAxis(180.0f, twistAxis);
			return;
		}

		// meat of swing-twist decomposition
		Vector3 p = Vector3.ProjectOnPlane(r, twistAxis);
		twist = new Quaternion(p.x, p.y, p.z, q.w);
		twist = Quaternion.Normalize(twist);
		swing = q * Quaternion.Inverse(twist);
	}

	public static Quaternion Sterp(this Quaternion a, Quaternion b, Vector3 twistAxis, float t)
	{
		Quaternion deltaRotation = b * Quaternion.Inverse(a);
		
		Quaternion swingFull;
		Quaternion twistFull;
		DecomposeSwingTwist(deltaRotation, twistAxis, out swingFull, out twistFull);
		
		Quaternion swing = Quaternion.Slerp(Quaternion.identity, swingFull, t);
		Quaternion twist = Quaternion.Slerp(Quaternion.identity, twistFull, t);
		
		return twist * swing;
	}

	public static Vector3 GetXYZ(this Quaternion q)
	{
		return new Vector3(q.x, q.y, q.z);
	}

	public static void RotateTo(this Rigidbody rb, PID3 rotationPid, PID3 angularVelocityPid, Quaternion target, float dt)
	{
		Quaternion toTarget = target * Quaternion.Inverse(rb.rotation);

		Vector3 rotationCorrection = rotationPid.GetOutput(toTarget.GetXYZ() * Mathf.Sign(toTarget.w), dt);
		Vector3 angularVelocityCorrection = angularVelocityPid.GetOutput(-rb.angularVelocity, dt);

		Vector3 torque = rotationCorrection * Mathf.Rad2Deg + angularVelocityCorrection;
		rb.AddTorque(torque, ForceMode.Acceleration);
	}

	public static void RotateTo(this Rigidbody rb, Quaternion target)
	{
		Quaternion toTarget = target * Quaternion.Inverse(rb.rotation);

		Vector3 rotationCorrection = toTarget.GetXYZ() * Mathf.Sign(toTarget.w) / Time.fixedDeltaTime;
		Vector3 angularVelocityCorrection = -rb.angularVelocity;

		Vector3 torque = rotationCorrection * Mathf.Rad2Deg;// + angularVelocityCorrection;
		rb.AddTorque(torque, ForceMode.VelocityChange);
	}

	public static void RotateTo(this Rigidbody rb, PID3 rotationPid, PID3 angularVelocityPid, Quaternion target, float dt, Vector3 stiffness)
	{
		Quaternion toTarget = target * Quaternion.Inverse(rb.rotation);

		Vector3 rotationCorrection = rotationPid.GetOutput(toTarget.GetXYZ() * Mathf.Sign(toTarget.w), dt);
		Vector3 angularVelocityCorrection = angularVelocityPid.GetOutput(-rb.angularVelocity, dt);

		Vector3 torque = rotationCorrection * Mathf.Rad2Deg + angularVelocityCorrection;
		rb.AddTorque(Vector3.Scale(torque, stiffness), ForceMode.Acceleration);
	}
}
