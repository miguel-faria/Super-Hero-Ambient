using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HeroBehaviour : MonoBehaviour {

	const float TASKFOCUSTIME = 10f;
	
	public Slider health;
	public Text outputSavedCitizens;
	public Text outputRemainingCitizens;
	public int startingLife;
	public int startingCitizens;
	int remainingCitizens;
	int savedCitizens;
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
	
	Animator anim;
	NavMeshAgent agent;
	AnimatorStateInfo state;
	Transform[] destinations;


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

	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void Attacked(GameObject attacker) {

		int damage = Random.Range (1, 6);
		if (life > 0) {
			life -= damage;
			health.value = life;
			Debug.Log("Hero := Took " + damage + " damage!");
		}else {
			inCombat = false;

			Debug.Log("I'm dead YO!!!!!!!!! - Hero");
			Destroy(gameObject);
		}

	}
}
