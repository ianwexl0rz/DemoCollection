using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class Actor : Entity, ILockOnTarget, IDestructable
{
	[SerializeField]
	protected ActorController controller;
	public Animator animator { get; protected set; }

	public bool isAwake = false;
	public bool InputEnabled { get; set; }
	public Vector3 move { get; set; }
	public float look { get; set; }
	public bool lockOn { get; set; }
	public bool IsVisible { get; set; }
	public bool Recenter { get; set; }

	public ILockOnTarget lockOnTarget = null;

	public Action<Actor> UpdateController = delegate { };
	public Action OnResetAbilities = null;
	public Action<float> OnHealthChanged = delegate {  };
	public Action UpdateAbilities = null;
	public Action FixedUpdateAbilities = null;

	[HideInInspector]
	public List<ActorAbility> abilities = new List<ActorAbility>();

	public float Health
	{
		get => health;
		set
		{
			if (health.Equals(value)) return;
			health = Mathf.Clamp(value, 0f, maxHealth);
			OnHealthChanged(health / maxHealth);
		}
	}

	public float maxHealth { get; protected set; }

	protected readonly TimerGroup actorTimerGroup = new TimerGroup();
	public Timer hitReaction { get; protected set; }
	public Timer jumpAllowance { get; protected set; }

	public Action OnDestroyCallback { get; set; }

	private Renderer[] renderers = null;
	private float health;

	private void Start()
	{
		if (controller != null)
		{
			controller.Engage(this);
		}
	}

	protected virtual void OnDestroy()
	{
		OnDestroyCallback();
	}

	public override void Awake()
	{
		base.Awake();
		animator = GetComponentInChildren<Animator>();
		Health = maxHealth = 100f;

		InputEnabled = true;

		renderers = GetComponentsInChildren<Renderer>();

		actorTimerGroup.Add(hitReaction = new Timer(0f, StartHitReaction, EndHitReaction, true));
		actorTimerGroup.Add(jumpAllowance = new Timer());
	}

	private void StartHitReaction()
	{
		if(this is Character character)
			character.MeleeCombat.isAttacking = false;

		InputEnabled = false;
		//Debug.LogFormat("Started Hit Reaction with duration of {0}.", hitReaction.Duration);
	}

	private void EndHitReaction()
	{
		InputEnabled = true;
		//Debug.Log("Ended Hit Reaction.");
	}

	public override void Tick(float deltaTime)
	{
		base.Tick(deltaTime);

		UpdateController(this);
		actorTimerGroup.Tick(Time.deltaTime);
		IsVisible = renderers.Any(r => r != null && r.isVisible);
	}

	protected override void OnPauseEntity(bool value)
	{
		if(animator != null)
			animator.SetPaused(value);
	}

	public ActorController GetController() => controller;

	public void SetController(ActorController newController, object context = null)
	{
		newController.Engage(this, context);
		controller = newController;
	}

	private void ResetAbilities()
	{
		OnResetAbilities?.Invoke();
	}

	public virtual Vector3 GetLookPosition()
	{
		return transform.position;
	}

	public virtual Vector3 GetGroundPosition()
	{
		return transform.position;
	}

	public override void ApplyHit(Entity instigator, Vector3 point, Vector3 direction, AttackData attackData)
	{
		// Applies knockback.
		base.ApplyHit(instigator, point, direction, attackData);

		Health = Mathf.Max(Health - attackData.damage, 0f);
		//Debug.Log("Hit " + name + " - HP: " + health + "/" + maxHealth);

		// TODO: Get reaction type from AttackData 
		var duration = Mathf.Max(hitReaction.Duration - hitReaction.Current, attackData.stun);
		hitReaction.Reset(duration);

		if(this is Character character)
		{
			character.MeleeCombat.isAttacking = false;
			character.MeleeCombat.cancelOK = true;
		}
	}
}
