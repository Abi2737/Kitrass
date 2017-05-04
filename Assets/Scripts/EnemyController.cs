using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
	public Transform target;

	[System.Serializable]
	public class MoveSettings
	{
		public float forwardMinVel = 700;
		public float forwardMaxVel = 1200;
		public float horizontalVel = 200;
		public float verticalVel = 200;
		public float increaseForwardVelPerSecond = 2;
		public float decreaseForwardVelPerSecond = 0.5f;
	}

	public float distanceFromTarget = 2f;

	[System.Serializable]
	public class InputSettings
	{
		public string VERTICAL_AXIS = "PlayerVertical";
		public string HORIZONTAL_AXIS = "PlayerHorizontal";
	}

	public MoveSettings moveSettings = new MoveSettings();
	public InputSettings inputSettings = new InputSettings();

	Vector3 _velocity;
	Rigidbody _rBody;
	float _verticalInput, _horizontalInput;

	float _forwardSpeed;

	Transform _transform;

	GamePlayerController _playerController;

	bool _dead;

	private void Start()
	{
		if (GetComponent<Rigidbody>())
			_rBody = GetComponent<Rigidbody>();
		else
			Debug.LogError("No rigidbody!");

		_velocity = Vector3.zero;

		_transform = transform;

		_forwardSpeed = moveSettings.forwardMinVel;

		_playerController = GameObject.Find("PlayerGameObject").GetComponent<GamePlayerController>();

		_dead = false;
	}

	private void GetInput()
	{
		_verticalInput = Input.GetAxis(inputSettings.VERTICAL_AXIS);
		_horizontalInput = Input.GetAxis(inputSettings.HORIZONTAL_AXIS);
	}

	private void Update()
	{
		if (_dead)
		{
			DestroyImmediate(gameObject);
			return;
		}

		GetInput();

		if (_forwardSpeed < moveSettings.forwardMaxVel)
			_forwardSpeed += moveSettings.increaseForwardVelPerSecond * Time.deltaTime;

		if (target)
		{
			if (Vector3.Distance(_transform.position, target.position) < distanceFromTarget)
			{
				_forwardSpeed -= moveSettings.decreaseForwardVelPerSecond * Time.deltaTime;
				Debug.Log(Vector3.Distance(transform.position, target.position) + " " + _forwardSpeed);
			}
		}
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
		_velocity.z = _forwardSpeed * Time.deltaTime;

		if (target)
		{
			_velocity.x = moveSettings.horizontalVel * _horizontalInput * Time.deltaTime;
			_velocity.y = moveSettings.verticalVel * _verticalInput * Time.deltaTime;
		}
	}

	private void Turn()
	{
		if (target)
		{
			_transform.LookAt(target, target.up);
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Player")
		{
			_playerController.Die();
			_dead = true;
		}
		//else if (other.tag == "Wall")
		//{
		//	Debug.Log("asdasdasdas");
		//	_dead = true;
		//}
	}
}
