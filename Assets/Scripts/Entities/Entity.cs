using UnityEngine;
using System;

public class Entity : MonoBehaviour
{
	private Vector3 savedRbPosition;
	private Vector3 savedVelocity;
	private Vector3 savedAngularVelocity;
	private bool savedKinematic;

	protected bool physicsPaused;

	[HideInInspector]
	public float localTimeScale = 1f;
	public float TimeScale { get { return localTimeScale * Time.timeScale; } }

	public Rigidbody rb { get; private set; }

	// Use this for initialization
	protected virtual void Awake()
	{
		rb = GetComponent<Rigidbody>();

		Physics.autoSyncTransforms = false;
	}

	protected virtual void OnEnable()
	{
		GameManager.I.AddEntity(this);
		GameManager.I.PauseAllPhysics += PauseEntity;
	}

	protected virtual void OnDisable()
	{
		if(!GameManager.I) { return; }

		GameManager.I.RemoveEntity(this);
		GameManager.I.PauseAllPhysics -= PauseEntity;
	}

	public virtual void OnUpdate()
	{
	}

	public virtual void OnLateUpdate()
	{
	}

	public virtual void OnFixedUpdate()
	{
	}

	protected virtual void OnPauseEntity(bool value)
	{
	}

	bool savedPhysicsPaused;

	public void PauseEntity(bool value)
	{
		
		if(value)
		{
			savedPhysicsPaused = physicsPaused;

			if(!savedPhysicsPaused)
				PausePhysics(true);
		}
		else
		{
			if(!savedPhysicsPaused)
				PausePhysics(false);
		}
		
		OnPauseEntity(value);
	}

	private float savedLocalTimeScale = 1f;

	public void PausePhysics(bool value)
	{
		physicsPaused = value;

		if(physicsPaused)
		{
			savedRbPosition = rb.position;
			savedLocalTimeScale = localTimeScale;
			savedKinematic = rb.isKinematic;
			savedVelocity = rb.velocity;
			savedAngularVelocity = rb.angularVelocity;

			localTimeScale = 0f;
			rb.isKinematic = true;
			rb.velocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
			rb.interpolation = RigidbodyInterpolation.None;

			// Account for interpolation
			rb.position = transform.position;
			rb.Sleep();
		}
		else
		{
			rb.WakeUp();
			localTimeScale = savedLocalTimeScale;
			rb.isKinematic = savedKinematic;
			rb.interpolation = RigidbodyInterpolation.Interpolate;

			rb.AddForce(savedVelocity, ForceMode.VelocityChange);
			rb.AddTorque(savedAngularVelocity, ForceMode.VelocityChange);

			rb.position = savedRbPosition;
		}
	}
}
