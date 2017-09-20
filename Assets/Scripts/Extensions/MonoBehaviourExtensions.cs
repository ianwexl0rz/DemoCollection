using UnityEngine;
using System.Collections;
using System;

public static class MonoBehaviourExtensions
{
	// Execute in some number of seconds (if zero, next frame)
	public static Coroutine ExecuteWithDelay(this MonoBehaviour mono, Action action, float delay)
	{
		return mono.StartCoroutine(DelayedExecute(action, delay));
	}

	private static IEnumerator DelayedExecute(Action action, float delay)
	{
		yield return new WaitForSecondsRealtime(delay);
		action();
	}


	// Start a coroutine and stop an existing coroutine if necessary
	public static Coroutine OverrideCoroutine(this MonoBehaviour mono, ref Coroutine coroutine, IEnumerator ienumerator)
	{
		if(coroutine != null)
		{
			mono.StopCoroutine(coroutine);
		}

		return coroutine = mono.StartCoroutine(ienumerator);
	}
}
