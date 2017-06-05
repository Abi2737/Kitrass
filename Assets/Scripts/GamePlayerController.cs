using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts;
using UnityEngine.SceneManagement;
using System;


public enum Actions
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

public class GamePlayerController : MonoBehaviour
{
	[System.Serializable]
	public class MoveSettings
	{
		public float forwardMinVel = 700;
		public float forwardMaxVel = 1200;
		public float increaseForwardVelPerSecond = 2;
		public float verticalVel = 200;
		public float horizontalVel = 200;
		public float turnSpeed = 7;
	}

	[System.Serializable]
	public class InputSettings
	{
		public float inputDelay = 0.1f;
		public string VERTICAL_AXIS = "PlayerVertical";
		public string HORIZONTAL_AXIS = "PlayerHorizontal";
	}

	public float pointsPerSecond = 10;

	public MoveSettings moveSettings = new MoveSettings();
	public InputSettings inputSettings = new InputSettings();

	Vector3 _velocity;
	Rigidbody _rBody;
	float _verticalInput, _horizontalInput;

	bool _moveLeft, _moveRight;
	bool _moveUp, _moveDown;
	int _canMoveHorizontal;

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

	bool _dead;

	ScoreManager _scoreManager;

	float _forwardSpeed;

	bool _commandReceived;

	private void Start()
	{
		if (GetComponent<Rigidbody>())
			_rBody = GetComponent<Rigidbody>();
		else
			Debug.LogError("No rigidbody!");

		_velocity = Vector3.zero;

		_verticalInput = _horizontalInput = 0;

		_turnLeft = _turnRight = _turnUp = _turnDown = false;
		_moveLeft = _moveRight = _moveUp = _moveDown = false;

		_canMoveHorizontal = 0;

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

		_dead = false;

		_scoreManager = GameObject.Find("ScoreManagerGameObject").GetComponent<ScoreManager>();

		_forwardSpeed = moveSettings.forwardMinVel;

		_commandReceived = false;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Wall")
		{
			Die();
			return;
		}

		//Debug.Log(this.name + " trigger: othName: " + other.name);

		if (!_pieceRoadChanged)
		{
			_pieceRoadChanged = true;

			_canTurn = true;

			// disable the parent that the player doesn't see that the piece was disable
			_thePieceRoadWhereIam.parent.parent.Disable();

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

	public void Die()
	{
		_dead = true;
	}

	private void OnTriggerExit(Collider other)
	{
		_pieceRoadChanged = false;
	}

	public void CommandAction(Actions action)
	{
		_commandReceived = true;

		_verticalInput = 0;
		if (action == Actions.MOVE_UP)
			_verticalInput = 1;
		else if (action == Actions.MOVE_DOWN)
			_verticalInput = -1;

		_horizontalInput = 0;
		if (action == Actions.MOVE_RIGHT)
			_horizontalInput = 1;
		else if (action == Actions.MOVE_LEFT)
			_horizontalInput = -1;

		if (_canTurn)
		{
			_turnLeft = action == Actions.TURN_LEFT;
			_turnRight = action == Actions.TURN_RIGHT;
			_turnUp = action == Actions.TURN_UP;
			_turnDown = action == Actions.TURN_DOWN;
		}
	}
	
	private void GetInput()
	{
		if (_commandReceived)
			return;

		//_verticalInput = Input.GetAxis(inputSettings.VERTICAL_AXIS);
		//_horizontalInput = Input.GetAxis(inputSettings.HORIZONTAL_AXIS);

		//_verticalInput = 0;
		//if (Input.GetKey(KeyCode.W))
		//	_verticalInput = 1;
		//else if (Input.GetKey(KeyCode.S))
		//	_verticalInput = -1;


		//_horizontalInput = 0;
		//if (Input.GetKey(KeyCode.D))
		//	_horizontalInput = 1;
		//else if (Input.GetKey(KeyCode.A))
		//	_horizontalInput = -1;

		if (_canTurn)
		{
			_moveLeft = Input.GetKeyDown(KeyCode.A);
			_moveRight = Input.GetKeyDown(KeyCode.D);
			_moveUp = Input.GetKeyDown(KeyCode.W);
			_moveDown = Input.GetKeyDown(KeyCode.S);
		}

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
		if (_dead)
		{
			DestroyImmediate(gameObject);
			return;
		}

		GetInput();

		if (_forwardSpeed < moveSettings.forwardMaxVel )
			_forwardSpeed += moveSettings.increaseForwardVelPerSecond * Time.deltaTime;

		//Debug.Log(_forwardSpeed);

		_scoreManager.AddToScore(pointsPerSecond * Time.deltaTime);

		//Debug.Log(_dir + " " + _plane + " " + _upsideDown);
		//Debug.Log(_thePieceRoadWhereIam.piece.transform.position + " type: " + _thePieceRoadWhereIam.type);

		//if (_thePieceRoadWhereIam.plane == Assets.Scripts.Plane.YOZ)
			Debug.Log(_upsideDown + " " + _thePieceRoadWhereIam.dir + " " + _thePieceRoadWhereIam.plane + " " + 
				_upsideDown + " " + _dir + " " + _plane);
		//else
		//	Debug.Log(_thePieceRoadWhereIam.plane);

		//Debug.Log( _upsideDown + " " + _thePieceRoadWhereIam.upsideDown + " " + _plane + " " + _thePieceRoadWhereIam.plane);
	}

	private void FixedUpdate()
	{
		MoveForward();
		
		_rBody.velocity = transform.TransformDirection(_velocity);
	}

	private void MoveForward()
	{
		// move
		_velocity.z = _forwardSpeed * Time.deltaTime;

		_velocity.y = moveSettings.verticalVel * _verticalInput * Time.deltaTime;

		//_velocity.x = moveSettings.horizontalVel * _horizontalInput * Time.deltaTime;
	}

	private void LateUpdate()
	{
		Turn();

		Move();
	}

	private void Move()
	{
		if (_moveLeft)
		{
			MoveLeft();
		}
		else if (_moveRight)
		{
			MoveRight();
		}
		else if (_moveUp)
		{
			MoveUp();
		}
		else if (_moveDown)
		{
			MoveDown();
		}
	}

	private void MoveLeft()
	{
		MoveLeftOrRight(true);
	}

	private void MoveRight()
	{
		MoveLeftOrRight(false);
	}

	private void MoveLeftOrRight(bool left)
	{
		int sign = 1;
		if (_upsideDown)
			sign = -1;

		if (!left)
			sign = -sign;

		Vector3 pos = transform.position;
		Vector3 piecePos = _thePieceRoadWhereIam.piece.transform.position;

		switch (_dir)
		{
			case Direction.FORWARD:
				switch (_plane)
				{
					case Assets.Scripts.Plane.ZOX:
						pos.x = piecePos.x - RoadPositions.WIDTH_PIECE / 3 * sign;
						break;
					case Assets.Scripts.Plane.XOY:
						pos.y = piecePos.y + RoadPositions.WIDTH_PIECE / 3 * sign;
						break;
					case Assets.Scripts.Plane.YOZ:
						pos.z = piecePos.z - RoadPositions.WIDTH_PIECE / 3 * sign;
						break;
				}
				break;

			case Direction.RIGHT:
				switch (_plane)
				{
					case Assets.Scripts.Plane.ZOX:
						pos.z = piecePos.z + RoadPositions.WIDTH_PIECE / 3 * sign;
						break;
					case Assets.Scripts.Plane.XOY:
						pos.x = piecePos.x + RoadPositions.WIDTH_PIECE / 3 * sign;
						break;
					case Assets.Scripts.Plane.YOZ:
						pos.y = piecePos.y + RoadPositions.WIDTH_PIECE / 3 * sign;
						break;
				}
				break;

			case Direction.BACKWARD:
				switch (_plane)
				{
					case Assets.Scripts.Plane.ZOX:
						pos.x = piecePos.x + RoadPositions.WIDTH_PIECE / 3 * sign;
						break;
					case Assets.Scripts.Plane.XOY:
						pos.y = piecePos.y - RoadPositions.WIDTH_PIECE / 3 * sign;
						break;
					case Assets.Scripts.Plane.YOZ:
						pos.z = piecePos.z + RoadPositions.WIDTH_PIECE / 3 * sign;
						break;
				}
				break;

			case Direction.LEFT:
				switch (_plane)
				{
					case Assets.Scripts.Plane.ZOX:
						pos.z = piecePos.z - RoadPositions.WIDTH_PIECE / 3 * sign;
						break;
					case Assets.Scripts.Plane.XOY:
						pos.x = piecePos.x - RoadPositions.WIDTH_PIECE / 3 * sign;
						break;
					case Assets.Scripts.Plane.YOZ:
						pos.y = piecePos.y - RoadPositions.WIDTH_PIECE / 3 * sign;
						break;
				}
				break;
		}

		transform.position = pos;
	}

	private void MoveUp()
	{
		MoveDownOrUp(false);
	}

	private void MoveDown()
	{
		MoveDownOrUp(true);
	}

	private void MoveDownOrUp(bool down)
	{
		int sign = 1;
		if (_upsideDown)
			sign = -1;

		if (!down)
			sign = -sign;

		Vector3 pos = transform.position;
		Vector3 piecePos = _thePieceRoadWhereIam.piece.transform.position;

		switch (_dir)
		{
			case Direction.FORWARD:
				switch (_plane)
				{
					case Assets.Scripts.Plane.ZOX:
						pos.y = piecePos.y - RoadPositions.HEIGHT_PIECE / 3 * sign;
						break;
					case Assets.Scripts.Plane.XOY:
						pos.z = piecePos.z + RoadPositions.HEIGHT_PIECE / 3 * sign;
						break;
					case Assets.Scripts.Plane.YOZ:
						pos.x = piecePos.x - RoadPositions.HEIGHT_PIECE / 3 * sign;
						break;
				}
				break;

			case Direction.RIGHT:
				switch (_plane)
				{
					case Assets.Scripts.Plane.ZOX:
						pos.y = piecePos.y - RoadPositions.HEIGHT_PIECE / 3 * sign;
						break;
					case Assets.Scripts.Plane.XOY:
						pos.z = piecePos.z + RoadPositions.HEIGHT_PIECE / 3 * sign;
						break;
					case Assets.Scripts.Plane.YOZ:
						pos.x = piecePos.x - RoadPositions.HEIGHT_PIECE / 3 * sign;
						break;
				}
				break;

			case Direction.BACKWARD:
				switch (_plane)
				{
					case Assets.Scripts.Plane.ZOX:
						pos.y = piecePos.y - RoadPositions.HEIGHT_PIECE / 3 * sign;
						break;
					case Assets.Scripts.Plane.XOY:
						pos.z = piecePos.z + RoadPositions.HEIGHT_PIECE / 3 * sign;
						break;
					case Assets.Scripts.Plane.YOZ:
						pos.x = piecePos.x - RoadPositions.HEIGHT_PIECE / 3 * sign;//
						break;
				}
				break;

			case Direction.LEFT:
				switch (_plane)
				{
					case Assets.Scripts.Plane.ZOX:
						pos.y = piecePos.y - RoadPositions.HEIGHT_PIECE / 3 * sign;
						break;
					case Assets.Scripts.Plane.XOY:
						pos.z = piecePos.z + RoadPositions.HEIGHT_PIECE / 3 * sign;
						break;
					case Assets.Scripts.Plane.YOZ:
						pos.x = piecePos.x - RoadPositions.HEIGHT_PIECE / 3 * sign;
						break;
				}
				break;
		}

		transform.position = pos;
	}

	private void Turn()
	{
		_commandReceived = false;

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
