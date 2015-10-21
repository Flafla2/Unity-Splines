using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Bezier))]
public class BezierEditor : Editor {

	private const int curve_draw_iter = 20;

	private void OnSceneGUI() {
		Bezier b = target as Bezier;
		Transform tr = b.transform;
		Quaternion r = Tools.pivotRotation == PivotRotation.Local ? tr.rotation : Quaternion.identity;

		Vector3 p0 = tr.TransformPoint(b.p0);
		Vector3 p1 = tr.TransformPoint(b.p1);
		Vector3 p2 = tr.TransformPoint(b.p2);
		Vector3 p3 = tr.TransformPoint(b.p3);

		Handles.color = Color.white;

		float t = 0;
		Vector3 prev = p0;
		for(int x=1;x<=curve_draw_iter;x++) {
			t = (float)x / (float)curve_draw_iter;

			Vector3 next = Bezier.PointOnBezier(p0,p1,p2,p3,t);
			Handles.DrawLine(prev,next);
			prev = next;
		}

		EditorGUI.BeginChangeCheck();
		p0 = Handles.DoPositionHandle(p0, r);
		if (EditorGUI.EndChangeCheck()) {
			Undo.RecordObject(b, "Move Point");
			EditorUtility.SetDirty(b);
			b.p0 = tr.InverseTransformPoint(p0);
		}

		EditorGUI.BeginChangeCheck();
		p1 = Handles.DoPositionHandle(p1, r);
		if (EditorGUI.EndChangeCheck()) {
			Undo.RecordObject(b, "Move Point");
			EditorUtility.SetDirty(b);
			b.p1 = tr.InverseTransformPoint(p1);
		}

		EditorGUI.BeginChangeCheck();
		p2 = Handles.DoPositionHandle(p2, r);
		if (EditorGUI.EndChangeCheck()) {
			Undo.RecordObject(b, "Move Point");
			EditorUtility.SetDirty(b);
			b.p2 = tr.InverseTransformPoint(p2);
		}

		EditorGUI.BeginChangeCheck();
		p3 = Handles.DoPositionHandle(p3, r);
		if (EditorGUI.EndChangeCheck()) {
			Undo.RecordObject(b, "Move Point");
			EditorUtility.SetDirty(b);
			b.p3 = tr.InverseTransformPoint(p3);
		}
	}

}
