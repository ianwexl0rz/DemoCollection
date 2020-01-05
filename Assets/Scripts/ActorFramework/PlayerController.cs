using UnityEngine;
using Rewired;

[CreateAssetMenu(fileName = "Player Controller", menuName = "Actor/Controllers/Player Controller")]
public class PlayerController : ActorController
{
	private Player player;

	protected override void Init(Actor actor, object context = null)
	{
		player = GameManager.player;
		inputBuffer.Clear();
	}

	protected override void Tick(Actor actor)
	{
		base.Tick(actor);
		
		actor.move = CalculateMove();

		if(!(actor is Character character)) return;
		
		// Lock On
		if(player.GetButtonDown(PlayerAction.LockOn))
			character.TryLockOn();

		// Run
		character.Run = player.GetButton(PlayerAction.Sprint);

		// Roll
		if(player.GetButtonDown(PlayerAction.Roll))
			inputBuffer.Add(PlayerAction.Roll, 0.1f);

		// Jump
		if(player.GetButtonDown(PlayerAction.Jump))
			inputBuffer.Add(PlayerAction.Jump, 0.1f);

		// Attack
		if(player.GetButtonDown(PlayerAction.Attack))
			inputBuffer.Add(PlayerAction.Attack, 0.5f);
	}

	protected override void Clean(Actor actor)
	{
		actor.move = Vector3.zero;
	}

	private Vector3 CalculateMove()
	{
		var move = new Vector3
		{
			x = player.GetAxis(PlayerAction.MoveHorizontal),
			z = player.GetAxis(PlayerAction.MoveVertical)
		};
		
		var deadZone = GameManager.Settings.deadZone;

		// Early out if move input is less than the dead zone.
		if(move.magnitude < deadZone) return Vector3.zero;

		// Remap the input so the range is [0-1] accounting for the dead zone.
		move = move.normalized * Mathf.InverseLerp(deadZone, 1, move.magnitude);

		// Orient the input relative to the camera.
		return GameManager.Camera.YawRotation * move;
	}
}
