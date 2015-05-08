using UnityEngine;
using System.Collections;

public class CitizenBehaviour : MonoBehaviour {
	
	// Use this for initialization
	Animator anim;
	NavMeshAgent agent;
	AnimatorStateInfo state;
	Transform[] destinations;
	float time = 0f;
	float fieldOfViewAngle = 110f;

	public int life = 3;
	public delegate void AttackedAction(GameObject gob);
	public static event AttackedAction OnAttack;

	bool noPerception = true;
	bool isBeingAttacked = false;
	bool isRunningFromVillain = false;
	bool isRunningFromScream = false;
	float runTime = float.MaxValue;
	Vector3 direction = new Vector3();

	// Evil Variables

	public delegate void SeenHeroAction(Vector3 hero);
	public static event SeenHeroAction OnVision;

	int transformationState = 0;
	bool heroSeen = false;
	Vector3 heroPosition = new Vector3();

	void OnEnable()
	{
		OnAttack += HeardScream;
	}
	
	
	void OnDisable()
	{
		OnAttack -= HeardScream;
	}
	

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

		if (!IsEvil ()) {
			if (isRunningFromScream && Time.time - runTime >= 1.5f) {
				StopRunning ();
				return;
			}
			if (isRunningFromScream || isRunningFromVillain) {
				Run ();
				return;
			}
			if (noPerception && Time.time - time >= 1.5f) {
				RandomWalk ();
				return;
			}
		}
		if (heroSeen) {
			WarnVillains ();
			return;
		}
		if (noPerception && Time.time - time >= 1.5f) {
			RandomWalk ();
			return;
		}
		
		
	}
	
	void OnTriggerEnter (Collider other)
	{
		Debug.Log ("seen a villain");
		// If the player has entered the trigger sphere...
		if (other.gameObject.CompareTag ("Villain") && !IsEvil ()) {
			// Create a vector from the enemy to the player and store the angle between it and forward.
			direction = other.transform.position - transform.position;
			float angle = Vector3.Angle (direction, transform.forward);

			// If the angle between forward and where the player is, is less than half the angle of view...
			if (angle < fieldOfViewAngle * 0.5f) {
				Debug.Log ("Villain colision");
				isRunningFromVillain = true;
				return;
				
			}
		}

		if (other.gameObject.CompareTag ("Hero") && IsEvil ()) {
			// Create a vector from the enemy to the player and store the angle between it and forward.
			heroPosition = other.transform.position - transform.position;
			float angle = Vector3.Angle (heroPosition, transform.forward);
			
			// If the angle between forward and where the player is, is less than half the angle of view...
			if (angle < fieldOfViewAngle * 0.5f) {
				heroSeen = true;
				return;	
			}
		}
	}


	void OnTriggerStay (Collider other)
	{
		// If the player has entered the trigger sphere...
		if (other.gameObject.CompareTag ("Villain") && !IsEvil ()) {
			// Create a vector from the enemy to the player and store the angle between it and forward.
			direction = other.transform.position - transform.position;
			float angle = Vector3.Angle (direction, transform.forward);
			
			// If the angle between forward and where the player is, is less than half the angle of view...
			if (angle < fieldOfViewAngle * 0.5f) {
				Debug.Log ("Villain colision");
				isRunningFromVillain = true;
				return;
				
			}
		}
		if (other.gameObject.CompareTag ("Hero") && IsEvil ()) {
			// Create a vector from the enemy to the player and store the angle between it and forward.
			heroPosition = other.transform.position - transform.position;
			float angle = Vector3.Angle (heroPosition, transform.forward);
			
			// If the angle between forward and where the player is, is less than half the angle of view...
			if (angle < fieldOfViewAngle * 0.5f) {
				heroSeen = true;
				return;	
			}
		}
	}

	void OnTriggerExit(Collider other)
	{
		noPerception = true;
		time = 0f;

		if (isRunningFromVillain) {
			isRunningFromVillain = false;
			return;
		}
		if (heroSeen) {
			heroSeen = false;
			return;
		}
	}

	// O Cidadao torna-se um minion se o seu estado e' igual a 5
	public bool IsEvil()
	{
		return (transformationState == 5);
	}

	public void Transformed ()
	{
		Debug.Log ("Being transformed!!!");
		if(transformationState < 5)
			transformationState++;
		if (OnAttack != null)
			OnAttack (this.gameObject);
	}

	public void Attacked()
	{
		Debug.Log ("BEING ATTACKED!!!");
		Debug.Log (life);
		isBeingAttacked = true;
		life--;
		if (OnAttack != null)
			OnAttack (this.gameObject);
	}
	
	void Run()
	{
		agent.speed = 8;
		agent.SetDestination(direction.normalized);
	}

	void StopRunning()
	{
		isRunningFromScream = false;
		isRunningFromVillain = false;
		noPerception = true;
		time = float.MaxValue;
	}

	void HeardScream(GameObject citizen)
	{
		Debug.Log ("EVENT TRIGGERED!!");
		float distance = Vector3.Distance (transform.position, citizen.transform.position);
		// In Hearing Range
		if (distance <= 12 && !IsEvil ()) {
			isRunningFromScream = true;
			runTime = Time.time;
			direction = citizen.transform.position - transform.position;
		}

	}

	void RandomWalk()
	{
		agent.SetDestination (destinations [Random.Range (0, destinations.Length)].position);
		agent.speed = 3.5f;
		time = Time.time;
	}

	// SPECIFIC EVIL BEHAVIOUR

	void WarnVillains()
	{
		if (OnVision != null)
			OnVision (heroPosition);
	}

}
