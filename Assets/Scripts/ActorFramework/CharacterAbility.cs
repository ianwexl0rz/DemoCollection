using UnityEngine;
using System.Collections;

//[CreateAssetMenu(fileName = "Actor Ability", menuName = "Actor/Ability/Base Ability")]
public class CharacterAbility : MonoBehaviour
{
	protected Character character;

	private void OnEnable()
	{
		character = GetComponent<Character>();
		character.UpdateAbilities += UpdateAbility;
		character.FixedUpdateAbilities += FixedUpdateAbility;
		character.OnResetAbilities += Reset;
		character.abilities.Add(this);
	}

	protected virtual void UpdateAbility()
	{
	}

	protected virtual void FixedUpdateAbility()
	{
	}

	private void OnDisable()
	{
		character.UpdateAbilities -= UpdateAbility;
		character.FixedUpdateAbilities -= FixedUpdateAbility;
		character.OnResetAbilities -= Reset;
		character.abilities.Remove(this);
	}

	private void Reset()
	{
	}
}
