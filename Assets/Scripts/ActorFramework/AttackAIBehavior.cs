using UnityEngine;

[CreateAssetMenu(fileName = "Follow", menuName = "Actor/AI Behaviors/Attack")]
class AttackAIBehavior : AIBehavior
{
	public override void Tick(ActorController controller, Actor actor)
	{
		if (controller.TrackedTarget)
			actor.InputBuffer.Add(PlayerAction.Attack, 0.02f);
	}
}
