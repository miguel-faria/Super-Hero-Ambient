using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using SuperHeroAmbient;

enum perceptionType {Saw, Heard, Touched}
enum beliefTypes {See, Hear, Touching}
enum desireTypes {Save, Follow, Fight, Flee}
enum intentionTypes {Move, FollowSound, Attack, Convert, Flee, KillHero, AskHelp}

public class HeroBehaviour : MonoBehaviour {

	public Slider health;
	public Text outputSavedCitizens;
	public Text outputRemainingCitizens;
	public Text inputKilledCitizens;
	public Text inputConvertedCitizens;
	public int startingLife;
	public int startingCitizens;
	int remainingCitizens;
	int savedCitizens;
	int killedCitizens;
	int convertedCitizens;
	int life;
	float lastDecisionTime;
	float time;
	float attackTime;
	float citizenInViewTime;
	float fieldOfViewAngle = 110f;           // Number of degrees, centred on forward, for the enemy see.
	bool isFollowing;
	bool inCombat;
	bool isAlive;
	bool updatedIntention;
	
	Animator anim;
	NavMeshAgent agent;
	AnimatorStateInfo state;
	Transform[] destinations;
	
	GameObject followedObject;
	GameObject hero;
	HeroBehaviour heroBehaviour;

	List<Perception> screamPerceps = new List<Perception>();
	List<Belief> beliefs = new List<Belief>();
	List<Desire> desires = new List<Desire>();
	Intention intention;

	//Getters and Setters
	public int Life {
		get {
			return life;
		}
		set {
			life = value;
		}
	}

	public bool InCombat {
		get {
			return inCombat;
		}
		set {
			inCombat = value;
		}
	}

	// Use this for initialization
	void Start () {
	
		outputRemainingCitizens.text = "" + startingCitizens;
		health.value = startingLife;
		life = startingLife;
		remainingCitizens = startingCitizens;
		savedCitizens = 0;
		outputSavedCitizens.text = "" + savedCitizens;
		attackTime = float.MinValue;
		anim = GetComponent<Animator> ();
		agent = GetComponent<NavMeshAgent>();

		GameObject[] objects = GameObject.FindGameObjectsWithTag("Destination");
		destinations = new Transform[objects.Length];
		for (int i = 0; i < objects.Length; i++) 
			destinations [i] = objects [i].transform;
		
		float min_dist = float.MaxValue;
		int index = -1;
		
		for (int i = 0; i < destinations.Length; i++) {
			if (destinations [i].position.magnitude < min_dist) {
				index = i;
				min_dist = destinations [i].position.magnitude;
			}
		}

		followedObject = destinations [index].gameObject;
		agent.SetDestination (followedObject.transform.position);
		intention = new Intention ((int)coverterIntentionTypes.Move, "Move Randomly", followedObject, 
		                           Vector3.Distance (this.transform.position, followedObject.transform.position));

		isFollowing = true;
		updatedIntention = true;
		anim.SetBool ("isWalking", true);
		state = anim.GetCurrentAnimatorStateInfo (0);

		lastDecisionTime = Time.time;
		time = Time.time;
		citizenInViewTime = float.MaxValue;
	}
	
	// Update is called once per frame
	void Update () {
		remainingCitizens = int.Parse (outputRemainingCitizens.text);
		killedCitizens = int.Parse (inputKilledCitizens.text);
		convertedCitizens = int.Parse (inputConvertedCitizens.text);
		beliefs = updateBeliefs(beliefs);

		if(intention.Possible && !intention.Concluded && ((Time.time - lastDecisionTime) < SuperHeroAmbient.Definitions.TASKFOCUSTIME)){
			executeIntention(intention);
		}
		else {
			desires = updateDesires(beliefs, desires);
			intention = updateIntention(beliefs, desires, intention);
			updatedIntention = true;
			executeIntention(intention);
			lastDecisionTime = Time.time;
		}
	}

	/***********************************************************************
	 ********************** BDI Architechture Methods **********************
	 ***********************************************************************/

	void executeIntention(Intention intention){

	}

	Intention updateIntention(List<Belief> beliefs, List<Desire> desires, Intention oldIntention){
		Intention newIntention;
		if (intention.Type != (int)coverterIntentionTypes.Move)
			newIntention = oldIntention;
		else {
			newIntention = new Intention(oldIntention.Type, oldIntention.Description, oldIntention.IntentObject, float.MaxValue);
		}

		return newIntention;
	}

	List<Desire> updateDesires(List<Belief> beliefs, List<Desire> oldDesires){
		List<Desire> newDesires = new List<Desire> (oldDesires);

		return newDesires;
	}

	List<Belief> updateBeliefs(List<Belief> oldBeliefs){
		List<Belief> newBeliefs = new List<Belief> (oldBeliefs);

		return newBeliefs;
	}

	List<Perception> getCurrentPerceptions(){
		List<Perception> newPerceptions = new List<Perception> (screamPerceps);

		return newPerceptions;
	}

	/***********************************************************************
	 ***************************** Sensor Methods **************************
	 ***********************************************************************/


	/***********************************************************************
	 **************************** Actuator Methods *************************
	 ***********************************************************************/

	public void Attack(GameObject attacker, string attackerType){
		ConverterVillainBehaviour convVillain;
		StrikerVillainBehaviour strikeVillain;
		int damage;
		if((Time.time - attackTime) >= 1.0f){
			//TODO: update animation
			if(Random.Range(1,11) > 6)
				damage = Random.Range(1,6);
			else
				damage = Random.Range(6,11);
			if (attackerType.Equals("Converter")) {
				convVillain = (ConverterVillainBehaviour)attacker.GetComponent (typeof(ConverterVillainBehaviour));
				convVillain.Attacked(damage);
			}else if(attackerType.Equals("Striker")){
				strikeVillain = (StrikerVillainBehaviour)attacker.GetComponent (typeof(StrikerVillainBehaviour));
				strikeVillain.Attacked(damage);
			}
			attackTime = Time.time;
		}
	}
	
	public void Attacked(GameObject attacker, string attackerType, int damage) {
		ConverterVillainBehaviour convVillain;
		StrikerVillainBehaviour strikeVillain;
		if (life > 0) {
			life -= damage;
			health.value = life;
			Debug.Log("Bayamax := Took " + damage + " damage!");
		}else {
			inCombat = false;			
			if (attackerType.Equals("Converter")) {
				convVillain = (ConverterVillainBehaviour)attacker.GetComponent (typeof(ConverterVillainBehaviour));
				convVillain.InCombat = false;
			}else if(attackerType.Equals("Striker")){
				strikeVillain = (StrikerVillainBehaviour)attacker.GetComponent (typeof(StrikerVillainBehaviour));
				strikeVillain.InCombat = false;
			}
			Debug.Log("I'm dead YO!!!!!!!!! - Hero");
			Destroy(gameObject);
		}

	}
}
