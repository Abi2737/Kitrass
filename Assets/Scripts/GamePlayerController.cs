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
	}

	public MoveSettings moveSettings = new MoveSettings();
	public InputSettings inputSettings = new InputSettings();

	Vector3 _velocity = Vector3.zero;
	Quaternion _targetRotation;
	Rigidbody _rBody;
	float _verticalInput, _horizontalInput;

	bool _turnLeft, _turnRight;
	bool _turnUp, _turnDown;



	RoadGeneration.PieceEntry _thePieceRoadWhereIam;
	bool _pieceRoadChanged;

	Vector3 _playerAngle;

	private void Start()
	{
		_targetRotation = transform.rotation;

		if (GetComponent<Rigidbody>())
			_rBody = GetComponent<Rigidbody>();
		else
			Debug.LogError("No rigidbody!");

		_verticalInput = _horizontalInput = 0;

		_turnLeft = _turnRight = _turnUp = _turnDown = false;

		_playerAngle = Vector3.zero;

		_thePieceRoadWhereIam = GameObject.Find("RoadGenerationGameObject").GetComponent<RoadGeneration>().GetRoadRoot();
	}

	private void OnTriggerEnter(Collider other)
	{
		//Debug.Log(this.name + " trigger: othName: " + other.name);

		if (!_pieceRoadChanged)
		{
			_pieceRoadChanged = true;

			//_thePieceRoadWhereIam = _thePieceRoadWhereIam.children[_indChildNextPiece];

			//Debug.Log("TG: " + _thePieceRoadWhereIam.piece.transform.position + " type: " + _thePieceRoadWhereIam.type);
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

		_turnLeft = Input.GetKeyDown(KeyCode.LeftArrow);
		_turnRight = Input.GetKeyDown(KeyCode.RightArrow);
		_turnUp = Input.GetKeyDown(KeyCode.UpArrow);
		_turnDown = Input.GetKeyDown(KeyCode.DownArrow);
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

		_velocity.y = moveSettings.verticalVel * _verticalInput * Time.deltaTime;
		
		_velocity.x = moveSettings.horizontalVel * _horizontalInput * Time.deltaTime;
	}

	private void Turn()
	{
		if (_turnLeft)
		{
			_playerAngle.y -= 90f;
			_turnLeft = false;
		}
		else if (_turnRight)
		{
			_playerAngle.y += 90f;
			_turnRight = false;
		}
		else if (_turnDown)
		{
			_playerAngle.x += 90f;
			_turnDown = false;
		}
		else if (_turnUp)
		{
			_playerAngle.x -= 90f;
			_turnUp = false;
		}


		transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(_playerAngle), moveSettings.turnSpeed * Time.deltaTime);
	}
}
