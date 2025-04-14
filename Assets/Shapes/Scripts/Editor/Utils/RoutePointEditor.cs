using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Shapes © Freya Holmér - https://twitter.com/FreyaHolmer/
// Website & Documentation - https://acegikmo.com/shapes/
namespace Shapes
{
	public class RoutePointEditor : SceneEditGizmos
	{
		static bool isEditing;

		private MapPoint selectedPoint;

		EditMode currentEditMode = EditMode.AddPoints;

		RTree tree;

		private void GoToNextEditMode()
		{
			currentEditMode = (EditMode)(((int)currentEditMode + 1) % (int)EditMode.COUNT);
		}

		enum EditMode
		{
			AddPoints,
			GetPointID,
			COUNT
		}

		public RoutePointEditor(Editor parentEditor) => this.parentEditor = parentEditor;

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

		public RouteLineData DoSceneHandles(LineMap lineMap, RouteLineData routeLineData, MapPointDictionary points, Polyline routePolyline)
		{
			CheckForCancelEditAction();
			if (IsHoldingAlt)
				return routeLineData;

			bool routeChanged = false;

			if (!isEditing)
			{
				routePolyline.enabled = false;
				selectedPoint = null;
				tree = null;
			}
			else
			{
				routePolyline.enabled = true;

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

					SelectLabel("Add/Select/Remove Points", EditMode.AddPoints);
					SelectLabel("Get Point ID", EditMode.GetPointID);

					GUI.Label(r, label);
					Handles.EndGUI();
				}

				if (routeLineData == null)
				{
					routeLineData = ScriptableObject.CreateInstance<RouteLineData>();
					int numRoutes = AssetDatabase.FindAssets("t:RouteLineData", new string[] { "Assets/Data/Routes" }).Length;
					AssetDatabase.CreateAsset(routeLineData, $"Assets/Data/Routes/RouteLineData{numRoutes + 1}.asset");
					routeChanged = true;
				}

				if (routeLineData.pointIDs.Count < 1)
				{
					selectedPoint = null;
				}
				else if ((selectedPoint == null || !routeLineData.pointIDs.Contains(selectedPoint.id)) && routeLineData.pointIDs.Count > 0)
				{
					selectedPoint = points.GetDictionary().Keys.First(m => m.id == routeLineData.pointIDs[routeLineData.pointIDs.Count - 1]);
				}

				if (tree == null)
				{
					tree = new RTree();
					lineMap.points.GetDictionary().ForEach(kvp => tree.Insert(kvp.Key));
					routeChanged = true;
				}

				Bounds cameraBounds = new Bounds();
				float z = lineMap.transform.position.z - Camera.current.transform.position.z;
				cameraBounds.SetMinMax(Camera.current.ViewportToWorldPoint(new Vector3(0, 0, 0)), Camera.current.ViewportToWorldPoint(new Vector3(1, 1, Camera.current.farClipPlane)));
				List<MapPoint> pointsInView = tree.Search(cameraBounds);

				if (currentEditMode == EditMode.AddPoints)
				{
					Handles.BeginGUI();
					bool selectPointButton = Event.current.command;
					bool removePointButton = Event.current.control;
					for (int i = 0; i < pointsInView.Count; i++)
					{
						MapPoint mp = pointsInView[i];
						if ((selectPointButton || removePointButton) && !routeLineData.pointIDs.Contains(mp.id))
						{
							continue;
						}

						Vector3 ptWorld = lineMap.transform.TransformPoint(mp.point);

						GUI.color = selectedPoint == mp ? Color.red : Color.white;

						if (TextureButton(ptWorld, UIAssets.Instance.pointEditColor, 0.5f, fade: false))
						{
							if (selectPointButton)
							{
								if (selectedPoint != mp)
								{
									selectedPoint = mp;
								}
								else
								{
									selectedPoint = points.GetDictionary().Keys.First(m => m.id == routeLineData.pointIDs[routeLineData.pointIDs.Count - 1]);
								}
							}
							else if (removePointButton)
							{
								Undo.RecordObjects(new Object[] { routeLineData }, "remove point");
								routeLineData.pointIDs.Remove(mp.id);
								EditorUtility.SetDirty(routeLineData);
								AssetDatabase.SaveAssets();
								routeChanged = true;
							}
							else
							{
								Undo.RecordObjects(new Object[] { routeLineData }, "add point");
								int insertIndex = routeLineData.pointIDs.Count > 0 ? routeLineData.pointIDs.FindIndex(id => id == selectedPoint.id) + 1 : 0;
								routeLineData.pointIDs.Insert(insertIndex, mp.id);
								selectedPoint = mp;
								EditorUtility.SetDirty(routeLineData);
								AssetDatabase.SaveAssets();
								routeChanged = true;
							}
						}
					}

					Handles.EndGUI();
				}
				else if (currentEditMode == EditMode.GetPointID)
				{
					Handles.BeginGUI();
					for (int i = 0; i < pointsInView.Count; i++)
					{
						MapPoint mp = pointsInView[i];
						if (!routeLineData.pointIDs.Contains(mp.id))
						{
							continue;
						}

						Vector3 ptWorld = lineMap.transform.TransformPoint(mp.point);
						Vector2 ptScreen = HandleUtility.WorldToGUIPoint(ptWorld);

						GUI.Label(new Rect(ptScreen.x + 10, ptScreen.y - 10, 100, 20), mp.id.ToString());

						if (TextureButton(ptWorld, UIAssets.Instance.pointEditColor, 0.5f, fade: false))
						{
							EditorGUIUtility.systemCopyBuffer = mp.id.ToString();
							Debug.Log($"Copied point ID {mp.id} to clipboard");
						}
					}

					Handles.EndGUI();
				}

				if (routeLineData.pointIDs.Count < 2)
					{
						routePolyline.Mesh.Clear();
					}
					else if (routeChanged)
					{
						routePolyline.SetPoints(lineMap.GetMapPointsFromIDs(routeLineData.pointIDs).Select(mp => new PolylinePoint() { point = mp.point, color = Color.white, thickness = mp.thickness }).ToArray());
						routeLineData.SetLength(routePolyline.points);
					}
			}

			return routeLineData;
		}

	}
}