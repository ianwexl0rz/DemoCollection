using System.Collections.Generic;
using System;

public class InputBuffer : List<InputBuffer.ActorInput>
{
	public class ActorInput
	{
		public Func<bool> Action;
		public float Valid;

		public ActorInput(Func<bool> action, float valid)
		{
			Action = action;
			Valid = valid;
		}

		public bool TryInput(float dt)
		{
			return Action() || (Valid -= dt) <= 0;
		}
	}

	public void Add(Func<bool> action, float valid)
	{
		Add(new ActorInput(action, valid));
	}

	public void Update(float deltaTime)
	{
		for(var i = Count; i-- > 0;)
		{
			if(this[i].TryInput(deltaTime))
				RemoveAt(i);
		}
	}
}
