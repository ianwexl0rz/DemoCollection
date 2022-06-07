using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public struct CombatEvent
{
	public Entity Instigator;
	public Entity Target;
	public Vector3 Point;
	public Vector3 Direction;
	public AttackData AttackData;

	public CombatEvent(Entity instigator, Entity target, Vector3 point, Vector3 direction, AttackData attackData)
	{
		Instigator = instigator;
		Target = target;
		Point = point;
		Direction = direction;
		AttackData = attackData;
	}
	
	public void Deconstruct(out Entity instigator, out Entity target, out Vector3 point, out Vector3 direction, out AttackData attackData)
	{
		instigator = Instigator;
		target = Target;
		point = Point;
		direction = Direction;
		attackData = AttackData;
	}
}

[Serializable]
public struct AttackData
{
	public string name;
	public int damage;
	public int staminaCost;
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
