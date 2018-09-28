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
	
	public IEnumerator Attack(AttackData data)
	{
		List<Actor> hitEnemies = new List<Actor>();

		while(true)
		{
			RaycastHit[] hits = Physics.RaycastAll(
				weaponTransform.position,
				weaponTransform.forward,
				((CapsuleCollider)attackCollider).height,
				LayerMask.GetMask("Actor", "PhysicsObject"));

			foreach(RaycastHit hit in hits)
			{
				Collider enemyCollider = hit.collider;

				Actor enemy = enemyCollider.GetComponent<Actor>();
				if(enemy == null || enemy == this) { continue; }

				if(!hitEnemies.Contains(enemy))
				{
					if(weaponTransform)
					{
						// TODO: Spawn a different kind of spark depending on what we hit.
						Instantiate(GameManager.HitSpark, hit.point, Quaternion.identity, null);
					}

					enemy.GetHit(this, data);
					hitEnemies.Add(enemy);

					GameManager.HitPauseTimer = Time.fixedDeltaTime * data.hitPause;
				}
			}

			yield return null;
		}
	}
}