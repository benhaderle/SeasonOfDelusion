using System.Collections;
using System.Collections.Generic;
using Shapes;
using UnityEngine;

public class RouteLine : MonoBehaviour
{
    private static readonly int ROUTE_LINE_LAYER = 8;
    [SerializeField] private Polyline polyline;
    public Polyline Polyline => polyline;
    private string routeName;
    public string RouteName => routeName;
    [SerializeField] private MeshCollider meshCollider;
    private float length;

    public void Setup(string routeName, List<PolylinePoint> points, Color color, float thickness)
    {
        this.routeName = routeName;
        gameObject.layer = ROUTE_LINE_LAYER;

        polyline.SetPoints(points);
        SetLineStyle(color, thickness, 0);

        Mesh mesh = new Mesh();
        ShapesMeshGen.GenPolylineMeshWithThickness(mesh, points, false, PolylineJoins.Simple, true, false, thickness);
        meshCollider.sharedMesh = mesh;
    }

    public void SetLineStyle(Color color, float thickness, int sortingOrder)
    {
        polyline.Color = color;
        polyline.Thickness = thickness;
        polyline.SortingOrder = sortingOrder;
    }

    public Vector3 GetPositionAlongRoute(float normalizedPosition)
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
                return Vector3.Lerp(polyline.points[i].point, polyline.points[i + 1].point, Mathf.InverseLerp(normalizedSegmentStart, normalizedSegmentEnd, normalizedPosition));
            }
        }

        return polyline.points[polyline.points.Count - 1].point;
    }
}
