using System;
using UnityEngine;

public interface ITrackable
{ 
	Vector3 GetEyesPosition();

	Vector3 GetGroundPosition();

	Vector3 GetCenter();

	float GetHeight();

	bool IsVisible();
}
	
