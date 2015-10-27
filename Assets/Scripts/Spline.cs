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
	
	public bool Loop {
		get { return _Loop; }
		set {
			_Loop = value;
            if (_Loop)
            {
                if (selected_elt == GetControlPointCount() - 1)
                    selected_elt = 0;
                points[0] = points[points.Length - 1];
                SetConstraint(0, HandleConstraint.Free);
            }
		}
	}
    [SerializeField]
    private bool _Loop = false;

    public bool DrawGizmo = false;
	public int selected_elt = 0;

    // ------- GUI / DRAWING FUNCTIONALITY ------- //

    private void OnDrawGizmos()
    {
        if (!DrawGizmo)
            return;

        Gizmos.color = Color.white;
        int num_curves = GetCurveCount();
        float t = 0;
        Vector3 prev = GetControlPoint(0);
        for (int i = 0; i < num_curves; i++)
        {
            for (int x = 1; x <= 20; x++)
            {
                t = i + (float)x / 20f;

                Vector3 next = GetPoint(t);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }

    // -------- GENERALIZED BEZIER ACCESS -------- //

    public Vector3 GetPoint(float t) {
    	return transform.TransformPoint(GetPointRaw(t));
    }

    private Vector3 GetPointRaw(float t) {
		int base_coord = (int)t * 3;
		float t_sub = t-(int)t;

		if(t == (int)t && t != 0) {
			base_coord-=3;
			t_sub = 1;
		}

		if(base_coord+3 >= points.Length)
			return transform.TransformPoint(points[points.Length-1]);
		if(base_coord < 0)
			return transform.TransformPoint(points[0]);

        Vector3 p0 = points[base_coord];
        Vector3 p1 = points[base_coord + 1];
        Vector3 p2 = points[base_coord + 2];
        Vector3 p3 = points[base_coord + 3];

        return Bezier.PointOnBezier(p0,p1,p2,p3,t_sub);
	}

	public Vector3 GetDeriv(float t) {
		return transform.TransformDirection(GetDerivRaw(t));
	}

	private Vector3 GetDerivRaw(float t) {
		int base_coord = (int)t * 3;
		if(base_coord+4 > points.Length || base_coord < 0)
			return Vector3.zero;
		float t_sub = t-(int)t;

		return Bezier.DerivOnBezier(points[base_coord],points[base_coord+1],points[base_coord+2],points[base_coord+3],t_sub);
	}

	public Vector3 GetDirection(float t) {
		return GetDeriv(t).normalized;
	}

	private Vector3 GetDirectionRaw(float t) {
		return GetDerivRaw(t).normalized;
	}

    // ------- SPLINE EDITING TOOLS ------- //

    public int GetControlPointCount() {
		return (points.Length-1)/3+1;
	}

	public int GetCurveCount() {
		return (points.Length-1)/3;
	}

	private Vector3? GetControlPointRaw(int index) {
		if(Loop && index == GetControlPointCount()-1)
			index = 0;

		if(index < 0 || index >= points.Length-1)
			return null;

		return points[index*3];
	}

	public Vector3 GetControlPoint(int index) {
		Vector3? raw = GetControlPointRaw(index);
		if(raw == null)
			return Vector3.zero;
		return transform.TransformPoint((Vector3)raw);
	}

	private Vector3? GetHandleRaw(int index, bool left) {
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
		Vector3? raw = GetHandleRaw(index,left);
		if(raw == null)
			return Vector3.zero;
		return transform.TransformPoint((Vector3)raw);
	}

	public void SetControlPointRaw(int index, Vector3 point) {
		if(Loop && index == GetControlPointCount()-1)
			index = 0;

		if(index < 0 || index >= GetControlPointCount())
			return;

		// 1. Move all of the handles over along with the control point
		Vector3? og = GetControlPointRaw(index);
		Vector3? diff = point-og;
		Vector3? left = GetHandleRaw(index,true);
		Vector3? right = GetHandleRaw(index,false);
		if(left != null)
			SetHandleRaw(index,true,(Vector3)(left+diff));
		if(right != null)
			SetHandleRaw(index,false, (Vector3)(right +diff));
		
		// 2. Actually set the point
		points[index*3] = point;
        if (Loop && index == 0)
            points[points.Length - 1] = point;
	}

	public void SetControlPoint(int index, Vector3 point) {
		SetControlPointRaw(index,transform.InverseTransformPoint(point));
	}

	public void SetHandle(int index, bool left, Vector3 point) {
        SetHandleRaw(index,left,transform.InverseTransformPoint(point));
	}

    private void SetHandleRaw(int index, bool left, Vector3 point, bool enforce_constraint = true)
    {
        if (Loop && index == 0 && left)
            index = GetControlPointCount() - 1;
        else if (Loop && index == GetControlPointCount() - 1 && !left)
            index = 0;

        int ind = index * 3 + (left ? -1 : 1);

        points[ind] = point;

        if(enforce_constraint)
            EnforceConstraint(index, left ? ConstraintEnforcementType.Left : ConstraintEnforcementType.Right);
    }

	public HandleConstraint GetConstraint(int index) {
		return constraints[index];
	}

	public void SetConstraint(int index, HandleConstraint constraint) {
		constraints[index] = constraint;
        if (Loop && index == 0)
            constraints[constraints.Length - 1] = constraint;
        if (Loop && index == constraints.Length - 1)
            constraints[0] = constraint;

        EnforceConstraint(index, ConstraintEnforcementType.Average);
	}

	private void EnforceConstraint(int index, ConstraintEnforcementType type) {
		if(!Loop && (index == 0 || index == GetControlPointCount()-1))
			return;

		Vector3? control = GetControlPointRaw(index);
        Vector3? left = GetHandleRaw(index,true);
        Vector3? right = GetHandleRaw(index, false);

        Vector3? diff_l = left-control;
		float mag_l = ((Vector3)diff_l).magnitude;
		Vector3? diff_r = right-control;
		float mag_r = ((Vector3)diff_r).magnitude;

        Vector3 dir_r = Vector3.zero;
        switch(type)
        {
            case ConstraintEnforcementType.Left:
                dir_r = (Vector3) (-diff_l / mag_l);
                break;
            case ConstraintEnforcementType.Right:
                dir_r = (Vector3)(diff_r / mag_r);
                break;
            case ConstraintEnforcementType.Average:
                dir_r = Vector3.Slerp((Vector3)(diff_r / mag_r), (Vector3)(-diff_l / mag_l), 0.5f);
                break;
        }

		switch(constraints[index]) {
			case HandleConstraint.Aligned:
			SetHandleRaw(index,false, (Vector3)(control + mag_r*dir_r),false);
            SetHandleRaw(index,true,(Vector3)(control - mag_l*dir_r), false);
			break;
			case HandleConstraint.Mirrored:
			float mag_avg = (mag_l+mag_r)/2f;
            SetHandleRaw(index, false, (Vector3)(control + mag_avg*dir_r), false);
            SetHandleRaw(index, true, (Vector3)(control - mag_avg*dir_r), false);
			break;
		}
	}

    // ------- CURVE ADDITION / DELETION OPERATIONS ------- //

    public void AddCurve() {
		if(Loop)
			return;

		Array.Resize<Vector3>(ref points, points.Length+3);
		Array.Resize<HandleConstraint>(ref constraints, constraints.Length+1);

		points[points.Length-1] = points[points.Length-4] + Vector3.right*3;
		points[points.Length-2] = points[points.Length-4] + Vector3.right*2;
		points[points.Length-3] = points[points.Length-4] + Vector3.right;

        EnforceConstraint(GetControlPointCount()-1, ConstraintEnforcementType.Average);
	}

	public void AddCurveBeginning() {
		if(Loop)
			return;

		Vector3 point = points[0];
		Vector3 deriv = GetDerivRaw(0);

		Array.Resize<Vector3>(ref points, points.Length+3);
		Array.Resize<HandleConstraint>(ref constraints, constraints.Length+1);
		ShiftArray<Vector3>(ref points, 3, 0);
		ShiftArray<HandleConstraint>(ref constraints, 1, 0);

		points[0] = point - 3*deriv;
		points[1] = point - 2*deriv;
		points[2] = point-deriv;

		constraints[0] = HandleConstraint.Free;

        EnforceConstraint(0, ConstraintEnforcementType.Average);
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

		Vector3 point = GetPointRaw((float)elt-0.5f);
		Vector3 deriv = GetDirectionRaw((float)elt-0.5f);
		
		Array.Resize<Vector3>(ref points, points.Length+3);
		ShiftArray<Vector3>(ref points, 3, elt*3-1);

		Array.Resize<HandleConstraint>(ref constraints, constraints.Length+1);
		ShiftArray<HandleConstraint>(ref constraints, 1, elt);

		points[elt*3] = point;
		points[elt*3+1] = point+deriv;
		points[elt*3-1] = point-deriv;
        constraints[elt] = HandleConstraint.Aligned;
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
