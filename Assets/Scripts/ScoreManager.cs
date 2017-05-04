using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
	public Text scoreText;

	float scoreValue;

	// Use this for initialization
	void Start ()
	{
		scoreValue = 0;	
	}
	
	// Update is called once per frame
	void Update ()
	{
		scoreText.text = "Score: " + Mathf.Round(scoreValue);	
	}

	public void AddToScore(float value)
	{
		scoreValue += value;
	}
}
