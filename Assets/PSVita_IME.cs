using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PSVita_IME : MonoBehaviour {

	// Use this for initialization
	void Awake () {
        Sony.Vita.Dialog.Main.Initialise();
    }
	
	// Update is called once per frame
	void Update () {
        Sony.Vita.Dialog.Main.Update();
    }
}
