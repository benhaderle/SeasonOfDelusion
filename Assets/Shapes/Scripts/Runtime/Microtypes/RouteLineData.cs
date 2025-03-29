using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Shapes;
using System;
using System.Linq;

namespace Shapes
{
    public class RouteLineData : ScriptableObject
    {
        [SerializeField] private string displayName;
        private string mapName;
        public List<int> pointIDs = new();
    }
}