using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "Game Settings", menuName = "Game Settings")]
public class ControlSettings : ScriptableObject
{
	public float deadZone = 0.06f;
	public Vector2 lookSensitivity = new Vector2(300f,200f);
	public bool invertY = true;

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
