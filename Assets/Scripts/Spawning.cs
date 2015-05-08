using UnityEngine;
using System.Collections;

public class Spawning : MonoBehaviour {


	// Use this for initialization
	void Start () {
		GameObject citizen = GameObject.Find ("SpawningCitizen");
		for (int i = 0; i < 20 ; i++) {
			Vector3 position = new Vector3(Random.Range(-1000f,1000f), 0, Random.Range(-1000f, 1000f));
			NavMeshHit hit;
			NavMesh.SamplePosition(position, out hit, 10, 1);
			position = hit.position;
			Quaternion rotation = new Quaternion();
			rotation.SetLookRotation(new Vector3 (0f, Random.Range (0f, 360f), 0f));
			Instantiate (citizen, position, rotation);
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
