using UnityEngine;
using System.Collections.Generic;
using InControl;
using System;
using UnityStandardAssets.ImageEffects;

public class GameManager : MonoBehaviour
{
	public Player activePlayer;
	public ControlSettings controlSettings;
	public ThirdPersonCamera mainCamera;
	[SerializeField] private GameObject hudPrefab = null;

	[Header("Actor Controllers")]
	public PlayerController playerBrain;
	public ActorController followerBrain;

	[Header("Gameplay")]
	[SerializeField] private GameObject lockOnIndicatorPrefab = null;
	[SerializeField] private GameObject hitSpark = null;
	[SerializeField] private GameObject hitSpark2 = null;

	private GameObject lockOnIndicator = null;
	private HUD hud;
	private int targetIndex;
	private List<Player> playerCharacters;
	private readonly List<Entity> entities = new List<Entity>();

	public Action<bool> PauseAllPhysics = delegate { };
	public Action<bool> OnPauseGame = delegate { };
	private bool gamePaused, physicsPaused;
	
	private float hitPauseTimer;

	private static GameManager _instance;
	public static GameManager I
	{
		get { if(!_instance) { _instance = FindObjectOfType<GameManager>(); } return _instance; }
	}

	public static GameObject LockOnIndicator => I.lockOnIndicator;
	public static float HitPauseTimer
	{
		get => I.hitPauseTimer;
		set => I.hitPauseTimer = value;
	}

	#region UNITY_METHODS
	private void Awake()
	{
		//QualitySettings.maxQueuedFrames = 1;
		Application.targetFrameRate = 60;

		hud = Instantiate(hudPrefab, transform).GetComponent<HUD>();
		lockOnIndicator = Instantiate(lockOnIndicatorPrefab);
		lockOnIndicator.SetActive(false);

		DontDestroyOnLoad(this);

		// Lock cursor by default.
		Cursor.lockState = CursorLockMode.Locked;

		playerCharacters = new List<Player>(FindObjectsOfType<Player>());

		if(activePlayer != null)
		{
			targetIndex = playerCharacters.IndexOf(activePlayer);
		}
	}

	private void Start()
	{
		// TODO: Spawn all characters from proxy meshes
		// 1) can take advantage of object pooling
		// 2) load character status from room data

		SetActivePlayer(playerCharacters[targetIndex], true);

		foreach(Player p in playerCharacters)
		{
			if(p != activePlayer) p.SetController(followerBrain);
		}
	}

	private void FixedUpdate()
	{
		if(!physicsPaused)
		{
			entities.ForEach(entity => entity.OnFixedUpdate());
		}
	}

	private void Update()
	{
		if(physicsPaused) { return; }

		// Swap characters
		if(InputManager.ActiveDevice.Action4.WasPressed || Input.GetKeyDown(KeyCode.Tab)) { CyclePlayer(); }

		// Hold the right bumper for slow-mo!
		Time.timeScale = InputManager.ActiveDevice.RightBumper.IsPressed || Input.GetKey(KeyCode.LeftAlt) ? 0.25f : 1f;

		hud.OnUpdate();

		//TODO: Move Camera stuff to player controller?
		mainCamera.UpdateRotation(); // Update camera rotation first so player input direction is correct
		entities.ForEach(entity => entity.OnUpdate()); // Update all the things!
		mainCamera.UpdatePosition(); // Update camera position
	}

	public void LateUpdate()
	{
		entities.ForEach(entity => entity.OnLateUpdate());

		if(InputManager.ActiveDevice.MenuWasPressed || Input.GetKeyDown(KeyCode.P))
		{
			gamePaused = !gamePaused;
			hud.SetPaused(gamePaused);
			OnPauseGame(gamePaused);
			Camera.main.GetComponent<BlurOptimized>().enabled = gamePaused;
		}

		if(!gamePaused && hitPauseTimer > 0)
		{
			InputManager.ActiveDevice.Vibrate(0.5f);
		}
		else
		{
			InputManager.ActiveDevice.Vibrate(0f);
		}

		if((gamePaused || hitPauseTimer > 0) && !physicsPaused)
		{
			physicsPaused = true;
			PauseAllPhysics(true);
		}

		if((!gamePaused && hitPauseTimer == 0f) && physicsPaused)
		{
			physicsPaused = false;
			PauseAllPhysics(false);
		}

		if(!gamePaused && hitPauseTimer > 0)
		{
			hitPauseTimer -= Time.fixedDeltaTime;
			hitPauseTimer = Mathf.Max(0f, hitPauseTimer);
		}
	}

	#endregion

	#region PUBLIC_METHODS
	public void ClickOnPlayer(Player newTarget)
	{
		// Only switch targets if the mouse is unlocked.
		if(Cursor.lockState != CursorLockMode.Locked)
		{
			SetActivePlayer(newTarget);
			Cursor.lockState = CursorLockMode.Locked;
		}
	}

	public static bool GetHitSpark(Entity entity, out GameObject hitSpark)
	{
		return hitSpark =
		(
			entity is Actor ? I.hitSpark :
			entity is Entity ? I.hitSpark2 : null
		);
	}

	public Player GetFirstInactivePlayer()
	{
		for(int i = 0; i < playerCharacters.Count; i++)
		{
			if(playerCharacters[i] != activePlayer)
			{
				return playerCharacters[i];
			}
		}
		return null;
	}

	public void AddEntity(Entity entity)
	{
		entities.Add(entity);
	}

	public void RemoveEntity(Entity entity)
	{
		entities.Remove(entity);
	}
	#endregion

	#region PRIVATE_METHODS
	private void CyclePlayer()
	{
		targetIndex = (targetIndex + 1) % playerCharacters.Count;
		SetActivePlayer(playerCharacters[targetIndex]);
	}

	private void SetActivePlayer(Player newTarget, bool immediate = false)
	{
		if(activePlayer == newTarget) { return; }

		Player oldPlayer = activePlayer;
		activePlayer = newTarget;

		if(oldPlayer) oldPlayer.SetController(followerBrain); // Set the old active player to use Follower Brain
		activePlayer.SetController(playerBrain); // Set the active player to use Player Brain
		mainCamera.SetTarget(activePlayer, immediate); // Set the camera to follow the active player
	}
	#endregion
}
