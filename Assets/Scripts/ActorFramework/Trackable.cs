using System;
using System.Collections;
using System.Collections.Generic;
using ActorFramework;
using DemoCollection.DataBinding;
using UnityEngine;

[Serializable]
public struct TrackingData
{
	public static readonly TrackingData Empty = new TrackingData(Vector3.zero, false);

	public Vector3 ScreenPos;
	public bool OnScreen;

	public TrackingData(Vector3 screenPos, bool onScreen)
	{
		ScreenPos = screenPos;
		OnScreen = onScreen;
	}
}

[RequireComponent(typeof(Entity))]
public class Trackable : ObservableMonobehaviour
{
	[SerializeField] private Health health;

	private Entity _owner;
	private TrackingData _trackingData;
	private TrackingData TrackingData
	{
		get => _trackingData;
		set
		{
			_trackingData = value;
			OnPropertyChanged("ScreenPosX");
			OnPropertyChanged("ScreenPosY");
			OnPropertyChanged("OnScreen");
		}
	}

	public bool OnScreen => _trackingData.OnScreen;
	
	public Vector3 ScreenPos => _trackingData.ScreenPos;
	
	public float ScreenPosX => _trackingData.ScreenPos.x;

	public float ScreenPosY => Screen.height - _trackingData.ScreenPos.y;

	public Health Health
	{
		get => health;
		private set => SetProperty(ref health, value);
	}

	public virtual Vector3 GetEyesPosition() => transform.position;

	public virtual Vector3 GetGroundPosition() => transform.position;

	public virtual Vector3 GetCenter() => transform.position;

	public virtual float GetHeight() => 0;

	public virtual bool IsVisible() => true;

	protected virtual void Awake()
	{
		
		_owner = GetComponent<Entity>();
		Health = _owner.GetComponent<Health>();
		PlayerController.PotentialTargets.Add(this);
	}

	protected void OnDestroy() => PlayerController.PotentialTargets.Remove(this);

	public void SetTrackableData(bool validTarget, Camera mainCamera)
	{
		if (!validTarget)
		{
			TrackingData = TrackingData.Empty;
		}
		else
		{
			var screenPos = mainCamera.WorldToScreenPoint(GetCenter());
			screenPos = new Vector3(Mathf.Round(screenPos.x), Mathf.Round(screenPos.y), screenPos.z);
			TrackingData = new TrackingData
			{
				ScreenPos = screenPos,
				OnScreen = screenPos.z > 0 && mainCamera.pixelRect.Contains(screenPos),
			};
		}
	}
}
	
