using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class CombatActor : Actor
{
	[Header("Melee")]
	public AttackData attackData;
	public bool isAttacking { get; set; }
	public bool activeHit { get; set; }
	public bool cancelOK { get; set; }
	public AttackDataSet attackDataSet = null;
	public Transform weaponTransform;
	public List<GameObject> hitObjects = new List<GameObject>();
	public WeaponCollision weaponCollision = new WeaponCollision();
	protected Timer stunned = new Timer();
	protected Timer jumpAllowance = new Timer();

	protected override void Awake()
	{
		base.Awake();
		actorTimerGroup.Add(stunned);
		actorTimerGroup.Add(jumpAllowance);

		
	}

	public void NewHit(AnimationEvent animEvent)
	{
		AttackData data = attackDataSet.attacks.Find(d => d.name == animEvent.stringParameter);

		if(data != null)
		{
			activeHit = true;
			attackData = data;
			hitObjects = new List<GameObject>();
		}

		Vector3 origin = weaponTransform.position;
		Vector3 end = origin + weaponTransform.forward * 1.2f;

		// TODO: Update all weaponCollisions in a "weapon collision set"
		weaponCollision.SetInitialPosition(origin, end);
		weaponCollision.pointBuffer.Clear();
	}

	public void EndHit()
	{
		activeHit = false;
		weaponCollision.pointBuffer.Clear();
	}

	public void CancelOK()
	{
		cancelOK = true;
	}

	protected override void OnGetHit(Vector3 hitPoint, Vector3 direction, AttackData data)
	{
		base.OnGetHit(hitPoint, direction, data);

		health = Mathf.Max(health - data.damage, 0f);
		//Debug.Log("Hit " + name + " - HP: " + health + "/" + maxHealth);

		// TODO: Get reaction type from AttackData 
		var stunTime = Mathf.Max(stunned.Duration - stunned.Current, data.stun);
		stunned.Reset();
		stunned.SetDuration(stunTime);
	}

	public void CheckHit(Vector3 origin, Vector3 end)
	{
		RaycastHit[] hits = Physics.RaycastAll(
			origin,
			(end - origin).normalized,
			(end - origin).magnitude);

		foreach(RaycastHit hit in hits)
		{
			GameObject go = hit.collider.gameObject;
			Entity entity = go.GetComponent<Entity>();

			if(hitObjects.Contains(go) || entity == this) { continue; }
			
			if(entity != null)
			{
				Vector3 hitDirection = (go.transform.position - transform.position).normalized;
				entity.GetHit(hit.point, hitDirection, attackData);
				GameManager.HitPauseTimer = Time.fixedDeltaTime * attackData.hitPause;
			}
			
			if(GameManager.GetHitSpark(entity, out GameObject hitspark))
			{
				Instantiate(hitspark, hit.point, Quaternion.identity);
			}

			hitObjects.Add(go);
		}
	}
}