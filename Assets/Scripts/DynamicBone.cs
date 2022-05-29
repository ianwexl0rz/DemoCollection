using ActorFramework;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DynamicBone : EntityPhysics
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

	private ConfigurableJoint _joint;
	private CapsuleCollider _capsule;
	private Quaternion _cacheRotation;
	private Vector3 _torqueIntegral;
	private Vector3 _torqueError;
	private EntityPhysics _parentPhysicsMover;

	private void Start()
    {
	    parentEntity.AddSubEntity(Entity);
	    _parentPhysicsMover = parentEntity.GetComponent<EntityPhysics>();
	    CreateJoint();
	    CreateCollider();
	    Rigidbody.maxAngularVelocity = 20f;
	    _cacheRotation = referenceBone.rotation;
	    Entity.LateTick += SyncReferenceBone;
	    Entity.FixedTick += Simulate;
    }

	private void OnDestroy()
	{
		parentEntity.RemoveSubEntity(Entity);
		Entity.LateTick -= SyncReferenceBone;
		Entity.FixedTick -= Simulate;
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
		else if (_capsule != null)
		{
			_capsule.radius = radius;
			if (referenceBone.transform.childCount > 0)
				_capsule.height = height + radius * 2;
		}
	}
#endif

	private void CreateCollider()
	{
		_capsule = gameObject.AddComponent<CapsuleCollider>();
		_capsule.direction = direction;
		_capsule.center = center;
		_capsule.radius = radius;
		_capsule.height = height + radius * 2;
	}

	private void CreateJoint()
	{
		_joint = gameObject.AddComponent<ConfigurableJoint>();
		_joint.connectedBody = _parentPhysicsMover.Rigidbody;
		_joint.autoConfigureConnectedAnchor = false;
		_joint.xMotion = ConfigurableJointMotion.Locked;
		_joint.yMotion = ConfigurableJointMotion.Locked;
		_joint.zMotion = ConfigurableJointMotion.Locked;
		_joint.anchor = Vector3.zero;
	}

	private Vector3 GetDirectionFromInt(int dir)
	{
		return dir == 0 ? Vector3.right :
			dir == 1 ? Vector3.up :
			Vector3.forward;
	}

	private void Simulate(float deltaTime)
	{
		var targetRotation = isRoot ? _cacheRotation : _parentPhysicsMover.Rigidbody.rotation * _cacheRotation;
		var axis = GetDirectionFromInt(_capsule.direction);

		_joint.connectedAnchor = isRoot ? parentEntity.transform.InverseTransformPoint(referenceBone.position) : referenceBone.localPosition;
		Rigidbody.centerOfMass = _capsule.center - axis * (_capsule.height * (centerOfMass - 0.5f));

		Rigidbody.AddForce(windDirection * windInfluence, ForceMode.Acceleration);
		var targetTorque = Rigidbody.rotation.TorqueTo(targetRotation, deltaTime);
		var torque = torquePID.Output(Rigidbody.angularVelocity, targetTorque, ref _torqueIntegral, ref _torqueError, deltaTime);
		Rigidbody.AddTorque(Vector3.Scale(torque, stiffnessPerAxis), ForceMode.Acceleration);
	}

	private void SyncReferenceBone(float deltaTime)
	{
		_cacheRotation = isRoot ? referenceBone.rotation : referenceBone.localRotation;
		if (syncReferenceBone) referenceBone.rotation = transform.rotation;
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
}
