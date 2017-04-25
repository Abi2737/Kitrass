using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Scripts
{
	public enum Plane
	{
		ZOX = 0,
		XOY = 1,
		YOZ = 2
	}

	public enum Direction
	{
		FORWARD = 0,
		RIGHT = 1,
		BACKWARD = 2,
		LEFT = 3
	}

	public static class RoadPositions
	{
		public static float LENGTH_PIECE = 12;
		public static float WIDTH_PIECE = 8;
		public static float HEIGHT_PIECE = 4;

		public static Vector3[] initialRotation =
		{
			// ZOX
			new Vector3(0, 0, 0),

			// XOY
			new Vector3(0, 90, -90),

			// YOZ
			new Vector3(-90, 0, -90)
		};

		public static Vector3[] upsideDownInitialRotation =
		{
			// ZOX
			new Vector3(0, 0, 180),

			// XOY
			new Vector3(0, 90, 90),

			// YOZ
			new Vector3(-90, 0, 90)
		};

		public static Vector3[] rotation =
		{
			// ZOX
			new Vector3(0, 90, 0),

			// XOY
			new Vector3(90, 0, 0),

			// YOZ
			new Vector3(90, 0, 0)
		};


		public static Vector3[][] gridTranslate =
		{
			// ZOX
			new Vector3[]
			{
				new Vector3(0, 0, 1),		// forward +z
				new Vector3(1, 0, 0),		// right +x
				new Vector3(0, 0, -1),		// backward -z
				new Vector3(-1, 0, 0)		// left -x
			},

			// XOY
			new Vector3[]
			{
				new Vector3(1, 0, 0),		// forward +x
				new Vector3(0, -1, 0),		// right -y
				new Vector3(-1, 0, 0),		// backward -x
				new Vector3(0, 1, 0)		// left +y
			},

			// YOZ
			new Vector3[]
			{
				new Vector3(0, 1, 0),		// forward +y
				new Vector3(0, 0, 1),		// right +z
				new Vector3(0, -1, 0),		// backward -y
				new Vector3(0, 0, -1)		// left -z
			}
		};

		public static Vector3[][] forwardTranslate =
		{
			// ZOX
			new Vector3[]
			{
				new Vector3(0, 0, LENGTH_PIECE),		// forward +z
				new Vector3(LENGTH_PIECE, 0, 0),		// right +x
				new Vector3(0, 0, -LENGTH_PIECE),		// backward -z
				new Vector3(-LENGTH_PIECE, 0, 0)		// left -x
			},

			// XOY
			new Vector3[]
			{
				new Vector3(LENGTH_PIECE, 0, 0),		// forward +x
				new Vector3(0, -LENGTH_PIECE, 0),		// right -y
				new Vector3(-LENGTH_PIECE, 0, 0),		// backward -x
				new Vector3(0, LENGTH_PIECE, 0)			// left +y
			},

			// YOZ
			new Vector3[]
			{
				new Vector3(0, LENGTH_PIECE, 0),		// forward +y
				new Vector3(0, 0, LENGTH_PIECE),		// right +z
				new Vector3(0, -LENGTH_PIECE, 0),		// backward -y
				new Vector3(0, 0, -LENGTH_PIECE)		// left -z
			}
		};

		public static Vector3[][] rightTranslate =
		{
			// ZOX
			new Vector3[]
			{
				new Vector3(WIDTH_PIECE/2 + LENGTH_PIECE/2, 0, LENGTH_PIECE/2 - WIDTH_PIECE/2),		// forward +z
				new Vector3(LENGTH_PIECE/2 - WIDTH_PIECE/2, 0, -WIDTH_PIECE/2 - LENGTH_PIECE/2),	// right +x
				new Vector3(-WIDTH_PIECE/2 - LENGTH_PIECE/2, 0, -LENGTH_PIECE/2 + WIDTH_PIECE/2),	// backward -z
				new Vector3(-LENGTH_PIECE/2 + WIDTH_PIECE/2, 0, WIDTH_PIECE/2 + LENGTH_PIECE/2)		// left -x
			},

			// XOY
			new Vector3[]
			{
				new Vector3(LENGTH_PIECE/2 - WIDTH_PIECE/2, -WIDTH_PIECE/2 - LENGTH_PIECE/2, 0),	// forward +x
				new Vector3(-WIDTH_PIECE/2 - LENGTH_PIECE/2, -LENGTH_PIECE/2 + WIDTH_PIECE/2, 0),	// right -y
				new Vector3(-LENGTH_PIECE/2 + WIDTH_PIECE/2, WIDTH_PIECE/2 + LENGTH_PIECE/2, 0),	// backward -x
				new Vector3(WIDTH_PIECE/2 + LENGTH_PIECE/2, LENGTH_PIECE/2 - WIDTH_PIECE/2, 0)		// left +y
			},
			
			// YOZ
			new Vector3[]
			{
				new Vector3(0, LENGTH_PIECE/2 - WIDTH_PIECE/2, WIDTH_PIECE/2 + LENGTH_PIECE/2),		// forward +y
				new Vector3(0, -WIDTH_PIECE/2 - LENGTH_PIECE/2, LENGTH_PIECE/2 - WIDTH_PIECE/2),	// right +z
				new Vector3(0, -LENGTH_PIECE/2 + WIDTH_PIECE/2, -WIDTH_PIECE/2 - LENGTH_PIECE/2),	// backward -y
				new Vector3(0, WIDTH_PIECE/2 + LENGTH_PIECE/2, -LENGTH_PIECE/2 + WIDTH_PIECE/2)		// left -z
			}
		};

		public static Vector3[][] leftTranslate =
		{
			// ZOX
			new Vector3[]
			{
				new Vector3(-WIDTH_PIECE/2 - LENGTH_PIECE/2, 0, LENGTH_PIECE/2 - WIDTH_PIECE/2),	// forward +z
				new Vector3(LENGTH_PIECE/2 - WIDTH_PIECE/2, 0, WIDTH_PIECE/2 + LENGTH_PIECE/2),		// right +x
				new Vector3(WIDTH_PIECE/2 + LENGTH_PIECE/2, 0, -LENGTH_PIECE/2 + WIDTH_PIECE/2),	// backward -z
				new Vector3(-LENGTH_PIECE/2 + WIDTH_PIECE/2, 0, -WIDTH_PIECE/2 - LENGTH_PIECE/2)	// left -x
			},
			
			// XOY
			new Vector3[]
			{
				new Vector3(LENGTH_PIECE/2 - WIDTH_PIECE/2, WIDTH_PIECE/2 + LENGTH_PIECE/2, 0),		// forward +x
				new Vector3(WIDTH_PIECE/2 + LENGTH_PIECE/2, -LENGTH_PIECE/2 + WIDTH_PIECE/2, 0),	// right -y
				new Vector3(-LENGTH_PIECE/2 + WIDTH_PIECE/2, -WIDTH_PIECE/2 - LENGTH_PIECE/2, 0),	// backward -x
				new Vector3(-WIDTH_PIECE/2 - LENGTH_PIECE/2, LENGTH_PIECE/2 - WIDTH_PIECE/2, 0)		// left +y
			},
			
			// YOZ
			new Vector3[]
			{
				new Vector3(0, LENGTH_PIECE/2 - WIDTH_PIECE/2, -WIDTH_PIECE/2 - LENGTH_PIECE/2),	// forward +y
				new Vector3(0, WIDTH_PIECE/2 + LENGTH_PIECE/2, LENGTH_PIECE/2 - WIDTH_PIECE/2),		// right +z
				new Vector3(0, -LENGTH_PIECE/2 + WIDTH_PIECE/2, WIDTH_PIECE/2 + LENGTH_PIECE/2),	// backward -y
				new Vector3(0, -WIDTH_PIECE/2 - LENGTH_PIECE/2, -LENGTH_PIECE/2 + WIDTH_PIECE/2)	// left -z
			}
		};

		public static Vector3[][] upTranslate =
		{
			// ZOX
			new Vector3[]
			{
				new Vector3(0, HEIGHT_PIECE/2 + LENGTH_PIECE/2, LENGTH_PIECE/2 - HEIGHT_PIECE/2),	// forward +z
				new Vector3(LENGTH_PIECE/2 - HEIGHT_PIECE/2, HEIGHT_PIECE/2 + LENGTH_PIECE/2, 0),	// right +x
				new Vector3(0, HEIGHT_PIECE/2 + LENGTH_PIECE/2, -LENGTH_PIECE/2 + HEIGHT_PIECE/2),	// backward -z
				new Vector3(-LENGTH_PIECE/2 + HEIGHT_PIECE/2, HEIGHT_PIECE/2 + LENGTH_PIECE/2, 0)	// left -x
			},
			
			// XOY
			new Vector3[]
			{
				new Vector3(LENGTH_PIECE/2 - HEIGHT_PIECE/2, 0, -HEIGHT_PIECE/2 - LENGTH_PIECE/2),	// forward +x
				new Vector3(0, -LENGTH_PIECE/2 + HEIGHT_PIECE/2, -HEIGHT_PIECE/2 - LENGTH_PIECE/2),	// right -y
				new Vector3(-LENGTH_PIECE/2 + HEIGHT_PIECE/2, 0, -HEIGHT_PIECE/2 - LENGTH_PIECE/2),	// backward -x
				new Vector3(0, LENGTH_PIECE/2 - HEIGHT_PIECE/2, - HEIGHT_PIECE/2 - LENGTH_PIECE/2)	// left +y
			},

			// YOZ
			new Vector3[]
			{
				new Vector3(HEIGHT_PIECE/2 + LENGTH_PIECE/2, LENGTH_PIECE/2 - HEIGHT_PIECE/2, 0),	// forward +y
				new Vector3(HEIGHT_PIECE/2 + LENGTH_PIECE/2, 0, +LENGTH_PIECE/2 - HEIGHT_PIECE/2),	// right +z
				new Vector3(HEIGHT_PIECE/2 + LENGTH_PIECE/2, -LENGTH_PIECE/2 + HEIGHT_PIECE/2, 0),	// backward -y
				new Vector3(HEIGHT_PIECE/2 + LENGTH_PIECE/2, 0, -LENGTH_PIECE/2 + HEIGHT_PIECE/2)	// left -z
			}
		};

		public static Vector3[][] downTranslate =
		{
			// ZOX
			new Vector3[]
			{
				new Vector3(0, -HEIGHT_PIECE/2 - LENGTH_PIECE/2, LENGTH_PIECE/2 - HEIGHT_PIECE/2),	// forward +z
				new Vector3(LENGTH_PIECE/2 - HEIGHT_PIECE/2, -HEIGHT_PIECE/2 - LENGTH_PIECE/2, 0),	// right +x
				new Vector3(0, -HEIGHT_PIECE/2 - LENGTH_PIECE/2, -LENGTH_PIECE/2 + HEIGHT_PIECE/2),	// backward -z
				new Vector3(-LENGTH_PIECE/2 + HEIGHT_PIECE/2, -HEIGHT_PIECE/2 - LENGTH_PIECE/2, 0)	// left -x
			},

			// XOY
			new Vector3[]
			{
				new Vector3(LENGTH_PIECE/2 - HEIGHT_PIECE/2, 0, HEIGHT_PIECE/2 + LENGTH_PIECE/2),	// forward +x
				new Vector3(0, -LENGTH_PIECE/2 + HEIGHT_PIECE/2, HEIGHT_PIECE/2 + LENGTH_PIECE/2),	// right -y
				new Vector3(-LENGTH_PIECE/2 + HEIGHT_PIECE/2, 0, HEIGHT_PIECE/2 + LENGTH_PIECE/2),	// backward -x
				new Vector3(0, LENGTH_PIECE/2 - HEIGHT_PIECE/2,  HEIGHT_PIECE/2 + LENGTH_PIECE/2)	// left +y
			},

			// YOZ
			new Vector3[]
			{
				new Vector3(-HEIGHT_PIECE/2 - LENGTH_PIECE/2, LENGTH_PIECE/2 - HEIGHT_PIECE/2, 0),	// forward +y
				new Vector3(-HEIGHT_PIECE/2 - LENGTH_PIECE/2, 0, LENGTH_PIECE/2 - HEIGHT_PIECE/2),	// right +z
				new Vector3(-HEIGHT_PIECE/2 - LENGTH_PIECE/2, -LENGTH_PIECE/2 + HEIGHT_PIECE/2, 0),	// backward -y
				new Vector3(-HEIGHT_PIECE/2 - LENGTH_PIECE/2, 0, -LENGTH_PIECE/2 + HEIGHT_PIECE/2)	// left -z
			}
		};
	}
}
