using ActorFramework;
using UnityEngine;

[CreateAssetMenu(fileName = "Follow", menuName = "Actor/AI Behaviors/Follow")]
public class FollowAIBehavior : AIBehavior
{
	public float startDistance = 3f;
	public float stopDistance = 2f;
	public float startRunDistance = 6f;
	public float stopRunDistance = 4f;
	
	private CharacterInputs _inputs = new();

	public override void Tick(ActorController controller, Actor actor)
	{
		if(actor == null || actor.Equals(null) || controller.TrackedTarget == null || controller.TrackedTarget.Equals(null)) { return; }

		var toTarget = (controller.TrackedTarget.GetEyesPosition() - actor.Trackable.GetEyesPosition()).WithY(0f);

		IActorMotor motor = actor.GetComponent<IActorMotor>();
		if (motor != null)
		{
			var targetDistance = toTarget.magnitude;
			var normalizedTarget = toTarget.normalized;

			if (_inputs.Move == Vector3.zero)
			{
				if (targetDistance > startDistance)
				{
					_inputs.Move = normalizedTarget;
					_inputs.Look = normalizedTarget;
				}
			}
			else if (targetDistance > stopDistance)
			{
				_inputs.Move = normalizedTarget;
				_inputs.Look = normalizedTarget;
				_inputs.Run = targetDistance > startRunDistance || (_inputs.Run && targetDistance > stopRunDistance);
			}
			else
			{
				_inputs.Move = Vector3.zero;
			}
			motor.SetInputs(ref _inputs);
		}
	}
}
