using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using SuperHeroAmbient;

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
	float usedSuperSpeedTime;
	float timeLastPercep;
	bool isFollowing;
	bool inCombat;
	bool isAlive;
	bool updatedIntention;
	bool saveCrush;
	bool superSpeedCharged;
	bool usedSuperSpeed;
	bool noPerceptions;
	
	Animator anim;
	NavMeshAgent agent;
	AnimatorStateInfo state;
	Transform[] destinations;
	
	GameObject followedObject;
	GameObject crush;
	GameObject villain;
	
	List<Belief> beliefs = new List<Belief>();
	List<Belief> screams = new List<Belief> ();
	List<Desire> desires = new List<Desire>();
	Intention intention;

	void OnEnable(){
		CitizenBehaviour.OnAttack += HeardScream;
	}

	void OnDisable(){
		CitizenBehaviour.OnAttack -= HeardScream;
	}

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

		villain = null;
		crush = null;
		followedObject = destinations [index].gameObject;
		agent.SetDestination (followedObject.transform.position);
		intention = null;
		intention = new Intention ((int)heroIntentionTypes.Move, "Move Randomly", followedObject, 
		                           Vector3.Distance (this.transform.position, followedObject.transform.position));

		isFollowing = true;
		updatedIntention = true;
		saveCrush = false;
		superSpeedCharged = true;
		usedSuperSpeed = false;
		noPerceptions = false;
		anim.SetBool ("isWalking", true);
		state = anim.GetCurrentAnimatorStateInfo (0);

		lastDecisionTime = Time.time;
		time = Time.time;
		citizenInViewTime = float.MaxValue;
		usedSuperSpeedTime = float.MaxValue;
		timeLastPercep = float.MaxValue;

	}
	
	// Update is called once per frame
	void Update () {
		remainingCitizens = int.Parse (outputRemainingCitizens.text);
		killedCitizens = int.Parse (inputKilledCitizens.text);
		convertedCitizens = int.Parse (inputConvertedCitizens.text);
		beliefs = updateBeliefs (beliefs);

		if (intention != null){
			if (saveCrush) {

			} else if (screams != null) {

			} else if(beliefs != null && noPerceptions && ((Time.time - timeLastPercep) > 5.0f)){

			} else {
				if (intention.Possible && !intention.Concluded && ((Time.time - lastDecisionTime) < SuperHeroAmbient.Definitions.TASKFOCUSTIME)) {
					executeIntention (intention);
				} else {
					desires = updateDesires (beliefs, desires);
					intention = updateIntention (beliefs, desires, intention);
					updatedIntention = true;
					executeIntention (intention);
					lastDecisionTime = Time.time;
				}
			}
		}
	}

	/***********************************************************************
	 ********************** BDI Architechture Methods **********************
	 ***********************************************************************/

	void executeIntention(Intention intention){

	}

	Intention updateIntention(List<Belief> beliefs, List<Desire> desires, Intention oldIntention){
		Intention newIntention;
		if (intention.Type != (int)villainIntentionTypes.Move)
			newIntention = oldIntention;
		else {
			newIntention = new Intention(oldIntention.Type, oldIntention.Description, oldIntention.IntentObject, float.MaxValue);
		}

		return newIntention;
	}

	List<Desire> updateDesires(List<Belief> beliefs, List<Desire> oldDesires){
		List<Desire> newDesires = new List<Desire> (oldDesires);

		foreach (Belief belief in beliefs) {
			if(!alreadyInDesires(belief, newDesires)){
				if(belief.Type == (int)heroBeliefTypes.See){
					if(belief.BeliefObject.CompareTag("PowerUP")){
						newDesires.Add(new Desire((int)heroDesireTypes.Pick,"Pick Power UP", belief.BeliefObject, 0.4f));
					}else if(belief.BeliefObject.CompareTag("Citizen")){
						if(belief.BeliefObject.name.Equals("Crush")){
							newDesires.Add(new Desire((int)heroDesireTypes.HealCrush, "Heal Crush", belief.BeliefObject, 0.15f));
						}else{
							newDesires.Add(new Desire((int)heroDesireTypes.Save, "Save Citizen", belief.BeliefObject, 0.25f));
						}
					}else if(belief.BeliefObject.CompareTag("Villain")){

					}
				}else if(belief.Type == (int)heroBeliefTypes.Touching){

				}else if(belief.Type == (int)heroBeliefTypes.CanUseSuperSpeed){

				}
			}
		}

		return newDesires;
	}

	List<Belief> updateBeliefs(List<Belief> oldBeliefs){
		List<Belief> newBeliefs = new List<Belief> (oldBeliefs);
		List<Perception> perceptions = getCurrentPerceptions ();

		if (perceptions == null) {
			noPerceptions = true;
		} else {
			timeLastPercep = Time.time;
		}

		foreach (Perception percep in perceptions) {
			if(!alreadyInBeliefs(percep, newBeliefs)){
				if(percep.Type == (int)heroPerceptionType.Touched){
					newBeliefs.Add(new TouchVillainBelief(percep.ObjectPercepted));
				} else if(percep.Type == (int)heroPerceptionType.Saw){
					if(percep.Tag.Equals("Villain"))
						newBeliefs.Add(new SeeVillainBelief(percep.ObjectPercepted));
					else if(percep.Tag.Equals("Citizen")){
						if(percep.ObjectPercepted.name.Equals("Crush")){
							newBeliefs.Add(new SeeCrushBelief(percep.ObjectPercepted));
							if(!crush)
								crush = percep.ObjectPercepted;
						} else
							newBeliefs.Add(new SeeCitizenBelief(percep.ObjectPercepted));
					}
				} else if(percep.Type == (int)heroPerceptionType.SuperSpeedAvailable){
					newBeliefs.Add (new CanSuperSpeedBelief(this.gameObject));
				}
			}
		}

		return newBeliefs;
	}

	List<Perception> getCurrentPerceptions(){
		List<Perception> newPerceptions = new List<Perception> ();

		GameObject[] citizens = GameObject.FindGameObjectsWithTag ("Citizen");
		GameObject[] villains = GameObject.FindGameObjectsWithTag ("Villain");
		//GameObject[] powerUPs = GameObject.FindGameObjectsWithTag ("PowerUP");

		for (int i = 0; i < citizens.Length; i++) {
			CitizenBehaviour citizenSC = (CitizenBehaviour)citizens[i].GetComponent(typeof (CitizenBehaviour));
			if(InSight(citizens[i].gameObject) && ((!citizenSC.IsEvil()) || (citizens[i].name.Equals("Crush"))))
				newPerceptions.Add(new Perception(citizens[i].gameObject, (int)heroPerceptionType.Saw));
		}

		for (int i = 0; i < villains.Length; i++) {
			if(IsTouching(villains[i].gameObject)){
				newPerceptions.Add(new Perception(villains[i].gameObject, (int)heroPerceptionType.Touched));
			}else if(InSight(villains[i].gameObject))
				newPerceptions.Add(new Perception(villains[i].gameObject, (int)heroPerceptionType.Saw));
		}

		/*for (int i = 0; i < powerUPs.Length; i++) {
			if(InSight(powerUPs[i].gameObject))
				newPerceptions.Add(new Perception(powerUPs[i].gameObject, (int)heroPerceptionType.Saw));
		}*/

		if (!superSpeedCharged && ((Time.time - usedSuperSpeedTime) > Definitions.SUPERSPEEDCOOLDOWN))
			newPerceptions.Add (new Perception (this.gameObject, (int)heroPerceptionType.SuperSpeedAvailable));

		if (usedSuperSpeed && ((Time.time - usedSuperSpeedTime) > Definitions.SUPERSPEEDMAXTIME)) {
			usedSuperSpeed = false;
			agent.speed = 3.5f;
			anim.SetFloat("Speed", 3.5f);
		}

		return newPerceptions;
	}

	Belief findOriginatingBelief(Desire desire, List<Belief> beliefsList){
		Belief origBelief = null;
		foreach (Belief belief in beliefsList) {
			if((((belief.Type == (int)villainBeliefTypes.Hear) && (desire.Type == (int)villainDesireTypes.Follow)) || 
			    ((belief.Type == (int)villainBeliefTypes.See) && ((desire.Type == (int)villainDesireTypes.Convert) || 
			                                                  (desire.Type == (int)villainDesireTypes.Flee))) ||
			    ((belief.Type == (int)villainBeliefTypes.Touching) && (desire.Type == (int)villainDesireTypes.DefendAgainstHero)))
			   && (desire.SubjectObject.Equals(belief.BeliefObject))){
				origBelief = belief;
			}
		}
		return origBelief;
	}
	
	bool alreadyInBeliefs(Perception perception, List<Belief> beliefsList){
		foreach (Belief belief in beliefsList) {
			if ((((belief.Type == (int)villainBeliefTypes.Hear) && (perception.Type == (int)villainPerceptionTypes.Heard)) ||
			     ((belief.Type == (int)villainBeliefTypes.See) && (perception.Type == (int)villainPerceptionTypes.Saw)) ||
			     ((belief.Type == (int)villainBeliefTypes.Touching) && (perception.Type == (int)villainPerceptionTypes.Touched)))
			    && (belief.BeliefObject.Equals (perception.ObjectPercepted))) {
				return true;
			}
		}
		return false;
	}
	
	bool alreadyInDesires(Belief belief, List<Desire> desiresList){
		foreach(Desire desire in desiresList){
			if((((belief.Type == (int)villainBeliefTypes.Hear) && (desire.Type == (int)villainDesireTypes.Follow)) || 
			    ((belief.Type == (int)villainBeliefTypes.See) && ((desire.Type == (int)villainDesireTypes.Convert) || 
			                                                  (desire.Type == (int)villainDesireTypes.Flee))) ||
			    ((belief.Type == (int)villainBeliefTypes.Touching) && (desire.Type == (int)villainDesireTypes.DefendAgainstHero)))
			   && (desire.SubjectObject.Equals(belief.BeliefObject))){
				return true;
			}
		}
		return false;
	}
	
	bool existsDesire(int desireType, List<Desire> desiresList){
		
		foreach(Desire desire in desiresList){
			if(desire.Type == desireType)
				return true;
		}
		
		return false;
	}
	
	bool existsBelief<T>(int beliefType, List<Belief> beliefList){
		
		foreach(Belief belief in beliefList){
			if((belief.Type == beliefType) && (belief is T))
				return true;
		}
		
		return false;
	}

	/***********************************************************************
	 ***************************** Sensor Methods **************************
	 ***********************************************************************/

	bool VillainInRangeSuperSenses(GameObject villain){
		return Vector3.Distance(this.transform.position, villain.transform.position) < Definitions.SUPERSENSESAREA;
	}

	bool InSight(GameObject other){
		Vector3 direction = other.transform.position - this.transform.position;
		float distance = Vector3.Distance (this.transform.position, other.transform.position);
		float angle = Vector3.Angle (direction, this.transform.forward);
		if (other.CompareTag ("Citizen")/* || other.CompareTag ("PowerUp")*/) {
			return ((distance < Definitions.HEROMAXVIEWDISTANCE) &&
				(angle < Definitions.FIELDOFVIEWANGLE * 0.5f));
		} else if (other.CompareTag ("Villain")) {
			return ((distance < Definitions.HEROMAXVIEWDISTANCE) &&
				(distance > Definitions.MAXTOUCHINGDISTANCE) &&
				(angle < Definitions.FIELDOFVIEWANGLE * 0.5f));
		} else {
			return false;
		}

	}

	bool IsTouching(GameObject other){
		float distance = Vector3.Distance (this.transform.position, other.transform.position);
		return ((distance <= Definitions.MAXTOUCHINGDISTANCE) && (distance >= 0.0f));
	}

	void HeardScream(GameObject screamer){
		float distance = Vector3.Distance (this.transform.position, screamer.transform.position);
		if (distance > Definitions.AOEHEARINGAREA && distance < Definitions.HEROMAXHEARINGDISTANCE) {
			if((crush != null) && (screamer.name.Equals("Crush"))){
				saveCrush = true;
			}else{
				Perception screamPercep = new Perception(screamer, (int)heroPerceptionType.Heard);
				if(!alreadyInBeliefs(screamPercep, screams)){
					screams.Add(new HearScreamBelief(screamer, screamer.transform.position));
				}
			}
			noPerceptions = false;
		}
	}

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
