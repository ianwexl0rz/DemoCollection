using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class ActorAnimationEvents : MonoBehaviour
{
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
		AttackData data = player.attackDataSet.attacks.Find(d => d.clip == animEvent.animatorClipInfo.clip);

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
