using UnityEngine;

[System.Serializable]
public class AIBehavior : ScriptableObject
{
	public virtual void Tick(ActorController controller, Actor actor)
	{
	}
}
