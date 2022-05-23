using System;
using System.Collections.Generic;
using System.Linq;
using DemoCollection;
using DemoCollection.DataBinding;
using UnityEngine;

[CreateAssetMenu]
public class LockOn : ScriptableObject
{
	public struct IndicatorData
	{
		public bool HasTarget;
		public float TargetX;
		public float TargetY;

		public IndicatorData(bool enabled, Vector3 lockOnScreenPos)
		{
			HasTarget = enabled && lockOnScreenPos.z > 0;
			TargetX = lockOnScreenPos.x;
			TargetY = UnityEngine.Screen.height - lockOnScreenPos.y;
		}
	}

	public static event Action<ITrackable> TargetChanged;
	
	public static event Action<IndicatorData> SetIndicatorData;
	
	[Header("Lock On")]
	[SerializeField] private LockOnIndicator lockOnIndicatorPrefab = null;
	[SerializeField] private float range = 10f;
	[SerializeField] private float angleThreshold = 90f;

	private static LockOnIndicator _lockOnIndicator;
	private static bool _lookInputStale;
	private static LockOn _instance;
	private static Actor _playerActor;

	[SerializeField] private List<GameObject> potentialDataProviders = new List<GameObject>();
	
	public static ITrackable TrackableCandidate { get; private set; }

	public void Init()
	{
		_instance = this;
		_lockOnIndicator = Instantiate(lockOnIndicatorPrefab);
		_lockOnIndicator.Init();
	}
    
	private static Vector2 GetScreenPos(ITrackable trackable, Camera mainCamera)
    {
        return (Vector2) mainCamera.WorldToScreenPoint(trackable.GetEyesPosition()) - new Vector2(mainCamera.pixelWidth, mainCamera.pixelHeight) * 0.5f;
    }

    public static void UpdateLockOn(Camera mainCamera, Vector2 lookInput)
    {
	    // TODO: Potential targets should add or remove themselves on Tick.

        var potentialTargets = Physics.OverlapSphere(_playerActor.transform.position, _instance.range)
	        .Select(collider => collider.GetComponent<ITrackable>())
	        .Except(new []{_playerActor.GetComponent<ITrackable>(), _playerActor.TrackedTarget, null})
	        .Where(trackable => trackable.IsVisible())
            .ToDictionary(trackable => trackable, trackable => GetScreenPos(trackable, mainCamera));
        
        if (_playerActor.TrackedTarget == null)
        {
            TrackableCandidate = GetTrackableClosestToCenter(potentialTargets);
            _lookInputStale = true;
        }
        else
        {
	        TrackableCandidate = _playerActor.TrackedTarget;
	        
	        if (_lookInputStale && lookInput.Equals(Vector2.zero)) _lookInputStale = false;
            if (!_lookInputStale && lookInput.sqrMagnitude > 0)
            {
                var halfScreenPixels = new Vector2(mainCamera.pixelWidth, mainCamera.pixelHeight) * 0.5f;
                var currentTargetScreenPos = (Vector2) mainCamera.WorldToScreenPoint(_playerActor.TrackedTarget.GetEyesPosition()) - halfScreenPixels;
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
        _lockOnIndicator.UpdatePosition(_playerActor.TrackedTarget != null, TrackableCandidate,
            mainCamera.transform.position);

        var lockOnScreenPos = mainCamera.WorldToScreenPoint(_lockOnIndicator.transform.position);
        SetIndicatorData?.Invoke(new IndicatorData(_playerActor.TrackedTarget != null, lockOnScreenPos));
    }

    private static ITrackable GetTrackableClosestToCenter(Dictionary<ITrackable, Vector2> potentialTargets)
	{
		if (potentialTargets.Count == 0)
			return null;

		ITrackable bestTrackable = null;
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

	private static ITrackable GetTrackableClosestToVector(Dictionary<ITrackable, Vector2> potentialTargets, Vector2 lookInput, Vector2 currentTargetScreenPos)
	{
		ITrackable bestTrackable = null;
		var smallestAngle = Mathf.Infinity;

		foreach(var screenPosByTarget in potentialTargets)
		{
			var angle = Vector2.Angle(lookInput.normalized, screenPosByTarget.Value - currentTargetScreenPos.normalized);
			if (angle >= smallestAngle) continue;

			bestTrackable = screenPosByTarget.Key;
			smallestAngle = angle;
		}
		
		return smallestAngle <= _instance.angleThreshold ? bestTrackable : null;
	}

	public static void OnTargetChanged(ITrackable newTarget)
	{
		TargetChanged?.Invoke(newTarget);
	}

	public static void SetPlayerActor(Actor playerActor)
	{
		_playerActor = playerActor;
	}
}
