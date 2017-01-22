using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Boundary
{
    public float xMin, xMax, yMin, yMax;
}

public class PlayerMovement : MonoBehaviour {

    public float forceMag;
    public Boundary boundary;
    public float distance = 1.5f;

    private Rigidbody rb;

	// Use this for initialization
	void Start () {
        rb = GetComponent<Rigidbody>();
        Cursor.visible = false;
    }
	
	// Update is called once per frame
	void Update () {
        

        //Vector3 movement = new Vector3(mousePos.x - rb.position.x, mousePos.y - rb.position.y, 0.0f);
        ObjectFollowCursor();



        rb.position = new Vector3(
            Mathf.Clamp(rb.position.x, boundary.xMin, boundary.xMax),
            Mathf.Clamp(rb.position.y, boundary.yMin, boundary.yMax),
            0.0f
           );

        Quaternion quat = new Quaternion();
        transform.rotation = quat;
	}

    void ObjectFollowCursor()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 point = ray.origin + (ray.direction * distance);
        //Debug.Log( "World point " + point );
        point.z = 0.0f;
        

        transform.position = point;
    }
}
