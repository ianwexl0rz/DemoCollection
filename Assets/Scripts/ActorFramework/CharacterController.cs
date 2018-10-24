using System;
using UnityEngine;

public class CharacterController : ScriptableObject 
{
	protected virtual void Init(Character character)
	{
	}

	protected virtual void Tick(Character character)
	{
	}

	protected virtual void Clean(Character character)
	{
	}

	public virtual void Engage(Character character)
	{
		var controller = character.GetController();
		if(controller != null)
		{
			controller.Disengage(character);
		}

		Init(character);
		character.UpdateController += Tick;
	}

	public virtual void Disengage(Character character)
	{
		character.UpdateController -= Tick;
		Clean(character);
	}
}
