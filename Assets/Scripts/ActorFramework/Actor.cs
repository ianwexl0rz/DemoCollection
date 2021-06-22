using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class Actor : Entity, ILockOnTarget, IDamageable
{
	protected Animator Animator { get; private set; }
	public bool InputEnabled { get; set; }
	public Vector3 Move { get; set; }
	public bool IsVisible { get; set; }
	public event Action<float> OnHealthChanged = delegate {  };

	public ILockOnTarget lockOnTarget = null;
	public float Health { get; set; }
	public float MaxHealth { get; set; }
	
	public readonly InputBuffer InputBuffer = new InputBuffer();

	protected Timer HitReaction { get; private set; }
	protected Timer JumpAllowance { get; private set; }

	private ActorController _controller;
	private Renderer[] _renderers = null;
	private Coroutine _damageFlash = null;
	private readonly TimerGroup _actorTimerGroup = new TimerGroup();
	private static readonly int DamageFlash = Shader.PropertyToID("_DamageFlash");
	

	public override void Awake()
	{
		base.Awake();
		Animator = GetComponentInChildren<Animator>();
		Health = MaxHealth = 100f;

		InputEnabled = true;

		_renderers = GetComponentsInChildren<Renderer>();

		_actorTimerGroup.Add(HitReaction = new Timer(0f, StartHitReaction, EndHitReaction, true));
		_actorTimerGroup.Add(JumpAllowance = new Timer());
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
		foreach (var r in _renderers)
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
		if (_controller) _controller.Tick(this, deltaTime);
		InputBuffer.Tick(deltaTime);
		_actorTimerGroup.Tick(deltaTime);
		IsVisible = _renderers.Any(r => r != null && r.isVisible);
	}

	protected override void OnPauseEntity(bool value)
	{
		if(Animator != null) Animator.SetPaused(value);
	}
	
	public void SetController(ActorController newController, object context = null)
	{
		if (_controller != null)
			_controller.Clean(this);
			
		newController.Init(this, context);
		_controller = newController;
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
		return InputBuffer.ConsumeAction(actionId);
	}

	public override void ApplyHit(Entity instigator, Vector3 point, Vector3 direction, AttackData attackData)
	{
		// Apply knockback.
		base.ApplyHit(instigator, point, direction, attackData);

		// Apply damage.
		TakeDamage(attackData.damage);

		// Do damage flash.
		this.OverrideCoroutine(ref _damageFlash, DoDamageFlash(0.2f));

		// TODO: Get reaction type from AttackData 
		var duration = Mathf.Max(HitReaction.Duration - HitReaction.Current, attackData.stun);
		HitReaction.Reset(duration);
	}
}
