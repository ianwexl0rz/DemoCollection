using System;
using System.Collections.Generic;
using ActorFramework;
using DemoCollection;
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
	public static List<Trackable> PotentialTargets { get; } = new();

	public event Action<Actor> OnPossessActor;
	public event Action<Actor> OnReleaseActor;
	public event Action ChangedRecentlyHitList;
	public event Action<Trackable> RequestUpdateReticle;

	[field: SerializeField] public float ShowHealthOnHitDuration { get; private set; } = 1f;
	[field: SerializeField] public float LockOnRange { get; private set; } = 10f;
	[field: SerializeField] public float ChangeTargetAngleLimit { get; private set; } = 90f;
	public Dictionary<Trackable, float> RecentlyHit { get; } = new();
	
	private Player _player;
	private Camera _mainCamera;
	private ThirdPersonCamera _gameCamera;
	private Trackable _trackableCandidate;
	private Vector3 _facingDirection;
	private Vector2 _lookInput;
	private CharacterInputs _inputs = new();
	
	// Cached actor components
	private IActorMotor _motor;
	private MeleeWeaponUser _meleeWeaponUser;

	public void Init(PlayerControllerContext context)
	{
		_player = context.Player;
		_mainCamera = context.MainCamera;
		_gameCamera = context.GameCamera;
	}
	
	public override void Possess(Actor actor, object context = null)
	{
		_motor = actor.GetComponent<IActorMotor>();
		_meleeWeaponUser = actor.GetComponent<MeleeWeaponUser>();

		if (_meleeWeaponUser) _meleeWeaponUser.RegisterPlayerCallbacks(this);
		if (actor.Trackable) RecentlyHit.Remove(actor.Trackable);
		OnPossessActor?.Invoke(actor);
	}

	public override void Release(Actor actor)
	{
		actor.InputBuffer.Clear();
		TrackedTarget = null;
		_facingDirection = Vector3.zero;
		OnReleaseActor?.Invoke(actor);

		if (_meleeWeaponUser) _meleeWeaponUser.UnregisterPlayerCallbacks(this);
		_motor?.SetInputs(ref _inputs);
	}

	public override void Tick(Actor actor, float deltaTime)
	{
		var isDead = !actor.IsAlive();
		if (isDead || (TrackedTarget && TrackedTarget.Owner?.Health.Current <= 0))
		{
			TrackedTarget = null;
		}
		
		if (isDead) return;

		if(_player.GetButtonDown(PlayerAction.LockOn)) 
			HandleLockOnInput(actor);
		
		if(_player.GetButtonDown(PlayerAction.Attack))
			actor.InputBuffer.Add(PlayerAction.Attack, 0.5f);

		var isRunning = _player.GetButtonLongPress(PlayerAction.Evade);
		CalculateMoveAndOrientation(actor, isRunning, out var move);
		
		if (_motor != null)
		{
			_inputs.Move = move;
			_inputs.Look = _facingDirection;
			_inputs.Run = isRunning;
			_inputs.BeginRoll = _player.GetButtonShortPressUp(PlayerAction.Evade);
			_inputs.BeginJump = _player.GetButtonDown(PlayerAction.Jump);

			_motor.SetInputs(ref _inputs);
		}
	}

	public override void LateTick(Actor actor, float deltaTime)
	{
		var wasNeutral = _lookInput == Vector2.zero;
		
		_lookInput = _player.GetAxis2D(
			PlayerAction.LookHorizontal, 
			PlayerAction.LookVertical);
		
		_gameCamera.UpdatePositionAndRotation(_lookInput, TrackedTarget);
		UpdateRecentlyHitList();

		if (!actor.IsAlive()) return;
		UpdatePotentialTargets(actor);
		UpdateTrackingReticle(wasNeutral && _lookInput != Vector2.zero);

		UIController.Instance.UpdateHud();
	}

	private void UpdateTrackingReticle(bool freshInput)
	{
		_trackableCandidate = TrackedTarget ? null : GetTrackableClosestToCenter(_mainCamera);

		if (TrackedTarget && freshInput &&
		    GetTrackableClosestToVector(_lookInput, TrackedTarget, ChangeTargetAngleLimit) is Trackable newTarget &&
		    !ReferenceEquals(newTarget, null))
		{
			TrackedTarget = newTarget;
		}

		RequestUpdateReticle?.Invoke(TrackedTarget);
	}

	public void AddTargetToRecentlyHitList(CombatEvent combatEvent)
	{
		var trackable = combatEvent.Target.GetComponent<Trackable>();
		if (!trackable || trackable.Owner == null) return;

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

	public void SetOrientationOnAttack(Actor actor)
	{
		if (TrackedTarget)
		{
			_facingDirection = actor.DirectionToTrackable(TrackedTarget);
		}
		else
		{
			var move = CalculateMove();
			if (move != Vector3.zero) _facingDirection = move.normalized;
		}
	}

	private void CalculateMoveAndOrientation(Actor actor, bool isRunning, out Vector3 move)
	{
		if (actor.InputEnabled)
		{
			move = CalculateMove();

			if (TrackedTarget && !isRunning)
			{
				_facingDirection = actor.DirectionToTrackable(TrackedTarget);
			}
			else if (move != Vector3.zero)
			{
				_facingDirection = move.normalized;
			}
		}
		else move = Vector3.zero;
	}
	
	private Vector3 CalculateMove() => GameManager.Camera.YawRotation * new Vector3
	{
		x = _player.GetAxis(PlayerAction.MoveHorizontal),
		z = _player.GetAxis(PlayerAction.MoveVertical)
	};

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

	private void UpdatePotentialTargets(Actor actor)
	{
		foreach (var trackable in PotentialTargets)
		{
			var isValid = actor.transform != trackable.transform &&
			              Vector3.Distance(actor.transform.position, trackable.transform.position) < LockOnRange;

			if (!isValid && TrackedTarget == trackable) TrackedTarget = null;

			trackable.SetTrackableData(isValid, _mainCamera);
		}
	}

	public static Trackable GetTrackableClosestToCenter(Camera mainCamera)
	{
		if (PotentialTargets.Count == 0) return null;

		Trackable bestTrackable = null;
		var bestDistance = Mathf.Infinity;

		foreach (var trackable in PotentialTargets)
		{
			if (!trackable.OnScreen || trackable.Owner?.Health.Current <= 0)
				continue;
		    
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

		foreach (var trackable in PotentialTargets)
		{
			if (trackable == currentTarget || !trackable.OnScreen || trackable.Owner?.Health.Current <= 0)
				continue;
			
			var fromCurrentTarget = (Vector2)(trackable.ScreenPos - currentTarget.ScreenPos);
			var angle = Vector2.Angle(lookInput.normalized, fromCurrentTarget.normalized);
			if (angle >= smallestAngle) continue;

			bestTrackable = trackable;
			smallestAngle = angle;
		}
		
		return smallestAngle <= angleTolerance ? bestTrackable : null;
	}
}
