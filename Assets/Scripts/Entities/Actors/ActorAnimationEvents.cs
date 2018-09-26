using UnityEngine;

public class ActorAnimationEvents : MonoBehaviour
{
	private Player player = null;

	private void Awake()
	{

		player = (transform.parent ?? transform).GetComponentInChildren<Player>();
	}

	public void CancelOK()
	{
		player.SetCancelOK();
	}

	public void NewHit(AnimationEvent animEvent)
	{
		AttackData data = player.attackDataSet.attacks.Find(d => d.name == animEvent.stringParameter);

		if(data != null)
		{
			//StartCoroutine(player.Attack(data));
			player.attackCoroutine = player.Attack(data);
		}
	}

	public void EndHit()
	{
		player.EndHit();
	}
}
