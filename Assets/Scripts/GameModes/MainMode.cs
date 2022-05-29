using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

[Serializable]
public class MainMode : GameMode
{
    private static IEnumerator _hitPause;
    private static MainMode _instance;
    private static List<Entity> _entities;
    private static List<CombatEvent> _combatEvents;
    private static bool _physicsPaused;
    
    [SerializeField] private ThirdPersonCamera gameCamera = null;

    [Header("Actor Controllers")]
    [SerializeField] private ActorController playerBrain = null;
    [SerializeField] private ActorController followerBrain = null;
    
    [Header("Hit Sparks")]
    [SerializeField] private GameObject blueHitSparkPrefab = null;
    [SerializeField] private GameObject orangeHitSparkPrefab = null;
    
    private Actor _playerActor = null;
    private int _actorIndex;
    private List<Actor> _actorsInScene;
    private Camera _mainCamera;
    [NonSerialized] private bool _cachedPhysicsPaused;

    public ThirdPersonCamera MainCamera => gameCamera;

    public static void AddEntity(Entity entity) => _entities.Add(entity);
    public static void RemoveEntity(Entity entity) => _entities.Remove(entity);

    public override void Init(object context = null, Action callback = null)
    {
        if (_instance == null)
        {
            _instance = this;
            _entities = new List<Entity>();
            _combatEvents = new List<CombatEvent>();
            _actorsInScene = new List<Actor>(Object.FindObjectsOfType<Actor>());
            _mainCamera = gameCamera.GetComponent<Camera>();
            gameCamera.Init();
        }

        if (context is Actor actor)
        {
            _actorIndex = _actorsInScene.IndexOf(actor);
            SetPlayer(actor, true);
        }

        SetPhysicsPaused(_cachedPhysicsPaused);
        callback?.Invoke();
    }

    public override void FixedTick(float deltaTime)
    {
        foreach (var entity in _entities) entity.OnFixedTick(Time.fixedDeltaTime);
    }

    public override void Tick(float deltaTime)
    {
        // Swap characters
        if (player.GetButtonDown(PlayerAction.SwitchPlayer)) CyclePlayer();

        // TODO: Player input should be received even if hit pause is active.

        // Wait for hit pause to conclude.
        if (_hitPause != null && _hitPause.MoveNext()) return;

        // Hold the right bumper for slow-mo!
        Time.timeScale = player.GetButton(PlayerAction.SlowMo) ? 0.01f : 1f;

        // (Debug) Adjust health.
        if (Input.GetKeyDown(KeyCode.RightBracket)) _playerActor.Health.ApplyChange(5);
        if (Input.GetKeyDown(KeyCode.LeftBracket)) _playerActor.Health.ApplyChange(-5);

        foreach (var entity in _entities) entity.OnTick(Time.deltaTime);
    }

    public override void LateTick(float deltaTime)
    {
        foreach (var entity in _entities) entity.OnLateTick(deltaTime);
        
        ResolveCombatEvents();

        // Pause game if requested.
        if (player.GetButtonDown(PlayerAction.Pause))
            SetMode<PauseMode>();
    }

    public override void Clean()
    {
        _cachedPhysicsPaused = _physicsPaused;
        player.StopVibration();
    }

    public static bool PhysicsPaused => _physicsPaused;

    public static void SetPhysicsPaused(bool value)
    {
        if (_physicsPaused == value) return;
        _physicsPaused = value;
        foreach (var entity in _entities) entity.OnSetPaused(value);
    }

    private void CyclePlayer()
    {
        _actorIndex = (_actorIndex + 1) % _actorsInScene.Count;
        SetPlayer(_actorsInScene[_actorIndex]);
    }

    private void SetPlayer(Actor newPlayer, bool immediate = false)
    {
        if (_playerActor == newPlayer) return;

        if (_playerActor != null)
        {
            // Set the old active player to use Follower Brain
            _playerActor.SetController(followerBrain, newPlayer.GetComponent<Trackable>());
        }

        _playerActor = newPlayer;
        _playerActor.SetController(playerBrain, new PlayerControllerContext()
        {
            MainCamera = _mainCamera,
            GameCamera = gameCamera
        });
        
        gameCamera.SetFollowTarget(_playerActor.GetComponent<Trackable>(), immediate); // Set the camera to follow the active player
        LockOn.SetPlayerActor(_playerActor);
    }
    
    public static void AddCombatEvent(CombatEvent combatEvent) => _combatEvents.Add(combatEvent);

    public void ResolveCombatEvents()
    {
        foreach (var combatEvent in _combatEvents)
        {
            var (instigator, target, point, direction, attackData) = combatEvent;
            target.OnGetHit(combatEvent);
            _hitPause = HitPause(Time.fixedDeltaTime * attackData.hitPause);

            if (GetHitSpark(target, out var hitSpark)) Object.Instantiate(hitSpark, point, Quaternion.identity);
        }

        _combatEvents.Clear();
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