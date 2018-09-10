using UnityEngine;
using System.Collections.Generic;
using InControl;
using System;
using System.Collections;

public class GameManager : MonoBehaviour
{
	public Player activePlayer;
	public ControlSettings controlSettings = null;
	public ThirdPersonCamera mainCamera = null;
	public GameObject pausePrefab = null;

	[Space]
	[Header("UI")]
	public RectTransform healthBarFill = null;

	[Space]
	[Header("Actor Brains")]
	public PlayerController playerBrain = null;
	public ActorController followerBrain = null;

	private int targetIndex = 0;
	private List<Player> playerCharacters;
	private List<Entity> entities = new List<Entity>();

	public Action<bool> OnPauseGame = delegate (bool value) { };
	private bool gamePaused = false;

	public bool IsPaused { get { return gamePaused; } }

	private static GameManager _instance;
	public static GameManager I
	{
		get { if(!_instance) { _instance = FindObjectOfType<GameManager>(); } return _instance; }
	}

	#region UNITY_METHODS
	private void Awake()
	{
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
		SetActivePlayer(playerCharacters[targetIndex]);

		foreach(Player p in playerCharacters)
		{
			if(p != activePlayer) p.SetController(followerBrain);
		}

		mainCamera.Setup();
	}

	private void FixedUpdate()
	{
		if(!gamePaused)
		{
			entities.ForEach(entity => entity.OnFixedUpdate());
		}

		if(queuePause)
		{
			if(!gamePaused)
			{
				entities.ForEach(entity => entity.CacheRbPosition());
			}

			queuePause = false;
			gamePaused = !gamePaused;
			OnPauseGame(gamePaused);
			pausePrefab.SetActive(gamePaused);
		}
	}

	bool queuePause = false;

	private void Update()
	{
		if(InputManager.ActiveDevice.MenuWasPressed)
		{
			queuePause = true;
		}

		if(gamePaused) { return; }

		// Swap characters
		if(InputManager.ActiveDevice.Action4.WasPressed) { CyclePlayer(); }

		// Hold the right bumper for slow-mo!
		Time.timeScale = InputManager.ActiveDevice.RightBumper.IsPressed ? 0.25f : 1f;

		UpdateHUD();

		//TODO: Move Camera stuff to player controller?
		mainCamera.UpdateRotation(); // Update camera rotation first so player input direction is correct
		entities.ForEach(entity => entity.OnUpdate()); // Update all the things!
		mainCamera.UpdatePosition(); // Update camera position

		if(queuePause & !gamePaused)
		{
			entities.ForEach(entity => entity.CachePosition());
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

	private void UpdateHUD()
	{
		if(Input.GetKeyDown(KeyCode.RightBracket))
		{
			activePlayer.health = Mathf.Min(activePlayer.health + 5f, activePlayer.maxHealth);
		}

		if(Input.GetKeyDown(KeyCode.LeftBracket))
		{
			activePlayer.health = Mathf.Max(activePlayer.health - 5f, 0f);
		}

		healthBarFill.anchorMax = new Vector2(activePlayer.health / activePlayer.maxHealth, healthBarFill.anchorMax.y);
	}

	private void CyclePlayer()
	{
		targetIndex = (targetIndex + 1) % playerCharacters.Count;
		SetActivePlayer(playerCharacters[targetIndex]);
	}

	private void SetActivePlayer(Player newTarget)
	{
		if(activePlayer == newTarget) { return; }

		Player oldPlayer = activePlayer;
		activePlayer = newTarget;

		if(oldPlayer) oldPlayer.SetController(followerBrain); // Set the old active player to use Follower Brain
		activePlayer.SetController(playerBrain); // Set the active player to use Player Brain
		mainCamera.SetTarget(activePlayer); // Set the camera to follow the active player
	}
	#endregion
}
