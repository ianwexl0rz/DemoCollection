using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoDestroyParticleSystem : MonoBehaviour {

	public float offsetCloserToCamera = 0f;

	private ParticleSystem ps;
	private Vector3 originalPos;

	private void OnEnable()
	{
		ps = GetComponent<ParticleSystem>();
		originalPos = transform.position;
		GameManager.I.OnPauseGame += ToggleParticlePlayback;
	}

	private void OnDisable()
	{
		GameManager.I.OnPauseGame -= ToggleParticlePlayback;
	}

	void ToggleParticlePlayback(bool paused)
	{
		if(paused) ps.Pause(); else ps.Play();
	}

	void Update ()
	{
		Vector3 dir = Camera.main.transform.position - originalPos;
		transform.position = originalPos + dir.normalized * offsetCloserToCamera;

		if(!ps.IsAlive())
		{
			Destroy(gameObject);
		}
	}
}
