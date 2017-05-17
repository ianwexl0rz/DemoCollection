using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {
    //animations are called in unity blend tree so don't need to be named within this script. they will be triggered based on movement speed
    
	public float walkSpeed = 4;
    public float runSpeed = 20;

    public float turnSmoothTime = 0.2f; //time it takets from angle to go from current value to target value
    float turnSmoothVelocity; //ref

    public float speedSmoothTime = 0.1f;
    float speedSmoothVelocity;
    float currentSpeed;

    Animator animator;
	Transform cameraT;


	// Use this for initialization
	void Start () {
        animator = GetComponent<Animator>();
		cameraT = Camera.main.transform;
	}
	
	// Update is called once per frame
	void Update () {
        //keyboard input vector2
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        //turns input vector into direction
        Vector2 inputDir = input.normalized;

        //only calculate direction if input direction is not 00
        if (inputDir != Vector2.zero)
        {
            //charactor rotation using trigonometry. finding angle of rotation (theta). arctan ? 
			float targetRotation = Mathf.Atan2(inputDir.x, inputDir.y) * Mathf.Rad2Deg + cameraT.eulerAngles.y;
            transform.eulerAngles = Vector3.up * Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref turnSmoothVelocity, turnSmoothTime); //returns angle in radians but we want it in degrees, so we multiply by radians to degrees

        }
        bool running = Input.GetKey(KeyCode.LeftShift); //hold down shift key to run
        float targetSpeed = (running ? runSpeed : walkSpeed) * inputDir.magnitude; //if we are running the speed = runspeed outwise speed = walk speed. *magnitude determines that if there's no input, magnitude will be 0 so character wont move at 0
        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, speedSmoothVelocity);
        //move character
        transform.Translate(transform.forward * currentSpeed * Time.deltaTime, Space.World);

		if(animator != null)
		{
			//control speed percent in animator so that character walks or runs depending on speed
			float animationSpeedPercent = inputDir.magnitude;

			if(!running)
			{
				// If not running, slow down the animation proportionally
				animationSpeedPercent *= walkSpeed / runSpeed;
			}

			//reference for animator
			animator.SetFloat("speedPercent", animationSpeedPercent, speedSmoothTime, Time.deltaTime);
		}
    }
}
