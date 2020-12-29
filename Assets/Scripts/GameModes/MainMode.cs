using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

[Serializable]
public class MainMode : GameMode
{
    public static event Action<Actor> OnSetPlayer = delegate {  };
    public static event Action OnUnsetPlayer = delegate {  };
    
    [FormerlySerializedAs("activePlayer")]
    [SerializeField] private Character playerCharacter = null;
    [SerializeField] private ThirdPersonCamera gameCamera = null;
    [SerializeField] private CombatSystem combatSystem = null;

    [Header("Actor Controllers")]
    [SerializeField] private PlayerController playerBrain = null;
    [SerializeField] private ActorController followerBrain = null;
    
    private int playerIndex;
    private List<Character> playerCharacters;

    private Camera mainCamera;
    private bool cachedPhysicsPaused;
    [NonSerialized] private static bool physicsPaused;
    [NonSerialized] private bool initialized;

    public ThirdPersonCamera MainCamera => gameCamera;
    public Character PlayerCharacter => playerCharacter;

    private static List<Entity> entities = new List<Entity>();

    public static void AddEntity(Entity entity) => entities.Add(entity);
    public static void RemoveEntity(Entity entity) => entities.Remove(entity);

    public override void Init(object context, Action callback = null)
    {
        if (!initialized)
        {
            mainCamera = gameCamera.GetComponent<Camera>();
            gameCamera.Init();

            initialized = true;
            cachedPhysicsPaused = false;
            
            combatSystem.Init(player);

            playerCharacters = new List<Character>(Object.FindObjectsOfType<Character>());
            if (playerCharacter != null) playerIndex = playerCharacters.IndexOf(playerCharacter);

            SetPlayer(playerCharacter, true);
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
        if (CombatSystem.TickHitPause()) return;

        // Hold the right bumper for slow-mo!
        Time.timeScale = player.GetButton(PlayerAction.SlowMo) ? 0.01f : 1f;

        // (Debug) Adjust health.
        if (Input.GetKeyDown(KeyCode.RightBracket)) playerCharacter.TakeDamage(-5f);
        if (Input.GetKeyDown(KeyCode.LeftBracket)) playerCharacter.TakeDamage(5f);

        foreach (var entity in entities) entity.Tick(Time.deltaTime);
    }

    public override void LateTick(float deltaTime)
    {
        foreach (var entity in entities) entity.LateTick(Time.deltaTime);

        var lookInput = player.GetAxis2D(PlayerAction.LookHorizontal, PlayerAction.LookVertical);
        
        gameCamera.UpdatePositionAndRotation(lookInput, playerCharacter.lockOnTarget);
        
        if (!physicsPaused) combatSystem.UpdateLockOn(playerCharacter, mainCamera);
        combatSystem.ResolveCombatEvents();

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

    private void SetPlayer(Character newTarget, bool immediate = false)
    {
        if (playerCharacter != newTarget && playerCharacter)
        {
            playerCharacter.SetController(followerBrain, newTarget); // Set the old active player to use Follower Brain
            UnsetPlayer();
        }

        playerCharacter = newTarget;
        playerCharacter.SetController(playerBrain); // Set the active player to use Player Brain
        gameCamera.SetFollowTarget(playerCharacter, immediate); // Set the camera to follow the active player

        OnSetPlayer(playerCharacter);
    }
        
    private void UnsetPlayer()
    {
        playerCharacter = null;
        OnUnsetPlayer();
    }
}