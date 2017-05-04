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
		public float forwardVel = 700;
		public float verticalVel = 200;
		public float horizontalVel = 200;

		public float turnSpeed = 2;
	}

	[System.Serializable]
	public class InputSettings
	{
		public float inputDelay = 0.1f;
		public string VERTICAL_AXIS = "PlayerVertical";
		public string HORIZONTAL_AXIS = "PlayerHorizontal";
	}

	enum Actions
	{
		NONE = 0,
		MOVE_LEFT = 1,
		MOVE_RIGHT = 2,
		MOVE_UP = 3,
		MOVE_DOWN = 4,
		TURN_LEFT = 5,
		TURN_RIGHT = 6,
		TURN_UP = 7,
		TURN_DOWN = 8
	}

	public MoveSettings moveSettings = new MoveSettings();
	public InputSettings inputSettings = new InputSettings();

	Vector3 _velocity = Vector3.zero;
	Quaternion _targetRotation;
	Rigidbody _rBody;
	float _verticalInput, _horizontalInput;

	bool _turnLeft, _turnRight;
	bool _turnUp, _turnDown;
	bool _canTurn;

	Actions _actionTaken;
	Direction _dir;
	Assets.Scripts.Plane _plane;
	bool _upsideDown;

	RoadGeneration _roadGeneration;
	RoadGeneration.PieceEntry _thePieceRoadWhereIam;
	bool _pieceRoadChanged;
	int _numPiecesChanged;

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

		_roadGeneration = GameObject.Find("RoadGenerationGameObject").GetComponent<RoadGeneration>();

		_thePieceRoadWhereIam = _roadGeneration.GetPlayerStartPiece();
		_thePieceRoadWhereIam.playerWasHere = true;

		_numPiecesChanged = 0;

		_actionTaken = Actions.NONE;

		_canTurn = true;

		_dir = Direction.FORWARD;

		_plane = Assets.Scripts.Plane.ZOX;

		_upsideDown = false;
	}

	private void OnTriggerEnter(Collider other)
	{
		//Debug.Log(this.name + " trigger: othName: " + other.name);

		if (!_pieceRoadChanged)
		{
			_pieceRoadChanged = true;

			_canTurn = true;

			if ( _thePieceRoadWhereIam.IsCrossRoadType() )
			{
				if (_actionTaken == Actions.TURN_LEFT)
					_thePieceRoadWhereIam = _thePieceRoadWhereIam.children[0];
				else
					_thePieceRoadWhereIam = _thePieceRoadWhereIam.children[1];
			}
			else
			{
				_thePieceRoadWhereIam = _thePieceRoadWhereIam.children[0];
			}

			_thePieceRoadWhereIam.playerWasHere = true;

			_numPiecesChanged++;
			if (_numPiecesChanged == _roadGeneration.genAlgoSettings.numChromosoms * _roadGeneration.genAlgoSettings.numPiecesOnChromosome)
			{
				_roadGeneration.AddPiecesToRoad();
				_roadGeneration.RemovePiecesFromRoad();

				_numPiecesChanged = 0;
			}

			_actionTaken = Actions.NONE;
			
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

		if (_canTurn)
		{
			_turnLeft = Input.GetKeyDown(KeyCode.LeftArrow);
			_turnRight = Input.GetKeyDown(KeyCode.RightArrow);
			_turnUp = Input.GetKeyDown(KeyCode.UpArrow);
			_turnDown = Input.GetKeyDown(KeyCode.DownArrow);
		}
	}

	private void Update()
	{
		GetInput();

		//Debug.Log(_dir + " " + _plane + " " + _upsideDown);
		//Debug.Log(_thePieceRoadWhereIam.piece.transform.position + " type: " + _thePieceRoadWhereIam.type);
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
		bool changed = false;
		if (_turnLeft)
		{
			_actionTaken = Actions.TURN_LEFT;
			CalculateDirAndPlane();
			changed = true;
		}
		else if (_turnRight)
		{
			_actionTaken = Actions.TURN_RIGHT;		
			CalculateDirAndPlane();
			changed = true;
		}
		else if (_turnDown)
		{
			_actionTaken = Actions.TURN_DOWN;
			CalculateDirAndPlane();
			changed = true;
		}
		else if (_turnUp)
		{
			_actionTaken = Actions.TURN_UP;
			CalculateDirAndPlane();
			changed = true;
		}

		if (changed)
		{
			Vector3 rotation = RoadPositions.rotation[(int)_plane];
			Vector3 initRotation = RoadPositions.initialRotation[(int)_plane];
			if (_upsideDown)
				initRotation = RoadPositions.upsideDownInitialRotation[(int)_plane];

			_playerAngle = initRotation + rotation * (int)_dir;

			_canTurn = _turnLeft = _turnRight = _turnUp = _turnDown = false;
		}

		transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(_playerAngle), moveSettings.turnSpeed * Time.deltaTime);
	}




	private void CalculateDirAndPlane()
	{
		switch (_actionTaken)
		{
			case Actions.TURN_LEFT:
				if (_upsideDown)
					CalculateDirAndPlaneForRightPiece();
				else
					CalculateDirAndPlaneForLeftPiece();
				break;

			case Actions.TURN_RIGHT:
				if (_upsideDown)
					CalculateDirAndPlaneForLeftPiece();
				else
					CalculateDirAndPlaneForRightPiece();
				break;

			case Actions.TURN_UP:
				switch (_plane)
				{
					case Assets.Scripts.Plane.ZOX:
						CalculateDirAndPlaneForUpPiecePlaneZOX();
						break;

					case Assets.Scripts.Plane.XOY:
						CalculateDirAndPlaneForUpPiecePlaneXOY();
						break;

					case Assets.Scripts.Plane.YOZ:
						CalculateDirAndPlaneForUpPiecePlaneYOZ();
						break;
				}
				break;

			case Actions.TURN_DOWN:
				switch (_plane)
				{
					case Assets.Scripts.Plane.ZOX:
						CalculateDirAndPlaneForDownPiecePlaneZOX();
						break;

					case Assets.Scripts.Plane.XOY:
						CalculateDirAndPlaneForDownPiecePlaneXOY();
						break;

					case Assets.Scripts.Plane.YOZ:
						CalculateDirAndPlaneForDownPiecePlaneYOZ();
						break;
				}
				break;
		}
	}

	private void CalculateDirAndPlaneForLeftPiece()
	{
		if (_dir == Direction.FORWARD)
			_dir = Direction.LEFT;
		else
			_dir = (Direction)((int)_dir - 1);
	}

	private void CalculateDirAndPlaneForRightPiece()
	{
		if (_dir == Direction.LEFT)
			_dir = Direction.FORWARD;
		else
			_dir = (Direction)((int)_dir + 1);
	}

	private void CalculateDirAndPlaneForUpPiecePlaneZOX()
	{
		// parent.plane == ZOX
		switch (_dir)
		{
		case Direction.FORWARD:
			_plane = Assets.Scripts.Plane.XOY;
			if (_upsideDown)
				_dir = Direction.RIGHT;
			else
				_dir = Direction.LEFT;
			_upsideDown = false;
			break;

		case Direction.BACKWARD:
			_plane = Assets.Scripts.Plane.XOY;
			if (_upsideDown)
				_dir = Direction.RIGHT;
			else
				_dir = Direction.LEFT;
			_upsideDown = true;
			break;

		case Direction.RIGHT:
			_plane = Assets.Scripts.Plane.YOZ;
			if (_upsideDown)
				_dir = Direction.BACKWARD;
			else
				_dir = Direction.FORWARD;
			_upsideDown = true;
			break;

		case Direction.LEFT:
			_plane = Assets.Scripts.Plane.YOZ;
			if (_upsideDown)
				_dir = Direction.BACKWARD;
			else
				_dir = Direction.FORWARD;
			_upsideDown = false;
			break;
		}
	}

	private void CalculateDirAndPlaneForUpPiecePlaneXOY()
	{
		// parent.plane == XOY
		switch (_dir)
		{
		case Direction.FORWARD:
			_plane = Assets.Scripts.Plane.YOZ;
			if (_upsideDown)
				_dir = Direction.RIGHT;
			else
				_dir = Direction.LEFT;
			_upsideDown = true;
			break;

		case Direction.BACKWARD:
			_plane = Assets.Scripts.Plane.YOZ;
			if (_upsideDown)
				_dir = Direction.RIGHT;
			else
				_dir = Direction.LEFT;
			_upsideDown = false;
			break;

		case Direction.RIGHT:
			_plane = Assets.Scripts.Plane.ZOX;
			if (_upsideDown)
				_dir = Direction.FORWARD;
			else
				_dir = Direction.BACKWARD;
			_upsideDown = false;
			break;

		case Direction.LEFT:
			_plane = Assets.Scripts.Plane.ZOX;
			if (_upsideDown)
				_dir = Direction.FORWARD;
			else
				_dir = Direction.BACKWARD;
			_upsideDown = true;
			break;
		}
	}

	private void CalculateDirAndPlaneForUpPiecePlaneYOZ()
	{
		// parent.plane == YOZ
		switch (_dir)
		{
		case Direction.FORWARD:
			_plane = Assets.Scripts.Plane.ZOX;
			if (_upsideDown)
				_dir = Direction.LEFT;
			else
				_dir = Direction.RIGHT;
			_upsideDown = true;
			break;

		case Direction.BACKWARD:
			_plane = Assets.Scripts.Plane.ZOX;
			if (_upsideDown)
				_dir = Direction.LEFT;
			else
				_dir = Direction.RIGHT;
			_upsideDown = false;
			break;

		case Direction.RIGHT:
			_plane = Assets.Scripts.Plane.XOY;
			if (_upsideDown)
				_dir = Direction.BACKWARD;
			else
				_dir = Direction.FORWARD;
			_upsideDown = false;
			break;

		case Direction.LEFT:
			_plane = Assets.Scripts.Plane.XOY;
			if (_upsideDown)
				_dir = Direction.BACKWARD;
			else
				_dir = Direction.FORWARD;
			_upsideDown = true;
			break;
		}
	}

	private void CalculateDirAndPlaneForDownPiecePlaneZOX()
	{
		// parent.plane == ZOX
		switch (_dir)
		{
		case Direction.FORWARD:
			_plane = Assets.Scripts.Plane.XOY;
			if (_upsideDown)
				_dir = Direction.LEFT;
			else
				_dir = Direction.RIGHT;
			_upsideDown = true;
			break;

		case Direction.BACKWARD:
			_plane = Assets.Scripts.Plane.XOY;
			if (_upsideDown)
				_dir = Direction.LEFT;
			else
				_dir = Direction.RIGHT;
			_upsideDown = false;
			break;

		case Direction.RIGHT:
			_plane = Assets.Scripts.Plane.YOZ;
			if (_upsideDown)
				_dir = Direction.FORWARD;
			else
				_dir = Direction.BACKWARD;
			_upsideDown = false;
			break;

		case Direction.LEFT:
			_plane = Assets.Scripts.Plane.YOZ;
			if (_upsideDown)
				_dir = Direction.FORWARD;
			else
				_dir = Direction.BACKWARD;
			_upsideDown = true;
			break;
		}
	}

	private void CalculateDirAndPlaneForDownPiecePlaneXOY()
	{
		// parent.plane == XOY
		switch (_dir)
		{
		case Direction.FORWARD:
			_plane = Assets.Scripts.Plane.YOZ;
			if (_upsideDown)
				_dir = Direction.LEFT;
			else
				_dir = Direction.RIGHT;
			_upsideDown = false;
			break;

		case Direction.BACKWARD:
			_plane = Assets.Scripts.Plane.YOZ;
			if (_upsideDown)
				_dir = Direction.LEFT;
			else
				_dir = Direction.RIGHT;
			_upsideDown = true;
			break;

		case Direction.RIGHT:
			_plane = Assets.Scripts.Plane.ZOX;
			if (_upsideDown)
				_dir = Direction.BACKWARD;
			else
				_dir = Direction.FORWARD;
			_upsideDown = true;
			break;

		case Direction.LEFT:
			_plane = Assets.Scripts.Plane.ZOX;
			if (_upsideDown)
				_dir = Direction.BACKWARD;
			else
				_dir = Direction.FORWARD;
			_upsideDown = false;
			break;
		}
	}

	private void CalculateDirAndPlaneForDownPiecePlaneYOZ()
	{
		// parent.plane == YOZ
		switch (_dir)
		{
			case Direction.FORWARD:
				_plane = Assets.Scripts.Plane.ZOX;
				if (_upsideDown)
					_dir = Direction.RIGHT;
				else
					_dir = Direction.LEFT;
				_upsideDown = false;
				break;

			case Direction.BACKWARD:
				_plane = Assets.Scripts.Plane.ZOX;
				if (_upsideDown)
					_dir = Direction.RIGHT;
				else
					_dir = Direction.LEFT;
				_upsideDown = true;
				break;

			case Direction.RIGHT:
				_plane = Assets.Scripts.Plane.XOY;
				if (_upsideDown)
					_dir = Direction.FORWARD;
				else
					_dir = Direction.BACKWARD;
				_upsideDown = true;
				break;

			case Direction.LEFT:
				_plane = Assets.Scripts.Plane.XOY;
				if (_upsideDown)
					_dir = Direction.FORWARD;
				else
					_dir = Direction.BACKWARD;
				_upsideDown = false;
				break;
		}
	}
}
