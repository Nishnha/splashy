using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddToModel : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        Vector3 position = transform.position;
        position.z = 0;
        // Keep the player locked in the z axis
        transform.position = position;
	}
}
