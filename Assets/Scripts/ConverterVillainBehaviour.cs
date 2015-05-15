using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using SuperHeroAmbient;

//Behaviour Class
public class ConverterVillainBehaviour : MonoBehaviour
{
	public Slider health;
	public Text outputConvertedCitizens;
	public Text outputRemainingCitizens;
	public Text inputKilledCitizens;
	public int startingLife;
	int remainingCitizens;
	int convertedCitizens;
	int killedCitizens;
	int life;
	float lastDecisionTime;
	float time;
	float citizenInViewTime;
	float convertTime;
	float attackTime;
	float askForHelpTime;
	bool isFollowing;
	bool inCombat;
	bool isAlive;
	bool citizenInView = false;
	bool updatedIntention;
	bool procNeed = true;
	bool converting;
	bool isRoaming;

	GameObject followedObject;
	GameObject hero;
	HeroBehaviour heroBehaviour;
	Vector3 citizenPos = new Vector3();
	GameObject[] citizens = null;
	CitizenBehaviour citizenSc = null;

	Animator anim;
	NavMeshAgent agent;
	AnimatorStateInfo state;
	Transform[] destinations;

	List<Perception> screamPerceps = new List<Perception>();
	List<Belief> beliefs = new List<Belief>();
	List<Desire> desires = new List<Desire>();
	Intention intention;

	//Event Methods
	public delegate void AskForHelp(GameObject asker);
	public static event AskForHelp OnAskHelp;

	//Getters and Setters

	public bool IsFollowing {
		get {
			return isFollowing;
		}
		set {
			isFollowing = value;
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

	public bool IsAlive {
		get {
			return isAlive;
		}
		set {
			isAlive = value;
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
		convertedCitizens = 0;
		outputConvertedCitizens.text = "" + convertedCitizens;
		hero = GameObject.FindGameObjectWithTag ("Hero");
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
		citizens = GameObject.FindGameObjectsWithTag ("Citizen");
		beliefs = updateBeliefs (beliefs);
		desires = updateDesires (beliefs, desires);


		StartCoroutine (UpdateBehaviour ());
		if (intention.Type == 0)
			isRoaming = true; else isRoaming = false;

		if (isRoaming) {
			
			float dist = Vector3.Distance(this.transform.position, this.GetComponent<NavMeshAgent>().destination);
			//Debug.Log ("Wherever the wind takes me: " + dist);
			if (dist < 2) agent.SetDestination (destinations [Random.Range (0, destinations.Length)].position);
			
		}

		//updateAnimation (intention.IntentObject);
	}

	IEnumerator UpdateBehaviour (){


		if (isAlive && procNeed) {
			procNeed=false;
			remainingCitizens = int.Parse (outputRemainingCitizens.text);
			killedCitizens = int.Parse (inputKilledCitizens.text);
			beliefs = updateBeliefs (beliefs);
			
			if (intention.Possible && !intention.Concluded && ((Time.time - lastDecisionTime) < SuperHeroAmbient.Definitions.TASKFOCUSTIME)) {
				executeIntention (intention);
			} else {
				desires = updateDesires (beliefs, desires);
				intention = updateIntention (beliefs, desires, intention);
				updatedIntention = true;
				executeIntention (intention);
				lastDecisionTime = Time.time;
			}
			//Debug.Log ("Acessing");
			if(converting){
				yield return new WaitForSeconds(2);
				converting=false;}
			procNeed = true;
		}
		//Debug.Log ("Acessing");

	}

	/*void updateAnimation (GameObject destinationObject)
	{
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
			Convert ();
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
	}*/


	/***********************************************************************
	 ********************** BDI Architechture Methods **********************
	 ***********************************************************************/

	void executeIntention(Intention intention){
		if (updatedIntention) {
			Debug.Log ("Coverter Villain Executing Intention: " + intention.Description);
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
		} else if (intention.Type == (int)villainIntentionTypes.Convert) {
			citizenSc = (CitizenBehaviour)intention.IntentObject.GetComponent (typeof(CitizenBehaviour));
			citizenPos = intention.IntentObject.transform.position;
			followedObject = intention.IntentObject;
			if (CitizenInRange (intention.IntentObject)) {
				if (CitizenIsEvil (citizenSc)) {
					intention.Concluded = true;
					intention.DistanceToDestination = float.MaxValue;
				} else if (CitizenIsDead (citizenSc)) {
					intention.Possible = false;
					intention.DistanceToDestination = float.MaxValue;
				} else if (citizenSc.IsImmune()) {
					intention.Possible = false;
					intention.DistanceToDestination = float.MaxValue;
				} else if (((Time.time - convertTime) >= 1.0f)) {
					if(Random.Range(1,11) > 6){
						Debug.Log("Failed Convertion");
						Convert (citizenSc, false);
					} else{
						Convert (citizenSc, true);
						converting=true;
						for (int i = 0; i < citizens.Length; i++) {
							if ((citizens [i] != intention.IntentObject) && (CitizenInRange (citizens [i])) && 
								(!CitizenIsEvil ((CitizenBehaviour)citizens [i].GetComponent (typeof(CitizenBehaviour))))) {
								if(Random.Range(1,11) > 6){
									Debug.Log("Failed Convertion");
									Convert ((CitizenBehaviour)citizens [i].GetComponent (typeof(CitizenBehaviour)), false);
								} else{
									Convert ((CitizenBehaviour)citizens [i].GetComponent (typeof(CitizenBehaviour)), true);
								}
							}
						}
						convertTime = Time.time;
					}
				}
			} else {
				if (CitizenIsDead (citizenSc)) {
					intention.Possible = false;
					intention.DistanceToDestination = float.MaxValue;
				} else {
					Follow (citizenPos);
				}
			}
		} else if (intention.Type == (int)villainIntentionTypes.Flee) {
			if (!HeroInRange ()) {
				intention.Concluded = true;
				intention.DistanceToDestination = float.MaxValue;
				agent.Stop();
				followedObject = null;
			} else if (HeroIsDead ()) {
				intention.Possible = false;
				intention.DistanceToDestination = float.MaxValue;
				agent.Stop();
				followedObject = null;
			} else {
				runFrom(hero);
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
		} else if(intention.Type == (int)villainIntentionTypes.KillHero) { 
			if(HeroIsDead()){
				intention.Concluded = true;
				intention.DistanceToDestination = float.MaxValue;
				if(inCombat)
					inCombat = false;
			} else if(HeroInAttackRange()){
				Attack(hero);
				inCombat = true;
			}else{
				followedObject = intention.IntentObject;
				Follow(followedObject.transform.position);
			}
		} else if(intention.Type == (int)villainIntentionTypes.AskHelp) { 
			if (!HeroInRange ()) {
				intention.Concluded = true;
				intention.DistanceToDestination = float.MaxValue;
				StopFollowing ();
			} else if (HeroIsDead ()) {
				intention.Possible = false;
				intention.DistanceToDestination = float.MaxValue;
				StopFollowing ();
			} else {
				if((OnAskHelp != null) && ((Time.time - askForHelpTime) > 0.5f)){
					OnAskHelp(this.gameObject);
					askForHelpTime = Time.time;
				}
				Vector3 objective = hero.transform.position - transform.position;
				Follow (objective.normalized);
				if(inCombat)
					inCombat = false;
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
		
		if (isTouching (hero)) {
			perceptions.Add (new Perception (hero, (int)villainPerceptionTypes.Touched));
		} else if (inSight (hero)) {
			perceptions.Add (new Perception (hero, (int)villainPerceptionTypes.Saw));
		}

		return perceptions;
	}

	List<Belief> updateBeliefs(List<Belief> oldBeliefs){

		foreach (Belief belief in oldBeliefs) {
			
			

			if(belief.BeliefObject == null) 
				oldBeliefs.Remove(belief);
			
			
		}

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
					newDesires.Add(new Desire((int)villainDesireTypes.Follow, "Follow Scream", belief.BeliefObject, 0.25f));
				}else if((belief.Type == (int)villainBeliefTypes.See) && (belief is SeeCitizenBelief) && !alreadyConverted(belief.BeliefObject) && !AlreadyImmune (belief.BeliefObject)){
					newDesires.Add(new Desire((int)villainDesireTypes.Convert, "Convert Citizen", belief.BeliefObject, 0.25f));
				}else if((belief.Type == (int)villainBeliefTypes.See) && (belief is SeeHeroBelief)){
					newDesires.Add(new Desire((int)villainDesireTypes.Flee, "Flee from Hero", belief.BeliefObject, 0.3f));
				}else if(belief.Type == (int)villainBeliefTypes.Touching){
					newDesires.Add(new Desire((int)villainDesireTypes.DefendAgainstHero, "Fight the Hero", belief.BeliefObject, 0.2f));
               	}
			}
		}

		return newDesires;
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
				newIntention = new Intention ((int)villainIntentionTypes.KillHero, "Kill the Hero", hero,
				                              Vector3.Distance (currentPosition, hero.transform.position));
			} else if (inCombat && (life < 40) && existsDesire ((int)villainDesireTypes.Flee, desires) && (hero != null)) {
				newIntention = new Intention ((int)villainIntentionTypes.AskHelp, "Flee from Combat and Ask for Help", hero, 
				                              Vector3.Distance (currentPosition, hero.transform.position));
			} else {
				foreach (Desire desire in desires) {
					if (Vector3.Distance (this.transform.position, desire.ObjectiveDestination) * desire.PreferenceFactor < newIntention.DistanceToDestination) {
						if (desire.Type == (int)villainDesireTypes.Convert){
							newIntention = new Intention ((int)villainIntentionTypes.Convert, "Convert Citizen", desire.SubjectObject,
							                              Vector3.Distance (currentPosition, desire.ObjectiveDestination));
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

	/***************************************************************
	 ************************* Sensor Methods **********************
	 ***************************************************************/

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

	bool AlreadyImmune (GameObject citizen) {
		if (citizen.tag.Equals ("Citizen")) {
			CitizenBehaviour citizenBehaviour = (CitizenBehaviour) citizen.GetComponent(typeof(CitizenBehaviour));
			return citizenBehaviour.IsImmune();
		} else {
			Debug.Log("Only citizens can be converted!");
			return false;
		}
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

	bool CitizenIsEvil(CitizenBehaviour citizen){
		if (citizen.IsEvil())
			return true;
		else {
			return false;
		}
	}

	bool CitizenIsDead (CitizenBehaviour citizen)
	{
		if (citizen.Life <= 0)
			return true;
		else {
			return false;
		}
	}

	bool HeroIsDead(){
		if (heroBehaviour.Life <= 0)
			return true;
		else {
			return false;
		}
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


	/***************************************************************
	 ************************ Actuator Methods *********************
	 ***************************************************************/
	/*void KilledCitizen ()
	{
		// Laugh
		agent.Stop();
		anim.SetTrigger ("Laugh");
		anim.SetBool("isWalking",false);
		
		citizenInView = false;
		noPerception = true;
		citizenSc = null;
		UpdateAnimations (false, false, false, true, false);
	}*/
	
	
	void StopFollowing()
	{
		if (followedObject.CompareTag ("Citizen"))
			citizenSc = null;
		followedObject = null;
		time = float.MaxValue;
	}
	
	void Convert (CitizenBehaviour citizen, bool success)
	{
		citizen.Converted (success);
		if (success) {
			if (citizen.IsEvil ()) {
				remainingCitizens--;
				outputRemainingCitizens.text = "" + remainingCitizens;
				convertedCitizens++;
				outputConvertedCitizens.text = "" + convertedCitizens;
			}
			anim.SetTrigger ("Convert");
			agent.Stop ();

			//UpdateAnimations (false, false, false, true, true);
		}

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
				heroBehaviour.Attacked(this.gameObject, damage);
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
		anim.SetBool ("isRunning", true);

		//UpdateAnimations (true, false, false, true, false);
	}
	
	void RandomWalk()
	{
		agent.Resume();
		agent.SetDestination (destinations [Random.Range (0, destinations.Length)].position);
		agent.speed = 3.5f;
		//UpdateAnimations (true, false, false, true, false);
		anim.SetBool ("isWalking", true);
		anim.SetBool ("isRunning", false);
		time = Time.time;

	}


	void runFrom(GameObject run){

		Vector3 objective = run.transform.position - transform.position;
		agent.SetDestination(objective.normalized);


	}


//	void OnTriggerStay(Collider other)
//	{
//		if (other.gameObject.CompareTag ("Citizen")) {
//			// Create a vector from the enemy to the player and store the angle between it and forward.
//			Vector3 direction = other.transform.position - transform.position;
//			float angle = Vector3.Angle (direction, transform.forward);
//			
//			// If the angle between forward and where the player is, is less than half the angle of view...
//			if (angle < fieldOfViewAngle * 0.5f) {
//				Transform citizen = other.gameObject.transform;
//				citizenPos.Set (citizen.position.x, citizen.position.y, citizen.position.z);
//				return;
//			}
//		}
//	}
//
//	void OnTriggerExit(Collider other)
//	{
//		citizenInViewTime = Time.time;
//		citizenInView = false;
//	}

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

	IEnumerator WaitForStuff(float waitTime) {
		yield return new WaitForSeconds(waitTime);

	}

}


 
