using UnityEngine;
using System.Collections.Generic;
using InControl;
using System;
using Unity.Entities;
using UnityStandardAssets.ImageEffects;

public class GameManager : MonoBehaviour
{
	public Player activePlayer;
	public ControlSettings controlSettings;
	public ThirdPersonCamera mainCamera;
	public HUD hud;

	[Header("Actor Controllers")]
	public PlayerController playerBrain;
	public CharacterController followerBrain;

	[Header("Gameplay")]
	[SerializeField]
	private GameObject hitSpark = null;
	[SerializeField]
	private GameObject hitSpark2 = null;

	private int targetIndex;
	private List<Player> playerCharacters;
	private readonly List<Actor> actors = new List<Actor>();

	public Action<bool> PauseAllPhysics = delegate { };
	public Action<bool> OnPauseGame = delegate { };
	private bool gamePaused, physicsPaused;
	
	private float hitPauseTimer;

	private static GameManager _instance;
	public static GameManager I
	{
		get { if(!_instance) { _instance = FindObjectOfType<GameManager>(); } return _instance; }
	}

	public static float HitPauseTimer
	{
		get => I.hitPauseTimer;
		set => I.hitPauseTimer = value;
	}

	#region UNITY_METHODS
	private void Awake()
	{
		QualitySettings.maxQueuedFrames = 1;

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
			actors.ForEach(actor =>
			{
				//if(actor.GetComponent<GameObjectEntity>() == null)
					actor.OnFixedUpdate();
			});
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
		actors.ForEach(entity => entity.OnUpdate()); // Update all the things!
		mainCamera.UpdatePosition(); // Update camera position
	}

	public void LateUpdate()
	{
		actors.ForEach(actors => actors.OnLateUpdate());

		if(InputManager.ActiveDevice.MenuWasPressed || Input.GetKey(KeyCode.P))
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

		if((!gamePaused && Mathf.Approximately(hitPauseTimer, 0f)) && physicsPaused)
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

	public static bool GetHitSpark(Actor actor, out GameObject hitSpark)
	{
		return hitSpark =
		(
			actor is Character ? I.hitSpark :
			actor is Actor ? I.hitSpark2 : null
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

	public void AddEntity(Actor actor)
	{
		actors.Add(actor);
	}

	public void RemoveEntity(Actor actor)
	{
		actors.Remove(actor);
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
