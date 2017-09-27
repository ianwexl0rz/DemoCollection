using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

public class Actor : Entity
{
	[SerializeField]
	protected ActorBrain _brain = null;

	public bool isAwake = false;

	public PhysicMaterial activeMaterial = null;
	public PhysicMaterial stunnedMaterial = null;

	public Vector3 move { get; set; }
	public Vector3 look { get; set; }
	public bool lockOn { get; set; }

	public Transform lockOnTarget = null;
	public Animator animator { get; private set; }
	public ActorBrain brain { get { return _brain; } private set { _brain = value; } }

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

	protected override void OnEnable()
	{
		base.OnEnable();
		OnUpdate += GetInput;
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		OnUpdate -= GetInput;
	}

	private void Start()
	{
		look = transform.forward;

		if(brain != null)
		{
			brain.Init(this);
		}
	}

	private void GetInput()
	{
		if(isAwake && brain != null)
		{
			brain.Process(this);
		}
		else
		{
			move = Vector3.zero;
			ResetAbilities();
		}
	}

	public void SetBrain(ActorBrain newBrain)
	{
		if(brain != null)
		{
			brain.Clean(this);
		}

		brain = newBrain;
		brain.Init(this);
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

	private IEnumerator Hit(Vector3 attackerPos, AttackData data)
	{
		// Reduce health
		health = Mathf.Max(health - data.damage, 0f);

		Debug.Log("Hit " + name + " - HP: " + health + "/" + maxHealth);

		// Set stun time, if greater than current stun time
		stunTime = Mathf.Max(stunTime, data.stun);

		// Apply knockback
		rb.velocity = (transform.position - attackerPos).normalized * data.knockback;

		//StartCoroutine(SlowMo(0.025f, 0.05f));

		while(stunTime > 0f)
		{
			// If we got stunned, we want to apply a different physics material until it's over
			collider.material = stunnedMaterial;
			stunTime -= Time.deltaTime;
			yield return null;
		}

		collider.material = activeMaterial;
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
