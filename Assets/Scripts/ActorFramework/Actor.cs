using UnityEngine;
using System;
using System.Collections;
using ActorFramework;

public class Actor : Entity, IDamageable
{
	private static readonly int DamageFlash = Shader.PropertyToID("_DamageFlash");

	public event Action<InputBuffer> ConsumeInput;
	public event Action OnDeath;

	[SerializeField] private ActorController controller;
	[SerializeField] private Transform rig;
	[SerializeField] private Ragdoll ragdollPrefab;

	private Renderer[] _renderers;
	private Coroutine _damageFlash;
	private TimerGroup _inputTimerGroup;
	private Ragdoll _ragdoll;

	public Animator Animator { get; private set; }

	public bool InputEnabled { get; set; }

	public Health Health { get; private set; }
	public Stamina Stamina { get; private set; }
	public bool IsAlive() => Health.Current > 0;

	public readonly InputBuffer InputBuffer = new InputBuffer();
	public Timer HitReaction { get; private set; }

	public Timer JumpAllowance { get; private set; }

	public ActorController Controller
	{
		get => controller;
		private set => controller = value;
	}

	public override void Awake()
	{
		base.Awake();
		Animator = GetComponent<Animator>();
		Health = GetComponent<Health>();
		Stamina = GetComponent<Stamina>();

		_renderers = GetComponentsInChildren<Renderer>();

		_inputTimerGroup = new TimerGroup();
		_inputTimerGroup.AddRange( new Timer[]
		{
			JumpAllowance = new Timer()
		});

		HitReaction = new Timer(0f, () => InputEnabled = false, () => InputEnabled = true, true);
		InputEnabled = true;

		Health.Depleted += Die;
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
		_inputTimerGroup.Tick(deltaTime);
		if (Controller) Controller.Tick(this, deltaTime);
		InputBuffer.Tick(deltaTime);
	}

	public override void OnLateTick(float deltaTime)
	{
		base.OnLateTick(deltaTime);
		if (Controller) Controller.LateTick(this, deltaTime);
	}
	
	public override void OnFixedTick(float deltaTime)
	{
		base.OnFixedTick(deltaTime);
		HitReaction.Tick(deltaTime);

		if (!IsAlive())
		{
			if (_deathCoroutine != null && !_deathCoroutine.MoveNext())
			{
				_deathCoroutine = null;
			}

			if (_ragdoll)
			{
				var rb = GetComponent<Rigidbody>();
				var ragdollRb = _ragdoll.GetComponentInChildren<Rigidbody>();
				var capsuleCollider = GetComponent<CapsuleCollider>();
				if (rb && ragdollRb && capsuleCollider)
				{
					rb.MovePosition(ragdollRb.position - Vector3.up * (capsuleCollider.height * 0.5f));
				}
			}
			return;
		}
		if (InputEnabled) ConsumeInput?.Invoke(InputBuffer);
	}

	private void SetAnimatorPaused(bool paused)
	{
		if(Animator) Animator.enabled = !paused;
	}
	
	public void SetController(ActorController newController, object context = null)
	{
		if (Controller != null)
			Controller.Release(this);

		if (newController != null)
			newController.Possess(this, context);
		
		Controller = newController;
	}

	public Vector3 DirectionToTrackable(Trackable trackable) => (trackable.GetEyesPosition() - Trackable.GetEyesPosition()).WithY(0f).normalized;

	void InitRagdoll()
	{
		var rb = GetComponent<Rigidbody>();
		if (rb && ragdollPrefab && rig)
		{
			_ragdoll = Instantiate(ragdollPrefab, transform.position, transform.rotation);
			_ragdoll.CopyTransforms(rig, rb, _ragdoll.targetRig);
			
			rb.isKinematic = true;
			rb.detectCollisions = false;
			rb.velocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
		}
		
		foreach (var r in _renderers)
		{
			r.enabled = false;
		}
	}
	
	IEnumerator DeathCoroutine()
	{
		// Wait a frame for physics to be applied
		yield return null;
		OnDeath?.Invoke();
		InitRagdoll();
	}
	
	private IEnumerator _deathCoroutine;
	
	public void Die()
	{
		_deathCoroutine = DeathCoroutine();
	}

	private void HandleGetHit(CombatEvent combatEvent)
	{
		var attackData = combatEvent.AttackData;

		// Do damage flash.
		this.OverrideCoroutine(ref _damageFlash, DoDamageFlash(0.2f));

		// TODO: Get reaction type from AttackData 
		var duration = Mathf.Max(HitReaction.Duration - HitReaction.Current, attackData.stun);
		HitReaction.Reset(duration);
	}
}
