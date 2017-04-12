using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerEnter : MonoBehaviour {

	private void OnTriggerEnter(Collider other)
	{
		Debug.Log("Trigger");
		Debug.Log(other.name);
		Debug.Log(this.name);
	}
}
