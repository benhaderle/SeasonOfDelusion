using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

// Shapes © Freya Holmér - https://twitter.com/FreyaHolmer/
// Website & Documentation - https://acegikmo.com/shapes/
namespace Shapes {

	[CustomEditor(typeof(LineMap))]
	[CanEditMultipleObjects]
	public class LineMapEditor : ShapeRendererEditor
	{
		SerializedProperty propPoints = null;
		SerializedProperty propPointStyles;
		SerializedProperty propGeometry = null;
		SerializedProperty propJoins = null;
		SerializedProperty propThickness = null;
		SerializedProperty propThicknessSpace = null;
		ScenePointEditor scenePointEditor;
		ReorderableList pointStyles;

		public override void OnEnable()
		{
			base.OnEnable();

			scenePointEditor = new ScenePointEditor(this) { hasAddRemoveMode = false, hasEditThicknessMode = true, hasEditColorMode = true, hasAddRemoveGridMode = true, hasConnectGridMode = true, hasMapStyleMode = true };

			pointStyles = new ReorderableList(serializedObject, propPointStyles, true, true, true, true)
			{
				drawElementCallback = DrawMapPointStyleElement
			};
		}

		public override void OnInspectorGUI()
		{
			base.BeginProperties();
			EditorGUILayout.PropertyField(propGeometry);
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(propJoins);
			ShapesUI.FloatInSpaceField(propThickness, propThicknessSpace);
			pointStyles.DoLayoutList();

			scenePointEditor.GUIEditButton("Edit Points in Scene");

			base.EndProperties();
		}

		void OnSceneGUI()
		{
			LineMap p = target as LineMap;
			scenePointEditor.useFlatThicknessHandles = p.Geometry == PolylineGeometry.Flat2D;
			scenePointEditor.hasEditThicknessMode = p.ThicknessSpace == ThicknessSpace.Meters;
			bool changed = scenePointEditor.DoSceneHandles(p, p.points, p.transform, p.Thickness, p.Color, p.pointStyles.Prepend(new MapPointStyle {id="None"}).ToList());
			if (changed)
				p.UpdateMesh(force: true);

		}
		
		
		// Draws the elements on the list
		void DrawMapPointStyleElement( Rect r, int i, bool isActive, bool isFocused ) {
			r.yMin += 1;
			r.yMax -= 2;
			SerializedProperty prop = propPointStyles.GetArrayElementAtIndex( i );
			SerializedProperty pId= prop.FindPropertyRelative( nameof(MapPointStyle.id) );
			SerializedProperty pThickness = prop.FindPropertyRelative( nameof(MapPointStyle.thickness) );
			SerializedProperty pColor = prop.FindPropertyRelative( nameof(MapPointStyle.color) );

			using( var chChk = new EditorGUI.ChangeCheckScope() )
			{
				const float THICKNESS_MARGIN = 2;
				const float rightSideWidth = ShapesUI.POS_COLOR_FIELD_COLOR_WIDTH + ShapesUI.POS_COLOR_FIELD_THICKNESS_WIDTH + THICKNESS_MARGIN;

				Rect rectColor = r;
				rectColor.x = r.xMax - ShapesUI.POS_COLOR_FIELD_COLOR_WIDTH;
				rectColor.width = ShapesUI.POS_COLOR_FIELD_COLOR_WIDTH;

				Rect rectID = r;
				rectID.width -= rightSideWidth;

				Rect rectThickness = r;
				rectThickness.x = r.xMax - rightSideWidth + THICKNESS_MARGIN;
				rectThickness.width = ShapesUI.POS_COLOR_FIELD_THICKNESS_WIDTH;

				EditorGUI.PropertyField(rectID, pId);
				EditorGUIUtility.labelWidth = 18;
				EditorGUI.PropertyField(rectThickness, pThickness, new GUIContent("Th", "thickness"));
				EditorGUI.PropertyField(rectColor, pColor, GUIContent.none);
				if( chChk.changed )
					pThickness.floatValue = Mathf.Max( 0.001f, pThickness.floatValue ); // Make sure it's never 0 or under
			}
		}
	}
}