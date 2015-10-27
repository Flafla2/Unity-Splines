using UnityEngine;

public class Bezier : MonoBehaviour {

	public Vector3 p0, p1, p2, p3;

	public void Reset() {
		p0 = new Vector3(1,0,0);
		p1 = new Vector3(2,0,0);
		p2 = new Vector3(3,0,0);
		p3 = new Vector3(4,0,0);
	}

	public Vector3 GetPoint(float t) {
		return transform.TransformPoint(PointOnBezier(p0,p1,p2,p3,t));
	}

	public Vector3 GetDirection(float t) {
		return DerivOnBezier(p0,p1,p2,p3,t).normalized;
	}

	// Second Order (Three point / parabolic) bezier solver.
	// Equation: (1-t)^2*P_0 + 2*t*(1-t)*P_1 + t^2*P_2
	public static Vector3 PointOnBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t) {
		return (1-t)*(1-t)*p0 + 2*t*(1-t)*p1 + t*t*p2;
	}

	// First derivative of Second Order (Three point / parabolic) bezier solver.
	// Equation: 2*(P_1-P_0)*(1-t) + 2*t*(P_2-P_1)
	public static Vector3 DerivOnBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t) {
		return 2*(1-t)*(p1-p0) + 2*t*(p2-p1);
	}

	// Third Order (Four point) bezier solver
	// Equation: (1-t)^3*P_0 + 3*t*(1-t)^2*P_1 + 3*t^2*(1-t)*P_2 + t^3*P_3
	public static Vector3 PointOnBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t) {
		return (1-t)*(1-t)*(1-t)*p0 + 3*t*(1-t)*(1-t)*p1 + 3*t*t*(1-t)*p2 + t*t*t*p3;
	}

	// First derivative of Third Order (Four point) bezier solver.
	// Equation: 3*(1-t)^2*(P_1-P_0) + 6*t*(1-t)*(P_2-P_1) + 3*t^2*(P_3-P_2)
	public static Vector3 DerivOnBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t) {
		return 3*(1-t)*(1-t)*(p1-p0) + 6*t*(1-t)*(p2-p1) + 3*t*t*(p3-p2);
	}

	// Finds the value t after moving a "real" distance `dist`.  This can be used to move along a curve at a constant
	// velocity.
	public static float GetTWithRealDistance(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t0, float dist) {
		return dist / DerivOnBezier(p0,p1,p2,p3,t0).magnitude;
	}

}
