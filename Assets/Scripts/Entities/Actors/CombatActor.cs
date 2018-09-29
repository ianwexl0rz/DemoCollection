using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponCollision
{
	public Vector3 origin, end, lastOrigin, lastEnd = Vector3.zero;

	public void SetInitialPosition(Vector3 p0, Vector3 p1)
	{
		lastOrigin = p0;
		lastEnd = p1;
	}

	public void SetCurrentPosition(Vector3 p0, Vector3 p1)
	{
		origin = p0;
		end = p1;
	}

	public void CheckHits(CombatActor attacker, int steps)
	{
		attacker.CheckHit(origin, end);
		Debug.DrawLine(origin, end, Color.red, Time.fixedDeltaTime * 8);

		for (int i = steps; i-- > 0;)
		{
			float t = (i + 1f) / (steps + 1f);

			Vector3 p0 = Vector3.Lerp(lastOrigin, origin, t);
			Vector3 p1 = Vector3.Lerp(lastEnd, end, t);

			attacker.CheckHit(p0, p1);
			Debug.DrawLine(p0, p1, Color.red, Time.fixedDeltaTime * 8);
		}

		attacker.CheckHit(lastOrigin, origin);
		Debug.DrawLine(lastOrigin, origin, Color.red, Time.fixedDeltaTime * 8);
	}
}

public class CombatActor : Actor
{
	[Header("Melee")]
	public AttackData attackData;
	public bool isAttacking { get; set; }
	public bool activeHit { get; set; }
	public bool cancelOK { get; set; }
	public AttackDataSet attackDataSet = null;
	public Transform weaponTransform;
	public List<Entity> hitEntities = new List<Entity>();

	public WeaponCollision weaponCollision = new WeaponCollision();

	public void NewHit(AnimationEvent animEvent)
	{
		AttackData data = attackDataSet.attacks.Find(d => d.name == animEvent.stringParameter);

		if(data != null)
		{
			activeHit = true;
			attackData = data;
			hitEntities = new List<Entity>();
		}
	}

	public void EndHit()
	{
		activeHit = false;
	}

		public void CancelOK()
	{
		cancelOK = true;
	}

	protected override void OnGetHit(Vector3 direction, AttackData data)
	{
		health = Mathf.Max(health - data.damage, 0f);
		//Debug.Log("Hit " + name + " - HP: " + health + "/" + maxHealth);

		// TODO: Get reaction type from AttackData 
		hitReaction = Stunned(data.stun);
	}

	public void CheckHit(Vector3 origin, Vector3 end)
	{
		RaycastHit[] hits = Physics.RaycastAll(
			origin,
			(end - origin).normalized,
			(end - origin).magnitude,
			LayerMask.GetMask("Actor", "PhysicsObject"));

		foreach(RaycastHit hit in hits)
		{
			Collider hitCollider = hit.collider;

			Entity entity = hitCollider.GetComponent<Entity>();
			if(entity == null || entity == this) { continue; }

			if(!hitEntities.Contains(entity))
			{
				Vector3 hitDirection = (entity.transform.position - transform.position).normalized;
				entity.GetHit(hit.point, hitDirection, attackData);
				hitEntities.Add(entity);

				GameManager.HitPauseTimer = Time.fixedDeltaTime * attackData.hitPause;
			}
		}
	}
}