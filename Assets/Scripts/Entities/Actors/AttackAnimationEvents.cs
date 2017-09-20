using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

[Serializable]
public class AttackData
{
	public string name = "New Attack";
	public AnimationClip clip = null;
	public float damage = 0f;
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

public class AttackAnimationEvents : MonoBehaviour
{
	public List<AttackData> actions = new List<AttackData>();
	private Player player = null;

	private void Awake()
	{
		player = transform.parent.GetComponent<Player>();
	}

	public void CancelOK()
	{
		player.SetCancelOK();
	}

	public void NewHit(AnimationEvent animEvent)
	{
		AttackData data = actions.Find(d => d.clip == animEvent.animatorClipInfo.clip);

		if(data != null)
		{
			StartCoroutine(player.Attack(data));
		}
	}

	public void EndHit()
	{
		player.EndHit();
	}
}
