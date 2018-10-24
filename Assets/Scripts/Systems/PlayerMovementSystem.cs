using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

[UpdateAfter(typeof(FixedUpdate))]
public class PlayerMovementSystem : ComponentSystem
{
    // Start is called before the first frame update
	private struct PlayerGroup
	{
		public Transform transform;
		public Rigidbody rb;
		public Player Player;
		public CapsuleCollider capsuleCollider;
	}

    // Update is called once per frame
    protected override void OnUpdate()
    {
	    foreach(var entity in GetEntities<PlayerGroup>())
	    {
		    //player.Player.ProcessPhysics();

			var dt = Time.fixedDeltaTime;

		    var player = entity.Player;
		    var velocity = entity.rb.velocity;
		    var grounded = entity.Player.grounded;

			if(player.stunned.InProgress) return;

			if(player.RootMotionOverride)
			{
				player.currentSpeed = Vector3.zero;
				player.UpdateRotation();
				return;
			}

			//---- FIND GROUND SYSTEM

			var hits = Physics.SphereCastAll(entity.transform.position + Vector3.up * 0.25f, 0.2f, Vector3.down, 0.1f, ~LayerMask.GetMask("Actor"), QueryTriggerInteraction.Ignore);
			var groundNormal = Vector3.down;

			bool wasGrounded = grounded;

			if(hits.Length > 0)
			{
				var groundHit = hits[0];

				for(var i = 1; i < hits.Length; i++)
				{
					if(hits[i].normal.y > groundHit.normal.y)
					{
						groundHit = hits[i];
					}
				}
				groundNormal = groundHit.normal.normalized;

				if(!grounded)
				{
					entity.Player.remainingJumps = 0;
					grounded = true;
				}
			}
			else
			{
				grounded = false;
			}

			// Get the ground incline (positive = uphill, negative = downhill)
			var incline = Vector3.Dot(groundNormal, -player.currentSpeed.normalized);

			// We aren't grounded if the slope is too steep!
			grounded &= Mathf.Abs(incline) < 0.75f;

			var yVelocity = velocity.y;

			// --- JUMP SYSTEM

			#region Jump Logic
			if(!grounded && wasGrounded && entity.Player.remainingJumps == 0)
			{
				entity.Player.jumpAllowance.Reset();
				entity.Player.jumpAllowance.SetDuration(dt * 4);
			}

			//Disable extra jumps if we're falling too fast
			if(!grounded && !entity.Player.jumpAllowance.InProgress && yVelocity <= -5f)
			{
				entity.Player.remainingJumps = 0;
			}

			//Did we queue a jump ?
			if(entity.Player.queueJump && entity.Player.remainingJumps > 0)
			{
				entity.Player.queueJump = false;
				grounded = false;
				entity.Player.remainingJumps--;

				//jump!
			   yVelocity = Mathf.Sqrt(2 * -Physics.gravity.y * entity.Player.gravityScale * entity.Player.jumpHeight);
			}
			#endregion

			// -- CHANGE 

			var targetSpeed = player.move * Mathf.Max(player.minSpeed, (player.Run ? player.runSpeed : player.walkSpeed));
		    player.currentSpeed = Vector3.SmoothDamp(player.currentSpeed, targetSpeed, ref player.speedSmoothVelocity, player.speedSmoothTime * (grounded ? 1f : 8f));

			if(grounded) // No directional input in the air
			{
				if(incline >= 0f)
				{
					// Set move velocity if we are on level ground OR going uphill
					velocity = player.currentSpeed;
				}
				else
				{
					// Do some math to make the move vector parallel to the ground
					var cross = Vector3.Cross(player.currentSpeed.normalized, Vector3.up);
					velocity = Vector3.Cross(groundNormal, cross) * player.currentSpeed.magnitude;

					// Make the move speed a bit faster or slower depending on the incline
					velocity *= 1 - incline * 0.5f;
				}

				// Counteract gravity (so we don't slide on an incline!)
				velocity -= Physics.gravity * Time.fixedDeltaTime;
			}
			else
			{
				velocity = player.currentSpeed.WithY(yVelocity);
				velocity += Physics.gravity.y * (player.gravityScale - 1f) * Vector3.up * Time.fixedDeltaTime;
			}

		    entity.Player.grounded = grounded;
		    entity.rb.velocity = velocity;
			entity.rb.centerOfMass = grounded ? Vector3.zero : entity.capsuleCollider.center;

		    entity.Player.UpdateRotation();
	    }
    }
}
