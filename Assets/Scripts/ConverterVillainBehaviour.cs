using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ConverterVillainBehaviour : MonoBehaviour
{
	const float TASKFOCUSTIME = 10f;

	public Slider health;
	public Text outputConvertedCitizens;
	public Text outputRemainingCitizens;
	public int startingLife;
	int remainingCitizens;
	int convertedCitizens;
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
	void Start ()
	{
		Time.timeScale = 1;
		life = startingLife;
		isAlive = true;
		inCombat = false;
		convertedCitizens = 0;
		//hero = GameObject.FindGameObjectWithTag ("Hero");
		//heroBehaviour = hero.GetComponent<HeroBehaviour> ();
		anim = GetComponent<Animator> ();
		agent = GetComponent<NavMeshAgent>();

		GameObject[] objects = GameObject.FindGameObjectsWithTag("Destination");
		GameObject[] citizens = GameObject.FindGameObjectsWithTag("Citizen");
		int objLength = objects.Length;
		int citizensLength = citizens.Length;
		destinations = new Transform[objLength + citizensLength/* + 1*/];
		for (int i = 0; i < objLength; i++)
			destinations [i] = objects [i].transform ;

		for (int i = 0; i < citizensLength; i++)
			destinations [objLength + i] = citizens[i].transform ;

		//destinations [objLength + citizensLength] = GameObject.FindGameObjectWithTag ("Hero").transform;
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
		isFollowing = true;
		Debug.Log ("Villain: " + agent.destination);
		anim.SetBool ("isWalking", true);
		state = anim.GetCurrentAnimatorStateInfo (0);

		lastDecisionTime = Time.time;
		time = Time.time;
	}
	
	// Update is called once per frame
	void Update ()
	{
		//remainingCitizens = int.Parse (outputRemainingCitizens.text);

		updateAnimation ();
	}

	void updateAnimation ()
	{
		state = anim.GetCurrentAnimatorStateInfo (0);
		//Debug.Log(state.IsName("idle"));
		//Debug.Log(agent.destination.Equals(GameObject.Find("Dest1").transform.position));
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
		
		if (state.IsName ("walk") && Time.time - time >= 5) {
			agent.SetDestination (destinations [Random.Range (0, destinations.Length)].position);
			Debug.Log (agent.destination);
			anim.SetBool ("isWalking", true);
			time = Time.time;
			return;
		}
	}
}

