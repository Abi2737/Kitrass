using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BtnStart : MonoBehaviour
{
	public void OnButtonStartClick()
	{
		StarGame();
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Return))
		{
			StarGame();
		}
	}

	private void StarGame()
	{
		SceneManager.LoadScene(1);
	}
}
