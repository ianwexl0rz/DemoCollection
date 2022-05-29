using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using ActorFramework;
using DemoCollection;

public class Actor : Entity, IDamageable, ITracker
{
	private static readonly int DamageFlash = Shader.PropertyToID("_DamageFlash");

	public event Action<InputBuffer> ConsumeInput;
	public event Action<PlayerController> PossessedByPlayer;
	public event Action<PlayerController> ReleasedByPlayer;

	[SerializeField] private ActorController controller;

	private Renderer[] _renderers;
	private Coroutine _damageFlash;
	private TimerGroup _actorTimerGroup;
	
	//public Trackable Trackable { get; private set; }

	public Animator Animator { get; private set; }

	public bool InputEnabled { get; set; }
	
	[field: SerializeField] public float TrackingRange { get; set; } = 10f;

	[SerializeField] private Trackable _trackedTarget;
	public Trackable TrackedTarget
	{
		get => _trackedTarget;
		set
		{
			if (_trackedTarget == value) return;
			_trackedTarget = value;
			LockOn.OnTargetChanged(_trackedTarget);
		}
	}

	public Health Health { get; private set; }

	public readonly InputBuffer InputBuffer = new InputBuffer();
	public Timer HitReaction { get; private set; }

	public Timer JumpAllowance { get; private set; }
	
	public override void Awake()
	{
		base.Awake();
		Animator = GetComponent<Animator>();
		Health = GetComponent<Health>();
		Health.Depleted += Die;

		_renderers = GetComponentsInChildren<Renderer>();
		//Trackable = GetComponent<Trackable>();

		_actorTimerGroup = new TimerGroup();
		_actorTimerGroup.AddRange( new Timer[]
		{
			HitReaction = new Timer(0f, () => InputEnabled = false, () => InputEnabled = true,true),
			JumpAllowance = new Timer()
		});
		
		InputEnabled = true;

		GetHit += HandleGetHit;
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

		// // TODO: Tracked objects should unset themselves on destroy.
		// if (TrackedTarget == null && !ReferenceEquals(TrackedTarget, null))
		// {
		// 	TrackedTarget = null;
		// }
		
		if (controller) controller.Tick(this, deltaTime);
		InputBuffer.Tick(deltaTime);
		_actorTimerGroup.Tick(deltaTime);
	}

	public override void OnLateTick(float deltaTime)
	{
		base.OnLateTick(deltaTime);
		if (controller) controller.LateTick(this, deltaTime);
	}
	
	public override void OnFixedTick(float deltaTime)
	{
		base.OnFixedTick(deltaTime);
		if (InputEnabled) ConsumeInput?.Invoke(InputBuffer);
	}

	private void SetAnimatorPaused(bool paused)
	{
		if(Animator) Animator.enabled = !paused;
	}
	
	public void SetController(ActorController newController, object context = null)
	{
		if (controller != null)
			controller.Release(this);

		newController.Possess(this, context);
		controller = newController;
	}

	public Vector3 GetTrackedTargetDirection() => (TrackedTarget.GetEyesPosition() - Trackable.GetEyesPosition()).WithY(0f).normalized;

	public void Die() => this.WaitForEndOfFrameThen(() => Destroy(gameObject));

	private void HandleGetHit(CombatEvent combatEvent)
	{
		var attackData = combatEvent.AttackData;

		// Do damage flash.
		this.OverrideCoroutine(ref _damageFlash, DoDamageFlash(0.2f));

		// TODO: Get reaction type from AttackData 
		var duration = Mathf.Max(HitReaction.Duration - HitReaction.Current, attackData.stun);
		HitReaction.Reset(duration);
	}
	
	private void ReleaseTarget() => TrackedTarget = null;

	public void OnPossessedByPlayer(PlayerController playerController) => PossessedByPlayer?.Invoke(playerController);

	public void OnReleasedByPlayer(PlayerController playerController) => ReleasedByPlayer?.Invoke(playerController);
}
