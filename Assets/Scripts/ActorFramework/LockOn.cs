using System;
using System.Collections.Generic;
using System.Linq;
using DemoCollection;
using DemoCollection.DataBinding;
using UnityEngine;

[Serializable]
public class LockOn
{
	public static event Action<Trackable> TargetChanged;
	
	public static event Action<Trackable> SetIndicatorData;

	[field: SerializeField] public float Range { get; private set; } = 10f;
	[field: SerializeField] public float AngleThreshold { get; private set; } = 90f;
	
	[field: SerializeField] public List<Trackable> Trackables { get; private set; }

	private static bool _lookInputStale;
	private static LockOn _instance;
	private static Actor _playerActor;
	
	public static Trackable TrackableCandidate { get; private set; }

	public void Init()
	{
		_instance = this;
		Trackables = new List<Trackable>();
	}

    public static void UpdateLockOn(Camera mainCamera, Vector2 lookInput)
    {
	    var potentialTargets = _instance.Trackables
		    .Where(trackable => trackable.InRangeOfPlayer)
	        .Where(trackable => trackable.OnScreen)
	        .ToDictionary(trackable => trackable, trackable => (Vector2)trackable.ScreenPos - mainCamera.pixelRect.size * 0.5f);

        var currentTarget = _playerActor.TrackedTarget;
        if (currentTarget == null)
        {
            TrackableCandidate = GetTrackableClosestToCenter(potentialTargets);
            _lookInputStale = true;
        }
        else
        {
	        TrackableCandidate = currentTarget;
	        
	        if (_lookInputStale && lookInput.Equals(Vector2.zero)) _lookInputStale = false;
            if (!_lookInputStale && lookInput.sqrMagnitude > 0)
            {
                var currentTargetScreenPos = (Vector2)currentTarget.ScreenPos - mainCamera.pixelRect.size * 0.5f;
                var newTarget = GetTrackableClosestToVector(potentialTargets, lookInput, currentTargetScreenPos);
                if (!ReferenceEquals(newTarget, null))
                {
                    TrackableCandidate = newTarget;
                    _lookInputStale = true;
                        
                    _playerActor.TrackedTarget = TrackableCandidate;
                }
            }
        }

        // Update lock-on indicator position.
        // _lockOnIndicator.UpdatePosition(_playerActor.TrackedTarget != null, TrackableCandidate,
        //     mainCamera.transform.position);

        SetIndicatorData?.Invoke(currentTarget);
    }

    private static Trackable GetTrackableClosestToCenter(Dictionary<Trackable, Vector2> potentialTargets)
	{
		if (potentialTargets.Count == 0)
			return null;

		Trackable bestTrackable = null;
		var bestDistance = Mathf.Infinity;

		foreach (var screenPosByTarget in potentialTargets)
		{
			var distanceFromCenter = screenPosByTarget.Value.magnitude;
			if (distanceFromCenter >= bestDistance) continue;
			
			bestTrackable = screenPosByTarget.Key;
			bestDistance = distanceFromCenter;
		}

		return bestTrackable;
	}

	private static Trackable GetTrackableClosestToVector(Dictionary<Trackable, Vector2> potentialTargets, Vector2 lookInput, Vector2 currentTargetScreenPos)
	{
		Trackable bestTrackable = null;
		var smallestAngle = Mathf.Infinity;

		foreach(var screenPosByTarget in potentialTargets)
		{
			var angle = Vector2.Angle(lookInput.normalized, screenPosByTarget.Value - currentTargetScreenPos.normalized);
			if (angle >= smallestAngle) continue;

			bestTrackable = screenPosByTarget.Key;
			smallestAngle = angle;
		}
		
		return smallestAngle <= _instance.AngleThreshold ? bestTrackable : null;
	}
	
	public void OnSetCanBeTracked(Trackable trackable, bool value)
	{
		if (value) Trackables.Add(trackable);
		else Trackables.Remove(trackable);
	}

	public static void OnTargetChanged(Trackable newTarget) => TargetChanged?.Invoke(newTarget);

	public static void SetPlayerActor(Actor playerActor) => _playerActor = playerActor;

	public void RequestRefreshTrackables(Actor playerActor, Camera mainCamera)
	{
		foreach (var trackable in Trackables) trackable.CheckProximityToPlayer(playerActor, mainCamera);
	}
}
