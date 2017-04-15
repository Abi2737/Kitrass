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

	public enum PieceType
	{
		SIMPLE = 0,
		LEFT = 1,
		RIGHT = 2,
		LEFT_AND_RIGHT = 3,
		UP = 4,
		DOWN = 5,

		COUNT = 6,
		NONE = 100
	}

	public class PieceEntry
	{
		public GameObject piece;
		public PieceType type;
		public Direction dir;
		public Assets.Scripts.Plane plane;
		public PieceEntry parent;
		public List<PieceEntry> children;
		public Vector3 gridPos;

		public PieceEntry()
		{
			piece = null;
			type = PieceType.SIMPLE;
			dir = Direction.FORWARD;
			plane = Assets.Scripts.Plane.ZOX;
			parent = null;
			children = new List<PieceEntry>();
			gridPos = Vector3.zero;
		}
	}

	public GeneticAlgoSettings genAlgoSettings = new GeneticAlgoSettings();

	PieceEntry _root;
	List<PieceEntry> _leafs;
	HashSet<Vector3> _takenPos;

	GeneticAlgorithm _genAlgo;

	private void Awake()
	{
		_root = new PieceEntry();
		_root.piece = Instantiate(simplePiece);
		_root.type = PieceType.SIMPLE;
		_root.piece.transform.position = Vector3.zero;
		_root.gridPos = Vector3.zero;

		// ZOX
		_root.plane = Assets.Scripts.Plane.ZOX;
		_root.piece.transform.eulerAngles = RoadPositions.initialRotation[(int)_root.plane];

		// XOY
		//_root.plane = Assets.Scripts.Plane.XOY;
		//_root.piece.transform.eulerAngles = RoadPositions.initialRotation[(int)_root.plane];

		// YOZ
		//_root.plane = Assets.Scripts.Plane.YOZ;
		//_root.piece.transform.eulerAngles = RoadPositions.initialRotation[(int)_root.plane];

		_leafs = new List<PieceEntry>();
		_leafs.Add(_root);

		_takenPos = new HashSet<Vector3>();
		_takenPos.Add(_root.gridPos);


		_genAlgo = new GeneticAlgorithm(
			genAlgoSettings.numChromosoms,
			genAlgoSettings.numPiecesOnChromosome,
			genAlgoSettings.elitismPercentage,
			genAlgoSettings.crossoverPercentage,
			genAlgoSettings.mutationPercentage,
			genAlgoSettings.mutationChangePercentage);


		//DebugCreateInitialRoad();
		//DebugCreateInitialStraightRoad(20);
		//DebugCreateInitialOneRightPieceRoad(4, 1);
		//DebugCreateInitialOneLeftPieceRoad(4, 1);
	}

	// Use this for initialization
	void Start ()
	{
		PrintRoad();
	}

	// Update is called once per frame
	void Update ()
	{
		//if (Input.GetKeyDown(KeyCode.Space))
		//{
		//	AddPiecesToRoad();
		//}

		//if (Input.GetKeyDown(KeyCode.K))
		//{
		//	string s = "";
		//	foreach (var v in _takenPos)
		//	{
		//		s += "(" + v.x + "," + v.y + "," + v.z + ") ";
		//	}
		//	Debug.Log(s);
		//}

		DebugCreateRuntimeRoad();
	}

	public PieceEntry GetRoadRoot()
	{
		return _root;
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
		}

		return Instantiate(simplePiece);
	}


	private PieceEntry CreateAndAddPieceToRoad(PieceEntry parent, PieceType type)
	{
		// create the new piece
		PieceEntry newPiece = new PieceEntry();
		// set it with the given type
		newPiece.type = type;

		Direction dir = parent.dir;  // newPiece's direction
		Vector3[] translate = RoadPositions.forwardTranslate[(int)parent.plane];
		Vector3 rotation = RoadPositions.rotation[(int)parent.plane];

		// set the newPiece's plane
		newPiece.plane = parent.plane;

		switch (parent.type)
		{
			case PieceType.LEFT:
				dir = DirectionToLeft(parent.dir);

				translate = RoadPositions.leftTranslate[(int)parent.plane];
				break;

			case PieceType.RIGHT:
				dir = DirectionToRight(parent.dir);

				translate = RoadPositions.rightTranslate[(int)parent.plane];
				break;

			case PieceType.LEFT_AND_RIGHT:
				if (parent.children.Count == 0) // first time add the new piece on the parent's left
				{
					dir = DirectionToLeft(parent.dir);

					translate = RoadPositions.leftTranslate[(int)parent.plane];
				}
				else    // second time add the new piece on the parent's right
				{
					dir = DirectionToRight(parent.dir);

					translate = RoadPositions.rightTranslate[(int)parent.plane];
				}
				break;
		}

		// calculate the newPiece's grid position based on the parent's grid position and the newPiece's direction
		newPiece.gridPos = parent.gridPos + RoadPositions.gridTranslate[(int)parent.plane][(int)dir];

		// set the direction
		newPiece.dir = (Direction)dir;

		// instantiate the piece according with the given type
		newPiece.piece = InstantiatePiece(type);

		// calculate the newPiece's world position based on the parent's world position
		int ind = (int)parent.dir;
		Vector3 offsetTranslate = Vector3.zero;
		//Vector3 offsetTranslate = RoadPositions.forwardTranslateXOZoffset[(int)dir];
		newPiece.piece.transform.position = parent.piece.transform.position + translate[ind] + offsetTranslate;

		// set the newPiece's rotation (-90 initial rotation of the prefab)
		newPiece.piece.transform.eulerAngles = RoadPositions.initialRotation[(int)newPiece.plane] + rotation * (int)dir;


		// add the newPiece in the parent's list of children
		parent.children.Add(newPiece);

		// set the newPiece's parent
		newPiece.parent = parent;


		// mark the grid pos as taken
		_takenPos.Add(newPiece.gridPos);

		return newPiece;
	}

	private bool IsEmptyPos(PieceEntry parent, PieceType childType)
	{
		// calculate the newPiece's direction
		Direction dir = parent.dir;
		Assets.Scripts.Plane plane = parent.plane;
		switch (parent.type)
		{
			case PieceType.LEFT:
				dir = DirectionToLeft(parent.dir);
				break;

			case PieceType.RIGHT:
				dir = DirectionToRight(parent.dir);
				break;

			case PieceType.LEFT_AND_RIGHT:
				if (parent.children.Count == 0)
				{
					dir = DirectionToLeft(parent.dir);
				}
				else
				{
					dir = DirectionToRight(parent.dir);
				}
				break;
		}

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

		if (crtType != PieceType.NONE)
		{
			List<PieceEntry> newLeafs = new List<PieceEntry>();
			foreach (var pe in _leafs)
			{
				if (!IsEmptyPos(pe, crtType))
					continue;

				PieceEntry newPiece = CreateAndAddPieceToRoad(pe, crtType);
				_takenPos.Add(newPiece.gridPos);
				newLeafs.Add(newPiece);

				if (pe.type == PieceType.LEFT_AND_RIGHT)
				{
					if (!IsEmptyPos(pe, crtType))
						continue;

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
		PieceEntry p12 = CreateAndAddPieceToRoad(p11, PieceType.LEFT_AND_RIGHT);
		PieceEntry p13 = CreateAndAddPieceToRoad(p12, PieceType.RIGHT);
		PieceEntry p14 = CreateAndAddPieceToRoad(p13, PieceType.RIGHT);
		PieceEntry p15 = CreateAndAddPieceToRoad(p14, PieceType.SIMPLE);

		PieceEntry p16 = CreateAndAddPieceToRoad(p12, PieceType.RIGHT);
		PieceEntry p17 = CreateAndAddPieceToRoad(p16, PieceType.LEFT);
		PieceEntry p18 = CreateAndAddPieceToRoad(p17, PieceType.SIMPLE);
	}

	private void DebugCreateInitialStraightRoad(int numPieces)
	{
		PieceEntry parent = _root;

		for ( int i = 0; i < numPieces; i++ )
		{
			parent = CreateAndAddPieceToRoad(parent, PieceType.SIMPLE);
		}
	}

	private void DebugCreateInitialOneRightPieceRoad(int numPieces, int indRightPiece)
	{
		PieceEntry parent = _root;

		for (int i = 0; i < indRightPiece - 1; i++)
		{
			parent = CreateAndAddPieceToRoad(parent, PieceType.SIMPLE);
		}

		parent = CreateAndAddPieceToRoad(parent, PieceType.RIGHT);

		for (var i = indRightPiece; i < numPieces; i++ )
		{
			parent = CreateAndAddPieceToRoad(parent, PieceType.SIMPLE);
		}

		for (int i = 0; i < indRightPiece - 1; i++)
		{
			parent = CreateAndAddPieceToRoad(parent, PieceType.SIMPLE);
		}

		parent = CreateAndAddPieceToRoad(parent, PieceType.RIGHT);

		for (var i = indRightPiece; i < numPieces; i++)
		{
			parent = CreateAndAddPieceToRoad(parent, PieceType.SIMPLE);
		}

		for (int i = 0; i < indRightPiece - 1; i++)
		{
			parent = CreateAndAddPieceToRoad(parent, PieceType.SIMPLE);
		}

		parent = CreateAndAddPieceToRoad(parent, PieceType.RIGHT);

		for (var i = indRightPiece; i < numPieces; i++)
		{
			parent = CreateAndAddPieceToRoad(parent, PieceType.SIMPLE);
		}

		for (int i = 0; i < indRightPiece + 1; i++)
		{
			parent = CreateAndAddPieceToRoad(parent, PieceType.SIMPLE);
		}

		parent = CreateAndAddPieceToRoad(parent, PieceType.RIGHT);

		for (var i = indRightPiece; i < numPieces; i++)
		{
			parent = CreateAndAddPieceToRoad(parent, PieceType.SIMPLE);
		}

		for (int i = 0; i < indRightPiece + 1; i++)
		{
			parent = CreateAndAddPieceToRoad(parent, PieceType.SIMPLE);
		}

		parent = CreateAndAddPieceToRoad(parent, PieceType.RIGHT);

		for (var i = indRightPiece; i < numPieces; i++)
		{
			parent = CreateAndAddPieceToRoad(parent, PieceType.SIMPLE);
		}
	}

	private void DebugCreateInitialOneLeftPieceRoad(int numPieces, int indRightPiece)
	{
		PieceEntry parent = _root;

		for (int i = 0; i < indRightPiece - 1; i++)
		{
			parent = CreateAndAddPieceToRoad(parent, PieceType.SIMPLE);
		}

		parent = CreateAndAddPieceToRoad(parent, PieceType.LEFT);

		for (var i = indRightPiece; i < numPieces; i++)
		{
			parent = CreateAndAddPieceToRoad(parent, PieceType.SIMPLE);
		}

		for (int i = 0; i < indRightPiece - 1; i++)
		{
			parent = CreateAndAddPieceToRoad(parent, PieceType.SIMPLE);
		}

		parent = CreateAndAddPieceToRoad(parent, PieceType.LEFT);

		for (var i = indRightPiece; i < numPieces; i++)
		{
			parent = CreateAndAddPieceToRoad(parent, PieceType.SIMPLE);
		}

		for (int i = 0; i < indRightPiece - 1; i++)
		{
			parent = CreateAndAddPieceToRoad(parent, PieceType.SIMPLE);
		}

		parent = CreateAndAddPieceToRoad(parent, PieceType.LEFT);

		for (var i = indRightPiece; i < numPieces; i++)
		{
			parent = CreateAndAddPieceToRoad(parent, PieceType.SIMPLE);
		}

		for (int i = 0; i < indRightPiece + 1; i++)
		{
			parent = CreateAndAddPieceToRoad(parent, PieceType.SIMPLE);
		}

		parent = CreateAndAddPieceToRoad(parent, PieceType.LEFT);

		for (var i = indRightPiece; i < numPieces; i++)
		{
			parent = CreateAndAddPieceToRoad(parent, PieceType.SIMPLE);
		}

		for (int i = 0; i < indRightPiece + 1; i++)
		{
			parent = CreateAndAddPieceToRoad(parent, PieceType.SIMPLE);
		}

		parent = CreateAndAddPieceToRoad(parent, PieceType.LEFT);

		for (var i = indRightPiece; i < numPieces; i++)
		{
			parent = CreateAndAddPieceToRoad(parent, PieceType.SIMPLE);
		}
	}


	private Direction DirectionToRight(Direction relativeTo)
	{
		int resultDir = (int)relativeTo + 1;
		if (resultDir > 3)
			resultDir = 0;

		return (Direction)resultDir;
	}

	private Direction DirectionToLeft(Direction relativeTo)
	{
		int resultDir = (int)relativeTo - 1;
		if (resultDir < 0)
			resultDir = 3;

		return (Direction)resultDir;
	}

	private Direction GetChildDirection(PieceType parentType, Direction parentDir, Direction relativeToParent = Direction.FORWARD)
	{
		Direction childDir = parentDir;
		switch (parentType)
		{
			case PieceType.LEFT:
				childDir = DirectionToLeft(parentDir);
				break;

			case PieceType.RIGHT:
				childDir = DirectionToRight(parentDir);
				break;

			case PieceType.LEFT_AND_RIGHT:
				if (relativeToParent == Direction.LEFT)
					childDir = DirectionToLeft(parentDir);
				else
					childDir = DirectionToRight(parentDir);

				break;
		}

		return childDir;
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


	// relativeToParent matters only if the parent type si LEFT_AND_RIGHT
	private bool ItCanBeAddedToRoad(PieceEntry parent, PieceType type, Direction relativeToParent = Direction.FORWARD)
	{
		// parintele este pus si cand a fost pus s-a stiut ca piesele imediat urmatoare (copilul/copii)
		// se pot pune. Daca adaug piesa curanta de tipul 'type' atunci copilul/copii ei au loc?

		// calculate the newPiece's direction
		Direction newPieceDir = GetChildDirection(parent.type, parent.dir, relativeToParent);
		Vector3 newPiecePos = parent.gridPos + RoadPositions.gridTranslate[(int)parent.plane][(int)newPieceDir];

		List<Vector3> childrenGridPos = new List<Vector3>();
		Direction childDir = newPieceDir;

		switch (type)
		{
			case PieceType.SIMPLE:
				childrenGridPos.Add(newPiecePos + RoadPositions.gridTranslate[(int)parent.plane][(int)childDir]);
				break;

			case PieceType.LEFT:
			case PieceType.RIGHT:
				childDir = GetChildDirection(type, newPieceDir);
				childrenGridPos.Add(newPiecePos + RoadPositions.gridTranslate[(int)parent.plane][(int)childDir]);
				break;

			case PieceType.LEFT_AND_RIGHT:
				childDir = GetChildDirection(type, newPieceDir, Direction.LEFT);
				childrenGridPos.Add(newPiecePos + RoadPositions.gridTranslate[(int)parent.plane][(int)childDir]);

				childDir = GetChildDirection(type, newPieceDir, Direction.RIGHT);
				childrenGridPos.Add(newPiecePos + RoadPositions.gridTranslate[(int)parent.plane][(int)childDir]);
				break;
			default:
				break;
		}

		foreach (var checkPos in childrenGridPos)
		{
			if (_takenPos.Contains(checkPos))
				return false;
		}

		return true;
	}

	private void AddPiecesToRoad()
	{
		var genes = _genAlgo.GetGenes();

		System.Random rnd = new System.Random();
		
		foreach (var gene in genes)
		{
			for (var i = 0; i < _leafs.Count; i++)
			{
				if (gene == true)       // it means a special piece will be added
				{
					var possiblePieceTypes = GetPossibleSpecialPieceTypes(_leafs[i]);
					if ( possiblePieceTypes.Count > 0 )
					{
						CreateAndAddPieceToRoad(_leafs[i], possiblePieceTypes[rnd.Next(possiblePieceTypes.Count)]);
						_leafs[i] = _leafs[i].children[0];
					}
					else if (ItCanBeAddedToRoad(_leafs[i], PieceType.SIMPLE))
					{
						CreateAndAddPieceToRoad(_leafs[i], PieceType.SIMPLE);
						_leafs[i] = _leafs[i].children[0];
					}
					else
					{
						Debug.Log("COLLISION");
						//_leafs[i] = HandlePieceToPieceCollision(_leafs[i]);
					}
				}
				else
				{
					if (ItCanBeAddedToRoad(_leafs[i], PieceType.SIMPLE))
					{
						CreateAndAddPieceToRoad(_leafs[i], PieceType.SIMPLE);
						_leafs[i] = _leafs[i].children[0];
					}
					else
					{
						var possiblePieceTypes = GetPossibleSpecialPieceTypes(_leafs[i]);
						if (possiblePieceTypes.Count > 0)
						{
							_leafs[i] = CreateAndAddPieceToRoad(_leafs[i], possiblePieceTypes[rnd.Next(possiblePieceTypes.Count)]);
						}
						else
						{
							Debug.Log("COLLISION");
							//_leafs[i] = HandlePieceToPieceCollision(_leafs[i]);
						}
					}
				}
			} 
		}

		_genAlgo.Evolve();
	}
}
