using UnityEngine;

[CreateAssetMenu(fileName = "Game Settings", menuName = "Game Settings")]
public class GameSettings : ScriptableObject
{
	public float deadZone = 0f;
	public float _lookSensitivityX = 300f;
	public float _lookSensitivityY = 150f;

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

	[SerializeField] private bool invertX = true;
	[SerializeField] private bool invertY = true;

	private static GameSettings _instance;
	public static GameSettings I
	{
		get
		{
			if(!_instance)
			{
				_instance = GameManager.I.gameSettings;
			}
			return _instance;
		}
	}

	public void SetInvertX(bool value)
	{
		invertX = value;
	}

	public void SetInvertY(bool value)
	{
		invertY = value;
	}
}
