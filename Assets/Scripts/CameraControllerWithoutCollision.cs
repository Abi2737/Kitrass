using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControllerWithoutCollision : MonoBehaviour
{
	public Transform target;

	[System.Serializable]
	public class PositionSettings
	{
		public Vector3 targetPosOffset = new Vector3(0, 0, 0);
		public float lookSmooth = 100f;
		public float distanceFromTarget = -4;
		public float zoomSmooth = 100;
		public float maxZoom = -2;
		public float minZoom = -15;
	}

	[System.Serializable]
	public class OrbitSettings
	{
		public float xRotation = -20;
		public float yRotation = -180;
		public float maxXRotation = 25;
		public float minXRotation = -85;
		public float vOrbitSmooth = 150;
		public float hOrbitSmooth = 150;
	}

	[System.Serializable]
	public class InputSettings
	{
		public string ORBIT_HORIZONTAL_SNAP = "OrbitHorizontalSnap";
		public string ORBIT_HORIZONTAL = "OrbitHorizontal";
		public string ORBIT_VERTICAL = "OrbitVertical";
		public string ZOOM = "Mouse ScrollWheel";
	}

	public PositionSettings positionSettings = new PositionSettings();
	public OrbitSettings orbitSettings = new OrbitSettings();
	public InputSettings inputSettings = new InputSettings();

	Vector3 targetPos = Vector3.zero;
	Vector3 destination = Vector3.zero;
	float vOrbitInput, hOrbitInput, zoomInput, hOrbitSnapInput;

	private void Start()
	{
		SetCameraTarget(target);

		targetPos = target.position + positionSettings.targetPosOffset;
		destination = Quaternion.Euler(orbitSettings.xRotation, orbitSettings.yRotation, 0) * -Vector3.forward * positionSettings.distanceFromTarget;
		destination += targetPos;
		transform.position = destination;
	}

	public void SetCameraTarget(Transform t)
	{
		target = t;

		if (target == null)
		{
			Debug.LogError("Your camera needs a target.");
		}
	}

	private void GetInput()
	{
		vOrbitInput = Input.GetAxisRaw(inputSettings.ORBIT_VERTICAL);
		hOrbitInput = Input.GetAxisRaw(inputSettings.ORBIT_HORIZONTAL);
		hOrbitSnapInput = Input.GetAxisRaw(inputSettings.ORBIT_HORIZONTAL_SNAP);
		zoomInput = Input.GetAxisRaw(inputSettings.ZOOM);
	}

	private void Update()
	{
		GetInput();
		OrbitTarget();
		ZoomInOnTarget();
	}

	private void LateUpdate()
	{
		// moving
		MoveToTarget();

		// rotating
		LookAtTarget();
	}

	private void MoveToTarget()
	{
		targetPos = target.position + positionSettings.targetPosOffset;
		destination = Quaternion.Euler(orbitSettings.xRotation, orbitSettings.yRotation + target.eulerAngles.y, 0) * -Vector3.forward * positionSettings.distanceFromTarget;
		destination += targetPos;
		transform.position = destination;
	}

	private void LookAtTarget()
	{
		Quaternion targetRotation = Quaternion.LookRotation(targetPos - transform.position);
		transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, positionSettings.lookSmooth * Time.deltaTime);
	}

	private void OrbitTarget()
	{
		if ( hOrbitSnapInput > 0 )
		{
			orbitSettings.xRotation = -20;
			orbitSettings.yRotation = -180;
		}

		orbitSettings.xRotation += -vOrbitInput * orbitSettings.vOrbitSmooth * Time.deltaTime;
		orbitSettings.yRotation += -hOrbitInput * orbitSettings.hOrbitSmooth * Time.deltaTime;

		if (orbitSettings.xRotation > orbitSettings.maxXRotation)
		{
			orbitSettings.xRotation = orbitSettings.maxXRotation;
		}
		if (orbitSettings.xRotation < orbitSettings.minXRotation)
		{
			orbitSettings.xRotation = orbitSettings.minXRotation;
		}
	}

	private void ZoomInOnTarget()
	{
		positionSettings.distanceFromTarget += zoomInput * positionSettings.zoomSmooth * Time.deltaTime;

		if (positionSettings.distanceFromTarget > positionSettings.maxZoom)
		{
			positionSettings.distanceFromTarget = positionSettings.maxZoom;
		}
		if (positionSettings.distanceFromTarget < positionSettings.minZoom)
		{
			positionSettings.distanceFromTarget = positionSettings.minZoom;
		}
	}



	//public GameObject target;

	//private Vector3 _positionOffset = Vector3.zero;

	//// Use this for initialization
	//void Start ()
	//{
	//	if ( target != null )
	//		_positionOffset = transform.position - target.transform.position;	
	//}

	//// Update is called once per frame
	//void Update ()
	//{
	//	if ( target != null )
	//	{
	//		//transform.LookAt(target.transform);

	//		transform.position = target.transform.position + _positionOffset;

	//		transform.RotateAround(target.transform.position, Vector3.up, Time.deltaTime * target.transform.eulerAngles.y);
	//	}	
	//}
}
