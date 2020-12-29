using Rewired;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class GameMode
{
	public static GameMode Current { get; private set; }
	
	protected static Player player;
	private static List<GameMode> modes;
	
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

	public static void SetPlayer(Player player) => GameMode.player = player;

	public static void RegisterModes(List<GameMode> modes) => GameMode.modes = modes;
	
	public static void SetMode<T>(object context = null, Action callback = null) where T : GameMode
	{
		var newMode = modes.First(mode => mode is T);
		GameManager.I.WaitForEndOfFrameThen(() => SetMode(newMode, context, callback));
	}

	private static void SetMode(GameMode value, object context, Action callback)
	{
		if (Current == value) return;
		Current?.Clean();
		Current = value;
		Current.Init(context, callback);
	}
}
