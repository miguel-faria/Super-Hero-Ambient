using UnityEngine;
using System.Collections;

public class CitizenManager : MonoBehaviour {

	public GameObject MagicAura = null;
	public bool converted;
	public GameObject convertedCitizen;
	Vector3 position;
	GameObject child, aura;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {

		position = transform.GetChild (0).transform.position;
		if (aura != null){
			//Debug.Log("Aura update");
			aura.transform.position = position;
	}
		if (converted) {
			converted = false;
			StartCoroutine(Conversion());
		}
	
	}



	IEnumerator Conversion(){

		//Debug.Log ("conversao");
		Quaternion rotation = new Quaternion ();
		aura = (GameObject)Instantiate (MagicAura, transform.position, rotation);
		aura.transform.SetParent (transform);
		yield return new WaitForSeconds(1.3f);
		child = transform.GetChild (0).gameObject;
		Destroy (child);
		child = (GameObject)Instantiate (convertedCitizen, position, rotation);
		child.transform.parent = this.transform;
		child.GetComponent<CitizenBehaviour>().transformationState = 3;
		//child.GetComponent<CitizenBehaviour>.isWalking = true;
		//return true;

	}




}
