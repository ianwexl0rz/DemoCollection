using UnityEngine;
using InControl;

[CreateAssetMenu(fileName = "Player Controller", menuName = "Actor/Controllers/Player Controller")]
public class PlayerController : ActorController
{
	private readonly InputBuffer inputBuffer = new InputBuffer();
	private LockOnIndicator indicator;

	protected override void Init(Actor actor)
	{
		var inactivePlayer = GameManager.I.GetFirstInactivePlayer();
		actor.lockOnTarget = inactivePlayer ? inactivePlayer.transform : null;
		indicator = GameManager.LockOnIndicator;
		inputBuffer.Clear();
	}

	protected override void Tick(Actor actor)
	{
		inputBuffer.Update(Time.deltaTime);

		var inputDevice = InputManager.ActiveDevice;

		actor.move = CalculateMove(inputDevice);
		actor.lockOn = inputDevice.LeftTrigger.IsPressed || Input.GetMouseButton(1);

		if(!(actor is Player player)) return;

		//player.aimingMode = gamePad.RightTrigger.IsPressed;
		//player.Recenter = playerInput.RightStickButton.WasPressed;
		
		// Run
		player.Run = inputDevice.RightTrigger.IsPressed || Input.GetKey(KeyCode.LeftShift);

		// Roll
		player.ShouldRoll = inputDevice.Action2.IsPressed || Input.GetKey(KeyCode.LeftControl);

		// Jump
		if(inputDevice.Action1.WasPressed || inputDevice.LeftBumper.WasPressed || Input.GetKeyDown(KeyCode.Space))
			inputBuffer.Add(player.Jump, 0.1f);

		// Attack
		if(inputDevice.Action3.WasPressed || Input.GetMouseButtonDown(0))
			inputBuffer.Add(player.LightAttack, 0.5f);

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

	protected override void LateTick(Actor actor)
	{
		indicator.UpdatePosition(actor.lockOn, actor.lockOnTarget);
	}

	protected override void Clean(Actor actor)
	{
		actor.move = Vector3.zero;
	}

	public Vector3 CalculateMove(InputDevice playerInput)
	{
		var move = new Vector3(playerInput.LeftStickX, 0, playerInput.LeftStickY);
		var deadZone = GameSettings.I.deadZone;

		// Early out if move input is less than the dead zone.
		if(move.magnitude < deadZone) return Vector3.zero;

		// Remap the input so the range is [0-1] accounting for the dead zone.
		move = move.normalized * (Mathf.Clamp01(move.magnitude) - deadZone) / (1f - deadZone);

		// Orient the input relative to the camera.
		return Quaternion.AngleAxis(GameManager.I.mainCamera.transform.eulerAngles.y, Vector3.up) * move;
	}
}
