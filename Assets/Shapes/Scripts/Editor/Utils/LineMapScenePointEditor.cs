using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

// Shapes © Freya Holmér - https://twitter.com/FreyaHolmer/
// Website & Documentation - https://acegikmo.com/shapes/
namespace Shapes
{

	public class LineMapScenePointEditor : SceneEditGizmos
	{
		static bool isEditing;
		private MapPoint selectedMapPoint;
		public bool useFlatThicknessHandles = false;
		public event Action<ShapeRenderer, int> onValuesChanged = delegate { };
		public bool[] colorEnabledArray = null;
		public bool[] positionEnabledArray = null;

		EditMode currentEditMode = EditMode.AddMovePoints;

		private RTree tree;

		void GoToNextEditMode()
		{
			while (true)
			{
				currentEditMode = (EditMode)(((int)currentEditMode + 1) % (int)EditMode.COUNT);
				break;
			}
		}

		enum EditMode
		{
			AddMovePoints,
			RemovePoints,
			ConnectPoints,
			EditPointStyle,
			COUNT
		}

		public LineMapScenePointEditor(Editor parentEditor) => this.parentEditor = parentEditor;

		protected override bool IsEditing
		{
			get => isEditing;
			set => isEditing = value;
		}

		bool TextureButton(Vector3 worldPos, Texture2D tex, float scale, bool fade = true)
		{
			Rect r = new Rect(0, 0, tex.width * scale, tex.height * scale);
			r.center = HandleUtility.WorldToGUIPoint(worldPos);

			Vector2 mousePos = Event.current.mousePosition;

			if (fade)
			{
				float t = Mathf.InverseLerp(200, 64, Vector2.Distance(mousePos, r.center));
				float a = Mathf.Lerp(0.3f, 1f, t);
				GUI.color = new Color(1, 1, 1, a);
			}

			bool pressed = GUI.Button(r, tex, GUIStyle.none);
			if (fade)
				GUI.color = Color.white;
			return pressed;
		}


		bool movingPoint = false;
		public bool DoSceneHandles(LineMap lineMap, MapPointDictionary points, Transform tf, List<MapPointStyle> styles = null)
		{

			MapPoint MakeGridPoint(MapPoint mapPoint, Vector3 offset)
			{
				return new MapPoint(points.GetNextID(), mapPoint.point + offset, mapPoint.color, mapPoint.thickness, mapPoint.styleID);
			}

			CheckForCancelEditAction();
			if (IsHoldingAlt)
				return false;

			bool changed = false;

			Vector3 GetWorldPt(MapPoint mp) => tf.TransformPoint(mp.point);

			if (!isEditing)
			{
				tree = null;
			}
			else
			{
				if (Event.current.isKey && Event.current.keyCode == KeyCode.Tab)
				{
					if (Event.current.type == EventType.KeyDown)
						GoToNextEditMode();
					Event.current.Use();
				}

				// okay this is a bit of a hack but YKNOW it's fine
				// to prevent this from being drawn for every selected object
				if (Selection.gameObjects.Length > 0 && Selection.gameObjects[0] == lineMap.gameObject)
				{
					if (Event.current.type == EventType.MouseMove)
						SceneView.lastActiveSceneView.Repaint();

					Handles.BeginGUI();
					Vector2 mousePos = Event.current.mousePosition;
					Rect r = new Rect(mousePos.x + 32, mousePos.y, Screen.width, 128);


					string label = "Press Tab to cycle modes:";

					void SelectLabel(string str, EditMode mode, bool exists = true)
					{
						if (exists == false)
							return;
						if (mode == currentEditMode)
							label += "\n> " + str;
						else
							label += "\n  " + str;
					}

					SelectLabel("Add + Move Points", EditMode.AddMovePoints);
					SelectLabel("Remove Points", EditMode.RemovePoints);
					SelectLabel("Connect Points", EditMode.ConnectPoints);
					SelectLabel("Edit Point Style", EditMode.EditPointStyle);

					GUI.Label(r, label);
					Handles.EndGUI();
				}

				bool DoAddGridPoint(Vector3 pt, MapPoint newPoint, List<MapPoint> connectedPoints)
				{
					if (TextureButton(pt, UIAssets.Instance.pointEditAdd, 0.5f))
					{
						// add point
						changed = true;
						Undo.RecordObject(lineMap, "add point");
						lineMap.AddPoint(newPoint);
						connectedPoints.ForEach(p => lineMap.AddConnection(newPoint, p));
						tree.Insert(newPoint);
						return true;
					}

					return false;
				}

				Quaternion handleRotation = Tools.pivotRotation == PivotRotation.Global ? Quaternion.identity : tf.rotation;



				if (currentEditMode == EditMode.AddMovePoints)
				{
					void ExtrapolatedAddPoints(MapPoint point, Vector2 offset, ref List<Vector2> usedPoints)
					{
						MapPoint newPtData = MakeGridPoint(point, offset);
						Vector3 ptWorld = tf.TransformPoint(newPtData.point);

						Vector2 ptScreen = Camera.current.WorldToScreenPoint(ptWorld);
						if (usedPoints.Any(p => (ptScreen - p).sqrMagnitude < 2500))
						{
							return;
						}
						usedPoints.Add(Camera.current.WorldToScreenPoint(ptWorld));

						Handles.EndGUI();
						Handles.DrawDottedLine(tf.TransformPoint(point.point), ptWorld, 5f);
						Handles.BeginGUI();

						_ = DoAddGridPoint(ptWorld, newPtData, new List<MapPoint>() { point });
					}


					if (tree == null)
					{
						tree = new RTree();
						lineMap.points.GetDictionary().ForEach(kvp => tree.Insert(kvp.Key));
					}

					List<Vector2> usedPoints = new();

					Bounds cameraBounds = new Bounds();
					float z = lineMap.transform.position.z - Camera.current.transform.position.z;
					cameraBounds.SetMinMax(Camera.current.ViewportToWorldPoint(new Vector3(0, 0, 0)), Camera.current.ViewportToWorldPoint(new Vector3(1, 1, Camera.current.farClipPlane)));
					List<MapPoint> pointsInView = tree.Search(cameraBounds);

					for (int i = 0; i < pointsInView.Count; i++)
					{
						MapPoint mp = pointsInView[i];
						Vector3 ptWorld = GetWorldPt(mp);

						if (Camera.current != null)
						{
							Vector2 ptScreen = Camera.current.WorldToScreenPoint(ptWorld);
							if (usedPoints.Any(p => (p - ptScreen).sqrMagnitude < 500))
							{
								continue;
							}

							usedPoints.Add(ptScreen);
						}

						Vector3 newPosWorld = Handles.PositionHandle(ptWorld, handleRotation);
						if (GUI.changed)
						{
							changed = true;
							Undo.RecordObject(lineMap, "modify points");
							mp.point = tf.InverseTransformPoint(newPosWorld);
							tree.UpdateBounds(mp);
						}
					}

					Handles.BeginGUI();
					float addDistance = .25f * Mathf.Sqrt(Camera.current.transform.position.z / -1f);
					for (int i = 0; i < pointsInView.Count; i++)
					{
						if (usedPoints.Count > 50)
						{
							continue;
						}

						MapPoint mp = pointsInView[i];
						Vector3 ptWorld = GetWorldPt(mp);

						ExtrapolatedAddPoints(mp, new Vector2(0, addDistance), ref usedPoints);
						ExtrapolatedAddPoints(mp, new Vector2(0, -addDistance), ref usedPoints);
						ExtrapolatedAddPoints(mp, new Vector2(addDistance, 0), ref usedPoints);
						ExtrapolatedAddPoints(mp, new Vector2(-addDistance, 0), ref usedPoints);
					}
					Handles.EndGUI();
				}
				else if (currentEditMode == EditMode.RemovePoints)
				{
					Handles.BeginGUI();
					for (int i = 0; i < points.GetDictionary().Keys.Count; i++)
					{
						MapPoint mp = points.GetDictionary().Keys.ElementAt(i);

						Vector3 ptWorld = GetWorldPt(mp);
						if (TextureButton(ptWorld, UIAssets.Instance.pointEditRemove, 0.5f))
						{
							// delete point
							changed = true;
							Undo.RecordObject(lineMap, "delete point");
							lineMap.RemovePoint(mp);
							tree.Delete(mp);
							break;
						}
					}

					Handles.EndGUI();
				}
				else if (currentEditMode == EditMode.ConnectPoints)
				{
					Handles.BeginGUI();
					for (int i = 0; i < points.GetDictionary().Keys.Count; i++)
					{
						MapPoint mp = points.GetDictionary().Keys.ElementAt(i);

						Vector3 ptWorld = GetWorldPt(mp);

						GUI.color = Color.white;
						if (selectedMapPoint == mp)
						{
							GUI.color = Color.red;
						}

						if (TextureButton(ptWorld, UIAssets.Instance.pointEditColor, 0.5f, fade: false))
						{
							if (selectedMapPoint == null)
							{
								selectedMapPoint = mp;
							}
							else if (selectedMapPoint == mp)
							{
								selectedMapPoint = null;
							}
							else
							{
								// delete point
								changed = true;
								Undo.RecordObject(lineMap, "delete point");
								if (points[selectedMapPoint].Contains(mp))
								{
									lineMap.RemoveConnection(selectedMapPoint, mp);
								}
								else
								{
									lineMap.AddConnection(selectedMapPoint, mp);
								}
								selectedMapPoint = null;
							}
							break;
						}
					}

					Handles.EndGUI();
				}
				else if (currentEditMode == EditMode.EditPointStyle)
				{
					Handles.BeginGUI();
					foreach (MapPoint mp in points.GetDictionary().Keys)
					{
						Vector3 ptWorld = GetWorldPt(mp);

						Color col = mp.color;
						col.a = 1f;
						GUI.color = col;
						if (TextureButton(ptWorld, UIAssets.Instance.pointEditColor, 0.5f, fade: false))
						{
							Rect popupWindowRect = new Rect(0, 0, UIAssets.Instance.pointEditColor.width * .5f, UIAssets.Instance.pointEditColor.height * .5f);
							popupWindowRect.center = HandleUtility.WorldToGUIPoint(ptWorld);
							PopupWindow.Show(popupWindowRect, new MapPointStylePopupWindow(styles, lineMap, mp));
						}
					}

					GUI.color = Color.white;
					Handles.EndGUI();
				}
			}

			return changed;
		}

	}

	public class MapPointStylePopupWindow : PopupWindowContent
	{
		private List<MapPointStyle> styles;
		MapPoint mapPoint;
		LineMap lineMap;
		private int index = 0;

		public MapPointStylePopupWindow(List<MapPointStyle> styles, LineMap lineMap, MapPoint mapPoint) : base()
		{
			this.styles = styles;
			this.lineMap = lineMap;
			this.mapPoint = mapPoint;
		}

		public override Vector2 GetWindowSize()
		{
			return new Vector2(200, 150);
		}

		public override void OnGUI(Rect rect)
		{
			GUILayout.Label("Popup Options Example", EditorStyles.boldLabel);
			index = styles.FindIndex(s => s.id == mapPoint.styleID);
			index = Mathf.Max(0, index);
			index = EditorGUILayout.Popup(index, styles.Select(s => s.id).ToArray());
			if (styles[index].id != mapPoint.styleID)
			{
				Undo.RecordObject( lineMap, "modify style");
				mapPoint.styleID = styles[index].id;
				if (index != 0)
				{
					mapPoint.thickness = styles[index].thickness;
					mapPoint.color = styles[index].color;
				}
				(lineMap as ShapeRenderer)?.UpdateAllMaterialProperties();
				(lineMap as ShapeRenderer)?.UpdateMesh(force: true);
				ShapesUI.RepaintAllSceneViews();
			}
		}
	}

	public class RTree
	{
		private class Node
		{
			public Node Parent; // Reference to the parent node
			public List<MapPoint> Points; // Only for leaf nodes
			public List<Node> Children; // Only for branch nodes
			public Bounds Bounds;

			public bool IsLeaf => Children.Count == 0;

			public Node()
			{
				Points = new List<MapPoint>();
				Children = new List<Node>();
				Bounds = new Bounds();
			}
		}

		private Node root;
		private const int MaxLeafSize = 10;
		private const int MaxBranchSize = 2;

		public RTree()
		{
			root = new Node();
		}

		public void Insert(MapPoint point)
		{
			Insert(point, root);
		}

		private void Insert(MapPoint point, Node node)
		{
			if (node.IsLeaf)
			{
				if (node.Points.Count < MaxLeafSize)
				{
					node.Points.Add(point);
					UpdateBounds(node, true);
				}
				else
				{
					SplitLeafNode(node, point);
				}
			}
			else
			{
				Node bestChild = ChooseBestChild(node, point);
				Insert(point, bestChild);

				if (node.Children.Count > MaxBranchSize)
				{
					SplitBranchNode(node);
				}
			}
		}

		private void SplitLeafNode(Node node, MapPoint point)
		{
			Node newNode = new Node();
			node.Points.Add(point);

			// Distribute points between the two nodes
			List<MapPoint> allPoints = new List<MapPoint>(node.Points);
			node.Points.Clear();

			for (int i = 0; i < allPoints.Count; i++)
			{
				if (i < allPoints.Count / 2)
				{
					node.Points.Add(allPoints[i]);
				}
				else
				{
					newNode.Points.Add(allPoints[i]);
				}
			}

			UpdateBounds(node);
			UpdateBounds(newNode);

			if (node == root)
			{
				// Create a new root
				Node newRoot = new Node();
				newRoot.Children.Add(node);
				newRoot.Children.Add(newNode);

				// Set parent references
				node.Parent = newRoot;
				newNode.Parent = newRoot;

				root = newRoot;
				UpdateBounds(root);
			}
			else
			{
				// Add the new node to the parent
				if (node.Parent == null)
				{
					throw new InvalidOperationException("Parent node reference is required for non-root splits.");
				}

				node.Parent.Children.Add(newNode);
				newNode.Parent = node.Parent;

				// Check if the parent now exceeds the maximum number of children
				if (node.Parent.Children.Count > MaxBranchSize)
				{
					SplitBranchNode(node.Parent);
				}

				UpdateBounds(node.Parent, true);
			}
		}

		private void SplitBranchNode(Node node)
		{
			Node newNode = new Node();

			// Distribute children between the two nodes
			List<Node> allChildren = new List<Node>(node.Children);
			node.Children.Clear();

			for (int i = 0; i < allChildren.Count; i++)
			{
				if (i < allChildren.Count / 2)
				{
					node.Children.Add(allChildren[i]);
				}
				else
				{
					newNode.Children.Add(allChildren[i]);
				}
			}

			// Update parent references for the new children
			foreach (Node child in newNode.Children)
			{
				child.Parent = newNode;
			}

			UpdateBounds(node);
			UpdateBounds(newNode);

			if (node == root)
			{
				// Create a new root
				Node newRoot = new Node();
				newRoot.Children.Add(node);
				newRoot.Children.Add(newNode);

				// Set parent references
				node.Parent = newRoot;
				newNode.Parent = newRoot;

				root = newRoot;
				UpdateBounds(root);
			}
			else
			{
				// Add the new node to the parent
				if (node.Parent == null)
				{
					throw new InvalidOperationException("Parent node reference is required for non-root splits.");
				}

				node.Parent.Children.Add(newNode);
				newNode.Parent = node.Parent;

				// Check if the parent now exceeds the maximum number of children
				if (node.Parent.Children.Count > MaxBranchSize)
				{
					SplitBranchNode(node.Parent);
				}

				UpdateBounds(node.Parent, true);
			}
		}

		private Node ChooseBestChild(Node node, MapPoint point)
		{
			Node bestChild = null;
			float bestAreaIncrease = float.MaxValue;

			foreach (Node child in node.Children)
			{
				Bounds tempBounds = child.Bounds;
				tempBounds.Encapsulate(point.point);
				float areaIncrease = (tempBounds.size.x * tempBounds.size.y) - (child.Bounds.size.x * child.Bounds.size.y);

				if (areaIncrease < bestAreaIncrease)
				{
					bestAreaIncrease = areaIncrease;
					bestChild = child;
				}
			}

			return bestChild;
		}

		public void UpdateBounds(MapPoint point)
		{
			UpdateBounds(point, root);
		}

		private bool UpdateBounds(MapPoint point, Node node)
		{
			// Check if the node's bounds contain the point
			if (!node.Bounds.Contains(point.point))
			{
				return false;
			}

			// If the node is a leaf, update its bounds
			if (node.IsLeaf)
			{
				node.Bounds = new Bounds(node.Points[0].point, Vector3.zero);
				foreach (MapPoint p in node.Points)
				{
					node.Bounds.Encapsulate(p.point);
				}
				return true;
			}

			// If the node is not a leaf, recursively update its children
			bool updated = false;
			foreach (Node child in node.Children)
			{
				if (UpdateBounds(point, child))
				{
					updated = true;
				}
			}

			// Update the current node's bounds if any child was updated
			if (updated)
			{
				node.Bounds = new Bounds();
				foreach (Node child in node.Children)
				{
					node.Bounds.Encapsulate(child.Bounds.min);
					node.Bounds.Encapsulate(child.Bounds.max);
				}
			}

			return updated;
		}

		public List<MapPoint> Search(Bounds area)
		{
			List<MapPoint> results = new List<MapPoint>();
			Search(area, root, results);
			return results;
		}

		private void Search(Bounds area, Node node, List<MapPoint> results)
		{
			// Check if the node's bounds intersect with the search area
			if (!node.Bounds.Intersects(area))
			{
				return;
			}

			if (node.IsLeaf)
			{
				// If it's a leaf node, check each point
				foreach (MapPoint point in node.Points)
				{
					if (area.Contains(point.point))
					{
						results.Add(point);
					}
				}
			}
			else
			{
				// If it's a branch node, recursively search its children
				foreach (Node child in node.Children)
				{
					Search(area, child, results);
				}
			}
		}

		private void UpdateBounds(Node node, bool updateRecursively = false)
		{
			if (node.IsLeaf)
			{
				// If it's a leaf node, calculate bounds based on its points
				if (node.Points.Count > 0)
				{
					node.Bounds = new Bounds(node.Points[0].point, Vector3.zero);
					foreach (MapPoint point in node.Points)
					{
						node.Bounds.Encapsulate(point.point);
					}
				}
				else
				{
					node.Bounds = new Bounds(); // Empty bounds
				}
			}
			else
			{
				// If it's a branch node, calculate bounds based on its children
				if (node.Children.Count > 0)
				{
					node.Bounds = new Bounds(node.Children[0].Bounds.min, Vector3.zero);
					foreach (Node child in node.Children)
					{
						node.Bounds.Encapsulate(child.Bounds.min);
						node.Bounds.Encapsulate(child.Bounds.max);
					}
				}
				else
				{
					node.Bounds = new Bounds(); // Empty bounds
				}
			}

			if (node != root && updateRecursively)
			{
				UpdateBounds(node.Parent, true);
			}
		}
		
		public void Delete(MapPoint point)
		{
			Delete(point, root);
		}
		
		private bool Delete(MapPoint point, Node node)
		{
			// If the node is a leaf, try to remove the point
			if (node.IsLeaf)
			{
				if (node.Points.Remove(point))
				{
					UpdateBounds(node, true);
					return true;
				}
				return false;
			}
		
			// If the node is not a leaf, search its children
			foreach (Node child in node.Children)
			{
				if (child.Bounds.Contains(point.point))
				{
					if (Delete(point, child))
					{
						// If the child becomes empty, remove it
						if (child.IsLeaf && child.Points.Count == 0 || !child.IsLeaf && child.Children.Count == 0)
						{
							node.Children.Remove(child);
						}
		
						// Update bounds and check if the parent needs rebalancing
						UpdateBounds(node, true);
						return true;
					}
				}
			}
		
			return false;
		}
    }
}
