using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DynamicBone : Entity
{
	[SerializeField] private bool isRoot = false;
	[SerializeField] private Entity parentEntity = null;
	[SerializeField] private Transform referenceBone = null;
	[SerializeField] private bool syncReferenceBone = false;

	[Header("Collider Settings")]
	[SerializeField] private float radius = 0.07f;
	[SerializeField] private float height = 0.2f;
	[SerializeField] private int direction = 0;
	[SerializeField] private bool flip = false;
	[SerializeField] private Vector3 center = Vector3.zero;

	[Header("Behavior Settings")]
	[SerializeField] private Vector3 windDirection = Vector3.up;
	[SerializeField, Range(0, 1)] private float windInfluence = 0f;
	[SerializeField, Range(0, 1)] private float centerOfMass = 0.5f;
	[SerializeField] private Vector3 stiffnessPerAxis = Vector3.one;
	[SerializeField] private PID3 torquePID = null;

	private ConfigurableJoint joint;
	private CapsuleCollider capsule;
	private Quaternion cacheRotation;
	private Vector3 torqueIntegral;
	private Vector3 torqueError;

	protected override void Start()
    {
	    parentEntity.AddSubEntity(this);
	    CreateJoint();
	    CreateCollider();
	    Rigidbody.maxAngularVelocity = 20f;
	    cacheRotation = referenceBone.rotation;
	    LateTick += SyncWithReferenceBone;
	    FixedTick += Simulate;
    }

	protected override void OnDestroy()
	{
		parentEntity.RemoveSubEntity(this);
		LateTick -= SyncWithReferenceBone;
		FixedTick -= Simulate;
	}

#if UNITY_EDITOR
	private void OnValidate()
	{
		if (!Application.isPlaying && referenceBone != null)
		{
			if (syncReferenceBone)
			{
				transform.position = referenceBone.position;
				transform.rotation = referenceBone.rotation;

				if (referenceBone.transform.childCount > 0)
				{
					var child = referenceBone.GetChild(0);

					var dir = 0;

					if (child.localPosition.y * child.localPosition.y > child.localPosition.x * child.localPosition.x)
						dir++;

					if (child.localPosition.z * child.localPosition.z > child.localPosition.y * child.localPosition.y)
						dir++;

					direction = dir;
					center = child.localPosition * 0.5f;
					height = child.localPosition.magnitude;
				}
				else
				{
					center = GetDirectionFromInt(direction) * height * (flip ? -0.5f : 0.5f);
				}
			}
		}
		else if (capsule != null)
		{
			capsule.radius = radius;
			if (referenceBone.transform.childCount > 0)
				capsule.height = height + radius * 2;
		}
	}
#endif

	private void CreateCollider()
	{
		capsule = gameObject.AddComponent<CapsuleCollider>();
		capsule.direction = direction;
		capsule.center = center;
		capsule.radius = radius;
		capsule.height = height + radius * 2;
	}

	private void CreateJoint()
	{
		joint = gameObject.AddComponent<ConfigurableJoint>();
		joint.connectedBody = parentEntity.Rigidbody;
		joint.autoConfigureConnectedAnchor = false;
		joint.xMotion = ConfigurableJointMotion.Locked;
		joint.yMotion = ConfigurableJointMotion.Locked;
		joint.zMotion = ConfigurableJointMotion.Locked;
		joint.anchor = Vector3.zero;
	}

	private Vector3 GetDirectionFromInt(int dir)
	{
		return dir == 0 ? Vector3.right :
			dir == 1 ? Vector3.up :
			Vector3.forward;
	}

	private void Simulate(float deltaTime)
	{
		var targetRotation = isRoot ? cacheRotation : parentEntity.Rigidbody.rotation * cacheRotation;
		var axis = GetDirectionFromInt(capsule.direction);

		joint.connectedAnchor = isRoot ? parentEntity.transform.InverseTransformPoint(referenceBone.position) : referenceBone.localPosition;
		Rigidbody.centerOfMass = capsule.center - axis * (capsule.height * (centerOfMass - 0.5f));

		Rigidbody.AddForce(windDirection * windInfluence, ForceMode.Acceleration);
		var targetTorque = Rigidbody.rotation.TorqueTo(targetRotation, deltaTime);
		var torque = torquePID.Output(Rigidbody.angularVelocity, targetTorque, ref torqueIntegral, ref torqueError, deltaTime);
		Rigidbody.AddTorque(Vector3.Scale(torque, stiffnessPerAxis), ForceMode.Acceleration);
	}
	
#if UNITY_EDITOR
	private void OnDrawGizmosSelected()
	{
		var pos = transform.TransformPoint(center);
		var rot = transform.rotation * Quaternion.FromToRotation(Vector3.up, GetDirectionFromInt(direction));

		Gizmos.color = Color.red;
		GizmosEx.DrawWireCapsule(pos, rot, radius, height + radius * 2);

		if (Rigidbody == null) { return; }

		Gizmos.color = Color.gray;
		Gizmos.DrawSphere(transform.TransformPoint(Rigidbody.centerOfMass), 0.05f);
	}
#endif

	private void SyncWithReferenceBone(float deltaTime)
	{
		cacheRotation = isRoot ? referenceBone.rotation : referenceBone.localRotation;
		if (syncReferenceBone) referenceBone.rotation = transform.rotation;
	}
}
