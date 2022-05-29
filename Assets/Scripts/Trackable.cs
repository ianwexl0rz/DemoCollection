using System;
using System.Collections;
using System.Collections.Generic;
using ActorFramework;
using DemoCollection.DataBinding;
using UnityEngine;

public interface ITracker
{
	Trackable TrackedTarget { get; set; }
}

[RequireComponent(typeof(Entity))]
public class Trackable : ObservableMonobehaviour
{
	public event Action Destroyed;

	private Entity _owner;
	
	[SerializeField] private bool canBeTracked;
	[SerializeField] private bool inRangeOfPlayer;
	[SerializeField] private bool onScreen;
	[SerializeField] private Vector3 screenPos;
	[SerializeField] private Health health;

	public bool CanBeTracked
	{
		get => canBeTracked;
		set
		{
			if (canBeTracked != value)
			{
				canBeTracked = value;
				PlayerController.Instance.Tracking.OnSetCanBeTracked(this, value);
			}
		}
	}
	
	public bool InRangeOfPlayer
	{
		get => inRangeOfPlayer;
		private set => SetProperty(ref inRangeOfPlayer, value);
	}

	public bool OnScreen
	{
		get => onScreen;
		private set => SetProperty(ref onScreen, value);
	}

	public float ScreenPosX => screenPos.x;

	public float ScreenPosY => Screen.height - screenPos.y;

	public Vector3 ScreenPos
	{
		get => screenPos;
		set
		{
			screenPos = value;
			OnPropertyChanged("ScreenPosX");
			OnPropertyChanged("ScreenPosY");
		}
	}

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
		PlayerController.PossessedActor += StartTracking;
		PlayerController.ReleasedActor += StopTracking;
	}

	protected void OnDestroy()
	{
		CanBeTracked = false;
		Destroyed?.Invoke();
	}

	private void StartTracking(Actor newActor)
	{
		if (newActor != _owner) CanBeTracked = true;
	}
	
	private void StopTracking(Actor oldActor)
	{
		CanBeTracked = false;
		InRangeOfPlayer = false;
		ScreenPos = Vector3.zero;
		OnScreen = false;
	}

	public void CheckProximityToPlayer(Actor playerActor, Camera mainCamera)
	{
		var distance = Vector3.Distance(playerActor.transform.position, _owner.transform.position);
		InRangeOfPlayer = CanBeTracked && distance <= PlayerController.Instance.Tracking.Range;
		
		ScreenPos = InRangeOfPlayer
			? (Vector3) mainCamera.WorldToScreenPoint(GetCenter())
			: Vector3.zero;

		OnScreen = InRangeOfPlayer && ScreenPos.z > 0 && mainCamera.pixelRect.Contains(ScreenPos);
	}
}
	
