using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu]
public class LockOn : ScriptableObject
{
	[Header("Lock On")]
	[SerializeField] private LockOnIndicator lockOnIndicatorPrefab = null;
	[SerializeField] private float range = 10f;
	[SerializeField] private float angleThreshold = 90f;

	private static LockOnIndicator lockOnIndicator;
	private static bool lookInputStale;
	private static LockOn _instance;

	[SerializeField] private List<GameObject> potentialDataProviders = new List<GameObject>();
	
	public static ITrackable TrackableCandidate { get; private set; }

	public void Init()
	{
		_instance = this;
		lockOnIndicator = Instantiate(lockOnIndicatorPrefab);
		lockOnIndicator.Init();
		//MainMode.OnSetPlayer += actor => _instance.SetFocusedTransform(actor.transform);
	}

	// public void SetFocusedTransform(Transform parent)
	// {
	// 	gameObject.SetActive(false);
	// 	var t = transform;
	// 	t.SetParent(parent);
	// 	t.localPosition = Vector3.zero;
	// 	gameObject.SetActive(true);
	// }
    
	private static Vector2 GetScreenPos(ITrackable trackable, Camera mainCamera)
    {
        return (Vector2) mainCamera.WorldToScreenPoint(trackable.GetEyesPosition()) - new Vector2(mainCamera.pixelWidth, mainCamera.pixelHeight) * 0.5f;
    }

    public static void UpdateLockOn(CharacterMotor playerCharacterMotor, Camera mainCamera, Vector2 lookInput)
    {
        var currentTarget = playerCharacterMotor.TrackedTarget;
        
        // TODO: Potential targets should add or remove themselves on Tick.

        var potentialTargets = Physics.OverlapSphere(playerCharacterMotor.transform.position, _instance.range)
	        .Select(collider => collider.GetComponent<ITrackable>())
	        .Except(new []{playerCharacterMotor, currentTarget, null})
	        .Where(trackable => trackable.IsVisible)
            .ToDictionary(trackable => trackable, trackable => GetScreenPos(trackable, mainCamera));
        
        if (currentTarget == null)
        {
            TrackableCandidate = GetTrackableClosestToCenter(potentialTargets);
            lookInputStale = true;
        }
        else
        {
	        TrackableCandidate = currentTarget;
	        
	        if (lookInputStale && lookInput.Equals(Vector2.zero)) lookInputStale = false;
            if (!lookInputStale && lookInput.sqrMagnitude > 0)
            {
                var halfScreenPixels = new Vector2(mainCamera.pixelWidth, mainCamera.pixelHeight) * 0.5f;
                var currentTargetScreenPos = (Vector2) mainCamera.WorldToScreenPoint(currentTarget.GetEyesPosition()) - halfScreenPixels;
                var newTarget = GetTrackableClosestToVector(potentialTargets, lookInput, currentTargetScreenPos);
                if (!ReferenceEquals(newTarget, null))
                {
                    TrackableCandidate = newTarget;
                    lookInputStale = true;
                        
                    playerCharacterMotor.TrackedTarget = TrackableCandidate;
                }
            }
        }

        // Update lock-on indicator position.
        lockOnIndicator.UpdatePosition(playerCharacterMotor.TrackedTarget != null, TrackableCandidate,
            mainCamera.transform.position);
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
}
