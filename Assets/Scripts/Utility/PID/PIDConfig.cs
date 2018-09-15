using UnityEngine;

[CreateAssetMenu]
public class PIDConfig : ScriptableObject
{
	public float Kp = 1;
	public float Ki = 0;
	public float Kd = 0.1f;
}
