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

    public void Setup(string routeName, List<PolylinePoint> points, Color color, float thickness)
    {
        this.routeName = routeName;
        gameObject.layer = ROUTE_LINE_LAYER;

        polyline.SetPoints(points);
        SetLineStyle(color, thickness);

        Mesh mesh = new Mesh();
        ShapesMeshGen.GenPolylineMeshWithThickness(mesh, points, false, PolylineJoins.Simple, true, false, thickness);
        meshCollider.sharedMesh = mesh;

    }

    public void SetLineStyle(Color color, float thickness)
    {
        polyline.Color = color;
        polyline.Thickness = thickness;
    }
}
