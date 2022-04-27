using UnityEngine;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(Actor))]
public class MeleeCombat : MonoBehaviour
{
	private static readonly int Attack = Animator.StringToHash("lightAttack");

	[SerializeField] private Transform weaponRoot = null;
	[SerializeField] private Vector3 forwardAxis = new Vector3(-1,0,0);
	[SerializeField] private MeleeWeapon weapon = null;
	[SerializeField] private float distThreshold = 0.1f;

	public bool isAttacking { get; set; }
	public bool cancelOK { get; set; }
	public bool ActiveHit { get; set; }

	public Transform WeaponRoot => weaponRoot;

	private AttackData attackData;
	private List<GameObject> hitObjects = new List<GameObject>();

	private Vector3 origin, end, lastOrigin, lastEnd, finalOrigin, finalEnd = Vector3.zero;
	private List<Vector3> pointBuffer = new List<Vector3>();
	private List<Color> colors = new List<Color>();

	private WeaponTrail weaponTrail = null;

	private Actor actor = null;

	private CharacterMotor _motor;

	private void Awake()
	{
		actor = GetComponent<Actor>();
		actor.OnHandleAbilityInput += HandleInput;
		actor.OnUpdateSubFrameAnimation += ProcessAttackAnimation;
		actor.OnReceiveHit += ReceiveHit;

		_motor = GetComponent<CharacterMotor>();

		if(weapon == null) return;

		InitWeapon();
	}

	private void HandleInput()
	{
		if (!isAttacking && actor.TryConsumeAction(PlayerAction.Attack))
		{
			isAttacking = true;
			actor.InputEnabled = false;

			// TODO: Should maybe set attack ID and generic attack trigger?
			if(actor.Animator != null) { actor.Animator.SetTrigger(Attack); }
		}
	}

	private void ProcessAttackAnimation(float progress)
	{
		if (ActiveHit)
		{
			var characterLastTRS = _motor.LastTRS;

			// Calculate the position and rotation the weapon WOULD have if the character did not move/rotate this frame.
			// This allows us to blend to the ACTUAL position/rotation over multiple steps.
			var lastWeaponPos = characterLastTRS.MultiplyPoint3x4(transform.InverseTransformPoint(WeaponRoot.position));
			var lastWeaponRot = characterLastTRS.rotation * Quaternion.Inverse(transform.rotation) * WeaponRoot.rotation;
			
			if (CheckHits(progress, lastWeaponPos, lastWeaponRot, out var combatEvents))
			{
				//TODO: If we hit more than one thing, trigger hits over sequential frames?
				MainMode.AddCombatEvents(combatEvents);
			}
		}
	}

	private void ReceiveHit()
	{
		isAttacking = false;
		cancelOK = true;
	}

	[ContextMenu("Refresh Weapon")]
	private void InitWeapon()
	{
		for(var i = weaponRoot.childCount; i-- > 0;)
		{
			DestroyImmediate(weaponRoot.GetChild(i).gameObject);
		}

		if(weapon.prefab != null)
		{
			var w = Instantiate(weapon.prefab, weaponRoot).transform;
			w.localScale = Vector3.one;

			if(weapon.forwardAxis != forwardAxis)
			{
				var localRot = Quaternion.FromToRotation(weapon.forwardAxis, forwardAxis);
				//w.localPosition = localRot * w.localPosition;
				w.localRotation = localRot;
			}
		}

#if UNITY_EDITOR
		if(!Application.isPlaying) { return; }
#endif

		if(weapon.showTrail)
		{
			if(weaponTrail == null)
			{
				var trailGo = new GameObject("WeaponTrail", typeof(WeaponTrail), typeof(MeshFilter), typeof(MeshRenderer));
				trailGo.transform.SetParent(transform, false);

				weaponTrail = trailGo.GetComponent<WeaponTrail>();
				weaponTrail.Init(weapon.trailMaterial);
			}
			else
			{
				weaponTrail.SetMaterial(weapon.trailMaterial);
			}
		}
		else if(weaponTrail != null)
		{
			Destroy(weaponTrail.gameObject);
			weaponTrail = null;
		}
	}

	public void NewHit(AnimationEvent animEvent)
	{
		// TODO: This should be a dictionary lookup instead of a find...
		AttackData data = weapon.attackDataSet.attacks.Find(d => d.name == animEvent.stringParameter);
		
		attackData = data;
		hitObjects = new List<GameObject>();

		ClearWeaponTrail();

		// TODO: Update all weaponCollisions in a "weapon collision set"
		ActiveHit = true;
	}

	public void EndHit()
	{
		ClearWeaponTrail();
		ActiveHit = false;
	}

	public void CancelOK()
	{
		 isAttacking = false;

		actor.InputEnabled = true;

		//cancelOK = true;
	}

	// TODO: CombatEvents should be passed in as an array and return the number of hits, so the function is non-allocating.
	public bool CheckHits(float completion, Vector3 lastWeaponPosition, Quaternion lastWeaponRotation, out List<CombatEvent> combatEvents)
	{
		combatEvents = new List<CombatEvent>();

		var debugTime = Time.fixedDeltaTime * 8;
		var pos = Vector3.Lerp(lastWeaponPosition, weaponRoot.position, completion);
		var rot = Quaternion.Slerp(lastWeaponRotation, weaponRoot.rotation, completion);

		origin = pos;
		end = origin + rot * forwardAxis * weapon.length;

		Vector3 currentVector = end - origin;
        Vector3 lastVector = lastEnd - lastOrigin;

        int steps = 1 + (int)((currentVector - lastVector).magnitude / distThreshold);

        float colorRange = ((float)steps).LinearRemap(1f, 5f, 0.5f, 0f);
        Color color = Color.HSVToRGB(Mathf.Clamp01(colorRange), 1, 1);

		Vector3[] addPoints = new Vector3[steps * 2];
		Color[] addColors = new Color[steps * 2];

		var success = false;
		int currentStep;
		//progress = 0;
		
		for (currentStep = 0; currentStep < steps; currentStep++)
        {
			var progress = (currentStep + 1f) / steps;

			Vector3 blendedOrigin = Vector3.Lerp(lastOrigin, origin, progress);
            Vector3 blendedEnd = blendedOrigin + Vector3.Slerp(lastVector, currentVector, progress);

			Debug.DrawLine(blendedOrigin, blendedEnd, color, debugTime);
	        Debug.DrawLine(currentStep == 0 ? lastEnd : addPoints[currentStep * 2 - 1], blendedEnd, color, debugTime);
	        Debug.DrawLine(currentStep == 0 ? lastOrigin : addPoints[currentStep * 2 - 2], blendedOrigin, color, debugTime);

			addPoints[currentStep * 2] = finalOrigin = blendedOrigin;
			addPoints[currentStep * 2 + 1] = finalEnd = blendedEnd;
			addColors[currentStep * 2] = addColors[currentStep * 2 + 1] = Color.white;

			success |= CheckHit(blendedOrigin, blendedEnd, ref combatEvents);
			success |= CheckHit(blendedEnd, blendedOrigin, ref combatEvents);

			//if (success) break;
		}

		if(pointBuffer.Count == 0)
		{

			Vector3[] lastPoints =
			{
				lastOrigin,
				lastEnd
			};

			pointBuffer.AddRange(lastPoints);
			Debug.DrawLine(lastOrigin, lastEnd, color, debugTime);

			Color[] lastColors = { color, color };
			colors.AddRange(lastColors);
		}

		// Only necessary if the hit check loop can be broken out of early...
		// Requires "currentStep" to be declared in the function scope 
		if (currentStep < steps - 1)
		{
			var newLength = (currentStep + 1) * 2;

			Vector3[] validPoints = new Vector3[newLength];
			Color[] validColors = new Color[newLength];

			Array.Copy(addPoints, validPoints, newLength);
			Array.Copy(addColors, validColors, newLength);

			pointBuffer.AddRange(validPoints);
			colors.AddRange(validColors);
		}
		else
		{
			pointBuffer.AddRange(addPoints);
			colors.AddRange(addColors);
		}

		// Fade colors over time (probably don't want to fade whole chunks like this)
		//for(var i = 0; i < colors.Count; i++)
		//{
		//	var oldColor = colors[i];
		//	colors[i] *= 0.5f;
		//}

		//pointBuffer.AddRange(addPoints);
		//colors.AddRange(addColors);

		var localPoints = new List<Vector3>(pointBuffer);
		for(var j = localPoints.Count; j-- > 0;)
		{
			localPoints[j] = transform.InverseTransformPoint(localPoints[j]);
		}
		
		lastOrigin = finalOrigin;
		lastEnd = finalEnd;

		if(weapon.showTrail)
		{
			weaponTrail.UpdateAndShowMesh(localPoints, colors);
		}

		return success;
	}

	private bool CheckHit(Vector3 origin, Vector3 end, ref List<CombatEvent> newCombatEvents)
	{
		var hits = Physics.RaycastAll(
			origin,
			(end - origin).normalized,
			(end - origin).magnitude,
			LayerMask.GetMask("Actor", "PhysicsObject"));

		var success = false;

		foreach(var hit in hits)
		{
			var go = hit.collider.gameObject;

			// Hit self
			if(go.transform.root == transform) { continue; }

			var entity = go.GetComponentInChildren<Entity>() ?? go.GetComponentInParent<Entity>();

			// Get GO of the entity because we may have hit a child GO collider
			if(entity != null) go = entity.gameObject;

			if(hitObjects.Contains(go)) { continue; }

			if(entity != null)
			{
				var hitDirection = (hit.point - transform.position).WithY(0f).normalized;
//				var hitDirection = (go.transform.position - transform.position).WithY(0f).normalized;
				var combatEvent = new CombatEvent(actor, entity, hit.point, hitDirection, attackData);
				
				newCombatEvents.Add(combatEvent);
				success = true;
			}

			hitObjects.Add(go);
		}
		
		return success;
	}

	private void ClearWeaponTrail()
	{
		lastOrigin = weaponRoot.position;
		lastEnd = lastOrigin + weaponRoot.rotation * forwardAxis * weapon.length;

		pointBuffer.Clear();
		colors.Clear();

		if(weapon.showTrail)
		{
			weaponTrail.HideMesh();
		}
	}
}
