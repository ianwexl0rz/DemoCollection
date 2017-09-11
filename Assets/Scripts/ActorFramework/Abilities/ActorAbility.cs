using UnityEngine;
using System.Collections;

//[CreateAssetMenu(fileName = "Actor Ability", menuName = "Actor/Ability/Base Ability")]
public class ActorAbility : MonoBehaviour
{
	protected Actor actor;

	private void OnEnable()
	{
		actor = GetComponent<Actor>();
		actor.UpdateAbilities += UpdateAbility;
		actor.FixedUpdateAbilities += FixedUpdateAbility;
		actor.OnResetAbilities += Reset;
		actor.abilities.Add(this);
	}

	protected virtual void UpdateAbility()
	{
	}

	protected virtual void FixedUpdateAbility()
	{
	}

	private void OnDisable()
	{
		actor.UpdateAbilities -= UpdateAbility;
		actor.FixedUpdateAbilities -= FixedUpdateAbility;
		actor.OnResetAbilities -= Reset;
		actor.abilities.Remove(this);
	}

	private void Reset()
	{
	}
}
