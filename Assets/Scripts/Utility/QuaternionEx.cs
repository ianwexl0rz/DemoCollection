using UnityEngine;

public static class QuaternionEx
{
	public static void ToSwingTwist(this Quaternion rotation, Vector3 twistAxis, out Quaternion swing, out Quaternion twist, bool twistFirst = false)
	{
		Vector3 twistVector = Vector3.Project(rotation.Vector(), twistAxis);
		twist = VectorScalar(twistVector, rotation.w);
		
		
		swing = twistFirst ? Quaternion.Inverse(twist) * rotation : rotation * Quaternion.Inverse(twist);
	}

	/// <summary>
	/// Creates a rotation from a vector and scalar part.
	/// </summary>
	public static Quaternion VectorScalar(Vector3 vector, float scalar)
	{
		return new Quaternion(vector.x, vector.y, vector.z, scalar).normalized;
	}

	/// <summary>
	/// Returns the vector part of a quaternion (x, y, z.)
	/// </summary>
	public static Vector3 Vector(this Quaternion q)
	{
		return new Vector3(q.x, q.y, q.z);
	}

	/// <summary>
	/// Negates a quaternion component-wise.
	/// </summary>
	public static Quaternion Negate(this Quaternion q)
	{
		return new Quaternion(-q.x, -q.y, -q.z, -q.w);
	}

	/// <summary>
	/// Returns the torque required to rotate the quaternion to the target over deltaTime.
	/// </summary>
	public static Vector3 TorqueTo(this Quaternion current, Quaternion target, float deltaTime)
	{
		Quaternion toTarget = target * Quaternion.Inverse(current);
		if (toTarget.w < 0) toTarget = toTarget.Negate();
		toTarget.ToAngleAxis(out float angle, out Vector3 axis);

		return axis * (angle * Mathf.Deg2Rad / deltaTime);
	}

	/// <summary>
	/// Returns the torque required to rotate the quaternion to the target over deltaTime.
	/// </summary>
	public static Vector3 TorqueTo(this Quaternion current, Quaternion target, float deltaTime, float maxSpeed)
	{
		Quaternion toTarget = target * Quaternion.Inverse(current);
		if (toTarget.w < 0) toTarget = toTarget.Negate();
		toTarget.ToAngleAxis(out float angle, out Vector3 axis);

		return axis * Mathf.Min(angle * Mathf.Deg2Rad / deltaTime, maxSpeed);
	}

	public static Quaternion SwingTwistInterpolate(this Quaternion from, Quaternion to, Vector3 twistAxis, float t)
	{
		Quaternion rotation = to * Quaternion.Inverse(from);
		rotation.ToSwingTwist(twistAxis, out Quaternion swing, out Quaternion twist);

		return Quaternion.Slerp(Quaternion.identity, swing, t) * Quaternion.Slerp(Quaternion.identity, twist, t);
	}
	
	public static Quaternion SmoothDamp(Quaternion current, Quaternion target, ref Quaternion currentVelocity, float smoothTime)
	{
		if (Quaternion.Dot(current, target) < 0) target = target.Negate();

		var result = new Quaternion(
			Mathf.SmoothDamp(current.x, target.x, ref currentVelocity.x, smoothTime),
			Mathf.SmoothDamp(current.y, target.y, ref currentVelocity.y, smoothTime),
			Mathf.SmoothDamp(current.z, target.z, ref currentVelocity.z, smoothTime),
			Mathf.SmoothDamp(current.w, target.w, ref currentVelocity.w, smoothTime)
		).normalized;
		
		var rDt = 1f / Time.deltaTime;
		currentVelocity.x = (result.x - current.x) * rDt;
		currentVelocity.y = (result.y - current.y) * rDt;
		currentVelocity.z = (result.z - current.z) * rDt;
		currentVelocity.w = (result.w - current.w) * rDt;
		
		return result;
	}
}
