using UnityEngine;
using System;
using System.Collections.Generic;

public class Spline : MonoBehaviour {

	// Format: p1, h1, h2, p2, h1, h2, p3...
	// That is, (p1), (right handle of p1), (left handle of p2), (p2), (right handle of p2), (left handle of p3)...
	[SerializeField]
	private Vector3[] points;
	// Format: p1, p2, p3...
	[SerializeField]
	private HandleConstraint[] constraints;
	[SerializeField]
	public bool Loop {
		get { return _Loop; }
		set {
			_Loop = value;
			if(_Loop)
				points[0] = points[points.Length-1];
		}
	}
	private bool _Loop = false;
	public int selected_elt = 0;

	public Vector3 GetPoint(float t) {
		int base_coord = (int)t * 3;
		if(Loop)
			base_coord %= points.Length-3;
		float t_sub = t%1;

		if(t == (int)t && t != 0) {
			base_coord-=3;
			t_sub = 1;
		}

		if(base_coord+3 >= points.Length)
			return transform.TransformPoint(points[points.Length-1]);
		if(base_coord < 0)
			return transform.TransformPoint(points[0]);

		return transform.TransformPoint(Bezier.PointOnBezier(points[base_coord],points[base_coord+1],points[base_coord+2],points[base_coord+3],t_sub));
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



	public int GetControlPointCount() {
		return (points.Length-1)/3+1;
	}

	public int GetCurveCount() {
		return (points.Length-1)/3;
	}

	private Vector3 GetControlPointRaw(int index) {
		if(Loop && index == GetControlPointCount()-1)
			index = 0;

		if(index < 0 || index >= points.Length-1)
			return null;

		return points[index*3];
	}

	public Vector3 GetControlPoint(int index) {
		Vector3 raw = GetControlPointRaw(index);
		if(raw == null)
			return null;
		return transform.TransformPoint(raw);
	}

	private Vector3 GetHandleRaw(int index, bool left) {
		if(Loop && index == 0 && left)
			index = GetControlPointCount()-1;
		if(Loop && index == GetControlPointCount()-1 && !left)
			index = 0;
		int ind = index*3 + (left ? -1 : 1);

		if(ind < 0 || ind >= points.Length-1)
			return null;

		return points[ind];
	}

	public Vector3 GetHandle(int index, bool left) {
		Vector3 raw = GetHandleRaw(index,left);
		if(raw == null)
			return null;
		return transform.TransformPoint(raw);
	}

	public void SetControlPointRaw(int index, Vector3 point) {
		if(Loop && index == GetControlPointCount()-1)
			index = 0;

		if(index < 0 || index >= GetControlPointCount())
			return;

		// 1. Move all of the handles over along with the control point
		Vector3 og = GetControlPointRaw(index);
		Vector3 diff = point-og;
		Vector3 left = GetHandleRaw(index,true);
		Vector3 right = GetHandleRaw(index,false);
		if(left != null)
			SetHandleRaw(index,true,left+diff);
		if(right != null)
			SetHandleRaw(index,false,right+diff);
		
		// 2. Actually set the point
		points[index*3] = point;
	}

	public void SetControlPoint(int index, Vector3 point) {
		SetControlPointRaw(index,transform.InverseTransformPoint(point));
	}

	public void SetHandle(int index, bool left, Vector3 point) {
		if(Loop && index == 0 && left)
			index = GetControlPointCount()-1;
		else if(Loop && index == GetControlPointCount()-1 && !left)
			index = 0;

		int true_index = index*3;
		int active = left ? true_index-1 : true_index+1;

		points[active] = transform.InverseTransformPoint(point);

		EnforceConstraint(index, left ? ConstraintEnforcementType.Left : ConstraintEnforcementType.Right);
	}

	public HandleConstraint GetConstraint(int index) {
		return constraints[index];
	}

	public void SetConstraint(int index, HandleConstraint constraint) {
		constraints[index] = constraint;

		EnforceContraint(index, ConstraintEnforcementType.Average);
	}

	private void EnforceContraint(int index, ConstraintEnforcementType type) {
		if(!Loop && (index == 0 || index == GetControlPointCount()-1))
			return;
		int true_index = index*3;
		int left =  (true_index-1+points.Length)%points.Length;
		int right = (true_index+1)%points.Length;

		Vector3 control = points[true_index];
		Vector3 diff_l = points[left]-control;
		float mag_l = diff_l.magnitude;
		Vector3 diff_r = points[right]-control;
		float mag_r = diff_r.magnitude;
		Vector3 dir_r = Vector3.Slerp(diff_r/mag_r,-diff_l/mag_l,0.5f);
		switch(constraints[index]) {
			case HandleConstraint.Aligned:
			points[right] = control + mag_r*dir_r;
			points[left] = control - mag_l*dir_r;
			break;
			case HandleConstraint.Mirrored:
			float mag_avg = (mag_l+mag_r)/2f;
			points[right] = control + mag_avg*dir_r;
			points[left] = control - mag_avg*dir_r;
			break;
		}
	}

	

	public void AddCurve() {
		if(Loop)
			return;

		Array.Resize<Vector3>(ref points, points.Length+3);
		Array.Resize<HandleConstraint>(ref constraints, constraints.Length+1);

		points[points.Length-1] = points[points.Length-4] + Vector3.right*3;
		points[points.Length-2] = points[points.Length-4] + Vector3.right*2;
		points[points.Length-3] = points[points.Length-4] + Vector3.right;

		EnforceContraint(GetControlPointCount()-1, ConstraintEnforcementType.Average);
	}

	public void AddCurveBeginning() {
		if(Loop)
			return;

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

		EnforceContraint(0, ConstraintEnforcementType.Average);
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
		if(Loop && elt == 0)
			elt = GetControlPointCount()-1;
		if(elt*3 > points.Length && elt != 0)
			return;

		Vector3 point = GetPoint((float)elt-0.5f);
		Vector3 deriv = GetDirection((float)elt-0.5f);
		
		Array.Resize<Vector3>(ref points, points.Length+3);
		ShiftArray<Vector3>(ref points, 3, elt*3-1);

		Array.Resize<HandleConstraint>(ref constraints, constraints.Length+1);
		ShiftArray<HandleConstraint>(ref constraints, 1, elt);

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

	private enum ConstraintEnforcementType {
		Left, Right, Average
	}
}
