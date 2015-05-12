using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using SuperHeroAmbient;

public class StrikerVillainBehaviour : MonoBehaviour
{
	public Slider health;
	public Text outputKilledCitizens;
	public Text outputRemainingCitizens;
	public Text inputConvertedCitizens;
	public int startingLife;
	int life;
	int killedCitizens;
	int convertedCitizens;
	int remainingCitizens;
	bool isAlive;
	bool inCombat;
	bool isFollowing;
	bool updatedIntention;
	float lastDecisionTime;
	float time;
	float citizenInViewTime;
	float convertTime;
	float attackTime;
	float askForHelpTime;

	Animator anim;
	NavMeshAgent agent;
	AnimatorStateInfo state;
	Transform[] destinations;

	GameObject hero;
	HeroBehaviour heroBehaviour;
	GameObject[] citizens;
	CitizenBehaviour citizenSc;
	GameObject followedObject;

	List<Perception> screamPerceps = new List<Perception>();
	List<Belief> beliefs = new List<Belief>();
	List<Desire> desires = new List<Desire>();
	Intention intention;

	public bool InCombat {
		get {
			return inCombat;
		}
		set {
			inCombat = value;
		}
	}

	// Use this for initialization
	void Start ()
	{
		Time.timeScale = 1;
		life = startingLife;
		health.value = startingLife;
		isAlive = true;
		inCombat = false;
		killedCitizens = 0;
		outputKilledCitizens.text = "" + killedCitizens;
		hero = GameObject.FindGameObjectWithTag ("Hero");
		heroBehaviour = hero.GetComponent<HeroBehaviour> ();
		anim = GetComponent<Animator> ();
		agent = GetComponent<NavMeshAgent>();
		convertedCitizens = int.Parse(inputConvertedCitizens.text);
		
		GameObject[] objects = GameObject.FindGameObjectsWithTag("Destination");
		citizens = GameObject.FindGameObjectsWithTag ("Citizen");
		
		int objLength = objects.Length;
		destinations = new Transform[objLength];
		for (int i = 0; i < objLength; i++)
			destinations [i] = objects [i].transform ;
		
		float min_dist = float.MaxValue;
		int index = 0;
		
		for (int i = 0; i < destinations.Length; i++) {
			if (destinations [i].position.magnitude < min_dist) {
				index = i;
				min_dist = destinations [i].position.magnitude;
			}
		}
		
		followedObject = destinations [index].gameObject;
		agent.SetDestination (followedObject.transform.position);
		intention = new Intention ((int)coverterIntentionTypes.Move, "Move Randomly", followedObject, 
		                           Vector3.Distance(this.transform.position, followedObject.transform.position));
		isFollowing = true;
		updatedIntention = true;
		anim.SetBool ("isWalking", true);
		state = anim.GetCurrentAnimatorStateInfo (0);
		
		lastDecisionTime = Time.time;
		time = Time.time;
		citizenInViewTime = float.MaxValue;
		convertTime = float.MinValue;
		attackTime = float.MinValue;
		askForHelpTime = float.MinValue;
	}
	
	// Update is called once per frame
	void Update ()
	{
		remainingCitizens = int.Parse (outputRemainingCitizens.text);
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

	void Attack(GameObject attacked)
	{
		if (Time.time - attackTime >= 1f) {
			//TODO: update animation
			if(attacked.CompareTag("Citizen")){
				Debug.Log ("Attacking Citizen " + attacked.ToString());
				citizenSc.Attacked ();
			}else if(attacked.CompareTag("Hero")){
				Debug.Log ("Attacking Hero");
				int damage;
				if(Random.Range(1,11) < 9)
					damage = Random.Range(1,6);
				else
					damage = Random.Range(6,11);
				heroBehaviour.Attacked(this.gameObject, "Converter", damage);
			}
			attackTime = Time.time;
		}
	}

	public void Attacked(int damage){
		if (life > 0) {
			life -= damage;
			health.value = life;
			Debug.Log("Dormammu := Took " + damage + " damage!");
		}else {
			inCombat = false;
			if(heroBehaviour.InCombat)
				heroBehaviour.InCombat = false;;
			Debug.Log("I'm dead YO!!!!!!!!! - Darth Vader");
			Destroy(gameObject);
		}
	}
}

