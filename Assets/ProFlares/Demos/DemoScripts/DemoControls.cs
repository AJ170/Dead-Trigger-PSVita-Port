//This asset was uploaded by https://unityassetcollection.com


/// ProFlares - v1.08 - Copyright 2014-2015 All rights reserved - ProFlares.com


using UnityEngine;
using System.Collections;

public class DemoControls : MonoBehaviour {
	
	public GameObject Setup1;
	
	public GameObject Setup1Extra;
	public Color Setup1Ambient;
	
	public GameObject Setup2;
	
	public GameObject Setup2Extra;
	public Color Setup2Ambient;
	public GameObject Setup3;
 	
	public GameObject Setup3Extra;
	public Color Setup3Ambient; 
	public bool Toggle;
	
	
	
	// Use this for initialization
	void Start () {
		 Swap(1);
	}
	 
	 
	public void Swap(int number){
		switch(number){
		
			case(1):{
				Setup1.SetActive(true);
				Setup2.SetActive(false);
				if(Setup3)Setup3.SetActive(false);
			
				if(Setup1Extra) Setup1Extra.SetActive(true);
				if(Setup2Extra) Setup2Extra.SetActive(false);
				if(Setup3Extra) Setup3Extra.SetActive(false);
			
				RenderSettings.ambientLight = Setup1Ambient;
			}break;
		
			case(2):{
				Setup1.SetActive(false);
				Setup2.SetActive(true);
				if(Setup3)Setup3.SetActive(false);
			
				if(Setup1Extra) Setup1Extra.SetActive(false);
				if(Setup2Extra) Setup2Extra.SetActive(true);
				if(Setup3Extra) Setup3Extra.SetActive(false);
			
				RenderSettings.ambientLight = Setup2Ambient;
				
			}break;
		
			case(3):{
				Setup1.SetActive(false);
				Setup2.SetActive(false);
				if(Setup3)Setup3.SetActive(true);
			
				if(Setup1Extra) Setup1Extra.SetActive(false);
				if(Setup2Extra) Setup2Extra.SetActive(false);
				if(Setup3Extra) Setup3Extra.SetActive(true);
			
				RenderSettings.ambientLight = Setup3Ambient;
				
			}break;
		}
	}
	
	public bool hideGUI;
	
	public ProFlareBatch batchLeft;
	public ProFlareBatch batchRight;
	
	void Update(){
	
		if(Input.GetKeyUp("1")){
			Swap(1);	
		} 
		if(Input.GetKeyUp("2")){
			Swap(2);	
		} 
		 
		
		if(batchLeft&&batchRight){
 			if(Input.GetKeyUp(KeyCode.M)){
				batchLeft.VR_Depth =	batchLeft.VR_Depth+0.05f;
				batchLeft.VR_Depth = Mathf.Clamp01(batchLeft.VR_Depth);
				batchRight.VR_Depth = batchLeft.VR_Depth;
			} 
			
			if(Input.GetKeyUp(KeyCode.N)){
				batchLeft.VR_Depth =	batchLeft.VR_Depth-0.05f;
				batchLeft.VR_Depth = Mathf.Clamp01(batchLeft.VR_Depth);
				batchRight.VR_Depth = batchLeft.VR_Depth;
			}
 		}
		
	}
	void OnGUI(){
	
		if(hideGUI)
			return;

	}
}
