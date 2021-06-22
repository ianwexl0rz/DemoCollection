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
	
	public static ILockOnTarget LockOnCandidate { get; private set; }

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
    
	private static Vector2 GetScreenPos(ILockOnTarget lockOnTarget, Camera mainCamera)
    {
        return (Vector2) mainCamera.WorldToScreenPoint(lockOnTarget.GetLookPosition()) - new Vector2(mainCamera.pixelWidth, mainCamera.pixelHeight) * 0.5f;
    }

    public static void UpdateLockOn(Character playerCharacter, Camera mainCamera, Vector2 lookInput)
    {
        var currentTarget = playerCharacter.lockOnTarget;
        
        // TODO: Potential targets should add or remove themselves on Tick.
        
        var potentialTargets = Physics.OverlapSphere(playerCharacter.transform.position, _instance.range)
	        .Select(c => c.GetComponent<ILockOnTarget>())
            .Except(new []{playerCharacter, currentTarget, null})
            .Where(k => k.IsVisible)
            .ToDictionary(k => k, k => GetScreenPos(k, mainCamera));

        if (currentTarget == null)
        {
            LockOnCandidate = GetTargetClosestToCenter(potentialTargets);
            lookInputStale = true;
        }
        else
        {
	        if (lookInputStale && lookInput.Equals(Vector2.zero)) lookInputStale = false;
            if (!lookInputStale && lookInput.sqrMagnitude > 0)
            {
                var halfScreenPixels = new Vector2(mainCamera.pixelWidth, mainCamera.pixelHeight) * 0.5f;
                var currentTargetScreenPos = (Vector2) mainCamera.WorldToScreenPoint(currentTarget.GetLookPosition()) - halfScreenPixels;
                var newTarget = GetTargetClosestToVector(potentialTargets, lookInput, currentTargetScreenPos);
                if (!ReferenceEquals(newTarget, null))
                {
                    LockOnCandidate = newTarget;
                    lookInputStale = true;
                        
                    playerCharacter.SetLockOnTarget(LockOnCandidate);
                }
            }
        }

        // Update lock-on indicator position.
        lockOnIndicator.UpdatePosition(playerCharacter.lockOnTarget != null, LockOnCandidate,
            mainCamera.transform.position);
    }

    private static ILockOnTarget GetTargetClosestToCenter(Dictionary<ILockOnTarget, Vector2> potentialTargets)
	{
		if (potentialTargets.Count == 0)
			return null;

		ILockOnTarget bestTarget = null;
		var bestDistance = Mathf.Infinity;

		foreach (var screenPosByTarget in potentialTargets)
		{
			var distanceFromCenter = screenPosByTarget.Value.magnitude;
			if (distanceFromCenter >= bestDistance) continue;
			
			bestTarget = screenPosByTarget.Key;
			bestDistance = distanceFromCenter;
		}

		return bestTarget;
	}

	private static ILockOnTarget GetTargetClosestToVector(Dictionary<ILockOnTarget, Vector2> potentialTargets, Vector2 lookInput, Vector2 currentTargetScreenPos)
	{
		ILockOnTarget bestTarget = null;
		var smallestAngle = Mathf.Infinity;

		foreach(var screenPosByTarget in potentialTargets)
		{
			var angle = Vector2.Angle(lookInput.normalized, screenPosByTarget.Value - currentTargetScreenPos.normalized);
			if (angle >= smallestAngle) continue;

			bestTarget = screenPosByTarget.Key;
			smallestAngle = angle;
		}
		
		return smallestAngle <= _instance.angleThreshold ? bestTarget : null;
	}
}
