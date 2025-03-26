using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

// Shapes © Freya Holmér - https://twitter.com/FreyaHolmer/
// Website & Documentation - https://acegikmo.com/shapes/
namespace Shapes {

	[CustomEditor( typeof(LineMap) )]
	[CanEditMultipleObjects]
	public class LineMapEditor : ShapeRendererEditor {
		SerializedProperty propPoints = null;
		SerializedProperty propGeometry = null;
		SerializedProperty propJoins = null;
		SerializedProperty propThickness = null;
		SerializedProperty propThicknessSpace = null;


		ScenePointEditor scenePointEditor;
		bool showZ;

		public override void OnEnable() {
			base.OnEnable();

			scenePointEditor = new ScenePointEditor( this ) { hasAddRemoveMode = false, hasEditThicknessMode = true, hasEditColorMode = true, hasAddRemoveGridMode = true, hasConnectGridMode = true };
		}

		public override void OnInspectorGUI() {
			base.BeginProperties();
			if( Event.current.type == EventType.Layout )
				showZ = targets.Any( x => ( (LineMap)x ).Geometry != PolylineGeometry.Flat2D );
			EditorGUILayout.PropertyField( propGeometry );
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField( propJoins );
			ShapesUI.FloatInSpaceField( propThickness, propThicknessSpace );

			scenePointEditor.GUIEditButton( "Edit Points in Scene" );

			base.EndProperties();
		}

		void OnSceneGUI()
		{
			LineMap p = target as LineMap;
			scenePointEditor.useFlatThicknessHandles = p.Geometry == PolylineGeometry.Flat2D;
			scenePointEditor.hasEditThicknessMode = p.ThicknessSpace == ThicknessSpace.Meters;
			bool changed = scenePointEditor.DoSceneHandles(p, p.points, p.transform, p.Thickness, p.Color);
			if (changed)
				p.UpdateMesh(force: true);
			
		}
	}

}