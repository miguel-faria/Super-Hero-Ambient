using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ConverterVillainBehaviour : MonoBehaviour
{
	public Slider health;
	public Text outputConvertedCitizens;
	public Text outputRemainingCitizens;
	public int startingLife;
	int remainingCitizens;
	int convertedCitizens;
	int life;
	GameObject hero;
	HeroBehaviour heroBehaviour;

	
	// Use this for initialization
	void Start ()
	{
		Time.timeScale = 1;
		life = startingLife;
		convertedCitizens = 0;
		hero = GameObject.FindGameObjectWithTag ("Hero");
		heroBehaviour = hero.GetComponent<HeroBehaviour> ();
	}
	
	// Update is called once per frame
	void Update ()
	{
		//remainingCitizens = int.Parse (outputRemainingCitizens.text);
	}
}

