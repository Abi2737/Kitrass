using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts;

public class RoadGeneration : MonoBehaviour
{
	public GameObject simplePiece;
	public GameObject rightPiece;
	public GameObject leftPiece;
	public GameObject leftAndRightPiece;

	public enum PieceType
	{
		NONE,

		SIMPLE,
		LEFT,
		RIGHT,
		LEFT_AND_RIGHT
	}

	public enum Direction
	{
		FORWARD = 0,
		RIGHT = 1,
		BACKWARD = 2,
		LEFT = 3
	}

	public class PieceEntry
	{
		public GameObject piece;
		public PieceType type;
		public Direction dir;
		public List<PieceEntry> children;
		public Vector3 gridPos;

		public PieceEntry()
		{
			piece = null;
			type = PieceType.SIMPLE;
			dir = Direction.FORWARD;
			children = new List<PieceEntry>();
			gridPos = Vector3.zero;
		}
	}

	private PieceEntry _root;
	private List<PieceEntry> _leafs;
	private HashSet<Vector3> _takenPos;

	private void Awake()
	{
		_root = new PieceEntry();
		_root.piece = Instantiate(simplePiece);
		_root.type = PieceType.SIMPLE;
		_root.piece.transform.position = Vector3.zero;
		_root.gridPos = Vector3.zero;

		_leafs = new List<PieceEntry>();
		_leafs.Add(_root);

		_takenPos = new HashSet<Vector3>();
		_takenPos.Add(_root.gridPos);


		DebugCreateInitialRoad();
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
		//DebugCreateRuntimeRoad();
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

	private PieceEntry CreateAndAddPieceToRoad(PieceEntry parent, PieceType type)
	{
		// create the new piece
		PieceEntry newPiece = new PieceEntry();
		// set it with the given type
		newPiece.type = type;

		// instantiate the piece according with the given type
		switch (type)
		{
			case PieceType.SIMPLE:
				newPiece.piece = Instantiate(simplePiece);
				break;
			case PieceType.LEFT:
				newPiece.piece = Instantiate(leftPiece);
				break;
			case PieceType.RIGHT:
				newPiece.piece = Instantiate(rightPiece);
				break;
			case PieceType.LEFT_AND_RIGHT:
				newPiece.piece = Instantiate(leftAndRightPiece);
				break;
		}
		
		int dir = (int)parent.dir;	// newPiece's direction
		Vector3[] translateXOZ = RoadPositions.forwardTranslateXOZ;

		switch (parent.type)
		{
			case PieceType.LEFT:
				dir = (int)parent.dir - 1;
				if (dir < 0)
					dir = 3;

				translateXOZ = RoadPositions.leftTranslateXOZ;
				break;

			case PieceType.RIGHT:
				dir = (int)parent.dir + 1;
				if (dir > 3)
					dir = 0;

				translateXOZ = RoadPositions.rightTranslateXOZ;
				break;

			case PieceType.LEFT_AND_RIGHT:
				if (parent.children.Count == 0)	// first time add the new piece on the parent's left
				{
					dir = (int)parent.dir - 1;
					if (dir < 0)
						dir = 3;

					translateXOZ = RoadPositions.leftTranslateXOZ;
				}
				else	// second time add the new piece on the parent's right
				{
					dir = (int)parent.dir + 1;
					if (dir > 3)
						dir = 0;

					translateXOZ = RoadPositions.rightTranslateXOZ;
				}
				break;
		}

		// calculate the newPiece's grid position based on the parent's grid position and the newPiece's direction
		newPiece.gridPos = parent.gridPos + RoadPositions.gridTranslateXOZ[dir];

		// set the direction
		newPiece.dir = (Direction)dir;

		// calculate the newPiece's world position based on the parent's world position
		int ind = (int)parent.dir;
		Vector3 offsetTranslate = Vector3.zero;
		//Vector3 offsetTranslate = RoadPositions.forwardTranslateXOZoffset[(int)dir];
		newPiece.piece.transform.position = parent.piece.transform.position + translateXOZ[ind] + offsetTranslate;

		// set the newPiece's rotation (-90 initial rotation of the prefab)
		newPiece.piece.transform.eulerAngles = new Vector3(0, -90 + 90 * dir, 0);

		// add the newPiece in the parent's list of children
		parent.children.Add(newPiece);
		
		return newPiece;
	}

	private bool IsEmptyPos(PieceEntry parent, PieceType childType)
	{
		// calculate the newPiece's direction
		int dir = (int)parent.dir;
		switch (parent.type)
		{
			case PieceType.LEFT:
				dir = (int)parent.dir - 1;
				if (dir < 0)
					dir = 3;
				break;

			case PieceType.RIGHT:
				dir = (int)parent.dir + 1;
				if (dir > 3)
					dir = 0;
				break;

			case PieceType.LEFT_AND_RIGHT:
				if (parent.children.Count == 0)
				{
					dir = (int)parent.dir - 1;
					if (dir < 0)
						dir = 3;
				}
				else
				{
					dir = (int)parent.dir + 1;
					if (dir > 3)
						dir = 0;
				}
				break;
		}

		// calculate the checking position based on the parent gridPosition and the newPiece's direction
		Vector3 checkPos = parent.gridPos + RoadPositions.gridTranslateXOZ[dir];

		return !_takenPos.Contains(checkPos);
	}


	private void DebugCreateRuntimeRoad()
	{
		PieceType crtType = PieceType.NONE;

		if (Input.GetKeyDown(KeyCode.S))
		{
			crtType = PieceType.SIMPLE;
		}
		else if (Input.GetKeyDown(KeyCode.R))
		{
			crtType = PieceType.RIGHT;
		}
		else if (Input.GetKeyDown(KeyCode.L))
		{
			crtType = PieceType.LEFT;
		}
		else if (Input.GetKeyDown(KeyCode.B))
		{
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
}
