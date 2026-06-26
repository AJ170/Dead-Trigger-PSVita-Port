using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProFlares_Demo_Space : MonoBehaviour {


	public GameObject missilePrefab;

	public Transform missileLauncher;

	public List<GameObject> missileList = new List<GameObject>();
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
	

		if (Input.GetKey("space"))
		{
			GameObject missileClone = Instantiate<GameObject>(missilePrefab);

			missileClone.transform.position = missileLauncher.position;

			//missileClone.GetComponent<Rigidbody>().velocity = Vector3.up*1;
		}


		
	}
}
