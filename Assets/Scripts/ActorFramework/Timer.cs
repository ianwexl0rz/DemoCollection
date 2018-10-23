using System;

public class Timer
{
	private readonly Action onEnd;
	private readonly Action<float> overTime;

	public float Current { get; private set; }
	public float Duration { get; private set; }
	public bool Persistent { get; }
	public bool InProgress => Current < Duration;
	private readonly bool normalizedTime;

	public Timer(bool persistent = true)
	{
		Persistent = persistent;
	}

	public Timer(float duration, Action onStart, Action onEnd, bool persistent = false, Action<float> overTime = null, bool normalizedTime = true)
	{
		Current = 0f;

		Duration = duration;
		Persistent = persistent;
		this.onEnd = onEnd;
		this.overTime = overTime;
		this.normalizedTime = normalizedTime;
		onStart?.Invoke();
	}

	public void SetDuration(float time)
	{
		Duration = time;
	}

	public bool Tick(float dt)
	{
		if(Math.Abs(Current - Duration) < float.Epsilon) return false;

		Current += dt;

		if(Current < Duration)
		{
			if(overTime == null) return true;

			var t = Current;
			if(normalizedTime) t /= Duration;
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
}
