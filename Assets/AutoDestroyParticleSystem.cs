using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoDestroyParticleSystem : MonoBehaviour {

	private ParticleSystem ps;

	void Awake()
	{
		ps = GetComponent<ParticleSystem>();
	}
	
	void Update ()
	{
		if(!ps.IsAlive())
		{
			Destroy(gameObject);
		}
	}
}
