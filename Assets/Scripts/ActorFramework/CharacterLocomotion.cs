﻿using System;
using System.Collections;
using KinematicCharacterController;
using UnityEditor;
using UnityEngine;

namespace ActorFramework
{
    public struct CharacterInputs
    {
        public Vector3 Move;
        public Vector3 Look;
        public bool Run;
        public bool BeginJump;
        public bool BeginRoll;
        public bool IsInHitStun;
    }
    
    public class CharacterLocomotion : MonoBehaviour, ICharacterController
    {
        public event Action<AnimatedMotorProperties> OnAnimatedPropertiesChanged;

        public KinematicCharacterMotor Motor;

        [Header("Stable Movement")]
        public float MaxStableMoveSpeed = 10f;
        public float StableMovementSharpness = 15;
        public float OrientationSharpness = 10;

        [Header("Air Movement")]
        public float MaxAirMoveSpeed = 10f;
        public float AirAccelerationSpeed = 5f;
        public float Drag = 0.1f;
        
        [Header("Jumping")]
        public bool AllowJumpingWhenSliding = false;
        public float JumpSpeed = 10f;
        public float JumpPreGroundingGraceTime = 0f;
        public float JumpPostGroundingGraceTime = 0f;

        [Header("Rolling")]
        public float RollSpeed = 540f;

        [Header("Misc")]
        public Vector3 Gravity = new Vector3(0, -30f, 0);
        public Transform MeshRoot;
        
        private Vector3 _moveInputVector;
        private Vector3 _lookInputVector;
        private bool _shouldRun;
        private bool _shouldBeginJump;
        private bool _shouldBeginAttack;
        private bool _rollRequested = false;
        private bool _jumpRequested = false;
        private bool _jumpConsumed = false;
        private bool _jumpedThisFrame = false;
        private bool _isHitReacting = false;
        private float _timeSinceJumpRequested = Mathf.Infinity;
        private float _timeSinceLastAbleToJump = 0f;
        private float _rollAngle;
        
        [SerializeField] private PID3 torquePID = null;
        private Vector3 _torqueIntegral;
        private Vector3 _torqueError;
        
        private void OnEnable()
        {
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        }

        private void OnDisable()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
        }

        public void OnBeforeAssemblyReload()
        {
        }

        public void OnAfterAssemblyReload()
        {
            Motor.CharacterController = this;
        }
        
        private void Start()
        {
            Motor.CharacterController = this;
        }

        public void SetInputs(ref CharacterInputs inputs)
        {
            _moveInputVector = inputs.Move;
            _lookInputVector = inputs.Look;
            _shouldRun = inputs.Run;
            
            if (inputs.BeginRoll)
            {
                _rollRequested = true;
            }
            
            // Jumping input
            if (inputs.BeginJump)
            {
                _timeSinceJumpRequested = 0f;
                _jumpRequested = true;
            }
        }

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            var upwards = -Gravity.normalized;
            
            if ((_lookInputVector != Vector3.zero || Motor.CharacterUp != upwards) && OrientationSharpness > 0f)
            {
                var forward = Vector3.Cross(Motor.CharacterRight, upwards);

                // Smoothly interpolate from current to target look direction
                var smoothedLookInputDirection = Vector3.Slerp(forward, _lookInputVector, 1 - Mathf.Exp(-OrientationSharpness * deltaTime)).normalized;

                // Set the current rotation (which will be used by the KinematicCharacterMotor)
                currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, upwards);
            }
        }

        private IEnumerator Roll()
        {
            Motor.enabled = false;
            var rb = GetComponent<Rigidbody>();
            rb.maxAngularVelocity = 50f;
            rb.isKinematic = false;
            
            while (_rollAngle < 360f)
            {
                yield return new WaitForFixedUpdate();
                
                _rollRequested = false;
                _rollAngle += RollSpeed * Time.fixedDeltaTime;

                var t = Mathf.Cos(_rollAngle * Mathf.Deg2Rad) * 0.5f + 0.5f;
                rb.centerOfMass = Mathf.Lerp(Motor.Capsule.height, 0, t) * Vector3.up;

                var forward = _moveInputVector != Vector3.zero ? _moveInputVector : Vector3.Cross(Motor.CharacterRight, Vector3.up);
                var targetRotation = Quaternion.LookRotation(_moveInputVector) * Quaternion.AngleAxis(_rollAngle, Vector3.right);
                var targetTorque = rb.rotation.TorqueTo(targetRotation, Time.fixedDeltaTime);
                var torque = torquePID.Output(rb.angularVelocity, targetTorque, ref _torqueIntegral, ref _torqueError,
                    Time.fixedDeltaTime);
                rb.AddTorque(torque, ForceMode.Acceleration);
                rb.velocity = (forward * MaxStableMoveSpeed).WithY(rb.velocity.y);
            }
            
            rb.isKinematic = true;
            _rollAngle = 0f;
            _rbVelocity = rb.velocity;
            _justSwitchedFromRigidbody = true;
            Motor.enabled = true;
            Motor.SetPosition(rb.position);
            Motor.SetRotation(rb.rotation);
        }

        private bool _justSwitchedFromRigidbody;
        private Vector3 _rbVelocity;

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (_justSwitchedFromRigidbody)
            {
                _justSwitchedFromRigidbody = false;
                currentVelocity = _rbVelocity;
            }
            
            var targetMovementVelocity = Vector3.zero;
            if (Motor.GroundingStatus.IsStableOnGround)
            {
                // Reorient source velocity on current ground slope (this is because we don't want our smoothing to cause any velocity losses in slope changes)
                currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, Motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;

                // Calculate target velocity
                // var inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp);
                var inputRight = Vector3.Cross(_moveInputVector, Vector3.up);
                var reorientedInput = Vector3.Cross(Motor.GroundingStatus.GroundNormal, inputRight).normalized * _moveInputVector.magnitude;
                targetMovementVelocity = reorientedInput * MaxStableMoveSpeed;

                // Smooth movement Velocity
                currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1 - Mathf.Exp(-StableMovementSharpness * deltaTime));
                
                if (_rollRequested)
                {
                    StartCoroutine(Roll());
                    return;
                }
            }
            else
            {
                // Add move input
                if (_moveInputVector.sqrMagnitude > 0f)
                {
                    targetMovementVelocity = _moveInputVector * MaxAirMoveSpeed;

                    // Prevent climbing on un-stable slopes with air movement
                    if (Motor.GroundingStatus.FoundAnyGround)
                    {
                        var perpendicularObstructionNormal = Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp).normalized;
                        targetMovementVelocity = Vector3.ProjectOnPlane(targetMovementVelocity, perpendicularObstructionNormal);
                    }

                    var velocityDiff = Vector3.ProjectOnPlane(targetMovementVelocity - currentVelocity, Gravity);
                    currentVelocity += velocityDiff * AirAccelerationSpeed * deltaTime;
                }

                // Gravity
                currentVelocity += Gravity * deltaTime;

                // Drag
                currentVelocity *= 1f / (1f + Drag * deltaTime);
            }
            
            // Handle jumping
            _jumpedThisFrame = false;
            _timeSinceJumpRequested += deltaTime;
            if (_jumpRequested)
            {
                // See if we actually are allowed to jump
                if (!_jumpConsumed && ((AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround) || _timeSinceLastAbleToJump <= JumpPostGroundingGraceTime))
                {
                    // Calculate jump direction before ungrounding
                    var jumpDirection = Motor.CharacterUp;
                    if (Motor.GroundingStatus.FoundAnyGround && !Motor.GroundingStatus.IsStableOnGround)
                    {
                        jumpDirection = Motor.GroundingStatus.GroundNormal;
                    }

                    // Makes the character skip ground probing/snapping on its next update. 
                    // If this line weren't here, the character would remain snapped to the ground when trying to jump. Try commenting this line out and see.
                    Motor.ForceUnground(0.1f);

                    // Add to the return velocity and reset jump state
                    currentVelocity += (jumpDirection * JumpSpeed) - Vector3.Project(currentVelocity, Motor.CharacterUp);
                    _jumpRequested = false;
                    _jumpConsumed = true;
                    _jumpedThisFrame = true;
                }
            }
        }

        public void BeforeCharacterUpdate(float deltaTime)
        {
        }

        public void PostGroundingUpdate(float deltaTime)
        {
        }

        public void AfterCharacterUpdate(float deltaTime)
        {
            // Handle jump-related values
            {
                // Handle jumping pre-ground grace period
                if (_jumpRequested && _timeSinceJumpRequested > JumpPreGroundingGraceTime)
                {
                    _jumpRequested = false;
                }

                // Handle jumping while sliding
                if (AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround)
                {
                    // If we're on a ground surface, reset jumping values
                    if (!_jumpedThisFrame)
                    {
                        _jumpConsumed = false;
                    }
                    _timeSinceLastAbleToJump = 0f;
                }
                else
                {
                    // Keep track of time since we were last able to jump (for grace period)
                    _timeSinceLastAbleToJump += deltaTime;
                }
            }
            
            var input = new AnimatedMotorProperties()
            {
                MoveSpeedNormalized = Motor.BaseVelocity.magnitude / MaxStableMoveSpeed,
                IsGrounded = Motor.GroundingStatus.FoundAnyGround,
                DirectionY = Mathf.Clamp01(Mathf.InverseLerp(1f, -1f, Motor.Velocity.y)),
                VelocityX = Vector3.Dot(Motor.BaseVelocity, Motor.CharacterRight) / MaxStableMoveSpeed,
                VelocityZ = Vector3.Dot(Motor.BaseVelocity, Motor.CharacterForward) / MaxStableMoveSpeed,
                IsHitReacting = _isHitReacting
            };

            OnAnimatedPropertiesChanged?.Invoke(input);
        }

        public bool IsColliderValidForCollisions(Collider coll)
        {
            return true;
        }

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            ref HitStabilityReport hitStabilityReport)
        {
        }

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition,
            Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
        }

        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {
        }
    }
}