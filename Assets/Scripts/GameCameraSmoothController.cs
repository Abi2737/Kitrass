using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameCameraSmoothController : MonoBehaviour
{
	public Transform target;

	[System.Serializable]
	public class PositionSettings
	{
		public Vector3 targetPosOffset = new Vector3(0, 1.5f, -1);
		public float distanceDamp = 1f;
	}

	public PositionSettings _posSettings = new PositionSettings();

	Transform _transform;
	Vector3 _velocity;

	private void Awake()
	{
		_transform = transform;
		_velocity = Vector3.one;
	}

	private void FixedUpdate()
	{
		Vector3 toPos = target.position + (target.rotation * _posSettings.targetPosOffset);
		_transform.position = Vector3.SmoothDamp(_transform.position, toPos, ref _velocity, _posSettings.distanceDamp);

		_transform.LookAt(target, target.up);
	}
}
