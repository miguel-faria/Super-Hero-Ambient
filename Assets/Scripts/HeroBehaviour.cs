using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class HeroBehaviour : MonoBehaviour {

	public Slider heroHealth;
	public Text outputSavedCitizens;
	public Text outputRemainingCitizens;
	public int startingCitizens;
	public int startingHealth;
	int savedCitizens;
	int remainingCitizens;
	int life;
	int neededSaves;
	int cooldown;
	int super_speed;
	int speed;
	float last_attack;
	float collision_time;
	bool hasSuperSpeed = false;
	bool inCombat = false;
	Collider object_collided;
	GameObject villain;
	Villain1Behaviour villainBehaviour;

	// Use this for initialization
	void Start () {
		savedCitizens = 0;
		Time.timeScale = 1;
		life = startingHealth;
		remainingCitizens = startingCitizens;
		neededSaves = Mathf.RoundToInt (0.75f * startingCitizens);
		cooldown = 200;
		super_speed = 50;
		speed = 20;
		collision_time = float.MaxValue;
		last_attack = Time.time;
		/*for (int i = 0; i < 3; i++) {
			v = GameObject.FindGameObjectWithTag("Villain"+(i+1));
			villains.Add(v);
			villainsBehaviour.Add(v.GetComponent("Villain"+(i+1)+"Behaviour"));
		}*/
		villain = GameObject.FindGameObjectWithTag ("Villain1");
		villainBehaviour = villain.GetComponent<Villain1Behaviour> ();
		outputSavedCitizens.text = "" + savedCitizens;
		outputRemainingCitizens.text = "" + remainingCitizens;
	}
	
	// Update is called once per frame
	void Update () {
		updateTime ();
		if (Time.timeScale > 0) {
			if (inCombat) {
				if ((Time.time - last_attack) > 1f) {
					Debug.Log ("Hero Attacks!!!");
					villainBehaviour.attacked ();
					last_attack = Time.time;
				} 
			} else if (!hasSuperSpeed && (cooldown <= 0))
				activateSpeed ();
			else if (hasSuperSpeed && (super_speed <= 0)) {
				speed = 20;
				hasSuperSpeed = false;
			} else {
				if (!hasSuperSpeed && (Time.time - collision_time > 0.20f))
					crossCollision ();
				else if (hasSuperSpeed && ((Time.time - collision_time) > 0.20f / 4))
					crossCollision ();
				else  
					move ();
			}
		}

	}

	void updateTime(){
		cooldown--;
		super_speed--;		
	}

	void activateSpeed(){
		Debug.Log ("Super Speed... ACTIVATE!!!");
		speed = 80;
		cooldown = 200;
		super_speed = 50;
		hasSuperSpeed = true;
	}

	void move()
	{
		transform.Translate(Vector3.forward * Time.deltaTime * speed);
	}
	
	void OnTriggerEnter(Collider collider)
	{
		if (collider.gameObject.tag == "Cross")
			collision_time = Time.time;
		else if (collider.gameObject.tag == "Obstacle") 
			obstacleCollision ();
		else if (collider.gameObject.tag == "Citizen")
			citizenCollision (collider);
		else if (collider.gameObject == villain) {
			Debug.Log("Hero Attacks!!!");
			inCombat = true;
			villainBehaviour.attacked ();
		}
	}

	void citizenCollision(Collider collider)
	{
		remainingCitizens--;
		Debug.Log ("Saved Citizen!");
		Destroy(collider.gameObject);
		savedCitizens++;
		villainBehaviour.updateCitizens (remainingCitizens);
		outputSavedCitizens.text = "" + savedCitizens;
		outputRemainingCitizens.text = "" + remainingCitizens;
		if (savedCitizens >= neededSaves) {
			Debug.Log("Hero Wins!!");
			Time.timeScale = 0;
		}
	}
	
	void obstacleCollision()
	{
		transform.Rotate(0, 180, 0);
	}
	
	void crossCollision()
	{
		List<int> list = new List<int>();
		list.Add(0);
		list.Add(90);
		list.Add(270);
		int directionIndex = Random.Range(0,3);
		transform.Rotate(0, list[directionIndex], 0);
		collision_time = float.MaxValue;
	}

	/*void villainCollision(MonoBehaviour villainBehaviour)
	{
		villainBehaviour.
	}*/

	public void attacked()
	{
		int damage = Random.Range (1, 11);
		if (life > 0) {
			life -= damage;
			heroHealth.value = life;
			Debug.Log("Hero := Took " + damage + " damage!");
		}else
		{
			inCombat = false;
			if(villainBehaviour.getInCombat())
				villainBehaviour.setInCombat(false);
			Debug.Log("I'm dead YO!!!!!!!!! - Hero");
			Destroy(gameObject);
			Debug.Log("Villain Wins!!");
			Time.timeScale = 0;
		}
	}

	public bool getInCombat(){
		return inCombat;
	}
	
	public void setInCombat(bool newInCombat){
		inCombat = newInCombat;
	}

	public void updateCitizens(int citizens){
		remainingCitizens = citizens;
	}
}