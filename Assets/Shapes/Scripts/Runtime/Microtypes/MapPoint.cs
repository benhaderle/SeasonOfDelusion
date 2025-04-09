using System;
using System.Collections.Generic;
using UnityEngine;

// Shapes © Freya Holmér - https://twitter.com/FreyaHolmer/
// Website & Documentation - https://acegikmo.com/shapes/
namespace Shapes {

	[Serializable]
	public class MapPoint
	{
		public int id;
		/// <summary>Position of this point</summary>
		public Vector3 point;
		public string styleID;
		/// <summary>The color tint of this point</summary>
		[ShapesColorField(true)] public Color color = Color.white;
		/// <summary>The thickness multiplier for this point</summary>
		public float thickness = 1f;

		public bool Equals(MapPoint other)
    {
        if (other is null)
            return false;

        return this.point == other.point && this.color == other.color
            && this.thickness == other.thickness;
    }

    public override bool Equals(object obj) => Equals(obj as MapPoint);
    public override int GetHashCode() => (id).GetHashCode();

		/// <summary>Creates a polyline point</summary>
		/// <param name="point">The position of this point</param>
		public MapPoint(int id, Vector3 point)
		{
			this.id = id;
			this.point = point;
			this.color = Color.white;
			this.thickness = 1f;
		}

		/// <summary>Creates a polyline point</summary>
		/// <param name="point">The position of this point</param>
		public MapPoint(int id,  Vector2 point ) {
			this.id = id;
			this.point = point;
			this.color = Color.white;
			this.thickness = 1;
		}

		/// <summary>Creates a polyline point</summary>
		/// <param name="point">The position of this point</param>
		/// <param name="color">The color of this point</param>
		public MapPoint(int id,  Vector3 point, Color color ) {
			this.id = id;
			this.point = point;
			this.color = color;
			this.thickness = 1;
		}

		/// <summary>Creates a polyline point</summary>
		/// <param name="point">The position of this point</param>
		/// <param name="color">The color of this point</param>
		public MapPoint(int id, Vector2 point, Color color ) {
			this.id = id;
			this.point = point;
			this.color = color;
			this.thickness = 1;
		}

		/// <summary>Creates a polyline point</summary>
		/// <param name="point">The position of this point</param>
		/// <param name="color">The color tint of this point</param>
		/// <param name="thickness">The thickness multiplier of this point</param>
		public MapPoint(int id, Vector3 point, Color color, float thickness, string styleID ) {
			this.id = id;
			this.styleID = styleID;
			this.point = point;
			this.color = color;
			this.thickness = thickness;
		}

		/// <summary>Creates a polyline point</summary>
		/// <param name="point">The position of this point</param>
		/// <param name="color">The color tint of this point</param>
		/// <param name="thickness">The thickness multiplier of this point</param>
		public MapPoint(int id, Vector2 point, Color color, float thickness, string styleID ) {
			this.id = id;
			this.styleID = styleID;
			this.point = point;
			this.color = color;
			this.thickness = thickness;
		}
	}

}