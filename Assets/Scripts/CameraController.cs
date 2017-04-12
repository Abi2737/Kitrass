using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
	public Transform target;

	[System.Serializable]
	public class PositionSettings
	{
		public Vector3 targetPosOffset = new Vector3(0, 0, 0);
		public float lookSmooth = 100;
		public float distanceFromTarget = -4;
		public float zoomSmooth = 100;
		public float maxZoom = -2;
		public float minZoom = -15;
		public bool smoothFollow = true;
		public float smooth = 0.05f;

		[HideInInspector]
		public float newDistance = -4; // set by zoom input

		[HideInInspector]
		public float adjustmentDistance = -4;
	}

	[System.Serializable]
	public class OrbitSettings
	{
		public float xRotation = 0;
		public float yRotation = 0;
		public float maxXRotation = 25;
		public float minXRotation = -50;
		public float vOrbitSmooth = 100f;
		public float hOrbitSmooth = 100f;
	}

	[System.Serializable]
	public class InputSettings
	{
		public string ORBIT_HORIZONTAL_SNAP = "OrbitHorizontalSnap";
		public string ORBIT_HORIZONTAL = "OrbitHorizontal";
		public string ORBIT_VERTICAL = "OrbitVertical";
		public string ZOOM = "Mouse ScrollWheel";
	}

	[System.Serializable]
	public class DebugSettings
	{
		public bool drawDesiredCollisionLines = true;
		public bool drawAdjustedCollisionLines = true;
	}

	public PositionSettings positionSettings = new PositionSettings();
	public OrbitSettings orbitSettings = new OrbitSettings();
	public InputSettings inputSettings = new InputSettings();
	public DebugSettings debugSettings = new DebugSettings();
	public CollisionHandler collisionHandler = new CollisionHandler();

	Vector3 targetPos = Vector3.zero;
	Vector3 destination = Vector3.zero;
	Vector3 adjustedDestination = Vector3.zero;
	Vector3 camVel = Vector3.zero;
	float vOrbitInput, hOrbitInput, zoomInput, hOrbitSnapInput;

	private void Start()
	{
		SetCameraTarget(target);

		vOrbitInput = hOrbitInput = zoomInput = hOrbitSnapInput = 0;

		MoveToTarget();

		collisionHandler.Initialize(Camera.main);
		collisionHandler.UpdateCameraClipPoints(transform.position, transform.rotation, ref collisionHandler.adjustedCameraClipPoints);
		collisionHandler.UpdateCameraClipPoints(destination, transform.rotation, ref collisionHandler.desiredCameraClipPoints);
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

	private void FixedUpdate()
	{
		// moving
		MoveToTarget();

		// rotating
		LookAtTarget();

		collisionHandler.UpdateCameraClipPoints(transform.position, transform.rotation, ref collisionHandler.adjustedCameraClipPoints);
		collisionHandler.UpdateCameraClipPoints(destination, transform.rotation, ref collisionHandler.desiredCameraClipPoints);

		// draw debug lines
		for (int i = 0; i < 5; i++)
		{
			if (debugSettings.drawDesiredCollisionLines)
			{
				Debug.DrawLine(targetPos, collisionHandler.desiredCameraClipPoints[i], Color.white);
			}

			if (debugSettings.drawAdjustedCollisionLines)
			{
				Debug.DrawLine(targetPos, collisionHandler.adjustedCameraClipPoints[i], Color.green);
			}
		}

		collisionHandler.CheckColliding(targetPos); // using raycast
		positionSettings.adjustmentDistance = collisionHandler.GetAdjustedDistanceWithRayFrom(targetPos);
	}

	private void MoveToTarget()
	{
		targetPos = target.position + positionSettings.targetPosOffset;
		destination = Quaternion.Euler(orbitSettings.xRotation, orbitSettings.yRotation + target.eulerAngles.y, 0) * Vector3.forward * positionSettings.distanceFromTarget;
		destination += targetPos;


		if (collisionHandler.colliding)
		{
			adjustedDestination = Quaternion.Euler(orbitSettings.xRotation, orbitSettings.yRotation + target.eulerAngles.y, 0) * Vector3.forward * -positionSettings.adjustmentDistance;
			adjustedDestination += targetPos;

			if (positionSettings.smoothFollow)
			{
				// use smooth damp function
				transform.position = Vector3.SmoothDamp(transform.position, adjustedDestination, ref camVel, positionSettings.smooth);
			}
			else
			{
				transform.position = adjustedDestination;
			}
		}
		else
		{
			if (positionSettings.smoothFollow)
			{
				// use smooth damp function
				transform.position = Vector3.SmoothDamp(transform.position, destination, ref camVel, positionSettings.smooth);
			}
			else
			{
				transform.position = destination;
			}
		}
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
			orbitSettings.xRotation = 0;
			orbitSettings.yRotation = 0;
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



	[System.Serializable]
	public class CollisionHandler
	{
		public LayerMask collisionLayer;

		[HideInInspector]
		public bool colliding = false;

		[HideInInspector]
		public Vector3[] adjustedCameraClipPoints;

		[HideInInspector]
		public Vector3[] desiredCameraClipPoints;

		Camera camera;

		public void Initialize(Camera cam)
		{
			camera = cam;

			adjustedCameraClipPoints = new Vector3[5];
			desiredCameraClipPoints = new Vector3[5];
		}

		public void UpdateCameraClipPoints(Vector3 cameraPosition, Quaternion atRotation, ref Vector3[] intoArary)
		{
			if (!camera)
				return;

			intoArary = new Vector3[5];

			float z = camera.nearClipPlane;
			float x = Mathf.Tan(camera.fieldOfView / 3.41f) * z;
			float y = x / camera.aspect;

			// top left
			intoArary[0] = (atRotation * new Vector3(-x, y, z)) + cameraPosition;
			// top right
			intoArary[1] = (atRotation * new Vector3(x, y, z)) + cameraPosition; 
			// bottom left
			intoArary[2] = (atRotation * new Vector3(-x, -y, z)) + cameraPosition;
			// bottom right
			intoArary[3] = (atRotation * new Vector3(x, -y, z)) + cameraPosition;
			// camera's position
			intoArary[4] = cameraPosition - camera.transform.forward;
		}

		private bool CollisionDetectedAtClipPoints(Vector3[] clipPoints, Vector3 fromPosition)
		{
			for ( int i = 0; i < clipPoints.Length; i++ )
			{
				Ray ray = new Ray(fromPosition, clipPoints[i] - fromPosition);
				float distance = Vector3.Distance(clipPoints[i], fromPosition);

				if (Physics.Raycast(ray, distance, collisionLayer))
				{
					return true;
				}
			}

			return false;
		}

		public float GetAdjustedDistanceWithRayFrom(Vector3 from)
		{
			float distance = -1;

			for ( int i = 0; i < desiredCameraClipPoints.Length; i++ )
			{
				Ray ray = new Ray(from, desiredCameraClipPoints[i] - from);
				RaycastHit hit;

				if (Physics.Raycast(ray, out hit))
				{
					if (distance == -1)
					{
						distance = hit.distance;
					}
					else
					{
						if (distance > hit.distance)
						{
							distance = hit.distance;
						}
					}
				}
			}

			if (distance == -1)
			{
				return 0;
			}
			else
			{
				return distance; 
			}
		}

		public void CheckColliding(Vector3 targetPosition)
		{
			if ( CollisionDetectedAtClipPoints(desiredCameraClipPoints, targetPosition))
			{
				colliding = true;
			}
			else
			{
				colliding = false;
			}
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
