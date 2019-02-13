using UnityEngine;

[CreateAssetMenu(menuName = "Weapon/Melee Weapon", fileName = "New Melee Weapon")]
public class MeleeWeapon : ScriptableObject
{
	public GameObject prefab = null;
	public Vector3 forwardAxis = Vector3.up;
	public float length = 1f;
	public Material trailMaterial = null;
	public bool showTrail = true;
	public AttackDataSet attackDataSet = null;
}
