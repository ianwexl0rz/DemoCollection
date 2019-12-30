using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameManager
{
    [Serializable]
    public class MainMode : GameMode
    {
        [SerializeField] private Character activePlayer;
        [SerializeField] private ThirdPersonCamera gameCamera;
        
        [Header("UI")]
        [SerializeField] private HealthBar healthBar = null;

        [Header("Lock On")]
        [SerializeField] private LockOnCollider lockOnColliderPrefab = null;
        [SerializeField] private LockOnIndicator lockOnIndicatorPrefab = null;

        [Header("Hit Sparks")]
        [SerializeField] private GameObject blueHitSparkPrefab = null;
        [SerializeField] private GameObject orangeHitSparkPrefab = null;

        [Header("Actor Controllers")]
        [SerializeField] private PlayerController playerBrain = null;
        [SerializeField] private ActorController followerBrain = null;

        public Action<bool> OnPauseGame = delegate { };
        
        private int playerIndex;
        private List<Character> playerCharacters;
        private LockOnCollider lockOnCollider;
        private LockOnIndicator lockOnIndicator;
        private Camera mainCamera;
        private bool lockedOn;
        private bool lookInputStale;
        private IEnumerator hitPause;
        private bool cachedPhysicsPaused;
        [NonSerialized] private bool physicsPaused;
        [NonSerialized] private bool initialized;

        public ThirdPersonCamera MainCamera => gameCamera;
        
        private static List<Entity> entities = new List<Entity>();
        private static List<CombatEvent> combatEvents = new List<CombatEvent>();

        public static void AddEntity(Entity entity) => entities.Add(entity);
        public static void RemoveEntity(Entity entity) => entities.Remove(entity);
        public static void AddCombatEvent(CombatEvent combatEvent) => combatEvents.Add(combatEvent);

        public override void Init(object context, Action callback = null)
        {
            if (!initialized)
            {
                mainCamera = gameCamera.GetComponent<Camera>();
                gameCamera.Init();

                initialized = true;
                cachedPhysicsPaused = false;
                lockOnCollider = Instantiate(lockOnColliderPrefab);
                lockOnIndicator = Instantiate(lockOnIndicatorPrefab);
                lockOnIndicator.Init();

                playerCharacters = new List<Character>(FindObjectsOfType<Character>());
                if (activePlayer != null) playerIndex = playerCharacters.IndexOf(activePlayer);

                SetActivePlayer(activePlayer, true);
                Cursor.lockState = CursorLockMode.Locked;
            }

            callback?.Invoke();
        }

        private Vector3 lastVelocity;
        
        public override void FixedTick(float deltaTime)
        {
            ResolveCombatEvents();
            foreach (var entity in entities) entity.FixedTick(Time.fixedDeltaTime);
        }

        public override void Tick(float deltaTime)
        {
            // Swap characters
            if (player.GetButtonDown(PlayerAction.SwitchPlayer)) CyclePlayer();
            
            // TODO: Player input should be received even if hit pause is active.
            
            // Wait for hit pause to conclude.
            if (hitPause != null && hitPause.MoveNext()) return;

            // Hold the right bumper for slow-mo!
            Time.timeScale = player.GetButton(PlayerAction.SlowMo) ? 0.01f : 1f;

            // (Debug) Adjust health.
            if (Input.GetKeyDown(KeyCode.RightBracket)) activePlayer.Health += 5f;
            if (Input.GetKeyDown(KeyCode.LeftBracket)) activePlayer.Health -= 5f;

            foreach (var entity in entities) entity.Tick(Time.deltaTime);
        }

        public override void LateTick(float deltaTime)
        {
            foreach (var entity in entities) entity.LateTick(Time.deltaTime);

            var lookInput = player.GetAxis2D(PlayerAction.LookHorizontal, PlayerAction.LookVertical);
            var lockOnTarget = activePlayer.IsLockedOn ? activePlayer.lockOnTarget : null;
            gameCamera.UpdatePositionAndRotation(lookInput, lockOnTarget);

            if (!physicsPaused) UpdateLockOn();

            // Pause game if requested.
            if (player.GetButtonDown(PlayerAction.Pause)) PauseGame();
        }

        public override void Clean()
        {
        }

        private bool PhysicsPaused
        {
            set
            {
                if (physicsPaused == value) return;
                physicsPaused = value;
                foreach (var entity in entities) entity.SetPaused(value);
            }
        }

        private void PauseGame()
        {
            cachedPhysicsPaused = physicsPaused;
            PhysicsPaused = true;
            OnPauseGame(true);
            player.StopVibration();
            SetMode(GameModeType.Pause, new PauseMode.Context
            {
                onResume = UnpauseGame
            });
        }

        private void UnpauseGame()
        {
            PhysicsPaused = cachedPhysicsPaused;
            OnPauseGame(false);
        }

        private IEnumerator HitPause(float duration)
        {
            yield return new WaitForEndOfFrame();
            PhysicsPaused = true;

            while (duration > 0)
            {
                duration -= Time.deltaTime;
                player.SetVibration(0, 0.5f);
                player.SetVibration(1, 0.5f);
                yield return null;
            }

            player.StopVibration();
            PhysicsPaused = false;
        }

        private void CyclePlayer()
        {
            playerIndex = (playerIndex + 1) % playerCharacters.Count;
            SetActivePlayer(playerCharacters[playerIndex]);
        }

        private void SetActivePlayer(Character newTarget, bool immediate = false)
        {
            if (activePlayer != newTarget && activePlayer)
            {
                activePlayer.SetController(followerBrain, newTarget); // Set the old active player to use Follower Brain
                healthBar.UnregisterPlayer(activePlayer);
            }

            activePlayer = newTarget;
            lockOnCollider.Init(activePlayer.transform);
            activePlayer.SetController(playerBrain); // Set the active player to use Player Brain
            gameCamera.SetFollowTarget(activePlayer, immediate); // Set the camera to follow the active player
            healthBar.RegisterPlayer(activePlayer);
        }

        private void UpdateLockOn()
        {
            var closestToCenter = lockOnCollider.GetTargetClosestToCenter(mainCamera, activePlayer);

            if (!activePlayer.lockOn)
            {
                activePlayer.lockOnTarget = closestToCenter;
                lookInputStale = true;
            }

            if (activePlayer.IsLockedOn)
            {
                var lookVector = player.GetAxis2D(PlayerAction.LookHorizontal, PlayerAction.LookVertical);
                if (Settings.InvertX) lookVector.x *= -1; // TODO: Setting should be cached.
                if (Settings.InvertY) lookVector.y *= -1; // TODO: Setting should be cached.
                if (lookInputStale && lookVector.Equals(Vector2.zero)) lookInputStale = false;
                if (!lookInputStale && lookVector.sqrMagnitude > 0)
                {
                    var newTarget = lockOnCollider.GetTargetClosestToVector(activePlayer, activePlayer.lockOnTarget, lookVector);
                    if (!ReferenceEquals(newTarget, null))
                    {
                        activePlayer.lockOnTarget = newTarget;
                        lookInputStale = true;
                    }
                }
            }

            // Update lock-on indicator position.
            lockOnIndicator.UpdatePosition(activePlayer.lockOn, activePlayer.lockOnTarget,
                mainCamera.transform.position);
        }

        private void ResolveCombatEvents()
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

        private bool GetHitSpark(Entity entity, out GameObject hitSpark)
        {
            return hitSpark =
                entity is Actor ? blueHitSparkPrefab :
                !(entity is null) ? orangeHitSparkPrefab : null;
        }
    }
}