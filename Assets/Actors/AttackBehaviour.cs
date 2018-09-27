using UnityEngine;

public class AttackBehaviour : StateMachineBehaviour
{
	public bool applyRootMotion = false;
	Player player = null;

	private bool fullyTransitioned = false;

	// OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
	override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		animator.SetBool("isAttacking", true);
		fullyTransitioned = false;
	}

	// OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
	override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if(!animator.IsInTransition(0) && !fullyTransitioned)
		{
			fullyTransitioned = true;
			player = animator.GetComponent<Player>();
			player.rootMotionOverride = applyRootMotion;
			if(applyRootMotion) player.animator.applyRootMotion = true;
		}
	}

	// OnStateExit is called when a transition ends and the state machine finishes evaluating this state
	override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		animator.SetBool("isAttacking", false);
		animator.applyRootMotion = false;
		player.rootMotionOverride = false;
		player.SetCancelOK();
	}

	// OnStateMove is called right after Animator.OnAnimatorMove(). Code that processes and affects root motion should be implemented here
	//override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
	//
	//}

	// OnStateIK is called right after Animator.OnAnimatorIK(). Code that sets up animation IK (inverse kinematics) should be implemented here.
	//override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
	//
	//}
}