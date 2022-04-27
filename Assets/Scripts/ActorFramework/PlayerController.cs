using UnityEngine;
using Rewired;

[CreateAssetMenu(fileName = "Player Controller", menuName = "Actor/Controllers/Player Controller")]
public class PlayerController : ActorController
{
	public static PlayerController instance;
	
	public static Player Player { get; private set; }

	public static void RegisterPlayer(Player player) => Player = player;

	public override void Init(Actor actor, object context = null)
	{
		if (instance == null) instance = this;

		actor.TrackedTarget = null;
		actor.InputBuffer.Clear();
		HealthBar.RegisterPlayer(actor);
	}

	public override void Clean(Actor actor)
	{
		//if (actor is Character character) character.SetLockOnTarget(null);
		HealthBar.UnregisterPlayer(actor);
	}

	public override void Tick(Actor actor, float deltaTime)
	{
		actor.Move = CalculateMove();

		// Lock On
		if(Player.GetButtonDown(PlayerAction.LockOn))
			HandleLockOnInput(actor);

		// Run
		if(actor.GetComponent<CharacterMotor>() is CharacterMotor motor) 
			motor.Run = Player.GetButton(PlayerAction.Sprint);

		// Roll
		if(Player.GetButtonDown(PlayerAction.Roll))
			actor.InputBuffer.Add(PlayerAction.Roll, 0.1f);

		// Jump
		if(Player.GetButtonDown(PlayerAction.Jump))
			actor.InputBuffer.Add(PlayerAction.Jump, 0.1f);

		// Attack
		if(Player.GetButtonDown(PlayerAction.Attack))
			actor.InputBuffer.Add(PlayerAction.Attack, 0.5f);
	}

	private static Vector3 CalculateMove()
	{
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
