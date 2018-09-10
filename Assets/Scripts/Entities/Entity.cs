using UnityEngine;
using System;

public class Entity : MonoBehaviour
{
	private Vector3 savedPosition;
	private Vector3 savedRbPosition;
	private Vector3 savedVelocity;
	private Vector3 savedAngularVelocity;
	private bool savedKinematic;

	protected bool paused;

	[HideInInspector]
	public float localTimeScale = 1f;
	public float TimeScale { get { return localTimeScale * Time.timeScale; } }

	public Rigidbody rb { get; private set; }

	// Use this for initialization
	protected virtual void Awake()
	{
		rb = GetComponent<Rigidbody>();
	}

	protected virtual void OnEnable()
	{
		GameManager.I.AddEntity(this);
		GameManager.I.OnPauseGame += PauseEntity;
	}

	protected virtual void OnDisable()
	{
		if(!GameManager.I) { return; }

		GameManager.I.RemoveEntity(this);
		GameManager.I.OnPauseGame -= PauseEntity;
	}

	public void CachePosition()
	{
		savedPosition = transform.position;
	}

	public virtual void CacheRbPosition()
	{
		savedRbPosition = rb.position;
	}

	public virtual void OnUpdate()
	{
	}

	public virtual void OnFixedUpdate()
	{
	}

	protected virtual void OnPauseEntity(bool value)
	{
	}

	public void PauseEntity(bool value)
	{
		PausePhysics(value);
		OnPauseEntity(value);
	}

	public void PausePhysics(bool value)
	{
		paused = value;

		if(paused)
		{
			savedKinematic = rb.isKinematic;
			savedVelocity = rb.velocity;
			savedAngularVelocity = rb.angularVelocity;
			rb.Sleep();

			rb.isKinematic = true;
			rb.velocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;

			// Account for interpolation
			transform.position = savedPosition;
		}
		else
		{
			rb.WakeUp();
			rb.isKinematic = savedKinematic;
			rb.AddForce(savedVelocity, ForceMode.VelocityChange);
			rb.AddTorque(savedAngularVelocity, ForceMode.VelocityChange);

			rb.position = transform.position = savedRbPosition;
			//transform.position = savedRbPosition;
		}
	}
}
