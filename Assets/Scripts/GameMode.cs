using Rewired;
using System;
using UnityEngine;

public enum GameModeType
{
	Main,
	Pause
}

public abstract class GameMode
{
	protected static bool ValidateContext<T>(object context, out T specificContext) where T : Context
	{
		specificContext = context as T;
		if(specificContext != null) return true;
		
		Debug.LogError($"Cannot initialize mode because {typeof(T)} is invalid!");
		return false;
	}
	
	public abstract class Context { }
    
	public abstract void Init(object context, Action callback = null);

	public abstract void Tick(float deltaTime);

	public abstract void LateTick(float deltaTime);
	
	public abstract void FixedTick(float deltaTime);
    
	public abstract void Clean();
}
