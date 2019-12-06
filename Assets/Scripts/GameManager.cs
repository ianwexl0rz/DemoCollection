using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Linq;
using Rewired;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour
{
	public Character activePlayer;
	public GameSettings gameSettings;
	public ThirdPersonCamera mainCamera;
	[SerializeField] private HUD hud = null;

	[Header("Actor Controllers")]
	public PlayerController playerBrain;
	public ActorController followerBrain;

	[FormerlySerializedAs("lockOnColliderPrefab")]
	[Header("Gameplay")]
	[SerializeField] private LockOnSystem lockOnSystemPrefab = null;
	[SerializeField] private GameObject hitSpark = null;
	[SerializeField] private GameObject hitSpark2 = null;

	private LockOnSystem lockOnSystem = null;
	//private LockOnIndicator lockOnIndicator = null;
	private int playerIndex;
	private List<Character> playerCharacters;
	private readonly List<Entity> entities = new List<Entity>();

	public Action<bool> PauseAllPhysics = delegate { };
	public Action<bool> OnPauseGame = delegate { };
	private bool gamePaused, physicsPaused, togglePaused;
	private Coroutine hitPauseCoroutine;
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

		//QualitySettings.maxQueuedFrames = 1;
		Application.targetFrameRate = 60;

		lockOnSystem = Instantiate(lockOnSystemPrefab);
		lockOnSystem.SetMainCamera(mainCamera.GetComponent<Camera>());

		// Cache reference to player.
		player = ReInput.players.GetPlayer(0);

		DontDestroyOnLoad(this);

		// Lock cursor by default.
		//Cursor.lockState = CursorLockMode.Locked;

		playerCharacters = new List<Character>(FindObjectsOfType<Character>());

		if(activePlayer != null)
		{
			playerIndex = playerCharacters.IndexOf(activePlayer);
		}

		//StartCoroutine(LateFixedUpdate());
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
		foreach(var entity in entities) entity.OnFixedUpdate(Time.fixedDeltaTime);
	}

	public void LateUpdate()
	{
		foreach(var entity in entities) entity.OnLateUpdate(Time.deltaTime);

		if (!gamePaused) mainCamera.UpdatePositionAndRotation();

		if (!physicsPaused)
		{
			var closestToCenter = lockOnSystem.GetTargetClosestToCenter(activePlayer);
			
			if(!activePlayer.lockOn)
			{
				activePlayer.lockOnTarget = closestToCenter;
				lookInputStale = true;
			}

			if(activePlayer.IsLockedOn)
			{
				var lookVector = player.GetAxis2D(PlayerAction.LookHorizontal, PlayerAction.LookVertical);
				if(lookInputStale && lookVector.Equals(Vector2.zero)) lookInputStale = false;
				if(!lookInputStale && lookVector.sqrMagnitude > 0)
				{
					var current = activePlayer.lockOnTarget;
					var newTarget = lockOnSystem.GetTargetClosestToVector(activePlayer, current, lookVector);
					if(!ReferenceEquals(newTarget, null))
					{
						activePlayer.lockOnTarget = newTarget;
						lookInputStale = true;
					}
				}
			}

			// Update lock-on indicator position.
			lockOnSystem.UpdateIndicator(activePlayer.lockOn, activePlayer.lockOnTarget);
		}

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

	public void ClickOnPlayer(Character newTarget)
	{
		// Only switch targets if the mouse is unlocked.
		if(Cursor.lockState == CursorLockMode.Locked) return;
		
		SetActivePlayer(newTarget);
		Cursor.lockState = CursorLockMode.Locked;
	}

	public static bool GetHitSpark(Entity entity, out GameObject hitSpark)
	{
		return hitSpark =
			entity is Actor ? I.hitSpark :
			!(entity is null) ? I.hitSpark2 : null;
	}

	public Character GetFirstInactivePlayer() => playerCharacters.FirstOrDefault(t => t != activePlayer);

	public void AddEntity(Entity entity) => entities.Add(entity);

	public void RemoveEntity(Entity entity) => entities.Remove(entity);

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
		lockOnSystem.Init(activePlayer.transform);
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
