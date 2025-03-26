using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Shapes;
using System;
using System.Linq;

public class RouteLine : MonoBehaviour
{
    [SerializeField] private Polyline polyline;
    [SerializeField] private List<RouteLineSegment> routeLineSegments;

    private void Start()
    {
        routeLineSegments.ForEach(rls => polyline.AddPoints(RoadModel.Instance.GetSegment(rls.roadName, rls.startRoadPointIndex, rls.endRoadPointIndex)));
        for (int i = 0; i < polyline.points.Count - 1; i++)
        {
            if ((polyline.points[i].point - polyline.points[i + 1].point).sqrMagnitude < .01f)
            {
                polyline.points.RemoveAt(i);
                i--;
            }
        }
    }
}

[Serializable]
public struct RouteLinePoint
{
    public string roadName;
    public int roadPointIndex;
}

[Serializable]
public struct RouteLineSegment
{
    public string roadName;
    public int startRoadPointIndex;
    public int endRoadPointIndex;
}
