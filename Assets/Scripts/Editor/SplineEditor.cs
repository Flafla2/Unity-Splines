using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Spline))]
public class SplineEditor : Editor {

	private const int curve_draw_iter = 20;

	private Spline spline;
	private Quaternion rot;
	private Transform tr;

	private int selected_elt = 0;

	private void OnSceneGUI() {
		spline = target as Spline;
		tr = spline.transform;
		rot = Tools.pivotRotation == PivotRotation.Local ? tr.rotation : Quaternion.identity;

		Handles.color = Color.white;

		int num_curves = spline.GetCurveCount();
		float t = 0;
		Vector3 prev = spline.GetControlPoint(0);
		for(int i=0;i<num_curves;i++) {
			for(int x=1;x<=curve_draw_iter;x++) {
				t = i + (float)x / (float)curve_draw_iter;

				Vector3 next = spline.GetPoint(t);
				Handles.DrawLine(prev,next);
				prev = next;
			}
		}

		
		for(int x=0;x<spline.GetControlPointCount();x++) {
			Handles.color = Color.yellow;
			if(x != 0)
				Handles.DrawLine(spline.GetControlPoint(x),spline.GetHandle(x,true));
			if(x < spline.GetCurveCount())
				Handles.DrawLine(spline.GetControlPoint(x),spline.GetHandle(x,false));

			if(x == selected_elt)
				continue;

			Handles.color = Color.white;
			Vector3 pos = spline.GetControlPoint(x);
			bool pressed = Handles.Button(pos,rot,HandleUtility.GetHandleSize(pos)*0.05f,HandleUtility.GetHandleSize(pos)*0.06f,Handles.DotCap);
			if(pressed) {
				EditorUtility.SetDirty( target );
				selected_elt = x;
			}
		}

		

		Vector3 pt = spline.GetControlPoint(selected_elt);
		EditorGUI.BeginChangeCheck();
		Vector3 npt = Handles.PositionHandle(pt, rot);
		if (EditorGUI.EndChangeCheck()) {
			Undo.RecordObject(spline, "Move Point");
			spline.SetControlPoint(selected_elt,tr.InverseTransformPoint(npt));
			EditorUtility.SetDirty(spline);
		}

		if(selected_elt != 0) {
			pt = spline.GetHandle(selected_elt,true);
			EditorGUI.BeginChangeCheck();
			pt = Handles.PositionHandle(pt, rot);
			if (EditorGUI.EndChangeCheck()) {
				Undo.RecordObject(spline, "Move Left Handle");
				spline.SetHandle(selected_elt,true,pt);
				EditorUtility.SetDirty(spline);
			}
		}

		if(selected_elt < spline.GetCurveCount()) {
			pt = spline.GetHandle(selected_elt,false);
			EditorGUI.BeginChangeCheck();
			pt = Handles.PositionHandle(pt, rot);
			if (EditorGUI.EndChangeCheck()) {
				Undo.RecordObject(spline, "Move Right Handle");
				spline.SetHandle(selected_elt,false,pt);
				EditorUtility.SetDirty(spline);
			}
		}
	}

	public override void OnInspectorGUI() {
		if(spline == null)
			return;

		spline.SetControlPoint(selected_elt,EditorGUILayout.Vector3Field("Point:",spline.GetControlPoint(selected_elt)));
		if(selected_elt != 0)
			spline.SetHandle(selected_elt,true,EditorGUILayout.Vector3Field("Left Handle:",spline.GetHandle(selected_elt,true)));
		if(selected_elt < spline.GetCurveCount())
			spline.SetHandle(selected_elt,false,EditorGUILayout.Vector3Field("Right Handle:",spline.GetHandle(selected_elt,false)));

		GUILayout.Label("Add Curve At:");
		EditorGUILayout.BeginHorizontal();
		if(GUILayout.Button("Beginning")) {
			Undo.RecordObject(spline, "Add Curve at Beginning");
			spline.AddCurveBeginning();
			EditorUtility.SetDirty(spline);
		}
		if(GUILayout.Button("End")) {
			Undo.RecordObject(spline, "Add Curve at End");
			spline.AddCurve();
			EditorUtility.SetDirty(spline);
		}
		EditorGUILayout.EndHorizontal();

		GUILayout.Label("Subdivide Curve:");
		EditorGUILayout.BeginHorizontal();
		if(selected_elt != 0 && GUILayout.Button("Left")) {
			Undo.RecordObject(spline, "Subdivide Curve Left");
			spline.SubdivideCurve(selected_elt);
			EditorUtility.SetDirty(spline);
		}
		if(selected_elt < spline.GetCurveCount() && GUILayout.Button("Right")) {
			Undo.RecordObject(spline, "Subdivide Curve Right");
			spline.SubdivideCurve(selected_elt+1);
			EditorUtility.SetDirty(spline);
		}
		EditorGUILayout.EndHorizontal();

		if(GUILayout.Button("Remove Curve")) {
			Undo.RecordObject(spline, "Remove Curve");
			spline.RemoveCurve(selected_elt);
			EditorUtility.SetDirty(spline);
		}
	}

}
