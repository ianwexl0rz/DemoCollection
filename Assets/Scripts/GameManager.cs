using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;
using Rewired;

public partial class GameManager : MonoBehaviour
{
	[SerializeField] private GameSettings gameSettings;
	[SerializeField] private GameModeType gameMode = GameModeType.Main;

	[Header("Game Modes")]
	[SerializeField] private MainMode mainMode = new MainMode();
	[SerializeField] private PauseMode pauseMode = new PauseMode();

	private static GameManager instance;
	private GameMode currentMode = null;

	private static GameManager I
	{
		get { if(!instance) { instance = FindObjectOfType<GameManager>(); } return instance; }
	}

	public static ILockOnTarget LockOnCandidate => I.mainMode.lockOnCandidate;
	
    public static GameSettings Settings => I.gameSettings;
    public static ThirdPersonCamera Camera => I.mainMode.MainCamera;
    public static bool PhysicsPaused => I.mainMode.PhysicsPaused;
    public static Character GetPlayerCharacter() => I.mainMode.ActivePlayer;
    public static Player player;

	#region UNITY_METHODS
	
	private void Awake()
	{
		instance = this;

		Application.targetFrameRate = 60;
		
		// Cache reference to player.
		player = ReInput.players.GetPlayer(0);

		DontDestroyOnLoad(this);
	}

	private void Start()
	{
		// TODO: Spawn all characters from proxy meshes
		// 1) can take advantage of object pooling
		// 2) load character status from room data

		SetMode(gameMode, null, null, true);
		Cursor.lockState = CursorLockMode.Locked;
	}

	private void Update() => currentMode.Tick(Time.deltaTime);

	public void FixedUpdate() => currentMode.FixedTick(Time.fixedDeltaTime);

	public void LateUpdate() => currentMode.LateTick(Time.deltaTime);
	
	#endregion

	#region PUBLIC_METHODS
	
	public static void RegisterOnPauseGame(Action<bool> action) => I.mainMode.OnPauseGame += action;
	public static void UnregisterOnPauseGame(Action<bool> action) => I.mainMode.OnPauseGame -= action;
	
	#endregion

	#region PRIVATE_METHODS

	private static void SetMode(GameModeType value, GameMode.Context context = null, Action callback = null, bool immediate = false)
	{
		GameMode mode;
		switch (value)
		{
			case GameModeType.Main:
				mode = I.mainMode;
				break;
			case GameModeType.Pause:
				mode = I.pauseMode;
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(value), value, $"GameModeType {nameof(value)} has no associated GameMode.");
		}

		void SetMode()
		{
			I.SetMode(mode, context, callback);
			I.gameMode = value;
		}
		
		if (immediate) SetMode();
		else I.WaitForEndOfFrameThen(SetMode);
	}

	private void SetMode(GameMode value, object context = null, Action callback = null)
	{
		if (currentMode == value) return;

		currentMode?.Clean();

		// Initialize the new mode.
		currentMode = value;
		currentMode.Init(context, callback);
	}
	
	#endregion
}
