using UnityEngine;
using System;
using System.Collections.Generic;

public class Actor : MonoBehaviour
{
	[SerializeField]
	protected ActorBrain _brain = null;

	[SerializeField]
	protected Transform _mesh = null;

	public bool isAwake = false;

	public Vector3 move { get; set; }
	public Vector3 look { get; set; }
	public bool lockOn { get; set; }

	public Transform lockOnTarget = null;

	public Rigidbody rb { get; private set; }
	public Animator animator { get; private set; }

	public ActorBrain brain { get { return _brain; } private set { _brain = value; } }
	public Transform mesh { get { return _mesh; } private set { _mesh = value; } }

	public Action OnResetAbilities = null;
	public Action UpdateAbilities = null;
	public Action FixedUpdateAbilities = null;

	[HideInInspector]
	public List<ActorAbility> abilities = new List<ActorAbility>();

	// Use this for initialization
	private void Start()
	{
		animator = GetComponentInChildren<Animator>();

		rb = GetComponent<Rigidbody>();
		look = transform.forward;

		if(brain != null)
		{
			brain.Init(this);
		}
		OnAwake();
	}

	private void OnEnable()
	{
		GameManager.I.AddActor(this);
	}

	private void OnDisable()
	{
		if(GameManager.I) GameManager.I.RemoveActor(this);
	}

	public void UpdateActor()
	{
		GetInput();
		OnUpdate();
		if(UpdateAbilities != null)
		{
			UpdateAbilities();
		}
		
	}

	private void FixedUpdate()
	{
		OnFixedUpdate();
		if(FixedUpdateAbilities != null)
		{
			FixedUpdateAbilities();
		}
	}

	protected virtual void OnUpdate()
	{
	}

	protected virtual void OnFixedUpdate()
	{
	}

	protected virtual void OnAwake()
	{
	}

	private void GetInput()
	{
		if(isAwake && brain != null)
		{
			brain.Process(this);
		}
		else
		{
			move = Vector3.zero;
			ResetAbilities();
		}
	}

	public void SetBrain(ActorBrain newBrain)
	{
		if(brain != null)
		{
			brain.Clean(this);
		}

		brain = newBrain;
		brain.Init(this);
	}

	private void ResetAbilities()
	{
		if(OnResetAbilities != null)
		{
			OnResetAbilities();
		}
	}
}
