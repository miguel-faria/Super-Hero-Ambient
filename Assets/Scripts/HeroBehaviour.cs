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
	int levelDarkSide;
	int damageBonus = 0;
	int armorBonus = 0;
	float lastDecisionTime;
	float time;
	float attackTime;
	float saveTime;
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
	StrikerVillainBehaviour strikerVillain;
	ConverterVillainBehaviour converterVillain;
	CitizenBehaviour citizenSC;
	Vector3 citizenPos = new Vector3();
	
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

	public int LevelDarkSide {
		get {
			return levelDarkSide;
		}
		set {
			levelDarkSide = value;
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
		levelDarkSide = 0;
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
		usedSuperSpeedTime = Time.time;
		citizenInViewTime = Time.time;
		timeLastPercep = Time.time;
		saveTime = Time.time;
		attackTime = Time.time;

	}
	
	// Update is called once per frame
	void Update () {
		remainingCitizens = int.Parse (outputRemainingCitizens.text);
		killedCitizens = int.Parse (inputKilledCitizens.text);
		convertedCitizens = int.Parse (inputConvertedCitizens.text);
		beliefs = updateBeliefs (beliefs);

		if (intention != null){
			if (saveCrush) {

			//} else if (screams != null) {

			} else if(beliefs != null && desires != null && (!intention.Possible || intention.Concluded) 
			          && noPerceptions && ((Time.time - timeLastPercep) > 5.0f)){

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
		if (updatedIntention) {
			Debug.Log ("Hero Executing Intention: " + intention.Description);
			updatedIntention = false;
		}
		
		if (intention.IntentObject == null) {
			intention.Possible = false;
			intention.DistanceToDestination = float.MaxValue;
			return;
		}

		switch (intention.Type) {
		case (int)heroIntentionTypes.AttackVillain:
			if(intention.IntentObject.name.Equals("Converter Villain")) {
				converterVillain = (ConverterVillainBehaviour)intention.IntentObject.GetComponent(typeof(ConverterVillainBehaviour));
				if(!converterVillain.IsAlive){
					intention.Concluded = true;
					inCombat = false;
					intention.DistanceToDestination = float.MaxValue;
				} else{
					if(VillainInAttackDistance(converterVillain.gameObject))
						Attack(converterVillain.gameObject);
					else{
						followedObject = converterVillain.gameObject;
						Follow(followedObject.transform.position);
					}
				}
			}else if(intention.IntentObject.name.Equals("Striker Villain")) {
				strikerVillain = (StrikerVillainBehaviour)intention.IntentObject.GetComponent(typeof(StrikerVillainBehaviour));
				if(!strikerVillain.isAlive){
					intention.Concluded = true;
					inCombat = false;
					intention.DistanceToDestination = float.MaxValue;
				} else{
					if(VillainInAttackDistance(strikerVillain.gameObject))
						Attack(strikerVillain.gameObject);
					else{
						followedObject = strikerVillain.gameObject;
						Follow(followedObject.transform.position);
					}
				}
			}
			break;
		case (int)heroIntentionTypes.FollowSound:
			if ((intention.SoundOrigin != Vector3.zero) &&
			    (Vector3.Distance (this.transform.position, intention.IntentObject.transform.position) == 0)){
				intention.Concluded = true;
				intention.DistanceToDestination = float.MaxValue;
			}else{
				followedObject = intention.IntentObject;
				Follow(followedObject.transform.position);
			}
			break;
		case (int)heroIntentionTypes.HealCrush:

			break;
		case (int)heroIntentionTypes.KillVillain:
			if(intention.IntentObject.name.Equals("Converter Villain")){
				converterVillain = (ConverterVillainBehaviour)intention.IntentObject.GetComponent(typeof(ConverterVillainBehaviour));
				if(!converterVillain.IsAlive){
					intention.Concluded = true;
					inCombat = false;
					intention.DistanceToDestination = float.MaxValue;
				} else {
					if(!VillainInRangeSuperSenses(converterVillain.gameObject)){
						followedObject = converterVillain.gameObject;
						Follow(followedObject.transform.position);
					}else if(VillainInRangeSuperSenses(converterVillain.gameObject) && 
					         !VillainInAttackDistance(converterVillain.gameObject) && superSpeedCharged){
						ActivateSuperSpeed();
						followedObject = converterVillain.gameObject;
						Follow(followedObject.transform.position);
					}else if(VillainInAttackDistance(converterVillain.gameObject))
						Attack(converterVillain.gameObject);
				}
			}else if(intention.IntentObject.name.Equals("Striker Villain")){
				strikerVillain = (StrikerVillainBehaviour)intention.IntentObject.GetComponent(typeof(StrikerVillainBehaviour));
				if(!strikerVillain.isAlive){
					intention.Concluded = true;
					inCombat = false;
					intention.DistanceToDestination = float.MaxValue;
				} else {
					if(!VillainInRangeSuperSenses(strikerVillain.gameObject)){
						followedObject = strikerVillain.gameObject;
						Follow(followedObject.transform.position);
					}else if(VillainInRangeSuperSenses(strikerVillain.gameObject) && 
					         !VillainInAttackDistance(strikerVillain.gameObject) && superSpeedCharged){
						ActivateSuperSpeed();
						followedObject = strikerVillain.gameObject;
						Follow(followedObject.transform.position);
					}else if(VillainInAttackDistance(strikerVillain.gameObject))
						Attack(strikerVillain.gameObject);
				}
			}
			break;
		case (int)heroIntentionTypes.Move:
			if (desires.Count != 0){
				intention.Possible = false;
				intention.DistanceToDestination = float.MaxValue;
			}else {
				if(Time.time - time >= 5f)
					RandomWalk();
				if(agent.transform.position.Equals(agent.destination))
					intention.Concluded = true;
			}
			break;
		case (int)heroIntentionTypes.PickupPowerUp:
			switch(intention.IntentObject.tag){
			case "Attack":
				if(Vector3.Distance(this.transform.position, intention.IntentObject.transform.position) <= 0.5f){
					CatchAttackPowerUp();
					Destroy (intention.IntentObject);
					intention.Concluded = true;
				}else{
					followedObject = intention.IntentObject;
					Follow(followedObject.transform.position);
				}
				break;
			case "Armor":
				if(Vector3.Distance(this.transform.position, intention.IntentObject.transform.position) <= 0.5f){
					CatchArmorPowerUp();
					Destroy (intention.IntentObject);
					intention.Concluded = true;
				}else{
					followedObject = intention.IntentObject;
					Follow(followedObject.transform.position);
				}
				break;
			case "Health":
				if(Vector3.Distance(this.transform.position, intention.IntentObject.transform.position) <= 0.5f){
					CatchHealthPowerUp();
					Destroy (intention.IntentObject);
					intention.Concluded = true;
				}else{
					followedObject = intention.IntentObject;
					Follow(followedObject.transform.position);
				}
				break;
			}
			break;
		case (int)heroIntentionTypes.Revenge:

			break;
		case (int)heroIntentionTypes.SaveCitizen:
			citizenSC = (CitizenBehaviour)intention.IntentObject.GetComponent (typeof(CitizenBehaviour));
			citizenPos = intention.IntentObject.transform.position;
			followedObject = intention.IntentObject;
			if(CitizenIsSaved(citizenSC)){
				intention.Concluded = true;
				intention.DistanceToDestination = float.MaxValue;
			}else if (CitizenInRange (intention.IntentObject)) {
				if (CitizenIsEvil (citizenSC)) {
					intention.Possible = false;
					intention.DistanceToDestination = float.MaxValue;
				} else if (CitizenIsDead (citizenSC)) {
					intention.Possible = false;
					intention.DistanceToDestination = float.MaxValue;
				} else if (((Time.time - saveTime) >= 1.0f)) {
					Save (citizenSC);
					saveTime = Time.time;
				}
			} else {
				if (CitizenIsDead (citizenSC)) {
					intention.Possible = false;
					intention.DistanceToDestination = float.MaxValue;
				} else {
					Follow (citizenPos);
				}
			}
			break;
		case (int)heroIntentionTypes.SaveCrush:
			CrushBehaviour crushBehaviour;
			if(crush != null){
				crushBehaviour = (CrushBehaviour)crush.GetComponent(typeof(CrushBehaviour));
				citizenPos = crush.transform.position;
			}else {
				crushBehaviour = (CrushBehaviour)intention.IntentObject.GetComponent (typeof(CrushBehaviour));
				citizenPos = intention.IntentObject.transform.position;
			}
			followedObject = intention.IntentObject;
			if (CitizenIsEvil (crushBehaviour)) {
				if(Random.Range(1,11) > 7){
					Save(crushBehaviour);
					saveTime = Time.time;
				}
			}else if (CitizenIsDead (crushBehaviour) && !saveCrush) {
				intention.Possible = false;
				intention.DistanceToDestination = float.MaxValue;
				RevengeMode();
			} else if(CitizenIsSaved(crushBehaviour)){
				intention.Concluded = true;
				intention.DistanceToDestination = float.MaxValue;
			}else if (CitizenInRange (intention.IntentObject)) {
				GameObject[] villains = GameObject.FindGameObjectsWithTag("Villains");
				float closestVillainPos = float.MaxValue;
				float distanceToCrush = Vector3.Distance(this.transform.position, intention.IntentObject.transform.position);
				GameObject villain = null;

				for(int i = 0; i < villains.Length; i++){
					float villainPos = Vector3.Distance(this.transform.position, villains[i].transform.position);
					if(villainPos < closestVillainPos){
						villain = villains[i];
						closestVillainPos = villainPos;
					}
				}

				if(((Time.time - attackTime) >= 1.0f) && (closestVillainPos < distanceToCrush)){
					Attack(villain);
					attackTime = Time.time;
				} else if (((Time.time - saveTime) >= 1.0f)){
					Save (crushBehaviour);
					saveTime = Time.time;
				}
			} else 
				Follow (citizenPos);
			break;
		default:
			break;
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
			foreach(Desire desire in desires){
				if(Vector3.Distance(currentPosition, desire.ObjectiveDestination) * desire.PreferenceFactor < newIntention.DistanceToDestination){
					GameObject intentionObject = desire.SubjectObject;
					switch(desire.Type){
					case (int)heroDesireTypes.Save:
						newIntention = new Intention((int)heroIntentionTypes.SaveCitizen, "Save Citizen", intentionObject, 
						                             Vector3.Distance(currentPosition, intentionObject.transform.position));
						break;
					case (int)heroDesireTypes.Pick:
						newIntention = new Intention((int)heroIntentionTypes.PickupPowerUp, "Pick up PowerUp", intentionObject, 
						                             Vector3.Distance(currentPosition, intentionObject.transform.position));
						break;
					case (int)heroDesireTypes.HealCrush:
						newIntention = new Intention((int)heroIntentionTypes.HealCrush, "Heal Chan, need to heal her!!", intentionObject, 
						                             Vector3.Distance(currentPosition, intentionObject.transform.position));
						break;
					case (int)heroDesireTypes.DefeatVillain:
						newIntention = new Intention((int)heroIntentionTypes.AttackVillain, "Defeat Villain", intentionObject, 
						                             Vector3.Distance(currentPosition, intentionObject.transform.position));
						break;
					case (int)heroDesireTypes.DefendAgainstVillain:
						newIntention = new Intention((int)heroIntentionTypes.AttackVillain, "Defend from Villain", intentionObject, 
						                             Vector3.Distance(currentPosition, intentionObject.transform.position));
						break;
					default:
						break;
					}
				}
			}
			if(chosenDesire != null){
				desires.Remove(chosenDesire);
				originatingBelief = findOriginatingBelief(chosenDesire, beliefs);
				if(originatingBelief != null)
					beliefs.Remove(originatingBelief);
			}
		} else {
			GameObject dest = destinations [Random.Range(0, destinations.Length)].gameObject;
			newIntention = new Intention((int)heroIntentionTypes.Move, "Move Randomly", dest,
			                             Vector3.Distance(currentPosition, dest.transform.position));
		}
		
		return newIntention;
	}

	List<Desire> updateDesires(List<Belief> beliefs, List<Desire> oldDesires){
		List<Desire> newDesires = new List<Desire> (oldDesires);

		foreach (Belief belief in beliefs) {
			if(!alreadyInDesires(belief, newDesires)){
				switch(belief.Type){
				case (int)heroBeliefTypes.See:
					switch(belief.BeliefObject.tag){
					case "PowerUP":
						newDesires.Add(new Desire((int)heroDesireTypes.Pick,"Pick Power UP", belief.BeliefObject, 0.4f));
						break;
					case "Citizen":
						if(belief.BeliefObject.name.Equals("Crush")){
							newDesires.Add(new Desire((int)heroDesireTypes.HealCrush, "Heal Crush", belief.BeliefObject, 0.15f));
						}else{
							newDesires.Add(new Desire((int)heroDesireTypes.Save, "Save Citizen", belief.BeliefObject, isDark() ? 0.3f : 0.25f));
						}
						break;
					case "Villain":
						newDesires.Add(new Desire((int)heroDesireTypes.DefeatVillain, "Defeat Villain", belief.BeliefObject, isDark() ? 0.25f : 0.3f));
						break;
					default:
						break;
					}
					break;
				case (int)heroBeliefTypes.Touching:
					newDesires.Add(new Desire((int)heroDesireTypes.DefendAgainstVillain, "Defend Myself", belief.BeliefObject, 0.2f));
					break;
				case (int)heroBeliefTypes.CanUseSuperSpeed:
					superSpeedCharged = true;
					break;
				default:
					break;
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
		GameObject[] attackPowerUPs = GameObject.FindGameObjectsWithTag ("Attack");
		GameObject[] armorPowerUPs = GameObject.FindGameObjectsWithTag ("Armor");
		GameObject[] healthPowerUPs = GameObject.FindGameObjectsWithTag ("Health");

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

		for (int i = 0; i < attackPowerUPs.Length; i++) {
			if(InSight(attackPowerUPs[i].gameObject))
				newPerceptions.Add(new Perception(attackPowerUPs[i].gameObject, (int)heroPerceptionType.Saw));
		}

		for (int i = 0; i < armorPowerUPs.Length; i++) {
			if(InSight(armorPowerUPs[i].gameObject))
				newPerceptions.Add(new Perception(armorPowerUPs[i].gameObject, (int)heroPerceptionType.Saw));
		}

		for (int i = 0; i < healthPowerUPs.Length; i++) {
			if(InSight(healthPowerUPs[i].gameObject))
				newPerceptions.Add(new Perception(healthPowerUPs[i].gameObject, (int)heroPerceptionType.Saw));
		}

		if (!superSpeedCharged && ((Time.time - usedSuperSpeedTime) > Definitions.SUPERSPEEDCOOLDOWN))
			newPerceptions.Add (new Perception (this.gameObject, (int)heroPerceptionType.SuperSpeedAvailable));

		if (usedSuperSpeed && ((Time.time - usedSuperSpeedTime) > Definitions.SUPERSPEEDMAXTIME)) {
			usedSuperSpeed = false;
			agent.speed = 8.0f;
			anim.SetFloat("Speed", 8.0f);
		}

		return newPerceptions;
	}

	Belief findBelief(int type, GameObject beliefObject, List<Belief> beliefsList){	
		foreach (Belief belief in beliefsList) {
			if((belief.Type == type) && (belief.BeliefObject.Equals(beliefObject))){
				return belief;
			}
		}
		return null;
	}

	Belief findOriginatingBelief(Desire desire, List<Belief> beliefsList){
		foreach (Belief belief in beliefsList) {
			if((((belief.Type == (int)villainBeliefTypes.Hear) && (desire.Type == (int)villainDesireTypes.Follow)) || 
			    ((belief.Type == (int)villainBeliefTypes.See) && ((desire.Type == (int)villainDesireTypes.Convert) || 
			                                                  (desire.Type == (int)villainDesireTypes.Flee))) ||
			    ((belief.Type == (int)villainBeliefTypes.Touching) && (desire.Type == (int)villainDesireTypes.DefendAgainstHero)))
			   && (desire.SubjectObject.Equals(belief.BeliefObject))){
				return belief;
			}
		}
		return null;
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

	bool isDark(){
		return (levelDarkSide == 10);
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

	bool VillainInAttackDistance(GameObject villain){
		return Vector3.Distance (this.transform.position, villain.transform.position) <= Definitions.MAXATTACKDISTANCE;
	}

	bool IsTouching(GameObject other){
		float distance = Vector3.Distance (this.transform.position, other.transform.position);
		return ((distance <= Definitions.MAXTOUCHINGDISTANCE) && (distance >= 0.0f));
	}

	bool CitizenInRange(GameObject citizen){
		return Vector3.Distance (this.transform.position, citizen.transform.position) < 3.0f;
	}

	bool CitizenIsSaved(CitizenBehaviour citizen){
		return citizen.IsSaved ();
	}

	bool CitizenIsDead(CitizenBehaviour citizen){
		return (citizen.Life <= 0);
	}

	bool CitizenIsEvil(CitizenBehaviour citizen){
		return citizen.IsEvil();
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

	void StopFollowing()
	{
		if (followedObject.CompareTag ("Citizen"))
			citizenSC = null;
		else if (followedObject.CompareTag ("Villain")) {
			if(followedObject.name.Equals("ConverterVillain"))
			   	converterVillain = null;
		   else
				strikerVillain = null;
		}
		followedObject = null;
		time = float.MaxValue;
	}

	void ActivateSuperSpeed(){
		agent.speed = 16.0f;
		anim.SetFloat("Speed", 16.0f);
		usedSuperSpeedTime = Time.time;
		usedSuperSpeed = true;
		superSpeedCharged = false;
	}

	void Save(CitizenBehaviour citizen){
		citizen.Saved ();
		if (citizen.IsSaved ()) {
			remainingCitizens--;
			outputRemainingCitizens.text = "" + remainingCitizens;
			savedCitizens++;
			outputSavedCitizens.text = "" + savedCitizens;
		}
		anim.SetTrigger ("Convert");
		agent.Stop ();
	}

	void RevengeMode(){
		levelDarkSide = 10;
	}

	void Attack(GameObject attacker){
		ConverterVillainBehaviour convVillain;
		StrikerVillainBehaviour strikeVillain;
		int damage;
		if (!inCombat)
			inCombat = true;
		if((Time.time - attackTime) >= 1.0f){
			//TODO: update animation
			if(Random.Range(1,11) > 6)
				damage = Random.Range(1,6);
			else
				damage = Random.Range(6,11);
			if (attacker.gameObject.name.Equals("ConverterVillain")) {
				convVillain = (ConverterVillainBehaviour)attacker.GetComponent (typeof(ConverterVillainBehaviour));
				convVillain.Attacked(damage+damageBonus);
			}else if(attacker.gameObject.name.Equals("StrikerVillain")){
				strikeVillain = (StrikerVillainBehaviour)attacker.GetComponent (typeof(StrikerVillainBehaviour));
				strikeVillain.Attacked(damage+damageBonus);
			}
			attackTime = Time.time;
		}
	}

	public void Attacked(GameObject attacker, int damage) {
		ConverterVillainBehaviour convVillain;
		StrikerVillainBehaviour strikeVillain;
		if (life > 0) {
			damage -= armorBonus;
			if(damage < 0) damage = 0;
			life -= damage;
			health.value = life;
			Debug.Log("Bayamax := Took " + damage + " damage!");
		}else {
			inCombat = false;			
			if (attacker.gameObject.name.Equals("ConverterVillain")) {
				convVillain = (ConverterVillainBehaviour)attacker.GetComponent (typeof(ConverterVillainBehaviour));
				convVillain.InCombat = false;
			}else if(attacker.gameObject.name.Equals("StrikerVillains")){
				strikeVillain = (StrikerVillainBehaviour)attacker.GetComponent (typeof(StrikerVillainBehaviour));
				strikeVillain.InCombat = false;
			}
			Debug.Log("I'm dead YO!!!!!!!!! - Hero");
			Destroy(gameObject);
		}
	}

	void StopAttacking(){
		UpdateAnimations (false, false, false, true, false);
	}
	
	void Follow(Vector3 destinationPos){
		agent.Resume();
		Debug.Log ("Gonna Stalk: " + followedObject);
		agent.SetDestination (destinationPos);
		if(agent.speed < 8.0f)
			agent.speed = 8.0f;
		//anim.SetBool ("isWalking", true);
		//UpdateAnimations (true, false, false, true, false);
	}
	
	void RandomWalk(){
		//agent.Resume();
		Debug.Log ("Wandering Hero");
		agent.SetDestination (destinations [Random.Range (0, destinations.Length)].position);
		agent.speed = 3.5f;
		//UpdateAnimations (true, false, false, true, false);
		//anim.SetBool ("isWalking", true);
		time = Time.time;
	}

	void UpdateAnimations(bool walking, bool laughing, bool combat, bool alive, bool convert){
		anim.SetBool ("isWalking", walking);
		anim.SetBool ("inCombat", combat);
		anim.SetBool ("isAlive", alive);
		
		if(laughing)
			anim.SetTrigger ("Laugh");
		if (convert)
			anim.SetTrigger ("Convert");
	}

	void CatchAttackPowerUp( ){

		damageBonus += 2;

	}

	void CatchArmorPowerUp( ){
		
		armorBonus += 1;
		
	}

	void CatchHealthPowerUp( ){
		
		life += 10;
		health.value = life;
		
	}


}
