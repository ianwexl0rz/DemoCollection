using System.Collections;
using System.Collections.Generic;
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

    public void UpdateLockOn(Character playerCharacter, Camera mainCamera)
    {
        var closestToCenter = lockOnCollider.GetTargetClosestToCenter(mainCamera, playerCharacter);

        if (!playerCharacter.IsLockedOn())
        {
            LockOnCandidate = closestToCenter;
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
                var newTarget = lockOnCollider.GetTargetClosestToVector(playerCharacter, playerCharacter.lockOnTarget, lookVector);
                if (!ReferenceEquals(newTarget, null))
                {
                    LockOnCandidate = newTarget;
                    lookInputStale = true;
                        
                    playerCharacter.SetLockOnTarget(LockOnCandidate);
                }
            }
        }

        // Update lock-on indicator position.
        lockOnIndicator.UpdatePosition(playerCharacter.IsLockedOn(), LockOnCandidate,
            mainCamera.transform.position);
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