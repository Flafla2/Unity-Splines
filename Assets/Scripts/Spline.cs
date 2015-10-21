using UnityEngine;
using System;
using System.Collections.Generic;

public class Spline : MonoBehaviour {

	// Format: p1, h1, h2, p2, h1, h2, p3...
	// That is, (p1), (right handle of p1), (left handle of p2), (p2), (right handle of p2), (left handle of p3)...
	private Vector3[] points;
	// Format: p1, p2, p3...
	private HandleConstraint[] constraints;

	public Vector3 GetPoint(float t) {
		int base_coord = (int)t * 3;
		float t_sub = t-(int)t;

		if(t == (int)t && t != 0) {
			base_coord-=3;
			t_sub = 1;
		}

		if(base_coord+3 >= points.Length || base_coord < 0)
			return Vector3.zero;

		return transform.TransformPoint(Bezier.PointOnBezier(points[base_coord],points[base_coord+1],points[base_coord+2],points[base_coord+3],t_sub));
	}

	public int GetControlPointCount() {
		return (points.Length-1)/3+1;
	}

	public int GetCurveCount() {
		return (points.Length-1)/3;
	}

	public HandleConstraint GetConstraint(int index) {
		return constraints[index];
	}

	public void SetConstraint(int index, HandleConstraint constraint) {
		constraints[index] = constraint;
	}

	public Vector3 GetControlPoint(int index) {
		return transform.TransformPoint(points[index * 3]);
	}

	public Vector3 GetHandle(int index, bool left) {
		if(left)
			return transform.TransformPoint(points[index*3-1]);
		return transform.TransformPoint(points[index*3+1]);
	}

	public void SetControlPoint(int index, Vector3 point) {
		int true_index = index * 3;
		Vector3 inv = transform.InverseTransformPoint(point);
		Vector3 diff = inv-points[true_index];
		if(true_index > 0)
			points[true_index-1] += diff;
		if(true_index < points.Length-1)
			points[true_index+1] += diff;
		points[true_index] = inv;
	}

	public void SetHandle(int index, bool left, Vector3 point) {
		int true_index = index*3;
		int active = left ? true_index-1 : true_index+1;
		int constrain = left ? true_index+1 : true_index-1;

		points[active] = transform.InverseTransformPoint(point);

		Vector3 control = points[true_index];
		Vector3 diff = point-points[active];
		if(constrain >= 0 && constrain < points.Length) {
			switch(constraints[index]) {
				case HandleConstraint.Aligned:
				float magnitude = (control-points[constrain]).magnitude;
				Vector3 dir = -diff.normalized;
				points[constrain] = control + magnitude*dir;
				break;
				case HandleConstraint.Mirrored:
				points[constrain] = control-diff;
				break;
			}
		}
	}

	public Vector3 GetDeriv(float t) {
		int base_coord = (int)t * 3;
		if(base_coord+4 > points.Length || base_coord < 0)
			return Vector3.zero;
		float t_sub = t-(int)t;

		return Bezier.DerivOnBezier(points[base_coord],points[base_coord+1],points[base_coord+2],points[base_coord+3],t_sub);
	}

	public Vector3 GetDirection(float t) {
		return GetDeriv(t).normalized;
	}

	public void AddCurve() {
		Array.Resize<Vector3>(ref points, points.Length+3);
		Array.Resize<HandleConstraint>(ref constraints, constraints.Length+1);

		points[points.Length-1] = points[points.Length-4] + Vector3.right*3;
		points[points.Length-2] = points[points.Length-4] + Vector3.right*2;
		points[points.Length-3] = points[points.Length-4] + Vector3.right;
	}

	public void AddCurveBeginning() {
		Vector3 point = points[0];
		Vector3 deriv = GetDeriv(0);

		Array.Resize<Vector3>(ref points, points.Length+3);
		Array.Resize<HandleConstraint>(ref constraints, constraints.Length+1);
		ShiftArray<Vector3>(ref points, 3, 0);
		ShiftArray<HandleConstraint>(ref constraints, 1, 0);

		points[0] = point - 3*deriv;
		points[1] = point - 2*deriv;
		points[2] = point-deriv;

		constraints[0] = HandleConstraint.Free;
	}

	public void RemoveCurve(int elt) {
		if(elt*3 > points.Length)
			return;

		if(elt*3 != points.Length) {
			ShiftArray<Vector3>(ref points, -3, elt*3-1);
			ShiftArray<HandleConstraint>(ref constraints, -1, elt);
		}

		Array.Resize<Vector3>(ref points, points.Length-3);
		Array.Resize<HandleConstraint>(ref constraints, constraints.Length-1);
	}

	// Subdivides a curve to the left - that is, adds a curve in between the left curve of the `elt` curve and the `elt` curve
	// elt is 0 indexed
	public void SubdivideCurve(int elt) {
		if(elt*3 > points.Length && elt != 0)
			return;

		Vector3 point = GetPoint((float)elt-0.5f);
		Vector3 deriv = GetDirection((float)elt-0.5f);
		
		Array.Resize<Vector3>(ref points, points.Length+3);
		ShiftArray<Vector3>(ref points, 3, elt*3-1);

		points[elt*3] = point;
		points[elt*3+1] = point+deriv;
		points[elt*3-1] = point-deriv;
	}

	private void ShiftArray<T>(ref T[] arr, int shift, int first) {
		bool r = shift > 0;
		shift = Mathf.Abs(shift);
		if(r) {
			for(int x=arr.Length-1;x >= first && x >= shift;x--)
				arr[x] = arr[x-shift];
		} else {
			for(int x=first;x<arr.Length-shift;x++)
				arr[x] = arr[x+shift];
		}
	}

	public void Reset() {
		points = new Vector3[] {
			new Vector3(1,0,0),
			new Vector3(2,0,0),
			new Vector3(3,0,0),
			new Vector3(4,0,0)
		};

		constraints = new HandleConstraint[] {
			HandleConstraint.Free,
			HandleConstraint.Free
		};
	}

	public enum HandleConstraint {
		Free, Aligned, Mirrored
	}
}
