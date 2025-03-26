using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CreateNeptune;
using System;
using Shapes;
using System.Linq;

public class RoadModel : Singleton<RoadModel>
{
    [SerializeField] private List<Road> roads;

    public PolylinePoint GetPoint(string roadName, int pointIndex)
    {
        PolylinePoint point = new();

        Road road = roads.First(r => r.name == roadName);
        if (road != null && pointIndex < road.polyline.points.Count)
        {
            point = road.polyline.points[pointIndex];
        }

        return point;
    }

    public List<PolylinePoint> GetSegment(string roadName, int startPointIndex, int endPointIndex)
    {
        List<PolylinePoint> points = new();

        Road road = roads.First(r => r.name == roadName);
        startPointIndex = Mathf.Clamp(startPointIndex, 0, road.polyline.points.Count - 1);
        endPointIndex = Mathf.Clamp(endPointIndex, 0, road.polyline.points.Count - 1);

        bool reverseOrder = endPointIndex < startPointIndex;
        if (reverseOrder)
        {
            int indexHolder = startPointIndex;
            startPointIndex = endPointIndex;
            endPointIndex = indexHolder;
        }

        if (road != null)
        {
            points = road.polyline.points.GetRange(startPointIndex, endPointIndex - startPointIndex);
        }

        if (reverseOrder)
        {
            points.Reverse();
        }

        return points;
    }
}

[Serializable]
public class Road
{
    public string name;
    public Polyline polyline;
}
