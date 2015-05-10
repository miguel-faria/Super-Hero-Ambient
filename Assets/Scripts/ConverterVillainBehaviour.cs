using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

enum perceptionType {Saw, Heard, Touched}
enum beliefTypes {See, Hear, Touching}
enum desireTypes {Convert, Follow, Fight, Flee}
enum intentionTypes {Move, FollowSound, Attack, Convert, Flee, KillHero, AskHelp}

//Utility classes
class Perception{

	string _tag;
	GameObject _objectPercepted;
	int _type;

	public Perception(GameObject perception, int type){
		_objectPercepted = perception;
		_type = type;
		_tag = _objectPercepted.tag;
	}

	public string Tag {
		get {
			return _tag;
		}
		set {
			_tag = value;
		}
	}	

	public GameObject ObjectPercepted {
		get {
			return _objectPercepted;
		}
		set {
			_objectPercepted = value;
		}
	}

	public int Type {
		get {
			return _type;
		}
		set {
			_type = value;
		}
	}
}

class Belief{

	int _type;
	string _description;
	GameObject _beliefObject;

	public Belief(int type, string description, GameObject beliefObject){
		_type = type;
		_description = description;
		_beliefObject = beliefObject;
	}

	public int Type {
		get {
			return _type;
		}
		set {
			_type = value;
		}
	}

	public string Description {
		get {
			return _description;
		}
		set {
			_description = value;
		}
	}

	public GameObject BeliefObject{
		get {
			return _beliefObject;
		}
		set {
			_beliefObject = value;
		}
	}
}

class SeeCitizenBelief : Belief{

	Vector3 _citizenPosition;

	public SeeCitizenBelief(GameObject citizen) : base ((int)beliefTypes.See, "Saw Citizen", citizen){
		_citizenPosition = citizen.transform.position;
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

	Vector3 _screamOrigin;

	public HearScreamBelief (GameObject screamer, Vector3 screamOrigin) : base((int)beliefTypes.Hear, "Heard a Scream", screamer) {
		_screamOrigin = screamOrigin;
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

	Vector3 _heroPosition;

	public SeeHeroBelief (GameObject hero): base((int)beliefTypes.See, "Saw the Hero", hero){
		_heroPosition = hero.transform.position;
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

	Vector3 _heroPosition;
	
	public TouchHeroBelief (GameObject hero): base((int)beliefTypes.Touching, "Touch the Hero", hero){
		_heroPosition = hero.transform.position;
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

	int _type;
	string _desireName;
	GameObject _subjectObject;
	Vector3 _objectiveDestination;

	public Desire(int type, string desireName, GameObject desireObject){
		_type = type;
		_desireName = desireName;
		_subjectObject = desireObject;
		_objectiveDestination = desireObject.transform.position;
	}

	public int Type {
		get {
			return _type;
		}
		set {
			_type = value;
		}
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

	int _type;
	string _description;
	GameObject _intentObject;
	bool _concluded;
	bool _possible;
	float _distanceToDestination;
	Vector3 _soundOrigin;

	public Intention(int type, string description, GameObject intentObject, float distance){
		_type = type;
		_description = description;
		_intentObject = intentObject;
		_concluded = false;
		_possible = true;
		_distanceToDestination = distance;
		_soundOrigin = Vector3.zero;
	}

	public Intention(int type, string description, GameObject intentObject, float distance, Vector3 soundOrigin){
		_type = type;
		_description = description;
		_intentObject = intentObject;
		_concluded = false;
		_possible = true;
		_distanceToDestination = distance;
		_soundOrigin = soundOrigin;
	}

	public int Type {
		get {
			return _type;
		}
		set {
			_type = value;
		}
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

	public float DistanceToDestination {
		get {
			return _distanceToDestination;
		}
		set {
			_distanceToDestination = value;
		}
	}

	public Vector3 SoundOrigin {
		get {
			return _soundOrigin;
		}
		set {
			_soundOrigin = value;
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
	public Text inputKilledCitizens;
	public int startingLife;
	int remainingCitizens;
	int convertedCitizens;
	int killedCitizens;
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
	Vector3 citizenPos = new Vector3();
	GameObject[] citizens = null;
	CitizenBehaviour citizenSc = null;
	bool citizenInView = false;
	bool noPerception = true;
	float citizenInViewTime;
	float convertTime;
	float attackTime;

	Animator anim;
	NavMeshAgent agent;
	AnimatorStateInfo state;
	Transform[] destinations;

	List<Perception> screamPerceps = new List<Perception>();
	List<Belief> beliefs = new List<Belief>();
	List<Desire> desires = new List<Desire>();
	Intention intention;

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
		inputKilledCitizens.text = "" + 0;
		killedCitizens = int.Parse(inputKilledCitizens.text);
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

		beliefs = updateBeliefs (beliefs);
		desires = updateDesires (beliefs, desires);

		followedObject = destinations [index].gameObject;
		agent.SetDestination (followedObject.transform.position);
		intention = new Intention ((int)intentionTypes.Move, "Move Randomly", followedObject, 
		                           Vector3.Distance(this.transform.position, followedObject.transform.position));
		isFollowing = true;
		Debug.Log ("Villain: " + agent.destination);
		anim.SetBool ("isWalking", true);
		state = anim.GetCurrentAnimatorStateInfo (0);

		lastDecisionTime = Time.time;
		time = Time.time;
		citizenInViewTime = float.MaxValue;
		convertTime = float.MinValue;
		attackTime = float.MinValue;
	}
	
	// Update is called once per frame
	void Update ()
	{
		remainingCitizens = int.Parse (outputRemainingCitizens.text);
		killedCitizens = int.Parse (inputKilledCitizens.text);
		beliefs = updateBeliefs(beliefs);

		if(intention.Possible && !intention.Concluded && ((Time.time - lastDecisionTime) < TASKFOCUSTIME)){
			executeIntention(intention);
		}
		else {
			desires = updateDesires(beliefs, desires);
			intention = updateIntention(beliefs, desires, intention);
			executeIntention(intention);
			lastDecisionTime = Time.time;
		}

		//updateAnimation (intention.IntentObject);
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
		Debug.Log ("Executing Intention: " + intention.Description);
		if (intention.IntentObject == null) {
			intention.Possible = false;
			return;
		}

		if (intention.Type == (int)intentionTypes.Attack) {
			if(HeroIsDead()){
				intention.Concluded = true;
				inCombat = false;
			} else{
				if(Vector3.Distance(this.transform.position, intention.IntentObject.transform.position) <= 1.5f)
					Attack(hero);
				else{
					intention.Possible = false;
					inCombat = false;
				}
			}
		} else if (intention.Type == (int)intentionTypes.Convert) {
			citizenSc = (CitizenBehaviour) intention.IntentObject.GetComponent(typeof(CitizenBehaviour));
			citizenPos = intention.IntentObject.transform.position;
			if(CitizenInRange(intention.IntentObject)){
				if(CitizenIsEvil(citizenSc))
					intention.Concluded = true;
				else if(CitizenIsDead(citizenSc)){
					intention.Possible = false;
				}else if(((Time.time - convertTime) >= 1.0f)){
					Convert(citizenSc);
					for(int i = 0; i < citizens.Length; i++){
						if((citizens[i] != intention.IntentObject) && (CitizenInRange(citizens[i])) && 
						   (!CitizenIsEvil((CitizenBehaviour) citizens[i].GetComponent(typeof(CitizenBehaviour))))){
							Convert((CitizenBehaviour) citizens[i].GetComponent(typeof(CitizenBehaviour)));
						}
					}
					convertTime = Time.time;
				}
			} else{
				if(CitizenIsDead(citizenSc))
					intention.Possible = false;
				else{
					Follow(citizenPos);
				}
			}
		} else if (intention.Type == (int)intentionTypes.Flee) {
			if(!HeroInRange()){
				intention.Concluded = true;
				StopFollowing();
			}else if(HeroIsDead()){
				intention.Possible = false;
				StopFollowing();
			}else{
				Vector3 objective = hero.transform.position - transform.position;
				Follow(objective.normalized);
			}
		} else if (intention.Type == (int)intentionTypes.FollowSound) {
			if((intention.SoundOrigin != Vector3.zero) &&
			   (Vector3.Distance(this.transform.position, intention.IntentObject.transform.position) == 0))
				intention.Concluded = true;
			else{
				followedObject = intention.IntentObject;
				Follow(followedObject.transform.position);
			}
		} else if (intention.Type == (int)intentionTypes.Move) {
			if (intention.Type == (int)intentionTypes.Move && desires != null){
				Debug.Log("Concluded Move Randomly");
				intention.Concluded = true;
			}else {
				RandomWalk();
			}
		} else {
			Debug.Log("Intention type not recognized!!");
		}
	}
	
	List<Perception> getCurrentPerceptions(){
		Debug.Log ("Getting perceptions from the world");
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
				perceptions.Add(new Perception(citizens[i], (int)perceptionType.Saw));
		}
		
		if (isTouching (hero)) {
			perceptions.Add (new Perception (hero, (int)perceptionType.Touched));
		} else if (inSight (hero)) {
			perceptions.Add (new Perception (hero, (int)perceptionType.Saw));
		}

		return perceptions;
	}

	List<Belief> updateBeliefs(List<Belief> oldBeliefs){
		Debug.Log ("Updating Beliefs List");
		List<Belief> newBeliefs = new List<Belief> (oldBeliefs);
		List<Perception> perceptions = getCurrentPerceptions ();

		foreach (Perception percep in perceptions) {
			if(!alreadyInBeliefs(percep, newBeliefs)){
				if (percep.Type == (int)perceptionType.Heard){
					newBeliefs.Add(new HearScreamBelief(percep.ObjectPercepted, percep.ObjectPercepted.transform.position));
				} else if (percep.Type == (int)perceptionType.Saw){
					if(percep.Tag.Equals("Hero")){
						newBeliefs.Add(new SeeHeroBelief(percep.ObjectPercepted));
					}else if(percep.Tag.Equals("Citizen")){
						newBeliefs.Add(new SeeCitizenBelief(percep.ObjectPercepted));
					}
				} else if (percep.Type == (int)perceptionType.Touched && percep.Tag.Equals("Hero")){
					newBeliefs.Add(new TouchHeroBelief(percep.ObjectPercepted));
				}
			}
		}

		return newBeliefs;
	}

	List<Desire> updateDesires(List<Belief> beliefs, List<Desire> oldDesires){
		Debug.Log ("Updating Desires List");
		List<Desire> newDesires = new List<Desire> (oldDesires);

		foreach (Belief belief in beliefs) {
			if(!alreadyInDesires(belief, newDesires)){
				if(belief.Type == (int)beliefTypes.Hear){
					newDesires.Add(new Desire((int)desireTypes.Follow, "Follow Scream", belief.BeliefObject));
				}else if((belief.Type == (int)beliefTypes.See) && (belief is SeeCitizenBelief) && !alreadyConverted(belief.BeliefObject)){
					newDesires.Add(new Desire((int)desireTypes.Convert, "Convert Citizen", belief.BeliefObject));
				}else if((belief.Type == (int)beliefTypes.See) && (belief is SeeHeroBelief)){
					newDesires.Add(new Desire((int)desireTypes.Flee, "Flee from Hero", belief.BeliefObject));
				}else if(belief.Type == (int)beliefTypes.Touching){
					newDesires.Add(new Desire((int)desireTypes.Fight, "Fight the Hero", belief.BeliefObject));
               	}
			}
		}

		return newDesires;
	}

	Intention updateIntention(List<Belief> beliefs, List<Desire> desires, Intention oldIntention){
		Debug.Log ("Updating Intention");
		Intention newIntention;
		if (intention.Type != (int)intentionTypes.Move)
			newIntention = oldIntention;
		else {
			newIntention = new Intention(oldIntention.Type, oldIntention.Description, oldIntention.IntentObject, float.MaxValue);
		}
		Vector3 currentPosition = this.transform.position;
		Desire chosenDesire = null;

		if (desires != null) {
			if (!inCombat && ((remainingCitizens + convertedCitizens + killedCitizens) < (Mathf.FloorToInt (0.4f * heroBehaviour.startingCitizens)))
				&& (existsBelief<SeeHeroBelief> ((int)beliefTypes.See, beliefs))) {
				newIntention = new Intention ((int)intentionTypes.KillHero, "Kill the Hero", hero,
				                              Vector3.Distance (currentPosition, hero.transform.position));
			} else if (inCombat && (life < 40) && existsDesire ((int)desireTypes.Flee, desires) && (hero != null)) {
				newIntention = new Intention ((int)intentionTypes.AskHelp, "Flee from Combat and Ask for Help", hero, 
				                              Vector3.Distance (currentPosition, hero.transform.position));
			} else {
				foreach (Desire desire in desires) {
					if (Vector3.Distance (this.transform.position, desire.ObjectiveDestination) < newIntention.DistanceToDestination) {
						if (desire.Type == (int)desireTypes.Convert){
							newIntention = new Intention ((int)intentionTypes.Convert, "Convert Citizen", desire.SubjectObject,
							                             Vector3.Distance (currentPosition, desire.ObjectiveDestination));
							chosenDesire = desire;
						}
						else if (desire.Type == (int)desireTypes.Fight){
							newIntention = new Intention ((int)intentionTypes.Attack, "Attack Hero", desire.SubjectObject,
							                              Vector3.Distance (currentPosition, desire.ObjectiveDestination));
							chosenDesire = desire;
						}
						else if (desire.Type == (int)desireTypes.Flee){
							newIntention = new Intention ((int)intentionTypes.Flee, "Flee from Hero", desire.SubjectObject,
							                              Vector3.Distance (currentPosition, desire.ObjectiveDestination));
							chosenDesire = desire;
						}
						else if (desire.Type == (int)desireTypes.Follow){
							newIntention = new Intention ((int)intentionTypes.FollowSound, "Follow Sound", desire.SubjectObject,
							                              Vector3.Distance (currentPosition, desire.ObjectiveDestination),
							                              desire.ObjectiveDestination);
							chosenDesire = desire;
						}
					}
				}
				if(chosenDesire != null){
					desires.Remove(chosenDesire);
				}
			}
		}
		else{
			GameObject dest = destinations [Random.Range(0, destinations.Length)].gameObject;
			newIntention = new Intention((int)intentionTypes.Move, "Move Randomly", dest,
			                             Vector3.Distance(currentPosition, dest.transform.position));
		}

		return newIntention;
	}

	bool alreadyInBeliefs(Perception perception, List<Belief> beliefsList){
		foreach (Belief belief in beliefsList) {
			if ((((belief.Type == (int)beliefTypes.Hear) && (perception.Type == (int)perceptionType.Heard)) ||
				((belief.Type == (int)beliefTypes.See) && (perception.Type == (int)perceptionType.Saw)) ||
				((belief.Type == (int)beliefTypes.Touching) && (perception.Type == (int)perceptionType.Touched)))
				&& (belief.BeliefObject.Equals (perception.ObjectPercepted))) {
				return true;
			}
		}
		return false;
	}

	bool alreadyInDesires(Belief belief, List<Desire> desiresList){
		foreach(Desire desire in desiresList){
			if((((belief.Type == (int)beliefTypes.Hear) && (desire.Type == (int)desireTypes.Follow)) || 
			    ((belief.Type == (int)beliefTypes.See) && ((desire.Type == (int)desireTypes.Convert) || 
			                                           		(desire.Type == (int)desireTypes.Flee))) ||
			    ((belief.Type == (int)beliefTypes.Touching) && (desire.Type == (int)desireTypes.Fight)))
			   && (desire.SubjectObject.Equals(belief.BeliefObject))){
				Debug.Log("Already in Desire List! List size = " + desiresList.Count);
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
		return ((Vector3.Distance(this.transform.position, hero.transform.position) < 30.0f) &&
		        (!InSightHero(this.gameObject)));
	}

	bool InSightHero(GameObject other){
		float distance = Vector3.Distance (hero.transform.position, other.transform.position);
		Vector3 direction = other.transform.position - this.transform.position;
		float angle = Vector3.Angle (direction, this.transform.forward);	
		// If the angle between forward and where the player is, is less than half the angle of view...
		if (distance < 35.0f && angle < fieldOfViewAngle * 0.5f) {
			Debug.Log ("Saw Citizen");
			return true;
		} else {
			return false;
		}

	}

	bool CitizenInRange(GameObject citizen)
	{
		float distance = Vector3.Distance (this.transform.position, citizen.transform.position);
		return (distance <= 4.0f);
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
			if (distance < 35.0f && angle < fieldOfViewAngle * 0.5f) {
				Debug.Log ("Saw Citizen");
				return true;
			}
		}
		if(other.CompareTag("Hero")){
			if (distance > 1.5f && distance < 30.0f && angle < fieldOfViewAngle * 0.5f){
				Debug.Log("Saw Hero");
				return true;
			}
		}
		
		return false;
	}
	
	bool isTouching(GameObject other){
		float distance = Vector3.Distance (this.transform.position, other.transform.position);
		if (distance <= 1.5f && distance >= 0.0f){
			return true;
		}
		return false;
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
		Debug.Log ("Following " + followedObject.ToString()); 
		noPerception = true;
		if (followedObject.CompareTag ("Citizen"))
			citizenSc = null;
		followedObject = null;
		time = float.MaxValue;
	}
	
	void Convert (CitizenBehaviour citizen)
	{
		Debug.Log ("Converting Citizen " + citizen.name); 
		citizen.Converted ();
		if (citizen.IsEvil ()) {
			remainingCitizens--;
			outputRemainingCitizens.text = "" + remainingCitizens;
			convertedCitizens++;
			outputConvertedCitizens.text = "" + convertedCitizens;
		}
		UpdateAnimations (false, false, false, true, true);
	}
	
	void Attack(GameObject attacked)
	{
		if (Time.time - attackTime >= 1f) {
			UpdateAnimations (false, false, true, true, false);
			if(attacked.CompareTag("Citizen")){
				Debug.Log ("Attacking Citizen " + attacked.ToString());
				citizenSc.Attacked ();
			}else if(attacked.CompareTag("Hero")){
				Debug.Log ("Attacking Hero");
				heroBehaviour.Attacked(this.gameObject);
			}
			attackTime = Time.time;
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
		UpdateAnimations (true, false, false, true, false);
	}
	
	void RandomWalk()
	{
		agent.Resume();
		agent.SetDestination (destinations [Random.Range (0, destinations.Length)].position);
		agent.speed = 3.5f;
		UpdateAnimations (true, false, false, true, false);
		time = Time.time;
	}
	
	void HeardScream(GameObject screamer){
		Debug.Log ("Converter Villain - Event Scream!!");
		float distance = Vector3.Distance (this.transform.position, screamer.transform.position);
		if (distance > 4.0f && distance < 40.0f) {
			screamPerceps.Add(new Perception(screamer, (int)perceptionType.Heard));
		}
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
}

