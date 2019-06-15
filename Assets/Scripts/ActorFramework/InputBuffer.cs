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
			// True is action is successful OR valid window has expired.
			return Action() || (Valid -= dt) <= 0;
		}
	}

	public void Add(Func<bool> action, float valid)
	{
		Add(new ActorInput(action, valid));
	}

	public void Update(float deltaTime)
	{
		// Attempt input from newest to oldest.
		for(var i = Count; i-- > 0;)
		{
			// Remove from the list if success or valid window has expired.
			if(this[i].TryInput(deltaTime))
				RemoveAt(i);
		}
	}
}
