using UnityEngine;

[CreateAssetMenu(fileName = "Follow", menuName = "Actor/AI Behaviors/Follow")]
public class FollowAIBehavior : AIBehavior
{
	public float startDistance = 3f;
	public float stopDistance = 2f;
	public float startRunDistance = 6f;
	public float stopRunDistance = 4f;

	public override void Tick(Actor actor)
	{
		if(actor == null || actor.Equals(null) || actor.TrackedTarget == null || actor.TrackedTarget.Equals(null)) { return; }

		var toTarget = (actor.TrackedTarget.GetEyesPosition() - actor.GetEyesPosition()).WithY(0f);

		if(actor.Move == Vector3.zero)
		{
			if(toTarget.magnitude > startDistance)
			{
				actor.Move = toTarget.normalized;
			}
		}
		else if(toTarget.magnitude > stopDistance)
		{
			if(actor is CharacterMotor player)
			{
				if(!player.Run && toTarget.magnitude > startRunDistance)
				{
					// Start running
					player.Run = true;
				}
				else if(player.Run && toTarget.magnitude < stopRunDistance)
				{
					// Stop running
					player.Run = false;
				}
			}

			actor.Move = toTarget.normalized;
		}
		else
		{
			actor.Move = Vector3.zero;
		}
	}
}
