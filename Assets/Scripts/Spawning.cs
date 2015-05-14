using UnityEngine;
using System.Collections;

public class Spawning : MonoBehaviour {

	public int startingCitizens;

	// Use this for initialization
	void Start () {
		GameObject citizen = GameObject.Find ("SpawningCitizen");
		for (int i = 0; i < startingCitizens ; i++) {
			Vector3 position = new Vector3(Random.Range(-45f,80f), 0, Random.Range(-70f, 50f));
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
