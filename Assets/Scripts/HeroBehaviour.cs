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
	int damageBonus;
	int armorBonus;
	float lastDecisionTime;
	float lastReactiveActionTime;
	float time;
	float attackTime;
	float saveTime;
	float citizenInViewTime;
	float usedSuperSpeedTime;
	float timeLastPercep;
	float lastScreamPercep;
	bool isFollowing;
	bool inCombat;
	bool isAlive;
	bool updatedIntention;
	bool saveCrush;
	bool superSpeedCharged;
	bool usedSuperSpeed;
	bool noPerceptions;
	bool reactiveAction;
	bool wantRevenge;
	bool onPatrol;
	bool usingEnhancedSensors;
	bool beingAttacked;
	
	Animator anim;
	NavMeshAgent agent;
	AnimatorStateInfo state;
	Transform[] destinations;
	
	GameObject followedObject;
	GameObject crush;
	GameObject villain;
	GameObject villainAttacker;
	CrushBehaviour crushBehaviour;
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
		CitizenBehaviour.OnDeath += CrushDied;
	}

	void OnDisable(){
		CitizenBehaviour.OnAttack -= HeardScream;
		CitizenBehaviour.OnDeath -= CrushDied;
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

	public bool IsAlive {
		get {
			return isAlive;
		}
		set {
			isAlive = value;
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
		armorBonus = 0;
		damageBonus = 0;
		anim = GetComponent<Animator> ();
		agent = GetComponent<NavMeshAgent>();

		GameObject[] objects = GameObject.FindGameObjectsWithTag("Destination");
		destinations = new Transform[objects.Length];
		for (int i = 0; i < objects.Length; i++) 
			destinations [i] = objects [i].transform;
		
		villain = null;
		crush = null;
		followedObject = destinations [Random.Range(1,destinations.Length)].gameObject;
		agent.SetDestination (followedObject.transform.position);
		intention = null;
		intention = new Intention ((int)heroIntentionTypes.Move, "Move Randomly", followedObject, 
		                           Vector3.Distance (this.transform.position, followedObject.transform.position));

		wantRevenge = false;
		reactiveAction = false;
		isFollowing = true;
		updatedIntention = true;
		saveCrush = false;
		superSpeedCharged = true;
		usedSuperSpeed = false;
		noPerceptions = false;
		onPatrol = false;
		usingEnhancedSensors = false;
		beingAttacked = false;
		isAlive = true;
		anim.SetBool ("isWalking", true);
		state = anim.GetCurrentAnimatorStateInfo (0);

		lastDecisionTime = Time.time;
		lastReactiveActionTime = Time.time;
		time = Time.time;
		usedSuperSpeedTime = Time.time;
		citizenInViewTime = Time.time;
		timeLastPercep = Time.time;
		saveTime = Time.time;
		attackTime = Time.time;
		lastScreamPercep = Time.time;

	}
	
	// Update is called once per frame
	void Update () {
		if (isAlive) {
			remainingCitizens = int.Parse (outputRemainingCitizens.text);
			killedCitizens = int.Parse (inputKilledCitizens.text);
			convertedCitizens = int.Parse (inputConvertedCitizens.text);
			beliefs = updateBeliefs (beliefs);

			if (reactiveAction && ((Time.time - lastReactiveActionTime) > Definitions.TASKFOCUSTIMEREACTIVE)) {
				reactiveAction = false;
				if (saveCrush)
					saveCrush = false;
				if (beingAttacked)
					beingAttacked = false;
			}

			if (!reactiveAction && saveCrush) {
				if (!InSight (crush))
					intention = new Intention ((int)heroIntentionTypes.FollowSound, "Follow Crush Secream", crush, 0);
				else
					intention = new Intention ((int)heroIntentionTypes.SaveCrush, "Save Crush", crush, 0);
				if (superSpeedCharged)
					ActivateSuperSpeed ();
				reactiveAction = true;
				lastReactiveActionTime = Time.time;
				updatedIntention = true;
				executeIntention (intention);

			} else if (!reactiveAction && beingAttacked) {
				intention = new Intention ((int)heroIntentionTypes.AttackVillain, "Defend Against Villain", villainAttacker, 0);
				inCombat = true;
				reactiveAction = true;
				lastReactiveActionTime = Time.time;
				updatedIntention = true;
				executeIntention (intention);

			} else if (!reactiveAction && screams.Count != 0 && ((Time.time - lastScreamPercep)) > 2.0f) {
				float minDist = float.MaxValue;
				Belief screamFollow = null;
				foreach (Belief belief in screams) {
					if (Vector3.Distance (this.transform.position, belief.BeliefObject.transform.position) < minDist) {
						screamFollow = belief;
						minDist = Vector3.Distance (this.transform.position, belief.BeliefObject.transform.position);
					}
				}
				if (!CitizenInRange (screamFollow.BeliefObject))
					intention = new Intention ((int)heroIntentionTypes.FollowSound, "Follow Citizen Scream", screamFollow.BeliefObject, 0);
				else
					intention = new Intention ((int)heroIntentionTypes.SaveCitizen, "Save Screaming Citizen", screamFollow.BeliefObject, 0);
				if (superSpeedCharged)
					ActivateSuperSpeed ();
				screams.Clear ();
				reactiveAction = true;
				lastReactiveActionTime = Time.time;
				updatedIntention = true;
				executeIntention (intention);

			} else if (!reactiveAction && beliefs.Count != 0 && desires.Count != 0 && (!intention.Possible || intention.Concluded) 
				&& noPerceptions && ((Time.time - timeLastPercep) > 5.0f)) {
				Patrol ();
				reactiveAction = true;
				lastReactiveActionTime = Time.time;

			} else {
				if (reactiveAction || 
				    ((intention != null) && intention.Possible && !intention.Concluded &&
				 	((Time.time - lastDecisionTime) < Definitions.TASKFOCUSTIME))) {
					executeIntention (intention);
					if (reactiveAction && intention.Concluded)
						reactiveAction = false;

				} else {
					desires = updateDesires (beliefs, desires);
					intention = updateIntention (beliefs, desires, intention);
					updatedIntention = true;
					executeIntention (intention);
					lastDecisionTime = Time.time;
				}
			}
			
		}

		if ((killedCitizens + convertedCitizens) > 0.4f * startingCitizens) {
			Debug.Log ("Villains Win this Game!! The city is doomed!! :(");
			Time.timeScale = 0;
		} else if (savedCitizens > 0.6f * startingCitizens) {
			Debug.Log ("Hero Wins this Game!! The city was saved!! :D");
			Time.timeScale = 0;
		} else if (strikerVillain != null && converterVillain != null && !strikerVillain.isAlive && !converterVillain.IsAlive) {
			Debug.Log ("Hero Defeated both villains!! The city was saved!! :D");
			Time.timeScale = 0;
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
			Debug.Log ("Hero Executing Intention: " + intention.Description);
			updatedIntention = false;
		}

		GameObject[] villains = GameObject.FindGameObjectsWithTag ("Villain");

		if (onPatrol) {
			if(Vector3.Distance(this.transform.position, followedObject.transform.position) < 0.5f){
				usingEnhancedSensors = true;
				onPatrol = false;
				reactiveAction = false;
			} else
				Follow(followedObject.transform.position);
		} else {
			switch (intention.Type) {
			case (int)heroIntentionTypes.AttackVillain:
				if (intention.IntentObject.name.Equals ("ConverterVillain")) {
					converterVillain = (ConverterVillainBehaviour)intention.IntentObject.GetComponent (typeof(ConverterVillainBehaviour));
					if (!converterVillain.IsAlive) {
						intention.Concluded = true;
						inCombat = false;
						intention.DistanceToDestination = float.MaxValue;
						if(reactiveAction)
							reactiveAction = false;
						if(beingAttacked){
							beingAttacked = false;
							villainAttacker = null;
						}
					} else {
						if (VillainInAttackDistance (intention.IntentObject)){
							Attack (intention.IntentObject);
						}else {
							followedObject = intention.IntentObject;
							Follow (followedObject.transform.position);
						}
					}

					if (!inCombat){
						intention.Concluded = true;
						intention.DistanceToDestination = float.MaxValue;
						if(reactiveAction)
							reactiveAction = false;
						if(beingAttacked){
							beingAttacked = false;
							villainAttacker = null;
						}
					}
				} else if (intention.IntentObject.name.Equals ("StrikerVillain")) {
					strikerVillain = (StrikerVillainBehaviour)intention.IntentObject.GetComponent (typeof(StrikerVillainBehaviour));
					if (!strikerVillain.isAlive) {
						intention.Concluded = true;
						inCombat = false;
						intention.DistanceToDestination = float.MaxValue;
						if(reactiveAction)
							reactiveAction = false;
						if(beingAttacked){
							beingAttacked = false;
							villainAttacker = null;
						}
					} else {
						if (VillainInAttackDistance (strikerVillain.gameObject)){
							Attack (intention.IntentObject);
						}else {
							followedObject = intention.IntentObject;
							Follow (followedObject.transform.position);
						}
					}

					if (!inCombat){
						intention.Concluded = true;
						intention.DistanceToDestination = float.MaxValue;
						if(reactiveAction)
							reactiveAction = false;
						if(beingAttacked){
							beingAttacked = false;
							villainAttacker = null;
						}
					}
				}
				break;

			case (int)heroIntentionTypes.FollowSound:
				if ((intention.SoundOrigin != Vector3.zero) &&
					(Vector3.Distance (this.transform.position, intention.IntentObject.transform.position) < 4.0f)) {
					intention.Concluded = true;
					intention.DistanceToDestination = float.MaxValue;
					if(reactiveAction){
						intention = new Intention ((int)heroIntentionTypes.SaveCitizen, "Save Screaming Citizen", intention.IntentObject,
						                           Vector3.Distance (this.transform.position, intention.IntentObject.transform.position));
					}
				} else {
					followedObject = intention.IntentObject;
					Follow (followedObject.transform.position);
				}
				break;

			case (int)heroIntentionTypes.HealCrush:
				if (crushBehaviour.Life == 5) {
					intention.Concluded = true;
					intention.DistanceToDestination = float.MaxValue;
				} else if (!crushBehaviour.IsAlive) {
					intention.Possible = false;
					intention.DistanceToDestination = float.MaxValue;
				} else {
					float distance = Vector3.Distance (this.transform.position, crush.transform.position);
					if (distance > Definitions.MAXTOUCHINGDISTANCE)
						Follow (crush.transform.position);
					else {
						Heal (crushBehaviour);
					}
				}
				break;

			case (int)heroIntentionTypes.KillVillain:
				if (intention.IntentObject.name.Equals ("ConverterVillain")) {
					converterVillain = (ConverterVillainBehaviour)intention.IntentObject.GetComponent (typeof(ConverterVillainBehaviour));
					if (!converterVillain.IsAlive) {
						intention.Concluded = true;
						inCombat = false;
						intention.DistanceToDestination = float.MaxValue;
					} else {
						if (!VillainInRangeSuperSenses (converterVillain.gameObject)) {
							followedObject = converterVillain.gameObject;
							Follow (followedObject.transform.position);
						} else if (VillainInRangeSuperSenses (converterVillain.gameObject) && 
							!VillainInAttackDistance (converterVillain.gameObject) && superSpeedCharged) {
							ActivateSuperSpeed ();
							followedObject = converterVillain.gameObject;
							Follow (followedObject.transform.position);
						} else if (VillainInAttackDistance (converterVillain.gameObject)){
							Attack (converterVillain.gameObject);
						}
					}
				} else if (intention.IntentObject.name.Equals ("StrikerVillain")) {
					strikerVillain = (StrikerVillainBehaviour)intention.IntentObject.GetComponent (typeof(StrikerVillainBehaviour));
					if (!strikerVillain.isAlive) {
						intention.Concluded = true;
						inCombat = false;
						intention.DistanceToDestination = float.MaxValue;
					} else {
						if (!VillainInRangeSuperSenses (strikerVillain.gameObject)) {
							followedObject = strikerVillain.gameObject;
							Follow (followedObject.transform.position);
						} else if (VillainInRangeSuperSenses (strikerVillain.gameObject) && 
							!VillainInAttackDistance (strikerVillain.gameObject) && superSpeedCharged) {
							ActivateSuperSpeed ();
							followedObject = strikerVillain.gameObject;
							Follow (followedObject.transform.position);
						} else if (VillainInAttackDistance (strikerVillain.gameObject)){
							Attack (strikerVillain.gameObject);
						}
					}
				}
				break;

			case (int)heroIntentionTypes.Move:
				if (beliefs.Count != 0) {
					intention.Possible = false;
					intention.DistanceToDestination = float.MaxValue;
				} else {
					if(Time.time - time >= 5.0f)
						RandomWalk();
					if(agent.transform.position.Equals(agent.destination))
						intention.Concluded = true;
				}
				break;

			case (int)heroIntentionTypes.PickupPowerUp:
				switch (intention.IntentObject.tag) {
				case "Attack":
					if (Vector3.Distance (this.transform.position, intention.IntentObject.transform.position) <= 0.5f) {
						CatchAttackPowerUp ();
						Destroy (intention.IntentObject);
						intention.Concluded = true;
					} else {
						followedObject = intention.IntentObject;
						Follow (followedObject.transform.position);
					}
					break;
				case "Armor":
					if (Vector3.Distance (this.transform.position, intention.IntentObject.transform.position) <= 0.5f) {
						CatchArmorPowerUp ();
						Destroy (intention.IntentObject);
						intention.Concluded = true;
					} else {
						followedObject = intention.IntentObject;
						Follow (followedObject.transform.position);
					}
					break;
				case "Health":
					if (Vector3.Distance (this.transform.position, intention.IntentObject.transform.position) <= 0.5f) {
						CatchHealthPowerUp ();
						Destroy (intention.IntentObject);
						intention.Concluded = true;
					} else {
						followedObject = intention.IntentObject;
						Follow (followedObject.transform.position);
					}
					break;
				}
				break;

			case (int)heroIntentionTypes.Revenge:
				if (villains [0].name.Equals ("ConverterVillain")) {
					converterVillain = (ConverterVillainBehaviour)villains [0].GetComponent (typeof(ConverterVillainBehaviour));
					strikerVillain = (StrikerVillainBehaviour)villains [1].GetComponent (typeof(StrikerVillainBehaviour));
				} else {
					converterVillain = (ConverterVillainBehaviour)villains [1].GetComponent (typeof(ConverterVillainBehaviour));
					strikerVillain = (StrikerVillainBehaviour)villains [0].GetComponent (typeof(StrikerVillainBehaviour));
				}
		
				if (Vector3.Distance (this.transform.position, converterVillain.gameObject.transform.position) < 
					Vector3.Distance (this.transform.position, strikerVillain.gameObject.transform.position)) {
					followedObject = converterVillain.gameObject;
				} else {
					followedObject = strikerVillain.gameObject;
				}

				if (followedObject.name.Equals ("ConverterVillain") && !converterVillain.IsAlive && strikerVillain.isAlive)
					followedObject = strikerVillain.gameObject;
				else if (followedObject.name.Equals ("StrikerVillain") && !strikerVillain.isAlive && converterVillain.IsAlive)
					followedObject = converterVillain.gameObject;

				if (!VillainInRangeSuperSenses(followedObject))
					Follow(followedObject.transform.position);
				else if(VillainInRangeSuperSenses(followedObject))
					ActivateSuperSpeed();
				else if(VillainInAttackDistance(followedObject)){
					Attack(followedObject);
				}

				if (!converterVillain.IsAlive && !strikerVillain.isAlive) {
					intention.Concluded = true;
					intention.DistanceToDestination = float.MaxValue;
					if (reactiveAction)
						reactiveAction = false;
				}
				break;

			case (int)heroIntentionTypes.SaveCitizen:
				citizenSC = (CitizenBehaviour)intention.IntentObject.GetComponent (typeof(CitizenBehaviour));
				citizenPos = intention.IntentObject.transform.position;
				followedObject = intention.IntentObject;
				if (CitizenIsSaved (citizenSC)) {
					intention.Concluded = true;
					intention.DistanceToDestination = float.MaxValue;
					if(reactiveAction)
						reactiveAction = false;
				} else if (CitizenIsEvil (citizenSC)) {
					intention.Possible = false;
					intention.DistanceToDestination = float.MaxValue;
				} else if (CitizenIsDead (citizenSC)) {
					intention.Possible = false;
					intention.DistanceToDestination = float.MaxValue;
				} else if (CitizenInRange (intention.IntentObject)) {
					float closestVillainPos = float.MaxValue;
					float distanceToCitizen = Vector3.Distance (this.transform.position, intention.IntentObject.transform.position);
					GameObject villain = null;
					
					for (int i = 0; i < villains.Length; i++) {
						float villainPos = Vector3.Distance (this.transform.position, villains [i].transform.position);
						if (villainPos < closestVillainPos) {
							villain = villains [i];
							closestVillainPos = villainPos;
						}
					}
					
					if (((Time.time - attackTime) >= 1.0f) && VillainInAttackDistance(villain)) {
						Attack (villain);
						attackTime = Time.time;
					} else if ((Time.time - saveTime) >= 1.0f) {
						Save (citizenSC);
						saveTime = Time.time;
					}
				} else {
					Follow (citizenPos);
				}
				break;

			case (int)heroIntentionTypes.SaveCrush:
				if (crush != null) {
					citizenPos = crush.transform.position;
				} else {
					crushBehaviour = (CrushBehaviour)intention.IntentObject.GetComponent (typeof(CrushBehaviour));
					citizenPos = intention.IntentObject.transform.position;
				}
				followedObject = intention.IntentObject;
				if (CitizenIsSaved (crushBehaviour)) {
					intention.Concluded = true;
					intention.DistanceToDestination = float.MaxValue;
					if(reactiveAction)
						reactiveAction = false;
				} else if (CitizenIsEvil (crushBehaviour)) {
					if (Random.Range (1, 11) > 7) {
						Save (crushBehaviour);
						saveTime = Time.time;
					}
				} else if (CitizenIsDead (crushBehaviour) && !saveCrush) {
					intention.Possible = false;
					intention.DistanceToDestination = float.MaxValue;
					RevengeMode ();
				} else if (CitizenInRange (intention.IntentObject)) {
					float closestVillainPos = float.MaxValue;
					float distanceToCrush = Vector3.Distance (this.transform.position, intention.IntentObject.transform.position);
					GameObject villain = null;

					for (int i = 0; i < villains.Length; i++) {
						float villainPos = Vector3.Distance (this.transform.position, villains [i].transform.position);
						if (villainPos < closestVillainPos) {
							villain = villains [i];
							closestVillainPos = villainPos;
						}
					}

					if (((Time.time - attackTime) >= 1.0f) && VillainInAttackDistance(villain)) {
						Attack (villain);
						attackTime = Time.time;
					} else if ((Time.time - saveTime) >= 1.0f) {
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
	}

	Intention updateIntention(List<Belief> beliefs, List<Desire> desires, Intention oldIntention){
		Intention newIntention;
		if (oldIntention.Type != (int)villainIntentionTypes.Move)
			newIntention = oldIntention;
		else {
			newIntention = new Intention(oldIntention.Type, oldIntention.Description, oldIntention.IntentObject, float.MaxValue);
		}
		
		List<Desire> emptyDesires = new List<Desire> ();
		foreach (Desire desire in desires) {
			if(desire.SubjectObject == null) 
				emptyDesires.Remove(desire);
		}
		foreach (Desire desire in emptyDesires) {
			desires.Remove (desire);
		}
		emptyDesires.Clear ();
		List<Belief> emptyBeliefs = new List<Belief> ();
		foreach (Belief belief in beliefs) {
			if(belief.BeliefObject == null) 
				emptyBeliefs.Remove(belief);
		}
		foreach (Belief belief in emptyBeliefs) {
			beliefs.Remove (belief);
		}
		emptyBeliefs.Clear ();

		Vector3 currentPosition = this.transform.position;
		Desire chosenDesire = null;
		Belief originatingBelief = null;

		if (desires.Count != 0) {
			foreach(Desire desire in desires){
				if(isDark()){
					if(desire.Type == (int)heroDesireTypes.DefeatVillain)
						desire.PreferenceFactor = 0.25f;
					else if(desire.Type == (int)heroDesireTypes.Save)
						desire.PreferenceFactor = 0.45f;
					else if(desire.Type == (int)heroDesireTypes.Pick)
						desire.PreferenceFactor = 0.3f;
				}
				if(Vector3.Distance(currentPosition, desire.ObjectiveDestination) * desire.PreferenceFactor < newIntention.DistanceToDestination){
					GameObject intentionObject = desire.SubjectObject;
					if(intentionObject == null)
						continue;
					switch(desire.Type){
					case (int)heroDesireTypes.Save:
						newIntention = new Intention((int)heroIntentionTypes.SaveCitizen, "Save Citizen", intentionObject, 
						                             Vector3.Distance(currentPosition, intentionObject.transform.position));
						chosenDesire = desire;
						break;
					case (int)heroDesireTypes.Pick:
						newIntention = new Intention((int)heroIntentionTypes.PickupPowerUp, "Pick up PowerUp", intentionObject, 
						                             Vector3.Distance(currentPosition, intentionObject.transform.position));
						chosenDesire = desire;
						break;
					case (int)heroDesireTypes.HealCrush:
						newIntention = new Intention((int)heroIntentionTypes.HealCrush, "Heal Chan, need to heal her!!", intentionObject, 
						                             Vector3.Distance(currentPosition, intentionObject.transform.position));
						chosenDesire = desire;
						break;
					case (int)heroDesireTypes.DefeatVillain:
						newIntention = new Intention((int)heroIntentionTypes.AttackVillain, "Defeat Villain", intentionObject, 
						                             Vector3.Distance(currentPosition, intentionObject.transform.position));
						chosenDesire = desire;
						break;
					case (int)heroDesireTypes.DefendAgainstVillain:
						newIntention = new Intention((int)heroIntentionTypes.AttackVillain, "Defend from Villain", intentionObject, 
						                             Vector3.Distance(currentPosition, intentionObject.transform.position));
						chosenDesire = desire;
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
			noPerceptions = true;
		}
		
		return newIntention;
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
				switch(belief.Type){
				case (int)heroBeliefTypes.See:
					switch(belief.BeliefObject.tag){
					case "PowerUP":
						newDesires.Add(new Desire((int)heroDesireTypes.Pick,"Pick Power UP", belief.BeliefObject, 0.4f));
						break;
					case "Citizen":
						if(belief.BeliefObject.name.Equals("Crush")){
							if(crushBehaviour.Life < 5)
								newDesires.Add(new Desire((int)heroDesireTypes.HealCrush, "Heal Crush", belief.BeliefObject, 0.15f));
							else{
								newDesires.Add(new Desire((int)heroDesireTypes.Save, "Save Crush",  belief.BeliefObject, 0.2f));
							}
						}else{
							newDesires.Add(new Desire((int)heroDesireTypes.Save, "Save Citizen", belief.BeliefObject, 0.25f));
						}
						break;
					case "Villain":
						newDesires.Add(new Desire((int)heroDesireTypes.DefeatVillain, "Defeat Villain", belief.BeliefObject, 0.35f));
						break;
					default:
						break;
					}
					break;
				case (int)heroBeliefTypes.Touching:
					newDesires.Add(new Desire((int)heroDesireTypes.DefendAgainstVillain, "Defend Myself", belief.BeliefObject, 0.1f));
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
							if(!crush){
								crush = percep.ObjectPercepted;
								crushBehaviour = (CrushBehaviour)crush.GetComponent(typeof(CrushBehaviour));
							}
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
			if(InSight(citizens[i].gameObject) && 
			   ((!citizenSC.IsEvil()) || (citizens[i].name.Equals("Crush"))) &&
			   !CitizenIsDead(citizenSC) && !CitizenIsSaved(citizenSC))
				newPerceptions.Add(new Perception(citizens[i].gameObject, (int)heroPerceptionType.Saw));
		}

		for (int i = 0; i < villains.Length; i++) {
			if(IsTouching(villains[i].gameObject) && !VillainIsDead(villains[i]) && 
			   (villains[i].name.Equals("ConverterVillain") || villains[i].name.Equals("StrikerVillain"))){
				newPerceptions.Add(new Perception(villains[i].gameObject, (int)heroPerceptionType.Touched));
			}else if(InSight(villains[i].gameObject) && !VillainIsDead(villains[i]) &&
			         (villains[i].name.Equals("ConverterVillain") || villains[i].name.Equals("StrikerVillain")))
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

		if (newPerceptions.Count != 0) {
			timeLastPercep = Time.time;
			if(usingEnhancedSensors)
				usingEnhancedSensors = false;
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
		float maxSeeRange = Definitions.HEROMAXVIEWDISTANCE;
		float viewAngle = Definitions.FIELDOFVIEWANGLE * 0.5f;
		if (usingEnhancedSensors) {
			maxSeeRange = 2 * Definitions.HEROMAXVIEWDISTANCE;
			viewAngle = 360.5f;
		}
		float angle = Vector3.Angle (direction, this.transform.forward);
		if (other.CompareTag ("Citizen") || other.CompareTag ("Attack") ||
		    other.CompareTag ("Armor") || other.CompareTag ("Health")) {
			return ((distance < maxSeeRange) &&
				(angle < viewAngle));
		} else if (other.CompareTag ("Villain")) {
			return ((distance < maxSeeRange) &&
				(distance > Definitions.MAXTOUCHINGDISTANCE) &&
				(angle < viewAngle));
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
		return Vector3.Distance (this.transform.position, citizen.transform.position) < 4.0f;
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

	bool VillainIsDead(GameObject villain){
		if (villain.name.Equals ("ConverterVillain")) {
			converterVillain = (ConverterVillainBehaviour)villain.GetComponent (typeof(ConverterVillainBehaviour));
			return !converterVillain.IsAlive;
		} else if (villain.name.Equals ("StrikerVillain")) {
			strikerVillain = (StrikerVillainBehaviour)villain.GetComponent (typeof(StrikerVillainBehaviour));
			return !strikerVillain.isAlive;
		} else {
			return false;
		}
	}

	void HeardScream(GameObject screamer){
		float distance = Vector3.Distance (this.transform.position, screamer.transform.position);
		if (distance > Definitions.AOEHEARINGAREA && distance < Definitions.HEROMAXHEARINGDISTANCE) {
			Debug.Log("Hero - Heard Citizen " + screamer.name + " scream!!");
			if((crush != null) && (screamer.name.Equals("Crush"))){
				reactiveAction = false;
				saveCrush = true;
			}else{
				Perception screamPercep = new Perception(screamer, (int)heroPerceptionType.Heard);
				if(!alreadyInBeliefs(screamPercep, screams)){
					screams.Add(new HearScreamBelief(screamer, screamer.transform.position));
				}
			}
			if(superSpeedCharged)
				ActivateSuperSpeed();
			noPerceptions = false;
		}
	}

	void CrushDied(GameObject victim, GameObject attacker){
		float dist = Vector3.Distance (this.transform.position, victim.transform.position);
		if (dist <= Definitions.SUPERHEARINGAREA)
			intention = new Intention ((int)heroIntentionTypes.KillVillain, "Kill " + attacker.name, attacker,
			                          Vector3.Distance (this.transform.position, attacker.transform.position));
		else
			intention = new Intention ((int)heroIntentionTypes.Revenge, "Revenge My Unity Chan's Death", victim,
			                           Vector3.Distance (this.transform.position, victim.transform.position));
		RevengeMode ();

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
		UpdateAnimations (false, true, false, false, false);
	}

	void ActivateSuperSpeed(){
		agent.speed = 16.0f;
		anim.SetFloat("Speed", 16.0f);
		usedSuperSpeedTime = Time.time;
		usedSuperSpeed = true;
		superSpeedCharged = false;
		UpdateAnimations (true, true, false, false, false);
	}

	void Save(CitizenBehaviour citizen){
		UpdateAnimations (false, true, false, true, false);
		citizen.Saved ();
		if (citizen.IsSaved ()) {
			Debug.Log("Saved Citizen");
			remainingCitizens--;
			outputRemainingCitizens.text = "" + remainingCitizens;
			savedCitizens++;
			outputSavedCitizens.text = "" + savedCitizens;
		}
		agent.Stop ();
		UpdateAnimations (true, true, false, false, false);
	}

	void RevengeMode(){
		levelDarkSide = 10;
		wantRevenge = true;
	}

	void Attack(GameObject attacked){
		ConverterVillainBehaviour convVillain;
		StrikerVillainBehaviour strikeVillain;
		int damage;
		if (VillainInAttackDistance (attacked)) {
			inCombat = true;
			UpdateAnimations (false, true, true, false, false);
		} else {
			inCombat = false;
			UpdateAnimations (true, true, false, false, false);
		}
		if((Time.time - attackTime) >= 1.0f){
			Debug.Log ("Attacking " + attacked.name);
			UpdateAnimations (false, true, true, false, true);
			if(Random.Range(1,21) > 16)
				damage = Random.Range(1,6) + damageBonus;
			else
				damage = Random.Range(6,16) + damageBonus;
			if (attacked.gameObject.name.Equals("ConverterVillain")) {
				convVillain = (ConverterVillainBehaviour)attacked.GetComponent (typeof(ConverterVillainBehaviour));
				convVillain.Attacked(damage);
			}else if(attacked.gameObject.name.Equals("StrikerVillain")){
				strikeVillain = (StrikerVillainBehaviour)attacked.GetComponent (typeof(StrikerVillainBehaviour));
				strikeVillain.Attacked(damage);
			}
			if (VillainIsDead(attacked)){
				inCombat = false;
				UpdateAnimations (true, true, false, false, false);
			}
			attackTime = Time.time;
		}
	}

	public void Attacked(GameObject attacker, int damage) {
		ConverterVillainBehaviour convVillain;
		StrikerVillainBehaviour strikeVillain;
		if (!beingAttacked) {
			reactiveAction = false;
			beingAttacked = true;
			villainAttacker = attacker;
		}
		if (life > 0) {
			damage -= armorBonus;
			if(damage < 0) 
				damage = 0;
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
			UpdateAnimations (false, false, false, false, false);
			Debug.Log("I'm dead YO!!!!!!!!! - Hero");
			isAlive = false;
			Destroy(this.gameObject);
			Debug.Log("The Villains Killed the Hero, the city is doomed!! :(");
			anim.SetTrigger("Death");
			Time.timeScale = 0;
		}
	}

	void StopAttacking(){
		UpdateAnimations (false, true, false, false, false);
	}
	
	void Follow(Vector3 destinationPos){
		agent.Resume();
		agent.SetDestination (destinationPos);
		if (agent.speed < 8.0f) {
			agent.speed = 8.0f;
			anim.SetFloat("Speed", 8.0f);
		}
		UpdateAnimations (true, true, false, false, false);
	}
	
	void RandomWalk(){
		agent.Resume();
		agent.SetDestination (destinations [Random.Range (0, destinations.Length)].position);
		agent.speed = 3.5f;
		anim.SetFloat("Speed", 3.5f);
		UpdateAnimations (true, true, false, false, false);
		time = Time.time;
	}

	void UpdateAnimations(bool walking, bool alive, bool combat, bool save, bool attacking){
		anim.SetBool ("isWalking", walking);
		anim.SetBool ("inCombat", combat);
		anim.SetBool ("isAlive", alive);
		
		if(save)
			anim.SetTrigger ("Save");
		if (attacking)
			anim.SetTrigger ("Attacking");
	}

	void CatchAttackPowerUp(){

		damageBonus += 2;

	}

	void CatchArmorPowerUp(){
		
		armorBonus += 1;
		
	}

	void CatchHealthPowerUp(){

		if(life < startingLife)
			life += 10;
		health.value = life;
		
	}

	void Heal(CitizenBehaviour citizen){
		citizen.Life = 5;
	}

	void Patrol(){
		GameObject[] patrolPoints = GameObject.FindGameObjectsWithTag ("PatrolDestinations");
		float distanceToClosest = float.MaxValue;

		for (int i = 0; i < patrolPoints.Length; i++) {
			float distanceToPoint = Vector3.Distance(this.transform.position, patrolPoints[i].transform.position);
			if(distanceToPoint < distanceToClosest){
				followedObject = patrolPoints[i];
				distanceToClosest = distanceToPoint;
			}
		}

		agent.SetDestination (followedObject.transform.position);
		onPatrol = true;
	}

	
	IEnumerator WaitForStuff(float waitTime) {
		yield return new WaitForSeconds(waitTime);
		
	}
}
