using UnityEngine;

[CreateAssetMenu(fileName = "Follow", menuName = "Actor/AI Behaviors/Attack")]
class AttackAIBehavior : AIBehavior
{
	public override void Tick(Actor actor)
	{
		if(actor.lockOnTarget != null)
			actor.InputBuffer.Add(PlayerAction.Attack, 0.02f);
	}
}
