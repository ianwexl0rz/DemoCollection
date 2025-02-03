using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ActorFramework;

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

	public override void Possess(Actor actor, object context = null)
	{
		if(context is Trackable trackable)
			TrackedTarget = trackable;
	}

	public override void Tick(Actor actor, float deltaTime)
	{
		if (!actor.IsAlive()) return;
		
		if (TrackedTarget && TrackedTarget.Owner.Health.Current <= 0) TrackedTarget = null;
		if (!TrackedTarget)
		{
			var motor = actor.GetComponent<IActorMotor>();
			if (motor != null)
			{
				// TODO: We shouldn't make garbage
				var inputs = new CharacterInputs();
				motor.SetInputs( ref inputs);
			}
			return;
		}
		
		// TODO: Make sure lockon target stays current.
		foreach(AIConditionalBehaviorGroup group in behaviorGroups)
		{
			if(EvaluateCondition(actor, group.condition, group.threshold))
			{
				group.behaviors.ForEach(behavior => behavior.Tick(this, actor));
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
		if (!TrackedTarget) { return false; }

		var vector = (TrackedTarget.GetEyesPosition() - actor.Trackable.GetEyesPosition()).WithY(0f);
		return vector.magnitude <= threshold;
	}
}
