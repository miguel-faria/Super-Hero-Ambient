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

	GameObject converter;
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
		intention = new Intention ((int)villainIntentionTypes.Move, "Move Randomly", followedObject, 
		                           Vector3.Distance(this.transform.position, followedObject.transform.position));
		recievedMessage = false;
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
		if (updatedIntention) {
			Debug.Log ("Striker Villain Executing Intention: " + intention.Description);
			updatedIntention = false;
		}
		
		if (intention.IntentObject == null) {
			intention.Possible = false;
			intention.DistanceToDestination = float.MaxValue;
			return;
		}
		
		if (intention.Type == (int)villainIntentionTypes.Attack) {
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
			if (desires.Count != 0){
				intention.Possible = false;
				intention.DistanceToDestination = float.MaxValue;
			}else {
				if(Time.time - time >= 5f)
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
		if (intention.Type != (int)villainIntentionTypes.Move)
			newIntention = oldIntention;
		else {
			newIntention = new Intention(oldIntention.Type, oldIntention.Description, oldIntention.IntentObject, float.MaxValue);
		}
		Vector3 currentPosition = this.transform.position;
		Desire chosenDesire = null;
		Belief originatingBelief = null;
		
		if (desires.Count != 0) {
			if (!inCombat && ((remainingCitizens + convertedCitizens + killedCitizens) < (Mathf.FloorToInt (0.4f * heroBehaviour.startingCitizens)))
			    && (existsBelief<SeeHeroBelief> ((int)villainBeliefTypes.See, beliefs)) && !HeroIsDead()) {
				newIntention = new Intention ((int)villainIntentionTypes.Attack, "Attack the Hero", hero,
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
						else if (desire.Type == (int)villainDesireTypes.DefendAgainstHero){
							newIntention = new Intention ((int)villainIntentionTypes.Attack, "Attack Hero", desire.SubjectObject,
							                              Vector3.Distance (currentPosition, desire.ObjectiveDestination));
								chosenDesire = desire;
						}
						else if (desire.Type == (int)villainDesireTypes.Flee){
							newIntention = new Intention ((int)villainIntentionTypes.Flee, "Flee from Hero", desire.SubjectObject,
							                              Vector3.Distance (currentPosition, desire.ObjectiveDestination));
							chosenDesire = desire;
						}
						else if (desire.Type == (int)villainDesireTypes.AttackCitizen){
							newIntention = new Intention ((int)villainIntentionTypes.Attack, "Attack Citizen", desire.SubjectObject,
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
			if(inSight(citizens[i]))
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
				} else if (percep.Type == (int) villainPerceptionTypes.Message){
					newBeliefs.Add (new ConverterInDangerBelief(percep.ObjectPercepted));
				}
			}
		}
		
		return newBeliefs;
	}
	
	List<Desire> updateDesires(List<Belief> beliefs, List<Desire> oldDesires){
		List<Desire> newDesires = new List<Desire> (oldDesires);
		
		foreach (Belief belief in beliefs) {
			if(!alreadyInDesires(belief, newDesires)){
				if(belief.Type == (int)villainBeliefTypes.Hear){
					newDesires.Add(new Desire((int)villainDesireTypes.Follow, "Follow Scream", belief.BeliefObject, 1));
				}else if((belief.Type == (int)villainBeliefTypes.See) && (belief is SeeCitizenBelief) && !alreadyConverted(belief.BeliefObject) && ImmuneCitizen(belief.BeliefObject)){
					newDesires.Add(new Desire((int)villainDesireTypes.AttackCitizen, "Attack Citizen", belief.BeliefObject, 1));
				}else if((belief.Type == (int)villainBeliefTypes.See) && (belief is SeeHeroBelief)){
					newDesires.Add(new Desire((int)villainDesireTypes.Flee, "Flee From Hero", belief.BeliefObject, 1));
				}else if(belief.Type == (int)villainBeliefTypes.Touching){
					newDesires.Add(new Desire((int)villainDesireTypes.DefendAgainstHero, "Fight the Hero", belief.BeliefObject, 1));
				}else if(belief.Type == (int)villainBeliefTypes.ConverterInDanger){
					newDesires.Add(new Desire((int)villainDesireTypes.DefendOtherVillain, "Fight the Hero", belief.BeliefObject, 1));
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
		return (distance <= Definitions.AOEAREA);
	}	
	
	bool alreadyConverted(GameObject citizen){
		if (citizen.tag.Equals ("Citizen")) {
			CitizenBehaviour citizenBehaviour = (CitizenBehaviour) citizen.GetComponent(typeof(CitizenBehaviour));
			return CitizenIsEvil(citizenBehaviour);
		} else {
			Debug.Log("Only citizens can be converted!");
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
		return (heroBehaviour.Life <= 0);
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
	}

	void Attack(GameObject attacked)
	{
		if (Time.time - attackTime >= 1f) {
			//UpdateAnimations (false, false, true, true, false);
			anim.SetBool("isAttacking", true);
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
			Debug.Log("Darth Vader := Took " + damage + " damage!");
		}else {
			inCombat = false;
			if(heroBehaviour.InCombat)
				heroBehaviour.InCombat = false;
			Debug.Log("I'm dead YO!!!!!!!!! - Darth Vader");
			anim.SetTrigger("Death");
			isAlive=false;
		}
	}
	
	void StopAttacking() 
	{
		UpdateAnimations (false, false, false, true, false);
	}
	
	void Follow(Vector3 destinationPos)
	{
		agent.Resume();
		agent.SetDestination (destinationPos);
		agent.speed = 8;
		anim.SetBool ("isWalking", true);
		//UpdateAnimations (true, false, false, true, false);
	}
	
	void RandomWalk()
	{
		agent.Resume();
		agent.SetDestination (destinations [Random.Range (0, destinations.Length)].position);
		agent.speed = 3.5f;
		//UpdateAnimations (true, false, false, true, false);
		anim.SetBool ("isWalking", true);
		time = Time.time;
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

