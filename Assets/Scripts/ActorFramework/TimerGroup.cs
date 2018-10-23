using System.Collections.Generic;
using System;

public class TimerGroup : List<Timer>
{
	public void Add(float duration, Action onStart, Action onEnd)
	{
		Add(new Timer(duration, onStart, onEnd));
	}
	
	public void Tick(float dt)
	{
		for(var i = Count; i-- > 0;)
		{
			var t = this[i];
			if(t.Tick(dt) || t.Persistent) continue;
			RemoveAt(i);
		}
	}
}
