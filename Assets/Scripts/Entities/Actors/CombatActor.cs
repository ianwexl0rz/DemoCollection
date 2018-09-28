using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatActor : Actor
{
	[Header("Melee")]
	public IEnumerator activeHit;
	public bool isAttacking { get; set; }
	public bool cancelOK { get; set; }
	public AttackDataSet attackDataSet = null;
	public GameObject attackBox = null;
	public Transform weaponTransform;
	protected Collider attackCollider = null;

	public void NewHit(AnimationEvent animEvent)
	{
		AttackData data = attackDataSet.attacks.Find(d => d.name == animEvent.stringParameter);

		if(data != null)
		{
			attackBox.SetActive(true);
			activeHit = Attack(data);
		}
	}

	public void EndHit()
	{
		activeHit = null;
		attackBox.SetActive(false);
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
	
	public IEnumerator Attack(AttackData data)
	{
		List<Entity> hitEntities = new List<Entity>();

		while(true)
		{
			RaycastHit[] hits = Physics.RaycastAll(
				weaponTransform.position,
				weaponTransform.forward,
				((CapsuleCollider)attackCollider).height,
				LayerMask.GetMask("Actor", "PhysicsObject"));

			foreach(RaycastHit hit in hits)
			{
				Collider hitCollider = hit.collider;

				Entity entity = hitCollider.GetComponent<Entity>();
				if(entity == null || entity == this) { continue; }

				if(!hitEntities.Contains(entity))
				{
					Vector3 hitDirection = (entity.transform.position - transform.position).normalized;
					entity.GetHit(hit.point, hitDirection, data);
					hitEntities.Add(entity);

					GameManager.HitPauseTimer = Time.fixedDeltaTime * data.hitPause;
				}
			}

			yield return null;
		}
	}
}