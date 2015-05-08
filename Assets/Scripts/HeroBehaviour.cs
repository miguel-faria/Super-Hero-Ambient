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
}
