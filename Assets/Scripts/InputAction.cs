using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rewired;

[CreateAssetMenu]
public class InputAction : ScriptableObject
{
    [SerializeField, ActionIdProperty(typeof(Controller.Axis))] private int moveHorizontal;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
