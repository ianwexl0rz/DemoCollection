using UnityEngine;

[CreateAssetMenu(fileName = "Follow", menuName = "Actor/AI Behaviors/Follow")]
public class FollowAI : AIBehavior
{
	public float startDistance = 3f;
	public float stopDistance = 2f;
	public float startRunDistance = 6f;
	public float stopRunDistance = 4f;

	public override void Tick(Character character)
	{
		if(character.lockOnTarget == null) { return; }

		Vector3 vector = character.lockOnTarget.position - character.transform.position;

		vector = Vector3.Scale(vector, new Vector3(1, 0, 1));

		if(character.move == Vector3.zero)
		{
			if(vector.magnitude > startDistance)
			{
				character.move = vector.normalized;
			}
		}
		else if(vector.magnitude > stopDistance)
		{
			if(character is Player)
			{
				Player player = character as Player;

				if(!player.Run && vector.magnitude > startRunDistance)
				{
					// Start running
					player.Run = true;
				}
				else if(player.Run && vector.magnitude < stopRunDistance)
				{
					// Stop running
					player.Run = false;
				}
			}

			character.move = vector.normalized;
		}
		else
		{
			character.move = Vector3.zero;
		}
	}
}
