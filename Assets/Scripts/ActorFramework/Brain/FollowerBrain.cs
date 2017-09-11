using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "Follower Brain", menuName = "Actor/Brain/Follower Brain")]
public class FollowerBrain : ActorBrain
{
	public float startDistance = 3f;
	public float stopDistance = 2f;
	public float startRunDistance = 6f;
	public float stopRunDistance = 4f;

	//public Transform leader = null;

	public override void Init(Actor actor)
	{
		if(GameManager.I.activePlayer != null)
		{
			actor.lockOnTarget = GameManager.I.activePlayer.transform;
		}
	}

	public override void Process(Actor actor)
	{
		if(actor.lockOnTarget == null) { return; }

		Vector3 vector = actor.lockOnTarget.position - actor.transform.position;

		vector = Vector3.Scale(vector, new Vector3(1, 0, 1));

		if(actor.move == Vector3.zero)
		{
			if(vector.magnitude > startDistance)
			{
				actor.move = vector.normalized;
			}
		}
		else if(vector.magnitude > stopDistance)
		{
			if(actor is Player)
			{
				Player player = actor as Player;

				if(!player.run && vector.magnitude > startRunDistance)
				{
					// Start running
					player.run = true;
				}
				else if(player.run && vector.magnitude < stopRunDistance)
				{
					// Stop running
					player.run = false;
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
