using UnityEngine;
using System.Collections;
using SuperHeroAmbient;

public class CitizenBehaviour : MonoBehaviour {
	
	// Use this for initialization
	Animator anim;
	NavMeshAgent agent;
	AnimatorStateInfo state;
	Transform[] destinations;


	int life;
	public delegate void AttackedAction(GameObject gob);
	public static event AttackedAction OnAttack;
	public delegate void Death(GameObject victim, GameObject attacker);
	public static event Death OnDeath;

	bool immune;
	
	bool isBeingAttacked;
	bool isBeingConverted;
	bool seenVillain;
	bool heardScream;
	bool isAlive;
	bool saved;

	float walkTime;
	float runTime;
	
	Vector3 villainDirection = new Vector3();
	Vector3 screamDirection = new Vector3();

	// Evil Variables

	public delegate void SeenHeroAction(Vector3 hero);
	public static event SeenHeroAction OnVision;

	public int transformationState;
	bool heroSeen;
	Vector3 heroPosition = new Vector3();

	public bool IsAlive {
		get {
			return isAlive;
		}
		set {
			isAlive = value;
		}
	}

	public bool IsBeingAttacked {
		get {
			return isBeingAttacked;
		}
		set {
			isBeingAttacked = value;
		}
	}

	public int Life {
		get {
			return life;
		}
		set {
			life = value;
		}
	}

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
		life = 5;
		transformationState = 0;
		immune = false;
		isBeingAttacked = false;
		isBeingConverted = false;
		seenVillain = false;
		heardScream = false;
		heroSeen = false;
		saved = false;
		isAlive = true;
		walkTime = 0.0f;
		runTime = 0.0f;
		GameObject[] objects = GameObject.FindGameObjectsWithTag("Destination");
		//GameObject.FindGameObjectWithTag ("FxTemporaire").SetActive(false);
		destinations = new Transform[objects.Length];
		for (int i = 0; i < objects.Length; i++)
			destinations [i] = objects [i].transform ;

		anim = GetComponent<Animator> ();
		agent = GetComponent<NavMeshAgent>();

		agent.SetDestination(destinations[Random.Range (0, destinations.Length)].position);
		anim.SetFloat ("Speed", 3f);
		state = anim.GetCurrentAnimatorStateInfo (0);
		//transformationState = 0;
	}
	
	// Update is called once per frame
	void Update () {

			if (!IsEvil ()) {
			if(Time.time - runTime >= 1.5f && Time.time - walkTime < 1.5f){
					if (isBeingAttacked || isBeingConverted) {
						Run (villainDirection);
						Scream();
						isBeingAttacked = false;
						isBeingConverted = false;
						//Debug.Log (this + " RunningFromConversion");
						return;
					}
					if (seenVillain) {
						Run (villainDirection);
						seenVillain = false;
						//Debug.Log (this + " RunningFromVillain");
						return;
					}
					if (heardScream) {
						Run (screamDirection);
						heardScream = false;
						//Debug.Log (this + " RunningFromScream");
						return;
					}
				} 
				if(Time.time - walkTime >= 1.5f && Time.time - runTime < 1.5f) {
					RandomWalk ();
					return;
				}
			}
			if (heroSeen) {
				WarnVillains ();
				return;
			}
			if(Time.time - walkTime >= 1.5f){
				RandomWalk ();
				return;
			}
	}
	
	void OnTriggerEnter (Collider other)
	{

		// If the player has entered the trigger sphere...
		if (other.gameObject.CompareTag ("Villain") && !IsEvil ()) {
			// Create a vector from the enemy to the player and store the angle between it and forward.
			villainDirection = other.transform.position - transform.position;
			float angle = Vector3.Angle (villainDirection, transform.forward);

			// If the angle between forward and where the player is, is less than half the angle of view...
			if (angle < Definitions.FIELDOFVIEWANGLE * 0.5f) {
				seenVillain = true;
				return;
				
			}
		}

		if (other.gameObject.CompareTag ("Hero") && IsEvil ()) {
			// Create a vector from the enemy to the player and store the angle between it and forward.
			heroPosition = other.transform.position - transform.position;
			float angle = Vector3.Angle (heroPosition, transform.forward);
			
			// If the angle between forward and where the player is, is less than half the angle of view...
			if (angle < Definitions.FIELDOFVIEWANGLE * 0.5f) {
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
			villainDirection = other.transform.position - transform.position;
			float angle = Vector3.Angle (villainDirection, transform.forward);
			
			// If the angle between forward and where the player is, is less than half the angle of view...
			if (angle < Definitions.FIELDOFVIEWANGLE * 0.5f) {
				seenVillain = true;
				return;
				
			}
		}
		if (other.gameObject.CompareTag ("Hero") && IsEvil ()) {
			// Create a vector from the enemy to the player and store the angle between it and forward.
			heroPosition = other.transform.position - transform.position;
			float angle = Vector3.Angle (heroPosition, transform.forward);
			
			// If the angle between forward and where the player is, is less than half the angle of view...
			if (angle < Definitions.FIELDOFVIEWANGLE * 0.5f) {
				heroSeen = true;
				return;	
			}
		}
	}

	void OnTriggerExit(Collider other)
	{
		if (other.gameObject.CompareTag ("Villain")) {
			seenVillain = false;
			return;
		}
		if (other.gameObject.CompareTag ("Hero")) {
			heroSeen = false;
			return;
		}
	}



	// O Cidadao torna-se um minion se o seu estado e' igual a 5
	public bool IsEvil()
	{
		return (transformationState == 5);
	}


	public bool IsSaved()
	{
		return saved;
	}

	public bool IsImmune()
	{
		return immune;
	}

/***********************************************************************
	 **************************** Sensor Methods ***************************
	 ***********************************************************************/

	public void Converted (bool success)
	{
		if (!saved) {
			if (success && !IsImmune ()) {
				if (transformationState < 5) {
					transformationState++;
					Debug.Log (this.name + " - Being transformed!!! More " + (5 - transformationState) + " to become evil!");
					if (transformationState == 5) {
						//aura.SetActive(true);
						transform.parent.GetComponent<CitizenManager> ().converted = true;

					}
					Vector3 villainDirection = GameObject.Find ("ConverterVillain").transform.position - transform.position;
					isBeingConverted = true;

				}
			} else {
				immune = true;
			}
		}
	}

	public void Attacked(GameObject attacker)
	{
		if (isAlive) {
			Debug.Log (this.name + " - BEING ATTACKED!!!");
			isBeingAttacked = true;
			life--;
			if (life <= 0) {
				isAlive = false;
				if (this.name.Equals ("Crush") && OnDeath != null)
					OnDeath (this.gameObject, attacker);
			}
		}
	}
	
	public void Saved() 
	{
		if (!saved) {
			Debug.Log (this.name + " - Being Saved!!! Need more " + (transformationState - (-3)) + " to be saved.");
			if (transformationState >= -3 && transformationState < 5)
				transformationState--;
			if (transformationState <= -3)
				saved = true;
		}
	}
	
	void HeardScream(GameObject citizen)
	{
		float distance = Vector3.Distance (transform.position, citizen.transform.position);
		// In Hearing Range
		if (distance <= Definitions.AOERUNNINGDISTANCE && !IsEvil ()) {
			heardScream = true;
			screamDirection = citizen.transform.position - transform.position;
		}
	}




/***********************************************************************
	 **************************** Actuator Methods *************************
	 ***********************************************************************/

	void Run(Vector3 target)
	{
		agent.speed = 8;
		anim.SetFloat ("Speed", 8f);
		agent.SetDestination(target.normalized);
		runTime = Time.time;
	}

	void Scream(){
		if (OnAttack != null)
			OnAttack (this.gameObject);
	}
	
	void RandomWalk()
	{	
		Vector3 position = new Vector3(Random.Range(-45f,80f), 0, Random.Range(-70f, 50f));
		NavMeshHit hit;
		NavMesh.SamplePosition(position, out hit, 10, 1);
		position = hit.position;
		//agent.SetDestination (destinations [Random.Range (0, destinations.Length)].position);
		agent.SetDestination (position);
		agent.speed = 3.5f;
		anim.SetFloat ("Speed", 3.5f);
		walkTime = Time.time;
	}

// SPECIFIC EVIL BEHAVIOUR

	void WarnVillains()
	{
		Debug.Log (this.name + " - Must Warn Villains!");
		if (OnVision != null)
			OnVision (heroPosition);
	}
}