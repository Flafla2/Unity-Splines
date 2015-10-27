using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Spline))]
public class SplineEditor : Editor {

	private const int curve_draw_iter = 20;

	private Spline spline;
	private Quaternion rot;
	private Transform tr;

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
			if(spline.Loop && x == spline.GetControlPointCount()-1)
				continue;

			Handles.color = Color.yellow;
			Vector3 pos = spline.GetControlPoint(x);
			bool pressed = false;
			if(x != 0 || spline.Loop) {
				Vector3 hn = spline.GetHandle(x,true);
				Handles.DrawLine(pos,hn);
				
				if(x == spline.selected_elt) {
					Handles.DotCap(0,hn,rot,HandleUtility.GetHandleSize(hn)*0.05f);
					Handles.Label(hn, "Left Handle");
				}
				else {
					Handles.color = Color.gray;
					pressed |= Handles.Button(hn,rot,HandleUtility.GetHandleSize(hn)*0.05f,HandleUtility.GetHandleSize(hn)*0.06f,Handles.DotCap);
					Handles.color = Color.yellow;
				}
			}
			if(x < spline.GetCurveCount()) {
				Vector3 hn = spline.GetHandle(x,false);
				Handles.DrawLine(pos,hn);
				Handles.DotCap(0,hn,rot,HandleUtility.GetHandleSize(hn)*0.05f);
				if(x == spline.selected_elt) {
					Handles.DotCap(0,hn,rot,HandleUtility.GetHandleSize(hn)*0.05f);
					Handles.Label(hn, "Right Handle");
				}
				else {
					Handles.color = Color.gray;
					pressed |= Handles.Button(hn,rot,HandleUtility.GetHandleSize(hn)*0.05f,HandleUtility.GetHandleSize(hn)*0.06f,Handles.DotCap);
					Handles.color = Color.yellow;
				}
			}

			if(x == spline.selected_elt) {
				Handles.color = Color.green;
				Handles.DotCap(0,pos,rot,HandleUtility.GetHandleSize(pos)*0.05f);
				continue;
			}

			Handles.color = Color.white;
			pressed |= Handles.Button(pos,rot,HandleUtility.GetHandleSize(pos)*0.05f,HandleUtility.GetHandleSize(pos)*0.06f,Handles.DotCap);
			if(pressed) {
				EditorUtility.SetDirty( target );
				spline.selected_elt = x;
			}
		}

		

		Vector3 pt = spline.GetControlPoint(spline.selected_elt);
		EditorGUI.BeginChangeCheck();
		Vector3 npt = Handles.PositionHandle(pt, rot);
		if (EditorGUI.EndChangeCheck()) {
			Undo.RecordObject(spline, "Move Point");
			spline.SetControlPoint(spline.selected_elt,tr.InverseTransformPoint(npt));
			EditorUtility.SetDirty(spline);
		}

		if(spline.Loop || spline.selected_elt != 0) {
			pt = spline.GetHandle(spline.selected_elt,true);
			EditorGUI.BeginChangeCheck();
			pt = Handles.PositionHandle(pt, rot);
			if (EditorGUI.EndChangeCheck()) {
				Undo.RecordObject(spline, "Move Left Handle");
				spline.SetHandle(spline.selected_elt,true,pt);
				EditorUtility.SetDirty(spline);
			}
		}

		if(spline.selected_elt < spline.GetCurveCount()) {
			pt = spline.GetHandle(spline.selected_elt,false);
			EditorGUI.BeginChangeCheck();
			pt = Handles.PositionHandle(pt, rot);
			if (EditorGUI.EndChangeCheck()) {
				Undo.RecordObject(spline, "Move Right Handle");
				spline.SetHandle(spline.selected_elt,false,pt);
				EditorUtility.SetDirty(spline);
			}
		}
	}

	public override void OnInspectorGUI() {
		if(spline == null)
			return;

        EditorGUI.BeginChangeCheck();
        spline.DrawGizmo = EditorGUILayout.Toggle("Draw Gizmo", spline.DrawGizmo);

		EditorGUI.BeginChangeCheck();
		bool loop = EditorGUILayout.Toggle("Loop:", spline.Loop);
		if (EditorGUI.EndChangeCheck()) {
			Undo.RecordObject(spline, "Toggle Loop");
			spline.Loop = loop;
			EditorUtility.SetDirty(spline);
		}

		EditorGUI.BeginChangeCheck();
		Vector3 point = EditorGUILayout.Vector3Field("Point:",spline.GetControlPoint(spline.selected_elt));
		if (EditorGUI.EndChangeCheck()) {
			Undo.RecordObject(spline, "Move Point");
			spline.SetControlPoint(spline.selected_elt,point);
			EditorUtility.SetDirty(spline);
		}
		if(spline.selected_elt != 0) {
			EditorGUI.BeginChangeCheck();
			point = EditorGUILayout.Vector3Field("Left Handle:",spline.GetHandle(spline.selected_elt,true));
			if (EditorGUI.EndChangeCheck()) {
				Undo.RecordObject(spline, "Move Left Handle");
				spline.SetHandle(spline.selected_elt,true,point);
				EditorUtility.SetDirty(spline);
			}
		}
		if(spline.selected_elt < spline.GetCurveCount()) {
			EditorGUI.BeginChangeCheck();
			point = EditorGUILayout.Vector3Field("Right Handle:",spline.GetHandle(spline.selected_elt,false));
			if (EditorGUI.EndChangeCheck()) {
				Undo.RecordObject(spline, "Move Left Handle");
				spline.SetHandle(spline.selected_elt,false,point);
				EditorUtility.SetDirty(spline);
			}
		}

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
		if((spline.selected_elt != 0 || spline.Loop) && GUILayout.Button("Left")) {
			Undo.RecordObject(spline, "Subdivide Curve Left");
			spline.SubdivideCurve(spline.selected_elt);
			EditorUtility.SetDirty(spline);
		}
		if((spline.selected_elt < spline.GetCurveCount() || spline.Loop) && GUILayout.Button("Right")) {
			Undo.RecordObject(spline, "Subdivide Curve Right");
			spline.SubdivideCurve(spline.selected_elt+1);
			EditorUtility.SetDirty(spline);
		}
		EditorGUILayout.EndHorizontal();

		if(GUILayout.Button("Remove Curve")) {
			Undo.RecordObject(spline, "Remove Curve");
			spline.RemoveCurve(spline.selected_elt);
			EditorUtility.SetDirty(spline);
		}

		if((spline.selected_elt != 0 && spline.selected_elt < spline.GetControlPointCount()-1) || spline.Loop) {
			EditorGUI.BeginChangeCheck();
			Spline.HandleConstraint nw = (Spline.HandleConstraint)EditorGUILayout.EnumPopup("Handle Constraint: ", spline.GetConstraint(spline.selected_elt));
			if(EditorGUI.EndChangeCheck()) {
				Undo.RecordObject(spline, "Set Constraint");
				spline.SetConstraint(spline.selected_elt,nw);
				if(spline.selected_elt == spline.GetControlPointCount()-1)
					spline.selected_elt = 0;
				EditorUtility.SetDirty(spline);
			}
		}
	}

}
