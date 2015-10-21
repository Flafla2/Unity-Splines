using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Line))]
public class LineEditor : Editor {
	private void OnSceneGUI() {
		Line line = target as Line;
		Transform t = line.transform;
		Quaternion r = Tools.pivotRotation == PivotRotation.Local ? t.rotation : Quaternion.identity;

		Vector3 p0 = t.TransformPoint(line.p0);
		Vector3 p1 = t.TransformPoint(line.p1);

		Handles.color = Color.white;
		Handles.DrawLine(p0, p1);
		
		EditorGUI.BeginChangeCheck();
		p0 = Handles.DoPositionHandle(p0, r);
		if (EditorGUI.EndChangeCheck()) {
			Undo.RecordObject(line, "Move Point");
			EditorUtility.SetDirty(line);
			line.p0 = t.InverseTransformPoint(p0);
		}

		EditorGUI.BeginChangeCheck();
		p1 = Handles.DoPositionHandle(p1, r);
		if (EditorGUI.EndChangeCheck()) {
			Undo.RecordObject(line, "Move Point");
			EditorUtility.SetDirty(line);
			line.p1 = t.InverseTransformPoint(p1);
		}
	}
	
}