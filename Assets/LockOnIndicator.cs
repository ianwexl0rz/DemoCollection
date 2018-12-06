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
		    indicatorPos += player.capsuleCollider.height * Vector3.up;
	    }

	    transform.position = indicatorPos;
	    transform.LookAt(Camera.main.transform.position.WithY(transform.position.y), Vector3.up);
	}
}
