using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CitizenBehaviour : MonoBehaviour {
	float collision_time;

	// Use this for initialization
	void Start () {
		collision_time = float.MaxValue;
	}
	
	// Update is called once per frame
	void Update () {
		if (Time.time - collision_time > 0.20f)
			crossCollision ();
		else  
			move ();
	}

	void move()
	{
		transform.Translate(Vector3.forward * Time.deltaTime * 20);
	}
	
	void OnTriggerEnter(Collider collider)
	{
		if (collider.gameObject.tag == "Cross")
			collision_time = Time.time;
		else if (collider.gameObject.tag == "Obstacle")
			obstacleCollision ();
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

	public void attacked()
	{
		Destroy(gameObject);
	}
	

}
