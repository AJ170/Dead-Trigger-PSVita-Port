using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleEnabler : MonoBehaviour {
	public bool bParticlesOn = true;

	void Awake() {
		DontDestroyOnLoad (gameObject);
	}

	// Update is called once per frame
	void Update () {
		if (Input.GetButtonDown ("DPad Up") || Input.GetKeyDown (KeyCode.Q)) {
			bParticlesOn = !bParticlesOn;
			SetParticles (bParticlesOn);
		}
	}

	void SetParticles(bool state) {
		ParticleSystem[] allParticles = FindObjectsOfType<ParticleSystem>();
		foreach (ParticleSystem ps in allParticles)
		{
			ps.gameObject.SetActive (state);
		}
	}
}
