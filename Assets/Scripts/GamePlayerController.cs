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

	Vector3 _playerAngle, _playerDesiredPositionMask;
	float _playerDesiredPosition;

	bool _dead;

	ScoreManager _scoreManager;

	float _forwardSpeed;

	bool _commandReceived;
	Actions _lastActionReceived;
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

		_playerDesiredPositionMask = Vector3.zero;
		_playerDesiredPosition = 0.0f;

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
		_lastActionReceived = Actions.NONE;

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

		//Debug.Log(this.name + " trigger: othName: " + other.name);

		if (!_pieceRoadChanged)
		{
			_pieceRoadChanged = true;

			_canTurn = true;

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

			_actionTaken = Actions.NONE;
			
			//Debug.Log("TG: " + _thePieceRoadWhereIam.piece.transform.position + " type: " + _thePieceRoadWhereIam.type);
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
		_lastActionReceived = action;

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

		if (_lastActionReceived == Actions.MOVE_DOWN || _lastActionReceived == Actions.MOVE_UP || 
			_lastActionReceived == Actions.MOVE_LEFT || _lastActionReceived == Actions.MOVE_RIGHT)
		{
			_verticalInput = 0;
			if (_lastActionReceived == Actions.MOVE_UP)
				_verticalInput = 1;
			else if (_lastActionReceived == Actions.MOVE_DOWN)
				_verticalInput = -1;

			_horizontalInput = 0;
			if (_lastActionReceived == Actions.MOVE_RIGHT)
				_horizontalInput = 1;
			else if (_lastActionReceived == Actions.MOVE_LEFT)
				_horizontalInput = -1;
		}

		if (_tcpServer == null || !_tcpServer.ClientConnected())
		{
			_verticalInput = 0;
			if (Input.GetKey(KeyCode.W))
				_verticalInput = 1;
			else if (Input.GetKey(KeyCode.S))
				_verticalInput = -1;


			_horizontalInput = 0;
			if (Input.GetKey(KeyCode.D))
				_horizontalInput = 1;
			else if (Input.GetKey(KeyCode.A))
				_horizontalInput = -1;
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

		//Debug.Log(_forwardSpeed);

		_scoreManager.AddToScore(pointsPerSecond * Time.deltaTime);



		if (_startTimer)
			_timeCount += Time.deltaTime;

		if (!_turnCommand && _timeCount >= WAIT_TILL_SEND_TIME)
		{
			_startTimer = false;
			_timeCount = 0f;

			_reward = _isInCollision ? MOVE_COLLISION_REWARD : MOVE_REWARD;
			SendRewardToServer();
		}


		//Debug.Log(_dir + " " + _plane + " " + _upsideDown);
		//Debug.Log(_thePieceRoadWhereIam.piece.transform.position + " type: " + _thePieceRoadWhereIam.type);

		//if (_thePieceRoadWhereIam.plane == Assets.Scripts.Plane.YOZ)
		//Debug.Log(_upsideDown + " " + _dir + " " + _plane);
		//else
		//	Debug.Log(_thePieceRoadWhereIam.plane);

		//Debug.Log( _upsideDown + " " + _thePieceRoadWhereIam.upsideDown + " " + _plane + " " + _thePieceRoadWhereIam.plane);

		//Debug.Log(transform.position.x + " " + _thePieceRoadWhereIam.piece.transform.position.x);
	}

	private void FixedUpdate()
	{
		MoveForward();
		//Move();

		_rBody.velocity = transform.TransformDirection(_velocity);
	}

	private void MoveForward()
	{
		// move
		_velocity.z = _forwardSpeed * Time.deltaTime;

		_velocity.y = moveSettings.verticalVel * _verticalInput * Time.deltaTime;

		_velocity.x = moveSettings.horizontalVel * _horizontalInput * Time.deltaTime;
	}

	private void LateUpdate()
	{
		Turn();
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

		Vector3 pos = transform.position;
		if (_playerDesiredPositionMask.x != 0)
		{
			pos.x = _playerDesiredPosition;
			if (Math.Abs(transform.position.x - pos.x) <= 0.01f)
				transform.position = pos;
		}
		else if (_playerDesiredPositionMask.y != 0)
		{
			pos.y = _playerDesiredPosition;
			if (Math.Abs(transform.position.y - pos.y) <= 0.01f)
				transform.position = pos;
		}
		else if (_playerDesiredPositionMask.z != 0)
		{
			pos.z = _playerDesiredPosition;
			if (Math.Abs(transform.position.z - pos.z) <= 0.01f)
				transform.position = pos;
		}


		if (transform.position != pos && _canTurn)
			transform.position = Vector3.Lerp(transform.position, pos, 5 * Time.deltaTime);
		else
			_playerDesiredPositionMask = Vector3.zero;
	}

	private void MoveLeft()
	{
		int sign = 1;
		if (_upsideDown)
			sign = -1;

		Vector3 pos = transform.position;
		Vector3 piecePos = _thePieceRoadWhereIam.piece.transform.position;

		_playerDesiredPositionMask = Vector3.zero;

		switch (_dir)
		{
			case Direction.FORWARD:
				switch (_plane)
				{
					case Assets.Scripts.Plane.ZOX:
						if (Math.Round(pos.x, 2) > Math.Round(piecePos.x, 2))
							pos.x = piecePos.x;
						else
							pos.x = piecePos.x - RoadPositions.WIDTH_PIECE / 3 * sign;

						_playerDesiredPositionMask = new Vector3(1, 0, 0);
						_playerDesiredPosition = pos.x;
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
						if (Math.Round(pos.z, 2) < Math.Round(piecePos.z, 2))
							pos.z = piecePos.z;
						else
							pos.z = piecePos.z + RoadPositions.WIDTH_PIECE / 3 * sign;

						_playerDesiredPositionMask = new Vector3(0, 0, 1);
						_playerDesiredPosition = pos.z;
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
						if (Math.Round(pos.x, 2) < Math.Round(piecePos.x, 2))
							pos.x = piecePos.x;
						else
							pos.x = piecePos.x + RoadPositions.WIDTH_PIECE / 3 * sign;

						_playerDesiredPositionMask = new Vector3(1, 0, 0);
						_playerDesiredPosition = pos.x;
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
						if (Math.Round(pos.z, 2) > Math.Round(piecePos.z, 2))
							pos.z = piecePos.z;
						else
							pos.z = piecePos.z - RoadPositions.WIDTH_PIECE / 3 * sign;

						_playerDesiredPositionMask = new Vector3(0, 0, 1);
						_playerDesiredPosition = pos.z;
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

		//_playerDesiredPosition = pos;
		//transform.position = pos;
	}

	private void MoveRight()
	{
		int sign = 1;
		if (_upsideDown)
			sign = -1;

		Vector3 pos = transform.position;
		Vector3 piecePos = _thePieceRoadWhereIam.piece.transform.position;

		_playerDesiredPositionMask = Vector3.zero;

		switch (_dir)
		{
			case Direction.FORWARD:
				switch (_plane)
				{
					case Assets.Scripts.Plane.ZOX:
						if (Math.Round(pos.x, 2) < Math.Round(piecePos.x, 2))
							pos.x = piecePos.x;
						else
							pos.x = piecePos.x + RoadPositions.WIDTH_PIECE / 3 * sign;

						_playerDesiredPositionMask = new Vector3(1, 0, 0);
						_playerDesiredPosition = pos.x;
						break;
					case Assets.Scripts.Plane.XOY:
						pos.y = piecePos.y - RoadPositions.WIDTH_PIECE / 3 * sign;
						break;
					case Assets.Scripts.Plane.YOZ:
						pos.z = piecePos.z + RoadPositions.WIDTH_PIECE / 3 * sign;
						break;
				}
				break;

			case Direction.RIGHT:
				switch (_plane)
				{
					case Assets.Scripts.Plane.ZOX:
						if (Math.Round(pos.z, 2) > Math.Round(piecePos.z, 2))
							pos.z = piecePos.z;
						else
							pos.z = piecePos.z - RoadPositions.WIDTH_PIECE / 3 * sign;

						_playerDesiredPositionMask = new Vector3(0, 0, 1);
						_playerDesiredPosition = pos.z;
						break;
					case Assets.Scripts.Plane.XOY:
						pos.x = piecePos.x - RoadPositions.WIDTH_PIECE / 3 * sign;
						break;
					case Assets.Scripts.Plane.YOZ:
						pos.y = piecePos.y - RoadPositions.WIDTH_PIECE / 3 * sign;
						break;
				}
				break;

			case Direction.BACKWARD:
				switch (_plane)
				{
					case Assets.Scripts.Plane.ZOX:
						if (Math.Round(pos.x, 2) > Math.Round(piecePos.x, 2))
							pos.x = piecePos.x;
						else
							pos.x = piecePos.x - RoadPositions.WIDTH_PIECE / 3 * sign;

						_playerDesiredPositionMask = new Vector3(1, 0, 0);
						_playerDesiredPosition = pos.x;
						break;
					case Assets.Scripts.Plane.XOY:
						pos.y = piecePos.y + RoadPositions.WIDTH_PIECE / 3 * sign;
						break;
					case Assets.Scripts.Plane.YOZ:
						pos.z = piecePos.z - RoadPositions.WIDTH_PIECE / 3 * sign;
						break;
				}
				break;

			case Direction.LEFT:
				switch (_plane)
				{
					case Assets.Scripts.Plane.ZOX:
						if (Math.Round(pos.z, 2) < Math.Round(piecePos.z, 2))
							pos.z = piecePos.z;
						else
							pos.z = piecePos.z + RoadPositions.WIDTH_PIECE / 3 * sign;

						_playerDesiredPositionMask = new Vector3(0, 0, 1);
						_playerDesiredPosition = pos.z;
						break;
					case Assets.Scripts.Plane.XOY:
						pos.x = piecePos.x + RoadPositions.WIDTH_PIECE / 3 * sign;
						break;
					case Assets.Scripts.Plane.YOZ:
						pos.y = piecePos.y + RoadPositions.WIDTH_PIECE / 3 * sign;
						break;
				}
				break;
		}

		//_playerDesiredPosition = pos;
		//transform.position = pos;
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
		float offset = 1;

		switch (_dir)
		{
			case Direction.FORWARD:
				switch (_plane)
				{
					case Assets.Scripts.Plane.ZOX:
						if (Math.Abs(pos.x - piecePos.x) < offset)
							pos.x = piecePos.x - RoadPositions.WIDTH_PIECE / 3 * sign;
						else
							pos.x = piecePos.x;
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
