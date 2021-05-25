using System;
using UnityEngine;

public abstract class ActorController : ScriptableObject
{
	protected abstract void OnTick(Actor actor, float deltaTime);

	public virtual void Init(Actor actor, object context = null) => actor.OnTick += OnTick;

	public void Tick(Actor actor, float deltaTime) => OnTick(actor, deltaTime);
}
