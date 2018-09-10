using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class TimeDilator : MonoBehaviour {

	public float speedMultiplier = 0.2f;
	public List<Entity> entities = new List<Entity>();

	private SphereCollider sphereCollider = null;

	// Use this for initialization
	void Awake ()
	{
		sphereCollider = GetComponent<SphereCollider>();
	}
	
	// Update is called once per frame
	void Update ()
	{

		foreach(Entity entity in entities)
		{
			float distance = Vector3.Distance(transform.position, entity.transform.position);

			distance /= sphereCollider.radius;

			entity.localTimeScale = Mathf.Lerp(speedMultiplier, 1f, distance);
		}
		
	}

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
