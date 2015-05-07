using UnityEngine;
using System.Collections;

public class CitizenBehaviour : MonoBehaviour {
	
	// Use this for initialization
	Animator anim;
	NavMeshAgent agent;
	AnimatorStateInfo state;
	Transform[] destinations;
	float time;
	float fieldOfViewAngle = 110f;
	
	//public delegate void CollisionAction();
	//public event CollisionAction onCollision;
	
	bool isRunning;
	float runTime;

	
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
		
		if (state.IsName("idle")) {
			agent.Resume();
			agent.SetDestination(destinations[Random.Range (0, destinations.Length)].position);
			anim.SetBool("isWalking",true);
			return;
		}
		
		if (state.IsName ("walk") && Time.time - time >= 2) {
			agent.SetDestination (destinations [Random.Range (0, destinations.Length)].position);
//			Debug.Log (agent.destination);
			anim.SetBool ("isWalking", true);
			time = Time.time;
			return;
		}
		
		
	}
	
	void OnTriggerEnter (Collider other)
	{
		Debug.Log ("seen a villain");
		// If the player has entered the trigger sphere...
		if (other.gameObject.CompareTag ("Villain")) {
			// Create a vector from the enemy to the player and store the angle between it and forward.
			Vector3 direction = other.transform.position - transform.position;
			//float angle = Vector3.Angle (direction, transform.forward);
			agent.SetDestination(direction.normalized);
			
			// If the angle between forward and where the player is, is less than half the angle of view...
			/*if (angle < fieldOfViewAngle * 0.5f) {
				Debug.Log ("Villain colision");

	


				if(onCollision != null)
					onCollision();
				agent.SetDestination (other.gameObject.transform.position);
				return;
				
			}*/
		}
	}


	void OnTriggerStay (Collider other)
	{
		Debug.Log ("still running");

		// If the player has entered the trigger sphere...
		if (other.gameObject.CompareTag ("Villain")) {
			// Create a vector from the enemy to the player and store the angle between it and forward.
			Vector3 direction = transform.position - other.transform.position;
			//float angle = Vector3.Angle (direction, transform.forward);
			agent.SetDestination (direction.normalized);
			
			// If the angle between forward and where the player is, is less than half the angle of view...

		}
	}
	
	void Run()
	{
		isRunning = true;
		runTime = Time.time;
		agent.speed = 10;
	}
}
