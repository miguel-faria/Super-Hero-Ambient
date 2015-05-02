using UnityEngine;
using System.Collections;

public class GOTOscript : MonoBehaviour {

	// Use this for initialization
	Animator anim;
	NavMeshAgent agent;
	AnimatorStateInfo state;
	Transform *destinations;


	void Start () 
	{
		destinations = GameObject.Find ("Destinations").GetComponentsInChildren (Transform);
		anim = GetComponent<Animator> ();
		agent = GetComponent<NavMeshAgent>();
		agent.SetDestination(destinations[1].position);
		anim.SetBool ("isWalking", true);
		state = anim.GetCurrentAnimatorStateInfo (0);
	}
	
	// Update is called once per frame
	void Update () {


		state = anim.GetCurrentAnimatorStateInfo (0);
		//Debug.Log(state.IsName("idle"));
		Debug.Log(agent.destination.Equals(GameObject.Find("exit").transform.position));
		if (Input.GetKeyDown ("space")) {
			agent.Stop();
			anim.SetTrigger ("Laugh");
			anim.SetBool("isWalking",false);
			return;
		}

		if (state.IsName("idle")) {
			agent.Resume();
			agent.SetDestination(GameObject.Find("exit").transform.position);
			anim.SetBool("isWalking",true);
			return;
		}

		if (state.IsName("walk") && agent.destination.Equals(GameObject.Find("exit").transform.position)) {
			anim.SetBool("isWalking",false);
			return;
		}


	}
}
