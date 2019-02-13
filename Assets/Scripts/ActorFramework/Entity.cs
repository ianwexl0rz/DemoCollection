using UnityEngine;
using System.Collections.Generic;
using System;
using InControl;

[Serializable]
public struct RigidbodyState
{
	public Vector3 position;
	public Quaternion rotation;
	public Vector3 velocity;
	public Vector3 angularVelocity;
	public bool isKinematic;

	public RigidbodyState(Rigidbody rb)
	{
		position = rb.position;
		rotation = rb.rotation;
		velocity = rb.velocity;
		angularVelocity = rb.angularVelocity;
		isKinematic = rb.isKinematic;
	}
}

public class Entity : MonoBehaviour
{
	private RigidbodyState savedState;

	protected bool paused;

	public Action OnLateUpdate = delegate { };
	protected Action OnEarlyFixedUpdate = delegate { };

	public Rigidbody rb { get; private set; }
	public bool IsPaused => paused;

	// Use this for initialization
	protected virtual void Awake()
	{
		rb = GetComponent<Rigidbody>();

		Physics.autoSyncTransforms = true;
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

		if(paused) { PauseEntity(false); }
	}

	public virtual void OnUpdate()
	{
	}

	/*/
	public virtual void OnLateUpdate()
	{
	}
	//*/

	public virtual void OnFixedUpdate()
	{
		OnEarlyFixedUpdate?.Invoke();
		OnEarlyFixedUpdate = null;
	}

	protected virtual void OnPauseEntity(bool value)
	{
	}

	protected virtual void OnGetHit(Vector3 direction, AttackData data)
	{
	}

	public void PauseEntity(bool value)
	{
		if(value == paused) { return; } else { paused = value; }
		
		if(value)
		{
			savedState = new RigidbodyState(rb);

			rb.isKinematic = true;
			rb.velocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;

			// Account for interpolation
			// TODO: Set rigidbody position between current transform position and previous transform position
			// depending on sub-frame collision. Also set animator.Simulate() to that time.
			rb.position = transform.position;
			rb.rotation = transform.rotation;
			rb.Sleep();
		}
		else
		{
			rb.WakeUp();
			rb.RestoreState(savedState);
		}

		OnPauseEntity(value);
	}

	protected virtual void OnGetHit(Vector3 hitPoint, Vector3 direction, AttackData data)
	{
		OnEarlyFixedUpdate = () => rb.AddForceAtPosition(direction * data.knockback / Time.fixedDeltaTime, hitPoint, ForceMode.Acceleration);
	}

	public void GetHit(Vector3 hitPoint, Vector3 direction, AttackData data)
	{
		OnGetHit(hitPoint, direction, data);
	}
}
