using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts;

public class PlayerSimpleAIController : MonoBehaviour {

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

	Vector3 _velocity = Vector3.zero;
	Quaternion _targetRotation;
	Rigidbody _rBody;
	float _verticalInput, _horizontalInput, _turnInput;
	float _depthInput;



	RoadGeneration.PieceEntry _thePieceRoadWhereIam;
	int _indChildNextPiece;
	bool _pieceRoadChanged, _rotatedOnSpecialPiece;

	float _playerRotation;
	Vector3 _playerUp;
	Assets.Scripts.Plane _playerPlane;

	private void Start()
	{
		_targetRotation = transform.rotation;

		if (GetComponent<Rigidbody>())
			_rBody = GetComponent<Rigidbody>();
		else
			Debug.LogError("No rigidbody!");

		_verticalInput = _horizontalInput = _turnInput = 0;
		_depthInput = 0;


		_thePieceRoadWhereIam = GameObject.Find("RoadGenerationGameObject").GetComponent<RoadGeneration>().GetPlayerStartPiece();
		_indChildNextPiece = 0;
		_pieceRoadChanged = false;
		_rotatedOnSpecialPiece = false;

		_playerRotation = 0;
		_playerUp = Vector3.up;
		_playerPlane = Assets.Scripts.Plane.ZOX;
	}

	private void OnTriggerEnter(Collider other)
	{
		//Debug.Log(this.name + " trigger: othName: " + other.name);

		if (!_pieceRoadChanged)
		{
			_pieceRoadChanged = true;
			_rotatedOnSpecialPiece = false;

			if (_indChildNextPiece < _thePieceRoadWhereIam.children.Count)
				_thePieceRoadWhereIam = _thePieceRoadWhereIam.children[_indChildNextPiece];

			Debug.Log("TG: " + _thePieceRoadWhereIam.piece.transform.position + " type: " + _thePieceRoadWhereIam.type);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		_pieceRoadChanged = false;
	}

	private void GetInput()
	{
		_verticalInput = Input.GetAxis(inputSettings.VERTICAL_AXIS);
		_horizontalInput = Input.GetAxis(inputSettings.HORIZONTAL_AXIS);

		_depthInput = Input.GetAxis(inputSettings.DEPTH_AXIS);

		_turnInput = Input.GetAxis(inputSettings.TURN_AXIS);
	}

	private void Update()
	{
		GetInput();
	}

	private void FixedUpdate()
	{
		MoveForward();
		Turn();

		_rBody.velocity = transform.TransformDirection(_velocity);
	}

	private void MoveForward()
	{
		// move
		_velocity.z = moveSettings.forwardVel * Time.deltaTime;
		//velocity.z = moveSettings.forwardVel * depthInput * Time.deltaTime;

		_velocity.y = moveSettings.verticalVel * _verticalInput * Time.deltaTime;

		_velocity.x = moveSettings.horizontalVel * _horizontalInput * Time.deltaTime;
	}

	private void Turn()
	{
		if (Mathf.Abs(_turnInput) > inputSettings.inputDelay)
		{
			_targetRotation *= Quaternion.AngleAxis(moveSettings.rotateVel * _turnInput * Time.deltaTime, Vector3.up);
			transform.rotation = _targetRotation;
		}

		ManagePlayerRotation();
		transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(_playerUp * _playerRotation), moveSettings.turnSpeed * Time.deltaTime);
	}

	private void ManagePlayerRotation()
	{
		if (_thePieceRoadWhereIam.type == RoadGeneration.PieceType.SIMPLE)
		{
			_indChildNextPiece = 0;
			return;
		}

		if (_rotatedOnSpecialPiece)
			return;

		//rotatedOnSpecialPiece = true;

		float turningPoint;

		switch (_thePieceRoadWhereIam.dir)
		{
			case Direction.FORWARD:
				turningPoint = _thePieceRoadWhereIam.piece.transform.position.z;
				turningPoint += RoadPositions.forwardTranslate[(int)_thePieceRoadWhereIam.plane][(int)_thePieceRoadWhereIam.dir].z / 2;
				turningPoint -= RoadPositions.WIDTH_PIECE;
				if (this.transform.position.z >= turningPoint)
				{
					_rotatedOnSpecialPiece = true;
				}
				break;

			case Direction.RIGHT:
				turningPoint = _thePieceRoadWhereIam.piece.transform.position.x;
				turningPoint += RoadPositions.forwardTranslate[(int)_thePieceRoadWhereIam.plane][(int)_thePieceRoadWhereIam.dir].x / 2;
				turningPoint -= RoadPositions.WIDTH_PIECE;
				if (this.transform.position.x >= turningPoint)
				{
					_rotatedOnSpecialPiece = true;
				}
				break;

			case Direction.BACKWARD:
				turningPoint = _thePieceRoadWhereIam.piece.transform.position.z;
				turningPoint += RoadPositions.forwardTranslate[(int)_thePieceRoadWhereIam.plane][(int)_thePieceRoadWhereIam.dir].z / 2;
				turningPoint += RoadPositions.WIDTH_PIECE;
				if (this.transform.position.z <= turningPoint)
				{
					_rotatedOnSpecialPiece = true;
				}
				break;

			case Direction.LEFT:
				turningPoint = _thePieceRoadWhereIam.piece.transform.position.x;
				turningPoint += RoadPositions.forwardTranslate[(int)_thePieceRoadWhereIam.plane][(int)_thePieceRoadWhereIam.dir].x / 2;
				turningPoint += RoadPositions.WIDTH_PIECE;
				if (this.transform.position.x <= turningPoint)
				{
					_rotatedOnSpecialPiece = true;
				}
				break;
		}


		float angle = 0;

		if (_rotatedOnSpecialPiece)
		{
			switch (_thePieceRoadWhereIam.type)
			{
				case RoadGeneration.PieceType.LEFT:
					_indChildNextPiece = 0;
					angle = -90.0f;
					break;

				case RoadGeneration.PieceType.RIGHT:
					_indChildNextPiece = 0;
					angle = 90.0f;
					break;

				case RoadGeneration.PieceType.LEFT_AND_RIGHT:
					System.Random rnd = new System.Random();
					_indChildNextPiece = rnd.Next(2);
					if (_indChildNextPiece == 0)
						angle = -90.0f;
					else
						angle = 90.0f;

					break;

				case RoadGeneration.PieceType.UP:
					_indChildNextPiece = 0;
					_playerPlane = Assets.Scripts.Plane.XOY;
					_playerUp = Vector3.back;
					angle = -90;
					break;

				case RoadGeneration.PieceType.DOWN:
					_indChildNextPiece = 0;
					_playerPlane = Assets.Scripts.Plane.XOY;
					_playerUp = Vector3.back;
					angle = 90;
					break;
			}
		}

		_playerRotation += angle;
	}
}
