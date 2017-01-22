using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuyMovement : MonoBehaviour {

    public float zLimit;

    private Rigidbody rb;

	// Use this for initialization
	void Start () {
        rb = GetComponent<Rigidbody>();

        
	}
	
	// Update is called once per frame
	void Update () {
        Vector3 pos = new Vector3(rb.position.x, rb.position.y, zLimit);
        rb.position = pos;
        
    }
}
