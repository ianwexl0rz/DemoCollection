using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ActorFramework;
using DemoCollection;
using DemoCollection.DataBinding;
using UnityEngine;
using Rewired;

public class PlayerControllerContext
{
	public Camera MainCamera;
	public ThirdPersonCamera GameCamera;
}

[CreateAssetMenu(fileName = "Player Controller KCC", menuName = "Actor/Controllers/Player Controller KCC")]
public class PlayerController : ActorController
{
	public static event Action<Actor> PossessedActor;
	public static event Action<Actor> ReleasedActor;
	public static event Action ChangedRecentlyHitList;
	
	[NonSerialized] public static PlayerController Instance;

	[field: SerializeField] public float ShowHealthOnHitDuration { get; private set; } = 1f;
	[field: SerializeField] public LockOn Tracking { get; private set; }

	public Dictionary<Trackable, float> RecentlyHit { get; private set; }


	private Camera _mainCamera;
	private ThirdPersonCamera _gameCamera;
	private ActorKinematicMotor _locomotion;
	private ActorPhysicalMotor _legacyMotor;
	
	public static Player Player { get; private set; }

	public override void Possess(Actor actor, object context = null)
	{
		if (Instance == null)
		{
			Instance = this;
			Tracking.Init();
			RecentlyHit = new Dictionary<Trackable, float>();
		}

		if (context is PlayerControllerContext playerControllerContext)
		{
			_mainCamera = playerControllerContext.MainCamera;
			_gameCamera = playerControllerContext.GameCamera;
		}

		_locomotion = actor.GetComponent<ActorKinematicMotor>();
		_legacyMotor = actor.GetComponent<ActorPhysicalMotor>();
		
		actor.TrackedTarget = null;
		actor.InputBuffer.Clear();
		
		PossessedActor?.Invoke(actor);
		actor.OnPossessedByPlayer(this);
	}

	public static void RegisterPlayer(Player player) => Player = player;

	public override void Release(Actor actor)
	{
		ReleasedActor?.Invoke(actor);
		actor.OnReleasedByPlayer(this);
		actor.TrackedTarget = null;
		FlushInputs();
	}

	public override void Tick(Actor actor, float deltaTime)
	{
		Tracking.RequestRefreshTrackables(actor, _mainCamera);
		UpdateRecentlyHitList();
		
		if(Player.GetButtonDown(PlayerAction.LockOn)) 
			HandleLockOnInput(actor);
		
		if(Player.GetButtonDown(PlayerAction.Attack))
			actor.InputBuffer.Add(PlayerAction.Attack, 0.5f);

		if (_locomotion != null)
		{
			var move = CalculateMove(actor);

			var inputs = new CharacterInputs()
			{
				Move = move,
				Look = CalculateLook(actor, move),
				Run = Player.GetButton(PlayerAction.Sprint),
				BeginJump = Player.GetButtonDown(PlayerAction.Jump),
				BeginRoll = Player.GetButtonDown(PlayerAction.Roll),
				IsInHitStun = actor.HitReaction.InProgress
			};

			_locomotion.SetInputs(ref inputs);
		}
		else if (_legacyMotor != null)
		{
			_legacyMotor.Move = CalculateMove(actor);
			_legacyMotor.Run = Player.GetButton(PlayerAction.Sprint);
			
			// Roll
			if(Player.GetButtonDown(PlayerAction.Roll))
				actor.InputBuffer.Add(PlayerAction.Roll, 0.1f);

			// Jump
			if(Player.GetButtonDown(PlayerAction.Jump))
				actor.InputBuffer.Add(PlayerAction.Jump, 0.1f);
		}
	}

	public override void LateTick(Actor actor, float deltaTime)
	{
		var lookInput = Player.GetAxis2D(
			PlayerAction.LookHorizontal * (GameManager.Settings.InvertX ? -1 : 1), 
			PlayerAction.LookVertical);

		_gameCamera.UpdatePositionAndRotation(lookInput, actor.TrackedTarget);

		LockOn.UpdateLockOn(_mainCamera, lookInput);
	}

	public void AddTargetToRecentlyHitList(CombatEvent combatEvent)
	{
		var trackable = combatEvent.Target.GetComponent<Trackable>();
		if (!trackable || !trackable.Health) return;

		if (RecentlyHit.TryAdd(trackable, Time.time))
		{
			ChangedRecentlyHitList?.Invoke();
		}
		else
		{
			RecentlyHit[trackable] = Time.time;
		}
	}

	private void UpdateRecentlyHitList()
	{
		var toRemove = new List<Trackable>();
		foreach(var pair in RecentlyHit)
		{
			if (Time.time >= pair.Value + ShowHealthOnHitDuration)
				toRemove.Add(pair.Key);
		}
		
		foreach (var trackable in toRemove) RecentlyHit.Remove(trackable);
		if (toRemove.Count > 0) ChangedRecentlyHitList?.Invoke();
	}

	private void FlushInputs()
	{
		if (_locomotion != null)
		{
			var inputs = new CharacterInputs();
			_locomotion.SetInputs(ref inputs);
		}
		else if (_legacyMotor != null)
		{
			_legacyMotor.Move = Vector3.zero;
			_legacyMotor.Run = false;
		}
	}

	private static Vector3 CalculateLook(Actor actor, Vector3 move)
	{
		if (!actor.InputEnabled) return actor.transform.forward;
		
		return actor.TrackedTarget == null ? move : actor.GetTrackedTargetDirection();
	}

	private static Vector3 CalculateMove(Actor actor)
	{
		if (!actor.InputEnabled) return Vector3.zero;
		
		var move = new Vector3
		{
			x = Player.GetAxis(PlayerAction.MoveHorizontal),
			z = Player.GetAxis(PlayerAction.MoveVertical)
		};
		
		var deadZone = GameManager.Settings.deadZone;

		// Early out if move input is less than the dead zone.
		if(move.magnitude < deadZone) return Vector3.zero;

		// Remap the input so the range is [0-1] accounting for the dead zone.
		move = move.normalized * Mathf.InverseLerp(deadZone, 1, move.magnitude);

		// Orient the input relative to the camera.
		return GameManager.Camera.YawRotation * move;
	}

	private void HandleLockOnInput(Actor actor)
	{
		if (actor.TrackedTarget != null)
		{
			// If we were locked on, break lock...
			actor.TrackedTarget = null;
		}
		else
		{
			// If we were not locked on, try to assign target...
			var candidate = LockOn.TrackableCandidate;
			if (candidate != null)
			{
				actor.TrackedTarget = candidate;
			}
			else
			{
				// Recenter the camera if there is no viable target.
				var cross = Vector3.Cross(actor.transform.right, Vector3.up);
				var lookRotation = Quaternion.LookRotation(cross);
				GameManager.Camera.SetTargetEulerAngles(lookRotation.eulerAngles);
			}
		}
	}
}
