using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AttackBehaviour : StateMachineBehaviour
{
	public bool applyRootMotion = false;
	private Player player = null;
	private Transform weaponTransform;

	private bool fullyTransitioned = false;

	// OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
	override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		player = animator.GetComponent<Player>();
		player.isAttacking = true;
		player.hitObjects = new List<GameObject>();

		weaponTransform = player.weaponTransform.transform;

		if(animator.parameters.Any(p => p.name == "isAttacking"))
		{
			animator.SetBool("isAttacking", true);
		}
		fullyTransitioned = false;
	}

	// OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
	override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if(!animator.IsInTransition(0) && !fullyTransitioned && !player.RootMotionOverride)
		{
			fullyTransitioned = true;
			player.RootMotionOverride = applyRootMotion;
			if(applyRootMotion) animator.applyRootMotion = true;
		}

		//SetWeaponInitialPosition();
	}

	// OnStateExit is called when a transition ends and the state machine finishes evaluating this state
	override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if(animator.parameters.Any(p => p.name == "isAttacking"))
		{
			animator.SetBool("isAttacking", false);
		}
		animator.applyRootMotion = false;
		player.RootMotionOverride = false;
		player.isAttacking = false;
		player.cancelOK = false;
	}

	// private void SetWeaponInitialPosition()
	// {
	// 	Vector3 origin = weaponTransform.position;
	// 	Vector3 end = origin + weaponTransform.forward * 1.2f;

	// 	// TODO: Update all weaponCollisions in a "weapon collision set"
	// 	player.weaponCollision.SetInitialPosition(origin, end);
	// }

	// OnStateMove is called right after Animator.OnAnimatorMove(). Code that processes and affects root motion should be implemented here
	//override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
	//
	//}

	// OnStateIK is called right after Animator.OnAnimatorIK(). Code that sets up animation IK (inverse kinematics) should be implemented here.
	//override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
	//
	//}
}