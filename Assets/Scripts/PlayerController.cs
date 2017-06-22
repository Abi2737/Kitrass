using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.UI;

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

public class PlayerController : MonoBehaviour
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
	bool _goToDesiredPos;

	int _movePosX, _movePosY;

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
	TcpServer _tcpServer;
	bool _turnCommand;
	float _reward;

	bool _isInCollision, _startTimer;
	float _timeCount;

	const float WAIT_TILL_SEND_TIME = 0.1f;

	const float DEAD_REWARD = -100;
	const float MOVE_REWARD = 0.1f;
	const float TURN_REWARD = 2.0f;
	const float MOVE_COLLISION_REWARD = 0.05f;


	public Text cmdRecvText;

	GameObject _lights;

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

		_movePosX = _movePosY = 0;

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

		_tcpServer = null;
		GameObject tcpGameObject = GameObject.Find("PersistentTCPServerGameObject");
		if (tcpGameObject != null)
		{
			_tcpServer = tcpGameObject.GetComponent<TcpServer>();
		}

		_turnCommand = false;

		_reward = MOVE_REWARD;

		_isInCollision = false;

		_timeCount = 0;
		_startTimer = false;

		_lights = null;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Wall")
		{
			Die();
			return;
		}

		if (other.gameObject.tag == "PickUp")
		{
			_scoreManager.AddToScore(10);
			other.gameObject.SetActive(false);
			return;
		}

		//if (_lights != null)
		//{
		//	_lights.transform.parent = transform;
		//	_lights.transform.position = transform.position;
		//	_lights.transform.rotation = transform.rotation;
		//}

		//Debug.Log(this.name + " trigger: othName: " + other.name);

		if (!_pieceRoadChanged)
		{
			_pieceRoadChanged = true;

			// disable the parent that the player doesn't see that the piece was disable
			_thePieceRoadWhereIam.parent.parent.Disable();
			_thePieceRoadWhereIam.parent.DisablePickups();

			if ( _thePieceRoadWhereIam.IsCrossRoadType() )
			{
				// UP AND DOWN ?!
				if (_actionTaken == Actions.TURN_LEFT || _actionTaken == Actions.TURN_UP)
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

			if (!_canTurn)
				_goToDesiredPos = true;

			_canTurn = true;
			_actionTaken = Actions.NONE;
			
			//Debug.Log("TG: " + _thePieceRoadWhereIam.piece.transform.position + " type: " + _thePieceRoadWhereIam.type);
		}
		else
		{
			// if (_thePieceRoadWhereIam.type != RoadGeneration.PieceType.SIMPLE)
			// {
			// 	if (_tcpServer)
			// 		_tcpServer.Send("?");
			// }
		}
	}

	public RoadGeneration.PieceEntry PlayerPiece()
	{
		return _thePieceRoadWhereIam;
	}

	public void Die()
	{
		_dead = true;

		Transform lights = transform.Find("LightsGameObject");
		lights.parent = null;

		_reward = DEAD_REWARD;
		SendRewardToServer();
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.gameObject.tag == "PickUp")
			return;

		_pieceRoadChanged = false;

		if (_turnCommand && !_dead && _thePieceRoadWhereIam.parent.type != RoadGeneration.PieceType.SIMPLE)
		{
			_turnCommand = false;
			_reward = TURN_REWARD;
			SendRewardToServer();
		}
	}

	private void OnCollisionStay(Collision collision)
	{
		_isInCollision = true;
	}

	private void OnCollisionExit(Collision collision)
	{
		_isInCollision = false;
	}

	private void SendRewardToServer()
	{
		int dead = _dead ? 1 : 0;
		_tcpServer.Send(_reward + ":" + dead + "|");
	}

	public void CommandAction(Actions action)
	{
		if (cmdRecvText.color == Color.red)
			cmdRecvText.color = Color.blue;
		else
			cmdRecvText.color = Color.red;

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

			if (_turnLeft || _turnRight || _turnUp || _turnDown)
			{
				_turnCommand = true;
				_startTimer = false;
				_timeCount = 0;
			}
		}

		if (!_turnCommand)
		{
			_startTimer = true;
			_timeCount = 0.0f;
		}
	}
	
	private void GetInput()
	{
		if (_commandReceived)
			return;

		if (_tcpServer == null || !_tcpServer.ClientConnected())
		{
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

			if (_turnLeft || _turnRight || _turnUp || _turnDown )
			{
				_turnCommand = true;
			}
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

		_scoreManager.AddToScore(pointsPerSecond * Time.deltaTime);

		//if ((_turnLeft || _turnRight || _turnUp || _turnDown))
		//{
		//	_lights = GameObject.Find("LightsGameObject");
		//	_lights.transform.parent = null;
		//	_lights.transform.position = transform.position;
		//	_lights.transform.rotation = transform.rotation;
		//}

		Debug.Log(_movePosX + " " + _movePosY);

		if (_startTimer)
			_timeCount += Time.deltaTime;

		if (!_turnCommand && _timeCount >= WAIT_TILL_SEND_TIME)
		{
			_startTimer = false;
			_timeCount = 0f;

			_reward = _isInCollision ? MOVE_COLLISION_REWARD : MOVE_REWARD;
			SendRewardToServer();
		}
	}

	private float ClosestPoint(float p1, float p2, float p3, float t, out int ind)
	{
		float d1 = Math.Abs(t - p1);
		float d2 = Math.Abs(t - p2);
		float d3 = Math.Abs(t - p3);

		if (d1 < d2)
		{
			if (d1 < d3)
			{
				ind = 0;
				return p1;
			}

			ind = 2;
			return p3;
		}

		if (d2 < d3)
		{
			ind = 1;
			return p2;
		}

		ind = 2;
		return p3;
	}

	private void GoToDesiredPosition()
	{
		Vector3 desiredPos = transform.position;

		int ind;

		switch (_thePieceRoadWhereIam.plane)
		{
			case Assets.Scripts.Plane.ZOX:
				if (_thePieceRoadWhereIam.dir == Direction.FORWARD || _thePieceRoadWhereIam.dir == Direction.BACKWARD)
				{
					desiredPos.x = _thePieceRoadWhereIam.piece.transform.position.x;
					desiredPos.y = _thePieceRoadWhereIam.piece.transform.position.y;

					desiredPos.x = ClosestPoint(desiredPos.x - RoadPositions.WIDTH_PIECE_DIV_3, desiredPos.x, desiredPos.x + RoadPositions.WIDTH_PIECE_DIV_3, transform.position.x, out ind);
					_movePosX = ind - 1;
					if (_thePieceRoadWhereIam.dir == Direction.BACKWARD)
						_movePosX = -_movePosX;

					desiredPos.y = ClosestPoint(desiredPos.y - RoadPositions.HEIGHT_PIECE_DIV_3, desiredPos.y, desiredPos.y + RoadPositions.HEIGHT_PIECE_DIV_3, transform.position.y, out ind);
					_movePosY = ind - 1;
				}
				else
				{
					desiredPos.y = _thePieceRoadWhereIam.piece.transform.position.y;
					desiredPos.z = _thePieceRoadWhereIam.piece.transform.position.z;

					desiredPos.z = ClosestPoint(desiredPos.z + RoadPositions.WIDTH_PIECE_DIV_3, desiredPos.z, desiredPos.z - RoadPositions.WIDTH_PIECE_DIV_3, transform.position.z, out ind);
					_movePosX = ind - 1;
					if (_thePieceRoadWhereIam.dir == Direction.LEFT)
						_movePosX = -_movePosX;

					desiredPos.y = ClosestPoint(desiredPos.y - RoadPositions.HEIGHT_PIECE_DIV_3, desiredPos.y, desiredPos.y + RoadPositions.HEIGHT_PIECE_DIV_3, transform.position.y, out ind);
					_movePosY = ind - 1;
				}
				break;

			case Assets.Scripts.Plane.XOY:
				if (_thePieceRoadWhereIam.dir == Direction.FORWARD || _thePieceRoadWhereIam.dir == Direction.BACKWARD)
				{
					desiredPos.y = _thePieceRoadWhereIam.piece.transform.position.y;
					desiredPos.z = _thePieceRoadWhereIam.piece.transform.position.z;

					desiredPos.y = ClosestPoint(desiredPos.y + RoadPositions.WIDTH_PIECE_DIV_3, desiredPos.y, desiredPos.y - RoadPositions.WIDTH_PIECE_DIV_3, transform.position.y, out ind);
					_movePosX = ind - 1;
					if (_thePieceRoadWhereIam.dir == Direction.BACKWARD)
						_movePosX = -_movePosX;

					desiredPos.z = ClosestPoint(desiredPos.z + RoadPositions.HEIGHT_PIECE_DIV_3, desiredPos.z, desiredPos.z - RoadPositions.HEIGHT_PIECE_DIV_3, transform.position.z, out ind);
					_movePosY = ind - 1;
				}
				else
				{
					desiredPos.x = _thePieceRoadWhereIam.piece.transform.position.x;
					desiredPos.z = _thePieceRoadWhereIam.piece.transform.position.z;

					desiredPos.x = ClosestPoint(desiredPos.x + RoadPositions.WIDTH_PIECE_DIV_3, desiredPos.x, desiredPos.x - RoadPositions.WIDTH_PIECE_DIV_3, transform.position.x, out ind);
					_movePosX = ind - 1;
					if (_thePieceRoadWhereIam.dir == Direction.LEFT)
						_movePosX = -_movePosX;

					desiredPos.z = ClosestPoint(desiredPos.z + RoadPositions.HEIGHT_PIECE_DIV_3, desiredPos.z, desiredPos.z - RoadPositions.HEIGHT_PIECE_DIV_3, transform.position.z, out ind);
					_movePosY = ind - 1;
				}
				break;

			case Assets.Scripts.Plane.YOZ:
				if (_thePieceRoadWhereIam.dir == Direction.FORWARD || _thePieceRoadWhereIam.dir == Direction.BACKWARD)
				{
					desiredPos.x = _thePieceRoadWhereIam.piece.transform.position.x;
					desiredPos.z = _thePieceRoadWhereIam.piece.transform.position.z;

					desiredPos.z = ClosestPoint(desiredPos.z - RoadPositions.WIDTH_PIECE_DIV_3, desiredPos.z, desiredPos.z + RoadPositions.WIDTH_PIECE_DIV_3, transform.position.z, out ind);
					_movePosX = ind - 1;
					if (_thePieceRoadWhereIam.dir == Direction.BACKWARD)
						_movePosX = -_movePosX;

					desiredPos.x = ClosestPoint(desiredPos.x - RoadPositions.HEIGHT_PIECE_DIV_3, desiredPos.x, desiredPos.x + RoadPositions.HEIGHT_PIECE_DIV_3, transform.position.x, out ind);
					_movePosY = ind - 1;
				}
				else
				{
					desiredPos.x = _thePieceRoadWhereIam.piece.transform.position.x;
					desiredPos.y = _thePieceRoadWhereIam.piece.transform.position.y;

					desiredPos.y = ClosestPoint(desiredPos.y + RoadPositions.WIDTH_PIECE_DIV_3, desiredPos.y, desiredPos.y - RoadPositions.WIDTH_PIECE_DIV_3, transform.position.y, out ind);
					_movePosX = ind - 1;
					if (_thePieceRoadWhereIam.dir == Direction.LEFT)
						_movePosX = -_movePosX;

					desiredPos.x = ClosestPoint(desiredPos.x - RoadPositions.HEIGHT_PIECE_DIV_3, desiredPos.x, desiredPos.x + RoadPositions.HEIGHT_PIECE_DIV_3, transform.position.x, out ind);
					_movePosY = ind - 1;
				}
				break;
		}

		if (_thePieceRoadWhereIam.upsideDown)
		{
			_movePosX = -_movePosX;
			_movePosY = -_movePosY;
		}

		transform.position = Vector3.Slerp(transform.position, desiredPos, 2 * Time.deltaTime);
		//transform.position = desiredPos;

		if (transform.position == desiredPos)
		{
			_goToDesiredPos = false;
		}
	}

	private void FixedUpdate()
	{
		if (_goToDesiredPos)
		{
			GoToDesiredPosition();
		}

		MoveForward();
		Move();

		_rBody.velocity = transform.TransformDirection(_velocity);

	}

	private void MoveForward()
	{
		// move
		_velocity.z = _forwardSpeed * Time.deltaTime;

		//_velocity.y = moveSettings.verticalVel * _verticalInput * Time.deltaTime;

		//_velocity.x = moveSettings.horizontalVel * _horizontalInput * Time.deltaTime;
	}

	private void LateUpdate()
	{
		Turn();
	}

	private void Move()
	{
		if ((_moveDown || _moveUp || _moveLeft || _moveRight) == false)
			return;

		int sign = 1;
		if (_upsideDown)
			sign = -1;

		if (_moveLeft)
			_movePosX -= 1 * sign;

		if (_moveRight)
			_movePosX += 1 * sign;

		if (_moveDown)
			_movePosY -= 1 * sign;

		if (_moveUp)
			_movePosY += 1 * sign;

		_movePosX = Math.Max(_movePosX, -1);
		_movePosX = Math.Min(_movePosX, +1);

		_movePosY = Math.Max(_movePosY, -1);
		_movePosY = Math.Min(_movePosY, +1);

		float lengthOffset = RoadPositions.LENGTH_PIECE_DIV_3;
		float heightOffset = RoadPositions.HEIGHT_PIECE_DIV_3;
		float widthOffset = RoadPositions.WIDTH_PIECE_DIV_3;

		float pfx, pfy, pfz;

		Vector3 desiredPos = transform.position;

		switch (_thePieceRoadWhereIam.plane)
		{
		case Assets.Scripts.Plane.ZOX:
			if (_thePieceRoadWhereIam.dir == Direction.FORWARD || _thePieceRoadWhereIam.dir == Direction.BACKWARD)
			{
				pfx = _thePieceRoadWhereIam.piece.transform.position.x + widthOffset * _movePosX;
				pfy = _thePieceRoadWhereIam.piece.transform.position.y + heightOffset * _movePosY;
				//pfz = _thePieceRoadWhereIam.piece.transform.position.z - lengthOffset * sign;

				desiredPos.x = pfx;
				desiredPos.y = pfy;
			}
			else
			{
				//pfx = _thePieceRoadWhereIam.piece.transform.position.x - lengthOffset * sign;
				pfy = _thePieceRoadWhereIam.piece.transform.position.y + heightOffset * _movePosY;
				pfz = _thePieceRoadWhereIam.piece.transform.position.z + widthOffset * _movePosX;

				desiredPos.y = pfy;
				desiredPos.z = pfz;
			}
			break;

		case Assets.Scripts.Plane.XOY:
			if (_thePieceRoadWhereIam.dir == Direction.FORWARD || _thePieceRoadWhereIam.dir == Direction.BACKWARD)
			{
				//pfx = _thePieceRoadWhereIam.piece.transform.position.x - lengthOffset * sign;
				pfy = _thePieceRoadWhereIam.piece.transform.position.y + widthOffset * _movePosX;
				pfz = _thePieceRoadWhereIam.piece.transform.position.z + heightOffset * _movePosY;

				desiredPos.y = pfy;
				desiredPos.z = pfz;
			}
			else
			{
				pfx = _thePieceRoadWhereIam.piece.transform.position.x + widthOffset * _movePosX;
				//pfy = _thePieceRoadWhereIam.piece.transform.position.y - lengthOffset * sign;
				pfz = _thePieceRoadWhereIam.piece.transform.position.z + heightOffset * _movePosY;

				desiredPos.x = pfx;
				desiredPos.z = pfz;
			}
			break;

		case Assets.Scripts.Plane.YOZ:
			if (_thePieceRoadWhereIam.dir == Direction.FORWARD || _thePieceRoadWhereIam.dir == Direction.BACKWARD)
			{
				pfx = _thePieceRoadWhereIam.piece.transform.position.x + heightOffset * _movePosY;
				//pfy = _thePieceRoadWhereIam.piece.transform.position.y - lengthOffset * sign;
				pfz = _thePieceRoadWhereIam.piece.transform.position.z + widthOffset * _movePosX;

				desiredPos.x = pfx;
				desiredPos.z = pfz;
			}
			else
			{
				pfx = _thePieceRoadWhereIam.piece.transform.position.x + lengthOffset * _movePosY;
				pfy = _thePieceRoadWhereIam.piece.transform.position.y + widthOffset * _movePosX;
				//pfz = _thePieceRoadWhereIam.piece.transform.position.z - lengthOffset * sign;

				desiredPos.x = pfx;
				desiredPos.y = pfy;
			}
			break;
		}

		transform.position = desiredPos;
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
