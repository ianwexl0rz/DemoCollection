using UnityEngine;

[CreateAssetMenu(fileName = "Follow", menuName = "Actor/AI Behaviors/Follow")]
public class FollowAIBehavior : AIBehavior
{
	public float startDistance = 3f;
	public float stopDistance = 2f;
	public float startRunDistance = 6f;
	public float stopRunDistance = 4f;

	public override void Tick(ActorController controller, Actor actor)
	{
		if(actor == null || actor.Equals(null) || controller.TrackedTarget == null || controller.TrackedTarget.Equals(null)) { return; }

		var toTarget = (controller.TrackedTarget.GetEyesPosition() - actor.Trackable.GetEyesPosition()).WithY(0f);

		if(!(actor.GetComponent<ActorPhysicalMotor>() is ActorPhysicalMotor motor)) return;

		
		if(motor.Move == Vector3.zero)
		{
			if(toTarget.magnitude > startDistance)
			{
				motor.Move = toTarget.normalized;
			}
		}
		else if(toTarget.magnitude > stopDistance)
		{
			if(!motor.Run && toTarget.magnitude > startRunDistance)
			{
				// Start running
				motor.Run = true;
			}
			else if(motor.Run && toTarget.magnitude < stopRunDistance)
			{
				// Stop running
				motor.Run = false;
			}

			motor.Move = toTarget.normalized;
		}
		else
		{
			motor.Move = Vector3.zero;
		}
	}
}
