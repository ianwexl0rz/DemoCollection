using UnityEngine;
using System.Collections.Generic;
using InControl;
using System;
using UnityStandardAssets.ImageEffects;
using System.Collections;
using System.Linq;

public class GameManager : MonoBehaviour
{
	public Character activePlayer;
	public GameSettings gameSettings;
	public ThirdPersonCamera mainCamera;
	[SerializeField] private HUD hud = null;

	[Header("Actor Controllers")]
	public PlayerController playerBrain;
	public ActorController followerBrain;

	[Header("Gameplay")]
	[SerializeField] private LockOnCollider lockOnColliderPrefab = null;
	[SerializeField] private LockOnIndicator lockOnIndicatorPrefab = null;
	[SerializeField] private GameObject hitSpark = null;
	[SerializeField] private GameObject hitSpark2 = null;

	private LockOnCollider lockOnCollider = null;
	private LockOnIndicator lockOnIndicator = null;
	private int playerIndex;
	private List<Character> playerCharacters;
	private readonly List<Entity> entities = new List<Entity>();

	public Action<bool> PauseAllPhysics = delegate { };
	public Action<bool> OnPauseGame = delegate { };
	private bool gamePaused, physicsPaused, togglePaused;
	private Coroutine hitPauseCoroutine;

	private static GameManager instance;

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

		lockOnCollider = Instantiate(lockOnColliderPrefab);
		lockOnCollider.SetMainCamera(mainCamera.GetComponent<Camera>());

		lockOnIndicator = Instantiate(lockOnIndicatorPrefab);
		lockOnIndicator.gameObject.SetActive(false);

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

		/*
		SetActivePlayer(playerCharacters[targetIndex], true);

		foreach(Character p in playerCharacters)
		{
			if(p != activePlayer) p.SetController(followerBrain);
		}
		*/
	}

	//private IEnumerator LateFixedUpdate()
	//{
	//	while(true)
	//	{
	//		yield return new WaitForFixedUpdate();

	//		if(gamePaused) { continue; }

	//		mainCamera.UpdateReferenceRotation();

	//		//mainCamera.UpdatePositionAndRotation(); // Update camera after physics

	//		//// Update the potential lock on target
	//		//if(!activePlayer.lockOn)
	//		//{
	//		//	activePlayer.lockOnTarget = lockOnCollider.GetTargetClosestToCenter(activePlayer);

	//		//	//potentialTargets.Sort(SortByProximityToScreenCenter);
	//		//	//activePlayer.lockOnTarget = potentialTargets[0];
	//		//}
	//	}
	//}

	private void Update()
	{
		// Swap characters
		if(InputManager.ActiveDevice.Action4.WasPressed || Input.GetKeyDown(KeyCode.Tab)) { CyclePlayer(); }

		// Hold the right bumper for slow-mo!
		Time.timeScale = InputManager.ActiveDevice.RightBumper.IsPressed || Input.GetKey(KeyCode.LeftAlt) ? 0.25f : 1f;

		foreach(var entity in entities)
		{
			entity.OnUpdate(Time.deltaTime);
		}
		
		if(Input.GetKeyDown(KeyCode.RightBracket)) activePlayer.Health += 5f;
		if(Input.GetKeyDown(KeyCode.LeftBracket)) activePlayer.Health -= 5f;
	}

	public void FixedUpdate()
	{
		foreach(var entity in entities)
		{
			entity.OnFixedUpdate(Time.fixedDeltaTime);
		}
	}

	public void LateUpdate()
	{
		foreach(var entity in entities)
		{
			entity.OnLateUpdate(Time.deltaTime);
		}

		if (!gamePaused)
			mainCamera.UpdatePositionAndRotation();

		if (!physicsPaused)
		{
			if (!activePlayer.lockOn)
				activePlayer.lockOnTarget = lockOnCollider.GetTargetClosestToCenter(activePlayer);

			// Update lock-on indicator position.
			lockOnIndicator.UpdatePosition(activePlayer.lockOn, activePlayer.lockOnTarget);
		}

		// Pause game if requested.
		if(InputManager.ActiveDevice.MenuWasPressed || Input.GetKeyDown(KeyCode.P) || togglePaused)
		{
			togglePaused = false;
			gamePaused = !gamePaused;
			hud.SetPaused(gamePaused);
			OnPauseGame(gamePaused);
			PhysicsPaused = gamePaused;
		}
	}

	#endregion

	#region PUBLIC_METHODS

	public void InitHitPause(float duration)
	{
		this.OverrideCoroutine(ref hitPauseCoroutine, HitPause(duration));
	}

	public void TogglePaused()
	{
		togglePaused = true;
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
		lockOnCollider.Init(activePlayer.transform);
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
				InputManager.ActiveDevice.Vibrate(0.5f);
			}
			else
			{
				InputManager.ActiveDevice.Vibrate(0);
			}

			yield return null;
		}

		InputManager.ActiveDevice.Vibrate(0);
		PhysicsPaused = false;
	}
	#endregion
}
