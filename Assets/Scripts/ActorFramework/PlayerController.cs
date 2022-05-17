using ActorFramework;
using DemoCollection;
using UnityEngine;
using Rewired;

[CreateAssetMenu(fileName = "Player Controller KCC", menuName = "Actor/Controllers/Player Controller KCC")]
public class PlayerController : ActorController
{
	public static PlayerController Instance;
	
	public static Player Player { get; private set; }

	public static void RegisterPlayer(Player player) => Player = player;

	private CharacterLocomotion _locomotion;
	private CharacterMotor _legacyMotor;

	public override void Init(Actor actor, object context = null)
	{
		if (Instance == null) Instance = this;

		_locomotion = actor.GetComponent<CharacterLocomotion>();
		_legacyMotor = actor.GetComponent<CharacterMotor>();
		
		actor.TrackedTarget = null;
		actor.InputBuffer.Clear();
		HudViewModel.RegisterActor(actor);
	}

	public override void Clean(Actor actor)
	{
		HudViewModel.UnregisterActor(actor);
		actor.TrackedTarget = null;
		FlushInputs();
	}

	public override void Tick(Actor actor, float deltaTime)
	{
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
			if (candidate != null) actor.TrackedTarget = candidate;
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
