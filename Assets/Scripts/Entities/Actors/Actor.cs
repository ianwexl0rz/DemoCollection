using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

public class Actor : Entity
{
	[SerializeField]
	protected ActorController _controller = null;

	public bool isAwake = false;

	public PhysicMaterial activeMaterial = null;
	public PhysicMaterial stunnedMaterial = null;

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

	private new Collider collider = null;

	public float health { get; set; }
	public float maxHealth { get; protected set; }
	protected float stunTime = 0f;
	protected Coroutine getHit = null;

	// Use this for initialization
	protected override void Awake()
	{
		base.Awake();
		animator = GetComponentInChildren<Animator>();
		collider = GetComponent<Collider>();
		collider.material = isAwake ? activeMaterial : stunnedMaterial;
		health = maxHealth = 100f;
	}

	private void Start()
	{
		//look = transform.forward;

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
	}

	public override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		ProcessPhysics();
	}

	protected override void OnPauseEntity(bool value)
	{
		PauseAnimation(value);
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

	private void PauseAnimation(bool paused)
	{
		if(animator != null) animator.speed = paused ? 0f : localTimeScale;
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

	public void GetHit(Vector3 attackerPos, AttackData data)
	{
		this.OverrideCoroutine(ref getHit, Hit(attackerPos, data));
		
	}

	/* ----- COMBAT STUFF ------ */

	private IEnumerator Hit(Vector3 attackerPos, AttackData data)
	{
		// Reduce health
		health = Mathf.Max(health - data.damage, 0f);

		Debug.Log("Hit " + name + " - HP: " + health + "/" + maxHealth);

		// Set stun time, if greater than current stun time
		stunTime = Mathf.Max(stunTime, data.stun);

		// Apply knockback
		rb.velocity = (transform.position - attackerPos).normalized * data.knockback;

		StartCoroutine(SlowMo(0.06f, 0.05f));

		while(stunTime > 0f)
		{
			// If we got stunned, we want to apply a different physics material until it's over
			collider.material = stunnedMaterial;
			stunTime -= Time.deltaTime;
			yield return null;
		}

		collider.material = activeMaterial;
	}

	/*
	private IEnumerator HitStun(float duration)
	{
		PauseEntity

			float time = 0f;

		while(time < duration)
		{
			Time.timeScale = 0.05f;
			time += Time.unscaledDeltaTime;
			yield return null;
		}
	}
	*/


	//*//
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
