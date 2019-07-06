using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public class HitData
{
	public Vector3 point;
	public Vector3 direction;
	public AttackData attackData;

	public HitData(Vector3 point, Vector3 direction, AttackData attackData)
	{
		this.point = point;
		this.direction = direction;
		this.attackData = attackData;
	}
}

[Serializable]
public class AttackData
{
	public string name = "New Attack";
	public float damage = 0f;
	public int hitPause = 8;
	public float knockback = 0f;
	public float stun = 0f;
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
	[SerializeField]
	private List<AttackData> _attacks = new List<AttackData>();
	public List<AttackData> attacks { get { return _attacks; } private set { _attacks = value; } }
}
