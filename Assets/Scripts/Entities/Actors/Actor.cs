using UnityEngine;
using System;
using System.Collections.Generic;

public class Actor : Entity
{
	[SerializeField]
	protected ActorBrain _brain = null;

	

	public bool isAwake = false;

	public Vector3 move { get; set; }
	public Vector3 look { get; set; }
	public bool lockOn { get; set; }

	public Transform lockOnTarget = null;

	public Animator animator { get; private set; }

	public ActorBrain brain { get { return _brain; } private set { _brain = value; } }

	public Action OnResetAbilities = null;
	public Action UpdateAbilities = null;
	public Action FixedUpdateAbilities = null;

	[HideInInspector]
	public List<ActorAbility> abilities = new List<ActorAbility>();

	// Use this for initialization
	protected override void Awake()
	{
		base.Awake();
		animator = GetComponentInChildren<Animator>();
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		OnUpdate += GetInput;
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		OnUpdate -= GetInput;
	}

	private void Start()
	{
		look = transform.forward;

		if(brain != null)
		{
			brain.Init(this);
		}
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
