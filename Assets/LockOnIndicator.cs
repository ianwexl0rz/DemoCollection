using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockOnIndicator : MonoBehaviour
{

    public void UpdatePosition(bool lockedOn, Transform target)
    {
		var showIndicator = lockedOn && target != null;
	    gameObject.SetActive(showIndicator);
	    if(!showIndicator) { return; }

	    var indicatorPos = target.position;
		if(target.GetComponent<Actor>() is Player player)
	    {
		    indicatorPos += (player.capsuleCollider.height + 0.2f) * Vector3.up;
	    }

	    transform.position = indicatorPos;
		var camera = GameManager.I.mainCamera;
	    transform.LookAt(camera.transform.position.WithY(transform.position.y), Vector3.up);
	}
}
