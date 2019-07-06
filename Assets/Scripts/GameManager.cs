using UnityEngine;
using System.Collections.Generic;
using InControl;
using System;
using UnityStandardAssets.ImageEffects;
using System.Collections;

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
	[SerializeField] private GameObject lockOnIndicatorPrefab = null;
	[SerializeField] private GameObject hitSpark = null;
	[SerializeField] private GameObject hitSpark2 = null;

	private LockOnIndicator lockOnIndicator = null;
	private int targetIndex;
	private List<Character> playerCharacters;
	private List<Character> potentialTargets;
	private readonly List<Entity> entities = new List<Entity>();

	public Action<bool> PauseAllPhysics = delegate { };
	public Action<bool> OnPauseGame = delegate { };
	private bool gamePaused, physicsPaused, togglePaused;
	
	private float hitPauseTimer;

	private static GameManager _instance;
	public static GameManager I
	{
		get { if(!_instance) { _instance = FindObjectOfType<GameManager>(); } return _instance; }
	}

	public bool PhysicsPaused => physicsPaused;
	public bool GamePaused => gamePaused;

	public static LockOnIndicator LockOnIndicator => I.lockOnIndicator;
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

		lockOnIndicator = Instantiate(lockOnIndicatorPrefab).GetComponent<LockOnIndicator>();
		//lockOnIndicator.gameObject.SetActive(false);

		DontDestroyOnLoad(this);

		// Lock cursor by default.
		//Cursor.lockState = CursorLockMode.Locked;

		playerCharacters = new List<Character>(FindObjectsOfType<Character>());

		if(activePlayer != null)
		{
			targetIndex = playerCharacters.IndexOf(activePlayer);
		}

		StartCoroutine(LateFixedUpdate());
	}

	private void Start()
	{
		// TODO: Spawn all characters from proxy meshes
		// 1) can take advantage of object pooling
		// 2) load character status from room data

		SetActivePlayer(activePlayer, true);

		/*
		SetActivePlayer(playerCharacters[targetIndex], true);

		foreach(Character p in playerCharacters)
		{
			if(p != activePlayer) p.SetController(followerBrain);
		}
		*/
	}

	private int SortByProximityToScreenCenter(Character a, Character b)
	{
		var cam = Camera.main;
		var p1 = cam.WorldToScreenPoint(a.transform.position);
		p1 = new Vector3(p1.x.LinearRemap(0, cam.pixelWidth, -1, 1), p1.y.LinearRemap(0, cam.pixelHeight, -1, 1), 0);

		var p2 = cam.WorldToScreenPoint(b.transform.position);
		p2 = new Vector3(p2.x.LinearRemap(0, cam.pixelWidth, -1, 1), p2.y.LinearRemap(0, cam.pixelHeight, -1, 1), 0);

		return p1.magnitude.CompareTo(p2.magnitude);
	}

	private IEnumerator LateFixedUpdate()
	{
		while(true)
		{
			yield return new WaitForFixedUpdate();

			if(gamePaused) { continue; }

			mainCamera.UpdateRotation(); // Update camera after physics

			// Update the potential lock on target
			if(!activePlayer.lockOn)
			{
				potentialTargets.Sort(SortByProximityToScreenCenter);
				activePlayer.lockOnTarget = potentialTargets[0];
			}
		}
	}

	private void Update()
	{
		//if(physicsPaused) { return; }

		// Swap characters
		//if(InputManager.ActiveDevice.Action4.WasPressed || Input.GetKeyDown(KeyCode.Tab)) { CyclePlayer(); }

		// Hold the right bumper for slow-mo!
		Time.timeScale = InputManager.ActiveDevice.RightBumper.IsPressed || Input.GetKey(KeyCode.LeftAlt) ? 0.25f : 1f;

		hud.OnUpdate();

		//TODO: Move Camera stuff to player controller?

		//entities.ForEach(entity => entity.OnUpdate()); // Update all the things!

		//mainCamera.UpdatePosition(); // Update camera position

	}

	public void LateUpdate()
	{
		if(!physicsPaused)
			lockOnIndicator.UpdatePosition(activePlayer.lockOn, activePlayer.lockOnTarget);

		if(InputManager.ActiveDevice.MenuWasPressed || Input.GetKeyDown(KeyCode.P) || togglePaused)
		{
			togglePaused = false;
			gamePaused = !gamePaused;
			hud.SetPaused(gamePaused);
			OnPauseGame(gamePaused);
			//Camera.main.GetComponent<BlurOptimized>().enabled = gamePaused;
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
			hitPauseTimer -= Time.deltaTime;
			hitPauseTimer = Mathf.Max(0f, hitPauseTimer);
		}
	}

	#endregion

	#region PUBLIC_METHODS

	public void TogglePaused()
	{
		togglePaused = true;
	}

	public void ClickOnPlayer(Character newTarget)
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

	public Character GetFirstInactivePlayer()
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

	private void SetActivePlayer(Character newTarget, bool immediate = false)
	{
		//if(activePlayer == newTarget) { return; }

		//Character oldPlayer = activePlayer;
		activePlayer = newTarget;

		potentialTargets = new List<Character>(playerCharacters);
		potentialTargets.Remove(activePlayer);

		//if(oldPlayer) oldPlayer.SetController(followerBrain); // Set the old active player to use Follower Brain
		activePlayer.SetController(playerBrain); // Set the active player to use Player Brain
		mainCamera.SetTarget(activePlayer, immediate); // Set the camera to follow the active player
	}
	#endregion
}
