using UnityEngine;
using Rewired;

[CreateAssetMenu(fileName = "Player Controller", menuName = "Actor/Controllers/Player Controller")]
public class PlayerController : ActorController
{
	private readonly InputBuffer inputBuffer = new InputBuffer();
	private Player player;

	protected override void Init(Actor actor, object context = null)
	{
		player = GameManager.player;
		inputBuffer.Clear();
	}

	protected override void Tick(Actor actor)
	{
		inputBuffer.Update(Time.deltaTime);
		
		actor.move = CalculateMove(actor);

		if(!(actor is Character character)) return;
		
		// Lock On
		if(player.GetButtonDown(PlayerAction.LockOn))
			character.TryLockOn();

		// Run
		character.Run = player.GetButton(PlayerAction.Sprint);

		// Roll
		if(player.GetButtonDown(PlayerAction.Roll))
			inputBuffer.Add(character.TryRoll, 0.1f);

		// Jump
		if(player.GetButtonDown(PlayerAction.Jump))
			inputBuffer.Add(character.TryJump, 0.1f);

		// Attack
		if(player.GetButtonDown(PlayerAction.Attack))
			inputBuffer.Add(character.TryAttack, 0.5f);
	}

	protected override void Clean(Actor actor)
	{
		actor.move = Vector3.zero;
	}

	private Vector3 CalculateMove(Actor actor)
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
		
		if(actor is Character character && character.IsLockedOn)
			return Quaternion.Slerp(GameManager.Camera.referenceRotation, character.lockOnOrientation, 0.5f) * move;
		
		// Orient the input relative to the camera.
		return GameManager.Camera.referenceRotation * move;
	}
}
