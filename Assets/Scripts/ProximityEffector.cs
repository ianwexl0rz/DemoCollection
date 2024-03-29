﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class ProximityEffector : MonoBehaviour {

	public float intensity = 1f;
	public List<Entity> entities = new List<Entity>();

	/*
	private SphereCollider sphereCollider = null;

	private void Awake()
	{
		sphereCollider = GetComponent<SphereCollider>();
	}
	
	private void Update()
	{
		foreach(Entity entity in entities)
		{
			float distance = Vector3.Distance(transform.position, entity.transform.position) / sphereCollider.radius;

			//entity.someProperty = intensity * (1 - distance);
		}
	}
	*/

	private void OnTriggerEnter(Collider other)
	{
		Entity e = other.GetComponent<Entity>();

		if(e != null && !entities.Contains(e))
		{
			entities.Add(e);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		Entity e = other.GetComponent<Entity>();

		if(e != null && entities.Contains(e))
		{
			entities.Remove(e);
		}
	}
}
