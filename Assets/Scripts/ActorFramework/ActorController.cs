using System;
using UnityEngine;

public class ActorController : ScriptableObject 
{
	public class Context
	{
	}
	
	protected virtual void Init(Actor actor, object context = null)
	{
	}

	protected virtual void Tick(Actor actor)
	{
	}

	protected virtual void Clean(Actor actor)
	{
	}

	public virtual void Engage(Actor actor, object context = null)
	{
		var oldController = actor.GetController();
		
		if(oldController)
		{
			oldController.Disengage(actor);
		}

		Init(actor, context);
		actor.UpdateController += Tick;
	}

	public virtual void Disengage(Actor actor)
	{
		actor.UpdateController -= Tick;
		Clean(actor);
	}
	
	protected static bool ValidateContext<T>(object context, out T specificContext) where T : Context
	{
		specificContext = context as T;
		if(specificContext != null) return true;
		
		Debug.LogError($"Cannot initialize mode because {typeof(T)} is invalid!");
		return false;
	}
}
