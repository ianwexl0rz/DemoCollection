using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AI Controller", menuName = "Actor/Controllers/AI Controller")]
public class AIController : ActorController
{
	public List<AIBehavior> behaviors = new List<AIBehavior>();

	protected override void Init(Actor actor)
	{
		if(GameManager.I.activePlayer != null)
		{
			actor.lockOnTarget = GameManager.I.activePlayer.transform;
		}
	}

	protected override void Tick(Actor actor)
	{
		behaviors.ForEach(module => module.Tick(actor));
	}
}
