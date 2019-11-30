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

	public static Quaternion SwingTwistInterpolate(this Quaternion from, Quaternion to, Vector3 twistAxis, float t)
	{
		Quaternion deltaRotation = to * Quaternion.Inverse(from);
		deltaRotation.SwingTwistDecomposition(twistAxis, out Quaternion swingFull, out Quaternion twistFull);

		return Quaternion.Slerp(Quaternion.identity, swingFull, t) * Quaternion.Slerp(Quaternion.identity, twistFull, t);
	}

	public static Vector3 GetXYZ(this Quaternion q)
	{
		return new Vector3(q.x, q.y, q.z);
	}

	public static Quaternion Negate(this Quaternion q)
	{
		return new Quaternion(-q.x, -q.y, -q.z, -q.w);
	}

	public static void RotateTo(this Rigidbody rb, PID3 torquePID, Quaternion target, float dt)
	{
		Quaternion toTarget = target * Quaternion.Inverse(rb.rotation);
		if (toTarget.w < 0) toTarget = toTarget.Negate();
		toTarget.ToAngleAxis(out float angle, out Vector3 axis);

		Vector3 targetTorque = axis * angle * Mathf.Deg2Rad / dt;
		Vector3 torque = torquePID.GetOutput(targetTorque - rb.angularVelocity, dt);
		rb.AddTorque(torque, ForceMode.Acceleration);
	}

	public static void RotateTo(this Rigidbody rb, PID3 torquePID, Quaternion target, float dt, Vector3 stiffness)
	{
		Quaternion toTarget = target * Quaternion.Inverse(rb.rotation);
		if (toTarget.w < 0) toTarget = toTarget.Negate();
		toTarget.ToAngleAxis(out float angle, out Vector3 axis);

		Vector3 targetTorque = axis * angle * Mathf.Deg2Rad / dt;
		Vector3 torque = torquePID.GetOutput(targetTorque - rb.angularVelocity, dt);
		rb.AddTorque(Vector3.Scale(torque, stiffness), ForceMode.Acceleration);
	}

	/**
	* Splits up a rotation into two other rotations(swing and twist), so that rotation = swing * twist
	* The original rotation has 3 Degrees of freedom, swing has 2 dof and twist has 1 dof
	*
	* @see    https://stackoverflow.com/questions/3684269/component-of-a-quaternion-rotation-around-an-axis
	* @see    https://euclideanspace.com/maths/geometry/rotations/for/decomposition/
	*
	* @param  rotation  The rotation which moves direction onto the new direction vector
	* @param  direction The direction vector which would get moved by rotation into it's new position.
	*                   rotation * direction == swing * twist * direction
	* @param  swing     Pointer to the Quaternion which will become the swing rotation
	* @param  twist     Pointer to the Quaternion which will become the twist rotation
	*/
	public static void SwingTwistDecomposition(this Quaternion rotation, Vector3 direction, out Quaternion swing, out Quaternion twist)
	{
		Vector3 rotationAxis = new Vector3(rotation.x, rotation.y, rotation.z);
		Vector3 twistAxis = Vector3.Project(rotationAxis, direction);
		twist = new Quaternion(twistAxis.x, twistAxis.y, twistAxis.z, rotation.w);
		twist = twist.normalized;
		swing = rotation * Quaternion.Inverse(twist);
	}

	public static Quaternion SwingTwistLimit(this Quaternion q, Vector3 limit)
	{
		// Make sure the scalar part is positive. Since quaternions have a double covering, q and -q represent the same orientation.
		if (q.w < 0)
		{
			q = q.Negate(); // Negate the quaternion. Still represents the same orientation.
		}

		// Here swing and twist are dependent. The twist can be applied before or after the swing. After (parent ->swing -> twist -> child) makes the most sense
		float rx, ry, rz;
		float s = q.x * q.x + q.w * q.w;
		if (s < Mathf.Epsilon)
		{
			// swing by 180 degrees is a singularity. We assume twist is zero.
			rx = 0;
			ry = q.y;
			rz = q.z;
		}
		else
		{
			float r = 1 / Mathf.Sqrt(s);

			rx = q.x * r;
			ry = (q.w * q.y - q.x * q.z) * r;
			rz = (q.w * q.z + q.x * q.y) * r;
		}

		// Twist Limit
		rx = Mathf.Clamp(rx, -limit.x, limit.x);

		// Swing Limit
		ry = Mathf.Clamp(ry, -limit.y, limit.y);
		rz = Mathf.Clamp(rz, -limit.z, limit.z);

		var qTwist = new Quaternion(rx, 0, 0, Mathf.Sqrt(Mathf.Max(0, 1 - rx * rx)));
		var qSwing = new Quaternion(0, ry, rz, Mathf.Sqrt(Mathf.Max(0, 1 - ry * ry - rz * rz)));

		return qSwing * qTwist;
	}
}
