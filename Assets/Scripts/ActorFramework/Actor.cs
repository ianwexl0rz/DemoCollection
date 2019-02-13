using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

public class Actor : Entity
{
	[SerializeField]
	protected ActorController controller;
	public Animator animator { get; protected set; }

	public bool isAwake = false;

	public Vector3 move { get; set; }
	public float look { get; set; }
	public bool lockOn { get; set; }

	public Actor lockOnTarget = null;

	public Action<Actor> UpdateController = delegate { };
	public Action OnResetAbilities = null;
	public Action UpdateAbilities = null;
	public Action FixedUpdateAbilities = null;

	[HideInInspector]
	public List<ActorAbility> abilities = new List<ActorAbility>();

	public float health { get; set; }
	public float maxHealth { get; protected set; }

	protected readonly TimerGroup actorTimerGroup = new TimerGroup();
	public Timer hitReaction { get; protected set; }
	public Timer jumpAllowance { get; protected set; }

	// Use this for initialization
	protected override void Awake()
	{
		base.Awake();
		animator = GetComponent<Animator>();
		health = maxHealth = 100f;

		actorTimerGroup.Add(hitReaction = new Timer());
		actorTimerGroup.Add(jumpAllowance = new Timer());
	}

	private void Start()
	{
		if(controller != null)
		{
			controller.Engage(this);
		}
	}

	public override void OnUpdate()
	{
		base.OnUpdate();
		UpdateController(this);
		ProcessAnimation();
		actorTimerGroup.Tick(Time.deltaTime);
	}

	public override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		ProcessPhysics();
	}

	protected override void OnPauseEntity(bool value)
	{
		if(animator != null)
		{
			animator.SetPaused(value);
		}
	}

	protected virtual void ProcessAnimation()
	{
	}

	protected virtual void ProcessPhysics()
	{
	}

	public ActorController GetController()
	{
		return controller;
	}

	public void SetController(ActorController newController)
	{
		newController.Engage(this);
		controller = newController;
	}

	private void ResetAbilities()
	{
		OnResetAbilities?.Invoke();
	}

	public virtual Vector3 GetLockOnPosition()
	{
		return transform.position;
	}

	protected override void OnGetHit(Vector3 hitPoint, Vector3 direction, AttackData data)
	{
		OnEarlyFixedUpdate = () =>
		{
			rb.AddForce(direction * data.knockback / Time.fixedDeltaTime, ForceMode.Acceleration);
			//rb.AddForceAtPosition(direction * data.knockback * 0.25f / Time.fixedDeltaTime, rb.position.WithY(hitPoint.y), ForceMode.Acceleration);
		};

		health = Mathf.Max(health - data.damage, 0f);
		//Debug.Log("Hit " + name + " - HP: " + health + "/" + maxHealth);

		// TODO: Get reaction type from AttackData 
		var duration = Mathf.Max(hitReaction.Duration - hitReaction.Current, data.stun);
		hitReaction.Reset(duration);
	}
}
