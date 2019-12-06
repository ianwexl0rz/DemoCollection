using UnityEngine;

[CreateAssetMenu(fileName = "Follow", menuName = "Actor/AI Behaviors/Attack")]
class AttackAIBehavior : AIBehavior
{
	public override void Tick(Actor actor)
	{
		if(actor.lockOnTarget == null) { return; }

		actor.lockOn = true;

		if(actor is Character character)
		{
			character.TryAttack();
		}
	}
}
