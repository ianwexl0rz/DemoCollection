using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class AIConditionalBehaviorGroup
{
	public AIBehaviorCondition condition = AIBehaviorCondition.Always;
	public float threshold = 0f;
	public List<AIBehavior> behaviors = new List<AIBehavior>();
}

public enum AIBehaviorCondition
{
	Always,
	LessThanOrEqualToDistance,
	GreaterThanDistance
}

[CreateAssetMenu(fileName = "AI Controller", menuName = "Actor/Controllers/AI Controller")]
public class AIController : ActorController
{
	public List<AIConditionalBehaviorGroup> behaviorGroups = new List<AIConditionalBehaviorGroup>();

	public override void Init(Actor actor, object context = null)
	{
		base.Init(actor, context);
		
		if(context is ILockOnTarget lockOnTarget)
			actor.lockOnTarget = lockOnTarget;
	}

	protected override void OnTick(Actor actor, float deltaTime)
	{
		//actor.lockOnTarget = PlayerController.instance.Actor;
		
		// TODO: Make sure lockon target stays current.
		
		foreach(AIConditionalBehaviorGroup group in behaviorGroups)
		{
			if(EvaluateCondition(actor, group.condition, group.threshold))
			{
				group.behaviors.ForEach(behavior => behavior.Tick(actor));
			}
		}
	}

	private bool EvaluateCondition(Actor actor, AIBehaviorCondition condition, float threshold)
	{
		switch(condition)
		{
			case AIBehaviorCondition.Always:
				return true;
			case AIBehaviorCondition.LessThanOrEqualToDistance:
				return ProximityCheck(actor, threshold);
			case AIBehaviorCondition.GreaterThanDistance:
				return !ProximityCheck(actor, threshold);
		}
		return true;
	}

	private bool ProximityCheck(Actor actor, float threshold)
	{
		if(actor.lockOnTarget == null) { return false; }

		var vector = (actor.lockOnTarget.GetLookPosition() - actor.GetLookPosition()).WithY(0f);
		return vector.magnitude <= threshold;
	}
}
