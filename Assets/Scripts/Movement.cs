using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour {

    public float zSpeed;



    private Rigidbody rb;

	// Use this for initialization
	void Start () {
        rb = GetComponent<Rigidbody>();
        rb.velocity = new Vector3(0, 0, -zSpeed);
    }
	


    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "PlayArea")
        {
            rb.velocity = new Vector3(0, 0, 0);
            rb.useGravity = true;
        }
    }
}
