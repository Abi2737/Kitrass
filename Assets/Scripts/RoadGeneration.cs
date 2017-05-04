using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts;

public class RoadGeneration : MonoBehaviour
{
	[System.Serializable]
	public class GeneticAlgoSettings
	{
		public int numChromosoms = 10;
		public int numPiecesOnChromosome = 5;
		public double elitismPercentage = 0.1;
		public double crossoverPercentage = 0.5;
		public double mutationPercentage = 0.2;
		public double mutationChangePercentage = 0.3;
	}

	public GameObject simplePiece;
	public GameObject rightPiece;
	public GameObject leftPiece;
	public GameObject leftAndRightPiece;
	public GameObject upPiece;
	public GameObject downPiece;
	public GameObject upAndDownPiece;

	public float crossroadPercentage = 0.1f;

	public enum PieceType
	{
		SIMPLE = 0,
		LEFT = 1,
		RIGHT = 2,
		LEFT_AND_RIGHT = 3,
		UP = 4,
		DOWN = 5,
		UP_AND_DOWN = 6,

		COUNT = 7,
		NONE = 100
	}

	public class PieceEntry
	{
		public GameObject piece;
		public PieceType type;
		public Direction dir;
		public Assets.Scripts.Plane plane;
		public bool upsideDown;
		public PieceEntry parent;
		public List<PieceEntry> children;
		public Vector3 gridPos;

		public bool playerWasHere;

		public PieceEntry()
		{
			piece = null;
			type = PieceType.SIMPLE;
			dir = Direction.FORWARD;
			plane = Assets.Scripts.Plane.ZOX;
			upsideDown = false; 
			parent = null;
			children = new List<PieceEntry>();
			gridPos = Vector3.zero;
			playerWasHere = false;
		}

		public bool IsCrossRoadType()
		{
			switch (type)
			{
				case PieceType.LEFT_AND_RIGHT:
				case PieceType.UP_AND_DOWN:
					return true;
			}

			return false;
		}

		public void Delete(bool deleteChildren = false)
		{
			if (!deleteChildren)
			{
				foreach (var child in children)
				{
					child.parent = null;
				}
			}
			else
			{
				foreach (var child in children)
				{
					child.Delete(true);
				}
			}

			Destroy(piece);
		}
	}

	public GeneticAlgoSettings genAlgoSettings = new GeneticAlgoSettings();

	PieceEntry _root;
	List<PieceEntry> _leafs;
	HashSet<Vector3> _takenPos;

	PieceEntry _playerStartPiece;

	GeneticAlgorithm _genAlgo;

	System.Random _rnd;

	private void Awake()
	{
		_rnd = new System.Random();

		_genAlgo = new GeneticAlgorithm(
			genAlgoSettings.numChromosoms,
			genAlgoSettings.numPiecesOnChromosome,
			genAlgoSettings.elitismPercentage,
			genAlgoSettings.crossoverPercentage,
			genAlgoSettings.mutationPercentage,
			genAlgoSettings.mutationChangePercentage);



		_root = new PieceEntry();
		_root.piece = Instantiate(simplePiece);
		_root.type = PieceType.SIMPLE;
		_root.piece.transform.position = Vector3.zero;
		_root.gridPos = Vector3.zero;

		_root.piece.transform.eulerAngles = RoadPositions.initialRotation[(int)_root.plane];

		_leafs = new List<PieceEntry>();
		_leafs.Add(_root);

		_takenPos = new HashSet<Vector3>();
		_takenPos.Add(_root.gridPos);


		

		

		//DebugCreateInitialRoad();
		//DebugCreateInitialRoad2();

		//DebugCreateInitialStraightRoad(30);

		int numPieces = genAlgoSettings.numChromosoms * genAlgoSettings.numPiecesOnChromosome;
		Vector3 pos = Vector3.back * numPieces * RoadPositions.LENGTH_PIECE;

		_root.piece.transform.position = pos;

		for (int i = 0; i < numPieces; i++)
		{
			_leafs[_leafs.Count - 1].playerWasHere = true;
			AddPieceToRoad(PieceType.SIMPLE);
		}

		_playerStartPiece = _leafs[_leafs.Count - 1];

		AddPiecesToRoad();
		AddPiecesToRoad();
	}

	// Use this for initialization
	void Start ()
	{
		PrintRoad();
	}

	// Update is called once per frame
	void Update ()
	{
		if (Input.GetKeyDown(KeyCode.Space))
		{
			AddPiecesToRoad();
		}

		if (Input.GetKeyDown(KeyCode.H))
		{
			RemovePiecesFromRoad();
		}

		//DebugCreateRuntimeRoad();
	}

	public PieceEntry GetPlayerStartPiece()
	{
		return _playerStartPiece;
	}

	private void PrintRoad()
	{
		string result = "";
		var crtPE = _root;
		while (true)
		{
			result += crtPE.type + " ";
			if ( crtPE.children.Count == 0 )
			{
				break;
			}

			crtPE = crtPE.children[0];
		}

		Debug.Log(result);
	}

	public GameObject InstantiatePiece(PieceType type)
	{
		switch (type)
		{
			case PieceType.SIMPLE:
				return Instantiate(simplePiece);

			case PieceType.LEFT:
				return Instantiate(leftPiece);

			case PieceType.RIGHT:
				return Instantiate(rightPiece);

			case PieceType.LEFT_AND_RIGHT:
				return Instantiate(leftAndRightPiece);

			case PieceType.UP:
				return Instantiate(upPiece);

			case PieceType.DOWN:
				return Instantiate(downPiece);

			case PieceType.UP_AND_DOWN:
				return Instantiate(upAndDownPiece);
		}

		return Instantiate(simplePiece);
	}

	private void CalculateChildDirAndPlaneForParentRightPiece(PieceEntry parent, out Direction dir, 
		out Assets.Scripts.Plane plane, out bool upsideDown)
	{
		if (parent.dir == Direction.LEFT)
			dir = Direction.FORWARD;
		else
			dir = (Direction)((int)parent.dir + 1);

		plane = parent.plane;
		upsideDown = parent.upsideDown;
	}

	private void CalculateChildDirAndPlaneForParentLeftPiece(PieceEntry parent, out Direction dir,
		out Assets.Scripts.Plane plane, out bool upsideDown)
	{
		if (parent.dir == Direction.FORWARD)
			dir = Direction.LEFT;
		else 
			dir = (Direction)((int)parent.dir - 1);

		plane = parent.plane;
		upsideDown = parent.upsideDown;
	}
	
	private void CalculateChildDirAndPlaneForParentUpPiecePlaneZOX(PieceEntry parent, out Direction dir,
		out Assets.Scripts.Plane plane, out bool upsideDown)
	{
		dir = parent.dir;
		plane = parent.plane;
		upsideDown = parent.upsideDown;

		// parent.plane == ZOX
		switch (parent.dir)
		{
		case Direction.FORWARD:
			plane = Assets.Scripts.Plane.XOY;
			if (upsideDown)
				dir = Direction.RIGHT;
			else
				dir = Direction.LEFT;
			upsideDown = false;
			break;

		case Direction.BACKWARD:
			plane = Assets.Scripts.Plane.XOY;
			if (upsideDown)
				dir = Direction.RIGHT;
			else
				dir = Direction.LEFT;
			upsideDown = true;
			break;

		case Direction.RIGHT:
			plane = Assets.Scripts.Plane.YOZ;
			if (upsideDown)
				dir = Direction.BACKWARD;
			else
				dir = Direction.FORWARD;
			upsideDown = true;
			break;

		case Direction.LEFT:
			plane = Assets.Scripts.Plane.YOZ;
			if (upsideDown)
				dir = Direction.BACKWARD;
			else
				dir = Direction.FORWARD;
			upsideDown = false;
			break;
		}
	}

	private void CalculateChildDirAndPlaneForParentUpPiecePlaneXOY(PieceEntry parent, out Direction dir,
		out Assets.Scripts.Plane plane, out bool upsideDown)
	{
		dir = parent.dir;
		plane = parent.plane;
		upsideDown = parent.upsideDown;

		// parent.plane == XOY
		switch (parent.dir)
		{
		case Direction.FORWARD:
			plane = Assets.Scripts.Plane.YOZ;
			if (upsideDown)
				dir = Direction.RIGHT;
			else
				dir = Direction.LEFT;
			upsideDown = true;
			break;

		case Direction.BACKWARD:
			plane = Assets.Scripts.Plane.YOZ;
			if (upsideDown)
				dir = Direction.RIGHT;
			else
				dir = Direction.LEFT;
			upsideDown = false;
			break;

		case Direction.RIGHT:
			plane = Assets.Scripts.Plane.ZOX;
			if (upsideDown)
				dir = Direction.FORWARD;
			else
				dir = Direction.BACKWARD;
			upsideDown = false;
			break;

		case Direction.LEFT:
			plane = Assets.Scripts.Plane.ZOX;
			if (upsideDown)
				dir = Direction.FORWARD;
			else
				dir = Direction.BACKWARD;
			upsideDown = true;
			break;
		}
	}

	private void CalculateChildDirAndPlaneForParentUpPiecePlaneYOZ(PieceEntry parent, out Direction dir,
		out Assets.Scripts.Plane plane, out bool upsideDown)
	{
		dir = parent.dir;
		plane = parent.plane;
		upsideDown = parent.upsideDown;

		// parent.plane == YOZ
		switch (parent.dir)
		{
		case Direction.FORWARD:
			plane = Assets.Scripts.Plane.ZOX;
			if (upsideDown)
				dir = Direction.LEFT;
			else
				dir = Direction.RIGHT;
			upsideDown = true;
			break;

		case Direction.BACKWARD:
			plane = Assets.Scripts.Plane.ZOX;
			if (upsideDown)
				dir = Direction.LEFT;
			else
				dir = Direction.RIGHT;
			upsideDown = false;
			break;

		case Direction.RIGHT:
			plane = Assets.Scripts.Plane.XOY;
			if (upsideDown)
				dir = Direction.BACKWARD;
			else
				dir = Direction.FORWARD;
			upsideDown = false;
			break;

		case Direction.LEFT:
			plane = Assets.Scripts.Plane.XOY;
			if (upsideDown)
				dir = Direction.BACKWARD;
			else
				dir = Direction.FORWARD;
			upsideDown = true;
			break;
		}
	}

	private void CalculateChildDirAndPlaneForParentDownPiecePlaneZOX(PieceEntry parent, out Direction dir,
		out Assets.Scripts.Plane plane, out bool upsideDown)
	{
		dir = parent.dir;
		plane = parent.plane;
		upsideDown = parent.upsideDown;

		// parent.plane == ZOX
		switch (parent.dir)
		{
			case Direction.FORWARD:
				plane = Assets.Scripts.Plane.XOY;
				if (upsideDown)
					dir = Direction.LEFT;
				else
					dir = Direction.RIGHT;
				upsideDown = true;
				break;

			case Direction.BACKWARD:
				plane = Assets.Scripts.Plane.XOY;
				if (upsideDown)
					dir = Direction.LEFT;
				else
					dir = Direction.RIGHT;
				upsideDown = false;
				break;

			case Direction.RIGHT:
				plane = Assets.Scripts.Plane.YOZ;
				if (upsideDown)
					dir = Direction.FORWARD;
				else
					dir = Direction.BACKWARD;
				upsideDown = false;
				break;

			case Direction.LEFT:
				plane = Assets.Scripts.Plane.YOZ;
				if (upsideDown)
					dir = Direction.FORWARD;
				else
					dir = Direction.BACKWARD;
				upsideDown = true;
				break;
		}
	}

	private void CalculateChildDirAndPlaneForParentDownPiecePlaneXOY(PieceEntry parent, out Direction dir,
		out Assets.Scripts.Plane plane, out bool upsideDown)
	{
		dir = parent.dir;
		plane = parent.plane;
		upsideDown = parent.upsideDown;

		// parent.plane == XOY
		switch (parent.dir)
		{
			case Direction.FORWARD:
				plane = Assets.Scripts.Plane.YOZ;
				if (upsideDown)
					dir = Direction.LEFT;
				else
					dir = Direction.RIGHT;
				upsideDown = false;
				break;

			case Direction.BACKWARD:
				plane = Assets.Scripts.Plane.YOZ;
				if (upsideDown)
					dir = Direction.LEFT;
				else
					dir = Direction.RIGHT;
				upsideDown = true;
				break;

			case Direction.RIGHT:
				plane = Assets.Scripts.Plane.ZOX;
				if (upsideDown)
					dir = Direction.BACKWARD;
				else
					dir = Direction.FORWARD;
				upsideDown = true;
				break;

			case Direction.LEFT:
				plane = Assets.Scripts.Plane.ZOX;
				if (upsideDown)
					dir = Direction.BACKWARD;
				else
					dir = Direction.FORWARD;
				upsideDown = false;
				break;
		}
	}

	private void CalculateChildDirAndPlaneForParentDownPiecePlaneYOZ(PieceEntry parent, out Direction dir,
		out Assets.Scripts.Plane plane, out bool upsideDown)
	{
		dir = parent.dir;
		plane = parent.plane;
		upsideDown = parent.upsideDown;

		// parent.plane == YOZ
		switch (parent.dir)
		{
		case Direction.FORWARD:
			plane = Assets.Scripts.Plane.ZOX;
			if (upsideDown)
				dir = Direction.RIGHT;
			else
				dir = Direction.LEFT;
			upsideDown = false;
			break;

		case Direction.BACKWARD:
			plane = Assets.Scripts.Plane.ZOX;
			if (upsideDown)
				dir = Direction.RIGHT;
			else
				dir = Direction.LEFT;
			upsideDown = true;
			break;

		case Direction.RIGHT:
			plane = Assets.Scripts.Plane.XOY;
			if (upsideDown)
				dir = Direction.FORWARD;
			else
				dir = Direction.BACKWARD;
			upsideDown = true;
			break;

		case Direction.LEFT:
			plane = Assets.Scripts.Plane.XOY;
			if (upsideDown)
				dir = Direction.FORWARD;
			else
				dir = Direction.BACKWARD;
			upsideDown = false;
			break;
		}
	}

	private void CalculateChildDirAndPlaneForParentUpPiece(PieceEntry parent, out Direction dir,
		out Assets.Scripts.Plane plane, out bool upsideDown)
	{
		dir = parent.dir;
		plane = parent.plane;
		upsideDown = parent.upsideDown; 

		switch (parent.plane)
		{
			case Assets.Scripts.Plane.ZOX:
				CalculateChildDirAndPlaneForParentUpPiecePlaneZOX(parent, out dir, out plane, out upsideDown);
				break;

			case Assets.Scripts.Plane.XOY:
				CalculateChildDirAndPlaneForParentUpPiecePlaneXOY(parent, out dir, out plane, out upsideDown);
				break;
				
			case Assets.Scripts.Plane.YOZ:
				CalculateChildDirAndPlaneForParentUpPiecePlaneYOZ(parent, out dir, out plane, out upsideDown);
				break;
		}
	}

	private void CalculateChildDirAndPlaneForParentDownPiece(PieceEntry parent, out Direction dir,
		out Assets.Scripts.Plane plane, out bool upsideDown)
	{
		dir = parent.dir;
		plane = parent.plane;
		upsideDown = parent.upsideDown;

		switch (parent.plane)
		{
			case Assets.Scripts.Plane.ZOX:
				CalculateChildDirAndPlaneForParentDownPiecePlaneZOX(parent, out dir, out plane, out upsideDown);
				break;

			case Assets.Scripts.Plane.XOY:
				CalculateChildDirAndPlaneForParentDownPiecePlaneXOY(parent, out dir, out plane, out upsideDown);
				break;

			case Assets.Scripts.Plane.YOZ:
				CalculateChildDirAndPlaneForParentDownPiecePlaneYOZ(parent, out dir, out plane, out upsideDown);
				break;
		}
	}


	private void CalculateChildDirAndPlane(PieceEntry parent, out Direction dir, out Assets.Scripts.Plane plane, 
		out bool upsideDown)
	{
		dir = parent.dir;
		plane = parent.plane;
		upsideDown = parent.upsideDown;

		switch (parent.type)
		{
		case PieceType.LEFT:
			if (parent.upsideDown)
				CalculateChildDirAndPlaneForParentRightPiece(parent, out dir, out plane, out upsideDown);
			else
				CalculateChildDirAndPlaneForParentLeftPiece(parent, out dir, out plane, out upsideDown);
			break;

		case PieceType.RIGHT:
			if (parent.upsideDown)
				CalculateChildDirAndPlaneForParentLeftPiece(parent, out dir, out plane, out upsideDown);
			else
				CalculateChildDirAndPlaneForParentRightPiece(parent, out dir, out plane, out upsideDown);
			break;

		case PieceType.LEFT_AND_RIGHT:
			if (parent.children.Count == 0) // first time add the new piece on the parent's left
				CalculateChildDirAndPlaneForParentLeftPiece(parent, out dir, out plane, out upsideDown);
			else
				CalculateChildDirAndPlaneForParentRightPiece(parent, out dir, out plane, out upsideDown);
			break;

		case PieceType.UP:
			CalculateChildDirAndPlaneForParentUpPiece(parent, out dir, out plane, out upsideDown);
			break;

		case PieceType.DOWN:
			CalculateChildDirAndPlaneForParentDownPiece(parent, out dir, out plane, out upsideDown);
			break;

		case PieceType.UP_AND_DOWN:
			if (parent.children.Count == 0)	// first time add the new piece on the parent's up
			{
				CalculateChildDirAndPlaneForParentUpPiece(parent, out dir, out plane, out upsideDown);
			}
			else
			{
				CalculateChildDirAndPlaneForParentDownPiece(parent, out dir, out plane, out upsideDown);
			}
			break;
		}
	}

	private Vector3 CalculateChildPosition(PieceEntry parent)
	{
		Vector3[] translate = RoadPositions.forwardTranslate[(int)parent.plane];

		switch (parent.type)
		{
		case PieceType.LEFT:
			if (parent.upsideDown)
				translate = RoadPositions.rightTranslate[(int)parent.plane];
			else
				translate = RoadPositions.leftTranslate[(int)parent.plane];
			break;

		case PieceType.RIGHT:
			if (parent.upsideDown)
				translate = RoadPositions.leftTranslate[(int)parent.plane];
			else
				translate = RoadPositions.rightTranslate[(int)parent.plane];
			break;

		case PieceType.LEFT_AND_RIGHT:
			if (parent.children.Count == 0) // first time add the new piece on the parent's left
			{
				translate = RoadPositions.leftTranslate[(int)parent.plane];
			}
			else    // second time add the new piece on the parent's right
			{
				translate = RoadPositions.rightTranslate[(int)parent.plane];
			}
			break;

		case PieceType.UP:
			if (parent.upsideDown)
				translate = RoadPositions.downTranslate[(int)parent.plane];
			else
				translate = RoadPositions.upTranslate[(int)parent.plane];
			break;

		case PieceType.DOWN:
			if (parent.upsideDown)
				translate = RoadPositions.upTranslate[(int)parent.plane];
			else
				translate = RoadPositions.downTranslate[(int)parent.plane];
			break;

		case PieceType.UP_AND_DOWN:
			if (parent.children.Count == 0) // first time add the new piece on the parent's down
			{
				if (parent.upsideDown)
					translate = RoadPositions.downTranslate[(int)parent.plane];
				else
					translate = RoadPositions.upTranslate[(int)parent.plane];
			}
			else    // second time add the new piece on the parent's right
			{
				if (parent.upsideDown)
					translate = RoadPositions.upTranslate[(int)parent.plane];
				else
					translate = RoadPositions.downTranslate[(int)parent.plane];
			}
			break;
		}

		// calculate the newPiece's world position based on the parent's world position
		Vector3 offsetTranslate = Vector3.zero;
		//Vector3 offsetTranslate = RoadPositions.forwardTranslateXOZoffset[(int)dir];

		return parent.piece.transform.position + translate[(int)parent.dir] + offsetTranslate;
	}

	private PieceEntry CreateAndAddPieceToRoad(PieceEntry parent, PieceType type)
	{
		// create the new piece
		PieceEntry newPiece = new PieceEntry();
		// set it with the given type
		newPiece.type = type;

		Direction dir;				// newPiece's direction
		Assets.Scripts.Plane plane; // newPiece's plane
		bool upsideDown;			// newPiece's upsideDown
		
		CalculateChildDirAndPlane(parent, out dir, out plane, out upsideDown);

		// set the direction
		newPiece.dir = dir;

		// set the newPiece's plane
		newPiece.plane = plane;

		// set the newPiece's upsideDown
		newPiece.upsideDown = upsideDown;

		// calculate the newPiece's grid position based on the parent's grid position and the newPiece's direction
		newPiece.gridPos = parent.gridPos + RoadPositions.gridTranslate[(int)plane][(int)dir];

		// instantiate the piece according with the given type
		newPiece.piece = InstantiatePiece(type);

		// calculate the newPiece's world position based on the parent's world position
		newPiece.piece.transform.position = CalculateChildPosition(parent);
		
		Vector3 rotation = RoadPositions.rotation[(int)plane];
		Vector3 initRotation = RoadPositions.initialRotation[(int)plane];
		if (upsideDown)
			initRotation = RoadPositions.upsideDownInitialRotation[(int)plane];

		// set the newPiece's rotation (-90 initial rotation of the prefab)
		newPiece.piece.transform.eulerAngles = initRotation + rotation * (int)dir;


		// add the newPiece in the parent's list of children
		parent.children.Add(newPiece);

		// set the newPiece's parent
		newPiece.parent = parent;


		// mark the grid pos as taken
		_takenPos.Add(newPiece.gridPos);

		return newPiece;
	}



	private bool IsEmptyPos(PieceEntry parent)
	{
		// calculate the newPiece's direction
		Direction dir;
		Assets.Scripts.Plane plane;
		bool upsideDown;
		CalculateChildDirAndPlane(parent, out dir, out plane, out upsideDown);

		// calculate the checking position based on the parent gridPosition and the newPiece's direction
		Vector3 checkPos = parent.gridPos + RoadPositions.gridTranslate[(int)plane][(int)dir];

		return !_takenPos.Contains(checkPos);
	}

	private void DebugCreateRuntimeRoad()
	{
		PieceType crtType = PieceType.NONE;

		if (Input.GetKeyDown(KeyCode.S))
		{
			Debug.Log('S');
			crtType = PieceType.SIMPLE;
		}
		else if (Input.GetKeyDown(KeyCode.R))
		{
			Debug.Log('R');
			crtType = PieceType.RIGHT;
		}
		else if (Input.GetKeyDown(KeyCode.L))
		{
			Debug.Log('L');
			crtType = PieceType.LEFT;
		}
		else if (Input.GetKeyDown(KeyCode.B))
		{
			Debug.Log('B');
			crtType = PieceType.LEFT_AND_RIGHT;
		}
		else if (Input.GetKeyDown(KeyCode.U))
		{
			Debug.Log('U');
			crtType = PieceType.UP;
		}
		else if (Input.GetKeyDown(KeyCode.D))
		{
			Debug.Log("D");
			crtType = PieceType.DOWN;
		}
		else if (Input.GetKeyDown(KeyCode.H))
		{
			Debug.Log("H");
			crtType = PieceType.UP_AND_DOWN;
		}

		if (crtType != PieceType.NONE)
		{
			List<PieceEntry> newLeafs = new List<PieceEntry>();
			foreach (var pe in _leafs)
			{
				if (!IsEmptyPos(pe))
					continue;

				PieceEntry newPiece = CreateAndAddPieceToRoad(pe, crtType);
				_takenPos.Add(newPiece.gridPos);
				newLeafs.Add(newPiece);

				if (pe.type == PieceType.LEFT_AND_RIGHT)
				{
					PieceEntry newPiece2 = CreateAndAddPieceToRoad(pe, crtType);
					_takenPos.Add(newPiece2.gridPos);
					newLeafs.Add(newPiece2);
				}
				else if (pe.type == PieceType.UP_AND_DOWN)
				{
					PieceEntry newPiece2 = CreateAndAddPieceToRoad(pe, crtType);
					_takenPos.Add(newPiece2.gridPos);
					newLeafs.Add(newPiece2);
				}
			}

			_leafs.Clear();
			_leafs = newLeafs;
		}
	}

	private void DebugCreateInitialRoad()
	{
		PieceEntry p1 = CreateAndAddPieceToRoad(_root, PieceType.SIMPLE);
		PieceEntry p2 = CreateAndAddPieceToRoad(p1, PieceType.SIMPLE);
		PieceEntry p3 = CreateAndAddPieceToRoad(p2, PieceType.LEFT_AND_RIGHT);
		PieceEntry p4 = CreateAndAddPieceToRoad(p3, PieceType.RIGHT);
		PieceEntry p5 = CreateAndAddPieceToRoad(p4, PieceType.LEFT);
		PieceEntry p6 = CreateAndAddPieceToRoad(p5, PieceType.LEFT);
		PieceEntry p7 = CreateAndAddPieceToRoad(p6, PieceType.SIMPLE);
		PieceEntry p8 = CreateAndAddPieceToRoad(p7, PieceType.LEFT);
		PieceEntry p9 = CreateAndAddPieceToRoad(p8, PieceType.RIGHT);
		PieceEntry p10 = CreateAndAddPieceToRoad(p9, PieceType.SIMPLE);

		PieceEntry p11 = CreateAndAddPieceToRoad(p3, PieceType.SIMPLE);
		PieceEntry p12 = CreateAndAddPieceToRoad(p11, PieceType.UP_AND_DOWN);
		PieceEntry p13 = CreateAndAddPieceToRoad(p12, PieceType.RIGHT);
		PieceEntry p14 = CreateAndAddPieceToRoad(p13, PieceType.RIGHT);
		PieceEntry p15 = CreateAndAddPieceToRoad(p14, PieceType.SIMPLE);

		PieceEntry p16 = CreateAndAddPieceToRoad(p12, PieceType.RIGHT);
		PieceEntry p17 = CreateAndAddPieceToRoad(p16, PieceType.LEFT);
		PieceEntry p18 = CreateAndAddPieceToRoad(p17, PieceType.SIMPLE);
	}

	private void DebugCreateInitialStraightRoad(int numPieces)
	{
		for ( int i = 0; i < numPieces; i++ )
		{
			AddPieceToRoad(PieceType.SIMPLE);
		}
	}

	private void DebugCreateInitialRoad2()
	{
		for (int i = 0; i < 5; i++)
		{
			AddPieceToRoad(PieceType.SIMPLE);
		}

		AddPieceToRoad(PieceType.UP);
		AddPieceToRoad(PieceType.SIMPLE);
		AddPieceToRoad(PieceType.SIMPLE);
		AddPieceToRoad(PieceType.SIMPLE);
		AddPieceToRoad(PieceType.SIMPLE);
		AddPieceToRoad(PieceType.SIMPLE);
		AddPieceToRoad(PieceType.LEFT_AND_RIGHT);
		AddPieceToRoad(PieceType.SIMPLE);
		AddPieceToRoad(PieceType.SIMPLE);
		AddPieceToRoad(PieceType.SIMPLE);
		AddPieceToRoad(PieceType.SIMPLE);
	}

	private void AddPieceToRoad(PieceType type)
	{
		for (var i = _leafs.Count - 1; i >= 0; i--)
		{
			if (_leafs[i].IsCrossRoadType())
			{
				CreateAndAddPieceToRoad(_leafs[i], type);
				CreateAndAddPieceToRoad(_leafs[i], type);

				_leafs.Add(_leafs[i].children[1]);
				_leafs[i] = _leafs[i].children[0];
			}
			else
			{
				_leafs[i] = CreateAndAddPieceToRoad(_leafs[i], type);
			}
		}
	}


	private List<PieceType> GetPossibleSpecialPieceTypes(PieceEntry parent)
	{
		var result = new List<PieceType>();

		// poate fi pusa o piesa de tipul LEFT_PIECE?
		if (ItCanBeAddedToRoad(parent, PieceType.LEFT))
			result.Add(PieceType.LEFT);

		// poate fi pusa o piesa de tipul RIGHT_PIECE?
		if (ItCanBeAddedToRoad(parent, PieceType.RIGHT))
			result.Add(PieceType.RIGHT);

		// poate fi pusa o piesa de tipul UP_PIECE?
		if (ItCanBeAddedToRoad(parent, PieceType.UP))
			result.Add(PieceType.UP);

		// poate fi pusa o piesa de tipul DOWN_PIECE?
		if (ItCanBeAddedToRoad(parent, PieceType.DOWN))
			result.Add(PieceType.DOWN);

		if (_rnd.NextDouble() <= crossroadPercentage)
		{
			// poate fi pusa o piesa de tipul LEFT_AND_RIGHT_PIECE?
			if (ItCanBeAddedToRoad(parent, PieceType.LEFT_AND_RIGHT))
				result.Add(PieceType.LEFT_AND_RIGHT);

			// poate fi pusa o piesa de tipul UP_AND_DOWN_PIECE?
			if (ItCanBeAddedToRoad(parent, PieceType.UP_AND_DOWN))
				result.Add(PieceType.UP_AND_DOWN);
		}

		return result;
	}

	private List<PieceType> GetPossiblePieceTypes(PieceEntry parent)
	{
		var result = GetPossibleSpecialPieceTypes(parent);

		// poate fi pusa o piesa de tipul SIMPLE_PIECE?
		if (ItCanBeAddedToRoad(parent, PieceType.SIMPLE))
			result.Add(PieceType.SIMPLE);

		return result;
	}

	private bool ItCanBeAddedToRoad(PieceEntry parent, PieceType type)
	{
		// parintele este pus si cand a fost pus s-a stiut ca piesele imediat urmatoare (copilul/copii)
		// se pot pune. Daca adaug piesa curanta de tipul 'type' atunci copilul/copii ei au loc?

		// calculate the newPiece's direction
		PieceEntry newPiece = new PieceEntry();
		CalculateChildDirAndPlane(parent, out newPiece.dir, out newPiece.plane, out newPiece.upsideDown);
		newPiece.type = type;
		newPiece.gridPos = parent.gridPos + RoadPositions.gridTranslate[(int)newPiece.plane][(int)newPiece.dir];

		List<Vector3> childrenGridPos = new List<Vector3>();
		Direction childDir = newPiece.dir;
		Assets.Scripts.Plane childPlane;
		bool upsideDown;

		switch (type)
		{
			case PieceType.SIMPLE:
				childrenGridPos.Add(newPiece.gridPos + RoadPositions.gridTranslate[(int)newPiece.plane][(int)childDir]);
				break;

			case PieceType.LEFT:
			case PieceType.RIGHT:
			case PieceType.UP:
			case PieceType.DOWN:
				CalculateChildDirAndPlane(newPiece, out childDir, out childPlane, out upsideDown);
				childrenGridPos.Add(newPiece.gridPos + RoadPositions.gridTranslate[(int)childPlane][(int)childDir]);
				break;

			case PieceType.LEFT_AND_RIGHT:
			case PieceType.UP_AND_DOWN:
				CalculateChildDirAndPlane(newPiece, out childDir, out childPlane, out upsideDown);
				childrenGridPos.Add(newPiece.gridPos + RoadPositions.gridTranslate[(int)childPlane][(int)childDir]);

				newPiece.children.Add(new PieceEntry());

				CalculateChildDirAndPlane(newPiece, out childDir, out childPlane, out upsideDown);
				childrenGridPos.Add(newPiece.gridPos + RoadPositions.gridTranslate[(int)childPlane][(int)childDir]);
				break;
		}

		foreach (var checkPos in childrenGridPos)
		{
			if (_takenPos.Contains(checkPos))
				return false;
		}

		return true;
	}

	private void AddSpecialPiece(int parentInd, bool firstCall = true)
	{
		var possiblePieceTypes = GetPossibleSpecialPieceTypes(_leafs[parentInd]);
		if (possiblePieceTypes.Count > 0)
		{
			if (_leafs[parentInd].IsCrossRoadType())
			{
				CreateAndAddPieceToRoad(_leafs[parentInd], possiblePieceTypes[_rnd.Next(possiblePieceTypes.Count)]);
				CreateAndAddPieceToRoad(_leafs[parentInd], possiblePieceTypes[_rnd.Next(possiblePieceTypes.Count)]);

				_leafs.Add(_leafs[parentInd].children[1]);
				_leafs[parentInd] = _leafs[parentInd].children[0];
			}
			else
			{
				_leafs[parentInd] = CreateAndAddPieceToRoad(_leafs[parentInd], possiblePieceTypes[_rnd.Next(possiblePieceTypes.Count)]);
			}
		}
		else if (firstCall)
		{
			AddSimplePiece(parentInd, false);
		}
		else
		{
			Debug.Log("COLLISION");
		}
	}
	
	private void AddSimplePiece(int parentInd, bool firstCall = true)
	{
		if (ItCanBeAddedToRoad(_leafs[parentInd], PieceType.SIMPLE))
		{
			if (_leafs[parentInd].IsCrossRoadType())
			{
				CreateAndAddPieceToRoad(_leafs[parentInd], PieceType.SIMPLE);
				CreateAndAddPieceToRoad(_leafs[parentInd], PieceType.SIMPLE);

				_leafs.Add(_leafs[parentInd].children[1]);
				_leafs[parentInd] = _leafs[parentInd].children[0];
			}
			else
			{
				_leafs[parentInd] = CreateAndAddPieceToRoad(_leafs[parentInd], PieceType.SIMPLE);
			}
		}
		else if (firstCall)
		{
			AddSpecialPiece(parentInd, false);
		}
		else
		{
			Debug.Log("COLLISION");
		}
	}

	public void AddPiecesToRoad()
	{
		var genes = _genAlgo.GetGenes();
		
		foreach (var gene in genes)
		{
			for (var i = _leafs.Count - 1; i >= 0; i--)
			{
				if (gene == true)       // it means a special piece will be added
				{
					AddSpecialPiece(i);
				}
				else
				{
					AddSimplePiece(i);
				}
			} 
		}

		_genAlgo.Evolve();
	}

	public void RemovePiecesFromRoad()
	{
		int numPieces = genAlgoSettings.numChromosoms * genAlgoSettings.numPiecesOnChromosome;
		PieceEntry aux = _root;

		for (int i = 0; i < numPieces; i++)
		{
			foreach(var child in _root.children)
			{
				if (child.playerWasHere)
					aux = child;
				else
					child.Delete(true);
			}

			_root.Delete();
			_root = aux;
		}
	}
}
