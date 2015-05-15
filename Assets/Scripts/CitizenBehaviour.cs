using UnityEngine;
using System.Collections;
using SuperHeroAmbient;

public class CitizenBehaviour : MonoBehaviour {
	
	// Use this for initialization
	Animator anim;
	NavMeshAgent agent;
	AnimatorStateInfo state;
	Transform[] destinations;
	float time = 0f;
	public GameObject MagicAura;

	int life = 3;
	public delegate void AttackedAction(GameObject gob);
	public static event AttackedAction OnAttack;

	bool immune = false;
	bool noPerception = true;
	bool isBeingAttacked = false;
	bool isRunningFromVillain = false;
	bool isRunningFromScream = false;
	bool isRunningFromConversion = false;
	float runTime = float.MaxValue;
	Vector3 direction = new Vector3();

	// Evil Variables

	public delegate void SeenHeroAction(Vector3 hero);
	public static event SeenHeroAction OnVision;

	int transformationState;
	bool heroSeen = false;
	Vector3 heroPosition = new Vector3();

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
		GameObject[] objects = GameObject.FindGameObjectsWithTag("Destination");
		//GameObject.FindGameObjectWithTag ("FxTemporaire").SetActive(false);
		destinations = new Transform[objects.Length];
		for (int i = 0; i < objects.Length; i++)
			destinations [i] = objects [i].transform ;

		anim = GetComponent<Animator> ();
		agent = GetComponent<NavMeshAgent>();
		
		time = Time.time;
		agent.SetDestination(destinations[Random.Range (0, destinations.Length)].position);
		anim.SetFloat ("Speed", 3f);
		state = anim.GetCurrentAnimatorStateInfo (0);
		transformationState = 0;
	}
	
	// Update is called once per frame
	void Update () {

		if (!IsEvil () && Time.time - runTime >= 3f) {

			if (isRunningFromConversion) {
				Run ();
				isRunningFromConversion = false;
				Debug.Log("AAAAH HELPUUUUUU-DEEEES");
				return;
			}
			if (isRunningFromScream) {
				StopRunning ();
				return;
			}
			if (isRunningFromScream || isRunningFromVillain) {
				Run ();
				return;
			}
			if (noPerception ) {
				RandomWalk ();
				return;
			}
		}
		if (heroSeen && Time.time - time >= 3f) {
			WarnVillains ();
			return;
		}
		if (noPerception && Time.time - time >= 3f) {
			RandomWalk ();
			return;
		}
		
		
	}
	
	void OnTriggerEnter (Collider other)
	{

		// If the player has entered the trigger sphere...
		if (other.gameObject.CompareTag ("Villain") && !IsEvil ()) {
			// Create a vector from the enemy to the player and store the angle between it and forward.
			direction = other.transform.position - transform.position;
			float angle = Vector3.Angle (direction, transform.forward);

			// If the angle between forward and where the player is, is less than half the angle of view...
			if (angle < Definitions.FIELDOFVIEWANGLE * 0.5f) {
				isRunningFromVillain = true;
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
			direction = other.transform.position - transform.position;
			float angle = Vector3.Angle (direction, transform.forward);
			
			// If the angle between forward and where the player is, is less than half the angle of view...
			if (angle < Definitions.FIELDOFVIEWANGLE * 0.5f) {
				isRunningFromConversion = true;
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


	public bool IsImmune()
	{
		return immune;
	}

/***********************************************************************
	 **************************** Sensor Methods ***************************
	 ***********************************************************************/

	public void Converted (bool success)
	{
		if (success) {
			if (transformationState < 5) {
				transformationState++;
				Debug.Log ("Being transformed!!! More " + (5 - transformationState) + " to become evil!");
				if (transformationState == 5) {
					//GameObject aura = GameObject.Find("ConvertionMagic");
					//aura.SetActive(true);
					
					Quaternion rotation = new Quaternion ();
					GameObject aura = (GameObject)Instantiate (MagicAura, transform.position, rotation);
					aura.transform.SetParent (transform);
				}
				direction = GameObject.Find("ConverterVillain").transform.position - transform.position;
				isRunningFromConversion=true;

			}
			if (OnAttack != null)
				OnAttack (this.gameObject);
		} else {
			immune = true;
		}
	}

	public void Attacked()
	{
		Debug.Log ("BEING ATTACKED!!!");
		isBeingAttacked = true;
		life--;
		if (OnAttack != null)
			OnAttack (this.gameObject);
	}
	
	public void Saved() 
	{
		if (transformationState > 0 && transformationState < 5)
			transformationState--;
	}
	
	void HeardScream(GameObject citizen)
	{
		float distance = Vector3.Distance (transform.position, citizen.transform.position);
		// In Hearing Range
		if (distance <= Definitions.AOERUNNINGDISTANCE && !IsEvil ()) {
			isRunningFromScream = true;
			runTime = Time.time;
			direction = citizen.transform.position - transform.position;
		}
	}




/***********************************************************************
	 **************************** Actuator Methods *************************
	 ***********************************************************************/

	void Run()
	{
		agent.speed = 8;
		//Debug.Log ("AHAHASHHAAHSLDJAÇSDJÇA");
		anim.SetFloat ("Speed", 8f);
		agent.SetDestination(direction.normalized);
		time = Time.time;
	}

	void StopRunning()
	{
		isRunningFromScream = false;
		isRunningFromVillain = false;
		noPerception = true;
		time = float.MaxValue;
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
		time = Time.time;
	}

// SPECIFIC EVIL BEHAVIOUR

	void WarnVillains()
	{
		if (OnVision != null)
			OnVision (heroPosition);
	}
}