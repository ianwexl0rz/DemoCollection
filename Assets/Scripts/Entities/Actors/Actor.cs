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

	public float hitPauseTimer = 0f;

	public override void OnUpdate()
	{
		base.OnUpdate();
		ProcessInput();
		ProcessAnimation();
		if(hitPauseTimer > 0f)
		{
			hitPauseTimer -= Time.deltaTime;
			hitPauseTimer = Mathf.Max(0f, hitPauseTimer);
		}
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

	public void GetHit(Actor attacker, AttackData data)
	{
		this.OverrideCoroutine(ref getHit, Hit(attacker, data));
		
	}

	/* ----- COMBAT STUFF ------ */

	private IEnumerator Hit(Actor attacker, AttackData data)
	{
		// Reduce health
		health = Mathf.Max(health - data.damage, 0f);

		//Debug.Log("Hit " + name + " - HP: " + health + "/" + maxHealth);

		// Set stun time, if greater than current stun time
		stunTime = Mathf.Max(stunTime, data.stun);

		// Apply knockback
		rb.velocity = (transform.position - attacker.transform.position).normalized * data.knockback;

		GameManager.I.hitPauseTimer = Time.fixedDeltaTime * 8f;

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

	private IEnumerator HitPause(float duration, Actor attacker)
	{
		/*/
		Time.timeScale = 0f;
		yield return new WaitForSecondsRealtime(duration);

		Time.timeScale = 1f;
		//*/

		//*/
		GameManager.I.queuePauseGame = true;
		yield return new WaitForSeconds(duration);

		GameManager.I.queuePauseGame = true;
		//*/

		/*/
		float time = 0f;
		while(time < duration)
		{
			//float t = Mathf.Cos(time / duration * 2 * Mathf.PI) * 0.5f + 0.5f;
			float t = time / duration;

			Time.timeScale = t * t * t * t;
			yield return null;

			time += Time.unscaledDeltaTime;
		}
		Time.timeScale = 1f;
		//*/

		/*/
		if(!physicsPaused) PausePhysics(true);
		if(!attacker.physicsPaused) attacker.PausePhysics(true);

		yield return new WaitForSecondsRealtime(duration);

		PausePhysics(false);
		attacker.PausePhysics(false);
		//*/

		/*/
		while(time < duration)
		{


			time += Time.deltaTime;
			transform.position = savedPosition + new Vector3(UnityEngine.Random.value,
				UnityEngine.Random.value,
				UnityEngine.Random.value).normalized * 0.05f;
			yield return null;
		}
		transform.position = savedPosition;
		//*/
	}


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
