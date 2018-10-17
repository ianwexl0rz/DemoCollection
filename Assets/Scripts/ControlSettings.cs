using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "Game Settings", menuName = "Game Settings")]
public class ControlSettings : ScriptableObject
{
	public float deadZone = 0.06f;
	public float _lookSensitivityX = 200f;
	public float _lookSensitivityY = 112.5f;

	//TODO: Make "invert X/Y custom" editor vars and store value as +/-
	public float lookSensitivityX
	{
		get
		{
			return _lookSensitivityX * (invertX ? -1 : 1);
		}
	}

	public float lookSensitivityY
	{
		get
		{
			return _lookSensitivityY * (invertY ? 1 : -1);
		}
	}

	[SerializeField]
	private bool invertX = true;
	[SerializeField]
	private bool invertY = true;

	private static ControlSettings _instance;
	public static ControlSettings I
	{
		get
		{
			if(!_instance)
			{
				_instance = GameManager.I.controlSettings;
			}
			return _instance;
		}
	}
}
