using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts;

public class GamePlayerController : MonoBehaviour
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
	bool pieceRoadChanged, rotatedOnSpecialPiece;

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
		pieceRoadChanged = false;
		rotatedOnSpecialPiece = false;
	}

	private void OnTriggerEnter(Collider other)
	{
		//Debug.Log(this.name + " trigger: othName: " + other.name);
		rotatedOnSpecialPiece = false;

		if (!pieceRoadChanged)
		{
			pieceRoadChanged = true;

			if (thePieceRoadWhereIam.children.Count > 0)
				thePieceRoadWhereIam = thePieceRoadWhereIam.children[0];

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

		float rotationAngle = GetAngleRotationForPlayer();
		if ( rotationAngle != 0.0f )
		{
			this.transform.Rotate(Vector3.up, rotationAngle);
		}
	}

	private float GetAngleRotationForPlayer()
	{
		if (thePieceRoadWhereIam.type == RoadGeneration.PieceType.SIMPLE)
		{
			return 0.0f;
		}

		if (rotatedOnSpecialPiece)
			return 0.0f;

		float turningPoint;

		switch (thePieceRoadWhereIam.dir)
		{
			case RoadGeneration.Direction.FORWARD:
				turningPoint = thePieceRoadWhereIam.piece.transform.position.z;
				turningPoint += RoadPositions.forwardTranslateXOZ[(int)thePieceRoadWhereIam.dir].z / 2;
				turningPoint -= RoadPositions.WIDTH_PIECE / 2;
				if (this.transform.position.z >= turningPoint)
				{
					rotatedOnSpecialPiece = true;
				}
				break;

			case RoadGeneration.Direction.RIGHT:
				turningPoint = thePieceRoadWhereIam.piece.transform.position.x;
				turningPoint += RoadPositions.forwardTranslateXOZ[(int)thePieceRoadWhereIam.dir].x / 2;
				turningPoint -= RoadPositions.WIDTH_PIECE / 2;
				if (this.transform.position.x >= turningPoint)
				{
					rotatedOnSpecialPiece = true;
				}
				break;

			case RoadGeneration.Direction.BACKWARD:
				turningPoint = thePieceRoadWhereIam.piece.transform.position.z;
				turningPoint += RoadPositions.forwardTranslateXOZ[(int)thePieceRoadWhereIam.dir].z / 2;
				turningPoint += RoadPositions.WIDTH_PIECE / 2;
				if (this.transform.position.z <= turningPoint)
				{
					rotatedOnSpecialPiece = true;
				}
				break;

			case RoadGeneration.Direction.LEFT:
				turningPoint = thePieceRoadWhereIam.piece.transform.position.x;
				turningPoint += RoadPositions.forwardTranslateXOZ[(int)thePieceRoadWhereIam.dir].x / 2;
				turningPoint += RoadPositions.WIDTH_PIECE / 2;
				if (this.transform.position.x <= turningPoint)
				{
					rotatedOnSpecialPiece = true;
				}
				break;
		}

		if (rotatedOnSpecialPiece)
		{
			switch (thePieceRoadWhereIam.type)
			{
				case RoadGeneration.PieceType.LEFT:
					return -90.0f;
				case RoadGeneration.PieceType.RIGHT:
					return 90.0f;
				case RoadGeneration.PieceType.LEFT_AND_RIGHT:
					return -90.0f;
			}
		}

		return 0.0f;
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
	}
}
