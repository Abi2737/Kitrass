using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Scripts
{
	static class RoadPositions
	{
		public static float LENGTH_PIECE = 12;
		public static float WIDTH_PIECE = 8;
		public static float HEIGHT_PIECE = 4;

		public static Vector3[] gridTranslateXOZ =
		{
			new Vector3(0, 0, 1),		// forward +z
			new Vector3(1, 0, 0),		// right +x
			new Vector3(0, 0, -1),		// backward -z
			new Vector3(-1, 0, 0)		// left -x
		};

		public static Vector3[] forwardTranslateXOZ =
		{
			new Vector3(0, 0, LENGTH_PIECE),		// forward +z
			new Vector3(LENGTH_PIECE, 0, 0),		// right +x
			new Vector3(0, 0, -LENGTH_PIECE),		// backward -z
			new Vector3(-LENGTH_PIECE, 0, 0)		// left -x
		};

		public static Vector3[] rightTranslateXOZ =
		{
			new Vector3(WIDTH_PIECE/2 + LENGTH_PIECE/2, 0, LENGTH_PIECE/2 - WIDTH_PIECE/2),		// forward +z
			new Vector3(LENGTH_PIECE/2 - WIDTH_PIECE/2, 0, -WIDTH_PIECE/2 - LENGTH_PIECE/2),	// right +x
			new Vector3(-WIDTH_PIECE/2 - LENGTH_PIECE/2, 0, -LENGTH_PIECE/2 + WIDTH_PIECE/2),	// backward -z
			new Vector3(-LENGTH_PIECE/2 + WIDTH_PIECE/2, 0, WIDTH_PIECE/2 + LENGTH_PIECE/2)		// left -x
		};

		public static Vector3[] leftTranslateXOZ =
		{
			new Vector3(-WIDTH_PIECE/2 - LENGTH_PIECE/2, 0, LENGTH_PIECE/2 - WIDTH_PIECE/2),	// forward +z
			new Vector3(LENGTH_PIECE/2 - WIDTH_PIECE/2, 0, WIDTH_PIECE/2 + LENGTH_PIECE/2),		// right +x
			new Vector3(WIDTH_PIECE/2 + LENGTH_PIECE/2, 0, -LENGTH_PIECE/2 + WIDTH_PIECE/2),	// backward -z
			new Vector3(-LENGTH_PIECE/2 + WIDTH_PIECE/2, 0, -WIDTH_PIECE/2 - LENGTH_PIECE/2)	// left -x
		};
	}
}
