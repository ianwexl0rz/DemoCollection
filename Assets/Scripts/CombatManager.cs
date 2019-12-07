using System.Collections.Generic;
using Rewired;
using UnityEngine;

[CreateAssetMenu]
public class CombatManager : ScriptableObject
{
    [Header("Lock On")] [SerializeField] private LockOnCollider lockOnColliderPrefab = null;
    [SerializeField] private LockOnIndicator lockOnIndicatorPrefab = null;

    [Header("Hit Sparks")] [SerializeField]
    private GameObject blueHitSparkPrefab = null;

    [SerializeField] private GameObject orangeHitSparkPrefab = null;

    private LockOnCollider collider;
    private LockOnIndicator indicator;
    private Character owner;
    private Camera mainCamera;
    private bool lockedOn;
    private bool lookInputStale;

    public void Init(Camera camera)
    {
        mainCamera = camera;
        collider = Instantiate(lockOnColliderPrefab);
        indicator = Instantiate(lockOnIndicatorPrefab);
        indicator.Init();
    }

    public void SetOwner(Character owner)
    {
        this.owner = owner;
        collider.Init(owner.transform);
    }

    public void UpdateLockOn(Player player)
    {
        var closestToCenter = collider.GetTargetClosestToCenter(mainCamera, owner);

        if (!owner.lockOn)
        {
            owner.lockOnTarget = closestToCenter;
            lookInputStale = true;
        }

        if (owner.IsLockedOn)
        {
            var lookVector = player.GetAxis2D(PlayerAction.LookHorizontal, PlayerAction.LookVertical);
            if (lookInputStale && lookVector.Equals(Vector2.zero)) lookInputStale = false;
            if (!lookInputStale && lookVector.sqrMagnitude > 0)
            {
                var current = owner.lockOnTarget;
                var newTarget = collider.GetTargetClosestToVector(owner, current, lookVector);
                if (!ReferenceEquals(newTarget, null))
                {
                    owner.lockOnTarget = newTarget;
                    lookInputStale = true;
                }
            }
        }

        // Update lock-on indicator position.
        indicator.UpdatePosition(owner.lockOn, owner.lockOnTarget, mainCamera.transform.position);
    }

    public void ResolveCombatEvents(ref List<CombatEvent> combatEvents)
    {
        foreach (var combatEvent in combatEvents)
        {
            var (instigator, target, point, direction, attackData) = combatEvent;
            target.ApplyHit(instigator, point, direction, attackData);
            GameManager.I.InitHitPause(Time.fixedDeltaTime * attackData.hitPause);

            if (GetHitSpark(target, out var hitSpark)) Instantiate(hitSpark, point, Quaternion.identity);
        }

        combatEvents.Clear();
    }

    private bool GetHitSpark(Entity entity, out GameObject hitSpark)
    {
        return hitSpark =
            entity is Actor ? blueHitSparkPrefab :
            !(entity is null) ? orangeHitSparkPrefab : null;
    }
}