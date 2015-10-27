using UnityEngine;
using System.Collections;

public class SplineCameraFollower : MonoBehaviour {

	public CameraShot[] Shots;
	public Transform Camera;
	public Transform Fade;
	public float FadeLength;

	private int CurrentShot = 0;
	private float SplineProgress = 0;
	private Material FadeMaterial;

	void Start () {
		if(Shots.Length == 0 || Camera == null)
			return;

		FadeMaterial = Fade.GetComponent<Renderer>().material;
		ApplyShot();
	}

	void Update () {
		
	}

	private void ApplyShot() {
		Camera.position = Shots[CurrentShot].Path.GetPoint(SplineProgress);
		if(Shots[CurrentShot].LookAt != null)
			Camera.LookAt(Shots[CurrentShot].LookAt);
		else
			Camera.rotation = Quaternion.LookRotation(Shots[CurrentShot].Path.GetDirection(SplineProgress));
	}

	[System.Serializable]
	public class CameraShot {
		public Transform LookAt;
		public Spline Path;
		public float Velocity;
	}
}
