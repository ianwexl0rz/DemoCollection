using System;
using UnityEngine;

namespace ActorFramework
{
    public class CharacterAnimator : MonoBehaviour
    {
        private static readonly int SpeedPercent = Animator.StringToHash("speedPercent");
        private static readonly int InAir = Animator.StringToHash("inAir");
        private static readonly int DirectionY = Animator.StringToHash("directionY");
        private static readonly int VelocityX = Animator.StringToHash("velocityX");
        private static readonly int VelocityZ = Animator.StringToHash("velocityZ");
        private static readonly int InHitStun = Animator.StringToHash("InHitStun");

        [SerializeField] private float dampTime = 0.05f;
        
        private Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            var motor = GetComponent<CharacterMotor>();

            motor.OnAnimatedPropertiesChanged += SetParameters;
        }

        private void SetParameters(CharacterMotor.AnimatedProperties input)
        {
            if(_animator == null || _animator.runtimeAnimatorController == null) { return; }

            _animator.SetFloat(SpeedPercent, input.MoveSpeedNormalized, dampTime, Time.deltaTime);
            
            foreach(var parameter in _animator.parameters)
            {
                var nameHash = parameter.nameHash;
                if(nameHash == InAir)
                {
                    _animator.SetBool(InAir, !input.IsGrounded);
                }
                else if(nameHash == DirectionY)
                {
                    _animator.SetFloat(DirectionY, input.DirectionY, dampTime, Time.deltaTime);
                }
                else if(nameHash == VelocityX)
                {
                    _animator.SetFloat(VelocityX, input.VelocityX, dampTime, Time.deltaTime);
                }
                else if(nameHash == VelocityZ)
                {
                    _animator.SetFloat(VelocityZ, input.VelocityZ, dampTime, Time.deltaTime);
                }
                else if(nameHash == InHitStun)
                {
                    _animator.SetBool(InHitStun, input.IsInHitStun);
                }
            }
        }
    }
}