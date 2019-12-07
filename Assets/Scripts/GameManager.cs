using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;
using Rewired;

public class GameManager : MonoBehaviour
{
	public Character activePlayer;
	public GameSettings gameSettings;
	public ThirdPersonCamera mainCamera;
	[SerializeField] private HUD hud = null;

	[Header("Actor Controllers")]
	public PlayerController playerBrain;
	public ActorController followerBrain;
	
	public Action<bool> PauseAllPhysics = delegate { };
	public Action<bool> OnPauseGame = delegate { };
	
	[Header("Gameplay")]
	[SerializeField] private CombatManager combatManager = null;

	private int playerIndex;
	private List<Character> playerCharacters;
	private readonly List<Entity> entities = new List<Entity>();
	private List<CombatEvent> combatEvents = new List<CombatEvent>();
	private Coroutine hitPauseCoroutine;
	private bool gamePaused, physicsPaused, togglePaused;
    private bool lookInputStale;
    
    private static GameManager instance;
    
	public Player player { get; private set; }

	public static GameManager I
	{
		get { if(!instance) { instance = FindObjectOfType<GameManager>(); } return instance; }
	}

	public bool PhysicsPaused
	{
		get => physicsPaused;
		private set
		{
			if(physicsPaused == value) return;
			physicsPaused = value;
			PauseAllPhysics(value);
		}
	}

	public bool GamePaused => gamePaused;

	#region UNITY_METHODS
	private void Awake()
	{
		instance = this;
		
		Application.targetFrameRate = 60;
		combatManager.Init(mainCamera.GetComponent<Camera>());

		// Cache reference to player.
		player = ReInput.players.GetPlayer(0);

		DontDestroyOnLoad(this);

		playerCharacters = new List<Character>(FindObjectsOfType<Character>());
		if(activePlayer != null) playerIndex = playerCharacters.IndexOf(activePlayer);
	}

	private void Start()
	{
		// TODO: Spawn all characters from proxy meshes
		// 1) can take advantage of object pooling
		// 2) load character status from room data

		SetActivePlayer(activePlayer, true);

		Cursor.lockState = CursorLockMode.Locked;
	}

	private void Update()
	{
		// Swap characters
		if(player.GetButtonDown(PlayerAction.SwitchPlayer)) { CyclePlayer(); }

		// Hold the right bumper for slow-mo!
		Time.timeScale = player.GetButton(PlayerAction.SlowMo) ? 0.25f : 1f;

		foreach(var entity in entities) entity.OnUpdate(Time.deltaTime);
		
		if(Input.GetKeyDown(KeyCode.RightBracket)) activePlayer.Health += 5f;
		if(Input.GetKeyDown(KeyCode.LeftBracket)) activePlayer.Health -= 5f;
	}

	public void FixedUpdate()
	{
		combatManager.ResolveCombatEvents(ref combatEvents);
		foreach(var entity in entities) entity.OnFixedUpdate(Time.fixedDeltaTime);
	}

	public void LateUpdate()
	{
		foreach(var entity in entities) entity.OnLateUpdate(Time.deltaTime);

		if (!gamePaused) mainCamera.UpdatePositionAndRotation();

		if (!physicsPaused) combatManager.UpdateLockOn(player);

		// Pause game if requested.
		if(player.GetButtonDown(PlayerAction.Pause)) TogglePaused();
	}

	#endregion

	#region PUBLIC_METHODS

	public void InitHitPause(float duration)
	{
		this.OverrideCoroutine(ref hitPauseCoroutine, HitPause(duration));
	}

	public void TogglePaused()
	{
		gamePaused = !gamePaused;
		hud.SetPaused(gamePaused);
		OnPauseGame(gamePaused);
		PhysicsPaused = gamePaused;
	}

	public void AddEntity(Entity entity) => entities.Add(entity);

	public void RemoveEntity(Entity entity) => entities.Remove(entity);

	public void AddCombatEvent(CombatEvent combatEvent) => combatEvents.Add(combatEvent);

	#endregion

	#region PRIVATE_METHODS
	
	private void CyclePlayer()
	{
		playerIndex = (playerIndex + 1) % playerCharacters.Count;
		SetActivePlayer(playerCharacters[playerIndex]);
	}

	private void SetActivePlayer(Character newTarget, bool immediate = false)
	{
		if(activePlayer != newTarget && activePlayer)
		{
			activePlayer.SetController(followerBrain); // Set the old active player to use Follower Brain
			hud.UnregisterPlayer(activePlayer);
		}

		activePlayer = newTarget;
		combatManager.SetOwner(activePlayer);
		activePlayer.SetController(playerBrain); // Set the active player to use Player Brain
		mainCamera.SetTarget(activePlayer, immediate); // Set the camera to follow the active player
		hud.RegisterPlayer(activePlayer);
	}

	private IEnumerator HitPause(float duration)
	{
		yield return new WaitForEndOfFrame();

		PhysicsPaused = true;

		while (duration > 0)
		{
			if (!gamePaused)
			{
				duration -= Time.deltaTime;
				player.SetVibration(0, 0.5f);
				player.SetVibration(1, 0.5f);
			}
			else
			{
				player.StopVibration();
			}

			yield return null;
		}

		player.StopVibration();
		PhysicsPaused = false;
	}
	#endregion
}
