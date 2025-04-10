using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Shapes;
using UnityEngine;

[CreateAssetMenu(fileName = "NewMapSaveData", menuName = "ScriptableObjects/MapSaveData")]
public class MapSaveDataSO : ScriptableObject
{
    private const int START_ID = 3606017;
    public MapSaveData data = new();
    //runtime dictionary for easy acces to savedata
    public Dictionary<int, MapPointSaveData> mapPointDictionary = new();
    /// <summary>
    /// Initializes map save data with the given points. Should be called the first time we load the map scene.
    /// </summary>
    /// <param name="mapPoints"></param>
    public void Load(List<MapPoint> mapPoints)
    {
        List<MapPointSaveData> newMapPointSaveDataList = new();
        for (int i = 0; i < mapPoints.Count; i++)
        {
            MapPointSaveData mapPointSaveData = data.mapPointSaveDataList.FirstOrDefault(mps => mps.id == mapPoints[i].id);
            if (mapPointSaveData == null)
            {
                mapPointSaveData = new MapPointSaveData();
                mapPointSaveData.id = mapPoints[i].id;
                mapPointSaveData.discovered = false;
            }
            mapPointDictionary.Add(mapPoints[i].id, mapPointSaveData);
            newMapPointSaveDataList.Add(mapPointSaveData);
        }
        data.mapPointSaveDataList = newMapPointSaveDataList;

        mapPointDictionary[START_ID].discovered = true;
    }
}

[Serializable]
public class MapSaveData
{
    [HideInInspector] public List<MapPointSaveData> mapPointSaveDataList = new();
}

[Serializable]
public class MapPointSaveData
{
    public int id;
    public bool discovered;
}
