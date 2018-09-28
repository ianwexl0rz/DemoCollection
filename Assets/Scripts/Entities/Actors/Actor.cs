using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

public class Actor : Entity
{
	[SerializeField]
	protected ActorController _controller = null;

	public bool isAwake = false;

	public Vector3 move { get; set; }
	public float look { get; set; }
	public bool lockOn { get; set; }

	public Transform lockOnTarget = null;
	public Animator animator { get; private set; }
	public ActorController controller { get { return _controller; } private set { _controller = value; } }

	public Action<Actor> UpdateController = delegate (Actor actor) { };
	public Action OnResetAbilities = null;
	public Action UpdateAbilities = null;
	public Action FixedUpdateAbilities = null;

	[HideInInspector]
	public List<ActorAbility> abilities = new List<ActorAbility>();

	public float health { get; set; }
	public float maxHealth { get; protected set; }
	protected float stunTime = 0f;
	protected float localPauseTimer = 0f;
	public IEnumerator hitReaction;

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
		if(localPauseTimer > 0f)
		{
			localPauseTimer -= Time.deltaTime;
			localPauseTimer = Mathf.Max(0f, localPauseTimer);
		}
	}

	public override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		hitReaction?.MoveNext();
		ProcessPhysics();
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

	protected virtual void ProcessPhysics()
	{
	}

	public void SetController(ActorController newController)
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

	protected IEnumerator Stunned(float newStunTime)
	{
		// Set stun time, if greater than current stun time
		stunTime = Mathf.Max(stunTime, newStunTime);

		while(stunTime > 0f)
		{
			yield return null;
			stunTime -= Time.fixedDeltaTime;
		}
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
