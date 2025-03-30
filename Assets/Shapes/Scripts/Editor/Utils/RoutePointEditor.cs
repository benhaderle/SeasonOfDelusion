using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEditor.IMGUI.Controls;
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

		private void GoToNextEditMode()
		{
			currentEditMode = (EditMode)(((int)currentEditMode + 1) % (int)EditMode.COUNT);
		}

		enum EditMode
		{
			AddPoints,
			RemovePoints,
			COUNT
		}

		SphereBoundsHandle discHandle = ShapesHandles.InitDiscHandle();

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

			if (!isEditing)
			{
				routePolyline.enabled = false;
				selectedPoint = null;
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

					SelectLabel("add/remove points", EditMode.AddPoints);

					GUI.Label(r, label);
					Handles.EndGUI();

				}

				if (routeLineData == null)
				{
					routeLineData = ScriptableObject.CreateInstance<RouteLineData>();
					int numRoutes = AssetDatabase.FindAssets("t:RouteLineData", new string[] { "Assets/Data/Routes" }).Length;
					AssetDatabase.CreateAsset(routeLineData, $"Assets/Data/Routes/RouteLineData{numRoutes + 1}.asset");
				}

				if (routeLineData.pointIDs.Count < 1)
				{
					selectedPoint = null;
				}
				else if ((selectedPoint == null || !routeLineData.pointIDs.Contains(selectedPoint.id)) && routeLineData.pointIDs.Count > 0)
				{
					selectedPoint = points.GetDictionary().Keys.First(m => m.id == routeLineData.pointIDs[routeLineData.pointIDs.Count - 1]);
				}

				if (currentEditMode == EditMode.AddPoints)
				{
					Handles.BeginGUI();
					bool selectPointButton = Event.current.command;
					bool removePointButton = Event.current.control;
					for (int i = 0; i < points.GetDictionary().Keys.Count; i++)
					{
						MapPoint mp = points.GetDictionary().Keys.ElementAt(i);
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
							}
							else
							{
								Undo.RecordObjects(new Object[] { routeLineData }, "add point");
								int insertIndex = routeLineData.pointIDs.Count > 0 ? routeLineData.pointIDs.FindIndex(id => id == selectedPoint.id) + 1 : 0;
								routeLineData.pointIDs.Insert(insertIndex, mp.id);
								selectedPoint = mp;
								EditorUtility.SetDirty(routeLineData);
								AssetDatabase.SaveAssets();
							}


							// if (selectedMapPoint == null)
							// {
							// 	selectedMapPoint = mp;
							// }
							// else if (selectedMapPoint == mp)
							// {
							// 	selectedMapPoint = null;
							// }
							// else
							// {
							// 	// delete point
							// 	changed = true;
							// 	Undo.RecordObject(lineMap, "delete point");
							// 	if (points[selectedMapPoint].Contains(mp))
							// 	{
							// 		lineMap.RemoveConnection(selectedMapPoint, mp);
							// 	}
							// 	else
							// 	{
							// 		lineMap.AddConnection(selectedMapPoint, mp);
							// 	}
							// 	selectedMapPoint = null;
							// }
							// break;
						}
					}

					Handles.EndGUI();
				}
				// 	if (currentEditMode == EditMode.EditThickness)
				// 	{
				// 		Transform camTf = SceneView.lastActiveSceneView.camera.transform;
				// 		Vector3 camPos = camTf.position;
				// 		Vector3 camUp = camTf.up;

				// 		// thickness controls
				// 		foreach (MapPoint mp in points.GetDictionary().Keys)
				// 		{
				// 			discHandle.radius = GetThicknessWorld(mp) / 2f;
				// 			discHandle.center = Vector3.zero;
				// 			Vector3 discPos = GetWorldPt(mp);
				// 			Vector3 dirToCamera = discPos - camPos;
				// 			Quaternion discRot = useFlatThicknessHandles ? tf.rotation : Quaternion.LookRotation(dirToCamera, camUp);
				// 			Matrix4x4 mtx = Matrix4x4.TRS(discPos, discRot, Vector3.one);

				// 			using (var chChk = new EditorGUI.ChangeCheckScope())
				// 			{
				// 				using (new Handles.DrawingScope(ShapesHandles.GetHandleColor(GetNetColor(mp)), mtx))
				// 					discHandle.DrawHandle();
				// 				if (chChk.changed)
				// 				{
				// 					changed = true;
				// 					Undo.RecordObject(lineMap, "edit thickness");
				// 					SetThicknessWorld(mp, discHandle.radius * 2);
				// 					break;
				// 				}
				// 			}
				// 		}
				// 	}
				// 	else if (currentEditMode == EditMode.EditColor)
				// 	{
				// 		Handles.BeginGUI();
				// 		foreach (MapPoint mp in points.GetDictionary().Keys)
				// 		{
				// 			Vector3 ptWorld = GetWorldPt(mp);
				// 			// Color newColor = EditorGUI.ColorField( r, GUIContent.none, GetColor( i ), true, true, ShapesConfig.USE_HDR_COLOR_PICKERS );

				// 			Color col = GetColor(mp);
				// 			col.a = 1f;
				// 			GUI.color = col;
				// 			if (TextureButton(ptWorld, UIAssets.Instance.pointEditColor, 0.5f, fade: false))
				// 			{
				// 				ShapesUI.ShowColorPicker(OnColorChanged, GetColor(mp));

				// 				void OnColorChanged(Color c)
				// 				{
				// 					Undo.RecordObject(lineMap, "modify color");
				// 					SetColor(mp, c);
				// 					(lineMap as ShapeRenderer)?.UpdateAllMaterialProperties();
				// 					(lineMap as ShapeRenderer)?.UpdateMesh(force: true);
				// 					ShapesUI.RepaintAllSceneViews();
				// 				}
				// 			}
				// 		}

				// 		GUI.color = Color.white;
				// 		Handles.EndGUI();
				// 	}
				// 	else if (currentEditMode == EditMode.PositionHandles)
				// 	{
				// 		foreach (MapPoint mp in points.GetDictionary().Keys)
				// 		{
				// 			Vector3 ptWorld = GetWorldPt(mp);
				// 			Vector3 newPosWorld = Handles.PositionHandle(ptWorld, handleRotation);
				// 			if (GUI.changed)
				// 			{
				// 				changed = true;
				// 				Undo.RecordObject(lineMap, "modify points");
				// 				mp.point = tf.InverseTransformPoint(newPosWorld);
				// 			}
				// 		}
				// 	}
				// 	else if (currentEditMode == EditMode.AddRemoveGridPoints)
				// 	{
				// 		void ExtrapolatedAddPoints(MapPoint point, Vector2 offset, ref List<Vector2> usedPoints)
				// 		{
				// 			if (usedPoints.Any(p => ((Vector2)point.point + offset - p).sqrMagnitude < .5f))
				// 			{
				// 				return;
				// 			}
				// 			MapPoint newPtData = MakeGridPoint(point, offset);
				// 			Vector3 ptWorld = tf.TransformPoint(newPtData.point);
				// 			usedPoints.Add((Vector2)point.point + offset);

				// 			Handles.EndGUI();
				// 			Handles.DrawDottedLine(tf.TransformPoint(point.point), ptWorld, 5f);
				// 			Handles.BeginGUI();

				// 			_ = DoAddGridPoint(ptWorld, newPtData, new List<MapPoint>() { point });
				// 		}

				// 		Handles.BeginGUI();
				// 		List<Vector2> usedPoints = points.GetDictionary().Keys.Select(p => (Vector2)p.point).ToList();
				// 		for (int i = 0; i < points.GetDictionary().Keys.Count; i++)
				// 		{
				// 			MapPoint mp = points.GetDictionary().Keys.ElementAt(i);
				// 			if (!points.GetDictionary().ContainsKey(mp))
				// 			{
				// 				Debug.Log(points.GetDictionary().ContainsKey(mp));
				// 				continue;
				// 			}
				// 			ExtrapolatedAddPoints(mp, new Vector2(0, 1), ref usedPoints);
				// 			ExtrapolatedAddPoints(mp, new Vector2(0, -1), ref usedPoints);
				// 			ExtrapolatedAddPoints(mp, new Vector2(1, 0), ref usedPoints);
				// 			ExtrapolatedAddPoints(mp, new Vector2(-1, 0), ref usedPoints);

				// 			Vector3 ptWorld = GetWorldPt(mp);
				// 			if (TextureButton(ptWorld, UIAssets.Instance.pointEditRemove, 0.5f))
				// 			{
				// 				// delete point
				// 				changed = true;
				// 				Undo.RecordObject(lineMap, "delete point");
				// 				lineMap.RemovePoint(mp);
				// 				break;
				// 			}
				// 		}

				// 		Handles.EndGUI();
				// 	}
				// 	else if (currentEditMode == EditMode.ConnectGridPoints)
				// 	{
				// 		Handles.BeginGUI();
				// 		for (int i = 0; i < points.GetDictionary().Keys.Count; i++)
				// 		{
				// 			MapPoint mp = points.GetDictionary().Keys.ElementAt(i);

				// 			Vector3 ptWorld = GetWorldPt(mp);

				// 			GUI.color = Color.white;
				// 			if (selectedMapPoint == mp)
				// 			{
				// 				GUI.color = Color.red;
				// 			}

				// 			if (TextureButton(ptWorld, UIAssets.Instance.pointEditColor, 0.5f, fade: false))
				// 			{
				// 				if (selectedMapPoint == null)
				// 				{
				// 					selectedMapPoint = mp;
				// 				}
				// 				else if (selectedMapPoint == mp)
				// 				{
				// 					selectedMapPoint = null;
				// 				}
				// 				else
				// 				{
				// 					// delete point
				// 					changed = true;
				// 					Undo.RecordObject(lineMap, "delete point");
				// 					if (points[selectedMapPoint].Contains(mp))
				// 					{
				// 						lineMap.RemoveConnection(selectedMapPoint, mp);
				// 					}
				// 					else
				// 					{
				// 						lineMap.AddConnection(selectedMapPoint, mp);
				// 					}
				// 					selectedMapPoint = null;
				// 				}
				// 				break;
				// 			}
				// 		}

				// 		Handles.EndGUI();
				// 	}
				// 	else if (currentEditMode == EditMode.MapPointStyle)
				// 	{
				// 		Handles.BeginGUI();
				// 		foreach (MapPoint mp in points.GetDictionary().Keys)
				// 		{
				// 			Vector3 ptWorld = GetWorldPt(mp);

				// 			Color col = GetColor(mp);
				// 			col.a = 1f;
				// 			GUI.color = col;
				// 			if (TextureButton(ptWorld, UIAssets.Instance.pointEditColor, 0.5f, fade: false))
				// 			{
				// 				Rect popupWindowRect = new Rect(0, 0, UIAssets.Instance.pointEditColor.width * .5f, UIAssets.Instance.pointEditColor.height * .5f);
				// 				popupWindowRect.center = HandleUtility.WorldToGUIPoint(ptWorld);
				// 				PopupWindow.Show(popupWindowRect, new MapPointStylePopupWindow(styles, lineMap, mp));
				// 			}
				// 		}

				// 		GUI.color = Color.white;
				// 		Handles.EndGUI();
				// 	}

				if (routeLineData.pointIDs.Count < 2)
				{
					routePolyline.Mesh.Clear();
				}
				else
				{
					routePolyline.SetPoints(lineMap.GetPolylinePointsFromIndices(routeLineData.pointIDs));
				}
			}

			return routeLineData;
		}

	}


	[ExecuteAlways]
	public class RouteLineDrawer : ImmediateModeShapeDrawer
	{
		public RouteLineDrawer() : base()
		{
			
		}

		public override void DrawShapes(Camera cam)
		{
			using (Draw.Command(cam))
			{
				// set up static parameters. these are used for all following Draw.Line calls
				Draw.LineGeometry = LineGeometry.Volumetric3D;
				Draw.ThicknessSpace = ThicknessSpace.Pixels;
				Draw.Thickness = 4; // 4px wide

				// set static parameter to draw in the local space of this object
				Draw.Matrix = transform.localToWorldMatrix;

				// draw lines
				Draw.Line(Vector3.zero, Vector3.right, Color.red);
				Draw.Line(Vector3.zero, Vector3.up, Color.green);
				Draw.Line(Vector3.zero, Vector3.forward, Color.blue);
			}

		}

	}
}