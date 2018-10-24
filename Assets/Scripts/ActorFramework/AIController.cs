using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AI Controller", menuName = "Actor/Controllers/AI Controller")]
public class AIController : CharacterController
{
	public List<AIBehavior> behaviors = new List<AIBehavior>();

	protected override void Init(Character character)
	{
		if(GameManager.I.activePlayer != null)
		{
			character.lockOnTarget = GameManager.I.activePlayer.transform;
		}
	}

	protected override void Tick(Character character)
	{
		behaviors.ForEach(module => module.Tick(character));
	}
}
