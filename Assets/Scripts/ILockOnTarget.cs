using System;
using UnityEngine;

public interface ILockOnTarget
{ 
	Vector3 GetLookPosition();

	Vector3 GetGroundPosition();

	bool IsVisible { get; set; }
}
