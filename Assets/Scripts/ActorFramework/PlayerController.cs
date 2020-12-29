using UnityEngine;
using Rewired;

[CreateAssetMenu(fileName = "Player Controller", menuName = "Actor/Controllers/Player Controller")]
public class PlayerController : ActorController
{
	public static PlayerController instance;
	
	public static Player Player { get; private set; }

	public static void RegisterPlayer(Player player) => Player = player;

	protected override void Init(Actor actor, object context = null)
	{
		if (instance == null) instance = this;
		
		base.Init(actor, context);
		inputBuffer.Clear();
	}

	public override void Tick()
	{
		base.Tick();
		
		actor.move = CalculateMove();

		if(!(actor is Character character)) return;
		
		// Lock On
		if(Player.GetButtonDown(PlayerAction.LockOn))
			character.TryLockOn();

		// Run
		character.Run = Player.GetButton(PlayerAction.Sprint);

		// Roll
		if(Player.GetButtonDown(PlayerAction.Roll))
			inputBuffer.Add(PlayerAction.Roll, 0.1f);

		// Jump
		if(Player.GetButtonDown(PlayerAction.Jump))
			inputBuffer.Add(PlayerAction.Jump, 0.1f);

		// Attack
		if(Player.GetButtonDown(PlayerAction.Attack))
			inputBuffer.Add(PlayerAction.Attack, 0.5f);
	}

	protected override void Clean() => actor.move = Vector3.zero;

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
}
