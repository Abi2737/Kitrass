using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
	[System.Serializable]
	public class MoveSettings
	{
		public float forwardVel = 500;
		public float rotateVel = 100;
	}

	[System.Serializable]
	public class InputSettings
	{
		public float inputDelay = 0.1f;
		public string FORWARD_AXIS = "Vertical";
		public string TURN_AXIS = "Horizontal";
	}

	public MoveSettings moveSettings = new MoveSettings();
	public InputSettings inputSettings = new InputSettings();

	Quaternion targetRotation;
	Rigidbody rBody;
	float forwardInput, turnInput;

	private void Start()
	{
		targetRotation = transform.rotation;

		if (GetComponent<Rigidbody>())
			rBody = GetComponent<Rigidbody>();
		else
			Debug.LogError("No rigidbody!");

		forwardInput = turnInput = 0;
	}

	private void GetInput()
	{
		forwardInput = Input.GetAxis(inputSettings.FORWARD_AXIS);
		turnInput = Input.GetAxis(inputSettings.TURN_AXIS);
	}

	private void Update()
	{
		GetInput();
		Turn();
	}

	private void FixedUpdate()
	{
		MoveForward();
	}

	private void MoveForward()
	{
		if ( Mathf.Abs(forwardInput) > inputSettings.inputDelay)
		{
			// move
			rBody.velocity = transform.forward * forwardInput * moveSettings.forwardVel * Time.deltaTime;
		}
		else
		{
			// zero velocity
			rBody.velocity = Vector3.zero;
		}
	}

	private void Turn()
	{
		targetRotation *= Quaternion.AngleAxis(moveSettings.rotateVel * turnInput * Time.deltaTime, Vector3.up);
		transform.rotation = targetRotation;
	}




	//private Rigidbody _playerRigidbody;

	//private float _speed = 5.0f;
	//private float _turnSpeed = 2.0f;

	//// Use this for initialization
	//void Start ()
	//{
	//	_playerRigidbody = gameObject.GetComponent<Rigidbody>();

	//	if (_playerRigidbody == null)
	//		Debug.Log("Could not get the player rigid body!");

	//	_playerRigidbody.useGravity = false;		
	//}

	////void FixedUpdate()
	////{
	////	if ( Input.GetKeyDown(KeyCode.UpArrow) )
	////	{
	////		playerRigidbody.AddRelativeForce(Vector3.forward * speed);
	////	}
	////}


	//// Update is called once per frame
	//void Update ()
	//{
	//	if (Input.GetKey(KeyCode.UpArrow))
	//	{
	//		transform.Translate(Vector3.forward * _speed * Time.deltaTime);
	//	}

	//	if ( Input.GetKey(KeyCode.DownArrow) )
	//	{
	//		transform.Translate(Vector3.back * _speed * Time.deltaTime);
	//	}

	//	if ( Input.GetKey(KeyCode.RightArrow) )
	//	{
	//		transform.Rotate(Vector3.up, _turnSpeed);
	//	}

	//	if (Input.GetKey(KeyCode.LeftArrow))
	//	{
	//		transform.Rotate(Vector3.up, -_turnSpeed);
	//	}
	//}
}
