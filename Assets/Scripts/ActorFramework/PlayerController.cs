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
	public Player Player;
	public Camera MainCamera;
	public ThirdPersonCamera GameCamera;
}

[CreateAssetMenu(fileName = "Player Controller KCC", menuName = "Actor/Controllers/Player Controller KCC")]
public class PlayerController : ActorController
{
	public static List<Trackable> PotentialTargets { get; private set; } = new List<Trackable>();

	public event Action<Actor> PossessedActor;
	public event Action<Actor> ReleasedActor;
	public event Action ChangedRecentlyHitList;
	public event Action<Trackable> RequestUpdateReticle;

	[field: SerializeField] public float ShowHealthOnHitDuration { get; private set; } = 1f;
	[field: SerializeField] public float LockOnRange { get; private set; } = 10f;
	[field: SerializeField] public float ChangeTargetAngleTolerance { get; private set; } = 90f;
	public Dictionary<Trackable, float> RecentlyHit { get; private set; } = new Dictionary<Trackable, float>();

	private Player _player;
	private Camera _mainCamera;
	private ThirdPersonCamera _gameCamera;
	private ActorKinematicMotor _locomotion;
	private ActorPhysicalMotor _legacyMotor;
	private Trackable _trackableCandidate;
	private bool _lookInputStale;

	public void Init(PlayerControllerContext context)
	{
		_player = context.Player;
		_mainCamera = context.MainCamera;
		_gameCamera = context.GameCamera;
	}
	
	public override void Possess(Actor actor, object context = null)
	{
		_locomotion = actor.GetComponent<ActorKinematicMotor>();
		_legacyMotor = actor.GetComponent<ActorPhysicalMotor>();

		RecentlyHit.Remove(actor.Trackable);

		PossessedActor?.Invoke(actor);
		actor.OnPossessedByPlayer(this);
	}

	public override void Release(Actor actor)
	{
		ReleasedActor?.Invoke(actor);
		actor.OnReleasedByPlayer(this);
		actor.InputBuffer.Clear();
		TrackedTarget = null;

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

	public override void Tick(Actor actor, float deltaTime)
	{
		if(_player.GetButtonDown(PlayerAction.LockOn)) 
			HandleLockOnInput(actor);
		
		if(_player.GetButtonDown(PlayerAction.Attack))
			actor.InputBuffer.Add(PlayerAction.Attack, 0.5f);

		if (_locomotion != null)
		{
			var move = CalculateMove(actor);
			var shouldRun = _player.GetButtonLongPress(PlayerAction.Evade);
			var look = CalculateLook(actor, move, shouldRun);

			var inputs = new CharacterInputs()
			{
				Move = move,
				Look = look,
				Run = shouldRun,
				BeginRoll = _player.GetButtonShortPressUp(PlayerAction.Evade),
				BeginJump = _player.GetButtonDown(PlayerAction.Jump),
				IsInHitStun = actor.HitReaction.InProgress
			};

			_locomotion.SetInputs(ref inputs);
		}
		else if (_legacyMotor != null)
		{
			_legacyMotor.Move = CalculateMove(actor);
			_legacyMotor.Run = _player.GetButtonLongPress(PlayerAction.Evade);
			
			// Roll
			if(_player.GetButtonShortPressUp(PlayerAction.Evade))
				actor.InputBuffer.Add(PlayerAction.Evade, 0.25f);

			// Jump
			if(_player.GetButtonDown(PlayerAction.Jump))
				actor.InputBuffer.Add(PlayerAction.Jump, 0.1f);
		}
	}

	public override void LateTick(Actor actor, float deltaTime)
	{
		var lookInput = _player.GetAxis2D(
			PlayerAction.LookHorizontal * (GameManager.Settings.InvertX ? -1 : 1), 
			PlayerAction.LookVertical);

		_gameCamera.UpdatePositionAndRotation(lookInput, TrackedTarget);

		UpdateRecentlyHitList();
		UpdateLockOn(actor, lookInput, ChangeTargetAngleTolerance);
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

	private Vector3 CalculateLook(Actor actor, Vector3 move, bool shouldRun)
	{
		if (!actor.InputEnabled) return actor.transform.forward;
		
		return TrackedTarget && !shouldRun ? actor.DirectionToTrackable(TrackedTarget) : move;
	}

	private Vector3 CalculateMove(Actor actor)
	{
		if (!actor.InputEnabled) return Vector3.zero;
		
		var move = new Vector3
		{
			x = _player.GetAxis(PlayerAction.MoveHorizontal),
			z = _player.GetAxis(PlayerAction.MoveVertical)
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
		if (TrackedTarget != null)
		{
			// If we were locked on, break lock...
			TrackedTarget = null;
		}
		else
		{
			// If we were not locked on, try to assign target...
			var candidate = _trackableCandidate;
			if (candidate != null)
			{
				TrackedTarget = candidate;
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
	
	public void UpdateLockOn(Actor actor, Vector2 lookInput, float angleTolerance)
	{
		foreach (var trackable in PotentialTargets)
		{
			trackable.RefreshTrackableData(actor, _mainCamera, LockOnRange);
		}
		
		if (TrackedTarget == null)
		{
			_trackableCandidate = GetTrackableClosestToCenter(_mainCamera);
			_lookInputStale = true;
		}
		else
		{
			_trackableCandidate = null;
	        
			if (_lookInputStale && lookInput.Equals(Vector2.zero)) _lookInputStale = false;
			if (!_lookInputStale && lookInput.sqrMagnitude > 0)
			{
				var newTarget = GetTrackableClosestToVector(lookInput, TrackedTarget, angleTolerance);
				if (!ReferenceEquals(newTarget, null))
				{
					TrackedTarget = newTarget;
					_lookInputStale = true;
				}
			}
		}

		RequestUpdateReticle?.Invoke(TrackedTarget);
	}

	public static Trackable GetTrackableClosestToCenter(Camera mainCamera)
	{
		if (PotentialTargets.Count == 0) return null;

		Trackable bestTrackable = null;
		var bestDistance = Mathf.Infinity;

		foreach (var trackable in PotentialTargets)
		{
			if (!trackable.OnScreen) continue;
		    
			var distanceFromCenter = Vector2.Distance(trackable.ScreenPos, mainCamera.pixelRect.size * 0.5f);
			if (distanceFromCenter >= bestDistance) continue;
			
			bestTrackable = trackable;
			bestDistance = distanceFromCenter;
		}

		return bestTrackable;
	}
	
	public static Trackable GetTrackableClosestToVector(Vector2 lookInput, Trackable currentTarget, float angleTolerance)
	{
		Trackable bestTrackable = null;
		var smallestAngle = Mathf.Infinity;

		foreach(var trackable in PotentialTargets)
		{
			if (trackable == currentTarget || !trackable.OnScreen) continue;
			
			var fromCurrentTarget = (Vector2)(trackable.ScreenPos - currentTarget.ScreenPos);
			var angle = Vector2.Angle(lookInput.normalized, fromCurrentTarget.normalized);
			if (angle >= smallestAngle) continue;

			bestTrackable = trackable;
			smallestAngle = angle;
		}
		
		return smallestAngle <= angleTolerance ? bestTrackable : null;
	}
}
