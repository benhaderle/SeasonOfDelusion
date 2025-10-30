using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CreateNeptune;
using Shapes;
using UnityEngine;

public class ElevationGraphView : MonoBehaviour
{
    [SerializeField] private Polyline elevationLine;
    [SerializeField] private float maxElevation;
    [SerializeField] private PoolContext runnerMarkerPoolContext;
    [SerializeField] private float runnerMarkerLineOffset = 50f;
    private Dictionary<string, GameObject> activeRunnerMarkerDictionary = new();
    private Rect elevationLineRect;

    private void Awake()
    {
        runnerMarkerPoolContext.Initialize();
        elevationLine.gameObject.SetActive(false);
    }

    public void SetElevationLine(AnimationCurve elevationCurve)
    {
        elevationLineRect = elevationLine.GetComponent<RectTransform>().rect;

        elevationLine.points.Clear();
        for (int i = 0; i < elevationCurve.length; i++)
        {
            Vector3 position = new();
            position.x = elevationCurve[i].time * elevationLineRect.width;
            position.y = Mathf.InverseLerp(0, maxElevation, elevationCurve[i].value) * elevationLineRect.height;

            elevationLine.AddPoint(position);
        }
        elevationLine.gameObject.SetActive(true);
    }

    public void InitializeRunnerMarkers(List<Runner> runners)
    {
        for (int i = 0; i < runners.Count; i++)
        {
            RectTransform runnerMarker = runnerMarkerPoolContext.GetPooledObject<RectTransform>();
            runnerMarker.anchorMax = Vector2.zero;
            runnerMarker.anchorMin = Vector2.zero;
            runnerMarker.anchoredPosition = GetCanvasPositionAlongLine(0);
            activeRunnerMarkerDictionary.Add(runners[i].Initials, runnerMarker.gameObject);
        }
    }

    public void CleanUp()
    {
        runnerMarkerPoolContext.ReturnAllToPool();
        activeRunnerMarkerDictionary.Clear();
        elevationLine.gameObject.SetActive(false);
    }

    public void UpdateRunners(ReadOnlyDictionary<Runner, RunnerState> runnerStateDictionary)
    {
        foreach (KeyValuePair<Runner, RunnerState> kvp in runnerStateDictionary)
        {
            GameObject runnerMarker = activeRunnerMarkerDictionary[kvp.Key.Initials];

            runnerMarker.GetComponent<RectTransform>().anchoredPosition = GetCanvasPositionAlongLine(kvp.Value.totalPercentDone);
        }
    }

    private Vector3 GetCanvasPositionAlongLine(float normalizedPosition)
    {
        Vector3 arrowPosition = elevationLine.points[elevationLine.points.Count - 1].point;;
        float normalizedSegmentEnd = 0;

        for (int i = 0; i < elevationLine.points.Count - 1; i++)
        {
            float normalizedSegmentStart = normalizedSegmentEnd;
            normalizedSegmentEnd = normalizedSegmentStart + (elevationLine.points[i + 1].point.x - elevationLine.points[i].point.x) / elevationLineRect.width;

            if (normalizedPosition >= normalizedSegmentStart && normalizedPosition <= normalizedSegmentEnd)
            {
                float t = Mathf.InverseLerp(normalizedSegmentStart, normalizedSegmentEnd, normalizedPosition);
                arrowPosition = Vector3.Lerp(elevationLine.points[i].point, elevationLine.points[i + 1].point, t);
                break;
            }
        }

        arrowPosition += new Vector3(0, runnerMarkerLineOffset, 0);

        return arrowPosition;
    }
}
