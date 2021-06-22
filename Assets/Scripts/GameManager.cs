using UnityEngine;
using System.Collections.Generic;
using Rewired;

public class GameManager : MonoBehaviour
{
	[SerializeField] private GameSettings gameSettings = null;

	[Header("Game Modes")]
	[SerializeField] private MainMode mainMode = new MainMode();
	[SerializeField] private PauseMode pauseMode = new PauseMode();

	private static GameManager instance;

	public static GameManager I => instance ? instance : instance = FindObjectOfType<GameManager>();
	
    public static GameSettings Settings => I.gameSettings;
    
    public static ThirdPersonCamera Camera => I.mainMode.MainCamera;

	#region UNITY_METHODS
	
	private void Awake()
	{
		instance = this;

		Application.targetFrameRate = -1;
		
		// Cache reference to player.
		var player = ReInput.players.GetPlayer(0);
		PlayerController.RegisterPlayer(player);
		GameMode.SetPlayer(player);
		GameMode.RegisterModes(new List<GameMode> { mainMode, pauseMode });

		DontDestroyOnLoad(this);
	}

	private void Start()
	{
		// TODO: Spawn all characters from proxy meshes
		// 1) can take advantage of object pooling
		// 2) load character status from room data

		GameMode.SetMode<MainMode>();
		Cursor.lockState = CursorLockMode.Locked;
	}

	private void Update() => GameMode.Current?.Tick(Time.deltaTime);

	private void FixedUpdate() => GameMode.Current?.FixedTick(Time.fixedDeltaTime);

	private void LateUpdate() => GameMode.Current?.LateTick(Time.deltaTime);
	
	#endregion

	#region PUBLIC_METHODS
	#endregion

	#region PRIVATE_METHODS
	#endregion
}
