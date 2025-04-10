using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Shapes;
using UnityEngine;

public class RouteLine : MonoBehaviour
{
    private static readonly int ROUTE_LINE_LAYER = 8;
    [SerializeField] private Polyline polyline;
    private List<int> mapPointIDs = new();
    public Polyline Polyline => polyline;
    private string routeName;
    public string RouteName => routeName;
    [SerializeField] private MeshCollider meshCollider;
    private float length;

    public void Setup(string routeName, List<MapPoint> points, Color color, float thickness)
    {
        this.routeName = routeName;
        gameObject.layer = ROUTE_LINE_LAYER;
        
        mapPointIDs = points.Select(mp => mp.id).ToList();

        PolylinePoint[] polylinePoints = points.Select(mp => new PolylinePoint() { point = mp.point, color = Color.white, thickness = mp.thickness }).ToArray();
        polyline.SetPoints(polylinePoints);
        SetLineStyle(color, thickness, 0);

        Mesh mesh = new Mesh();
        ShapesMeshGen.GenPolylineMeshWithThickness(mesh, polylinePoints, false, PolylineJoins.Simple, true, false, thickness);
        meshCollider.sharedMesh = mesh;
    }

    public void SetLineStyle(Color color, float thickness, int sortingOrder)
    {
        polyline.Color = color;
        polyline.Thickness = thickness;
        polyline.SortingOrder = sortingOrder;
    }

    public Vector3 GetPositionAlongRoute(float normalizedPosition, out int closestPointID)
    {
        //if we haven't calculated the length of the polyline yet, calculate it and cache it now
        if (length <= 0)
        {
            for (int i = 0; i < polyline.points.Count - 1; i++)
            {
                length += Vector3.Distance(polyline.points[i].point, polyline.points[i + 1].point);
            }
        }

        float normalizedSegmentEnd = 0;

        for (int i = 0; i < polyline.points.Count - 1; i++)
        {
            float normalizedSegmentStart = normalizedSegmentEnd;
            normalizedSegmentEnd = normalizedSegmentStart + Vector3.Distance(polyline.points[i].point, polyline.points[i + 1].point) / length;

            if (normalizedPosition >= normalizedSegmentStart && normalizedPosition <= normalizedSegmentEnd)
            {
                float t = Mathf.InverseLerp(normalizedSegmentStart, normalizedSegmentEnd, normalizedPosition);
                closestPointID = t < .5f ? mapPointIDs[i] : mapPointIDs[i + 1];
                return Vector3.Lerp(polyline.points[i].point, polyline.points[i + 1].point, t);
            }
        }

        closestPointID = mapPointIDs[mapPointIDs.Count - 1];
        return polyline.points[polyline.points.Count - 1].point;
    }
}
