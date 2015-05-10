using UnityEngine;
using System.Collections;

public class GOTOscript : MonoBehaviour {

	// Use this for initialization
	Animator anim;
	NavMeshAgent agent;
	AnimatorStateInfo state;
	Transform[] destinations;
	float time = 0f;
	float citizenInViewTime = float.MaxValue;
	float attackTime = float.MaxValue;
	float fieldOfViewAngle = 110f;           // Number of degrees, centred on forward, for the enemy see.

	Vector3 citizenPos = new Vector3();
	CitizenBehaviour citizenSc = null;
	bool citizenInView = false;
	bool noPerception = true;

	void Start () 
	{
		anim = GetComponent<Animator> ();
		agent = GetComponent<NavMeshAgent>();

		GameObject[] objects = GameObject.FindGameObjectsWithTag("Destination");
		destinations = new Transform[objects.Length];
		for (int i = 0; i < objects.Length; i++)
			destinations [i] = objects [i].transform ;

		time = Time.time;
		agent.SetDestination(destinations[Random.Range (0, destinations.Length)].position);
		//Debug.Log (agent.destination);
		anim.SetBool ("isWalking", true);
		state = anim.GetCurrentAnimatorStateInfo (0);
	}
	
	// Update is called once per frame
	void Update () {

		if (!citizenInView && !(Time.time - citizenInViewTime >= 1f)) {
			StopFollowing ();
			return;
		}
		if (citizenInView && CitizenIsDead ()) {
			KilledCitizen ();
			Debug.Log ("killed");
			return;
		}
		if ((citizenInView || Time.time - citizenInViewTime >= 1f) && !citizenSc.IsEvil() && !CitizenInRange ()) {
			Follow ();
			return;
		}
		if (citizenInView && CitizenInRange() && !citizenSc.IsEvil()) {
			Transform ();
			return;
		}
		if (noPerception && Time.time - time >= 5f) {
			Debug.Log ("noperception");
			RandomWalk ();
			return;
		}

		state = anim.GetCurrentAnimatorStateInfo (0);
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
	}

	bool CitizenInRange()
	{
		// Calculating the distance to the citizenInView
		if (citizenInView) {
			float distance = Vector3.Distance (transform.position, citizenPos);
			return (distance <= 4);
		}
		return false;
	}	

	bool CitizenIsDead ()
	{
	  if (citizenSc.Life <= 0)
			return true;
		return false;
	}

	void KilledCitizen ()
	{
		// Laugh
		agent.Stop();
		anim.SetTrigger ("Laugh");
		anim.SetBool("isWalking",false);

		citizenInView = false;
		noPerception = true;
		citizenSc = null;
		UpdateAnimations (false, false, false, true, false);
	}


	void StopFollowing()
	{
		noPerception = true;
		citizenSc = null;
		time = float.MaxValue;
	}

	void Transform ()
	{
		if (Time.time - attackTime >= 1f) {
			citizenSc.Converted ();
			UpdateAnimations (false, false, false, true, true);
			attackTime = Time.time;
		}
	}

	void Attack()
	{
		if (Time.time - attackTime >= 1f) {
			Debug.Log ("attacking");
			UpdateAnimations (false, false, true, true, false);
			citizenSc.Attacked ();
			attackTime = Time.time;
		}
	}

	void StopAttacking() 
	{
		UpdateAnimations (false, false, false, true, false);
	}

	void Follow()
	{
		agent.Resume();
		agent.SetDestination (citizenPos);
		agent.speed = 8;
		UpdateAnimations (true, false, false, true, false);
	}

	void RandomWalk()
	{
		agent.Resume();
		agent.SetDestination (destinations [Random.Range (0, destinations.Length)].position);
		agent.speed = 3.5f;
		UpdateAnimations (true, false, false, true, false);
		time = Time.time;
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
				citizenInView = true;
				noPerception = false;
				Transform citizen = other.gameObject.transform;
				citizenPos.Set(citizen.position.x, citizen.position.y, citizen.position.z);
				citizenSc = (CitizenBehaviour) other.gameObject.GetComponent(typeof(CitizenBehaviour));
				return;

				
			}
		}
	}

	void OnTriggerStay(Collider other)
	{
		if (other.gameObject.CompareTag ("Citizen")) {
			// Create a vector from the enemy to the player and store the angle between it and forward.
			Vector3 direction = other.transform.position - transform.position;
			float angle = Vector3.Angle (direction, transform.forward);
			
			// If the angle between forward and where the player is, is less than half the angle of view...
			if (angle < fieldOfViewAngle * 0.5f) {
				Transform citizen = other.gameObject.transform;
				citizenPos.Set (citizen.position.x, citizen.position.y, citizen.position.z);
				return;
			}
		}
	}

	void OnTriggerExit(Collider other)
	{
		citizenInViewTime = Time.time;
		citizenInView = false;
	}

	void UpdateAnimations(bool walking, bool laughing, bool combat, bool alive, bool convert)
	{
		anim.SetBool ("isWalking", walking);
		anim.SetBool ("inCombat", combat);
		anim.SetBool ("isAlive", alive);

		if(laughing)
			anim.SetTrigger ("Laugh");
		if (convert)
			anim.SetTrigger ("Convert");
	}
}
