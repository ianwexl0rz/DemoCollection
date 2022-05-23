using System;
using UnityEngine;

public interface ITrackable
{
	public event Action Destroyed;

	Vector3 GetEyesPosition();

	Vector3 GetGroundPosition();

	Vector3 GetCenter();

	float GetHeight();

	bool IsVisible();
}
	
