using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class Actor : Entity, ILockOnTarget, IDamageable
{
	public Animator animator { get; protected set; }
	public bool InputEnabled { get; set; }
	public Vector3 move { get; set; }
	public bool IsVisible { get; set; }
	public event Action<float> OnHealthChanged = delegate {  };

	public ILockOnTarget lockOnTarget = null;
	public Action<Actor> UpdateController = delegate { };
	public Action OnResetAbilities = null;
	public Action UpdateAbilities = null;
	public Action FixedUpdateAbilities = null;

	public event Action<Actor, float> OnTick;

	[HideInInspector]
	public List<ActorAbility> abilities = new List<ActorAbility>();

	public float Health { get; set; }
	public float MaxHealth { get; set; }

	protected readonly TimerGroup actorTimerGroup = new TimerGroup();
	public Timer hitReaction { get; protected set; }
	public Timer jumpAllowance { get; protected set; }
	
	public readonly InputBuffer inputBuffer = new InputBuffer();

	private Renderer[] renderers = null;
	private Coroutine damageFlash = null;
	private static readonly int DamageFlash = Shader.PropertyToID("_DamageFlash");


	public override void Awake()
	{
		base.Awake();
		animator = GetComponentInChildren<Animator>();
		Health = MaxHealth = 100f;

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

	private IEnumerator DoDamageFlash(float duration)
	{
		var time = 0f;
		while (time < duration)
		{
			var t = time / duration;
			var oneMinusT = 1 - t;
			SetDamageFlash(oneMinusT * oneMinusT);
			yield return null;
			if (!MainMode.PhysicsPaused) time += Time.deltaTime;
		}
		SetDamageFlash(0);
	}

	private void SetDamageFlash(float value)
	{
		foreach (var r in renderers)
		{
			var propertyBlock = new MaterialPropertyBlock();
			r.GetPropertyBlock(propertyBlock);
			propertyBlock.SetFloat(DamageFlash, value);
			r.SetPropertyBlock(propertyBlock);
		}
	}

	public override void Tick(float deltaTime)
	{
		base.Tick(deltaTime);
		OnTick?.Invoke(this, deltaTime);
		inputBuffer.Tick(deltaTime);
		actorTimerGroup.Tick(deltaTime);
		IsVisible = renderers.Any(r => r != null && r.isVisible);
	}

	protected override void OnPauseEntity(bool value)
	{
		if(animator != null) animator.SetPaused(value);
	}

	public void SetController(ActorController newController, object context = null)
	{
		newController.Init(this, context);
		OnTick = newController.Tick;
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

	public void TakeDamage(float damage)
	{
		// Calculate new health (un-clamped so we can do "overkill" events, etc.)
		var newHealth = Health - damage;

		// Clamp new health.
		newHealth = Mathf.Clamp(newHealth, 0, MaxHealth);
        
		// Early out if no change...
		if (Health.Equals(newHealth)) return;

		// Update health.
		Health = newHealth;
        
		// Do callback.
		OnHealthChanged(newHealth / MaxHealth);
        
		// Destroy if health is zero.
		if (newHealth < Mathf.Epsilon) Destroy();
	}

	public void Destroy()
	{
		this.WaitForEndOfFrameThen(() => Destroy(gameObject));
	}

	protected bool TryConsumeAction(int actionId)
	{
		return inputBuffer.ConsumeAction(actionId);
	}

	public override void ApplyHit(Entity instigator, Vector3 point, Vector3 direction, AttackData attackData)
	{
		// Apply knockback.
		base.ApplyHit(instigator, point, direction, attackData);

		// Apply damage.
		TakeDamage(attackData.damage);

		// Do damage flash.
		this.OverrideCoroutine(ref damageFlash, DoDamageFlash(0.2f));

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
