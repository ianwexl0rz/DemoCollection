using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Actor))]
public class MeleeCombat : MonoBehaviour
{
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

	private void Awake()
	{
		actor = GetComponent<Actor>();

		if(weapon == null) return;

		InitWeapon();
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
		//AttackData data = attackDataSet.attacks.Find(d => d.name == animEvent.stringParameter);
		AttackData data = weapon.attackDataSet.attacks.Find(d => d.name == animEvent.stringParameter);

		if(data != null)
		{
			attackData = data;
			hitObjects = new List<GameObject>();
		}

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

		if(actor is Character character)
		{
			character.InputEnabled = true;
		}

		//cancelOK = true;
	}

	public bool CheckHits(float completion, Vector3 lastWeaponPosition, Quaternion lastWeaponRotation)
	{
		//if(!owner.IsPaused) { return; }

        float debugTime = Time.fixedDeltaTime * 8;

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

		for(var i = 0; i < steps; i++)
        {
			var progress = (i + 1f) / steps;

			Vector3 blendedOrigin = Vector3.Lerp(lastOrigin, origin, progress);
            Vector3 blendedEnd = blendedOrigin + Vector3.Slerp(lastVector, currentVector, progress);

			Debug.DrawLine(blendedOrigin, blendedEnd, color, debugTime);
	        Debug.DrawLine(i == 0 ? lastEnd : addPoints[i * 2 - 1], blendedEnd, color, debugTime);
	        Debug.DrawLine(i == 0 ? lastOrigin : addPoints[i * 2 - 2], blendedOrigin, color, debugTime);

			addPoints[i * 2] = finalOrigin = blendedOrigin;
			addPoints[i * 2 + 1] = finalEnd = blendedEnd;
			addColors[i * 2] = addColors[i * 2 + 1] = Color.white;

			success = CheckHit(blendedOrigin, blendedEnd);
			success |= CheckHit(blendedEnd, blendedOrigin);

			if(success) break;
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
		// Requires "i" to be declared in the function scope 
		//if(i < steps - 1)
		//{
		//	var newLength = (i + 1) * 2;

		//	Vector3[] validPoints = new Vector3[newLength];
		//	Color[] validColors = new Color[newLength];

		//	Array.Copy(addPoints, validPoints, newLength);
		//	Array.Copy(addColors, validColors, newLength);

		//	pointBuffer.AddRange(validPoints);
		//	colors.AddRange(validColors);
		//}
		//else
		//{
		//	pointBuffer.AddRange(addPoints);
		//	colors.AddRange(addColors);
		//}

		// Fade colors over time (probably don't want to fade whole chunks like this)
		//for(var i = 0; i < colors.Count; i++)
		//{
		//	var oldColor = colors[i];
		//	colors[i] *= 0.5f;
		//}

		pointBuffer.AddRange(addPoints);
		colors.AddRange(addColors);

		var localPoints = new List<Vector3>(pointBuffer);
		for(var i = localPoints.Count; i-- > 0;)
		{
			localPoints[i] = transform.InverseTransformPoint(localPoints[i]);
		}
		
		lastOrigin = finalOrigin;
		lastEnd = finalEnd;

		if(weapon.showTrail)
		{
			weaponTrail.UpdateAndShowMesh(localPoints, colors);
		}

		return success;
	}

	public bool CheckHit(Vector3 origin, Vector3 end)
	{
		RaycastHit[] hits = Physics.RaycastAll(
			origin,
			(end - origin).normalized,
			(end - origin).magnitude,
			LayerMask.GetMask("Actor", "PhysicsObject"));

		var success = false;

		foreach(RaycastHit hit in hits)
		{
			GameObject go = hit.collider.gameObject;

			// Hit self
			if(go.transform.root == transform) { continue; }

			Entity entity = go.GetComponentInChildren<Entity>() ?? go.GetComponentInParent<Entity>();

			// Get GO of the entity because we may have hit a child GO collider
			if(entity != null) go = entity.gameObject;

			if(hitObjects.Contains(go)) { continue; }

			if(entity != null)
			{
				Vector3 hitDirection = (go.transform.position - transform.position).WithY(0f).normalized;
				entity.GetHit(hit.point, hitDirection, attackData);
				//GameManager.HitPauseTimer = Time.fixedDeltaTime * attackData.hitPause;

				GameManager.I.InitHitPause(Time.fixedDeltaTime * attackData.hitPause);

				//success = true;
			}

			if(GameManager.GetHitSpark(entity, out GameObject hitspark))
			{
				Instantiate(hitspark, hit.point, Quaternion.identity);
			}

			hitObjects.Add(go);
		}

		return success;
	}

	public void ClearWeaponTrail()
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
