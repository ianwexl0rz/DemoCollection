using UnityEngine;
using System.Collections.Generic;
using InControl;

public class GameManager : MonoBehaviour
{
	public Player activePlayer;
	public ControlSettings controlSettings = null;
	public ThirdPersonCamera mainCamera = null;

	[Space]
	[Header("Actor Brains")]
	public PlayerBrain playerBrain = null;
	public ActorBrain followerBrain = null;

	private int targetIndex = 0;
	private List<Player> playerCharacters;
	private List<Actor> actors = new List<Actor>();

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

		SetActivePlayer(playerCharacters[targetIndex]);

		foreach(Player p in playerCharacters)
		{
			if(p != activePlayer) p.SetBrain(followerBrain);
		}
	}

	private void Update()
	{
		// Swap characters
		if(InputManager.ActiveDevice.Action4.WasPressed)
		{
			I.CyclePlayer();
		}

		mainCamera.UpdateRotation(); // Update camera rotation first so player input direction is correct
		UpdateActors();
		mainCamera.UpdatePosition(); // Update camera position
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

	public void AddActor(Actor actor)
	{
		actors.Add(actor);
	}

	public void RemoveActor(Actor actor)
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

	private void UpdateActors()
	{
		foreach(Actor actor in actors)
		{
			actor.UpdateActor();
		}
	}

	private void SetActivePlayer(Player newTarget)
	{
		if(activePlayer == newTarget) { return; }

		Player oldPlayer = activePlayer;
		activePlayer = newTarget;

		if(oldPlayer) oldPlayer.SetBrain(followerBrain); // Set the old active player to use Follower Brain
		activePlayer.SetBrain(playerBrain); // Set the active player to use Player Brain
		mainCamera.SetTarget(activePlayer); // Set the camera to follow the active player
	}
	#endregion
}
