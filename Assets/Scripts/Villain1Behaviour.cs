﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class Villain1Behaviour : MonoBehaviour {

	public Slider villain1Health;
	public Text outputKilledCitizens;
	public Text outputRemainingCitizens;
	public int startingCitizens;
	public int startingHealth;
	int killedCitizens;
	int remainingCitizens;
	int neededSaves;
	int life;
	int speed;
	float collision_time;
	float last_attack;
	bool inCombat = false;
	Collider object_collided;
	GameObject hero;
	HeroBehaviour heroBehaviour;
	
	// Use this for initialization
	void Start () {
		Time.timeScale = 1;
		killedCitizens = 0;
		remainingCitizens = startingCitizens;
		neededSaves = Mathf.RoundToInt (0.75f * startingCitizens);
		life = startingHealth;
		speed = 20;
		collision_time = float.MaxValue;
		last_attack = Time.time;
		hero = GameObject.FindGameObjectWithTag ("Hero");
		heroBehaviour = hero.GetComponent<HeroBehaviour> ();
		outputKilledCitizens.text = "" + killedCitizens;
	}
	
	// Update is called once per frame
	void Update () {
		updateTime ();
		if (Time.timeScale > 0) {
			if (inCombat) { 
				if ((Time.time - last_attack) > 1f) {
					Debug.Log ("Villain Attacks!!!");
					heroBehaviour.attacked ();
					last_attack = Time.time;
				}
			} else if (Time.time - collision_time > 0.20f)
				crossCollision ();
			else 
				move ();
		}
		
	}
	
	void updateTime(){

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
			transform.Rotate (0, 180, 0);
		else if (collider.gameObject.tag == "Citizen")
			citizenCollision (collider);
		else if (collider.gameObject == hero) {
			Debug.Log ("Villain Attacks!!!");
			inCombat = true;
			heroBehaviour.attacked ();
		}
	}
	
	void citizenCollision(Collider collider)
	{
		remainingCitizens--;
		Debug.Log ("Villain killed citizen!");
		Destroy(collider.gameObject);
		killedCitizens++;
		heroBehaviour.updateCitizens (remainingCitizens);
		outputKilledCitizens.text = "" + killedCitizens;
		outputRemainingCitizens.text = "" + remainingCitizens;
		if ((startingCitizens - killedCitizens) < neededSaves) {
			Debug.Log("Villain Wins!!");
			Time.timeScale = 0;
		}
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

	public void attacked()
	{
		int damage = Random.Range (1, 11);
		if (life > 0) {
			life -= damage;
			villain1Health.value = life;
			Debug.Log("Villain := Took " + damage + " damage!");
		}else
		{
			inCombat = false;
			if(heroBehaviour.getInCombat())
				heroBehaviour.setInCombat(false);
			Debug.Log("I'm dead YO!!!!!!!!! - Villain1");
			Destroy(gameObject);
			Debug.Log("Hero Wins!!");
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