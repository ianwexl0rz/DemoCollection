using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using ActorFramework;
using DemoCollection;

public class Actor : Entity, IDamageable
{
	private static readonly int DamageFlash = Shader.PropertyToID("_DamageFlash");

	public event Action GetHit;
	
	public event Action<InputBuffer> ConsumeInput;
	
	[SerializeField] private ActorController controller;

	private Health _health;
	private Renderer[] _renderers;
	private Coroutine _damageFlash;
	private TimerGroup _actorTimerGroup;

	public Animator Animator { get; private set; }

	public bool InputEnabled { get; set; }

	public bool IsVisible { get; set; }

	public ITrackable TrackedTarget { get; set; }

	public Health Health => _health;

	public float MaxHealth { get; set; }

	public readonly InputBuffer InputBuffer = new InputBuffer();

	public Timer HitReaction { get; private set; }

	public Timer JumpAllowance { get; private set; }
	
	public override void Awake()
	{
		base.Awake();
		Animator = GetComponent<Animator>();
		_health = GetComponent<Health>();
		_health.RegisterCallbacks(this);

		InputEnabled = true;

		_renderers = GetComponentsInChildren<Renderer>();

		_actorTimerGroup = new TimerGroup();
		_actorTimerGroup.AddRange( new Timer[]
		{
			HitReaction = new Timer(0f, () => InputEnabled = false, () => InputEnabled = true,true),
			JumpAllowance = new Timer()
		});

		SetPaused += SetAnimatorPaused;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		SetPaused -= SetAnimatorPaused;
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

	public override void OnTick(float deltaTime)
	{
		base.OnTick(deltaTime);
		if (controller) controller.Tick(this, deltaTime);
		InputBuffer.Tick(deltaTime);
		_actorTimerGroup.Tick(deltaTime);
		IsVisible = _renderers.Any(r => r != null && r.isVisible);
	}
	
	public override void OnFixedTick(float deltaTime)
	{
		base.OnFixedTick(deltaTime);
		if (InputEnabled) ConsumeInput?.Invoke(InputBuffer);
	}

	private void SetAnimatorPaused(bool value)
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
	
	public Vector3 GetTrackedTargetDirection() => (TrackedTarget.GetEyesPosition() - GetEyesPosition()).WithY(0f).normalized;

	public void Die() => this.WaitForEndOfFrameThen(() => Destroy(gameObject));

	//public bool TryConsumeAction(int actionId) => InputBuffer.TryConsumeAction(actionId);

	public override void ApplyHit(Entity instigator, Vector3 point, Vector3 direction, AttackData attackData)
	{
		// Apply knockback.
		base.ApplyHit(instigator, point, direction, attackData);

		// Apply damage.
		_health.TakeDamage((int)attackData.damage);

		// Do damage flash.
		this.OverrideCoroutine(ref _damageFlash, DoDamageFlash(0.2f));

		// TODO: Get reaction type from AttackData 
		var duration = Mathf.Max(HitReaction.Duration - HitReaction.Current, attackData.stun);
		HitReaction.Reset(duration);
		
		GetHit?.Invoke();
	}

	private void OnGetHit() => GetHit?.Invoke();
}
