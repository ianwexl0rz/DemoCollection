using System;
using UnityEngine;

public class ActorController : ScriptableObject 
{
	protected virtual void Init(Actor actor)
	{
	}

	protected virtual void Tick(Actor actor)
	{
	}

	protected virtual void Clean(Actor actor)
	{
	}

	public virtual void Engage(Actor actor)
	{
		var oldController = actor.GetController();
		
		if(oldController)
		{
			oldController.Disengage(actor);
		}

		Init(actor);
		actor.UpdateController += Tick;
	}

	public virtual void Disengage(Actor actor)
	{
		actor.UpdateController -= Tick;
		Clean(actor);
	}
}
