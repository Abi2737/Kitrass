using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts;
using System;

public class GamePlayerController : MonoBehaviour
{
	[System.Serializable]
	public class MoveSettings
	{
		public float forwardVel = 500;
		public float verticalVel = 100;
		public float horizontalVel = 100;
		public float rotateVel = 100;

		public float turnSpeed = 10;
	}

	[System.Serializable]
	public class InputSettings
	{
		public float inputDelay = 0.1f;
		public string VERTICAL_AXIS = "PlayerVertical";
		public string HORIZONTAL_AXIS = "PlayerHorizontal";
		public string DEPTH_AXIS = "PlayerDepth";
		public string TURN_AXIS = "Horizontal";
	}

	public MoveSettings moveSettings = new MoveSettings();
	public InputSettings inputSettings = new InputSettings();

	Vector3 velocity = Vector3.zero;
	Quaternion targetRotation;
	Rigidbody rBody;
	float verticalInput, horizontalInput, turnInput;
	float depthInput;



	RoadGeneration.PieceEntry thePieceRoadWhereIam;
	int indChildNextPiece;
	bool pieceRoadChanged, rotatedOnSpecialPiece;

	float playerRotation;

	private void Start()
	{
		targetRotation = transform.rotation;

		if (GetComponent<Rigidbody>())
			rBody = GetComponent<Rigidbody>();
		else
			Debug.LogError("No rigidbody!");

		verticalInput = horizontalInput = turnInput = 0;
		depthInput = 0;


		thePieceRoadWhereIam = GameObject.Find("RoadGenerationGameObject").GetComponent<RoadGeneration>().GetRoadRoot();
		indChildNextPiece = 0;
		pieceRoadChanged = false;
		rotatedOnSpecialPiece = false;

		playerRotation = 0;
	}

	private void OnTriggerEnter(Collider other)
	{
		//Debug.Log(this.name + " trigger: othName: " + other.name);

		if (!pieceRoadChanged)
		{
			pieceRoadChanged = true;
			rotatedOnSpecialPiece = false;

			if (indChildNextPiece < thePieceRoadWhereIam.children.Count)
				thePieceRoadWhereIam = thePieceRoadWhereIam.children[indChildNextPiece];

			Debug.Log("TG: " + thePieceRoadWhereIam.piece.transform.position + " type: " + thePieceRoadWhereIam.type);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		pieceRoadChanged = false;
	}

	private void GetInput()
	{
		verticalInput = Input.GetAxis(inputSettings.VERTICAL_AXIS);
		horizontalInput = Input.GetAxis(inputSettings.HORIZONTAL_AXIS);

		depthInput = Input.GetAxis(inputSettings.DEPTH_AXIS);

		turnInput = Input.GetAxis(inputSettings.TURN_AXIS);
	}

	private void Update()
	{
		GetInput();
	}

	private void FixedUpdate()
	{
		MoveForward();
		Turn();

		rBody.velocity = transform.TransformDirection(velocity);
	}

	private void MoveForward()
	{
		// move
		velocity.z = moveSettings.forwardVel * Time.deltaTime;
		//velocity.z = moveSettings.forwardVel * depthInput * Time.deltaTime;

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

		ManagePlayerRotation();
		transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, playerRotation, 0), moveSettings.turnSpeed * Time.deltaTime);
	}

	private void ManagePlayerRotation()
	{
		if (thePieceRoadWhereIam.type == RoadGeneration.PieceType.SIMPLE)
		{
			indChildNextPiece = 0;
			return;
		}

		if (rotatedOnSpecialPiece)
			return;

		//rotatedOnSpecialPiece = true;

		float turningPoint;

		switch (thePieceRoadWhereIam.dir)
		{
			case Direction.FORWARD:
				turningPoint = thePieceRoadWhereIam.piece.transform.position.z;
				turningPoint += RoadPositions.forwardTranslate[(int)thePieceRoadWhereIam.plane][(int)thePieceRoadWhereIam.dir].z / 2;
				turningPoint -= RoadPositions.WIDTH_PIECE;
				if (this.transform.position.z >= turningPoint)
				{
					rotatedOnSpecialPiece = true;
				}
				break;

			case Direction.RIGHT:
				turningPoint = thePieceRoadWhereIam.piece.transform.position.x;
				turningPoint += RoadPositions.forwardTranslate[(int)thePieceRoadWhereIam.plane][(int)thePieceRoadWhereIam.dir].x / 2;
				turningPoint -= RoadPositions.WIDTH_PIECE;
				if (this.transform.position.x >= turningPoint)
				{
					rotatedOnSpecialPiece = true;
				}
				break;

			case Direction.BACKWARD:
				turningPoint = thePieceRoadWhereIam.piece.transform.position.z;
				turningPoint += RoadPositions.forwardTranslate[(int)thePieceRoadWhereIam.plane][(int)thePieceRoadWhereIam.dir].z / 2;
				turningPoint += RoadPositions.WIDTH_PIECE;
				if (this.transform.position.z <= turningPoint)
				{
					rotatedOnSpecialPiece = true;
				}
				break;

			case Direction.LEFT:
				turningPoint = thePieceRoadWhereIam.piece.transform.position.x;
				turningPoint += RoadPositions.forwardTranslate[(int)thePieceRoadWhereIam.plane][(int)thePieceRoadWhereIam.dir].x / 2;
				turningPoint += RoadPositions.WIDTH_PIECE;
				if (this.transform.position.x <= turningPoint)
				{
					rotatedOnSpecialPiece = true;
				}
				break;
		}


		float angle = 0;

		if (rotatedOnSpecialPiece)
		{
			switch (thePieceRoadWhereIam.type)
			{
				case RoadGeneration.PieceType.LEFT:
					indChildNextPiece = 0;
					angle = -90.0f;
					break;

				case RoadGeneration.PieceType.RIGHT:
					indChildNextPiece = 0;
					angle = 90.0f;
					break;

				case RoadGeneration.PieceType.LEFT_AND_RIGHT:
					System.Random rnd = new System.Random();
					indChildNextPiece = rnd.Next(2);
					if (indChildNextPiece == 0)
						angle = -90.0f;
					else
						angle = 90.0f;

					break;
			}
		}

		playerRotation += angle;
	}
}
