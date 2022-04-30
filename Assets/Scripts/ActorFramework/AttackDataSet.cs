using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public struct CombatEvent
{
	public Entity instigator;
	public Entity target;
	public Vector3 point;
	public Vector3 direction;
	public AttackData attackData;

	public CombatEvent(Entity instigator, Entity target, Vector3 point, Vector3 direction, AttackData attackData)
	{
		this.instigator = instigator;
		this.target = target;
		this.point = point;
		this.direction = direction;
		this.attackData = attackData;
	}
	
	public void Deconstruct(out Entity instigator, out Entity target, out Vector3 point, out Vector3 direction, out AttackData attackData)
	{
		instigator = this.instigator;
		target = this.target;
		point = this.point;
		direction = this.direction;
		attackData = this.attackData;
	}
}

[Serializable]
public struct AttackData
{
	public string name;
	public float damage;
	public int hitPause;
	public float knockback;
	public float stun;
}

public enum AttackType
{
	None,
	Light,
	Heavy
}

[Serializable]
public class AttackTimer
{
	public float time = 0f;
	public AttackType attackType = AttackType.Light;

	public AttackTimer(AttackType attackType, float time)
	{
		this.time = time;
		this.attackType = attackType;
	}
}

[CreateAssetMenu]
public class AttackDataSet : ScriptableObject
{
	[SerializeField] private List<AttackData> _attacks;

	public AttackData GetAttackData(string attackName) => _attacks.Find(d => d.name == attackName);
}
