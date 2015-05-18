using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace SuperHeroAmbient{
	
	enum villainPerceptionTypes {Saw, Heard, Touched, Message}
	enum villainBeliefTypes {See, Hear, Touching, ConverterInDanger}
	enum villainDesireTypes {Convert, Follow, DefendAgainstHero, AttackCitizen, DefendOtherVillain, Flee}
	enum villainIntentionTypes {Move, FollowSound, Approach, Attack, Convert, Flee, KillHero, AskHelp}
	enum heroPerceptionType {Saw, Heard, Touched, SuperSpeedAvailable}
	enum heroBeliefTypes {See, Hear, Touching, CanUseSuperSpeed}
	enum heroDesireTypes {Save, Follow, DefendAgainstVillain, Pick, HealCrush, AvengeCrush, DefeatVillain}
	enum heroIntentionTypes {Move, FollowSound, AttackVillain, SaveCitizen, PickupPowerUp, SaveCrush, HealCrush, Revenge, KillVillain}

	class Definitions{
		public const float TASKFOCUSTIME = 7.5f; // Time a BDI agent focus on a task
		public const float TASKFOCUSTIMEREACTIVE = 5.0f; // Time the hero does a reactive action
		public const float FIELDOFVIEWANGLE = 110.0f; // Number of degrees, centred on forward, for the enemy see.
		public const float VILLAINMAXVIEWDISTANCE = 17.5f;
		public const float HEROMAXVIEWDISTANCE = 20.0f;
		public const float MAXTOUCHINGDISTANCE = 2.0f;
		public const float AOEAREA = 4.0f;
		public const float AOERUNNINGDISTANCE = 12.0f;
		public const float HEROMAXHEARINGDISTANCE = 20.0f;
		public const float VILLAINMAXHEARINGDISTANCE = 17.5f;
		public const float AOEHEARINGAREA = 4.0f;
		public const float SUPERSENSESAREA = 35.0f;
		public const float SUPERSPEEDCOOLDOWN = 10.0f;
		public const float SUPERSPEEDMAXTIME = 7.5f;
		public const float MAXATTACKDISTANCE = 2.0f;
		public const float SUPERHEARINGAREA = 35.0f;
	}
	
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

	class ConverterInDangerBelief : Belief{
		Vector3 _villainPosition;
		
		public ConverterInDangerBelief(GameObject villain) : base ((int)villainBeliefTypes.ConverterInDanger, "Converter is in danger", villain){
			_villainPosition = villain.transform.position;
		}
		
		public Vector3 CitizenPosition {
			get {
				return _villainPosition;
			}
			set {
				_villainPosition = value;
			}
		}
		
	}
	
	class SeeCitizenBelief : Belief{
		
		Vector3 _citizenPosition;
		
		public SeeCitizenBelief(GameObject citizen) : base ((int)villainBeliefTypes.See, "Saw Citizen", citizen){
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
		
		public HearScreamBelief (GameObject screamer, Vector3 screamOrigin) : base((int)villainBeliefTypes.Hear, "Heard a Scream", screamer) {
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
		
		public SeeHeroBelief (GameObject hero): base((int)villainBeliefTypes.See, "Saw the Hero", hero){
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
		
		public TouchHeroBelief (GameObject hero): base((int)villainBeliefTypes.Touching, "Touch the Hero", hero){
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

	class TouchVillainBelief : Belief {

		Vector3 _villainPosition;
		
		public TouchVillainBelief (GameObject villain): base((int)heroBeliefTypes.Touching, "Touch a Villain", villain){
			_villainPosition = villain.transform.position;
		}
		
		public Vector3 VillainPosition {
			get {
				return _villainPosition;
			}
			set {
				_villainPosition = value;
			}
		}
	}
	
	class TouchCitizenBelief : Belief {
		Vector3 _citizenPosition;
		
		public TouchCitizenBelief (GameObject citizen): base((int)heroBeliefTypes.Touching, "Touch a Citizen", citizen){
			_citizenPosition = citizen.transform.position;
		}
		
		public Vector3 VillainPosition {
			get {
				return _citizenPosition;
			}
			set {
				_citizenPosition = value;
			}
		}
	}

	class SeeVillainBelief : Belief {
		
		Vector3 _villainPosition;
		
		public SeeVillainBelief (GameObject villain): base((int)heroBeliefTypes.See, "Saw a Villain", villain){
			_villainPosition = villain.transform.position;
		}
		
		public Vector3 VillainPosition {
			get {
				return _villainPosition;
			}
			set {
				_villainPosition = value;
			}
		}
	}

	class SeeCrushBelief : Belief{
		
		Vector3 _crushPosition;
		
		public SeeCrushBelief(GameObject crush) : base ((int)heroBeliefTypes.See, "Saw Her... Oh I love her...", crush){
			_crushPosition = crush.transform.position;
		}
		
		public Vector3 CrushPosition {
			get {
				return _crushPosition;
			}
			set {
				_crushPosition = value;
			}
		}
	}

	class SeePowerUPBelief : Belief {

		Vector3 _powerUpPosition;
		string _powerUpType;

		public SeePowerUPBelief(GameObject powerUp) : base((int)heroBeliefTypes.See, "Saw a PowerUp", powerUp){
			_powerUpPosition = powerUp.transform.position;
			_powerUpType = powerUp.tag;
		}

		public Vector3 PowerUpPosition {
			get {
				return _powerUpPosition;
			}
			set {
				_powerUpPosition = value;
			}
		}

		public string PowerUpType {
			get {
				return _powerUpType;
			}
			set {
				_powerUpType = value;
			}
		}
	}

	class CanSuperSpeedBelief : Belief{
				
		public CanSuperSpeedBelief(GameObject hero) : base ((int)heroBeliefTypes.CanUseSuperSpeed, "Can Use Super Speed", hero){}
	}

	class Desire{
		
		int _type;
		string _desireName;
		GameObject _subjectObject;
		Vector3 _objectiveDestination;
		float _preferenceFactor;
		
		public Desire(int type, string desireName, GameObject desireObject, float preferenceFactor){
			_type = type;
			_desireName = desireName;
			_subjectObject = desireObject;
			_objectiveDestination = desireObject.transform.position;
			_preferenceFactor = preferenceFactor;
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

		public float PreferenceFactor {
			get {
				return _preferenceFactor;
			}
			set {
				_preferenceFactor = value;
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

}
