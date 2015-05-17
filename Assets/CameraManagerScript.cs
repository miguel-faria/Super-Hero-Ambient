using UnityEngine;
using System.Collections;

public class CameraManagerScript : MonoBehaviour {

	Camera chan, converter, hero;



	// Use this for initialization
	void Start () {
		chan = (Camera) GameObject.Find ("CameraChan").GetComponent ("Camera");
		//converter = (Camera)GameObject.Find ("CameraConverter").GetComponent ("Camera");
		hero = (Camera) GameObject.Find ("CameraHero").GetComponent ("Camera");
		chan.enabled = false;
		//converter.enabled = false;
		hero.enabled = true;
	}
	
	// Update is called once per frame
	void Update () {

		if (Input.GetKeyDown(KeyCode.Alpha1)) {
			chan.enabled = true;
			//converter.enabled = false;
			hero.enabled = false;
		}
		if (Input.GetKeyDown(KeyCode.Alpha2)) {
			chan.enabled = false;
			//converter.enabled = true;
			hero.enabled = false;
		}
		if (Input.GetKeyDown(KeyCode.Alpha3)) {
			chan.enabled = false;
			//converter.enabled = false;
			hero.enabled = true;
		}

	}
}
