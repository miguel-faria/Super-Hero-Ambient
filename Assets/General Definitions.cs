using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace SuperHeroAmbient{

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

}
