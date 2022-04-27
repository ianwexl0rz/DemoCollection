using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class Actor : Entity, IDamageable
{
	private static readonly int DamageFlash = Shader.PropertyToID("_DamageFlash");

	public event Action OnReceiveHit;
	
	public event Action OnBeginHitReaction;
	
	public event Action OnEndHitReaction;
	
	public event Action OnHandleAbilityInput;

	public event Action<float> OnUpdateSubFrameAnimation;

	public event Action<float> OnHealthChanged = delegate {  };

	[SerializeField] private ActorController controller;

	private Renderer[] _renderers = null;
	private Coroutine _damageFlash = null;
	private readonly TimerGroup _actorTimerGroup = new TimerGroup();

	public Animator Animator { get; private set; }

	public bool InputEnabled { get; set; }

	public Vector3 Move { get; set; }

	public bool IsVisible { get; set; }

	public ITrackable TrackedTarget { get; set; }

	public float Health { get; set; }

	public float MaxHealth { get; set; }

	public readonly InputBuffer InputBuffer = new InputBuffer();

	public Timer HitReaction { get; private set; }

	public Timer JumpAllowance { get; private set; }


	public override void Awake()
	{
		base.Awake();
		Animator = GetComponentInChildren<Animator>();
		Health = MaxHealth = 100f;

		InputEnabled = true;

		_renderers = GetComponentsInChildren<Renderer>();

		_actorTimerGroup.Add(HitReaction = new Timer(0f, 
			() => InputEnabled = false, 
			() => InputEnabled = true,
			true));
		
		_actorTimerGroup.Add(JumpAllowance = new Timer());
	}

	protected override void UpdatePhysics(float deltaTime)
	{
		base.UpdatePhysics(deltaTime);
		if (InputEnabled) OnHandleAbilityInput?.Invoke();
	}

	protected override void UpdateAnimation(float deltaTime)
	{
		base.UpdateAnimation(deltaTime);
	}

	public void UpdateSubFrameAnimation(float progress)
	{
		OnUpdateSubFrameAnimation?.Invoke(progress);
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
			if (r == null) continue;
			var propertyBlock = new MaterialPropertyBlock();
			r.GetPropertyBlock(propertyBlock);
			propertyBlock.SetFloat(DamageFlash, value);
			r.SetPropertyBlock(propertyBlock);
		}
	}

	public override void Tick(float deltaTime)
	{
		base.Tick(deltaTime);
		if (controller) controller.Tick(this, deltaTime);
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
		if (controller != null)
			controller.Clean(this);
			
		newController.Init(this, context);
		controller = newController;
	}

	public virtual Vector3 GetEyesPosition() => transform.position;

	public virtual Vector3 GetGroundPosition() => transform.position;

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

	public bool TryConsumeAction(int actionId)
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
		
		OnReceiveHit?.Invoke();
	}
}
