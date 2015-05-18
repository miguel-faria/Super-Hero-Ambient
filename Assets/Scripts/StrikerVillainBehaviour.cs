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
	bool recievedMessage;
	public bool isAlive;
	bool inCombat;
	bool isFollowing;
	bool isRoaming = false;
	bool updatedIntention;
	bool procNeed = true;
	bool attacking;
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

	Vector3 lastDestination;
	GameObject converter;
	GameObject hero;
	HeroBehaviour heroBehaviour;
	ConverterVillainBehaviour converterBehaviour;
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

	void onEnable(){
		CitizenBehaviour.OnAttack += HeardScream;
	}
	
	void onDisable(){
		CitizenBehaviour.OnAttack -= HeardScream;
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
		converter = GameObject.Find ("ConverterVillain");
		heroBehaviour = hero.GetComponent<HeroBehaviour> ();
		anim = GetComponent<Animator> ();
		agent = GetComponent<NavMeshAgent>();

		GameObject[] villains = GameObject.FindGameObjectsWithTag ("Villain");
		for (int i = 0; i < villains.Length; i++) {
			if(villains[i].name.Equals("ConverterVillain"))
				converterBehaviour = (ConverterVillainBehaviour)villains[i].GetComponent(typeof(ConverterVillainBehaviour));
		}

		GameObject[] objects = GameObject.FindGameObjectsWithTag("Destination");
		citizens = GameObject.FindGameObjectsWithTag ("Citizen");
		
		int objLength = objects.Length;
		destinations = new Transform[objLength];
		for (int i = 0; i < objLength; i++)
			destinations [i] = objects [i].transform ;
		
		followedObject = destinations [Random.Range(1,destinations.Length)].gameObject;
		agent.SetDestination (followedObject.transform.position);
		intention = new Intention ((int)villainIntentionTypes.Move, "Move Randomly", followedObject, 
		                           Vector3.Distance(this.transform.position, followedObject.transform.position));
		lastDestination = followedObject.transform.position;
		recievedMessage = false;
		isFollowing = true;
		updatedIntention = true;
		UpdateAnimations (true, false, true, false, false);
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
		if (isAlive) {
			citizens = GameObject.FindGameObjectsWithTag ("Citizen");
			beliefs = updateBeliefs (beliefs);
			desires = updateDesires (beliefs, desires);
			intention = updateIntention (beliefs, desires, intention);
			StartCoroutine (updateRoutine ());

			if (isRoaming) {

				float dist = Vector3.Distance (this.transform.position, this.GetComponent<NavMeshAgent> ().destination);
				//Debug.Log ("Wherever the wind takes me: " + dist);
				if (dist < 2)
					agent.SetDestination (destinations [Random.Range (0, destinations.Length)].position);

			}

		}

	}


	IEnumerator updateRoutine (){

		if (procNeed) {
			procNeed = false;
			remainingCitizens = int.Parse (outputRemainingCitizens.text);
			convertedCitizens = int.Parse (inputConvertedCitizens.text);
			beliefs = updateBeliefs(beliefs);
			
			if((intention != null) && intention.Possible && !intention.Concluded &&
			   ((Time.time - lastDecisionTime) < SuperHeroAmbient.Definitions.TASKFOCUSTIME)){
				executeIntention(intention);
			}
			else {
				desires = updateDesires(beliefs, desires);
				intention = updateIntention(beliefs, desires, intention);
				updatedIntention = true;
				executeIntention(intention);
				lastDecisionTime = Time.time;
			}

			if(attacking){
				yield return new WaitForSeconds(2);
				attacking=false;}

			procNeed = true;
		}


	}

	/***********************************************************************
	 ********************** BDI Architechture Methods **********************
	 ***********************************************************************/
	
	void executeIntention(Intention intention){
		if (intention.IntentObject == null) {
			intention.Possible = false;
			intention.DistanceToDestination = float.MaxValue;
			return;
		}

		if (updatedIntention) {
			Debug.Log ("Striker Villain Executing Intention: " + intention.Description);
			updatedIntention = false;
		}
				
		if (intention.Type == (int)villainIntentionTypes.Attack) {
			if (intention.IntentObject.CompareTag ("Hero")) {
				if (HeroIsDead ()) {
					intention.Concluded = true;
					inCombat = false;
					intention.DistanceToDestination = float.MaxValue;
				} else {
					if (Vector3.Distance (this.transform.position, intention.IntentObject.transform.position) <= Definitions.MAXTOUCHINGDISTANCE)
						Attack (hero);
					else {
						intention.Possible = false;
						inCombat = false;
						intention.DistanceToDestination = float.MaxValue;
					}
				}
			} else {
				GameObject citizen = intention.IntentObject;
				citizenSc = (CitizenBehaviour)citizen.GetComponent (typeof(CitizenBehaviour));
				if (CitizenIsDead (citizenSc)) {
					intention.Concluded = true;
					inCombat = false;
					intention.DistanceToDestination = float.MaxValue;
				} else {
					if (Vector3.Distance (this.transform.position, intention.IntentObject.transform.position) <= Definitions.MAXTOUCHINGDISTANCE)
						Attack (citizen);
					else {
						intention.Possible = false;
						inCombat = false;
						intention.DistanceToDestination = float.MaxValue;
					}
				}
			}
		} else if (intention.Type == (int)villainIntentionTypes.Approach) {
			if(intention.IntentObject.CompareTag("Hero")){
				if (HeroIsDead ()) {
					intention.Concluded = true;
					intention.DistanceToDestination = float.MaxValue;
				} else if (Vector3.Distance (this.transform.position, intention.IntentObject.transform.position) <= Definitions.MAXTOUCHINGDISTANCE){
					intention.Concluded = true;
					intention.DistanceToDestination = float.MaxValue;
				} else {
					Follow(hero.transform.position);
				}
			}else {
				GameObject citizen = intention.IntentObject;
				citizenSc = (CitizenBehaviour)citizen.GetComponent (typeof(CitizenBehaviour));
				if (CitizenIsDead (citizenSc)) {
					intention.Concluded = true;
					inCombat = false;
					intention.DistanceToDestination = float.MaxValue;
				} else if (Vector3.Distance (this.transform.position, intention.IntentObject.transform.position) <= Definitions.MAXTOUCHINGDISTANCE){
					intention.Concluded = true;
					intention.DistanceToDestination = float.MaxValue;
				} else {
					Follow(citizen.transform.position);
				}
			}

		} else if (intention.Type == (int)villainIntentionTypes.Flee) {
			if (!HeroInRange ()) {
				intention.Concluded = true;
				intention.DistanceToDestination = float.MaxValue;
				StopFollowing ();
			} else if (HeroIsDead ()) {
				intention.Possible = false;
				intention.DistanceToDestination = float.MaxValue;
				StopFollowing ();
			} else {
				Vector3 objective = hero.transform.position - transform.position;
				Follow (objective.normalized);
			}
		} else if (intention.Type == (int)villainIntentionTypes.FollowSound) {
			if ((intention.SoundOrigin != Vector3.zero) &&
			    (Vector3.Distance (this.transform.position, intention.IntentObject.transform.position) == 0)){
				intention.Concluded = true;
				intention.DistanceToDestination = float.MaxValue;
			}else{
				followedObject = intention.IntentObject;
				Follow(followedObject.transform.position);
			}
		} else if (intention.Type == (int)villainIntentionTypes.Move) {
			if (beliefs.Count != 0){
				intention.Possible = false;
				intention.DistanceToDestination = float.MaxValue;
			}else {
				if(Time.time - time >= 7.5f)
					RandomWalk();
				if(agent.transform.position.Equals(agent.destination))
					intention.Concluded = true;
			}
		} else {
			Debug.Log("Intention type not recognized!!");
		}
	}
	
	Intention updateIntention(List<Belief> beliefs, List<Desire> desires, Intention oldIntention){
		Intention newIntention;
		if (oldIntention.Type != (int)villainIntentionTypes.Move)
			newIntention = oldIntention;
		else {
			newIntention = new Intention(oldIntention.Type, oldIntention.Description, oldIntention.IntentObject, float.MaxValue);
		}
		Vector3 currentPosition = this.transform.position;
		Desire chosenDesire = null;
		Belief originatingBelief = null;
		
		if (desires.Count != 0) {
			if (!inCombat && ((remainingCitizens + convertedCitizens + killedCitizens) < (Mathf.FloorToInt (0.4f * heroBehaviour.startingCitizens)))
			    && (existsBelief<TouchHeroBelief> ((int)villainBeliefTypes.Touching, beliefs)) && !HeroIsDead()) {
				newIntention = new Intention ((int)villainIntentionTypes.Attack, "Attack the Hero", hero,
				                              Vector3.Distance (currentPosition, hero.transform.position));
			} else if (!inCombat && ((remainingCitizens + convertedCitizens + killedCitizens) < (Mathf.FloorToInt (0.4f * heroBehaviour.startingCitizens)))
	           				 && (existsBelief<SeeHeroBelief> ((int)villainBeliefTypes.See, beliefs)) && !HeroIsDead()) {
				newIntention = new Intention ((int)villainIntentionTypes.Approach, "Approach the Hero", hero,
				                              Vector3.Distance (currentPosition, hero.transform.position));
			} else if (inCombat && (life < 40) && existsDesire ((int)villainDesireTypes.Flee, desires) && (hero != null)) {
				newIntention = new Intention ((int)villainIntentionTypes.Flee, "Flee from Combat", hero, 
				                              Vector3.Distance (currentPosition, hero.transform.position));
			} else {
				foreach (Desire desire in desires) {
					 if (Vector3.Distance (this.transform.position, desire.ObjectiveDestination) < newIntention.DistanceToDestination) {
						if (desire.Type == (int)villainDesireTypes.DefendOtherVillain){
							newIntention = new Intention((int)villainIntentionTypes.Move, "Move To Villain", desire.SubjectObject,
							                             Vector3.Distance(currentPosition, desire.ObjectiveDestination));
							chosenDesire = desire;
						}
						else if (desire.Type == (int)villainDesireTypes.DefendAgainstHero && HeroInRange()){
							newIntention = new Intention ((int)villainIntentionTypes.Attack, "Attack Hero", desire.SubjectObject,
							                              Vector3.Distance (currentPosition, desire.ObjectiveDestination));
								chosenDesire = desire;
						}else if (desire.Type == (int)villainDesireTypes.DefendAgainstHero && !HeroInRange()){
							newIntention = new Intention ((int)villainIntentionTypes.Approach, "Approach Hero", desire.SubjectObject,
							                              Vector3.Distance (currentPosition, desire.ObjectiveDestination));
							chosenDesire = desire;
						}
						else if (desire.Type == (int)villainDesireTypes.AttackCitizen && CitizenInRange(desire.SubjectObject)){
							newIntention = new Intention ((int)villainIntentionTypes.Attack, "Attack Citizen", desire.SubjectObject,
							                               Vector3.Distance (currentPosition, desire.ObjectiveDestination));
							chosenDesire = desire;
						} 
						else if (desire.Type == (int)villainDesireTypes.AttackCitizen && !CitizenInRange(desire.SubjectObject)){
							newIntention = new Intention ((int)villainIntentionTypes.Approach, "Approach Citizen", desire.SubjectObject,
							                              Vector3.Distance (currentPosition, desire.ObjectiveDestination));
							chosenDesire = desire;
						} 
						else if (desire.Type == (int)villainDesireTypes.Follow){
							newIntention = new Intention ((int)villainIntentionTypes.FollowSound, "Follow Sound", desire.SubjectObject,
							                              Vector3.Distance (currentPosition, desire.ObjectiveDestination),
							                              desire.ObjectiveDestination);
							chosenDesire = desire;
						}
					}
				}
				if(chosenDesire != null){
					desires.Remove(chosenDesire);
					originatingBelief = findOriginatingBelief(chosenDesire, beliefs);
					if(originatingBelief != null)
						beliefs.Remove(originatingBelief);
				}
			}
		}
		else{
			GameObject dest = destinations [Random.Range(0, destinations.Length)].gameObject;
			newIntention = new Intention((int)villainIntentionTypes.Move, "Move Randomly", dest,
			                             Vector3.Distance(currentPosition, dest.transform.position));
		}
		
		return newIntention;
	}
	
	List<Perception> getCurrentPerceptions(){
		List<Perception> perceptions;
		if (screamPerceps != null) {
			perceptions = new List<Perception> (screamPerceps);
			screamPerceps = new List<Perception> ();
		} else {
			perceptions = new List<Perception> ();
		}
		GameObject[] citizens = GameObject.FindGameObjectsWithTag ("Citizen");
		
		for (int i = 0; i < citizens.Length; i++) {
			if(CitizenInRange(citizens[i])&& !alreadySaved(citizens[i]))
			  	perceptions.Add(new Perception(citizens[i], (int) villainPerceptionTypes.Touched));
			citizenSc = (CitizenBehaviour)citizens[i].GetComponent(typeof(CitizenBehaviour));
			if(inSight(citizens[i]) && !alreadySaved(citizens[i]) && !CitizenIsEvil(citizenSc) && !CitizenIsDead(citizenSc))
				perceptions.Add(new Perception(citizens[i], (int)villainPerceptionTypes.Saw));
		}

		if (recievedMessage) {
			perceptions.Add (new Perception (converter, (int)villainPerceptionTypes.Message));
			recievedMessage = false;
		}

		if (isTouching (hero)) {
			perceptions.Add (new Perception (hero, (int)villainPerceptionTypes.Touched));
		} else if (inSight (hero)) {
			perceptions.Add (new Perception (hero, (int)villainPerceptionTypes.Saw));
		}
		
		return perceptions;
	}
	
	List<Belief> updateBeliefs(List<Belief> oldBeliefs){
		List<Belief> emptyBeliefs = new List<Belief> ();
		foreach (Belief belief in oldBeliefs) {
			if(belief.BeliefObject == null) 
				emptyBeliefs.Remove(belief);
		}
		foreach (Belief belief in emptyBeliefs) {
			oldBeliefs.Remove (belief);
		}
		emptyBeliefs.Clear ();

		List<Belief> newBeliefs = new List<Belief> (oldBeliefs);
		List<Perception> perceptions = getCurrentPerceptions ();
		
		foreach (Perception percep in perceptions) {
			if(!alreadyInBeliefs(percep, newBeliefs)){
				if (percep.Type == (int)villainPerceptionTypes.Heard){
					newBeliefs.Add(new HearScreamBelief(percep.ObjectPercepted, percep.ObjectPercepted.transform.position));
				} else if (percep.Type == (int)villainPerceptionTypes.Saw){
					if(percep.Tag.Equals("Hero")){
						newBeliefs.Add(new SeeHeroBelief(percep.ObjectPercepted));
					}else if(percep.Tag.Equals("Citizen")){
						newBeliefs.Add(new SeeCitizenBelief(percep.ObjectPercepted));
					}
				} else if (percep.Type == (int)villainPerceptionTypes.Touched && percep.Tag.Equals("Hero")){
					newBeliefs.Add(new TouchHeroBelief(percep.ObjectPercepted));
				} else if (percep.Type == (int)villainPerceptionTypes.Touched && percep.Tag.Equals ("Citizen")){
					newBeliefs.Add (new TouchCitizenBelief(percep.ObjectPercepted));
				} else if (percep.Type == (int) villainPerceptionTypes.Message){
					newBeliefs.Add (new ConverterInDangerBelief(percep.ObjectPercepted));
				}
			}
		}
		
		return newBeliefs;
	}
	
	List<Desire> updateDesires(List<Belief> beliefs, List<Desire> oldDesires){
		List<Desire> emptyDesires = new List<Desire> ();
		foreach (Desire desire in oldDesires) {
			if(desire.SubjectObject == null) 
				emptyDesires.Remove(desire);
		}
		foreach (Desire desire in emptyDesires) {
			desires.Remove (desire);
		}
		emptyDesires.Clear ();

		List<Desire> newDesires = new List<Desire> (oldDesires);

		foreach (Belief belief in beliefs) {
			if(!alreadyInDesires(belief, newDesires) && belief.BeliefObject != null){
				if(belief.Type == (int)villainBeliefTypes.Hear){
					newDesires.Add(new Desire((int)villainDesireTypes.Follow, "Follow Scream", belief.BeliefObject, 0.35f));
				}else if((belief.Type == (int)villainBeliefTypes.See) && (belief is SeeCitizenBelief) &&
				         !alreadyConverted(belief.BeliefObject) && (ImmuneCitizen(belief.BeliefObject) || !converterBehaviour.IsAlive)){
					newDesires.Add(new Desire((int)villainDesireTypes.AttackCitizen, "Attack Citizen", belief.BeliefObject, 0.2f));
				}else if((belief.Type == (int)villainBeliefTypes.Touching) && (belief is SeeCitizenBelief) &&
				         !alreadyConverted(belief.BeliefObject) && (ImmuneCitizen(belief.BeliefObject) || !converterBehaviour.IsAlive)){
					newDesires.Add(new Desire((int)villainDesireTypes.AttackCitizen, "Attack Citizen", belief.BeliefObject, 0.2f));
				}else if((belief.Type == (int)villainBeliefTypes.See) && (belief is SeeHeroBelief)){
					newDesires.Add(new Desire((int)villainDesireTypes.DefendAgainstHero, "Fight The Hero", belief.BeliefObject, 0.2f));
				}else if(belief.Type == (int)villainBeliefTypes.Touching){
					newDesires.Add(new Desire((int)villainDesireTypes.DefendAgainstHero, "Fight the Hero", belief.BeliefObject, 0.25f));
				}else if(belief.Type == (int)villainBeliefTypes.ConverterInDanger){
					newDesires.Add(new Desire((int)villainDesireTypes.DefendOtherVillain, "Fight the Hero", belief.BeliefObject, 0.45f));
				}					
			}
		}
		
		return newDesires;
	}
	
	Belief findOriginatingBelief(Desire desire, List<Belief> beliefsList){
		Belief origBelief = null;
		foreach (Belief belief in beliefsList) {
			if((((belief.Type == (int)villainBeliefTypes.Hear) && (desire.Type == (int)villainDesireTypes.Follow)) || 
			    ((belief.Type == (int)villainBeliefTypes.See) && ((desire.Type == (int)villainDesireTypes.AttackCitizen) || 
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
			    && (belief.BeliefObject.Equals (perception.ObjectPercepted)) ||
			    ((belief.Type == (int)villainBeliefTypes.ConverterInDanger) && (perception.Type == (int)villainPerceptionTypes.Message))) {
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
			    ((belief.Type == (int)villainBeliefTypes.ConverterInDanger) && (desire.Type == (int)villainDesireTypes.DefendOtherVillain)) ||
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
	
	bool HeroInRange(){
		return ((Vector3.Distance(this.transform.position, hero.transform.position) < Definitions.HEROMAXVIEWDISTANCE) &&
		        (!InSightOfHero(this.gameObject)));
	}
	
	bool HeroInAttackRange(){
		return isTouching(hero);
	}
	
	bool InSightOfHero(GameObject other){
		float distance = Vector3.Distance (hero.transform.position, other.transform.position);
		Vector3 direction = other.transform.position - this.transform.position;
		float angle = Vector3.Angle (direction, this.transform.forward);	
		// If the angle between forward and where the player is, is less than half the angle of view...
		if (distance < Definitions.VILLAINMAXVIEWDISTANCE && angle < Definitions.FIELDOFVIEWANGLE * 0.5f) {
			return true;
		} else {
			return false;
		}	
	}
	
	bool CitizenInRange(GameObject citizen)
	{
		float distance = Vector3.Distance (this.transform.position, citizen.transform.position);
		return (distance <= Definitions.MAXTOUCHINGDISTANCE);
	}	

	bool alreadySaved(GameObject citizen){
		if (citizen.tag.Equals ("Citizen")) {
			CitizenBehaviour citizenBehaviour = (CitizenBehaviour)citizen.GetComponent (typeof(CitizenBehaviour));
			return citizenBehaviour.IsSaved();
		} else {
			Debug.Log ("Only immune citizens can be attacked!");
			return false;
		}
	}

	bool ImmuneCitizen(GameObject citizen){
		if (citizen.tag.Equals ("Citizen")) {
			CitizenBehaviour citizenBehaviour = (CitizenBehaviour)citizen.GetComponent (typeof(CitizenBehaviour));
			return CitizenIsImmune (citizenBehaviour);
		} else {
			Debug.Log ("Only immune citizens can be attacked!");
			return false;
		}
	}

	bool alreadyConverted(GameObject citizen){

		if (citizen == null)
			return false;

		if (citizen.tag.Equals ("Citizen")) {
			CitizenBehaviour citizenBehaviour = (CitizenBehaviour) citizen.GetComponent(typeof(CitizenBehaviour));
			return CitizenIsEvil(citizenBehaviour);
		} else {
			Debug.Log("Only citizens can be converted!");
			return false;
		}
	}

	bool CitizenIsImmune(CitizenBehaviour citizen) {
		return (citizen.IsImmune ());
	}

	bool CitizenIsEvil(CitizenBehaviour citizen){
		return (citizen.IsEvil ());
	}
	
	bool CitizenIsDead (CitizenBehaviour citizen)
	{
		return (citizen.Life <= 0);
	}
	
	bool HeroIsDead(){
		return !heroBehaviour.IsAlive;
	}
	
	bool inSight(GameObject other){
		// Create a vector from the enemy to the player and store the angle between it and forward.
		Vector3 direction = other.transform.position - this.transform.position;
		float distance = Vector3.Distance (this.transform.position, other.transform.position);
		float angle = Vector3.Angle (direction, this.transform.forward);
		if (other.CompareTag ("Citizen")) {			
			// If the angle between forward and where the player is, is less than half the angle of view...
			if (distance < Definitions.VILLAINMAXVIEWDISTANCE && angle < Definitions.FIELDOFVIEWANGLE * 0.5f) {
				return true;
			}
		}
		if(other.CompareTag("Hero")){
			if (distance > Definitions.MAXTOUCHINGDISTANCE &&
			    distance < Definitions.VILLAINMAXVIEWDISTANCE &&
			    angle < Definitions.FIELDOFVIEWANGLE * 0.5f){
				return true;
			}
		}
		
		return false;
	}
	
	bool isTouching(GameObject other){
		float distance = Vector3.Distance (this.transform.position, other.transform.position);
		if (distance <= Definitions.MAXTOUCHINGDISTANCE && distance >= 0.0f){
			return true;
		}
		return false;
	}

	void HeardScream(GameObject screamer){
		float distance = Vector3.Distance (this.transform.position, screamer.transform.position);
		if (distance > Definitions.AOEHEARINGAREA && distance < Definitions.VILLAINMAXHEARINGDISTANCE) {
			screamPerceps.Add(new Perception(screamer, (int)villainPerceptionTypes.Heard));
		}
	}

	public void ReceivedMessage()
	{
		recievedMessage = true;
	}
	

	
	/***********************************************************************
	 **************************** Actuator Methods *************************
	 ***********************************************************************/
	
	void StopFollowing()
	{
		if (followedObject.CompareTag ("Citizen"))
			citizenSc = null;
		followedObject = null;
		time = float.MaxValue;
		UpdateAnimations (false, false, true, false, false);
	}

	void Attack(GameObject attacked)
	{
		if (Time.time - attackTime >= 1f) {
			if(attacked.CompareTag("Citizen")){
				Debug.Log ("Attacking Citizen " + attacked.ToString());
				citizenSc.Attacked (this.gameObject);
				if(!citizenSc.IsAlive){
					UpdateAnimations (false, false, true, true, false);
					heroBehaviour.LevelDarkSide += 1;
					killedCitizens++;
					outputKilledCitizens.text = "" + killedCitizens;
					remainingCitizens--;
					outputRemainingCitizens.text = "" + remainingCitizens;
				}else
					UpdateAnimations (false, true, true, false, true);
			}else if(attacked.CompareTag("Hero")){
				Debug.Log ("Attacking Hero");
				if(HeroInAttackRange()){
					inCombat = true;
					UpdateAnimations (false, true, true, false, true);
				} else{
					inCombat = false;
					UpdateAnimations (false, false, true, false, false);
				}
				int damage;
				if(Random.Range(1,11) <= 9)
					damage = Random.Range(1,6);
				else
					damage = Random.Range(6,11);
				heroBehaviour.Attacked(this.gameObject, damage);
				if (HeroIsDead())
					UpdateAnimations (false, false, true, true, false);
			}
			attackTime = Time.time;
			attacking = true;
		}
		isRoaming = false;
	}
	
	public void Attacked(int damage){
		if (life > 0) {
			life -= damage;
			health.value = life;
			Debug.Log("Dormammu := Took " + damage + " damage!");
		}else {
			inCombat = false;
			if(heroBehaviour.InCombat){
				heroBehaviour.InCombat = false;
				heroBehaviour.StrikerVillainDead = true;
			}
			Debug.Log("I'm dead YO!!!!!!!!! - Dormammu");
			UpdateAnimations(false, false, false, false, false);
			anim.SetTrigger("Death");
			isAlive = false;
			Destroy(this.gameObject);
		}
	}
	
	void StopAttacking() 
	{
		UpdateAnimations (false, false, true, false, false);
	}
	
	void Follow(Vector3 destinationPos)
	{
		agent.Resume();
		agent.SetDestination (destinationPos);
		agent.speed = 8;
		isRoaming = false;
		UpdateAnimations (true, false, true, false, false);
	}
	
	void RandomWalk()
	{
		Vector3 newDestination = destinations [Random.Range (0, destinations.Length)].position;
		while(newDestination.Equals(lastDestination))
			newDestination = destinations [Random.Range (0, destinations.Length)].position;
		agent.Resume();
		agent.SetDestination (newDestination);
		lastDestination = newDestination;
		agent.speed = 3.5f;
		anim.SetFloat("Speed", 3.5f);
		UpdateAnimations (true, false, true, false, false);
		time = Time.time;
	}
	
	void UpdateAnimations(bool walking, bool combat, bool alive, bool laughing, bool attacking)
	{
		anim.SetBool ("isWalking", walking);
		anim.SetBool ("isAttacking", combat);
		anim.SetBool ("isAlive", alive);
		
		if(laughing)
			anim.SetTrigger ("Laugh");
		if (attacking)
			anim.SetTrigger ("Attacking");
	}
}

