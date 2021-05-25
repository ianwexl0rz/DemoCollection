using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Rewired;
using UnityEngine;

[CreateAssetMenu]
public class CombatSystem : ScriptableObject
{
    private static Player player;
    private static List<CombatEvent> combatEvents;
    private static IEnumerator hitPause;

    [Header("Lock On")]
    [SerializeField] private LockOnCollider lockOnColliderPrefab = null;
    [SerializeField] private LockOnIndicator lockOnIndicatorPrefab = null;

    [Header("Hit Sparks")]
    [SerializeField] private GameObject blueHitSparkPrefab = null;
    [SerializeField] private GameObject orangeHitSparkPrefab = null;

    private LockOnCollider lockOnCollider;
    private LockOnIndicator lockOnIndicator;
    private bool lookInputStale;

    public static ILockOnTarget LockOnCandidate { get; private set; }

    private static List<ILockOnTarget> targetsInRange = new List<ILockOnTarget>();

    public void Init(Player player)
    {
        CombatSystem.player = player;
        combatEvents = new List<CombatEvent>();
        
        lockOnCollider = Instantiate(lockOnColliderPrefab);
        lockOnIndicator = Instantiate(lockOnIndicatorPrefab);
        lockOnIndicator.Init();
        
        MainMode.OnSetPlayer += actor => lockOnCollider.Init(actor.transform);
    }
    
    public static void AddCombatEvents(IEnumerable<CombatEvent> combatEvent) => combatEvents.AddRange(combatEvent);

    public void ResolveCombatEvents()
    {
        foreach (var combatEvent in combatEvents)
        {
            var (instigator, target, point, direction, attackData) = combatEvent;
            target.ApplyHit(instigator, point, direction, attackData);
            hitPause = HitPause(Time.fixedDeltaTime * attackData.hitPause);

            if (GetHitSpark(target, out var hitSpark)) Instantiate(hitSpark, point, Quaternion.identity);
        }

        combatEvents.Clear();
    }

    private Vector2 GetScreenPos(ILockOnTarget lockOnTarget, Camera mainCamera)
    {
        return (Vector2) mainCamera.WorldToScreenPoint(lockOnTarget.GetLookPosition()) - new Vector2(mainCamera.pixelWidth, mainCamera.pixelHeight) * 0.5f;
    }

    public void UpdateLockOn(Character playerCharacter, Camera mainCamera)
    {
        var currentTarget = playerCharacter.lockOnTarget;
        var potentialTargets = targetsInRange
            .Except(new []{playerCharacter, currentTarget})
            .Where(k => k.IsVisible)
            .ToDictionary(k => k, k => GetScreenPos(k, mainCamera));

        if (currentTarget == null)
        {
            LockOnCandidate = lockOnCollider.GetTargetClosestToCenter(potentialTargets);
            lookInputStale = true;
        }
        else
        {
            var lookVector = player.GetAxis2D(PlayerAction.LookHorizontal, PlayerAction.LookVertical);
            if (GameManager.Settings.InvertX) lookVector.x *= -1; // TODO: Setting should be cached.
            if (GameManager.Settings.InvertY) lookVector.y *= -1; // TODO: Setting should be cached.
            if (lookInputStale && lookVector.Equals(Vector2.zero)) lookInputStale = false;
            if (!lookInputStale && lookVector.sqrMagnitude > 0)
            {
                var halfScreenPixels = new Vector2(mainCamera.pixelWidth, mainCamera.pixelHeight) * 0.5f;
                var currentTargetScreenPos = (Vector2) mainCamera.WorldToScreenPoint(currentTarget.GetLookPosition()) - halfScreenPixels;
                var newTarget = lockOnCollider.GetTargetClosestToVector(potentialTargets, lookVector, currentTargetScreenPos);
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

    public static void AddTargetInRange(ILockOnTarget lockOnTarget)
    {
        if (targetsInRange.Contains(lockOnTarget)) return;
        targetsInRange.Add(lockOnTarget);

        if (lockOnTarget is IDamageable destructable)
            destructable.OnDestroyCallback = () => targetsInRange.Remove(lockOnTarget);
    }

    public static void RemoveTargetInRange(ILockOnTarget lockOnTarget)
    {
        if (!targetsInRange.Contains(lockOnTarget)) return;
        targetsInRange.Remove(lockOnTarget);
		
        // TODO: If the player is currently locked on, unlock.
        if (lockOnTarget is IDamageable destructable)
            destructable.OnDestroyCallback = () => { };
    }
    
    private bool GetHitSpark(Entity entity, out GameObject hitSpark)
    {
        return hitSpark =
            entity is Actor ? blueHitSparkPrefab :
            !(entity is null) ? orangeHitSparkPrefab : null;
    }
    
    private static IEnumerator HitPause(float duration)
    {
        yield return new WaitForEndOfFrame();
        MainMode.SetPhysicsPaused(true);

        while (duration > 0)
        {
            duration -= Time.deltaTime;
            player.SetVibration(0, 0.5f);
            player.SetVibration(1, 0.5f);
            yield return null;
        }

        player.StopVibration();
        MainMode.SetPhysicsPaused(false);
    }

    public static bool TickHitPause() => hitPause != null && hitPause.MoveNext();
}