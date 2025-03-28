using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

// Shapes © Freya Holmér - https://twitter.com/FreyaHolmer/
// Website & Documentation - https://acegikmo.com/shapes/
namespace Shapes
{
	/// <summary>A LineMap shape component</summary>
	[ExecuteAlways]
	[AddComponentMenu("Shapes/LineMap")]
	public class LineMap : ShapeRenderer
	{
		/// <summary>IMPORTANT: if you modify this list, you need to set meshOutOfDate to true, otherwise your changes won't apply</summary>
		// [SerializeField] public List<MapPoint> points = new();
		[SerializeField] public List<MapPointStyle> pointStyles = new();
		[SerializeField, SerializeReference] public MapPointDictionary points = new();

		// also called alignment
		[SerializeField] PolylineGeometry geometry = PolylineGeometry.Flat2D;
		/// <summary>Get or set the geometry type to use for this polyline</summary>
		public PolylineGeometry Geometry
		{
			get => geometry;
			set
			{
				geometry = value;
				SetIntNow(ShapesMaterialUtils.propAlignment, (int)geometry);
				UpdateMaterial();
				ApplyProperties();
			}
		}

		[SerializeField] PolylineJoins joins = PolylineJoins.Miter;
		/// <summary>The type of joins to use in all corners of this polyline</summary>
		public PolylineJoins Joins
		{
			get => joins;
			set
			{
				joins = value;
				meshOutOfDate = true;
				UpdateMaterial();
			}
		}

		[SerializeField] bool closed = true;
		/// <summary>Whether or not this polyline should form a closed loop</summary>
		public bool Closed
		{
			get => closed;
			set
			{
				closed = value;
				meshOutOfDate = true;
			}
		}

		[SerializeField] float thickness = 0.125f;
		/// <summary>The thickness of this polyline in the given thickness space</summary>
		public float Thickness
		{
			get => thickness;
			set => SetFloatNow(ShapesMaterialUtils.propThickness, thickness = value);
		}

		[SerializeField] ThicknessSpace thicknessSpace = Shapes.ThicknessSpace.Meters;
		/// <summary>The space in which Thickness is defined</summary>
		public ThicknessSpace ThicknessSpace
		{
			get => thicknessSpace;
			set => SetIntNow(ShapesMaterialUtils.propThicknessSpace, (int)(thicknessSpace = value));
		}

		/// <summary>The number of points in this polyline</summary>
		public int Count => points.Count;

		/// <summary>Adds a point to this polyline</summary>
		public void AddPoint(MapPoint point)
		{
			points.Add(point, new List<MapPoint>());

			meshOutOfDate = true;
		}

		public void RemovePoint(MapPoint point)
		{
			List<MapPoint> neighbors = points[point].Select(p => p).ToList();
			foreach (MapPoint neighbor in neighbors)
			{
				RemoveConnection(point, neighbor);
			}
			points.Remove(point);

			meshOutOfDate = true;
		}

		public void AddConnection(MapPoint p1, MapPoint p2)
		{
			points[p1].Add(p2);
			points[p2].Add(p1);
		}

		public void RemoveConnection(MapPoint p1, MapPoint p2)
		{
			points[p1].Remove(p2);
			points[p2].Remove(p1);
		}

		private protected override bool UseCamOnPreCull => true;

		internal override void CamOnPreCull()
		{
			if (meshOutOfDate)
			{
				meshOutOfDate = false;
				UpdateMesh(force: true);
			}
		}

		private protected override MeshUpdateMode MeshUpdateMode => MeshUpdateMode.SelfGenerated;
		private protected override void GenerateMesh()
		{
			if (Count < 2)
				return;

			List<List<MapPoint>> lines = new();

			Dictionary<MapPoint, List<MapPoint>> neighborsDictionary = new();
			foreach (KeyValuePair<MapPoint, List<MapPoint>> kvp in points.GetDictionary())
			{
				neighborsDictionary.Add(kvp.Key, kvp.Value.Select(p => p).ToList());
				int styleIndex = pointStyles.FindIndex(s => s.id == kvp.Key.styleID);
				if (styleIndex != -1)
				{
					kvp.Key.color = pointStyles[styleIndex].color;
					kvp.Key.thickness = pointStyles[styleIndex].thickness;
				}
			}

			while (neighborsDictionary.Sum(kvp => kvp.Value.Count) > 0)
			{
				MapPoint currentPoint = neighborsDictionary.Where(kvp => kvp.Value.Count > 0).OrderBy(kvp => kvp.Value.Count).ToList()[0].Key;
				MapPoint lastPoint = null;

				List<MapPoint> line = new()
				{
					currentPoint
				};

				while (neighborsDictionary[currentPoint].Count > 0)
				{
					MapPoint nextPoint;
					if (lastPoint == null || neighborsDictionary[currentPoint].Count == 1)
					{
						nextPoint = neighborsDictionary[currentPoint][0];
					}
					else
					{
						Vector2 lastSegment = currentPoint.point - lastPoint.point;
						nextPoint = neighborsDictionary[currentPoint].OrderByDescending(p => Vector2.Dot(lastSegment, p.point - currentPoint.point)).ToList()[0];
					}

					neighborsDictionary[nextPoint].Remove(currentPoint);
					neighborsDictionary[currentPoint].Remove(nextPoint);

					line.Add(nextPoint);
					lastPoint = currentPoint;
					currentPoint = nextPoint;
				}

				lines.Add(line);
			}

			Mesh.Clear();
			Mesh.CombineMeshes(lines.Select(l => new CombineInstance { mesh = ShapesMeshGen.GenLineMapMesh(new Mesh(), l, joins, flattenZ: geometry == PolylineGeometry.Flat2D, useColors: true) }).ToArray(), true, false, false);
		}

		private protected override void SetAllMaterialProperties()
		{
			SetFloat(ShapesMaterialUtils.propThickness, thickness);
			SetInt(ShapesMaterialUtils.propThicknessSpace, (int)thicknessSpace);
			SetInt(ShapesMaterialUtils.propAlignment, (int)geometry);
		}

		private protected override void ShapeClampRanges() => thickness = Mathf.Max(0f, thickness);

		private protected override Material[] GetMaterials()
		{
			if (joins.HasJoinMesh())
				return new[] { ShapesMaterialUtils.GetPolylineMat(joins)[BlendMode], ShapesMaterialUtils.GetPolylineJoinsMat(joins)[BlendMode] };
			return new[] { ShapesMaterialUtils.GetPolylineMat(joins)[BlendMode] };
		}

		// todo: this doesn't take point thickness or thickness space into account
		private protected override Bounds GetBounds_Internal()
		{
			if (Count < 2)
				return default;
			Vector3 min = Vector3.one * float.MaxValue;
			Vector3 max = Vector3.one * float.MinValue;
			foreach (Vector3 pt in points.GetDictionary().Select(kvp => kvp.Key.point))
			{
				min = Vector3.Min(min, pt);
				max = Vector3.Max(max, pt);
			}

			if (geometry == PolylineGeometry.Flat2D)
				min.z = max.z = 0;

			return new Bounds((max + min) * 0.5f, (max - min) + Vector3.one * (thickness * 0.5f));
		}

	}

	[Serializable]
	public class AdjacencyList<K>
	{
		private int nextID;
		public int GetNextID()
		{
			int id = nextID;
			nextID++;
			return id;
		}

		public ReadOnlyDictionary<K, List<K>> VertexDict => new ReadOnlyDictionary<K, List<K>>(_vertexDict);
		private SerializableDictionary<K, List<K>> _vertexDict = new SerializableDictionary<K, List<K>>();

		public AdjacencyList(K origin)
		{
			nextID = 1;
			AddVertex(origin);
		}

		public List<K> AddVertex(K key)
		{
			List<K> vertex = new List<K>();
			_vertexDict.Add(key, vertex);

			return vertex;
		}


		public void AddEdge(K startKey, K endKey)
		{
			List<K> startVertex = _vertexDict.ContainsKey(startKey) ? _vertexDict[startKey] : null;
			List<K> endVertex = _vertexDict.ContainsKey(endKey) ? _vertexDict[endKey] : null;

			if (startVertex == null)
				throw new ArgumentException("Cannot create edge from a non-existent start vertex.");

			if (endVertex == null)
				endVertex = AddVertex(endKey);

			startVertex.Add(endKey);
			endVertex.Add(startKey);
		}

		public void RemoveVertex(K key)
		{
			List<K> vertex = _vertexDict[key];

			//First remove the edges / adjacency entries
			int vertexNumAdjacent = vertex.Count;
			for (int i = 0; i < vertexNumAdjacent; i++)
			{
				K neighbourVertexKey = vertex[i];
				RemoveEdge(key, neighbourVertexKey);
			}

			//Lastly remove the vertex / adj. list
			_vertexDict.Remove(key);
		}

		public void RemoveEdge(K startKey, K endKey)
		{
			((List<K>)_vertexDict[startKey]).Remove(endKey);
			((List<K>)_vertexDict[endKey]).Remove(startKey);
		}

		public bool Contains(K key)
		{
			return _vertexDict.ContainsKey(key);
		}

		public int VertexDegree(K key)
		{
			return _vertexDict[key].Count;
		}

		public int NumVertices()
		{
			return _vertexDict.Count;
		}

	}

	[Serializable]
	public class MapPointDictionary : ISerializationCallbackReceiver
	{
		[Serializable]
		private class MapPointEntry
		{
			[SerializeReference] public MapPoint mapPoint;
			[SerializeReference] public List<MapPoint> neighbors;
		}

		private Dictionary<MapPoint, List<MapPoint>> dictionary = new();
		[SerializeField] private List<MapPointEntry> serializedList = new();

		[SerializeField, HideInInspector] private int nextID = 1;
		public MapPointDictionary()
		{
			dictionary.Add(new MapPoint(0, Vector2.zero, Color.white, 1f), new List<MapPoint>());
		}

		public void OnAfterDeserialize()
		{
			if (serializedList != null && serializedList.Count > 0)
			{
				dictionary.Clear();
				foreach (MapPointEntry mpe in serializedList)
				{
					dictionary.Add(mpe.mapPoint, mpe.neighbors);
				}
				serializedList.Clear();
			}
		}

		public void OnBeforeSerialize()
		{
			serializedList.Clear();
			dictionary.ForEach(kvp => serializedList.Add(new MapPointEntry { mapPoint = kvp.Key, neighbors = kvp.Value }));
		}

		public int GetNextID()
		{
			int id = nextID;
			nextID++;
			return id;
		}

		public List<MapPoint> this[MapPoint mp]
		{
			get => dictionary[mp];
			set => dictionary[mp] = value;
		}

		public void Add(MapPoint mp, List<MapPoint> ns)
		{
			dictionary.Add(mp, ns);
		}
		public void Remove(MapPoint mp)
		{
			dictionary.Remove(mp);
		}
		public Dictionary<MapPoint, List<MapPoint>> GetDictionary()
		{
			return dictionary;
		}

		public int Count => dictionary.Count;
	}

	[Serializable]
	public struct MapPointStyle
	{
		public string id;
		public float thickness;
		public Color color;
	}
}