using UnityEngine;

[CreateAssetMenu]
public class PID3 : ScriptableObject
{
	public float Kp = 1;
	public float Ki = 0;
	public float Kd = 0.1f;

	public Vector3 Output(Vector3 current, Vector3 setpoint, ref Vector3 integral, ref Vector3 error, float deltaTime)
	{
		var previousError = error;
		error = setpoint - current;
		integral += error * deltaTime;
		var derivative = (error - previousError) / deltaTime;

		return error * Kp + integral * Ki + derivative * Kd;
	}
}
