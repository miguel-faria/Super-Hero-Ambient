using UnityEngine;
using System.Collections;

public class Spawning : MonoBehaviour {

	public int startingCitizens;
	public GameObject attackPickup;
	public GameObject healthPickup;
	public GameObject armorPickup;
	public GameObject citizen;

	// Use this for initialization
	void Start () {

		for (int i = 0; i < startingCitizens ; i++) {
			Vector3 position = new Vector3(Random.Range(-45f,80f), 0, Random.Range(-70f, 50f));
			NavMeshHit hit;
			NavMesh.SamplePosition(position, out hit, 5, 1);
			position = hit.position;
			Quaternion rotation = new Quaternion();
			rotation.SetLookRotation(new Vector3 (0f, Random.Range (0f, 360f), 0f));
			Instantiate (citizen, position, rotation);
		}

		GameObject pickup;
		GameObject[] objects = GameObject.FindGameObjectsWithTag("Pickups");
		Transform[] pickups = new Transform[objects.Length];
		for (int i = 0; i < objects.Length; i++)
			pickups [i] = objects [i].transform ;

		objects = new GameObject[3];
		objects [0] = attackPickup;
		objects [1] = healthPickup;
		objects [2] = armorPickup;


		for (int i = 0; i < 3; i++) {
			for (int j = 0; j < 4; j++){
				int pos;
				while(true){
					pos = Random.Range(0, 15);
					if(pickups[pos] != null)
						break;
				}
				pickup = objects[i];
				Quaternion rotation = new Quaternion();
				rotation.SetLookRotation(new Vector3 (0f, 0f, 0f));
				Instantiate (pickup, pickups[pos].transform.position, rotation);
				pickups[pos] = null;
			}

		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
