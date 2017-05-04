using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameCameraSmoothController : MonoBehaviour
{
	public Transform target;

	[System.Serializable]
	public class PositionSettings
	{
		public Vector3 targetPosOffset = new Vector3(0, 1, -1.7f);
		public float distanceDamp = 1f;
		public float collisionOffsetPoint = 0.1f;
	}

	public PositionSettings _posSettings = new PositionSettings();

	Transform _transform, _lastTransform;
	Vector3 _velocity;
	RaycastHit _hit;

	private void Awake()
	{
		_transform = _lastTransform = transform;
		_velocity = Vector3.one;
	}

	private void FixedUpdate()
	{
		if (target)
		{
			Vector3 toPos = target.position + (target.rotation * _posSettings.targetPosOffset);
			if (Physics.Linecast(_transform.position, toPos, out _hit))
			{
				toPos = _posSettings.targetPosOffset;
				toPos.y = -_posSettings.collisionOffsetPoint;

				toPos = target.position + (target.rotation * toPos);
			}

			_transform.position = Vector3.SmoothDamp(_transform.position, toPos, ref _velocity, _posSettings.distanceDamp);

			_transform.LookAt(target, target.up);

			_lastTransform = _transform;
		}
		else
		{
			_lastTransform = _transform;
		}

	}
}
