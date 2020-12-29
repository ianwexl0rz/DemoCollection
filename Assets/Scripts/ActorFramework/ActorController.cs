using System;
using UnityEngine;

public class ActorController : ScriptableObject
{
	protected Actor actor;
	public Actor Actor => actor;
	
	public readonly InputBuffer inputBuffer = new InputBuffer();

	public class Context
	{
	}
	
	protected virtual void Init(Actor actor, object context = null)
	{
		this.actor = actor;
	}

	public virtual void Tick()
	{
		inputBuffer.Update(Time.deltaTime);
	}

	protected virtual void Clean()
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
		//actor.UpdateController += Tick;
	}

	public virtual void Disengage(Actor actor)
	{
		//actor.UpdateController -= Tick;
		Clean();
	}
	
	protected static bool ValidateContext<T>(object context, out T specificContext) where T : Context
	{
		specificContext = context as T;
		if(specificContext != null) return true;
		
		Debug.LogError($"Cannot initialize mode because {typeof(T)} is invalid!");
		return false;
	}
}
