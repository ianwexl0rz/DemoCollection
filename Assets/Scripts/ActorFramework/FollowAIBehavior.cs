﻿using UnityEngine;

[CreateAssetMenu(fileName = "Follow", menuName = "Actor/AI Behaviors/Follow")]
public class FollowAIBehavior : AIBehavior
{
	public float startDistance = 3f;
	public float stopDistance = 2f;
	public float startRunDistance = 6f;
	public float stopRunDistance = 4f;

	public override void Tick(Actor actor)
	{
		if(actor.lockOnTarget == null) { return; }

		actor.lockOn = false;

		Vector3 vector = (actor.lockOnTarget.GetLockOnPosition() - actor.GetLockOnPosition()).WithY(0f);

		if(actor.move == Vector3.zero)
		{
			if(vector.magnitude > startDistance)
			{
				actor.move = vector.normalized;
			}
		}
		else if(vector.magnitude > stopDistance)
		{
			if(actor is Character)
			{
				Character player = actor as Character;

				if(!player.motor.Run && vector.magnitude > startRunDistance)
				{
					// Start running
					player.motor.Run = true;
				}
				else if(player.motor.Run && vector.magnitude < stopRunDistance)
				{
					// Stop running
					player.motor.Run = false;
				}
			}

			actor.move = vector.normalized;
		}
		else
		{
			actor.move = Vector3.zero;
		}
	}
}