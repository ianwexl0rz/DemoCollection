using UnityEngine;
using System.Collections.Generic;
using DemoCollection;
using Rewired;

public class GameManager : MonoBehaviour
{
	[SerializeField] private Actor playerActor = null;
	[SerializeField] private GameSettings gameSettings = null;
	[SerializeField] private UIController uiController = null;

	[Header("Game Modes")]
	[SerializeField] private MainMode mainMode = new MainMode();
	[SerializeField] private PauseMode pauseMode = new PauseMode();

	private static GameManager _instance;
	public static GameManager Instance => _instance;
	
    public static GameSettings Settings => _instance.gameSettings;
    
    public static ThirdPersonCamera Camera => _instance.mainMode.MainCamera;

	#region UNITY_METHODS
	
	private void Awake()
	{
		_instance = this;

		Application.targetFrameRate = -1;
		QualitySettings.vSyncCount = 0;
		
		// Cache reference to player.
		var player = ReInput.players.GetPlayer(0);
		PlayerController.RegisterPlayer(player);
		GameMode.SetPlayer(player);
		GameMode.RegisterModes(new List<GameMode> { mainMode, pauseMode });
		
		mainMode.Init();
		uiController.Init();

		DontDestroyOnLoad(this);
	}

	private void Start()
	{
		// TODO: Spawn all characters from proxy meshes
		// 1) can take advantage of object pooling
		// 2) load character status from room data

		GameMode.SetMode<MainMode>(playerActor);
		//Cursor.lockState = CursorLockMode.Locked;
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
