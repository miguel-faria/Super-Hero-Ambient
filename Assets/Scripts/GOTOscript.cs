using UnityEngine;
using System.Collections;

public class GOTOscript : MonoBehaviour {

	// Use this for initialization
	Animator anim;
	NavMeshAgent agent;
	AnimatorStateInfo state;
	Transform[] destinations;
	float time;
	float fieldOfViewAngle = 110f;           // Number of degrees, centred on forward, for the enemy see.

	void Start () 
	{
		GameObject[] objects = GameObject.FindGameObjectsWithTag("Destination");
		destinations = new Transform[objects.Length];
		for (int i = 0; i < objects.Length; i++)
			destinations [i] = objects [i].transform ;

		anim = GetComponent<Animator> ();
		agent = GetComponent<NavMeshAgent>();

		time = Time.time;
		agent.SetDestination(destinations[Random.Range (0, destinations.Length)].position);
		//Debug.Log (agent.destination);
		anim.SetBool ("isWalking", true);
		state = anim.GetCurrentAnimatorStateInfo (0);
	}
	
	// Update is called once per frame
	void Update () {


		state = anim.GetCurrentAnimatorStateInfo (0);
		//Debug.Log(state.IsName("idle"));
		//Debug.Log(agent.destination.Equals(GameObject.Find("Dest1").transform.position));
		if (Input.GetKeyDown ("space")) {
			agent.Stop();
			anim.SetTrigger ("Laugh");
			anim.SetBool("isWalking",false);
			return;
		}

		if (state.IsName("idle")) {
			agent.Resume();
			agent.SetDestination(destinations[Random.Range (0, destinations.Length)].position);
			anim.SetBool("isWalking",true);
			return;
		}

		if (state.IsName ("walk") && Time.time - time >= 5) {
			agent.SetDestination (destinations [Random.Range (0, destinations.Length)].position);
//			Debug.Log (agent.destination);
			anim.SetBool ("isWalking", true);
			time = Time.time;
			return;
		}


	}

	void OnTriggerEnter (Collider other)
	{
		Debug.Log ("triggered");
		// If the player has entered the trigger sphere...
		if (other.gameObject.CompareTag ("Citizen")) {
			// Create a vector from the enemy to the player and store the angle between it and forward.
			Vector3 direction = other.transform.position - transform.position;
			float angle = Vector3.Angle (direction, transform.forward);
			
			// If the angle between forward and where the player is, is less than half the angle of view...
			if (angle < fieldOfViewAngle * 0.5f) {
				Debug.Log ("Citizen colision");
				agent.SetDestination (other.gameObject.transform.position);
				return;
				
			}
		}
	}





}
