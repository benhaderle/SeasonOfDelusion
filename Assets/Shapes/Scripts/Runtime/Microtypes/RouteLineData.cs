using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Shapes
{
    public class RouteLineData : ScriptableObject
    {
        public static readonly float UNITY_UNITS_TO_MILES = .1f;
        public static readonly float ELVEVATION_SCALE = 1156.46f * 3.281f;
        public static readonly float ELVEVATION_BASE = 0 * 3.281f;

        [SerializeField] private float length;
        public float Length => length;
        public List<int> pointIDs = new();
        [SerializeField] private AnimationCurve elevationCurve;
        public AnimationCurve ElevationCurve => elevationCurve;

        public void SetLength(List<PolylinePoint> points)
        {
            length = 0;
            for (int i = 0; i < points.Count - 1; i++)
            {
                length += Vector3.Distance(points[i].point, points[i + 1].point);
            }

            length *= UNITY_UNITS_TO_MILES;
        }

        public void SetElevationCurve(List<PolylinePoint> points)
        {
            elevationCurve = new AnimationCurve();

            float totalClimbing = 0;

            int nextPointIndex = 1;
            Vector3 currentLocation = points[0].point;

            float stepAmountInMiles = .1f;

            int numIterations = (int)(length / stepAmountInMiles);
            for (int i = 0; i < numIterations; i++)
            {
                float stepInUnits = stepAmountInMiles / UNITY_UNITS_TO_MILES;
                while (nextPointIndex < points.Count && Vector3.Distance(currentLocation, points[nextPointIndex].point) < stepInUnits)
                {
                    stepInUnits -= Vector3.Distance(currentLocation, points[nextPointIndex].point);
                    currentLocation = points[nextPointIndex].point;

                    nextPointIndex++;
                }

                currentLocation += (points[nextPointIndex].point - currentLocation).normalized * stepInUnits;

                elevationCurve.AddKey((float)i / numIterations, GetElevationAtPoint(currentLocation));
                totalClimbing += Mathf.Max(0, elevationCurve[elevationCurve.length - 1].value - elevationCurve[Mathf.Max(elevationCurve.length - 2, 0)].value);
            }

            Debug.Log($"{name} - {totalClimbing}ft");
        }

        private float GetElevationAtPoint(Vector3 worldPoint)
        {
            float elevation = 0;

            if (Physics.Raycast(worldPoint, Vector3.forward, out RaycastHit hit, LayerMask.GetMask("Elevation"), 1000))
            {
                Texture2D texture = (Texture2D)hit.collider.GetComponent<MeshRenderer>().material.mainTexture;
                Vector2 uv = hit.textureCoord;
                uv.x *= texture.width;
                uv.y *= texture.height;

                elevation = texture.GetPixel((int)uv.x, (int)uv.y).r * ELVEVATION_SCALE + ELVEVATION_BASE;
            }

            return elevation;
        }
    }
}