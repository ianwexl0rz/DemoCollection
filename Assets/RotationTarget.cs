using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(ConfigurableJoint), typeof(CapsuleCollider))]
public class RotationTarget : MonoBehaviour
{
	[SerializeField] private Transform original = null;
	[SerializeField] private Rigidbody connectedRigidbody = null;
	[SerializeField] private Vector3 windDirection = Vector3.up;
	[SerializeField, Range(0, 1)] private float windInfluence = 0f;
	[SerializeField, Range(0, 1)] private float centerOfMass = 0.5f;
	[SerializeField] private Vector3 stiffnessPerAxis = Vector3.one;
	[SerializeField] private PIDConfig rotationPidConfig = null;
	[SerializeField] private PIDConfig anglePidConfig = null;
	private Rigidbody rb;
	private PID3 rotationPid;
	private PID3 anglePid;
	private ConfigurableJoint joint;
	private CapsuleCollider capsule;
	private Quaternion cacheRotation;
	private bool needsRefresh;

    void Awake()
    {
	    rb = GetComponent<Rigidbody>();
	    joint = GetComponent<ConfigurableJoint>();
	    capsule = GetComponent<CapsuleCollider>();
	    rotationPid = new PID3(rotationPidConfig);
	    anglePid = new PID3(anglePidConfig);
	    rb.maxAngularVelocity = 20f;

	    cacheRotation = original.rotation;
    }

	void FixedUpdate()
	{
		var dt = Time.fixedDeltaTime;

		original.rotation = cacheRotation;
		joint.connectedAnchor = original.position + connectedRigidbody.velocity * dt;

		var axis = capsule.direction == 0 ? Vector3.right :
			capsule.direction == 1 ? Vector3.up :
			Vector3.forward;

		rb.centerOfMass = capsule.center - axis * capsule.height * (centerOfMass - 0.5f);

		rb.AddForce(windDirection * windInfluence);
		rb.AddTorque(connectedRigidbody.angularVelocity * dt, ForceMode.Acceleration);
		rb.RotateTo(rotationPid, anglePid, original.rotation, dt, stiffnessPerAxis);

		needsRefresh = true;
	}

	void OnDrawGizmos()
	{
		if(rb == null) { return; }

		Gizmos.DrawSphere(transform.TransformPoint(rb.centerOfMass), 0.05f);
	}

	void LateUpdate()
	{
		if(!needsRefresh) { return; }

		cacheRotation = original.rotation;
		original.rotation = rb.transform.rotation;
	}
}
