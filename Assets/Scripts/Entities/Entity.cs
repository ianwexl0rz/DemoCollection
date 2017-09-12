using UnityEngine;
using System;

public class Entity : MonoBehaviour
{
	[SerializeField]
	protected Transform _mesh = null;
	public Transform mesh { get { return _mesh; } private set { _mesh = value; } }

	private Vector3 savedPosition;
	private Vector3 savedVelocity;
	private Vector3 savedAngularVelocity;

	public Rigidbody rb { get; private set; }

	public Action OnUpdate = delegate () { };
	public Action OnFixedUpdate = delegate () { };

	// Use this for initialization
	protected virtual void Awake()
	{
		rb = GetComponent<Rigidbody>();
	}

	protected virtual void OnEnable()
	{
		GameManager.I.AddEntity(this);
		GameManager.I.OnSetPaused += OnSetPaused;
		OnUpdate += SaveTransformPosition;
	}

	protected virtual void OnDisable()
	{
		if(!GameManager.I) { return; }

		GameManager.I.RemoveEntity(this);
		GameManager.I.OnSetPaused -= OnSetPaused;
		OnUpdate -= SaveTransformPosition;
	}

	public void SaveTransformPosition()
	{
		if(Time.timeScale != 0f)
		{
			savedPosition = transform.position;
		}
	}

	public void OnSetPaused(bool paused)
	{
		if(paused)
		{
			savedVelocity = rb.velocity;
			savedAngularVelocity = rb.angularVelocity;
			rb.Sleep();

			transform.position = savedPosition; 
		}
		else
		{
			rb.WakeUp();
			rb.AddForce(savedVelocity, ForceMode.VelocityChange);
			rb.AddTorque(savedAngularVelocity, ForceMode.VelocityChange);
		}
	}
}
