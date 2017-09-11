using UnityEngine;

[CreateAssetMenu(fileName = "Empty Brain", menuName = "Actor/Brain/Empty Brain")]
public class ActorBrain : ScriptableObject 
{
	public virtual void Init(Actor actor)
	{
	}

	public virtual void Process(Actor actor)
	{
	}

	public virtual void Clean(Actor actor)
	{
	}
}
