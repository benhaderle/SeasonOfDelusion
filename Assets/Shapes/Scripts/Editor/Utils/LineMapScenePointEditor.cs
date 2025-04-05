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

		SphereBoundsHandle discHandle = ShapesHandles.InitDiscHandle();

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

		public bool DoSceneHandles(LineMap lineMap, MapPointDictionary points, Transform tf, float globalThicknessScale = 1f, Color globalColorTint = default, List<MapPointStyle> styles = null)
		{
			MapPoint MakeGridPoint(MapPoint gridPoint, Vector3 offset)
			{
				return new MapPoint(points.GetNextID(), gridPoint.point + offset, gridPoint.color, gridPoint.thickness);
			}

			CheckForCancelEditAction();
			if (IsHoldingAlt)
				return false;

			bool changed = false;

			Vector3 GetWorldPt(MapPoint mp) => tf.TransformPoint(mp.point);

			if (isEditing)
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
						return true;
					}

					return false;
				}

				Quaternion handleRotation = Tools.pivotRotation == PivotRotation.Global ? Quaternion.identity : tf.rotation;


				if (currentEditMode == EditMode.AddMovePoints)
				{
					void ExtrapolatedAddPoints(MapPoint point, Vector2 offset, ref List<Vector2> usedPoints)
					{
						if (usedPoints.Any(p => ((Vector2)point.point + offset - p).sqrMagnitude < .1f))
						{
							return;
						}
						MapPoint newPtData = MakeGridPoint(point, offset);
						Vector3 ptWorld = tf.TransformPoint(newPtData.point);
						usedPoints.Add((Vector2)point.point + offset);

						Handles.EndGUI();
						Handles.DrawDottedLine(tf.TransformPoint(point.point), ptWorld, 5f);
						Handles.BeginGUI();

						_ = DoAddGridPoint(ptWorld, newPtData, new List<MapPoint>() { point });
					}

					for (int i = 0; i < points.GetDictionary().Count; i++)
					{
						MapPoint mp = points.GetDictionary().Keys.ElementAt(i);

						Vector3 ptWorld = GetWorldPt(mp);
						Vector3 newPosWorld = Handles.PositionHandle(ptWorld, handleRotation);
						if (GUI.changed)
						{
							changed = true;
							Undo.RecordObject(lineMap, "modify points");
							mp.point = tf.InverseTransformPoint(newPosWorld);
						}
					}

					Handles.BeginGUI();
					List<Vector2> usedPoints = points.GetDictionary().Keys.Select(p => (Vector2)p.point).ToList();
					for (int i = 0; i < points.GetDictionary().Count; i++)
					{
						MapPoint mp = points.GetDictionary().Keys.ElementAt(i);

						float addDistance = .25f * Mathf.Sqrt(Camera.current.transform.position.z / -1f);
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
				Undo.RecordObjects(new Object[] { lineMap }, "modify style");
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
}
