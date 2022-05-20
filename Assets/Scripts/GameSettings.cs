using UnityEngine;

[CreateAssetMenu(fileName = "Game Settings", menuName = "Game Settings")]
public class GameSettings : ScriptableObject
{
	public float deadZone = 0f;
	public float _lookSensitivityX = 300f;
	public float _lookSensitivityY = 150f;
	
	//TODO: Make "invert X/Y custom" editor vars and store value as +/-
	public float LookSensitivityX => _lookSensitivityX * (InvertX ? -1 : 1);

	public float LookSensitivityY => _lookSensitivityY * (InvertY ? 1 : -1);

	public bool InvertY;
	public bool InvertX;
}
