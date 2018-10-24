using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

public class Character : Actor
{
	[SerializeField]
	protected CharacterController controller;
	protected Animator animator;

	public bool isAwake = false;

	public Vector3 move { get; set; }
	public float look { get; set; }
	public bool lockOn { get; set; }

	public Transform lockOnTarget = null;

	public Action<Character> UpdateController = delegate { };
	public Action OnResetAbilities = null;
	public Action UpdateAbilities = null;
	public Action FixedUpdateAbilities = null;

	[HideInInspector]
	public List<CharacterAbility> abilities = new List<CharacterAbility>();

	public float health { get; set; }
	public float maxHealth { get; protected set; }
	public IEnumerator hitReaction;
	protected readonly TimerGroup actorTimerGroup = new TimerGroup();

	// Use this for initialization
	protected override void Awake()
	{
		base.Awake();
		animator = GetComponent<Animator>();
		health = maxHealth = 100f;
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
		ProcessInput();
		ProcessAnimation();
		actorTimerGroup.Tick(Time.deltaTime);
	}

	public override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		hitReaction?.MoveNext();
		//ProcessPhysics();
	}

	protected override void OnPauseEntity(bool value)
	{
		if(animator != null)
		{
			animator.SetPaused(value);
		}
	}

	protected virtual void ProcessInput()
	{
		if(isAwake)
		{
			UpdateController(this);
		}
	}

	protected virtual void ProcessAnimation()
	{
	}

	public virtual void ProcessPhysics()
	{
	}

	public CharacterController GetController()
	{
		return controller;
	}

	public void SetController(CharacterController newController)
	{
		newController.Engage(this);
		controller = newController;
	}

	private void ResetAbilities()
	{
		if(OnResetAbilities != null)
		{
			OnResetAbilities();
		}
	}

	protected override void OnGetHit(Vector3 hitPoint, Vector3 direction, AttackData data)
	{
		OnEarlyFixedUpdate = () =>
		{
			rb.AddForce(direction * data.knockback / Time.fixedDeltaTime, ForceMode.Acceleration);
			rb.AddForceAtPosition(direction * data.knockback * 0.25f / Time.fixedDeltaTime, rb.position.WithY(hitPoint.y), ForceMode.Acceleration);
		};
	}

	/*//
	private IEnumerator SlowMo(float duration, float recovery)
	{
		yield return null;

		float time = 0f;

		while(time < duration)
		{
			Time.timeScale = 0.05f;
			time += Time.unscaledDeltaTime;
			yield return null;
		}

		time = 0f;
		while(time < recovery)
		{
			Time.timeScale = Mathf.Lerp(0.02f, 1f, time / recovery);
			time += Time.unscaledDeltaTime;
			yield return null;
		}

		Time.timeScale = 1f;
	}
	//*/
}
