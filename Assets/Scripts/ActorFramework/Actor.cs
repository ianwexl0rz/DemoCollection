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
	public bool InputEnabled { get; set; }


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
		animator = GetComponentInChildren<Animator>();
		health = maxHealth = 100f;

		InputEnabled = true;

		actorTimerGroup.Add(hitReaction = new Timer(0f, StartHitReaction, EndHitReaction, true));
		actorTimerGroup.Add(jumpAllowance = new Timer());
	}

	private void StartHitReaction()
	{
		if(this is Character character)
			character.meleeCombat.isAttacking = false;

		InputEnabled = false;
		//Debug.LogFormat("Started Hit Reaction with duration of {0}.", hitReaction.Duration);
	}

	private void EndHitReaction()
	{
		InputEnabled = true;
		//Debug.Log("Ended Hit Reaction.");
	}

	private void Start()
	{
		if(controller != null)
		{
			controller.Engage(this);
		}
	}

	 protected virtual void Update()
	{
		if(GameManager.I.GamePaused) { return; }

		UpdateController(this);
		//ProcessAnimation();
		actorTimerGroup.Tick(Time.deltaTime);
	}

	protected override void FixedUpdate()
	{
		base.FixedUpdate();

		if(!GameManager.I.PhysicsPaused)
			ProcessPhysics();
	}

	protected override void OnPauseEntity(bool value)
	{
		if(animator != null)
			animator.SetPaused(value);
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

	protected override void ApplyHit(HitData hit)
	{
		// Applies knockback.
		base.ApplyHit(hit);

		health = Mathf.Max(health - hit.attackData.damage, 0f);
		//Debug.Log("Hit " + name + " - HP: " + health + "/" + maxHealth);

		// TODO: Get reaction type from AttackData 
		var duration = Mathf.Max(hitReaction.Duration - hitReaction.Current, hit.attackData.stun);
		hitReaction.Reset(duration);

		if(this is Character character)
		{
			character.meleeCombat.isAttacking = false;
			character.meleeCombat.cancelOK = true;
		}
	}
}
