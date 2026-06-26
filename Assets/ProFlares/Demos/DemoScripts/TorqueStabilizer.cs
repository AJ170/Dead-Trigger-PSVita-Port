//This asset was uploaded by https://unityassetcollection.com


using UnityEngine;
using System.Collections;

public class TorqueStabilizer : MonoBehaviour {

    public float stability = 0.3f;
    public float speed = 2.0f;
	
	private Rigidbody thisRigidBody;

	void Start () {
		thisRigidBody = GetComponent<Rigidbody>();
	}

    // Update is called once per frame
    void FixedUpdate () {

        Vector3 predictedUp = Quaternion.AngleAxis(
            thisRigidBody.angularVelocity.magnitude * Mathf.Rad2Deg * stability / speed,
            thisRigidBody.angularVelocity
        ) * transform.up;

        Vector3 torqueVector = Vector3.Cross(predictedUp, Vector3.up);
        thisRigidBody.AddTorque(torqueVector * speed*GetComponent<Rigidbody>().mass*4);
    }
}