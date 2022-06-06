using ActorFramework;
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

		if (actor.GetComponent<ActorKinematicMotor>() is ActorKinematicMotor kinematicMotor)
		{
			var move = Vector3.zero;
			var look = kinematicMotor.transform.forward;
			var shouldRun = kinematicMotor.IsRunning;
			
			if (toTarget.magnitude > startDistance)
			{
				move = toTarget.normalized;
				look = move;

				if (!shouldRun && toTarget.magnitude > startRunDistance)
					shouldRun = true;
				
				if (shouldRun && toTarget.magnitude < stopRunDistance)
					shouldRun = false;
			}

			var inputs = new CharacterInputs()
			{
				Move = move,
				Look = look,
				Run = shouldRun
			};

			kinematicMotor.SetInputs(ref inputs);
		}
		else if (actor.GetComponent<ActorPhysicalMotor>() is ActorPhysicalMotor physicalMotor)
		{
			if (physicalMotor.Move == Vector3.zero)
			{
				if (toTarget.magnitude > startDistance)
				{
					physicalMotor.Move = toTarget.normalized;
				}
			}
			else if (toTarget.magnitude > stopDistance)
			{
				if (!physicalMotor.Run && toTarget.magnitude > startRunDistance)
				{
					// Start running
					physicalMotor.Run = true;
				}
				else if (physicalMotor.Run && toTarget.magnitude < stopRunDistance)
				{
					// Stop running
					physicalMotor.Run = false;
				}

				physicalMotor.Move = toTarget.normalized;
			}
			else
			{
				physicalMotor.Move = Vector3.zero;
			}
		}
	}
}
