using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

//Utility classes
class Belief{

	string _type;

	public Belief(string type){
		_type = type;
	}


	public string Type {
		get {
			return _type;
		}
		set {
			_type = value;
		}
	}
}

class SeeCitizenBelief : Belief{

	GameObject _citizen;
	Vector3 _citizenPosition;

	public SeeCitizenBelief(GameObject citizen) : base ("Saw Citizen"){
		_citizen = citizen;
		_citizenPosition = citizen.transform.position;
	}

	public GameObject Citizen {
		get {
			return _citizen;
		}
		set {
			_citizen = value;
		}
	}

	public Vector3 CitizenPosition {
		get {
			return _citizenPosition;
		}
		set {
			_citizenPosition = value;
		}
	}
}

class HearScreamBelief : Belief{

	GameObject _screamer;
	Vector3 _screamOrigin;

	public HearScreamBelief (GameObject screamer, Vector3 screamOrigin) : base("Heard a Scream") {
		_screamer = screamer;
		_screamOrigin = screamOrigin;
	}

	public GameObject Screamer {
		get {
			return _screamer;
		}
		set {
			_screamer = value;
		}
	}

	public Vector3 ScreamOrigin {
		get {
			return _screamOrigin;
		}
		set {
			_screamOrigin = value;
		}
	}
}

class SeeHeroBelief : Belief {

	GameObject _hero;
	Vector3 _heroPosition;

	SeeHeroBelief (GameObject hero): base("Saw the Hero"){

		_hero = hero;
		_heroPosition = hero.transform.position;
	}

	public GameObject Hero {
		get {
			return _hero;
		}
		set {
			_hero = value;
		}
	}

	public Vector3 HeroPosition {
		get {
			return _heroPosition;
		}
		set {
			_heroPosition = value;
		}
	}
}

class TouchHeroBelief : Belief {
	
	GameObject _hero;
	Vector3 _heroPosition;
	
	TouchHeroBelief (GameObject hero): base("Touch the Hero"){
		
		_hero = hero;
		_heroPosition = hero.transform.position;
	}
	
	public GameObject Hero {
		get {
			return _hero;
		}
		set {
			_hero = value;
		}
	}
	
	public Vector3 HeroPosition {
		get {
			return _heroPosition;
		}
		set {
			_heroPosition = value;
		}
	}
}

class Desire{

	string _desireName;
	GameObject _subjectObject;
	Vector3 _objectiveDestination;

	Desire(string desireName, GameObject desireObject){
		_desireName = desireName;
		_subjectObject = desireObject;
		_objectiveDestination = desireObject.transform.position;
	}

	public string DesireName {
		get {
			return _desireName;
		}
		set {
			_desireName = value;
		}
	}
	
	public GameObject SubjectObject {
		get {
			return _subjectObject;
		}
		set {
			_subjectObject = value;
		}
	}

	public Vector3 ObjectiveDestination {
		get {
			return _objectiveDestination;
		}
		set {
			_objectiveDestination = value;
		}
	}
}

class Intention {

	string _description;
	GameObject _intentObject;
	bool _concluded;
	bool _possible;

	public Intention(string description, GameObject intentObject){
		_description = description;
		_intentObject = intentObject;
		_concluded = false;
		_possible = true;
	}

	public string Description {
		get {
			return _description;
		}
		set {
			_description = value;
		}
	}

	public GameObject IntentObject {
		get {
			return _intentObject;
		}
		set {
			_intentObject = value;
		}
	}

	public bool Concluded {
		get {
			return _concluded;
		}
		set {
			_concluded = value;
		}
	}

	public bool Possible {
		get {
			return _possible;
		}
		set {
			_possible = value;
		}
	}
}

//Behaviour Class
public class ConverterVillainBehaviour : MonoBehaviour
{
	const float TASKFOCUSTIME = 10f;

	public Slider health;
	public Text outputConvertedCitizens;
	public Text outputRemainingCitizens;
	public int startingLife;
	int remainingCitizens;
	int convertedCitizens;
	int life;
	float lastDecisionTime;
	float time;
	float fieldOfViewAngle = 110f;           // Number of degrees, centred on forward, for the enemy see.
	bool isFollowing;
	bool inCombat;
	bool isAlive;

	GameObject followedObject;
	GameObject hero;
	HeroBehaviour heroBehaviour;

	Animator anim;
	NavMeshAgent agent;
	AnimatorStateInfo state;
	Transform[] destinations;

	List<Belief> beliefs = new List<Belief>();
	List<Desire> desires = new List<Desire>();
	Intention intention;

	// Use this for initialization
	void Start ()
	{
		Time.timeScale = 1;
		life = startingLife;
		health.value = startingLife;
		isAlive = true;
		inCombat = false;
		convertedCitizens = 0;
		outputConvertedCitizens.text = "" + convertedCitizens;
		hero = GameObject.FindGameObjectWithTag ("Hero");
		heroBehaviour = hero.GetComponent<HeroBehaviour> ();
		anim = GetComponent<Animator> ();
		agent = GetComponent<NavMeshAgent>();

		GameObject[] objects = GameObject.FindGameObjectsWithTag("Destination");
		int objLength = objects.Length;
		destinations = new Transform[objLength];
		for (int i = 0; i < objLength; i++)
			destinations [i] = objects [i].transform ;

		float min_dist = float.MaxValue;
		int index = 0;

		for (int i = 0; i < destinations.Length; i++) {
			if (destinations[i] != null && destinations [i].position.magnitude < min_dist) {
				index = i;
				min_dist = destinations [i].position.magnitude;
			}
		}

		followedObject = destinations [index].gameObject;
		agent.SetDestination (followedObject.transform.position);
		intention = new Intention ("Move Randomly", followedObject);
		isFollowing = true;
		Debug.Log ("Villain: " + agent.destination);
		anim.SetBool ("isWalking", true);
		state = anim.GetCurrentAnimatorStateInfo (0);

		lastDecisionTime = Time.time;
		time = Time.time;
	}
	
	// Update is called once per frame
	void Update ()
	{
		remainingCitizens = int.Parse (outputRemainingCitizens.text);
		beliefs = updateBeliefs(beliefs);

		if(intention.Possible && !intention.Concluded && ((Time.time - lastDecisionTime) < TASKFOCUSTIME)){
			executeIntention(intention);
		}
		else {
			desires = updateDesires(beliefs, desires);
			intention = updateIntention(beliefs, desires, intention);
			executeIntention(intention);
		}

		updateAnimation ();
	}

	void updateAnimation ()
	{
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
			Debug.Log (agent.destination);
			anim.SetBool ("isWalking", true);
			time = Time.time;
			return;
		}
	}

	void executeIntention(Intention intention){
		Debug.Log ("Executing Intention: " + intention.Description);
	}

	List<Belief> updateBeliefs(List<Belief> oldBeliefs){
		Debug.Log ("Updating Beliefs List");
		List<Belief> newBeliefs = new List<Belief> (oldBeliefs);

		return newBeliefs;
	}

	List<Desire> updateDesires(List<Belief> beliefs, List<Desire> oldDesires){
		Debug.Log ("Updating Desires List");
		List<Desire> newDesires = new List<Desire> (oldDesires);

		return newDesires;
	}

	Intention updateIntention(List<Belief> beliefs, List<Desire> desires, Intention oldIntention){
		Debug.Log ("Updating Intention");
		Intention newIntention = oldIntention;

		return newIntention;
	}
}

