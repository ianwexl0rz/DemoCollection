using System.Collections.Generic;
using System;

public class InputBuffer : List<KeyValuePair<int, float>>
{
	public void Add(int actionId, float valid)
	{
		Add(new KeyValuePair<int, float>(actionId, valid));
	}

	public bool ConsumeAction(int actionId)
	{
		var index = FindLastIndex(input => input.Key == actionId);
		var hasAction = index >= 0;
		if (hasAction) RemoveAt(index);

		return hasAction;
	}

	public void Tick(float deltaTime)
	{
		// Attempt input from newest to oldest.
		for(var i = Count; i-- > 0;)
		{
			var kp = this[i];
			var remainingTime = kp.Value - deltaTime;

			// Keep the input if time has not expired.
			if (remainingTime > 0)
			{
				this[i] = new KeyValuePair<int, float>(kp.Key, remainingTime);
				continue;
			}

			// Time is up!
			RemoveAt(i);
		}
	}
}
