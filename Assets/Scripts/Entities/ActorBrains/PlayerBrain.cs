using UnityEngine;
using System.Collections;
using InControl;

[CreateAssetMenu(fileName = "Player Brain", menuName = "Actor/Brain/Player Brain")]
public class PlayerBrain : ActorBrain
{
	public override void Init(Actor actor)
	{
		Player inactivePlayer = GameManager.I.GetFirstInactivePlayer();
		actor.lockOnTarget = inactivePlayer ? inactivePlayer.transform : null;
	}

	public override void Process(Actor actor)
	{
		InputDevice playerInput = InputManager.ActiveDevice;

		actor.move = CalculateMove(playerInput);
		actor.lockOn = playerInput.LeftTrigger.IsPressed;

		if(actor is Player)
		{
			Player player = actor as Player;

			//player.aimingMode = gamePad.RightTrigger.IsPressed;
			player.recenter = playerInput.RightStickButton.WasPressed;
			player.run = playerInput.RightTrigger.IsPressed || playerInput.Action2.IsPressed;
			player.jump = playerInput.Action1.WasPressed || playerInput.LeftBumper.WasPressed;
			player.attack = playerInput.Action3.WasPressed || Input.GetKeyDown(KeyCode.M);
		}

		/*
		// Update ability input
		for(int i = 0; i < actor.abilities.Count; i++)
		{
			if(actor.abilities[i] is WalkAbility)
			{
				((WalkAbility)actor.abilities[i]).run = Input.GetKey(KeyCode.LeftShift) || Input.GetButton("Fire1");
				((WalkAbility)actor.abilities[i]).jump = Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Fire2");
			}
		}
		*/
	}

	public override void Clean(Actor actor)
	{
		actor.move = Vector3.zero;
	}

	public Vector3 CalculateMove(InputDevice playerInput)
	{
		Vector3 _move = new Vector3(playerInput.LeftStickX, 0, playerInput.LeftStickY);

		float deadzone = ControlSettings.I.deadZone;

		// Check that the move input is greater than the deadzone.
		if(_move.magnitude >= deadzone)
		{
			// Remap the input so the range is [0-1] accounting for the deadzone.
			_move = _move.normalized * (Mathf.Clamp01(_move.magnitude) - deadzone) / (1f - deadzone);

			// Orient the input relative to the camera.
			return Quaternion.AngleAxis(GameManager.I.mainCamera.transform.eulerAngles.y, Vector3.up) * _move;
		}
		else
		{
			return Vector3.zero;
		}
	}
}
