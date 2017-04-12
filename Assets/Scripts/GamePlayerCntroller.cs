using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePlayerCntroller : MonoBehaviour
{
	[System.Serializable]
	public class MoveSettings
	{
		public float forwardVel = 500;
		public float verticalVel = 100;
		public float horizontalVel = 100;
		public float rotateVel = 100;
	}

	[System.Serializable]
	public class InputSettings
	{
		public float inputDelay = 0.1f;
		public string VERTICAL_AXIS = "PlayerVertical";
		public string HORIZONTAL_AXIS = "PlayerHorizontal";
		//public string TURN_AXIS = "Horizontal";
	}

	public MoveSettings moveSettings = new MoveSettings();
	public InputSettings inputSettings = new InputSettings();

	Vector3 velocity = Vector3.zero;
	Quaternion targetRotation;
	Rigidbody rBody;
	float verticalInput, horizontalInput, turnInput;

	private void Start()
	{
		targetRotation = transform.rotation;

		if (GetComponent<Rigidbody>())
			rBody = GetComponent<Rigidbody>();
		else
			Debug.LogError("No rigidbody!");

		verticalInput = horizontalInput = turnInput = 0;
	}

	private void GetInput()
	{
		verticalInput = Input.GetAxis(inputSettings.VERTICAL_AXIS);
		horizontalInput = Input.GetAxis(inputSettings.HORIZONTAL_AXIS);
		

		//turnInput = Input.GetAxis(inputSettings.TURN_AXIS);
	}

	private void Update()
	{
		GetInput();
	}

	private void FixedUpdate()
	{
		MoveForward();
		//Turn();

		rBody.velocity = transform.TransformDirection(velocity);
	}
	private void MoveForward()
	{
		// move
		velocity.z = moveSettings.forwardVel * Time.deltaTime;
		
		velocity.y = moveSettings.verticalVel * verticalInput * Time.deltaTime;
		
		velocity.x = moveSettings.horizontalVel * horizontalInput * Time.deltaTime;
	}

	private void Turn()
	{
		if (Mathf.Abs(turnInput) > inputSettings.inputDelay)
		{
			targetRotation *= Quaternion.AngleAxis(moveSettings.rotateVel * turnInput * Time.deltaTime, Vector3.up);
			transform.rotation = targetRotation;
		}
	}
}
