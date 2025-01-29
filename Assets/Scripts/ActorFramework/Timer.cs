using System;

[Serializable]
public class Timer
{
	private readonly Action onStart;
	private readonly Action onEnd;
	private readonly Action<float> overTime;

	public float Current { get; private set; }
	public float Duration { get; private set; }
	public bool Persistent { get; }
	public bool InProgress => Current < Duration && Duration > 0;
	public float NormalizedTime => Current / Duration;
	
	private readonly bool useNormalizedTime;

	public Timer(bool persistent = true)
	{
		Persistent = persistent;
	}

	public Timer(float duration, Action onStart, Action onEnd, bool persistent = false, Action<float> overTime = null, bool useNormalizedTime = true)
	{
		Current = 0f;

		Duration = duration;
		Persistent = persistent;
		this.onStart = onStart;
		this.onEnd = onEnd;
		this.overTime = overTime;
		this.useNormalizedTime = useNormalizedTime;
	}

	public void SetDuration(float time)
	{
		Duration = time;
	}

	public bool Tick(float dt)
	{
		if(Duration > 0 && Current < float.Epsilon)
		{
			onStart?.Invoke();
		}

		if(Math.Abs(Current - Duration) < float.Epsilon) return false;

		Current += dt;

		if(Current < Duration)
		{
			if(overTime == null) return true;

			var t = Current;
			if(useNormalizedTime) t /= Duration;
			overTime(t);
			
			return true;
		}

		Current = Duration;
		onEnd?.Invoke();
		return false;
	}

	public void Reset()
	{
		Current = 0;
	}

	public void Reset(float duration)
	{
		Current = 0;
		Duration = duration;
	}
}
