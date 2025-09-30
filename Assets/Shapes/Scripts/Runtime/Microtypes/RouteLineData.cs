using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Shapes
{
    public class RouteLineData : ScriptableObject
    {
        public static readonly float UNITY_UNITS_TO_MILES = .1f;
        [SerializeField] private float length;
        public float Length => length;
        public List<int> pointIDs = new();

        public void SetLength(List<PolylinePoint> points)
        {
            length = 0;
            for (int i = 0; i < points.Count - 1; i++)
            {
                length += Vector3.Distance(points[i].point, points[i+1].point);
            }

            length *= UNITY_UNITS_TO_MILES;
        }
    }
}