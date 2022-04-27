using System;
using UnityEngine;

public interface ITrackable
{ 
	Vector3 GetEyesPosition();

	Vector3 GetGroundPosition();

	bool IsVisible { get; set; }
}

public static class TrackableExtensions
{
	public static Vector3 GetCenter(this ITrackable trackable) => (trackable.GetEyesPosition() + trackable.GetGroundPosition()) * 0.5f;

	public static float GetHeight(this ITrackable trackable) => Vector3.Distance(trackable.GetEyesPosition(), trackable.GetGroundPosition()) * 2f;
}
	
