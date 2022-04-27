using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

[Serializable]
public class MainMode : GameMode
{
    private static IEnumerator hitPause;
    private static MainMode _instance;

    [FormerlySerializedAs("playerCharacter")]
    [FormerlySerializedAs("activePlayer")]
    [SerializeField] private CharacterMotor playerCharacterMotor = null;
    [SerializeField] private ThirdPersonCamera gameCamera = null;
    [SerializeField] private LockOn lockOn = null;

    [Header("Actor Controllers")]
    [SerializeField] private PlayerController playerBrain = null;
    [SerializeField] private ActorController followerBrain = null;
    
    [Header("Hit Sparks")]
    [SerializeField] private GameObject blueHitSparkPrefab = null;
    [SerializeField] private GameObject orangeHitSparkPrefab = null;
    
    private int playerIndex;
    private List<CharacterMotor> playerCharacters;
    private Camera mainCamera;
    private bool cachedPhysicsPaused;
    [NonSerialized] private static bool physicsPaused;
    [NonSerialized] private bool initialized;

    public ThirdPersonCamera MainCamera => gameCamera;

    private static List<Entity> entities = new List<Entity>();
    
    private static List<CombatEvent> combatEvents = new List<CombatEvent>();

    public static void AddEntity(Entity entity) => entities.Add(entity);
    public static void RemoveEntity(Entity entity) => entities.Remove(entity);

    public override void Init(object context, Action callback = null)
    {
        if (!initialized)
        {
            initialized = true;

            _instance = this;
            cachedPhysicsPaused = false;
            
            mainCamera = gameCamera.GetComponent<Camera>();
            gameCamera.Init();
            lockOn.Init();

            playerCharacters = new List<CharacterMotor>(Object.FindObjectsOfType<CharacterMotor>());
            if (playerCharacterMotor != null) playerIndex = playerCharacters.IndexOf(playerCharacterMotor);

            SetPlayer(playerCharacterMotor, true);
            Cursor.lockState = CursorLockMode.Locked;
        }

        SetPhysicsPaused(cachedPhysicsPaused);
        callback?.Invoke();
    }

    public override void FixedTick(float deltaTime)
    {
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
        if (Input.GetKeyDown(KeyCode.RightBracket)) playerCharacterMotor.TakeDamage(-5f);
        if (Input.GetKeyDown(KeyCode.LeftBracket)) playerCharacterMotor.TakeDamage(5f);

        foreach (var entity in entities) entity.Tick(Time.deltaTime);
    }

    public override void LateTick(float deltaTime)
    {
        foreach (var entity in entities) entity.LateTick(deltaTime);

        var lookInput = player.GetAxis2D(PlayerAction.LookHorizontal, PlayerAction.LookVertical);

        gameCamera.UpdatePositionAndRotation(lookInput, playerCharacterMotor.TrackedTarget);
        
        if (GameManager.Settings.InvertX) lookInput.x *= -1; // TODO: Setting should be cached.
        //if (GameManager.Settings.InvertY) lookInput.y *= -1; // TODO: Setting should be cached.

        if (!physicsPaused) LockOn.UpdateLockOn(playerCharacterMotor, mainCamera, lookInput);
        ResolveCombatEvents();

        // Pause game if requested.
        if (player.GetButtonDown(PlayerAction.Pause))
            SetMode<PauseMode>();
    }

    public override void Clean()
    {
        cachedPhysicsPaused = physicsPaused;
        player.StopVibration();
    }

    public static bool PhysicsPaused => physicsPaused;

    public static void SetPhysicsPaused(bool value)
    {
        if (physicsPaused == value) return;
        physicsPaused = value;
        foreach (var entity in entities) entity.SetPaused(value);
    }

    private void CyclePlayer()
    {
        playerIndex = (playerIndex + 1) % playerCharacters.Count;
        SetPlayer(playerCharacters[playerIndex]);
    }

    private void SetPlayer(CharacterMotor newPlayer, bool immediate = false)
    {
        if (playerCharacterMotor != null && playerCharacterMotor != newPlayer)
        {
            playerCharacterMotor.SetController(followerBrain, newPlayer); // Set the old active player to use Follower Brain
        }

        playerCharacterMotor = newPlayer;
        playerCharacterMotor.SetController(playerBrain); // Set the active player to use Player Brain
        gameCamera.SetFollowTarget(playerCharacterMotor, immediate); // Set the camera to follow the active player
    }
    
    public static void AddCombatEvents(IEnumerable<CombatEvent> combatEvent) => combatEvents.AddRange(combatEvent);

    public void ResolveCombatEvents()
    {
        foreach (var combatEvent in combatEvents)
        {
            var (instigator, target, point, direction, attackData) = combatEvent;
            target.ApplyHit(instigator, point, direction, attackData);
            hitPause = HitPause(Time.fixedDeltaTime * attackData.hitPause);

            if (GetHitSpark(target, out var hitSpark)) Object.Instantiate(hitSpark, point, Quaternion.identity);
        }

        combatEvents.Clear();
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
        SetPhysicsPaused(true);

        while (duration > 0)
        {
            duration -= Time.deltaTime;
            player.SetVibration(0, 0.5f);
            player.SetVibration(1, 0.5f);
            yield return null;
        }

        player.StopVibration();
        SetPhysicsPaused(false);
    }
}